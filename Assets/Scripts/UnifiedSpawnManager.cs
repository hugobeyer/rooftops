using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RoofTops
{
    /// <summary>
    /// A unified manager to spawn bonuses, jump pads, and props
    /// at any transform tagged "SpawnPoints" inside a module.
    /// </summary>
    public class UnifiedSpawnManager : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private GameObject bonusPrefab;
        [SerializeField] private GameObject jumpPadPrefab;
        [SerializeField] private GameObject[] propPrefabs;

        [Tooltip("Probability of spawning a bonus (0 to 1).")]
        [SerializeField] private float bonusFrequency = 0.25f;

        [Tooltip("Probability of spawning a jump pad (0 to 1).")]
        [SerializeField] private float jumpPadFrequency = 0.25f;

        [Tooltip("Probability of spawning a prop (0 to 1).")]
        [SerializeField] private float propFrequency = 0.5f;
        
        [Header("Debug")]
        [SerializeField] private bool debugLogging = true;
        
        // Track metrics for debugging
        private Dictionary<string, int> modulesProcessed = new Dictionary<string, int>();
        private Dictionary<string, int> spawnPointsFound = new Dictionary<string, int>();
        private Dictionary<string, int> itemsSpawned = new Dictionary<string, int>();

        private ModulePool modulePool;

        private void Start()
        {
            modulePool = ModulePool.Instance;
            if (modulePool == null)
            {
                Debug.LogError("UnifiedSpawnManager: No ModulePool found in scene.");
                enabled = false;
                return;
            }

            // Subscribe to the module pool's OnModuleSpawned event
            modulePool.OnModuleSpawned += OnModuleReadyForSpawning;
            
            if (debugLogging)
            {
                Debug.Log($"UnifiedSpawnManager: Initialized with {(bonusPrefab ? "bonus prefab" : "NO BONUS PREFAB")}, " +
                    $"{(jumpPadPrefab ? "jump pad prefab" : "NO JUMP PAD PREFAB")}, " +
                    $"{(propPrefabs?.Length > 0 ? propPrefabs.Length + " prop prefabs" : "NO PROP PREFABS")}");
                
                // Log all module prefabs for debugging
                if (modulePool.modulePrefabs != null)
                {
                    foreach (var prefab in modulePool.modulePrefabs)
                    {
                        Debug.Log($"ModulePool has prefab: {prefab.name}");
                        
                        // Check for spawn points in each prefab
                        var prefabSpawnPoints = prefab.GetComponentsInChildren<Transform>(true)
                            .Where(t => t.CompareTag("SpawnPoints") || t.name.Contains("SpawnPoint"))
                            .ToList();
                            
                        Debug.Log($"Prefab {prefab.name} has {prefabSpawnPoints.Count} spawn points");
                    }
                }
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from the event when this script is destroyed
            if (modulePool != null)
            {
                modulePool.OnModuleSpawned -= OnModuleReadyForSpawning;
            }
        }
        
        private void OnModuleReadyForSpawning(GameObject module)
        {
            // Only spawn items if the game has started
            if (GameManager.Instance == null || !GameManager.Instance.HasGameStarted)
                return;
                
            if (module == null)
            {
                Debug.LogError("UnifiedSpawnManager: Received a null module to spawn on!");
                return;
            }
            
            // Track metrics by module type
            string moduleType = module.name.Replace("(Clone)", "");
            if (!modulesProcessed.ContainsKey(moduleType))
                modulesProcessed[moduleType] = 0;
            modulesProcessed[moduleType]++;
            
            if (debugLogging)
                Debug.Log($"Processing module {module.name} (type: {moduleType}, processed count: {modulesProcessed[moduleType]})");
                
            SpawnAtAllPoints(module);
            
            // Log module processing stats every 10 modules
            if (modulesProcessed.Values.Sum() % 10 == 0 && debugLogging)
            {
                LogModuleStats();
            }
        }
        
        private void LogModuleStats()
        {
            Debug.Log("=== MODULE SPAWNING STATISTICS ===");
            foreach (var kvp in modulesProcessed)
            {
                string moduleType = kvp.Key;
                int spawnPointCount = spawnPointsFound.ContainsKey(moduleType) ? spawnPointsFound[moduleType] : 0;
                int spawnedItemCount = itemsSpawned.ContainsKey(moduleType) ? itemsSpawned[moduleType] : 0;
                
                Debug.Log($"Module {moduleType}: Processed {kvp.Value} times, " +
                    $"found {spawnPointCount} spawn points, " +
                    $"spawned {spawnedItemCount} items");
            }
            Debug.Log("==================================");
        }

        /// <summary>
        /// Spawns bonuses, jump pads, or props at the spawn points found within the given module.
        /// </summary>
        /// <param name="module">The module in which to spawn items.</param>
        private void SpawnAtAllPoints(GameObject module)
        {
            if (!module) return;
            
            string moduleType = module.name.Replace("(Clone)", "");

            // Use a HashSet to ensure we don't have duplicate spawn points
            HashSet<Transform> spawnPointsSet = new HashSet<Transform>();
            
            // Method 1: Direct tag search
            var taggedPoints = module.GetComponentsInChildren<Transform>(true)
                .Where(t => t.CompareTag("SpawnPoints"))
                .ToList();
            foreach (var point in taggedPoints)
                spawnPointsSet.Add(point);
            
            // Method 2: Name search
            var namedPoints = module.GetComponentsInChildren<Transform>(true)
                .Where(t => !spawnPointsSet.Contains(t) && 
                       (t.name.Contains("SpawnPoint") || 
                        t.name.Contains("Spawn_Point") || 
                        t.name.Contains("SpawnPos") || 
                        t.name.Contains("ItemSpawn")))
                .ToList();
            foreach (var point in namedPoints)
                spawnPointsSet.Add(point);
            
            // Method 3: Check if there's a dedicated spawner component
            var spawnerComponents = module.GetComponentsInChildren<MonoBehaviour>(true)
                .Where(mb => mb.GetType().Name.Contains("Spawn") || mb.GetType().Name.Contains("Spawner"))
                .ToList();
                
            if (spawnerComponents.Count > 0 && debugLogging)
            {
                Debug.Log($"Found {spawnerComponents.Count} spawner components on {moduleType}");
            }
            
            // Convert back to list for easier processing
            List<Transform> spawnPoints = spawnPointsSet.ToList();
            
            // Track metrics
            if (!spawnPointsFound.ContainsKey(moduleType))
                spawnPointsFound[moduleType] = 0;
            spawnPointsFound[moduleType] += spawnPoints.Count;
                
            if (spawnPoints.Count == 0)
            {
                Debug.LogWarning($"No spawn points found in module {module.name} (type: {moduleType})! Will create fallback spawn points.");
                
                // FALLBACK: Create temporary spawn points at fixed positions on the module
                BoxCollider moduleCollider = module.GetComponent<BoxCollider>();
                if (moduleCollider != null)
                {
                    // Get module dimensions
                    Vector3 center = moduleCollider.center;
                    Vector3 size = moduleCollider.size;
                    float moduleLength = size.z * module.transform.localScale.z;
                    
                    // Create spawn points along the center line only
                    int pointsCount = 3; // Number of points along the center
                    for (int i = 0; i < pointsCount; i++)
                    {
                        float zPos = center.z - size.z/2 + moduleLength * (i+1)/(pointsCount+1);
                        
                        // One centered point only (no left/right)
                        GameObject spawnPoint = new GameObject($"TempSpawnPoint_{i}");
                        spawnPoint.transform.SetParent(module.transform);
                        spawnPoint.transform.localPosition = new Vector3(0, 0, zPos); // At ground level (y=0)
                        spawnPoint.tag = "SpawnPoints";
                        spawnPoints.Add(spawnPoint.transform);
                    }
                    
                    Debug.Log($"Created {pointsCount} fallback spawn points for module {moduleType}");
                }
            }
            
            if (debugLogging)
                Debug.Log($"Found {spawnPoints.Count} unique spawn points in module {module.name} (type: {moduleType})");

            // 2) For each point, decide what to spawn (ONE ITEM PER POINT)
            int itemsSpawnedCount = 0;
            
            // Don't normalize - instead allow for a chance of nothing spawning
            // The sum of all frequencies should be less than 1.0 to allow for "nothing" chance
            float nothingChance = Mathf.Max(0, 1f - (bonusFrequency + jumpPadFrequency + propFrequency));
            
            foreach (var point in spawnPoints)
            {
                float roll = Random.value; // random between 0 and 1
                float cumulativeChance = 0f;

                // First check if we should spawn nothing
                cumulativeChance += nothingChance;
                if (roll < cumulativeChance)
                {
                    // Spawn nothing at this point
                    continue; 
                }
                
                // Check for bonus
                cumulativeChance += bonusFrequency;
                if (roll < cumulativeChance && bonusPrefab != null)
                {
                    // Spawn bonus
                    GameObject bonus = Instantiate(bonusPrefab, point.position, point.rotation);
                    bonus.transform.SetParent(module.transform);
                    itemsSpawnedCount++;
                    
                    if (debugLogging && itemsSpawnedCount % 5 == 0)
                        Debug.Log($"Spawned bonus at {point.name} in {moduleType}");
                    continue;
                }
                
                // Check for jump pad
                cumulativeChance += jumpPadFrequency;
                if (roll < cumulativeChance && jumpPadPrefab != null)
                {
                    // Spawn jump pad
                    GameObject jumpPad = Instantiate(jumpPadPrefab, point.position, point.rotation);
                    jumpPad.transform.SetParent(module.transform);
                    itemsSpawnedCount++;
                    
                    if (debugLogging && itemsSpawnedCount % 5 == 0)
                        Debug.Log($"Spawned jump pad at {point.name} in {moduleType}");
                    continue;
                }
                
                // Check for prop
                if (propPrefabs != null && propPrefabs.Length > 0)
                {
                    // Spawn props
                    int randomIndex = Random.Range(0, propPrefabs.Length);
                    GameObject chosenProp = propPrefabs[randomIndex];
                    GameObject propInstance = Instantiate(chosenProp, point.position, point.rotation);
                    propInstance.transform.SetParent(module.transform);
                    itemsSpawnedCount++;
                    
                    if (debugLogging && itemsSpawnedCount % 5 == 0)
                        Debug.Log($"Spawned prop at {point.name} in {moduleType}");
                }
            }
            
            // Track metrics
            if (!itemsSpawned.ContainsKey(moduleType))
                itemsSpawned[moduleType] = 0;
            itemsSpawned[moduleType] += itemsSpawnedCount;
            
            if (debugLogging)
                Debug.Log($"Spawned {itemsSpawnedCount} items on module {module.name} (type: {moduleType})");
        }
    }
} 