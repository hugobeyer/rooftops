using UnityEngine;

namespace RoofTops
{
    public class JumpPadCameraController : MonoBehaviour
    {
        [Header("References")]
        public Transform playerTransform;
        public Transform cameraTransform;
        private Camera mainCamera;
        private Camera targetCamera;
        private PlayerController playerController;

        [Header("Camera Settings")]
        public Vector3 cameraOffset = new Vector3(24, 5, -4);
        public float jumpPadFOV = 25f;
        public float lerpSpeed = 5f;

        private bool isJumpPadTriggered;
        private Vector3 currentVelocity = Vector3.zero;
        private float currentFovVelocity = 0f;
        private GameObject targetCameraObj;

        private void Start()
        {
            if (cameraTransform == null)
            {
                mainCamera = Camera.main;
                cameraTransform = mainCamera.transform;
            }
            else
            {
                mainCamera = cameraTransform.GetComponent<Camera>();
            }

            if (playerTransform == null)
            {
                playerController = FindAnyObjectByType<PlayerController>();
                if (playerController != null)
                    playerTransform = playerController.transform;
            }
            else
            {
                playerController = playerTransform.GetComponent<PlayerController>();
            }

            // Create target camera
            targetCameraObj = new GameObject("JumpPad Camera");
            targetCamera = targetCameraObj.AddComponent<Camera>();
            targetCamera.enabled = false;  // Don't render from this camera
        }

        private void OnDestroy()
        {
            if (targetCameraObj != null)
                Destroy(targetCameraObj);
        }

        private void LateUpdate()
        {
            if (playerTransform == null) return;

            // Only do smooth logic if jump pad is triggered
            if (!isJumpPadTriggered)
            {
                // If you have a default camera logic, you could put it here
                // or simply return to do nothing until triggered.
                return;
            }

            Vector3 desiredPosition = playerTransform.position + cameraOffset;
            cameraTransform.position = Vector3.SmoothDamp(
                cameraTransform.position,
                desiredPosition,
                ref currentVelocity,
                0.5f
            );

            float targetFov = jumpPadFOV;
            mainCamera.fieldOfView = Mathf.SmoothDamp(
                mainCamera.fieldOfView,
                targetFov,
                ref currentFovVelocity,
                0.5f
            );

            cameraTransform.LookAt(playerTransform);
        }

        public void OnJumpPadTriggered(bool triggered)
        {
            isJumpPadTriggered = triggered;
            if (triggered)
            {
                Vector3 targetPos = playerTransform.position + cameraOffset;
                targetCamera.transform.position = targetPos;
                targetCamera.transform.LookAt(playerTransform);
            }
        }
    }
}