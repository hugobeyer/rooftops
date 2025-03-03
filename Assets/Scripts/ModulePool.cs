using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace RoofTops
{
    public class ModulePool : MonoBehaviour
    {
        public static ModulePool Instance { get; private set; }

        // Add a delegate and event for module spawn/recycle notifications
        public delegate void ModuleSpawnedDelegate(GameObject module);
        public event ModuleSpawnedDelegate OnModuleSpawned;

        [Header("Module Settings")]
        public List<GameObject> modulePrefabs; // Assign all your module prefabs here
        public int numberOfModulesOnScreen = 5; // How many modules remain active at any time
        [SerializeField] private int initialPoolSizePerPrefab = 5; // New: Pre-populate pool

        [Header("Movement Settings")]
        public Transform moduleMovement; // Node that will be moved to simulate environment motion
        public Transform moduleVolume;   // Volume that defines the spawn/removal boundaries
        private float _gameSpeed;
        public float gameSpeed {
            get => _gameSpeed;
            private set => _gameSpeed = value;
        }

        // For backwards compatibility
        public float currentMoveSpeed { get { return gameSpeed; } set { gameSpeed = value; } }

        [Header("Debug")]
        public bool showDebugVisuals = true;

        [Header("Difficulty Progression")]
        public float constantGapRate = 0.1f;
        public float constantHeightRate = 0.05f;

        [Header("Randomness Settings")]
        public float gapRandomPercent = 0.1f;  // ±10% randomness
        public float heightRandomPercent = 0.1f; // ±10% randomness

        [Header("Ground Tags")]
        public string solidGroundTag = "GroundCollider";
        public string triggerGroundTag = "GroundedTrigger";

        [Header("Random Gap Settings")]
        public float gapRandomMin = 0.85f;
        public float gapRandomMax = 1.15f;

        [Header("Random Height Offset")]
        public float minHeightOffset = -0.5f;
        public float maxHeightOffset = 0.5f;

        public List<GameObject> activeModules = new List<GameObject>();
        private Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();
        private BoxCollider volumeCollider;
        private float volumeBackBoundary;
        private float volumeFrontBoundary;
        private float gameStartTime;

        private float TimeSinceStart => 
            !GameManager.Instance.HasGameStarted ? 0 : 
            Time.time - gameStartTime;

        private const float GRID_UNIT = 0.5f;

        private bool isMoving = true;
        private GameObject lastSpawnedPrefab;
#pragma warning disable 0414
        private float totalDistance = 0f;
#pragma warning restore 0414
        private bool canSpawnTridotAndJumpPads = true;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Create the module movement parent if not assigned
            if (moduleMovement == null)
            {
                moduleMovement = new GameObject("Module Movement").transform;
            }

            poolDictionary = new Dictionary<string, Queue<GameObject>>();
            activeModules = new List<GameObject>();

            if (!ValidateSetup()) return;

            // Pre-populate the pool
            InitializePool();

            gameStartTime = Time.time;
            gameSpeed = GameManager.Instance.initialGameSpeed;
            InitializeVolumeBoundaries();
            SpawnInitialModules();

            // Don't start moving until the game starts
            isMoving = false;
            
            // Subscribe to game start event
            if (GameManager.Instance != null)
            {
                GameManager.Instance.onGameStarted.AddListener(OnGameStart);
            }
        }

        void OnValidate()
        {
            numberOfModulesOnScreen = Mathf.Max(2, numberOfModulesOnScreen);
            initialPoolSizePerPrefab = Mathf.Max(1, initialPoolSizePerPrefab); // Ensure valid pool size
        }

        bool ValidateSetup()
        {
            if (modulePrefabs == null || modulePrefabs.Count == 0)
            {
                Debug.LogError($"{gameObject.name}: No module prefabs assigned!");
                return false;
            }
            if (!moduleVolume || !(volumeCollider = moduleVolume.GetComponent<BoxCollider>()))
            {
                Debug.LogError($"{gameObject.name}: Module volume or BoxCollider missing!");
                return false;
            }
            if (!moduleMovement)
            {
                Debug.LogError($"{gameObject.name}: No module movement transform assigned!");
                return false;
            }
            modulePrefabs.RemoveAll(prefab => prefab != null && prefab.name.ToLower().Contains("vista"));
            foreach (var prefab in modulePrefabs)
            {
                if (prefab == null || !prefab.GetComponent<BoxCollider>())
                {
                    Debug.LogError($"{gameObject.name}: Invalid prefab {prefab?.name} - missing BoxCollider!");
                    return false;
                }
            }
            // Check GameManager reference
            if (GameManager.Instance == null)
            {
                Debug.LogError($"{gameObject.name}: GameManager instance not found!");
                return false;
            }
            return true;
        }

        void InitializePool()
        {
            foreach (var prefab in modulePrefabs)
            {
                string key = prefab.name;
                poolDictionary[key] = new Queue<GameObject>();
                for (int i = 0; i < initialPoolSizePerPrefab; i++)
                {
                    GameObject module = Instantiate(prefab);
                    module.SetActive(false);
                    module.transform.SetParent(moduleMovement);
                    ConfigureGroundTags(module);
                    poolDictionary[key].Enqueue(module);
                }
            }
            Debug.Log($"Pool initialized with {initialPoolSizePerPrefab} modules per prefab.");
        }

        void InitializeVolumeBoundaries()
        {
            volumeBackBoundary = moduleVolume.transform.position.z - volumeCollider.size.z * moduleVolume.transform.localScale.z / 2;
            volumeFrontBoundary = moduleVolume.transform.position.z + volumeCollider.size.z * moduleVolume.transform.localScale.z / 2;
        }

        void SpawnInitialModules()
        {
            bool originalSpawnState = canSpawnTridotAndJumpPads;
            canSpawnTridotAndJumpPads = false;

            GameObject firstModule = GetModuleFromPool(GetRandomModulePrefab());
            firstModule.transform.SetParent(moduleMovement);
            BoxCollider firstBC = firstModule.GetComponent<BoxCollider>();
            firstModule.transform.position = new Vector3(
                0, 
                0, 
                volumeBackBoundary - (firstBC.center.z * firstModule.transform.localScale.z)
            );
            firstModule.SetActive(true);
            activeModules.Add(firstModule);
            
            // Notify that the first module is ready for items
            OnModuleSpawned?.Invoke(firstModule);

            GameObject lastModule = firstModule;
            BoxCollider lastBC = firstBC;

            for (int i = 1; i < numberOfModulesOnScreen; i++)
            {
                GameObject module = GetModuleFromPool(GetRandomModulePrefab());
                module.transform.SetParent(moduleMovement);
                module.SetActive(true);
                BoxCollider bc = module.GetComponent<BoxCollider>();
                float lastModuleEnd = lastModule.transform.position.z + 
                                     (lastBC.center.z + lastBC.size.z / 2) * lastModule.transform.localScale.z;
                module.transform.position = new Vector3(
                    0, 
                    0, 
                    lastModuleEnd - (bc.center.z - bc.size.z / 2) * module.transform.localScale.z
                );
                activeModules.Add(module);
                
                // Notify that this module is ready for items
                OnModuleSpawned?.Invoke(module);
                
                lastModule = module;
                lastBC = bc;
            }

            canSpawnTridotAndJumpPads = originalSpawnState;
        }

        void Update()
        {
            if (!isMoving)
                return;
            
            // Don't move modules if the game hasn't started
            if (!GameManager.Instance.HasGameStarted)
            {
                gameSpeed = GameManager.Instance.initialGameSpeed;
                return; // Add this return to prevent movement before game starts
            }
            
            float deltaTime = Time.deltaTime;
            // Always use GameManager's speed increase logic, ignoring DifficultyManager
            gameSpeed += GameManager.Instance.speedIncreaseRate * deltaTime;
            
            moduleMovement.Translate(Vector3.back * gameSpeed * deltaTime); // Single Translate call

            activeModules.RemoveAll(module => module == null); // Clean up nulls
            if (activeModules.Count == 0) return;

            RecycleModulesIfNeeded();
            MaintainModuleCount();
        }

        void RecycleModulesIfNeeded()
        {
            if (activeModules[0] == null) return;
            GameObject firstModule = activeModules[0];
            BoxCollider bc = firstModule.GetComponent<BoxCollider>();
            float moduleEnd = firstModule.transform.position.z + 
                             (bc.center.z + bc.size.z / 2) * firstModule.transform.localScale.z;

            if (moduleEnd < volumeBackBoundary)
            {
                GameObject lastModule = activeModules[activeModules.Count - 1];
                BoxCollider lastBC = lastModule.GetComponent<BoxCollider>();
                float lastModuleEnd = lastModule.transform.position.z + 
                                     (lastBC.center.z + lastBC.size.z / 2) * lastModule.transform.localScale.z;

                float gap = CalculateGap();
                float heightVariation = CalculateHeightVariation(firstModule.transform.position.y);
                float newZPosition = lastModuleEnd + gap - (bc.center.z - bc.size.z / 2) * firstModule.transform.localScale.z;
                float newYPosition = firstModule.transform.position.y + heightVariation;

                // Clean up existing spawned items on this module before recycling
                CleanUpModuleSpawnItems(firstModule);

                firstModule.transform.position = new Vector3(0, newYPosition, newZPosition);
                activeModules.RemoveAt(0);
                activeModules.Add(firstModule);
                
                // Notify that this module has been recycled and is ready for new items
                OnModuleSpawned?.Invoke(firstModule);
            }
        }
        
        private void CleanUpModuleSpawnItems(GameObject module)
        {
            if (module == null) return;
            
            string moduleType = module.name.Replace("(Clone)", "");
            
            // Track debugging info
            int itemsRemoved = 0;
            
            Debug.Log($"About to clean module of type {moduleType}");
            
            // ONLY remove actual instantiated items (ones with "Clone" in name)
            // This is the safest approach that won't break the modules
            for (int i = module.transform.childCount - 1; i >= 0; i--)
            {
                if (i >= module.transform.childCount) continue; // Safety check
                
                Transform child = module.transform.GetChild(i);
                if (child == null) continue;
                
                // Only remove objects that are clearly instantiated at runtime (have Clone in name)
                // and are not SpawnPoints or essential components
                if (child.name.Contains("(Clone)") && 
                    !child.name.Contains("SpawnPoint") &&
                    !child.CompareTag("SpawnPoints") &&
                    !child.CompareTag(solidGroundTag) && 
                    !child.CompareTag(triggerGroundTag))
                {
                    // This is a safe item to remove
                    Destroy(child.gameObject);
                    itemsRemoved++;
                }
            }
            
            Debug.Log($"Cleaned up {itemsRemoved} items from module {moduleType}");
        }

        void MaintainModuleCount()
        {
            while (activeModules.Count < numberOfModulesOnScreen)
            {
                GameObject newModule = GetModuleFromPool(GetRandomModulePrefab());
                if (newModule == null) break; // Safety check

                BoxCollider newBC = newModule.GetComponent<BoxCollider>();
                GameObject lastModule = activeModules[activeModules.Count - 1];
                BoxCollider lastBC = lastModule.GetComponent<BoxCollider>();
                float lastModuleEnd = lastModule.transform.position.z + 
                                     (lastBC.center.z + lastBC.size.z / 2) * lastModule.transform.localScale.z;

                float gap = CalculateGap();
                float heightVariation = CalculateHeightVariation(newModule.transform.position.y);
                float newZPosition = lastModuleEnd + gap - (newBC.center.z - newBC.size.z / 2) * newModule.transform.localScale.z;
                float newYPosition = newModule.transform.position.y + heightVariation;

                newModule.transform.position = new Vector3(0, newYPosition, newZPosition);
                newModule.SetActive(true);
                activeModules.Add(newModule);
                
                // Notify that this new module is ready for items
                OnModuleSpawned?.Invoke(newModule);
            }
        }

        private float CalculateGap()
        {
            // If constantGapRate is 0 or very close to 0, don't add any gap
            if (Mathf.Approximately(constantGapRate, 0f))
            {
                return 0f;
            }
            
            float baseGap = constantGapRate;
            
            // Only apply randomness if we have a non-zero gap and gapRandomPercent is greater than 0
            if (gapRandomPercent > 0)
            {
                // Use the min and max random values from the inspector
                float randomFactor = Random.Range(gapRandomMin, gapRandomMax);
                return baseGap * randomFactor;
            }
            
            return baseGap;
        }

        private float CalculateHeightVariation(float currentHeight)
        {
            // If constantHeightRate is 0 or very close to 0, don't add any height variation
            if (Mathf.Approximately(constantHeightRate, 0f))
            {
                return 0f;
            }
            
            // Only apply randomness if heightRandomPercent is greater than 0
            if (heightRandomPercent > 0)
            {
                // Use the min and max height offset values from the inspector
                // Multiply by constantHeightRate to scale the offsets based on difficulty
                float minOffset = minHeightOffset * constantHeightRate;
                float maxOffset = maxHeightOffset * constantHeightRate;
                
                return Random.Range(minOffset, maxOffset);
            }
            
            // If no randomness, just return 0 (no height change)
            return 0f;
        }

        void SpawnModule()
        {
            if (activeModules.Count == 0)
            {
                SpawnInitialModules();
                return;
            }

            GameObject prefab = GetRandomModulePrefab();
            GameObject module = GetModuleFromPool(prefab);
            if (module == null) return; // Safety check

            BoxCollider newBC = module.GetComponent<BoxCollider>();
            module.transform.SetParent(moduleMovement);
            module.SetActive(true);

            GameObject lastModule = activeModules[activeModules.Count - 1];
            BoxCollider lastBC = lastModule.GetComponent<BoxCollider>();
            float lastModuleEnd = lastModule.transform.position.z + 
                                 (lastBC.center.z + lastBC.size.z / 2) * lastModule.transform.localScale.z;

            float gap = CalculateGap();
            float heightVariation = CalculateHeightVariation(module.transform.position.y);
            float newZPosition = lastModuleEnd + gap - (newBC.center.z - newBC.size.z / 2) * module.transform.localScale.z;
            float newYPosition = module.transform.position.y + heightVariation;

            module.transform.position = new Vector3(0, newYPosition, newZPosition);
            activeModules.Add(module);

            if (canSpawnTridotAndJumpPads && GameManager.Instance.HasGameStarted)
            {
                // Your tridots/jump pad spawn logic here
            }
        }

        GameObject GetModuleFromPool(GameObject prefab)
        {
            if (prefab == null) return null;
            string key = prefab.name;
            GameObject newModule;

            if (!poolDictionary.ContainsKey(key))
            {
                poolDictionary[key] = new Queue<GameObject>();
            }

            if (poolDictionary[key].Count > 0)
            {
                newModule = poolDictionary[key].Dequeue();
                
                // Make sure to clean up any leftover spawned items when retrieving from pool
                CleanUpModuleSpawnItems(newModule);
            }
            else
            {
                newModule = Instantiate(prefab);
                newModule.transform.SetParent(moduleMovement);
                ConfigureGroundTags(newModule);
            }

            newModule.SetActive(true);
            return newModule;
        }

        void ReturnModuleToPool(GameObject module)
        {
            if (module == null) return;
            module.SetActive(false);
            string key = module.name.Replace("(Clone)", ""); // Handle clone suffix
            if (!poolDictionary.ContainsKey(key))
            {
                poolDictionary[key] = new Queue<GameObject>();
            }
            poolDictionary[key].Enqueue(module);
        }

        public string GetDifficultyInfo()
        {
            // Updated difficulty info to reflect constant rates
            return $"Constant Gap Rate: {constantGapRate:F2} m/s\n" +
                   $"Constant Height Rate: {constantHeightRate:F2} m/s";
        }

        public void SetMovement(bool moving)
        {
            isMoving = moving;
        }

        public GameObject GetNextModule()
        {
            if (activeModules.Count > 1)
            {
                return activeModules[1];
            }
            return null;
        }

        private GameObject GetRandomModulePrefab()
        {
            List<GameObject> availablePrefabs = modulePrefabs.FindAll(p => p != lastSpawnedPrefab);
            if (availablePrefabs.Count == 0) availablePrefabs = modulePrefabs;
            GameObject selectedPrefab = availablePrefabs[Random.Range(0, availablePrefabs.Count)];
            lastSpawnedPrefab = selectedPrefab;
            return selectedPrefab;
        }

        void ConfigureGroundTags(GameObject module)
        {
            foreach (Transform child in module.transform)
            {
                Collider col = child.GetComponent<Collider>();
                if (col != null)
                {
                    child.tag = col.isTrigger ? triggerGroundTag : solidGroundTag;
                }
            }
        }

        public void ResetSpeed()
        {
            gameSpeed = GameManager.Instance.initialGameSpeed;
        }

        public void ResetDistance()
        {
            totalDistance = 0f;
        }

        public void StopMovement()
        {
            isMoving = false;
        }

        // New: Full pool reset for game restarts
        public void ResetPool()
        {
            foreach (var module in activeModules)
            {
                if (module != null) ReturnModuleToPool(module);
            }
            activeModules.Clear();
            moduleMovement.position = Vector3.zero; // Reset movement position
            SpawnInitialModules();
            ResetSpeed();
            ResetDistance();
            isMoving = true;
        }

        void OnDestroy()
        {
            // Unsubscribe from events to prevent memory leaks
            if (GameManager.Instance != null)
            {
                GameManager.Instance.onGameStarted.RemoveListener(OnGameStart);
            }
            
            // Cleanup
            if (Instance == this) Instance = null;
        }

        public float GetMaxModuleHeight()
        {
            if (activeModules.Count == 0) return 0f;
            
            float uiZ = GameManager.Instance.gameplayUI.transform.position.z;
            float maxHeight = 0f;
            
            foreach (GameObject module in activeModules)
            {
                if (module != null)
                {
                    BoxCollider bc = module.GetComponent<BoxCollider>();
                    if (bc != null)
                    {
                        float moduleStart = module.transform.position.z - (bc.center.z - bc.size.z / 2) * module.transform.localScale.z;
                        float moduleEnd = module.transform.position.z + (bc.center.z + bc.size.z / 2) * module.transform.localScale.z;
                        
                        if (uiZ >= moduleStart && uiZ <= moduleEnd)
                        {
                            float moduleHeight = module.transform.position.y + 
                                                (bc.center.y + bc.size.y / 2) * module.transform.localScale.y;
                            maxHeight = moduleHeight;
                            break;
                        }
                    }
                }
            }
            
            return maxHeight;
        }

        // Method to set game speed externally (used by DifficultyManager)
        public void SetGameSpeed(float newSpeed)
        {
            // Ignore calls from DifficultyManager, but allow other components to set speed
            if (UnityEngine.StackTraceUtility.ExtractStackTrace().Contains("DifficultyManager"))
            {
                // Do nothing when called from DifficultyManager
                return;
            }
            
            gameSpeed = newSpeed;
        }

        // Add this method to handle game start
        private void OnGameStart()
        {
            // Start moving when the game starts
            isMoving = true;
            // Reset the game start time
            gameStartTime = Time.time;
        }
    }
}





