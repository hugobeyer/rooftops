using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RoofTops
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class DynamicRibbonCarpet : MonoBehaviour
    {
        [Header("Ribbon Properties")]
        [Tooltip("The width of the ribbon")]
        public float width = 2.0f;
        
        [Tooltip("How dense the ribbon segments are")]
        public float segmentDensity = 2.0f;
        
        [Tooltip("Height offset above the ground")]
        public float heightOffset = 1.5f;
        
        [Tooltip("Forward offset from start position")]
        public float forwardOffset = 3.0f;
        
        [Tooltip("How far ahead to extend the ribbon")]
        public float lookAheadDistance = 30.0f;
        
        [Header("Ground Detection")]
        [Tooltip("Layer mask for ground detection")]
        public LayerMask groundLayer = -1; // Default to everything
        
        [Tooltip("Distance to raycast for ground detection")]
        public float raycastDistance = 50f;
        
        [Tooltip("Width of ground detection (multiple raycasts)")]
        public float detectionWidth = 2.0f;
        
        [Tooltip("Number of raycasts across detection width")]
        public int raycastCount = 3;
        
        [Header("Movement")]
        [Tooltip("Should the ribbon automatically move forward")]
        public bool autoMove = true;
        
        [Tooltip("Automatic movement speed")]
        public float moveSpeed = 5f;
        
        [Tooltip("Apply gentle floating animation")]
        public bool enableFloating = true;
        
        [Tooltip("Floating animation amplitude")]
        public float floatAmplitude = 0.2f;
        
        [Tooltip("Floating animation frequency")]
        public float floatFrequency = 1.0f;
        
        [Header("Material Settings")]
        [Tooltip("Material for the ribbon")]
        public Material ribbonMaterial;
        
        [Tooltip("UV tiling - how many times to repeat the texture along the ribbon")]
        public Vector2 uvTiling = new Vector2(1, 10);
        
        [Tooltip("Should UVs scroll with movement")]
        public bool scrollUVs = true;
        
        [Tooltip("UV scroll speed")]
        public float uvScrollSpeed = 0.5f;
        
        [Header("Editor Preview")]
        [Tooltip("Enable ribbon preview in editor")]
        public bool showEditorPreview = true;
        
        [Tooltip("Preview path length when in editor")]
        public float editorPreviewLength = 30f;
        
        [Tooltip("Preview path height when in editor")]
        public float editorPreviewHeight = 2f;
        
        [Tooltip("How many segments to use in editor preview")]
        public int editorPreviewSegments = 20;
        
        // Internal variables
        private Mesh mesh;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private List<Vector3> vertices = new List<Vector3>();
        private List<int> triangles = new List<int>();
        private List<Vector2> uvs = new List<Vector2>();
        private List<Vector3> centerPoints = new List<Vector3>();
        
        private float currentUVOffset = 0f;
        private Vector3 startPosition;
        private Vector3 currentPosition;
        
        // Flag to track if we're in editor preview mode
        private bool isEditorPreview = false;
        
        void Start()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            
            if (ribbonMaterial != null)
            {
                meshRenderer.material = ribbonMaterial;
            }
            
            // Initialize the mesh
            mesh = new Mesh();
            mesh.name = "DynamicRibbonCarpet";
            meshFilter.mesh = mesh;
            
            // Store starting position
            startPosition = transform.position;
            currentPosition = startPosition;
        }
        
        void Update()
        {
            // Only run at runtime, not in editor preview
            if (!isEditorPreview)
            {
                // Update position if auto-move is enabled
                if (autoMove)
                {
                    currentPosition += Vector3.forward * moveSpeed * Time.deltaTime;
                }
                else
                {
                    // Otherwise use the current transform position
                    currentPosition = transform.position;
                }
                
                // Update UV scrolling
                if (scrollUVs && meshRenderer.material != null)
                {
                    currentUVOffset += Time.deltaTime * uvScrollSpeed;
                    meshRenderer.material.SetTextureOffset("_MainTex", new Vector2(0, currentUVOffset));
                }
                
                // Generate the ribbon mesh
                GenerateRibbon();
            }
        }
        
        private void GenerateRibbon()
        {
            // Clear lists
            vertices.Clear();
            triangles.Clear();
            uvs.Clear();
            centerPoints.Clear();
            
            // Calculate path points
            CalculatePathOverGround();
            
            // Create ribbon mesh from path points
            CreateRibbonMesh();
            
            // Apply to mesh
            mesh.Clear();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }
        
        private void CalculatePathOverGround()
        {
            // Start position (using current position with forward offset)
            Vector3 startPos = currentPosition - transform.forward * forwardOffset;
            startPos.x = 0; // Center on X axis
            
            // Add starting point
            centerPoints.Add(startPos);
            
            // Calculate number of segments based on distance and density
            float totalDistance = lookAheadDistance;
            int segments = Mathf.Max(10, Mathf.CeilToInt(totalDistance * segmentDensity));
            
            // Add intermediate points
            for (int i = 1; i <= segments; i++)
            {
                float t = (float)i / segments;
                float z = startPos.z - t * lookAheadDistance; // Moving "into" the screen (negative Z)
                
                // Find height at this position by sampling ground
                float heightAtPoint = SampleGroundHeight(new Vector3(0, startPos.y + 10f, z));
                
                // Add height offset
                heightAtPoint += heightOffset;
                
                // Apply floating animation
                if (enableFloating)
                {
                    heightAtPoint += floatAmplitude * Mathf.Sin(Time.time * floatFrequency + z * 0.1f);
                }
                
                Vector3 point = new Vector3(0, heightAtPoint, z);
                centerPoints.Add(point);
            }
        }
        
        private float SampleGroundHeight(Vector3 position)
        {
            // Default height if no ground is found
            float groundHeight = position.y - 5f;
            
            // Cast multiple rays across the width to find the average ground height
            for (int i = 0; i < raycastCount; i++)
            {
                // Calculate X offset for this ray
                float xOffset = 0;
                
                if (raycastCount > 1)
                {
                    // Distribute rays evenly across detection width
                    xOffset = detectionWidth * ((float)i / (raycastCount - 1) - 0.5f);
                }
                
                // Position with offset
                Vector3 rayPosition = position + new Vector3(xOffset, 0, 0);
                
                // Cast ray down
                RaycastHit hit;
                if (Physics.Raycast(rayPosition, Vector3.down, out hit, raycastDistance, groundLayer))
                {
                    // Use the highest point we find to avoid going through ground
                    groundHeight = Mathf.Max(groundHeight, hit.point.y);
                }
            }
            
            return groundHeight;
        }
        
        private void CreateRibbonMesh()
        {
            if (centerPoints.Count < 2) return;
            
            // Create ribbon mesh based on center points
            for (int i = 0; i < centerPoints.Count; i++)
            {
                Vector3 centerPoint = centerPoints[i];
                
                // Get forward direction
                Vector3 forward;
                if (i < centerPoints.Count - 1)
                {
                    forward = (centerPoints[i + 1] - centerPoint).normalized;
                }
                else if (i > 0)
                {
                    forward = (centerPoint - centerPoints[i - 1]).normalized;
                }
                else
                {
                    forward = Vector3.forward;
                }
                
                // Calculate right vector (perpendicular to forward and up)
                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
                
                // Calculate left and right points
                Vector3 leftPoint = centerPoint + right * (-width / 2f);
                Vector3 rightPoint = centerPoint + right * (width / 2f);
                
                // Add vertices
                vertices.Add(leftPoint);
                vertices.Add(rightPoint);
                
                // Add UVs
                float uvY = (float)i / (centerPoints.Count - 1) * uvTiling.y;
                uvs.Add(new Vector2(0, uvY));
                uvs.Add(new Vector2(uvTiling.x, uvY));
                
                // Add triangles - skip the first segment
                if (i > 0)
                {
                    int baseIndex = (i - 1) * 2;
                    
                    // First triangle
                    triangles.Add(baseIndex);
                    triangles.Add(baseIndex + 2);
                    triangles.Add(baseIndex + 1);
                    
                    // Second triangle
                    triangles.Add(baseIndex + 1);
                    triangles.Add(baseIndex + 2);
                    triangles.Add(baseIndex + 3);
                }
            }
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            // Regenerate the mesh when values change in the editor
            if (showEditorPreview && !Application.isPlaying)
            {
                GenerateEditorPreview();
            }
        }
        
        private void GenerateEditorPreview()
        {
            // Skip if the required components aren't initialized yet
            if (meshFilter == null)
            {
                meshFilter = GetComponent<MeshFilter>();
                if (meshFilter == null) return;
            }
            
            if (meshRenderer == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
                if (meshRenderer == null) return;
            }
            
            // Set a default material for preview if none is assigned
            if (meshRenderer.sharedMaterial == null && ribbonMaterial != null)
            {
                meshRenderer.sharedMaterial = ribbonMaterial;
            }
            else if (meshRenderer.sharedMaterial == null)
            {
                // Create a default preview material
                var defaultMat = new Material(Shader.Find("Standard"));
                defaultMat.color = new Color(0.5f, 0.8f, 1f, 0.8f);
                meshRenderer.sharedMaterial = defaultMat;
            }
            
            // Mark that we're generating preview
            isEditorPreview = true;
            
            // Create a preview mesh
            if (mesh == null)
            {
                mesh = new Mesh();
                mesh.name = "DynamicRibbonCarpet_Preview";
            }
            else
            {
                mesh.Clear();
            }
            
            // Clear lists
            vertices.Clear();
            triangles.Clear();
            uvs.Clear();
            centerPoints.Clear();
            
            // Try to sample ground heights for preview if possible
            bool usedRealGround = false;
            
            // Only attempt ground sampling if we have a non-zero ground layer set
            if (groundLayer != 0)
            {
                // Create a simple preview path
                Vector3 startPos = transform.position;
                centerPoints.Add(startPos);
                
                // Create path over actual ground
                for (int i = 1; i <= editorPreviewSegments; i++)
                {
                    float t = (float)i / editorPreviewSegments;
                    float z = startPos.z - t * editorPreviewLength;
                    
                    // Try to sample real ground height
                    RaycastHit hit;
                    Vector3 rayOrigin = new Vector3(0, startPos.y + 100f, z);
                    
                    if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 500f, groundLayer))
                    {
                        // Use actual ground height with offset
                        float y = hit.point.y + heightOffset;
                        Vector3 point = new Vector3(0, y, z);
                        centerPoints.Add(point);
                        usedRealGround = true;
                    }
                    else
                    {
                        // Failed to hit ground, use default sine wave pattern
                        usedRealGround = false;
                        centerPoints.Clear();
                        break;
                    }
                }
            }
            
            // If ground detection failed, use sine wave as fallback
            if (!usedRealGround)
            {
                GeneratePreviewPath();
            }
            
            // Create ribbon mesh from preview path
            CreateRibbonMesh();
            
            // Apply to mesh
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            // Assign to mesh filter
            if (meshFilter.sharedMesh != mesh)
            {
                meshFilter.sharedMesh = mesh;
            }
            
            // Reset flag
            isEditorPreview = false;
            
            // Mark the scene as dirty
            EditorUtility.SetDirty(this);
        }
        
        private void GeneratePreviewPath()
        {
            // Start at the current position
            Vector3 startPos = transform.position;
            centerPoints.Add(startPos);
            
            // Create a gently curved path ahead
            for (int i = 1; i <= editorPreviewSegments; i++)
            {
                float t = (float)i / editorPreviewSegments;
                float z = startPos.z - t * editorPreviewLength; // Moving "into" the screen (negative Z)
                
                // Create gentle wave pattern
                float y = startPos.y + editorPreviewHeight * Mathf.Sin(t * Mathf.PI * 2f);
                
                // Add a point to the path
                Vector3 point = new Vector3(0, y, z);
                centerPoints.Add(point);
            }
        }
        
        // Draw gizmos to show raycast paths
        private void OnDrawGizmosSelected()
        {
            if (!showEditorPreview) return;
            
            Gizmos.color = Color.yellow;
            
            // Visualize ground detection rays
            Vector3 startPos = transform.position;
            float rayStart = startPos.y + 10f;
            
            for (int seg = 0; seg <= editorPreviewSegments; seg += 5) // Draw every 5th point
            {
                float t = (float)seg / editorPreviewSegments;
                float z = startPos.z - t * editorPreviewLength;
                
                for (int i = 0; i < raycastCount; i++)
                {
                    // Calculate X offset for this ray
                    float xOffset = 0;
                    if (raycastCount > 1)
                    {
                        xOffset = detectionWidth * ((float)i / (raycastCount - 1) - 0.5f);
                    }
                    
                    // Position with offset
                    Vector3 rayPosition = new Vector3(xOffset, rayStart, z);
                    
                    // Draw ray
                    Gizmos.DrawLine(rayPosition, rayPosition + Vector3.down * raycastDistance);
                }
            }
        }
        
        [CustomEditor(typeof(DynamicRibbonCarpet))]
        public class DynamicRibbonCarpetEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                DynamicRibbonCarpet carpet = (DynamicRibbonCarpet)target;
                
                // Draw the default inspector
                DrawDefaultInspector();
                
                // Add a button to manually regenerate the preview
                if (GUILayout.Button("Update Preview") && !Application.isPlaying)
                {
                    carpet.GenerateEditorPreview();
                    SceneView.RepaintAll();
                }
            }
        }
#endif
    }
} 