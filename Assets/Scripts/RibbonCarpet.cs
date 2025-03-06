using UnityEngine;
using System.Collections.Generic;

namespace RoofTops
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class RibbonCarpet : MonoBehaviour
    {
        [Header("Ribbon Properties")]
        [Tooltip("The width of the ribbon")]
        public float width = 2.0f;
        
        [Tooltip("The length of each ribbon segment")]
        public float segmentLength = 0.5f;
        
        [Tooltip("Total length of the ribbon")]
        public float totalLength = 20.0f;
        
        [Tooltip("Height offset above the surface")]
        public float heightOffset = 0.05f;
        
        [Tooltip("How many segments to generate (calculated from totalLength)")]
        private int segments;
        
        [Header("Path Settings")]
        [Tooltip("The path to follow (if null, will use forward direction)")]
        public Transform[] pathPoints;
        
        [Tooltip("Use bezier curves between path points")]
        public bool useBezierPath = false;
        
        [Tooltip("How many points to sample along each bezier curve")]
        public int bezierSamples = 10;
        
        [Header("Material Settings")]
        [Tooltip("Material for the ribbon")]
        public Material ribbonMaterial;
        
        [Tooltip("UV tiling - how many times to repeat the texture along the ribbon")]
        public Vector2 uvTiling = new Vector2(1, 10);
        
        [Header("Raycast Settings")]
        [Tooltip("Maximum distance to check for ground")]
        public float maxRaycastDistance = 10f;
        
        [Tooltip("Layer mask for raycasting")]
        public LayerMask groundLayerMask = -1; // Default to everything
        
        // Internal variables
        private Mesh mesh;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private List<Vector3> vertices = new List<Vector3>();
        private List<int> triangles = new List<int>();
        private List<Vector2> uvs = new List<Vector2>();
        private List<Vector3> centerPoints = new List<Vector3>();
        
        void Start()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            
            if (ribbonMaterial != null)
            {
                meshRenderer.material = ribbonMaterial;
            }
            
            GenerateRibbon();
        }
        
        [ContextMenu("Regenerate Ribbon")]
        public void GenerateRibbon()
        {
            // Calculate segment count
            segments = Mathf.Max(2, Mathf.CeilToInt(totalLength / segmentLength));
            
            // Create a new mesh
            mesh = new Mesh();
            mesh.name = "RibbonCarpet";
            
            // Clear lists
            vertices.Clear();
            triangles.Clear();
            uvs.Clear();
            centerPoints.Clear();
            
            // Generate path points
            GeneratePathPoints();
            
            // Generate the mesh
            CreateRibbonMesh();
            
            // Apply to mesh filter
            meshFilter.mesh = mesh;
        }
        
        private void GeneratePathPoints()
        {
            if (pathPoints != null && pathPoints.Length >= 2)
            {
                if (useBezierPath)
                {
                    // Generate bezier curve points
                    for (int i = 0; i < pathPoints.Length - 1; i++)
                    {
                        Vector3 p0 = pathPoints[i].position;
                        Vector3 p3 = pathPoints[i + 1].position;
                        
                        // Calculate control points - simple method
                        Vector3 p1 = p0 + pathPoints[i].forward * Vector3.Distance(p0, p3) * 0.33f;
                        Vector3 p2 = p3 - pathPoints[i + 1].forward * Vector3.Distance(p0, p3) * 0.33f;
                        
                        // Sample bezier curve
                        for (int j = 0; j <= bezierSamples; j++)
                        {
                            float t = j / (float)bezierSamples;
                            centerPoints.Add(BezierPoint(p0, p1, p2, p3, t));
                        }
                    }
                }
                else
                {
                    // Simple linear path
                    foreach (Transform point in pathPoints)
                    {
                        centerPoints.Add(point.position);
                    }
                }
            }
            else
            {
                // Generate a straight path in forward direction
                for (int i = 0; i <= segments; i++)
                {
                    float t = i / (float)segments;
                    centerPoints.Add(transform.position + transform.forward * totalLength * t);
                }
            }
        }
        
        private void CreateRibbonMesh()
        {
            if (centerPoints.Count < 2)
            {
                Debug.LogError("Not enough path points to create ribbon!");
                return;
            }
            
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
                else
                {
                    forward = (centerPoint - centerPoints[i - 1]).normalized;
                }
                
                // Calculate right vector (perpendicular to forward and up)
                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
                
                // Raycast to find ground position
                RaycastHit hit;
                float rayStartHeight = 5f; // Start the ray well above the geometry
                
                Vector3 leftPoint = centerPoint + right * (-width / 2f);
                Vector3 rightPoint = centerPoint + right * (width / 2f);
                
                // Raycast for left point
                if (Physics.Raycast(leftPoint + Vector3.up * rayStartHeight, Vector3.down, out hit, maxRaycastDistance, groundLayerMask))
                {
                    leftPoint = hit.point + Vector3.up * heightOffset;
                }
                
                // Raycast for right point
                if (Physics.Raycast(rightPoint + Vector3.up * rayStartHeight, Vector3.down, out hit, maxRaycastDistance, groundLayerMask))
                {
                    rightPoint = hit.point + Vector3.up * heightOffset;
                }
                
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
            
            // Assign to mesh
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            
            // Recalculate normals and bounds
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }
        
        private Vector3 BezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;
            
            Vector3 point = uuu * p0;
            point += 3f * uu * t * p1;
            point += 3f * u * tt * p2;
            point += ttt * p3;
            
            return point;
        }
        
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (centerPoints == null || centerPoints.Count == 0)
            {
                // Preview mode - recreate center points without generating the mesh
                GeneratePathPoints();
            }
            
            // Draw path
            Gizmos.color = Color.yellow;
            for (int i = 0; i < centerPoints.Count - 1; i++)
            {
                Gizmos.DrawLine(centerPoints[i], centerPoints[i + 1]);
            }
            
            // Draw ribbon width
            Gizmos.color = Color.green;
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
                
                // Calculate right vector
                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
                
                // Draw width line
                Vector3 leftPoint = centerPoint + right * (-width / 2f);
                Vector3 rightPoint = centerPoint + right * (width / 2f);
                Gizmos.DrawLine(leftPoint, rightPoint);
                
                // Draw point
                Gizmos.DrawSphere(centerPoint, 0.1f);
            }
        }
#endif
    }
} 