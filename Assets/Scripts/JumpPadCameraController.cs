using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Required for Dictionary

namespace RoofTops
{
    public class JumpPadCameraController : MonoBehaviour
    {
        [Header("References")]
        public Transform playerTransform;
        public Transform cameraTransform;
        private Camera mainCamera;
        private PlayerController playerController;

        [Header("Camera Settings")]
        public Vector3 cameraOffset = new Vector3(0, 2, -2); // Additive position offset
        public Vector3 lookAtOffset = new Vector3(0, 1, 5); // Where to look at relative to player
        public float fovOffset = 10f; // Additive FOV change (can be positive or negative)
        public bool useLookAtDuringJump = true; // Toggle for look-at functionality

        [Header("Transition Timing")]
        [Tooltip("Time to transition from the current camera state to the jump pad state.")]
        public float transitionToJumpPadTime = 0.5f;

        [Tooltip("Time to hold the jump pad camera state before transitioning back.")]
        public float holdJumpPadTime = 1.0f;

        [Tooltip("Time to transition from the jump pad state back to the original camera state.")]
        public float transitionBackTime = 0.5f;

        [Header("Object Hiding")]
        public GameObject[] objectsToHide; // Array of GameObjects to hide/show
        private Dictionary<GameObject, Vector3> originalObjectPositions = new Dictionary<GameObject, Vector3>();

        private bool isJumpPadTriggered = false;
        private bool isTransitioning = false;
        private Vector3 additiveOffset = Vector3.zero;
        private float additiveFOV = 0f;
        private float lookAtWeight = 0f; // Controls how much the look-at affects rotation

        private Quaternion originalRotation;
        private Vector3 lookAtTarget = Vector3.zero;

        private void Start()
        {
            // Find the main camera if not assigned
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera == null)
            {
                Debug.LogError("Main camera not found!");
            }

            // Find player controller if not assigned
            if (playerController == null && playerTransform != null)
            {
                playerController = playerTransform.GetComponent<PlayerController>();
            }

            // Store the original positions of objects to hide.  Important to do this in Start()
            if (objectsToHide != null)
            {
                foreach (GameObject obj in objectsToHide)
                {
                    if (obj != null)
                    {
                         originalObjectPositions[obj] = obj.transform.position;
                    }
                }
            }
        }

        private void LateUpdate()
        {
            if (mainCamera == null || cameraTransform == null) return;

            // Apply additive position offset directly
            cameraTransform.localPosition += additiveOffset;

            // Handle look-at functionality
            if (isJumpPadTriggered && useLookAtDuringJump && lookAtWeight > 0 && playerTransform != null)
            {
                lookAtTarget = playerTransform.position + lookAtOffset;
                Debug.DrawLine(cameraTransform.position, lookAtTarget, Color.red);

                if (lookAtWeight > 0)
                {
                    Vector3 lookDirection = (lookAtTarget - cameraTransform.position).normalized;
                    Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                    cameraTransform.rotation = Quaternion.Slerp(originalRotation, targetRotation, lookAtWeight);
                }
            }

            // Apply additive FOV
            if (mainCamera != null)
            {
                mainCamera.fieldOfView += additiveFOV;
            }
        }

        public void OnJumpPadTriggered(bool triggered)
        {
            if (triggered && !isTransitioning)
            {
                isJumpPadTriggered = true;

                if (cameraTransform != null)
                {
                    originalRotation = cameraTransform.rotation;
                }

                // Hide objects
                HideObjects();

                StartCoroutine(DoJumpPadTransition());
            }
        }

        private void HideObjects()
        {
            if (objectsToHide != null)
            {
                foreach (GameObject obj in objectsToHide)
                {
                    if (obj != null)
                    {
                        obj.transform.position = new Vector3(obj.transform.position.x, -10000f, obj.transform.position.z);
                    }
                }
            }
        }

        private void ShowObjects()
        {
            if (objectsToHide != null)
            {
                foreach (GameObject obj in objectsToHide)
                {
                    if (obj != null)
                    {
                        // Restore from the dictionary
                        if (originalObjectPositions.ContainsKey(obj))
                        {
                            obj.transform.position = originalObjectPositions[obj];
                        }
                        else
                        {
                            Debug.LogWarning("Object not found in original positions dictionary: " + obj.name);
                        }
                    }
                }
            }
        }

        private System.Collections.IEnumerator DoJumpPadTransition()
        {
            isTransitioning = true;
            float elapsedTime = 0f;

            // Phase 1: Transition to jump pad camera (add offset)
            while (elapsedTime < transitionToJumpPadTime)
            {
                float t = elapsedTime / transitionToJumpPadTime;
                t = t * t * (3f - 2f * t);

                additiveOffset = Vector3.Lerp(Vector3.zero, cameraOffset, t);
                additiveFOV = Mathf.Lerp(0f, fovOffset, t);

                if (useLookAtDuringJump)
                {
                    lookAtWeight = Mathf.Lerp(0f, 1f, t);
                }

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            additiveOffset = cameraOffset;
            additiveFOV = fovOffset;
            lookAtWeight = useLookAtDuringJump ? 1f : 0f;

            yield return new WaitForSeconds(holdJumpPadTime);

            // Phase 3: Transition back to original camera
            elapsedTime = 0f;

            while (elapsedTime < transitionBackTime)
            {
                float t = elapsedTime / transitionBackTime;
                t = t * t * (3f - 2f * t);

                additiveOffset = Vector3.Lerp(cameraOffset, Vector3.zero, t);
                additiveFOV = Mathf.Lerp(fovOffset, 0f, t);

                if (useLookAtDuringJump)
                {
                    lookAtWeight = Mathf.Lerp(1f, 0f, t);
                }

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            additiveOffset = Vector3.zero;
            additiveFOV = 0f;
            lookAtWeight = 0f;

            isJumpPadTriggered = false;
            isTransitioning = false;

            // Show objects
            ShowObjects();
        }
    }
}