using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using RoofTops;
using System.Collections;

namespace RoofTops
{
    /// <summary>
    /// A reworked unified manager to spawn tridotes, jump pads, and props
    /// using a simpler "pattern" approach: random gaps in time/distance, 
    /// with items always spawning at a fixed start Z and destroyed at end Z.
    /// </summary>
    public class UnifiedSpawnManager : MonoBehaviour
    {
        [System.Serializable]
        public class SpawnableItem
        {
            public string name = "Item";
            public GameObject prefab;
            [Range(0f, 1f)]
            [Tooltip("Base probability of spawning this item (0 to 1)")]
            public float spawnFrequency = 0.2f;
            [Tooltip("Y offset for this item")]
            public float yOffset = 0f;
            [Tooltip("Enable/disable this item")]
            public bool enabled = true;
            [Tooltip("Prevent this item from spawning twice in a row")]
            public bool preventConsecutiveDuplicates = true;
            
            [Header("Chunk Control")]
            [Tooltip("Number of chunks to wait before this item starts appearing")]
            [Min(0)]
            public int startAfterChunk = 0;
            [Tooltip("Should this item stop appearing after a certain chunk?")]
            public bool hasEndChunk = false;
            [Tooltip("Chunk after which this item stops appearing")]
            [Min(0)]
            public int endAfterChunk = 10;
            
            [Header("Height-Based Spawning")]
            [Tooltip("Multiply spawn chance when height difference is detected")]
            public bool useHeightMultiplier = false;
            [Tooltip("Minimum height difference to apply multiplier")]
            public float minHeightDifference = 1f;
            [Tooltip("Maximum height difference to apply multiplier")]
            public float maxHeightDifference = 10f;
            [Tooltip("Multiplier to apply to spawn chance when height conditions are met")]
            public float heightMultiplier = 2.0f;
            [Tooltip("Should this item be preferred for upward or downward height changes?")]
            public HeightChangePreference heightPreference = HeightChangePreference.Both;
            
            [Header("Progression")]
            [Tooltip("Should this item's frequency change over time/chunks?")]
            public bool useProgressionAdjustment = false;
            [Tooltip("Percentage to increase/decrease spawn frequency per chunk (-100 to 100)")]
            [Range(-100f, 100f)]
            public float progressionPercentagePerChunk = 10f;
            
            // Current adjusted frequency based on progression
            [HideInInspector]
            public float currentAdjustedFrequency;
            
            // Initialize the current frequency
            public void Initialize()
            {
                currentAdjustedFrequency = spawnFrequency;
            }
            
            // Apply progression adjustment
            public void ApplyProgressionAdjustment(int chunksPassed)
            {
                if (!useProgressionAdjustment) return;
                
                // Calculate total percentage adjustment
                float totalPercentageAdjustment = progressionPercentagePerChunk * chunksPassed;
                
                // Apply the percentage adjustment to the base frequency
                float adjustedFrequency = spawnFrequency * (1f + (totalPercentageAdjustment / 100f));
                
                // Remove the Clamp01 to allow frequencies above 1.0
                currentAdjustedFrequency = adjustedFrequency;
            }
            
            // Get the effective frequency (considering progression)
            private static UnifiedSpawnManager _cachedManager;
            public float GetEffectiveFrequency()
            {
                // Get the base frequency (with or without progression adjustment)
                float baseFrequency = useProgressionAdjustment ? currentAdjustedFrequency : spawnFrequency;
                
                // Apply the global multiplier from the UnifiedSpawnManager
                if (_cachedManager == null)
                {
                    _cachedManager = FindObjectOfType<UnifiedSpawnManager>();
                }
                
                if (_cachedManager != null)
                {
                    float multipliedFrequency = baseFrequency * _cachedManager.globalFrequencyMultiplier;
                    Debug.Log($"Item {name}: Base frequency {baseFrequency:F3} * Global multiplier {_cachedManager.globalFrequencyMultiplier:F1} = {multipliedFrequency:F3}");
                    return multipliedFrequency;
                }
                
                return baseFrequency;
            }
            
            // Check if this item should be available in the current chunk
            public bool IsAvailableInChunk(int currentChunk)
            {
                // Check if we've reached the starting chunk
                bool hasStarted = currentChunk >= startAfterChunk;
                
                // Check if we've passed the ending chunk (if applicable)
                bool hasEnded = hasEndChunk && currentChunk > endAfterChunk;
                
                // Debug log to help diagnose issues with chunk availability
                if (startAfterChunk > 0 && !hasStarted)
                {
                    Debug.Log($"Item {name} not available yet: current chunk {currentChunk}, starts at chunk {startAfterChunk}");
                }
                
                return hasStarted && !hasEnded;
            }
        }

        public enum HeightChangePreference
        {
            Upward,    // Prefer when next platform is higher
            Downward,  // Prefer when next platform is lower
            Both       // No preference
        }

        [Header("Spawnable Items")]
        [SerializeField] private SpawnableItem[] spawnableItems;

        [Header("Spawn Settings")]
        [Tooltip("Fixed Z position where items spawn")]
        [SerializeField] private float startSpawnZ = 20f;

        [Tooltip("Z position where items are destroyed")]
        [SerializeField] private float endSpawnZ = -5f;

        [Tooltip("Fixed distance between spawns (in units)")]
        [SerializeField] private float spawnDistance = 1f;

        [Tooltip("Fixed X position for spawned items (0 = center)")]
        [SerializeField] private float fixedXPosition = 0f;

        [Header("Spawn Control")]
        [Tooltip("When false, no spawning will occur even if the game has started")]
        public bool enableSpawning = false;
        [Tooltip("Delay in seconds before spawning after activation")]
        public float spawnDelay = 0.5f;
        [Tooltip("Automatically increment chunk count based on distance")]
        public bool autoIncrementChunks = true;
        [Tooltip("Distance traveled to increment chunk counter")]
        public float distancePerChunk = 100f;

        [Header("Progression")]
        [Tooltip("Number of chunks/sections to consider for progression")]
        public int totalChunks = 10;
        [Tooltip("Current chunk/section (can be set externally)")]
        public int currentChunk = 0;

        [Header("Debug")]
        [Tooltip("Draw debug lines to visualize chunks")]
        public bool showChunkDebug = true;
        [Tooltip("Height of the chunk debug lines")]
        public float chunkDebugLineHeight = 5f;
        [Tooltip("Width of the chunk debug lines")]
        public float chunkDebugLineWidth = 10f;

        [Header("Global Settings")]
        [Tooltip("Global multiplier for all spawn frequencies")]
        [Range(0.1f, 5f)]
        public float globalFrequencyMultiplier = 1.0f;
        [Tooltip("Chance that nothing will spawn (0-1)")]
        [Range(0f, 1f)]
        public float nothingChance = 0.5f;

        // Internal references
        private List<GameObject> activeItems = new List<GameObject>();
        private float lastSpawnPosition = 0f;
        private float totalDistanceMoved = 0f;
        private float totalDistanceTraveled = 0f;
        private float lastChunkChangeDistance = 0f;
        private bool isMoving = true;
        private int lastSpawnedItemIndex = -1; // Track the last spawned item

        // Add these variables to the class
        private Queue<SpawnableItem> spawnQueue = new Queue<SpawnableItem>();
        private const int MAX_QUEUE_SIZE = 5;
        private bool isProcessingQueue = false;

        // Add this at the class level, near the other private variables
        private int groundLayerMask;

        // Add this near the top of the class
        public UnityEngine.Events.UnityEvent<int> onChunkChanged = new UnityEngine.Events.UnityEvent<int>();

        private void Start()
        {
            // Calculate max jumpable height using physics
            float gravity = Physics.gravity.magnitude;
           

            // Initialize all spawnable items
            foreach (var item in spawnableItems)
            {
                item.Initialize();
            }
            
            // Ensure currentChunk is at least 0
            currentChunk = Mathf.Max(0, currentChunk);
            
            // Log initial chunk state
            Debug.Log($"UnifiedSpawnManager starting at chunk {currentChunk}. Item availability:");
            foreach (var item in spawnableItems)
            {
                bool isAvailable = item.IsAvailableInChunk(currentChunk);
                Debug.Log($"  {item.name}: {(isAvailable ? "Available" : "Not Available")}, starts at chunk {item.startAfterChunk}");
                
                // Apply progression adjustment
                item.ApplyProgressionAdjustment(currentChunk);
            }
            
            // Initialize spawn position tracking
            lastSpawnPosition = 0f;
            totalDistanceMoved = 0f;
            
            // Set a higher default global multiplier if using very low frequencies
            bool usingLowFrequencies = true;
            foreach (var item in spawnableItems)
            {
                if (item.spawnFrequency > 0.05f)
                {
                    usingLowFrequencies = false;
                    break;
                }
            }
            
            if (usingLowFrequencies && globalFrequencyMultiplier == 1.0f)
            {
                globalFrequencyMultiplier = 10.0f;
                Debug.Log("Detected very low frequencies, automatically setting global multiplier to 10.0");
            }
            
            Debug.Log("UnifiedSpawnManager initialized with " + spawnableItems.Length + " spawnable items.");

            // Get the layer mask for the Ground layer
            groundLayerMask = 1 << LayerMask.NameToLayer("Ground");
            
            Debug.Log($"Ground layer mask initialized: {groundLayerMask}");
        }

        private void Update()
        {
            if (!isMoving || !enableSpawning) return;

            float speed = 0f;
            if (GameManager.Instance != null)
            {
                speed = Mathf.Abs(GameManager.Instance.CurrentSpeed);
            }
            else
            {
                // If no GameManager, use a default speed so spawning can still work
                speed = 5f;
            }

            // If speed is near zero, items won't be moving, so we can skip
            if (speed < 0.1f) return;

            // Track distance moved this frame
            float distanceMovedThisFrame = speed * Time.deltaTime;
            totalDistanceMoved += distanceMovedThisFrame;
            
            // Update progression if auto-increment is enabled
            if (autoIncrementChunks)
            {
                totalDistanceTraveled += distanceMovedThisFrame;
                
                // Check if we've traveled far enough to increment the chunk
                if (totalDistanceTraveled - lastChunkChangeDistance >= distancePerChunk)
                {
                    lastChunkChangeDistance = totalDistanceTraveled;
                    IncrementChunk();
                }
            }

            // Spawning logic - check if we've moved far enough for a spawn check
            if (totalDistanceMoved - lastSpawnPosition >= spawnDistance)
            {
                // Check each item independently
                CheckItemsForSpawning();
                
                // Update the last spawn position
                lastSpawnPosition = totalDistanceMoved;
            }
            
            // Process the spawn queue if we're not already doing so
            if (spawnQueue.Count > 0 && !isProcessingQueue)
            {
                StartCoroutine(ProcessSpawnQueue());
            }

            // Move existing items
            MoveActiveItems(speed);
        }

        // Add a new method to check each item independently
        private void CheckItemsForSpawning()
        {
            // Get only enabled items that are available in the current chunk
            List<SpawnableItem> availableItems = new List<SpawnableItem>();
            List<SpawnableItem> chunkExcludedItems = new List<SpawnableItem>();
            
            for (int i = 0; i < spawnableItems.Length; i++)
            {
                if (!spawnableItems[i].enabled || spawnableItems[i].prefab == null)
                {
                    continue;
                }
                
                if (spawnableItems[i].IsAvailableInChunk(currentChunk))
                {
                    availableItems.Add(spawnableItems[i]);
                }
                else if (spawnableItems[i].startAfterChunk > currentChunk)
                {
                    // This item is excluded because we haven't reached its starting chunk
                    chunkExcludedItems.Add(spawnableItems[i]);
                }
            }

            // Log excluded items for debugging
            if (chunkExcludedItems.Count > 0)
            {
                string excludedNames = string.Join(", ", chunkExcludedItems.Select(item => $"{item.name} (starts at {item.startAfterChunk})"));
                Debug.Log($"Items excluded due to chunk restrictions (current chunk: {currentChunk}): {excludedNames}");
            }

            if (availableItems.Count == 0)
            {
                Debug.LogWarning($"No available items for chunk {currentChunk}. Skipping spawn check.");
                return;
            }

            // Check each item independently
            foreach (var item in availableItems)
            {
                float itemFreq = item.GetEffectiveFrequency();
                float roll = Random.value;
                
                if (roll < itemFreq)
                {
                    // This item should spawn, add it to the queue if there's room
                    if (spawnQueue.Count < MAX_QUEUE_SIZE)
                    {
                        spawnQueue.Enqueue(item);
                        Debug.Log($"Item {item.name} queued for spawning (roll {roll:F3} < frequency {itemFreq:F3})");
                    }
                    else
                    {
                        Debug.Log($"Item {item.name} would spawn, but queue is full");
                    }
                }
                else
                {
                    Debug.Log($"Item {item.name} will not spawn (roll {roll:F3} >= frequency {itemFreq:F3})");
                }
            }
        }

        // Add a method to process the spawn queue
        private System.Collections.IEnumerator ProcessSpawnQueue()
        {
            isProcessingQueue = true;
            
            while (spawnQueue.Count > 0)
            {
                SpawnableItem itemToSpawn = spawnQueue.Dequeue();
                yield return StartCoroutine(SpawnSpecificItem(itemToSpawn));
                
                // Add a small delay between spawns
                yield return new WaitForSeconds(0.1f);
            }
            
            isProcessingQueue = false;
        }

        // Modify the SpawnItem method to handle a specific item
        private System.Collections.IEnumerator SpawnSpecificItem(SpawnableItem itemToSpawn)
        {
            yield return new WaitForSeconds(spawnDelay);

            // Get the spawn position base (without Y offset yet)
            Vector3 basePosition = new Vector3(fixedXPosition, 0f, startSpawnZ);
            
            // Get the actual ground height at spawn position
            float groundHeight = 0f;
            RaycastHit hit;
            bool foundGround = false;
            
            if (Physics.Raycast(basePosition + Vector3.up * 100f, Vector3.down, out hit, 200f))
            {
                groundHeight = hit.point.y;
                foundGround = true;
            }
            
            if (!foundGround)
            {
                Debug.LogWarning("No ground found at spawn position. Skipping spawn.");
                yield break;
            }
            
            // Check height difference ahead
            float heightDifference = GetHeightDifferenceAhead(basePosition);
            
            // If height difference is -1, it means no ground was found ahead, so don't spawn
            if (heightDifference < 0)
            {
                Debug.LogWarning("No ground found ahead of spawn position. Skipping spawn.");
                yield break;
            }
            
            bool isUpwardChange = IsHeightChangeUpward(basePosition);
            
            // Check if this would be a consecutive duplicate
            int itemIndex = System.Array.IndexOf(spawnableItems, itemToSpawn);
            if (itemToSpawn.preventConsecutiveDuplicates && itemIndex == lastSpawnedItemIndex)
            {
                Debug.Log($"Preventing consecutive duplicate of {itemToSpawn.name}");
                yield break;
            }
            
            // Remember this item for next time
            lastSpawnedItemIndex = itemIndex;
            
            // Create the spawn position with the item's Y offset RELATIVE TO THE GROUND HEIGHT
            Vector3 spawnPosition = new Vector3(
                fixedXPosition, 
                groundHeight + itemToSpawn.yOffset, // Add Y offset to actual ground height
                startSpawnZ
            );
            
            // Spawn the selected item
            GameObject spawnedItem = Instantiate(itemToSpawn.prefab, spawnPosition, Quaternion.identity);
            activeItems.Add(spawnedItem);
            
            string progressionInfo = itemToSpawn.useProgressionAdjustment ? 
                $", progression-adjusted frequency: {itemToSpawn.currentAdjustedFrequency:F3}" : "";
                
            Debug.Log($"[UnifiedSpawnManager] Spawned {itemToSpawn.name} at {spawnPosition} (ground height: {groundHeight}, Y offset: {itemToSpawn.yOffset}){progressionInfo}");
        }

        private void MoveActiveItems(float speed)
        {
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

            foreach (GameObject item in toRemove)
            {
                activeItems.Remove(item);
            }
        }

        public void StopMovement()
        {
            isMoving = false;
        }

        private void OnDrawGizmos()
        {
            // Only draw in edit mode or if debug is enabled
            if (!Application.isPlaying || showChunkDebug)
            {
                // For editor mode, we need to get the layer mask since Start() hasn't run
                int editorGroundLayerMask = 1 << LayerMask.NameToLayer("Ground");
                
                // Draw spawn plane
                Gizmos.color = new Color(0, 1, 0, 0.3f);
                Gizmos.DrawCube(new Vector3(fixedXPosition, 2.5f, startSpawnZ), new Vector3(5, 10, 0.1f));

                // Draw end plane
                Gizmos.color = new Color(1, 0, 0, 0.3f);
                Gizmos.DrawCube(new Vector3(fixedXPosition, 2.5f, endSpawnZ), new Vector3(5, 10, 0.1f));
                
                // Draw chunk debug lines
                if (showChunkDebug)
                {
                    Gizmos.color = Color.yellow;
                    
                    // Calculate how many chunks to show
                    int chunksToShow = totalChunks + 1;
                    
                    for (int i = 0; i <= chunksToShow; i++)
                    {
                        // Calculate Z position based on chunk distance
                        float zPos = startSpawnZ - (i * distancePerChunk);
                        
                        // Draw a horizontal line at this Z position
                        Vector3 lineStart = new Vector3(-chunkDebugLineWidth/2, chunkDebugLineHeight, zPos);
                        Vector3 lineEnd = new Vector3(chunkDebugLineWidth/2, chunkDebugLineHeight, zPos);
                        Gizmos.DrawLine(lineStart, lineEnd);
                        
                        // Draw a small vertical line at the center
                        Vector3 vertLineStart = new Vector3(0, chunkDebugLineHeight - 1, zPos);
                        Vector3 vertLineEnd = new Vector3(0, chunkDebugLineHeight + 1, zPos);
                        Gizmos.DrawLine(vertLineStart, vertLineEnd);
                        
                        // Highlight current chunk
                        if (i == currentChunk)
                        {
                            Gizmos.color = Color.red;
                            Gizmos.DrawSphere(new Vector3(0, chunkDebugLineHeight, zPos), 0.5f);
                            Gizmos.color = Color.yellow;
                        }
                    }
                }
                
                // Draw height difference visualization
                Vector3 currentPos = transform.position;
                
                // Draw a sphere at the current position
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(currentPos, 0.5f);
                
                // Draw spheres at half and full ahead positions
                Vector3 halfAheadPos = currentPos + Vector3.forward * 10f; // Now 10 units
                Vector3 fullAheadPos = currentPos + Vector3.forward * 20f; // Now 20 units
                
                Gizmos.color = Color.yellow; // Half distance
                Gizmos.DrawSphere(halfAheadPos, 0.4f);
                
                Gizmos.color = Color.green; // Full distance
                Gizmos.DrawSphere(fullAheadPos, 0.5f);
                
                // Try to visualize the height differences
                RaycastHit currentHit, halfAheadHit, fullAheadHit;
                bool currentHitFound = Physics.Raycast(currentPos + Vector3.up * 10f, Vector3.down, out currentHit, 20f, editorGroundLayerMask);
                bool halfAheadHitFound = Physics.Raycast(halfAheadPos + Vector3.up * 10f, Vector3.down, out halfAheadHit, 20f, editorGroundLayerMask);
                bool fullAheadHitFound = Physics.Raycast(fullAheadPos + Vector3.up * 10f, Vector3.down, out fullAheadHit, 20f, editorGroundLayerMask);
                
                if (currentHitFound)
                {
                    // Draw sphere at current hit point
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(currentHit.point, 0.3f);
                    
                    // Draw half distance visualization
                    if (halfAheadHitFound)
                    {
                        // Draw sphere at half ahead hit point
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawSphere(halfAheadHit.point, 0.3f);
                        
                        // Draw a line between current and half ahead hit points
                        Gizmos.DrawLine(currentHit.point, halfAheadHit.point);
                        
                        // Calculate and display the height difference
                        float halfHeightDiff = halfAheadHit.point.y - currentHit.point.y;
                        Vector3 halfMidPoint = (currentHit.point + halfAheadHit.point) / 2f;
                        
                        #if UNITY_EDITOR
                        UnityEditor.Handles.Label(halfMidPoint + Vector3.up * 1f, $"10m Diff: {halfHeightDiff:F2}");
                        #endif
                    }
                    
                    // Draw full distance visualization
                    if (fullAheadHitFound)
                    {
                        // Draw sphere at full ahead hit point
                        Gizmos.color = Color.green;
                        Gizmos.DrawSphere(fullAheadHit.point, 0.3f);
                        
                        // Draw a line between current and full ahead hit points
                        Gizmos.DrawLine(currentHit.point, fullAheadHit.point);
                        
                        // Calculate and display the height difference
                        float fullHeightDiff = fullAheadHit.point.y - currentHit.point.y;
                        Vector3 fullMidPoint = (currentHit.point + fullAheadHit.point) / 2f;
                        
                        #if UNITY_EDITOR
                        UnityEditor.Handles.Label(fullMidPoint + Vector3.up * 2f, $"20m Diff: {fullHeightDiff:F2}");
                        #endif
                    }
                }
            }
        }

        private float GetHeightDifferenceAhead(Vector3 position, float lookAheadDistance = 10f)
        {
            // Cast a ray down at the current position to find the ground
            RaycastHit currentHit;
            if (!Physics.Raycast(position + Vector3.up * 10f, Vector3.down, out currentHit, 20f, groundLayerMask))
            {
                Debug.LogWarning("No ground found at current position for height check");
                return 0f;
            }

            // Draw a red sphere at the current hit point
            Debug.DrawRay(position + Vector3.up * 10f, Vector3.down * 20f, Color.red, 2f);
            Debug.DrawLine(currentHit.point, currentHit.point + Vector3.up * 0.5f, Color.red, 2f);

            // Cast a ray down at the position ahead to find the ground
            Vector3 aheadPosition = position + Vector3.forward * lookAheadDistance;
            RaycastHit aheadHit;
            if (!Physics.Raycast(aheadPosition + Vector3.up * 10f, Vector3.down, out aheadHit, 20f, groundLayerMask))
            {
                Debug.LogWarning($"No ground found at ahead position (distance: {lookAheadDistance}) for height check");
                return 0f;
            }

            // Draw a green sphere at the ahead hit point
            Debug.DrawRay(aheadPosition + Vector3.up * 10f, Vector3.down * 20f, Color.green, 2f);
            Debug.DrawLine(aheadHit.point, aheadHit.point + Vector3.up * 0.5f, Color.green, 2f);

            // Calculate the height difference
            float heightDifference = aheadHit.point.y - currentHit.point.y;
            
            // Draw a line between the two hit points to visualize the height difference
            Debug.DrawLine(currentHit.point, aheadHit.point, Color.yellow, 2f);
            
            // Display the height difference as text in the scene
            Vector3 midPoint = (currentHit.point + aheadHit.point) / 2f;
            Debug.DrawLine(midPoint, midPoint + Vector3.up * 2f, Color.white, 2f);
            
            // Log the height difference
            Debug.Log($"Height difference detected at {lookAheadDistance}m ahead: {heightDifference:F2} units");
            
            return heightDifference;
        }

        // Get height difference at half distance ahead
        private float GetHalfDistanceHeightDifference(Vector3 position)
        {
            return GetHeightDifferenceAhead(position, 10f); // Half the standard distance (now 10 units)
        }

        // Get height difference at full distance ahead
        private float GetFullDistanceHeightDifference(Vector3 position)
        {
            return GetHeightDifferenceAhead(position, 20f); // Standard full distance (now 20 units)
        }

        private bool IsHeightChangeUpward(Vector3 position)
        {
            // Check both half and full distance
            float halfDistanceDiff = GetHalfDistanceHeightDifference(position);
            float fullDistanceDiff = GetFullDistanceHeightDifference(position);
            
            // Log both differences
            Debug.Log($"Height differences - Half (10m): {halfDistanceDiff:F2}, Full (20m): {fullDistanceDiff:F2}");
            
            // Consider it upward if either distance shows an upward trend
            bool isUpward = halfDistanceDiff > 0 || fullDistanceDiff > 0;
            
            // Log the direction
            Debug.Log($"Height change direction: {(isUpward ? "Upward" : "Downward")}");
            
            return isUpward;
        }

        // Method to increment the current chunk and update item frequencies
        public void IncrementChunk()
        {
            currentChunk = Mathf.Min(currentChunk + 1, totalChunks);
            
            // Fire the event with the new chunk index
            onChunkChanged.Invoke(currentChunk);
            
            // Update all item frequencies based on new chunk
            foreach (var item in spawnableItems)
            {
                item.ApplyProgressionAdjustment(currentChunk);
            }
            
            Debug.Log($"Advanced to chunk {currentChunk}/{totalChunks}. Updated item availability:");
            foreach (var item in spawnableItems)
            {
                bool isAvailable = item.IsAvailableInChunk(currentChunk);
                string progressionInfo = item.useProgressionAdjustment ? 
                    $", frequency: {item.currentAdjustedFrequency:F2}" : "";
                    
                Debug.Log($"  {item.name}: {(isAvailable ? "Available" : "Not Available")}{progressionInfo}");
            }
        }

        // Method to set the current chunk directly
        public void SetChunk(int chunk)
        {
            currentChunk = Mathf.Clamp(chunk, 0, totalChunks);
            
            // Update all item frequencies based on new chunk
            foreach (var item in spawnableItems)
            {
                item.ApplyProgressionAdjustment(currentChunk);
            }
        }

        // Fix the ShouldSpawnAnything method with proper math
        private bool ShouldSpawnAnything()
        {
            // Get only enabled items that are available in the current chunk
            List<SpawnableItem> availableItems = new List<SpawnableItem>();
            for (int i = 0; i < spawnableItems.Length; i++)
            {
                if (spawnableItems[i].enabled && 
                    spawnableItems[i].prefab != null && 
                    spawnableItems[i].IsAvailableInChunk(currentChunk))
                {
                    availableItems.Add(spawnableItems[i]);
                }
            }

            if (availableItems.Count == 0)
            {
                Debug.LogWarning($"No available items for chunk {currentChunk}. Skipping spawn.");
                return false;
            }
            
            // Calculate the total effective frequency of all available items
            float totalFrequency = 0f;
            foreach (var item in availableItems)
            {
                float itemFreq = item.GetEffectiveFrequency();
                totalFrequency += itemFreq;
                Debug.Log($"Item {item.name} frequency: {itemFreq:F3}");
            }
            
            // Roll a random number between 0 and 1
            float roll = Random.value;
            
            // If roll is less than total frequency, something should spawn
            bool shouldSpawn = roll < totalFrequency;
            Debug.Log($"Spawn check: Roll {roll:F3} vs Total frequency {totalFrequency:F3} = {(shouldSpawn ? "SPAWN" : "NO SPAWN")}");
            
            return shouldSpawn;
        }

        public void SetSpawningEnabled(bool enabled)
        {
            enableSpawning = enabled;
            Debug.Log("UnifiedSpawnManager spawning " + (enabled ? "enabled" : "disabled"));
            
            if (enabled)
            {
                // Force immediate spawn by setting last spawn position back
                lastSpawnPosition = totalDistanceMoved - spawnDistance;
            }
        }
        
        public void ForceSpawnItem()
        {
            if (isMoving && enableSpawning)
            {
                Debug.Log("[UnifiedSpawnManager] Force spawning all available items immediately");
                
                // Get all available items
                List<SpawnableItem> availableItems = new List<SpawnableItem>();
                for (int i = 0; i < spawnableItems.Length; i++)
                {
                    if (spawnableItems[i].enabled && 
                        spawnableItems[i].prefab != null && 
                        spawnableItems[i].IsAvailableInChunk(currentChunk))
                    {
                        availableItems.Add(spawnableItems[i]);
                    }
                }
                
                // Add one of each to the queue
                foreach (var item in availableItems)
                {
                    if (spawnQueue.Count < MAX_QUEUE_SIZE)
                    {
                        spawnQueue.Enqueue(item);
                    }
                }
                
                // Start processing the queue if not already doing so
                if (!isProcessingQueue)
                {
                    StartCoroutine(ProcessSpawnQueue());
                }
                
                lastSpawnPosition = totalDistanceMoved;
            }
        }
    }
}

