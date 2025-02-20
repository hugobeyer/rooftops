using UnityEngine;
using System.Collections;

namespace RoofTops
{
    public class CameraZoomEffect : MonoBehaviour
    {
        public static CameraZoomEffect Instance { get; private set; }

        [Header("Camera Settings")]
        public float blendInTime = 0.3f;  // Time to blend to jump position
        
        [Header("Position Offsets")]
        public float xOffset = -2f;
        public float yOffset = 0f;  // Added to player's Y position
        public float zOffset = -1f;  // Added to player's Z position

        [Header("FOV Settings")]
        public float baseJumpFOV = 70f;
        public float maxFOVIncrease = 10f;  // Maximum additional FOV at max blend
        
        private Transform targetPlayer;
        private Vector3 startPos;
        private float startFOV;
        private float blendProgress;
        private bool isBlending;
        private bool isFollowing;
        private Vector3 targetPos;
        private Camera cam;
        private Coroutine currentEffectCoroutine;
        private float currentBlendAmount;

        void Awake()
        {
            Instance = this;
            cam = GetComponent<Camera>();
        }

        void LateUpdate()
        {
            if (!isBlending && !isFollowing) return;
            if (targetPlayer == null) return;

            if (isBlending)
            {
                // Update blend progress
                blendProgress += Time.deltaTime / blendInTime;
                if (blendProgress >= 1f)
                {
                    blendProgress = 1f;
                    isBlending = false;
                    isFollowing = true;
                }

                // Calculate current target
                targetPos = new Vector3(
                    xOffset,  // Fixed X offset from world origin
                    targetPlayer.position.y + yOffset,  // Player Y + offset
                    targetPlayer.position.z + zOffset  // Player Z + offset
                );

                // Blend position and FOV
                transform.position = Vector3.Lerp(startPos, targetPos, blendProgress);
                
                // Calculate FOV based on blend amount
                float targetFOV = baseJumpFOV + (maxFOVIncrease * currentBlendAmount);
                cam.fieldOfView = Mathf.Lerp(startFOV, targetFOV, blendProgress);
            }
            else if (isFollowing)
            {
                // Direct follow
                transform.position = new Vector3(
                    xOffset,  // Fixed X offset from world origin
                    targetPlayer.position.y + yOffset,  // Player Y + offset
                    targetPlayer.position.z + zOffset  // Player Z + offset
                );
            }

            // Always look at player when blending or following
            Vector3 lookTarget = targetPlayer.position + Vector3.up;  // Look slightly above player
            transform.LookAt(lookTarget);
        }

        public void TriggerJumpPadEffect(Transform player, float jumpForce)
        {
            targetPlayer = player;
            startPos = transform.position;
            startFOV = cam.fieldOfView;
            blendProgress = 0f;
            isBlending = true;
            isFollowing = false;

            // Calculate blend amount based on jump force relative to base force
            JumpPad jumpPad = FindFirstObjectByType<JumpPad>();
            if (jumpPad != null)
            {
                float baseForce = jumpPad.baseJumpForce;
                currentBlendAmount = Mathf.Clamp01((jumpForce - baseForce) / baseForce);
            }

            // Start timer to stop following
            if (currentEffectCoroutine != null)
            {
                StopCoroutine(currentEffectCoroutine);
            }
            currentEffectCoroutine = StartCoroutine(StopFollowAfterJump(jumpForce));
        }

        private IEnumerator StopFollowAfterJump(float jumpForce)
        {
            float timeToApex = jumpForce / -Physics.gravity.y;
            float totalJumpTime = timeToApex * 2f;
            
            yield return new WaitForSeconds(totalJumpTime);
            
            // Blend back to starting FOV
            float elapsed = 0f;
            float returnBlendTime = 0.3f;
            float currentFOV = cam.fieldOfView;

            while (elapsed < returnBlendTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / returnBlendTime;
                cam.fieldOfView = Mathf.Lerp(currentFOV, startFOV, t);
                yield return null;
            }

            isFollowing = false;
            currentEffectCoroutine = null;
        }
    }
}
