using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Optional DOTween reference - only used if DOTWEEN_EXISTS is defined
#if DOTWEEN_EXISTS
using DG.Tweening;
#endif

namespace RoofTops
{
    public class FollowCollisionUpDown : MonoBehaviour
    {
        [Header("Raycast Settings")]
        [Tooltip("Layer mask for collision detection")]
        public LayerMask collisionLayer;
        
        [Tooltip("Starting height for raycasting")]
        public float rayCastStartY = 50f;
        
        [Tooltip("Lowest height for raycasting")]
        public float rayCastEndY = -100f;
        
        [Tooltip("How far to look ahead for calculating height")]
        public float lookAheadDistance = 10f;
        
        [Tooltip("Direction to look for calculating height")]
        public Vector3 lookDirection = Vector3.forward;
        
        [Header("Follow Settings")]
        [Tooltip("Offset from detected ground height")]
        public Vector3 positionOffset = new Vector3(0, 1f, 0);
        
        [Tooltip("Should this object move with your XZ position")]
        public bool maintainXZPosition = true;
        
        [Header("Smoothness Settings")]
        [Tooltip("Transition type for height changes")]
        public TransitionType transitionType = TransitionType.SmoothDamp;
        
        [Tooltip("How smooth the height transitions are (higher = smoother)")]
        [Range(0.01f, 10f)]
        public float smoothness = 2f;
        
        [Tooltip("Maximum movement speed when using SmoothDamp")]
        public float maxSpeed = 10f;
        
        [Tooltip("Animation curve for custom easing (optional)")]
        public AnimationCurve customEasingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Debug")]
        [Tooltip("Draw debug rays in scene view")]
        public bool drawDebugRays = true;
        
        [Tooltip("Debug ray color")]
        public Color debugRayColor = Color.yellow;
        
        [Tooltip("Last detected valid height")]
        [SerializeField] private float lastValidHeight;
        
        [Tooltip("Current target height")]
        [SerializeField] private float targetHeight;
        
        [Tooltip("Is a valid height currently detected")]
        [SerializeField] private bool heightDetected = false;
        
        // Enum for transition type selection
        public enum TransitionType
        {
            Lerp,
            SmoothDamp,
            Spring,
            EaseInOut,
            CustomCurve,
            #if DOTWEEN_EXISTS
            DOTween
            #endif
        }
        
        // Variables for smooth transitions
        private float currentVelocity; // Used for SmoothDamp
        private Vector3 springVelocity; // Used for spring physics
        private float transitionProgress = 1f; // Used for easing transitions
        private float transitionStartHeight;
        private float transitionDuration = 1f;
        private float initialY;
        
        // Only define the DOTween variables if DOTween exists
        #if DOTWEEN_EXISTS
        private Tween heightTween;
        #endif
        
        void Start()
        {
            // Store initial height
            initialY = transform.position.y;
            lastValidHeight = initialY;
            targetHeight = initialY;
            
            // Normalize look direction
            if (lookDirection != Vector3.zero)
            {
                lookDirection.Normalize();
            }
            else
            {
                lookDirection = Vector3.forward;
            }
            
            // Ensure we have a valid collision layer
            if (collisionLayer.value == 0)
            {
                Debug.LogWarning("FollowCollisionUpDown: No collision layer set. Using default layer.");
                collisionLayer = 1; // Default layer
            }
        }
        
        void Update()
        {
            // Find the desired height
            DetectHeight();
            
            // Apply the height with smoothing
            ApplySmoothHeight();
        }
        
        private void DetectHeight()
        {
            // Calculate look-ahead position (only XZ, maintain current Y)
            Vector3 currentPos = transform.position;
            Vector3 lookAheadPos = currentPos + new Vector3(
                lookDirection.x * lookAheadDistance,
                0,
                lookDirection.z * lookAheadDistance
            );
            
            // Perform raycast from the look-ahead position
            heightDetected = false;
            RaycastHit hit;
            
            Vector3 rayStart = new Vector3(lookAheadPos.x, rayCastStartY, lookAheadPos.z);
            Vector3 rayEnd = new Vector3(lookAheadPos.x, rayCastEndY, lookAheadPos.z);
            Vector3 rayDirection = (rayEnd - rayStart).normalized;
            float rayDistance = Mathf.Abs(rayCastStartY - rayCastEndY);
            
            // Cast the ray
            if (Physics.Raycast(rayStart, rayDirection, out hit, rayDistance, collisionLayer))
            {
                // Update the target height using the hit point
                targetHeight = hit.point.y + positionOffset.y;
                lastValidHeight = targetHeight;
                heightDetected = true;
                
                if (transitionType == TransitionType.EaseInOut || 
                    transitionType == TransitionType.CustomCurve
                    #if DOTWEEN_EXISTS
                    || transitionType == TransitionType.DOTween
                    #endif
                   )
                {
                    // Start a new transition if we have a significant height change
                    if (Mathf.Abs(targetHeight - transform.position.y) > 0.1f && transitionProgress >= 1f)
                    {
                        StartNewTransition();
                    }
                }
            }
            else
            {
                // If no hit, use the last valid height
                targetHeight = lastValidHeight;
            }
            
            // Draw debug ray
            if (drawDebugRays)
            {
                Debug.DrawLine(rayStart, heightDetected ? hit.point : rayEnd, debugRayColor, Time.deltaTime);
            }
        }
        
        private void StartNewTransition()
        {
            transitionStartHeight = transform.position.y;
            transitionProgress = 0f;
            transitionDuration = Mathf.Abs(targetHeight - transitionStartHeight) / maxSpeed;
            transitionDuration = Mathf.Clamp(transitionDuration, 0.1f, smoothness);
            
            // Wrap all DOTween-specific code in preprocessor directives
            #if DOTWEEN_EXISTS
            if (transitionType == TransitionType.DOTween)
            {
                if (heightTween != null)
                {
                    heightTween.Kill();
                }
                
                Vector3 targetPos = transform.position;
                targetPos.y = targetHeight;
                
                heightTween = transform.DOMoveY(targetHeight, transitionDuration)
                    .SetEase(Ease.InOutCubic);
            }
            #endif
        }
        
        private void ApplySmoothHeight()
        {
            Vector3 newPosition = transform.position;
            
            // If maintainXZPosition is true, keep following the object's XZ position
            if (maintainXZPosition)
            {
                newPosition.x = transform.position.x + positionOffset.x;
                newPosition.z = transform.position.z + positionOffset.z;
            }
            else
            {
                // Otherwise, apply full position offset
                newPosition.x += positionOffset.x;
                newPosition.z += positionOffset.z;
            }
            
            // Calculate smooth Y position based on transition type
            switch (transitionType)
            {
                case TransitionType.Lerp:
                    newPosition.y = Mathf.Lerp(newPosition.y, targetHeight, Time.deltaTime * smoothness);
                    break;
                    
                case TransitionType.SmoothDamp:
                    newPosition.y = Mathf.SmoothDamp(newPosition.y, targetHeight, ref currentVelocity, 1f / smoothness, maxSpeed);
                    break;
                    
                case TransitionType.Spring:
                    float springForce = (targetHeight - newPosition.y) * smoothness;
                    springVelocity.y += springForce * Time.deltaTime;
                    springVelocity.y *= 0.9f; // Damping
                    newPosition.y += springVelocity.y * Time.deltaTime;
                    break;
                    
                case TransitionType.EaseInOut:
                    // Standard easing
                    if (transitionProgress < 1f)
                    {
                        transitionProgress += Time.deltaTime / transitionDuration;
                        float t = Mathf.SmoothStep(0, 1, transitionProgress);
                        newPosition.y = Mathf.Lerp(transitionStartHeight, targetHeight, t);
                    }
                    break;
                    
                case TransitionType.CustomCurve:
                    // Custom curve easing
                    if (transitionProgress < 1f)
                    {
                        transitionProgress += Time.deltaTime / transitionDuration;
                        float t = customEasingCurve.Evaluate(Mathf.Clamp01(transitionProgress));
                        newPosition.y = Mathf.Lerp(transitionStartHeight, targetHeight, t);
                    }
                    break;
                    
                #if DOTWEEN_EXISTS
                case TransitionType.DOTween:
                    // DOTween handles this separately
                    return;
                #endif
            }
            
            // Apply the new position
            transform.position = newPosition;
        }
        
        // Get the current target height
        public float GetTargetHeight()
        {
            return targetHeight;
        }
        
        // Force an immediate update of height
        public void UpdateHeightImmediate()
        {
            DetectHeight();
            
            Vector3 newPosition = transform.position;
            newPosition.y = targetHeight;
            transform.position = newPosition;
            
            // Reset transition variables
            currentVelocity = 0f;
            springVelocity = Vector3.zero;
            transitionProgress = 1f;
            
            // Wrap DOTween-specific code in preprocessor directives
            #if DOTWEEN_EXISTS
            if (heightTween != null)
            {
                heightTween.Kill();
            }
            #endif
        }
        
        #if UNITY_EDITOR
        // Draw debug gizmos to visualize the height detection
        private void OnDrawGizmosSelected()
        {
            if (!drawDebugRays) return;
            
            Vector3 currentPos = transform.position;
            Vector3 lookAheadPos = currentPos + new Vector3(
                lookDirection.x * lookAheadDistance,
                0,
                lookDirection.z * lookAheadDistance
            );
            
            // Draw look ahead line
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(currentPos, lookAheadPos);
            
            // Draw vertical ray
            Gizmos.color = debugRayColor;
            Vector3 rayStart = new Vector3(lookAheadPos.x, rayCastStartY, lookAheadPos.z);
            Vector3 rayEnd = new Vector3(lookAheadPos.x, rayCastEndY, lookAheadPos.z);
            Gizmos.DrawLine(rayStart, rayEnd);
            
            // Draw sphere at the current target height
            if (Application.isPlaying && heightDetected)
            {
                Gizmos.color = Color.green;
                Vector3 targetPos = new Vector3(lookAheadPos.x, targetHeight, lookAheadPos.z);
                Gizmos.DrawSphere(targetPos, 0.5f);
                
                // Draw line from target to object
                Gizmos.color = Color.white;
                Gizmos.DrawLine(targetPos, transform.position);
            }
        }
        
        // Custom editor to add DOTween check
        [CustomEditor(typeof(FollowCollisionUpDown))]
        public class FollowCollisionUpDownEditor : Editor
        {
            private bool dotweenExists = false;
            
            private void OnEnable()
            {
                // Check if DOTween exists
                System.Type dotweenType = System.Type.GetType("DG.Tweening.DOTween, DOTween");
                dotweenExists = dotweenType != null;
                
                if (dotweenExists)
                {
                    // Add DOTWEEN_EXISTS symbol to the build
                    string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(
                        EditorUserBuildSettings.selectedBuildTargetGroup);
                    
                    if (!defines.Contains("DOTWEEN_EXISTS"))
                    {
                        PlayerSettings.SetScriptingDefineSymbolsForGroup(
                            EditorUserBuildSettings.selectedBuildTargetGroup,
                            (defines + ";DOTWEEN_EXISTS").TrimStart(';'));
                    }
                }
            }
            
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();
                
                if (!dotweenExists)
                {
                    EditorGUILayout.HelpBox(
                        "DOTween not detected. Install DOTween to enable DOTween transitions.", 
                        MessageType.Info);
                }
                
                FollowCollisionUpDown script = (FollowCollisionUpDown)target;
                
                EditorGUILayout.Space();
                if (GUILayout.Button("Update Height Immediately"))
                {
                    if (Application.isPlaying)
                    {
                        script.UpdateHeightImmediate();
                    }
                }
            }
        }
        #endif
    }
} 