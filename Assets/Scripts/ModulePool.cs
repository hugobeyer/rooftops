using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace RoofTops
{
    public class ModulePool : MonoBehaviour
    {
        public static ModulePool Instance { get; private set; }

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
        public float difficultyStartTime = 1f;  // When to start introducing gaps/variations
        public float maxGapSize = 8f;           // Maximum gap size at peak difficulty
        public float maxHeightVariation = 3f;   // Maximum height difference between modules
        public float difficultyRampDuration = 60f; // How long until reaching max difficulty
        public AnimationCurve gapDifficultyCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public AnimationCurve heightDifficultyCurve = AnimationCurve.Linear(0, 0, 1, 0.5f);

        [Header("Ground Tags")]
        public string solidGroundTag = "GroundCollider";
        public string triggerGroundTag = "GroundedTrigger";

        public List<GameObject> activeModules = new List<GameObject>();
        private Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();
        private BoxCollider volumeCollider;
        private float volumeBackBoundary;
        private float volumeFrontBoundary;
        private float gameStartTime;

        private float TimeSinceStart => 
            !GameManager.Instance.HasGameStarted ? 0 : 
            Time.time - gameStartTime;
        private float CurrentDifficultyFactor => 
            !GameManager.Instance.HasGameStarted ? 0 : 
            Mathf.Clamp01((TimeSinceStart - difficultyStartTime) / difficultyRampDuration);
        private float CurrentGapMultiplier => gapDifficultyCurve.Evaluate(CurrentDifficultyFactor);
        private float CurrentHeightMultiplier => heightDifficultyCurve.Evaluate(CurrentDifficultyFactor);

        private const float GRID_UNIT = 0.5f;

        private bool isMoving = true;
        private GameObject lastSpawnedPrefab;
#pragma warning disable 0414
        private float totalDistance = 0f;
#pragma warning restore 0414
        private bool canSpawnBonusAndJumpPads = true;

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

            isMoving = true;
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
            bool originalSpawnState = canSpawnBonusAndJumpPads;
            canSpawnBonusAndJumpPads = false;

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
                lastModule = module;
                lastBC = bc;
            }

            canSpawnBonusAndJumpPads = originalSpawnState;
        }

        public void Update()
        {
            if (GameManager.Instance.IsPaused || !isMoving) return;

            float deltaTime = Time.deltaTime;
            if (!GameManager.Instance.HasGameStarted)
            {
                gameSpeed = GameManager.Instance.initialGameSpeed;
            }
            else
            {
                gameSpeed += GameManager.Instance.speedIncreaseRate * deltaTime;
            }
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

                firstModule.transform.position = new Vector3(0, newYPosition, newZPosition);
                activeModules.RemoveAt(0);
                activeModules.Add(firstModule);
            }
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
            }
        }

        private float CalculateGap()
        {
            if (TimeSinceStart <= difficultyStartTime) return 0f;
            float maxCurrentGap = maxGapSize * CurrentGapMultiplier;
            int possibleIncrements = Mathf.FloorToInt(maxCurrentGap / GRID_UNIT);
            return possibleIncrements > 0 ? Random.Range(0, possibleIncrements + 1) * GRID_UNIT : 0f;
        }

        private float CalculateHeightVariation(float currentHeight)
        {
            if (TimeSinceStart <= difficultyStartTime) return 0f;
            float maxCurrentHeight = maxHeightVariation * CurrentHeightMultiplier;
            int possibleIncrements = Mathf.FloorToInt(maxCurrentHeight / GRID_UNIT);
            if (possibleIncrements <= 0) return 0f;
            int randomIncrements = Random.Range(-possibleIncrements, possibleIncrements + 1);
            float variation = randomIncrements * GRID_UNIT;
            return currentHeight + variation < 0 ? Mathf.Max(-currentHeight, 0) : variation;
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

            if (canSpawnBonusAndJumpPads && GameManager.Instance.HasGameStarted)
            {
                // Your bonus/jump pad spawn logic here
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
            if (TimeSinceStart <= difficultyStartTime)
                return "Difficulty not started";
            
            return $"Difficulty: {CurrentDifficultyFactor:P0}\n" +
                   $"Max Gap: {maxGapSize * CurrentDifficultyFactor:F1}m\n" +
                   $"Max Height: ±{maxHeightVariation * CurrentDifficultyFactor:F1}m";
        }

        public void SetMovement(bool moving)
        {
            isMoving = moving;
            if (moving) gameSpeed = GameManager.Instance.initialGameSpeed;
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
            gameSpeed = 0;
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

        public void SetGameSpeed(float speed) => _gameSpeed = speed;
    }
}