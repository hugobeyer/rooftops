using UnityEngine;
using RoofTops;

/// <summary>
/// Makes an object follow the active camera with customizable offset and smoothing.
/// </summary>
public class CameraFollower : MonoBehaviour
{
    [Header("Follow Settings")]
    [Tooltip("Offset from the camera position")]
    public Vector3 offset = Vector3.zero;
    
    [Tooltip("Whether to maintain the same relative position to the camera")]
    public bool maintainRelativePosition = true;
    
    [Tooltip("Whether to match the camera's rotation")]
    public bool matchRotation = false;
    
    [Header("Smoothing")]
    [Tooltip("Enable smooth movement")]
    public bool smoothMovement = true;
    
    [Tooltip("Movement smoothing factor (higher = faster)")]
    [Range(0.1f, 20f)]
    public float movementSmoothness = 5f;
    
    [Tooltip("Rotation smoothing factor (higher = faster)")]
    [Range(0.1f, 20f)]
    public float rotationSmoothness = 5f;
    
    [Header("Advanced")]
    [Tooltip("Update in FixedUpdate instead of LateUpdate")]
    public bool useFixedUpdate = false;
    
    [Tooltip("Only follow position on these axes")]
    public bool followX = true;
    public bool followY = true;
    public bool followZ = true;
    
    // Reference to the camera to follow
    private Camera targetCamera;
    private Transform cameraTransform;
    
    // Initial relative position if maintaining relative position
    private Vector3 initialRelativePosition;
    
    // For smoothing
    private Vector3 currentVelocity = Vector3.zero;
    
    private void Start()
    {
        // Try to find the camera
        FindCamera();
        
        if (cameraTransform != null && maintainRelativePosition)
        {
            // Store the initial relative position
            initialRelativePosition = transform.position - cameraTransform.position;
        }
    }
    
    private void OnEnable()
    {
        // Try to find the camera if not already set
        if (cameraTransform == null)
        {
            FindCamera();
        }
    }
    
    private void LateUpdate()
    {
        if (!useFixedUpdate)
        {
            UpdatePosition();
        }
    }
    
    private void FixedUpdate()
    {
        if (useFixedUpdate)
        {
            UpdatePosition();
        }
    }
    
    private void UpdatePosition()
    {
        // Make sure we have a camera to follow
        if (cameraTransform == null)
        {
            FindCamera();
            if (cameraTransform == null) return;
        }
        
        // Calculate target position
        Vector3 targetPosition;
        
        if (maintainRelativePosition)
        {
            targetPosition = cameraTransform.position + initialRelativePosition;
        }
        else
        {
            targetPosition = cameraTransform.position + offset;
        }
        
        // Apply axis constraints
        Vector3 currentPos = transform.position;
        if (!followX) targetPosition.x = currentPos.x;
        if (!followY) targetPosition.y = currentPos.y;
        if (!followZ) targetPosition.z = currentPos.z;
        
        // Apply position
        if (smoothMovement)
        {
            transform.position = Vector3.SmoothDamp(
                transform.position, 
                targetPosition, 
                ref currentVelocity, 
                1f / movementSmoothness
            );
        }
        else
        {
            transform.position = targetPosition;
        }
        
        // Apply rotation if needed
        if (matchRotation)
        {
            if (smoothMovement)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    cameraTransform.rotation,
                    Time.deltaTime * rotationSmoothness
                );
            }
            else
            {
                transform.rotation = cameraTransform.rotation;
            }
        }
    }
    
    /// <summary>
    /// Finds the active camera in the scene
    /// </summary>
    private void FindCamera()
    {
        // First try to get the NoiseMovement camera (main camera in RoofTops)
        NoiseMovement noiseMovement = FindFirstObjectByType<NoiseMovement>();
        if (noiseMovement != null)
        {
            targetCamera = noiseMovement.GetComponent<Camera>();
            if (targetCamera != null)
            {
                cameraTransform = targetCamera.transform;
                Debug.Log($"CameraFollower: Found NoiseMovement camera on {cameraTransform.name}");
                return;
            }
        }
        
        // Fallback to the main camera
        targetCamera = Camera.main;
        if (targetCamera != null)
        {
            cameraTransform = targetCamera.transform;
            Debug.Log($"CameraFollower: Found main camera on {cameraTransform.name}");
            return;
        }
        
        // Last resort - find any camera
        targetCamera = FindFirstObjectByType<Camera>();
        if (targetCamera != null)
        {
            cameraTransform = targetCamera.transform;
            Debug.Log($"CameraFollower: Found camera on {cameraTransform.name}");
            return;
        }
        
        Debug.LogWarning("CameraFollower: No camera found in the scene!");
    }
    
    /// <summary>
    /// Manually set the camera to follow
    /// </summary>
    public void SetTargetCamera(Camera camera)
    {
        if (camera != null)
        {
            targetCamera = camera;
            cameraTransform = camera.transform;
            
            if (maintainRelativePosition)
            {
                initialRelativePosition = transform.position - cameraTransform.position;
            }
            
            Debug.Log($"CameraFollower: Manually set target camera to {cameraTransform.name}");
        }
    }
    
    /// <summary>
    /// Reset the relative position
    /// </summary>
    public void ResetRelativePosition()
    {
        if (cameraTransform != null)
        {
            initialRelativePosition = transform.position - cameraTransform.position;
            Debug.Log("CameraFollower: Reset relative position");
        }
    }
} 