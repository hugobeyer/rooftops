using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ModulePool : MonoBehaviour
{
    public static ModulePool Instance { get; private set; }

    [Header("Module Settings")]
    public List<GameObject> modulePrefabs; // assign all your module prefabs here
    public int numberOfModulesOnScreen = 5; // how many modules remain active at any time

    [Header("Movement Settings")]
    public Transform moduleMovement; // node that will be moved to simulate environment motion
    public Transform moduleVolume;   // volume that defines the spawn/removal boundaries
    public float baseMoveSpeed = 6f;  // Starting at 6 m/s
    public float speedIncreaseRate = 0.1f;  // Increases by 0.1 m/s per second

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

    private float TimeSinceStart => Time.time - gameStartTime;
    private float CurrentDifficultyFactor => Mathf.Clamp01((TimeSinceStart - difficultyStartTime) / difficultyRampDuration);
    private float CurrentGapMultiplier => gapDifficultyCurve.Evaluate(CurrentDifficultyFactor);
    private float CurrentHeightMultiplier => heightDifficultyCurve.Evaluate(CurrentDifficultyFactor);

    private const float GRID_UNIT = 0.5f;

    private bool isMoving = true;  // New field to track movement state
    private GameObject lastSpawnedPrefab;
    public float currentMoveSpeed;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (!ValidateSetup()) return;
        gameStartTime = Time.time;
        currentMoveSpeed = baseMoveSpeed;
        InitializeVolumeBoundaries();
        SpawnInitialModules();

        // Ensure movement resets properly
        isMoving = true;
        currentMoveSpeed = baseMoveSpeed;
    }

    void OnValidate()
    {
        // Ensure we have valid settings
        numberOfModulesOnScreen = Mathf.Max(2, numberOfModulesOnScreen);
    }

    bool ValidateSetup()
    {
        if (modulePrefabs == null || modulePrefabs.Count == 0)
        {
            Debug.LogError($"{gameObject.name}: No module prefabs assigned!");
            return false;
        }

        if (!moduleVolume)
        {
            Debug.LogError($"{gameObject.name}: No module volume assigned!");
            return false;
        }

        volumeCollider = moduleVolume.GetComponent<BoxCollider>();
        if (!volumeCollider)
        {
            Debug.LogError($"{gameObject.name}: Module volume must have a BoxCollider!");
            return false;
        }

        if (!moduleMovement)
        {
            Debug.LogError($"{gameObject.name}: No module movement transform assigned!");
            return false;
        }

        // Remove any prefab with "vista" in its name
        modulePrefabs.RemoveAll(prefab => prefab.name.ToLower().Contains("vista"));

        // Validate remaining prefabs have BoxColliders
        foreach (var prefab in modulePrefabs)
        {
            if (!prefab.GetComponent<BoxCollider>())
            {
                Debug.LogError($"{gameObject.name}: Prefab {prefab.name} must have a BoxCollider!");
                return false;
            }
        }

        return true;
    }

    void InitializeVolumeBoundaries()
    {
        volumeBackBoundary = moduleVolume.transform.position.z - volumeCollider.size.z * moduleVolume.transform.localScale.z / 2;
        volumeFrontBoundary = moduleVolume.transform.position.z + volumeCollider.size.z * moduleVolume.transform.localScale.z / 2;
    }

    void SpawnInitialModules()
    {
        // Spawn first module at the back of the volume
        GameObject firstModule = GetModuleFromPool(GetRandomModulePrefab());
        firstModule.transform.SetParent(moduleMovement);
        BoxCollider firstBC = firstModule.GetComponent<BoxCollider>();
        
        // Position the first module so its collider starts at volumeBackBoundary
        firstModule.transform.position = new Vector3(
            0, 
            0,  // Force first module to ground level 
            volumeBackBoundary - (firstBC.center.z * firstModule.transform.localScale.z)
        );
        firstModule.SetActive(true);
        activeModules.Add(firstModule);

        // Spawn remaining modules forward with no gaps initially
        GameObject lastModule = firstModule;
        BoxCollider lastBC = firstBC;
        
        for (int i = 1; i < numberOfModulesOnScreen; i++)
        {
            GameObject module = GetModuleFromPool(GetRandomModulePrefab());
            module.transform.SetParent(moduleMovement);
            module.SetActive(true);

            BoxCollider bc = module.GetComponent<BoxCollider>();
            
            // Calculate where the last module ends
            float lastModuleEnd = lastModule.transform.position.z + 
                                (lastBC.center.z + lastBC.size.z/2) * lastModule.transform.localScale.z;
            
            // Position this module with no gap and at ground level initially
            module.transform.position = new Vector3(
                0, 
                0, 
                lastModuleEnd - (bc.center.z - bc.size.z/2) * module.transform.localScale.z
            );
            
            activeModules.Add(module);
            lastModule = module;
            lastBC = bc;
        }
    }

    void Update()
    {
        if(GameManager.Instance.IsPaused) return;

        if(isMoving)
        {
            currentMoveSpeed += speedIncreaseRate * Time.unscaledDeltaTime;
            moduleMovement.Translate(Vector3.back * currentMoveSpeed * Time.deltaTime);
        }

        // Clean up null references
        activeModules.RemoveAll(module => module == null);

        if (activeModules.Count == 0) return;

        RecycleModulesIfNeeded();
        MaintainModuleCount();
    }

    void RecycleModulesIfNeeded()
    {
        GameObject firstModule = activeModules[0];
        BoxCollider bc = firstModule.GetComponent<BoxCollider>();

        // Calculate where this module ends
        float moduleEnd = firstModule.transform.position.z + 
                         (bc.center.z + bc.size.z/2) * firstModule.transform.localScale.z;

        // If the module has moved completely past the volume's back boundary
        if (moduleEnd < volumeBackBoundary)
        {
            // Get the last module
            GameObject lastModule = activeModules[activeModules.Count - 1];
            BoxCollider lastBC = lastModule.GetComponent<BoxCollider>();

            // Calculate where the last module ends
            float lastModuleEnd = lastModule.transform.position.z + 
                                (lastBC.center.z + lastBC.size.z/2) * lastModule.transform.localScale.z;

            // Calculate gap and height variation based on current difficulty
            float gap = 0f;
            float heightVariation = 0f;
            
            if (TimeSinceStart > difficultyStartTime)
            {
                // Randomize gap in 0.5 unit increments
                float maxCurrentGap = maxGapSize * CurrentGapMultiplier;
                int possibleIncrements = Mathf.FloorToInt(maxCurrentGap / GRID_UNIT);
                if (possibleIncrements > 0)
                {
                    int randomIncrements = Random.Range(0, possibleIncrements + 1);
                    gap = randomIncrements * GRID_UNIT;
                }
                
                // Randomize height in 0.5 unit increments
                float maxCurrentHeight = maxHeightVariation * CurrentHeightMultiplier;
                int possibleHeightIncrements = Mathf.FloorToInt(maxCurrentHeight / GRID_UNIT);
                if (possibleHeightIncrements > 0)
                {
                    int randomHeightIncrements = Random.Range(-possibleHeightIncrements, possibleHeightIncrements + 1);
                    heightVariation = randomHeightIncrements * GRID_UNIT;
                    
                    // Ensure we don't go below ground level
                    float currentHeight = firstModule.transform.position.y;
                    if (currentHeight + heightVariation < 0)
                    {
                        heightVariation = Mathf.Max(-currentHeight, 0);
                        // Round to nearest grid unit
                        heightVariation = Mathf.Round(heightVariation / GRID_UNIT) * GRID_UNIT;
                    }
                }
            }

            // Position recycled module with gap and height variation
            firstModule.transform.position = new Vector3(
                0, 
                firstModule.transform.position.y + heightVariation, 
                lastModuleEnd + gap - (bc.center.z - bc.size.z/2) * firstModule.transform.localScale.z
            );
            
            // Move to end of list
            activeModules.RemoveAt(0);
            activeModules.Add(firstModule);
        }
    }

    void MaintainModuleCount()
    {
        while (activeModules.Count < numberOfModulesOnScreen)
        {
            GameObject newModule = GetModuleFromPool(GetRandomModulePrefab());
            BoxCollider newBC = newModule.GetComponent<BoxCollider>();

            moduleMovement.Translate(Vector3.back * currentMoveSpeed * Time.unscaledDeltaTime);

            // Get the last module
            GameObject lastModule = activeModules[activeModules.Count - 1];
            BoxCollider lastBC = lastModule.GetComponent<BoxCollider>();

            // Calculate where the last module ends
            float lastModuleEnd = lastModule.transform.position.z + 
                                (lastBC.center.z + lastBC.size.z/2) * lastModule.transform.localScale.z;

            // Calculate gap and height variation based on current difficulty
            float gap = 0f;
            float heightVariation = 0f;
            
            if (TimeSinceStart > difficultyStartTime)
            {
                // Randomize gap in 0.5 unit increments
                float maxCurrentGap = maxGapSize * CurrentGapMultiplier;
                int possibleIncrements = Mathf.FloorToInt(maxCurrentGap / GRID_UNIT);
                if (possibleIncrements > 0)
                {
                    int randomIncrements = Random.Range(0, possibleIncrements + 1);
                    gap = randomIncrements * GRID_UNIT;
                }
                
                // Randomize height in 0.5 unit increments
                float maxCurrentHeight = maxHeightVariation * CurrentHeightMultiplier;
                int possibleHeightIncrements = Mathf.FloorToInt(maxCurrentHeight / GRID_UNIT);
                if (possibleHeightIncrements > 0)
                {
                    int randomHeightIncrements = Random.Range(-possibleHeightIncrements, possibleHeightIncrements + 1);
                    heightVariation = randomHeightIncrements * GRID_UNIT;
                    
                    // Ensure we don't go below ground level
                    float currentHeight = newModule.transform.position.y;
                    if (currentHeight + heightVariation < 0)
                    {
                        heightVariation = Mathf.Max(-currentHeight, 0);
                        // Round to nearest grid unit
                        heightVariation = Mathf.Round(heightVariation / GRID_UNIT) * GRID_UNIT;
                    }
                }
            }

            // Position the new module with gap and height variation
            newModule.transform.position = new Vector3(
                0, 
                newModule.transform.position.y + heightVariation, 
                lastModuleEnd + gap - (newBC.center.z - newBC.size.z/2) * newModule.transform.localScale.z
            );

            activeModules.Add(newModule);
        }
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
        BoxCollider newBC = module.GetComponent<BoxCollider>();

        module.transform.SetParent(moduleMovement);
        module.SetActive(true);

        // Get the last module
        GameObject lastModule = activeModules[activeModules.Count - 1];
        BoxCollider lastBC = lastModule.GetComponent<BoxCollider>();

        // Calculate where the last module ends
        float lastModuleEnd = lastModule.transform.position.z + 
                            (lastBC.center.z + lastBC.size.z/2) * lastModule.transform.localScale.z;

        // Calculate gap and height variation based on current difficulty
        float gap = 0f;
        float heightVariation = 0f;
        
        if (TimeSinceStart > difficultyStartTime)
        {
            // Randomize gap in 0.5 unit increments
            float maxCurrentGap = maxGapSize * CurrentGapMultiplier;
            int possibleIncrements = Mathf.FloorToInt(maxCurrentGap / GRID_UNIT);
            if (possibleIncrements > 0)
            {
                int randomIncrements = Random.Range(0, possibleIncrements + 1);
                gap = randomIncrements * GRID_UNIT;
            }
            
            // Randomize height in 0.5 unit increments
            float maxCurrentHeight = maxHeightVariation * CurrentHeightMultiplier;
            int possibleHeightIncrements = Mathf.FloorToInt(maxCurrentHeight / GRID_UNIT);
            if (possibleHeightIncrements > 0)
            {
                int randomHeightIncrements = Random.Range(-possibleHeightIncrements, possibleHeightIncrements + 1);
                heightVariation = randomHeightIncrements * GRID_UNIT;
                
                // Ensure we don't go below ground level
                float currentHeight = module.transform.position.y;
                if (currentHeight + heightVariation < 0)
                {
                    heightVariation = Mathf.Max(-currentHeight, 0);
                    // Round to nearest grid unit
                    heightVariation = Mathf.Round(heightVariation / GRID_UNIT) * GRID_UNIT;
                }
            }
        }

        // Position the new module with gap and height variation
        module.transform.position = new Vector3(
            0, 
            module.transform.position.y + heightVariation, 
            lastModuleEnd + gap - (newBC.center.z - newBC.size.z/2) * module.transform.localScale.z
        );

        activeModules.Add(module);
    }

    GameObject GetModuleFromPool(GameObject prefab)
    {
        GameObject newModule;
        string key = prefab.name;
        
        if (poolDictionary.ContainsKey(key) && poolDictionary[key].Count > 0)
        {
            newModule = poolDictionary[key].Dequeue();
        }
        else
        {
            newModule = Instantiate(prefab);
        }
        
        newModule.transform.SetParent(moduleMovement);
        ConfigureGroundTags(newModule);
        return newModule;
    }

    void ReturnModuleToPool(GameObject module)
    {
        if (module == null) return;
        
        module.SetActive(false);
        string key = module.name;
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

    // New public method to stop/start movement
    public void SetMovement(bool moving)
    {
        isMoving = moving;
        if(moving) currentMoveSpeed = baseMoveSpeed;
    }

    // Add this new public method
    public GameObject GetNextModule()
    {
        // Return the next module in the list after the first one
        if (activeModules.Count > 1)
        {
            return activeModules[1];  // Index 1 is the next module
        }
        return null;
    }

    private GameObject GetRandomModulePrefab()
    {
        // Filter out the last spawned prefab
        List<GameObject> availablePrefabs = modulePrefabs.FindAll(p => p != lastSpawnedPrefab);
        
        // If no available prefabs (only one type exists), use original list
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
                // Assign tags based on collider type
                if (col.isTrigger)
                {
                    child.tag = triggerGroundTag;
                }
                else
                {
                    child.tag = solidGroundTag;
                }
            }
        }
    }

    public void ResetSpeed()
    {
        currentMoveSpeed = baseMoveSpeed;
    }

    void OnDestroy()
    {
        // Cleanup when the ModulePool is destroyed
        // PrepareForReset();
    }
}

