using UnityEngine;
using System.Collections;

namespace RoofTops
{
    [RequireComponent(typeof(Animator), typeof(PlayerController))]
    public class PlayerAnimatorController : MonoBehaviour
    {
        private Animator animator;
        private PlayerController playerController;
        private bool canJumpTrigger = true;
        private bool canFallTrigger = true;
        private bool wasGrounded;
        private bool jumpButtonWasReleased = true;  // Track if jump button was released
        private bool wasJumpButtonPressed = false;  // Track previous frame's button state
        private bool holdingJump = false;  // Track if jump button has been held since last jump

        // Animation parameter hashes
        private readonly int jumpTriggerHash = Animator.StringToHash("jumpTrigger");
        private readonly int runSpeedMultiplierHash = Animator.StringToHash("runSpeedMultiplier");
        private readonly int groundedBoolHash = Animator.StringToHash("groundedBool");
        private readonly int fallTriggerHash = Animator.StringToHash("fallTrigger");
        private readonly int jumpSpeedMultiplierHash = Animator.StringToHash("jumpSpeedMultiplier");
        private readonly int armActionTriggerHash = Animator.StringToHash("armActionTrigger");
        private readonly int armLayerIndex = 1;  // The index of your Arms layer (0 is Base layer)
        private readonly int armJumpTriggerHash = Animator.StringToHash("armsJumpTrigger");
        private readonly int armJumpStateHash = Animator.StringToHash("ArmsJumpAction");  // Add this with your exact state name
        private readonly int spineLayerIndex = 2;  // The index of your Spine layer
        private readonly int spineKnockTriggerHash = Animator.StringToHash("spineKnockTrigger");
        private readonly int fallLayerIndex = 3;  // Fall layer index
        private readonly int airStateHash = Animator.StringToHash("AirState");  // Hash for the air state
        private readonly int airBoolHash = Animator.StringToHash("airBool");  // New parameter for air state
        private readonly float baseSpineWeight = 0.3f;    // Base weight for spine animation
        private readonly float maxSpineWeight = 0.8f;     // Maximum weight for spine animation
        private readonly float spineWeightThreshold = 6f; // Speed at which max weight is reached
        private readonly int jumpMirrorBoolHash = Animator.StringToHash("jumpMirrorBool");
        private readonly int smallJumpBoolHash = Animator.StringToHash("smallJumpBool");

        [Header("Animation Timing")]
        public float armAnimationDelay = 1f;  // Time to wait before starting arm animation

        private float jumpStartHeight;
        private float maxJumpHeight;
        private float currentJumpTimeToApex;

        [Header("Spine Weight Settings")]
        // Minimum jump height to trigger spine weight change.
        public float jumpActivationThreshold = 1.5f;
        // The jump height at which the spine weight reaches 1.
        public float jumpHeightForMaxSpineWeight = 9f;
        // How long to blend the spine layer weight (in seconds) when impacting.
        public float spineBlendTime = 0.3f;
        public float spineResetDelay = 0.3f;  // How long to keep the spine weight before resetting
        public float spineResetDuration = 0.5f;  // How long it takes to smoothly reset to 0
        private float spineResetTime;  // When to start resetting
        private float spineTargetWeight;  // Target weight for smooth transitions
        private bool isResettingSpine;

        public float quickJumpThreshold = 0.2f; // Time threshold for considering it a quick tap
        private float jumpStartTime;

        private Coroutine airStateCoroutine;

        void Awake()
        {
            animator = GetComponent<Animator>();
            playerController = GetComponent<PlayerController>();
            
            // Initialize the Spine layer with 0 weight
            animator.SetLayerWeight(spineLayerIndex, 0);
        }

        void Update()
        {
            // Only check for pause, allow animations when game starts
            if (GameManager.Instance.IsPaused) return;
            
            bool isGroundedNow = playerController.IsGroundedOnCollider;
            
            // Safety check - if we're grounded but still in air state, force exit air state
            if (isGroundedNow && animator.GetBool(airBoolHash))
            {
                animator.SetBool(airBoolHash, false);
            }
            
            // Additional safety check - ensure we go to air state after jump animation
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (!isGroundedNow && stateInfo.IsName("JumpStart") && stateInfo.normalizedTime >= 0.8f)
            {
                animator.SetBool(airBoolHash, true);
            }
            
            // Handle jump animation
            if (wasGrounded != isGroundedNow)
            {
                if (!isGroundedNow) // Just left the ground
                {
                    if (canJumpTrigger)
                    {
                        animator.SetTrigger(jumpTriggerHash);
                        canJumpTrigger = false;
                        
                        // Start coroutine to delay air state
                        if (airStateCoroutine != null)
                        {
                            StopCoroutine(airStateCoroutine);
                        }
                        airStateCoroutine = StartCoroutine(DelayAirState());
                    }
                }
                else // Just landed
                {
                    canJumpTrigger = true;
                    animator.SetBool(airBoolHash, false);
                }
            }
            
            animator.SetBool(groundedBoolHash, isGroundedNow);
            
            // While airborne, ensure spine weight is zero.
            if (!playerController.IsGroundedOnCollider)
            {
                animator.SetLayerWeight(spineLayerIndex, 0f);
            }
            
            // Track max height during jump
            if (!isGroundedNow)
            {
                maxJumpHeight = Mathf.Max(maxJumpHeight, transform.position.y);
                
                // Check if we're in air state and trigger arm animation
                if (stateInfo.shortNameHash == airStateHash && animator.GetLayerWeight(armLayerIndex) == 0)
                {
                    StartCoroutine(BlendArmAnimation());
                }
            }
            
            HandleJumpTrigger();
            UpdateRunSpeed();
            HandleLandingImpact();
            
            wasGrounded = isGroundedNow;  // Store for next frame
        }

        void HandleJumpTrigger()
        {
            bool isJumpPressed = Input.GetButton("Jump");
            
            // Starting a new jump
            if (Input.GetButtonDown("Jump") && playerController.IsGroundedOnCollider && !holdingJump)
            {
                jumpStartTime = Time.time;
                animator.SetBool(smallJumpBoolHash, false); // Reset at start of jump
                
                // Randomize mirror state for this jump
                animator.SetBool(jumpMirrorBoolHash, Random.value > 0.5f);
                
                // Record jump start height
                jumpStartHeight = transform.position.y;
                maxJumpHeight = jumpStartHeight;
                
                var (distance, airTime) = playerController.PredictJumpTrajectory();
                float animationLength = 1f;
                float jumpSpeedRatio = animationLength / airTime;
                
                // Set the main jump animation speed (base layer)
                animator.SetFloat(jumpSpeedMultiplierHash, jumpSpeedRatio);
                animator.SetTrigger(jumpTriggerHash);
                
                holdingJump = true;
            }
            
            // If we're releasing the button
            if (wasJumpButtonPressed && !isJumpPressed)
            {
                float jumpDuration = Time.time - jumpStartTime;
                if (jumpDuration < quickJumpThreshold)
                {
                    animator.SetBool(smallJumpBoolHash, true);
                }
                
                holdingJump = false;
            }
            
            wasJumpButtonPressed = isJumpPressed;
        }

        private IEnumerator DelayAirState()
        {
            yield return new WaitForSeconds(0.3f);
            
            // If we're not grounded after the delay, ALWAYS go to air state
            if (!playerController.IsGroundedOnCollider || !animator.GetBool(groundedBoolHash))
            {
                animator.SetBool(airBoolHash, true);
            }
        }

        private IEnumerator WaitForJumpPeakThenAnimate()
        {
            yield return new WaitForSeconds(currentJumpTimeToApex);
            
            // Only start the animation if we're still in the air
            if (!playerController.IsGroundedOnCollider)
            {
                StartCoroutine(BlendArmAnimation());
            }
        }

        private IEnumerator BlendArmAnimation()
        {
            // Initial delay before starting blend
            yield return new WaitForSeconds(0.2f);  // Adjust this delay as needed
            
            // Start with zero weight
            animator.SetLayerWeight(armLayerIndex, 0);
            
            // Trigger the animation
            animator.SetTrigger(armJumpTriggerHash);
            
            // Blend in
            float blendInTime = 0.2f;
            float elapsed = 0;
            
            while (elapsed < blendInTime)
            {
                elapsed += Time.deltaTime;
                float weight = Mathf.Lerp(0, 1, elapsed / blendInTime);
                animator.SetLayerWeight(armLayerIndex, weight);
                yield return null;
            }

            // Keep full weight while in air
            while (!playerController.IsGroundedOnCollider)
            {
                animator.SetLayerWeight(armLayerIndex, 1f);
                yield return null;
            }
            
            // Blend out
            float blendOutTime = 0.3f;
            elapsed = 0;
            
            while (elapsed < blendOutTime)
            {
                elapsed += Time.deltaTime;
                float weight = Mathf.Lerp(1, 0, elapsed / blendOutTime);
                animator.SetLayerWeight(armLayerIndex, weight);
                yield return null;
            }
            
            animator.SetLayerWeight(armLayerIndex, 0);
        }

        void UpdateRunSpeed()
        {
            float speedRatio = playerController.runSpeedMultiplier;
            animator.SetFloat(runSpeedMultiplierHash, speedRatio);
        }

        public void UpdateGroundedState()
        {
            animator.SetBool(groundedBoolHash, playerController.IsGroundedOnCollider);
        }

        // Called via animation event when jump animation completes
        public void OnJumpAnimationComplete()
        {
            animator.ResetTrigger(jumpTriggerHash);
        }

        // Public method to set animation triggers from other scripts
        public void SetTrigger(string triggerName)
        {
            animator.SetTrigger(triggerName);
        }

        public void SetBool(string boolName, bool value)
        {
            animator.SetBool(boolName, value);
        }

        void HandleLandingImpact()
        {
            if (wasGrounded != playerController.IsGroundedOnCollider && playerController.IsGroundedOnCollider)
            {
                float jumpHeight = maxJumpHeight - jumpStartHeight;
                if (jumpHeight > jumpActivationThreshold)
                {
                    // Calculate and set target weight immediately
                    float normalizedFraction = Mathf.InverseLerp(jumpActivationThreshold, jumpHeightForMaxSpineWeight, jumpHeight);
                    spineTargetWeight = Mathf.Lerp(baseSpineWeight, 1f, normalizedFraction);
                    
                    animator.SetTrigger(spineKnockTriggerHash);
                    animator.SetLayerWeight(spineLayerIndex, spineTargetWeight);
                    
                    // Schedule the reset
                    spineResetTime = Time.time + spineResetDelay;
                    isResettingSpine = false;
                }
            }
            
            // Handle smooth reset
            if (Time.time >= spineResetTime && animator.GetLayerWeight(spineLayerIndex) > 0)
            {
                if (!isResettingSpine)
                {
                    isResettingSpine = true;
                    spineTargetWeight = animator.GetLayerWeight(spineLayerIndex);  // Start from current weight
                }
                
                float resetProgress = (Time.time - spineResetTime) / spineResetDuration;
                float currentWeight = Mathf.Lerp(spineTargetWeight, 0, resetProgress);
                animator.SetLayerWeight(spineLayerIndex, currentWeight);
            }
        }

        public void TriggerFallAnimation()
        {
            // Set layer weight to 1 for full override
            animator.SetLayerWeight(fallLayerIndex, 1f);
            
            // Trigger the fall animation
            animator.SetTrigger(fallTriggerHash);
        }

        public void TriggerJumpAnimation(float jumpForce)
        {
            // Record jump start height
            jumpStartHeight = transform.position.y;
            maxJumpHeight = jumpStartHeight;
            
            var (distance, airTime) = playerController.PredictJumpTrajectory();
            float animationLength = 1f;
            float jumpSpeedRatio = animationLength / airTime;
            
            // Set the main jump animation speed (base layer)
            animator.SetFloat(jumpSpeedMultiplierHash, jumpSpeedRatio);
            animator.SetTrigger(jumpTriggerHash);
        }
    }
} 