using System.Collections.Generic;
using UnityEngine;

namespace RoofTops
{
    /// <summary>
    /// Simple coin spawner that creates coins at module heights with random gaps.
    /// </summary>
    public class PatternSpawning : MonoBehaviour
    {
        [Header("Coin Settings")]
        [Tooltip("Array of coin prefabs to spawn randomly")]
        public GameObject[] coinPrefabs;

        [Tooltip("Scale of spawned coins")]
        public float coinScale = 1.0f;

        [Tooltip("Random rotation of coins")]
        public bool randomRotation = false;

        [Header("Spawn Settings")]
        [Tooltip("The Z position where coins spawn")]
        public float startSpawnZ = 64f;

        [Tooltip("The Z position where coins are destroyed")]
        public float endSpawnZ = -64f;

        [Tooltip("Min gap between coins")]
        public float minGap = 3f;
        
        [Tooltip("Max gap between coins")]
        public float maxGap = 15f;

        [Tooltip("Y offset above modules")]
        public float heightOffset = 1.0f;
        
        [Tooltip("Fixed X position for coins (0 = center)")]
        public float fixedXPosition = 0f;

        // Private variables
        private ModulePool modulePool;
        private GameObject coinsContainer;
        private List<GameObject> activeCoins = new List<GameObject>();
        private float timeSinceLastSpawn = 0f;
        private float nextSpawnGap = 0f;
        private List<float> availableHeights = new List<float>();

        private void Start()
        {
            // Get reference to ModulePool
            modulePool = ModulePool.Instance;
            if (modulePool == null)
            {
                Debug.LogError("CoinSpawner: ModulePool instance not found!");
                enabled = false;
                return;
            }

            // Validate coin prefabs
            if (coinPrefabs == null || coinPrefabs.Length == 0)
            {
                Debug.LogError("No coin prefabs assigned! Please assign at least one coin prefab.");
                enabled = false;
                return;
            }

            // Create container for coins
            coinsContainer = new GameObject("CoinsContainer");
            
            // Set initial spawn gap
            ResetSpawnGap();
            
            Debug.Log("PatternSpawning started. Movement direction: -Z (forced)");
        }

        private void Update()
        {
            if (modulePool == null || modulePool.activeModules.Count == 0)
                return;
                
            // Update available heights from modules
            UpdateAvailableHeights();
            
            // Check if it's time to spawn a new coin
            if (ShouldSpawnCoin())
            {
                // Spawn a coin at random height
                SpawnCoin();
                
                // Reset timer and set new gap
                timeSinceLastSpawn = 0f;
                ResetSpawnGap();
            }
            else
            {
                // Increment timer
                timeSinceLastSpawn += Time.deltaTime;
            }
            
            // Move existing coins
            MoveCoins();
        }
        
        private void ResetSpawnGap()
        {
            // Calculate a random gap for the next coin
            nextSpawnGap = Random.Range(minGap, maxGap);
        }
        
        private bool ShouldSpawnCoin()
        {
            // Speed-based timing calculation
            float speed = Mathf.Abs(modulePool.currentMoveSpeed);
            if (speed <= 0.1f) return false; // Don't spawn if barely moving
            
            // Calculate time needed to travel the gap distance
            float timeToTravel = nextSpawnGap / speed;
            
            // Spawn when enough time has passed
            return timeSinceLastSpawn >= timeToTravel;
        }
        
        private void UpdateAvailableHeights()
        {
            availableHeights.Clear();
            
            // Get heights from active modules
            foreach (var module in modulePool.activeModules)
            {
                if (module == null) continue;
                
                // Get module's height
                float moduleHeight = GetModuleHeight(module);
                if (!availableHeights.Contains(moduleHeight))
                {
                    availableHeights.Add(moduleHeight);
                }
            }
        }
        
        private float GetModuleHeight(GameObject module)
        {
            // Get the module's collider
            BoxCollider collider = module.GetComponent<BoxCollider>();
            if (collider != null)
            {
                return collider.bounds.max.y + heightOffset;
            }
            
            // Fallback to transform position
            return module.transform.position.y + heightOffset;
        }
        
        private void SpawnCoin()
        {
            if (coinPrefabs == null || coinPrefabs.Length == 0 || availableHeights.Count == 0)
                return;
                
            // Select a random coin prefab
            GameObject selectedCoinPrefab = coinPrefabs[Random.Range(0, coinPrefabs.Length)];
            if (selectedCoinPrefab == null) return;
                
            // Select a random height
            float height = availableHeights[Random.Range(0, availableHeights.Count)];
            
            // Create position at the start plane with fixed X position (no randomization)
            Vector3 spawnPos = new Vector3(fixedXPosition, height, startSpawnZ);
            
            // Spawn the coin
            GameObject coin = Instantiate(selectedCoinPrefab, spawnPos, Quaternion.identity);
            coin.transform.SetParent(coinsContainer.transform);
            
            // Set scale
            coin.transform.localScale = Vector3.one * coinScale;
            
            // Apply random rotation if enabled
            if (randomRotation)
            {
                coin.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            }
            
            // Track the coin
            activeCoins.Add(coin);
        }
        
        private void MoveCoins()
        {
            if (modulePool == null) return;
            
            // Always use negative Z movement with absolute speed value
            float speed = Mathf.Abs(modulePool.currentMoveSpeed);
            
            // Move and check each coin
            List<GameObject> coinsToRemove = new List<GameObject>();
            
            foreach (var coin in activeCoins)
            {
                if (coin == null)
                {
                    coinsToRemove.Add(coin);
                    continue;
                }
                
                // ALWAYS move in -Z direction
                coin.transform.position += Vector3.back * speed * Time.deltaTime;
                
                // Check if it's reached the end (-Z)
                if (coin.transform.position.z < endSpawnZ)
                {
                    coinsToRemove.Add(coin);
                    Destroy(coin);
                }
            }
            
            // Remove destroyed coins from the list
            foreach (var coin in coinsToRemove)
            {
                activeCoins.Remove(coin);
            }
        }
        
        private void OnDestroy()
        {
            // Clean up
            foreach (var coin in activeCoins)
            {
                if (coin != null)
                {
                    Destroy(coin);
                }
            }
            
            if (coinsContainer != null)
            {
                Destroy(coinsContainer);
            }
        }
        
        // For visualization in the editor
        private void OnDrawGizmosSelected()
        {
            // Draw spawn and destroy planes
            Gizmos.color = new Color(0, 1, 0, 0.3f); // Green
            Gizmos.DrawCube(new Vector3(fixedXPosition, 5, startSpawnZ), new Vector3(10, 10, 0.1f));
            
            Gizmos.color = new Color(1, 0, 0, 0.3f); // Red
            Gizmos.DrawCube(new Vector3(fixedXPosition, 5, endSpawnZ), new Vector3(10, 10, 0.1f));
            
            // Draw movement direction arrow (always -Z)
            Gizmos.color = Color.blue;
            Vector3 center = new Vector3(fixedXPosition, 5, (startSpawnZ + endSpawnZ) / 2);
            Gizmos.DrawRay(center, Vector3.back * 10f);
            
            // Add direction label
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(center + Vector3.up * 2, "Direction: ← Always -Z");
            #endif
        }
    }
} 