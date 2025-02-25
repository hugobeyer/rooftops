using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace RoofTops
{
    /// <summary>
    /// Visualization tool that draws a line connecting all active modules at their highest points,
    /// and spawns decorations that move along the Z-axis.
    /// </summary>
    public class ProgressLine : MonoBehaviour
    {
        [Header("Line Settings")]
        [Tooltip("Whether to show the line or just the decorations")]
        public bool showLine = true;
        
        [Tooltip("Color of the line")]
        public Color lineColor = Color.yellow;
        
        [Tooltip("Width of the line")]
        [Range(0.01f, 0.5f)]
        public float lineWidth = 0.1f;
        
        [Tooltip("Additional Y-axis offset from the top of modules")]
        public float additionalYOffset = 1.0f;
        
        [Tooltip("Draw mode for the line renderer")]
        public LineTextureMode textureMode = LineTextureMode.Tile;
        
        [Header("Connection Points")]
        [Tooltip("Show connection points with marker objects")]
        public bool showConnectionPoints = true;
        
        [Tooltip("Prefab for connection point markers (if null, spheres will be created)")]
        public GameObject connectionPointPrefab;
        
        [Tooltip("Scale of connection point markers")]
        public float markerScale = 0.5f;
        
        [Header("Moving Decoration System")]
        [Tooltip("Prefab to spawn as moving decorations")]
        public GameObject decorationPrefab;
        
        [Tooltip("Spacing between decorations in meters")]
        public float decorationSpacing = 1.0f;
        
        [Tooltip("Enable spawning decorations")]
        public bool enableDecorations = true;
        
        [Tooltip("Random rotation when spawned")]
        public bool randomRotation = true;
        
        [Tooltip("Scale of decoration objects")]
        public float decorationScale = 1.0f;
        
        [Header("Decoration Spawn Settings")]
        [Tooltip("The Z position where decorations start spawning")]
        public float startSpawnZ = -64f;
        
        [Tooltip("The Z position where decorations get destroyed")]
        public float endSpawnZ = 64f;
        
        [Tooltip("Override movement speed (set to 0 to use GameManager speed)")]
        public float overrideSpeed = 0f;
        
        [Tooltip("How often to spawn new decorations (in seconds)")]
        public float spawnInterval = 0.5f;
        
        [Header("Movement Direction")]
        [Tooltip("Direction decorations move along Z-axis. Set to -1 to move backward (decreasing Z) or 1 to move forward (increasing Z)")]
        public int movementDirection = 1;
        
        [Tooltip("Number of parallel lines to create")]
        public int lineCount = 5;
        
        [Tooltip("Vertical spacing between lines (in meters)")]
        public float lineSpacing = 1.0f;
        
        [Header("Coin Pattern Settings")]
        [Tooltip("Create rectangular patterns of coins instead of single coins")]
        public bool usePatterns = true;
        
        [Tooltip("Minimum number of coins along Z direction (depth)")]
        public int minPatternDepth = 2;
        
        [Tooltip("Maximum number of coins along Z direction (depth)")]
        public int maxPatternDepth = 7;
        
        [Tooltip("Minimum number of rows to use (1-5)")]
        public int minPatternRows = 1;
        
        [Tooltip("Maximum number of rows to use (1-5)")]
        public int maxPatternRows = 5;
        
        [Tooltip("Minimum gap between patterns (meters)")]
        public float minPatternGap = 5f;
        
        [Tooltip("Maximum gap between patterns (meters)")]
        public float maxPatternGap = 15f;
        
        // Container for all decorations
        private GameObject decorationsContainer;
        
        // Pattern building state
        private bool isCreatingPattern = false;
        private int currentPatternDepth = 0;
        private int currentPatternRows = 0;
        private int currentPatternColumn = 0;
        private int currentPatternRow = 0;
        private List<float> currentPatternHeights = new List<float>();
        private float currentPatternX = 0f;
        private List<float> currentPatternXPositions = new List<float>();
        
        private LineRenderer[] lineRenderers;
        private ModulePool modulePool;
        private List<GameObject> connectionMarkers = new List<GameObject>();
        private List<GameObject> activeDecorations = new List<GameObject>();
        
        // Spawn timing
        private float lastSpawnTime = 0f;
        private List<Vector3> spawnPositions = new List<Vector3>();
        private float nextPatternZ = 0f;
        
        private void Start()
        {
            // Create array of line renderers
            lineRenderers = new LineRenderer[lineCount];
            
            // Setup the main line renderer (or create if needed)
            LineRenderer mainRenderer = GetComponent<LineRenderer>();
            if (mainRenderer == null)
            {
                mainRenderer = gameObject.AddComponent<LineRenderer>();
            }
            lineRenderers[0] = mainRenderer;
            
            // Create additional line renderers as child objects
            for (int i = 1; i < lineCount; i++)
            {
                GameObject lineObj = new GameObject($"Line_{i}");
                lineObj.transform.SetParent(transform);
                lineObj.transform.localPosition = Vector3.zero;
                
                LineRenderer renderer = lineObj.AddComponent<LineRenderer>();
                lineRenderers[i] = renderer;
            }
            
            // Configure all line renderers
            foreach (var renderer in lineRenderers)
            {
                SetupLineRenderer(renderer);
            }
            
            // Get a reference to the module pool
            modulePool = ModulePool.Instance;
            if (modulePool == null)
            {
                Debug.LogError("ProgressLine: ModulePool instance not found!");
                enabled = false;
                return;
            }
            
            // Create decorations container
            if (decorationsContainer == null)
            {
                decorationsContainer = new GameObject("MovingDecorations");
                decorationsContainer.transform.position = Vector3.zero;
                decorationsContainer.transform.rotation = Quaternion.identity;
            }
            
            // Set initial visibility for all lines
            foreach (var renderer in lineRenderers)
            {
                renderer.enabled = showLine;
            }
        }
        
        private void SetupLineRenderer(LineRenderer renderer)
        {
            // Configure the line renderer with good defaults
            if (renderer.material == null)
            {
                renderer.material = new Material(Shader.Find("Sprites/Default"));
            }
            
            renderer.startColor = lineColor;
            renderer.endColor = lineColor;
            renderer.startWidth = lineWidth;
            renderer.endWidth = lineWidth;
            renderer.textureMode = textureMode;
            renderer.useWorldSpace = true;
            renderer.positionCount = 0; // Will be set dynamically
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
        
        private void LateUpdate()
        {
            if (modulePool == null || modulePool.activeModules == null || modulePool.activeModules.Count == 0)
                return;
            
            // Update line renderers visibility if it changed
            foreach (var renderer in lineRenderers)
            {
                if (renderer.enabled != showLine)
                {
                    renderer.enabled = showLine;
                }
            }
                
            // Update line visualization to show where objects will spawn
            UpdateLineRenderer();
            
            // Handle decoration spawning and movement
            if (enableDecorations && decorationPrefab != null)
            {
                SpawnDecorations();
                MoveDecorations();
            }
        }
        
        /// <summary>
        /// Toggle the visibility of the line
        /// </summary>
        /// <param name="show">Whether to show the line</param>
        public void ToggleLine(bool show)
        {
            showLine = show;
            if (lineRenderers != null)
            {
                foreach (var renderer in lineRenderers)
                {
                    if (renderer != null)
                    {
                        renderer.enabled = show;
                    }
                }
            }
        }
        
        private void UpdateLineRenderer()
        {
            List<GameObject> modules = modulePool.activeModules;
            
            // Clear existing markers
            ClearConnectionMarkers();
            
            // Store spawn positions for decorations
            spawnPositions.Clear();
            
            // Base points from modules
            List<Vector3> baseLinePoints = new List<Vector3>();
            
            // Generate positions for each module's first and last points
            foreach (var module in modules)
            {
                if (module == null)
                    continue;
                    
                // Get the front and back points of the module at maximum height
                Vector3 frontPoint = GetModulePoint(module, true); // Front point
                Vector3 backPoint = GetModulePoint(module, false); // Back point
                
                // Add points to the base line
                baseLinePoints.Add(frontPoint);
                baseLinePoints.Add(backPoint);
                
                // Create visual markers at connection points if enabled (only on base line)
                if (showConnectionPoints && showLine)
                {
                    CreateConnectionMarker(frontPoint);
                    CreateConnectionMarker(backPoint);
                }
                
                // Store base spawn positions
                Vector3 frontSpawnPoint = new Vector3(frontPoint.x, frontPoint.y, startSpawnZ);
                Vector3 backSpawnPoint = new Vector3(backPoint.x, backPoint.y, startSpawnZ);
                
                // Add to spawn positions - for the base line
                spawnPositions.Add(frontSpawnPoint);
                spawnPositions.Add(backSpawnPoint);
            }
            
            // If we have base points, use them for all lines with Y offsets
            if (baseLinePoints.Count > 0)
            {
                // Set up each line with appropriate Y offset
                for (int lineIndex = 0; lineIndex < lineCount; lineIndex++)
                {
                    LineRenderer renderer = lineRenderers[lineIndex];
                    float yOffset = lineIndex * lineSpacing;
                    
                    // Create points for this line
                    List<Vector3> linePoints = new List<Vector3>();
                    
                    // Copy base points with Y offset
                    foreach (var basePoint in baseLinePoints)
                    {
                        Vector3 offsetPoint = basePoint + new Vector3(0, yOffset, 0);
                        linePoints.Add(offsetPoint);
                        
                        // For lines above the base, add spawn points too
                        if (lineIndex > 0)
                        {
                            Vector3 spawnPoint = new Vector3(offsetPoint.x, offsetPoint.y, startSpawnZ);
                            spawnPositions.Add(spawnPoint);
                        }
                    }
                    
                    // Set the position count for this line renderer
                    renderer.positionCount = linePoints.Count;
                    
                    // Apply all points to the line renderer
                    for (int i = 0; i < linePoints.Count; i++)
                    {
                        renderer.SetPosition(i, linePoints[i]);
                    }
                }
            }
            else
            {
                // No modules found, clear line renderers
                foreach (var renderer in lineRenderers)
                {
                    renderer.positionCount = 0;
                }
            }
        }
        
        private void SpawnDecorations()
        {
            // Check if it's time to spawn new decorations
            if (Time.time - lastSpawnTime < spawnInterval || spawnPositions.Count == 0)
                return;
            
            // Reset the spawn timer
            lastSpawnTime = Time.time;
            
            if (usePatterns)
            {
                // If we're not currently building a pattern, initialize a new one
                if (!isCreatingPattern)
                {
                    // Initialize the next pattern position if needed
                    if (nextPatternZ == 0f)
                    {
                        nextPatternZ = startSpawnZ;
                    }
                    
                    // Only create a new pattern if it would be within our spawn bounds
                    bool canCreatePattern = (movementDirection > 0 && nextPatternZ <= endSpawnZ) || 
                                           (movementDirection < 0 && nextPatternZ >= endSpawnZ);
                    
                    if (canCreatePattern)
                    {
                        // Generate a random pattern size
                        currentPatternDepth = Random.Range(minPatternDepth, maxPatternDepth + 1);
                        currentPatternRows = Mathf.Min(Random.Range(minPatternRows, maxPatternRows + 1), lineCount);
                        
                        // Prepare the pattern's row heights
                        PreparePatternHeights(currentPatternRows);
                        
                        // Reset column counter to start spawning from first column
                        currentPatternColumn = 0;
                        
                        // Mark that we're now creating a pattern
                        isCreatingPattern = true;
                        
                        // Spawn the entire first column of coins
                        SpawnPatternColumn(currentPatternColumn);
                        
                        // Move to the next column
                        currentPatternColumn++;
                        
                        Debug.Log($"Started new pattern: {currentPatternDepth}x{currentPatternRows}, spawned column 1/{currentPatternDepth}");
                    }
                }
                else
                {
                    // We're already building a pattern, spawn the next column
                    SpawnPatternColumn(currentPatternColumn);
                    
                    Debug.Log($"Spawned pattern column {currentPatternColumn+1}/{currentPatternDepth}");
                    
                    // Move to the next column
                    currentPatternColumn++;
                    
                    // Check if we've completed the entire pattern
                    if (currentPatternColumn >= currentPatternDepth)
                    {
                        // We've finished this pattern
                        isCreatingPattern = false;
                        
                        // Calculate the next pattern position with a random gap
                        float patternLength = currentPatternDepth * decorationSpacing;
                        float patternGap = Random.Range(minPatternGap, maxPatternGap);
                        nextPatternZ += movementDirection * (patternLength + patternGap);
                        
                        Debug.Log($"Completed pattern: {currentPatternDepth}x{currentPatternRows}");
                    }
                }
            }
            else
            {
                // Original behavior: spawn just one decoration
                if (spawnPositions.Count > 0)
                {
                    int randomIndex = Random.Range(0, spawnPositions.Count);
                    SpawnDecoration(spawnPositions[randomIndex]);
                }
            }
        }
        
        private void PreparePatternHeights(int rows)
        {
            // Clear previous heights and X positions
            currentPatternHeights.Clear();
            currentPatternXPositions.Clear();
            
            // Group spawn positions by Y-coordinate to get the available heights
            Dictionary<float, List<Vector3>> heightGroups = new Dictionary<float, List<Vector3>>();
            
            foreach (var pos in spawnPositions)
            {
                float y = Mathf.Round(pos.y * 10f) / 10f; // Round to nearest 0.1 to group similar heights
                if (!heightGroups.ContainsKey(y))
                {
                    heightGroups[y] = new List<Vector3>();
                }
                heightGroups[y].Add(pos);
            }
            
            // Get the available heights sorted
            List<float> availableHeights = new List<float>(heightGroups.Keys);
            availableHeights.Sort();
            
            // If we don't have enough lines, reduce rows
            rows = Mathf.Min(rows, availableHeights.Count);
            if (rows <= 0) return;
            
            // Choose a random starting row if not using all rows
            int startRowIndex = 0;
            if (rows < availableHeights.Count)
            {
                startRowIndex = Random.Range(0, availableHeights.Count - rows + 1);
            }
            
            // Store the heights and corresponding X positions we'll use
            for (int i = 0; i < rows; i++)
            {
                float height = availableHeights[startRowIndex + i];
                currentPatternHeights.Add(height);
                
                // Get a random X position from the list of positions at this height
                if (heightGroups[height].Count > 0)
                {
                    // Select a specific X position for this row
                    Vector3 posAtHeight = heightGroups[height][Random.Range(0, heightGroups[height].Count)];
                    currentPatternXPositions.Add(posAtHeight.x);
                }
                else
                {
                    // Fallback in case there are no positions at this height
                    currentPatternXPositions.Add(0f);
                }
            }
            
            // We don't need this anymore as we're using specific X positions per row
            // currentPatternX = spawnPositions[Random.Range(0, spawnPositions.Count)].x;
        }
        
        // Bring back the method to spawn an entire column at once
        private void SpawnPatternColumn(int column)
        {
            if (decorationPrefab == null || currentPatternHeights.Count == 0)
                return;
            
            // Calculate the Z position for this column using standard decoration spacing
            float z = startSpawnZ + column * decorationSpacing * movementDirection;
            
            // Spawn coins for each row at this column
            for (int rowIndex = 0; rowIndex < currentPatternHeights.Count; rowIndex++)
            {
                float y = currentPatternHeights[rowIndex];
                // Use the row-specific X position
                float x = currentPatternXPositions[rowIndex];
                
                // Create coin position
                Vector3 coinPosition = new Vector3(x, y, z);
                
                // Spawn the coin
                SpawnDecoration(coinPosition);
            }
        }
        
        // We'll keep this method since it's still referenced in some places
        private void SpawnSingleCoin(int column, int row)
        {
            if (decorationPrefab == null || row >= currentPatternHeights.Count)
                return;
                
            // Calculate the Z position for this column
            float z = startSpawnZ + column * decorationSpacing * movementDirection;
            
            // Get the Y height and X position for this row
            float y = currentPatternHeights[row];
            float x = currentPatternXPositions[row];
            
            // Create coin position
            Vector3 coinPosition = new Vector3(x, y, z);
            
            // Spawn the coin
            SpawnDecoration(coinPosition);
        }
        
        // Extended decoration spawn method that includes Z-offset information
        private GameObject SpawnDecorationWithOffset(Vector3 position, float zOffset)
        {
            if (decorationPrefab == null)
                return null;
                
            // Create the decoration object
            GameObject decoration = Instantiate(decorationPrefab, position, Quaternion.identity);
            
            // Parent to decorations container
            decoration.transform.SetParent(decorationsContainer.transform);
            
            // Set scale
            decoration.transform.localScale = Vector3.one * decorationScale;
            
            // Set random rotation if enabled
            if (randomRotation)
            {
                decoration.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            }
            
            // Add pattern offset component to track relative position in pattern
            PatternOffset offsetComponent = decoration.AddComponent<PatternOffset>();
            offsetComponent.zOffset = zOffset;
            
            // Add to active decorations list
            activeDecorations.Add(decoration);
            
            return decoration;
        }
        
        // Update the regular SpawnDecoration method to use the extended version
        private void SpawnDecoration(Vector3 position)
        {
            SpawnDecorationWithOffset(position, 0f);
        }
        
        // Add a new component to track pattern offsets
        private class PatternOffset : MonoBehaviour
        {
            public float zOffset = 0f;
            public bool initialized = false;
            
            private void Start()
            {
                // Apply the initial offset on first frame
                if (!initialized)
                {
                    ApplyOffset();
                    initialized = true;
                }
            }
            
            public void ApplyOffset()
            {
                // Get the ProgressLine component
                ProgressLine progressLine = FindObjectOfType<ProgressLine>();
                if (progressLine != null)
                {
                    // Apply the pattern offset in the correct direction
                    transform.position += new Vector3(0, 0, zOffset * progressLine.movementDirection);
                }
            }
        }
        
        // Update the MoveDecorations method to maintain pattern structure
        private void MoveDecorations()
        {
            // Get movement speed directly from ModulePool which has the actual movement speed
            float speed = overrideSpeed;
            if (speed == 0f && ModulePool.Instance != null)
            {
                // Use ModulePool's direct movement speed
                speed = ModulePool.Instance.currentMoveSpeed;
            }
            
            // Apply movement and check for decorations that need to be removed
            List<GameObject> decorationsToRemove = new List<GameObject>();
            
            foreach (var decoration in activeDecorations)
            {
                if (decoration == null)
                {
                    decorationsToRemove.Add(decoration);
                    continue;
                }
                
                // Apply the pattern offset on the first frame if needed
                PatternOffset offset = decoration.GetComponent<PatternOffset>();
                if (offset != null && !offset.initialized)
                {
                    offset.ApplyOffset();
                    offset.initialized = true;
                }
                
                // Move the decoration along Z axis based on direction
                Vector3 moveDirection = new Vector3(0, 0, movementDirection);
                decoration.transform.position += moveDirection * speed * Time.deltaTime;
                
                // Check if it passed the end plane - note we check differently based on movement direction
                bool reachedEnd = false;
                if (movementDirection > 0)
                {
                    // Moving toward positive Z, check if we passed endSpawnZ
                    reachedEnd = decoration.transform.position.z > endSpawnZ;
                }
                else
                {
                    // Moving toward negative Z, check if we passed endSpawnZ
                    reachedEnd = decoration.transform.position.z < endSpawnZ;
                }
                
                if (reachedEnd)
                {
                    decorationsToRemove.Add(decoration);
                    Destroy(decoration);
                }
            }
            
            // Remove destroyed decorations from the list
            foreach (var decoration in decorationsToRemove)
            {
                activeDecorations.Remove(decoration);
            }
        }
        
        private void CreateConnectionMarker(Vector3 position)
        {
            GameObject marker;
            
            if (connectionPointPrefab != null)
            {
                // Use the provided prefab
                marker = Instantiate(connectionPointPrefab, position, Quaternion.identity, transform);
            }
            else
            {
                // Create a simple sphere
                marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.transform.SetParent(transform);
                marker.transform.position = position;
                marker.transform.localScale = Vector3.one * markerScale;
                
                // Set material color
                Renderer renderer = marker.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = new Material(Shader.Find("Standard"));
                    renderer.material.color = lineColor;
                }
                
                // Remove collider to avoid physics interactions
                Collider collider = marker.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }
            }
            
            connectionMarkers.Add(marker);
        }
        
        private void ClearConnectionMarkers()
        {
            foreach (var marker in connectionMarkers)
            {
                if (marker != null)
                {
                    Destroy(marker);
                }
            }
            
            connectionMarkers.Clear();
        }
        
        private Vector3 GetModulePoint(GameObject module, bool isFront)
        {
            // Get the module's collider
            BoxCollider collider = module.GetComponent<BoxCollider>();
            if (collider != null)
            {
                // Calculate the Z position (front or back edge)
                float zOffset = isFront ? -collider.size.z/2 : collider.size.z/2;
                Vector3 localEdge = new Vector3(0, 0, zOffset) + collider.center;
                
                // Calculate the maximum Y height
                float maxY = collider.bounds.max.y;
                
                // Create a point at the specified edge with maximum height
                Vector3 point = module.transform.TransformPoint(localEdge);
                point.y = maxY + additionalYOffset; // Use max Y plus additional offset
                
                return point;
            }
            
            // Fallback to transform position if no collider
            Vector3 posOffset = isFront ? Vector3.back : Vector3.forward;
            return module.transform.position + posOffset + Vector3.up * additionalYOffset;
        }
        
        private void OnDestroy()
        {
            // Clean up any remaining markers
            ClearConnectionMarkers();
            
            // Clean up all decorations
            foreach (var decoration in activeDecorations)
            {
                if (decoration != null)
                {
                    Destroy(decoration);
                }
            }
            activeDecorations.Clear();
            
            // Clean up containers
            if (decorationsContainer != null)
            {
                Destroy(decorationsContainer);
            }
        }

        // For visualizing the spawn zone in the editor
        private void OnDrawGizmosSelected()
        {
            // Draw the Z limits as vertical planes
            Gizmos.color = new Color(1f, 0.5f, 0, 0.3f); // Orange semi-transparent
            Vector3 center = transform.position;
            
            // Create a plane at the start Z
            Vector3 startCenter = new Vector3(center.x, center.y, startSpawnZ);
            Gizmos.DrawCube(startCenter, new Vector3(20f, 10f, 0.1f));
            
            // Create a plane at the end Z
            Vector3 endCenter = new Vector3(center.x, center.y, endSpawnZ);
            Gizmos.DrawCube(endCenter, new Vector3(20f, 10f, 0.1f));
            
            // Draw a line connecting them with an arrow
            Gizmos.color = new Color(1f, 0.5f, 0, 1f); // Solid orange
            Gizmos.DrawLine(startCenter, endCenter);
            
            // Draw direction arrow
            Vector3 direction = (endCenter - startCenter).normalized;
            Vector3 arrowPos = Vector3.Lerp(startCenter, endCenter, 0.5f);
            Vector3 arrowSize = new Vector3(1f, 1f, 1f);
            
            // Display movement direction
            Handles.Label(arrowPos + Vector3.up * 2, "Movement: " + (movementDirection > 0 ? "Forward →" : "Backward ←"));
        }
    }
}