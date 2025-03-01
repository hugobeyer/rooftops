using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using RoofTops;

namespace RoofTops
{
    /// <summary>
    /// A reworked unified manager to spawn tridotes, jump pads, and props
    /// using a simpler "pattern" approach: random gaps in time/distance, 
    /// with items always spawning at a fixed start Z and destroyed at end Z.
    /// Frequencinothinges are still probability based (like the old code).
    /// </summary>
    public class UnifiedSpawnManager : MonoBehaviour
    {
        [Header("Spawn Objects")]
        [SerializeField] private GameObject tridotPrefab;
        [SerializeField] private GameObject jumpPadPrefab;
        [SerializeField] private GameObject[] propPrefabs;

        [Header("Spawn Frequencies")]
        [Tooltip("Probability of spawning a tridots (0 to 1).")]
        [SerializeField] private float tridotFrequency = 0.1f;

        [Tooltip("Probability of spawning a jump pad (0 to 1).")]
        [SerializeField] private float jumpPadFrequency = 1.1f;

        [Tooltip("Probability of spawning a prop (0 to 1).")]
        [SerializeField] private float propFrequency = 1.1f;

        [Header("Spawn Settings (like PatternSpawning)")]
        [Tooltip("Fixed Z position where items spawn")]
        [SerializeField] private float startSpawnZ = 64f;

        [Tooltip("Z position where items are destroyed")]
        [SerializeField] private float endSpawnZ = -64f;

        [Tooltip("Minimum gap (in distance) before next spawn")]
        [SerializeField] private float minGap = 3f;

        [Tooltip("Maximum gap (in distance) before next spawn")]
        [SerializeField] private float maxGap = 15f;

        [Tooltip("Fixed X position for spawned items (0 = center)")]
        [SerializeField] private float fixedXPosition = 0f;

        [Tooltip("Y offset for spawned items (height)")]
        [SerializeField] private float spawnY = 0.0f;

        [Header("Spawn Delay")]
        [Tooltip("Time (seconds) to wait before spawning anything when the game starts.")]
        [SerializeField] private float spawnDelay = 13f;

        // Accessors for external difficulty modifications
        public float TridotFrequency { get => tridotFrequency; set => tridotFrequency = value; }
        public float JumpPadFrequency { get => jumpPadFrequency; set => jumpPadFrequency = value; }
        public float PropFrequency  { get => propFrequency;  set => propFrequency  = value; }

        // Internal references
        private List<GameObject> activeItems = new List<GameObject>();
        private float timeSinceLastSpawn = 0f;
        private float nextSpawnGap = 0f;

        // NEW: We'll maintain a list of valid spawn point transforms
        private List<Transform> spawnPoints = new List<Transform>();

        private bool canSpawn = false;
        private bool isMoving = true; // Add this flag to control movement

        private void Start()
        {
            // Instead of spawning immediately, wait for 'spawnDelay' seconds
            Invoke(nameof(EnableSpawning), spawnDelay);

            // Gather all spawn points once at startup
            spawnPoints = FindAllSpawnPoints();

            // Set initial random gap
            SetNextSpawnGap();
        }

        private void EnableSpawning()
        {
            canSpawn = true;
        }

        private void Update()
        {
            if (!canSpawn || !isMoving) return; // Skip if not moving or can't spawn

            // Optional check: Only spawn if the game is started
            if (GameManager.Instance != null && !GameManager.Instance.HasGameStarted)
                return;

            float speed = 0f;
            if (GameManager.Instance != null)
            {
                speed = Mathf.Abs(GameManager.Instance.CurrentSpeed);
            }

            // If speed is near zero, items won't be moving, so we can skip
            if (speed < 0.1f)
            {
                return;
            }

            // 1) Spawning logic
            if (ShouldSpawnItem(speed))
            {
                SpawnItem();
                timeSinceLastSpawn = 0f;    // reset
                SetNextSpawnGap();         // new random gap
            }
            else
            {
                timeSinceLastSpawn += Time.deltaTime;
            }

            // 2) Move existing items
            MoveActiveItems(speed);
        }

        private bool ShouldSpawnItem(float speed)
        {
            // Convert gap distance to time at current speed
            float timeToTravelGap = nextSpawnGap / speed;
            return timeSinceLastSpawn >= timeToTravelGap;
        }

        private void SetNextSpawnGap()
        {
            nextSpawnGap = Random.Range(minGap, maxGap);
        }

        private void SpawnItem()
        {
            // If no spawn points exist, skip spawning
            if (spawnPoints.Count == 0)
            {
                Debug.LogWarning("No spawn points found. Skipping spawn.");
                return;
            }

            // Probability-based approach (like before)
            float roll = Random.value;
            float cumulativeChance = 0f;

            float nothingChance = Mathf.Max(0, 1f - (tridotFrequency + jumpPadFrequency + propFrequency));
            cumulativeChance += nothingChance;

            if (roll < cumulativeChance)
                return; // "Spawn nothing"

            GameObject spawnedItem = null;

            // Spawn logic for tridots / jump pad / prop
            cumulativeChance += tridotFrequency;
            if (roll < cumulativeChance && tridotPrefab != null)
            {
                spawnedItem = Instantiate(tridotPrefab, Vector3.zero, Quaternion.identity);
            }
            else
            {
                cumulativeChance += jumpPadFrequency;
                if (roll < cumulativeChance && jumpPadPrefab != null)
                    spawnedItem = Instantiate(jumpPadPrefab, Vector3.zero, Quaternion.identity);
                else if (propPrefabs != null && propPrefabs.Length > 0)
                {
                    int randomIndex = Random.Range(0, propPrefabs.Length);
                    spawnedItem = Instantiate(propPrefabs[randomIndex], Vector3.zero, Quaternion.identity);
                }
            }

            if (spawnedItem != null)
            {
                // Pick a random spawn point from our list
                Transform chosenPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
                
                // Place the item at the chosen point
                spawnedItem.transform.position = chosenPoint.position;

                // Track in activeItems so we can move/destroy it
                activeItems.Add(spawnedItem);

                Debug.Log($"[UnifiedSpawnManager] Spawned {spawnedItem.name} at {chosenPoint.position}.");
            }
        }

        private void MoveActiveItems(float speed)
        {
            // Move them in -Z and destroy if crossing endSpawnZ
            List<GameObject> toRemove = new List<GameObject>();

            foreach (GameObject item in activeItems)
            {
                if (item == null)
                {
                    toRemove.Add(item);
                    continue;
                }

                item.transform.position -= Vector3.forward * speed * Time.deltaTime;

                if (item.transform.position.z <= endSpawnZ)
                {
                    toRemove.Add(item);
                    Destroy(item);
                }
            }

            // Clean up
            foreach (GameObject item in toRemove)
            {
                activeItems.Remove(item);
            }
        }

        public void UpdateSpawnFrequencies(float newTridotFrequency, float newJumpPadFrequency, float newPropFrequency)
        {
            tridotFrequency = Mathf.Clamp01(newTridotFrequency);
            jumpPadFrequency = Mathf.Clamp01(newJumpPadFrequency);
            propFrequency   = Mathf.Clamp01(newPropFrequency);
        }

        // Add StopMovement method to stop spawning and moving items
        public void StopMovement()
        {
            isMoving = false;
        }

        // We can optionally remove or replace OnDrawGizmos if you still want planes.
        // For now, let's keep a simple gizmo to show spawn & end Z.

        private void OnDrawGizmos()
        {
            // Draw spawn plane
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawCube(new Vector3(fixedXPosition, 5f, startSpawnZ), new Vector3(10, 10, 0.1f));

            // Draw end plane
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawCube(new Vector3(fixedXPosition, 5f, endSpawnZ), new Vector3(10, 10, 0.1f));
        }

        // NEW: This method replicates your old "FindAllSpawnPoints" logic (adapt as needed).
        private List<Transform> FindAllSpawnPoints()
        {
            var results = new List<Transform>();

            // Example: find by tag
            var allTransforms = FindObjectsOfType<Transform>(true);
            var spawnPointsByTag = allTransforms.Where(t => t.CompareTag("SpawnPoints")).ToList();
            results.AddRange(spawnPointsByTag);

            // Or find by name if you want:
            var spawnPointsByName = allTransforms.Where(t =>
                t.name.Contains("SpawnPoint") || 
                t.name.Contains("Spawn_Point") || 
                t.name.Contains("SpawnPos") || 
                t.name.Contains("ItemSpawn"))
                .ToList();

            // Combine them, ignoring duplicates
            results.AddRange(spawnPointsByName.Except(spawnPointsByTag));

            return results;
        }
    }
}
