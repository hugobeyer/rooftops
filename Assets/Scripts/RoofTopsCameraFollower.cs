using UnityEngine;
using RoofTops;

namespace RoofTops
{
    /// <summary>
    /// A simplified script that makes an object follow the RoofTops game camera.
    /// Specifically designed to work with NoiseMovement camera.
    /// </summary>
    public class RoofTopsCameraFollower : MonoBehaviour
    {
        [Header("Position Settings")]
        [Tooltip("Offset from the camera position")]
        public Vector3 offset = new Vector3(0, 0, 2f);
        
        [Tooltip("Whether to follow camera position changes")]
        public bool followPosition = true;
        
        [Tooltip("Whether to follow camera rotation changes")]
        public bool followRotation = false;
        
        [Header("Follow Options")]
        [Tooltip("Follow only on these axes")]
        public bool followX = true;
        public bool followY = true;
        public bool followZ = true;
        
        [Tooltip("Smooth movement (0 = no smoothing, higher = smoother)")]
        [Range(0, 20)]
        public float smoothing = 5f;
        
        // Reference to the camera
        private Transform cameraTransform;
        private NoiseMovement noiseMovement;
        
        // For smoothing
        private Vector3 velocity = Vector3.zero;
        
        private void Start()
        {
            FindCamera();
        }
        
        private void LateUpdate()
        {
            if (cameraTransform == null)
            {
                FindCamera();
                if (cameraTransform == null) return;
            }
            
            if (followPosition)
            {
                // Calculate target position
                Vector3 targetPosition = cameraTransform.position + offset;
                
                // Apply axis constraints
                if (!followX) targetPosition.x = transform.position.x;
                if (!followY) targetPosition.y = transform.position.y;
                if (!followZ) targetPosition.z = transform.position.z;
                
                // Apply position with smoothing
                if (smoothing > 0)
                {
                    transform.position = Vector3.SmoothDamp(
                        transform.position,
                        targetPosition,
                        ref velocity,
                        1f / smoothing
                    );
                }
                else
                {
                    transform.position = targetPosition;
                }
            }
            
            if (followRotation)
            {
                transform.rotation = cameraTransform.rotation;
            }
        }
        
        /// <summary>
        /// Find the game's camera
        /// </summary>
        private void FindCamera()
        {
            // First try to get the NoiseMovement camera
            noiseMovement = NoiseMovement.Instance;
            if (noiseMovement == null)
            {
                noiseMovement = FindObjectOfType<NoiseMovement>();
            }
            
            if (noiseMovement != null)
            {
                cameraTransform = noiseMovement.transform;
                Debug.Log($"RoofTopsCameraFollower: Found camera on {cameraTransform.name}");
                return;
            }
            
            // Fallback to main camera
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
                Debug.Log($"RoofTopsCameraFollower: Using main camera as fallback");
                return;
            }
            
            Debug.LogWarning("RoofTopsCameraFollower: No camera found!");
        }
        
        /// <summary>
        /// Reset the follower to the current camera position plus offset
        /// </summary>
        [ContextMenu("Reset Position")]
        public void ResetPosition()
        {
            if (cameraTransform != null)
            {
                transform.position = cameraTransform.position + offset;
                velocity = Vector3.zero;
            }
        }
        
        /// <summary>
        /// Update the offset based on current position relative to camera
        /// </summary>
        [ContextMenu("Set Offset From Current Position")]
        public void SetOffsetFromCurrentPosition()
        {
            if (cameraTransform != null)
            {
                offset = transform.position - cameraTransform.position;
                Debug.Log($"RoofTopsCameraFollower: Updated offset to {offset}");
            }
        }
    }
}