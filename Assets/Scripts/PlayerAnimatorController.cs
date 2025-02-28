using UnityEngine;
using System.Collections;
using UnityEngine.Animations;

namespace RoofTops
{
    [RequireComponent(typeof(Animator), typeof(PlayerController))]
    public class PlayerAnimatorController : MonoBehaviour
    {
        private Animator animator;
        private PlayerController playerController;
        private bool canJumpTrigger = true;
#pragma warning disable 0414
        private bool canFallTrigger = false;         
        private bool wasGrounded;
        private bool jumpButtonWasReleased = false; 
#pragma warning restore 0414
        private bool wasJumpButtonPressed = false;  // Track previous frame's button state
        private bool holdingJump = false;  // Track if jump button has been held since last jump

        // Animation parameter hashes
        private static readonly int jumpTriggerHash = Animator.StringToHash("jumpTrigger");
        private readonly int runSpeedMultiplierHash = Animator.StringToHash("runSpeedMultiplier");
        private static readonly int groundedBoolHash = Animator.StringToHash("groundedBool");
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
        private static readonly int airBoolHash = Animator.StringToHash("airBool");  // New parameter for air state
        private readonly float baseSpineWeight = 0.3f;    // Base weight for spine animation
#pragma warning disable 0414
        private float maxSpineWeight = 1.0f;        
        private float spineWeightThreshold = 0.5f;  
#pragma warning restore 0414
        private readonly int jumpMirrorBoolHash = Animator.StringToHash("jumpMirrorBool");
        private static readonly int smallJumpBoolHash = Animator.StringToHash("smallJumpBool");

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

        [Header("Ground Check")]
        private float groundProximityThreshold = 0.1f;  // How close to ground to start transition
        private bool isNearGround;

        private CharacterController cc;
        private static readonly int IsGrounded = Animator.StringToHash("groundedBool");
        private static readonly int IsFalling = Animator.StringToHash("isFalling");  // Keep as bool

        // Add these with your existing hash definitions
        private static readonly int TurnLayerIndex = 4;  // Adjust this to match your Turn layer index
        private static readonly int TurnTrigger = Animator.StringToHash("turnTrigger");

        // Add with your other serialized fields
        [Header("Turn Animation")]
        [SerializeField] private float turnDuration = 2f;
        [SerializeField] private float turnStartDelay = 0.5f;

        private readonly int dashLayerIndex = 5; // Add with other layer indices
        private readonly int watchLayerIndex = 6; // Add the Watch layer index
        
        [Header("Watch Animation")]
        [SerializeField] private float watchStartDelay = 1.0f;
        [SerializeField] private float watchDuration = 3.0f;
        [SerializeField] private float watchFadeInTime = 0.5f;
        [SerializeField] private float watchFadeOutTime = 0.5f;
        private readonly int watchTriggerHash = Animator.StringToHash("watchTrigger");

        // Add these variables at the class level, near the other private fields
        private bool isDelayedGroundCheckActive = false;
        private int groundedFrameCounter = 0;
        private const int GROUNDED_FRAME_THRESHOLD = 3; // Number of frames to maintain grounded state

        void Awake()
        {
            animator = GetComponent<Animator>();
            playerController = GetComponent<PlayerController>();
            cc = GetComponent<CharacterController>();
            
            // Initialize the Spine layer with 0 weight
            animator.SetLayerWeight(spineLayerIndex, 0);
            
            // Set Turn layer weight and start delayed trigger
            animator.SetLayerWeight(TurnLayerIndex, 1f);
            StartCoroutine(DelayedTurn());
            
            // Initialize Watch layer with 0 weight
            animator.SetLayerWeight(watchLayerIndex, 0f);
        }

        private IEnumerator DelayedTurn()
        {
            yield return new WaitForSeconds(2f);  // Same delay as particles
            animator.SetTrigger(TurnTrigger);
        }

        void Start()
        {
            // Subscribe to game start event
            if (GameManager.Instance != null)
            {
                GameManager.Instance.onGameStarted.AddListener(OnGameStart);
            }
        }

        void OnGameStart()
        {
            // Wait before triggering turn
            StartCoroutine(DelayedTurn());
            
            // Start the watch animation sequence
            StartCoroutine(PlayWatchAnimation());
        }

        void Update()
        {
            // SUPER OBVIOUS DEBUG - should appear in console if Update is running
            if (Input.GetKeyDown(KeyCode.T))
            {
                Debug.LogError("T KEY PRESSED - ATTEMPTING TO ACTIVATE WATCH LAYER");
                
                // Try to directly set the layer weight without any coroutines
                if (animator != null)
                {
                    // Log all layers to verify they exist
                    for (int i = 0; i < animator.layerCount; i++)
                    {
                        Debug.LogError($"LAYER {i}: {animator.GetLayerName(i)}");
                    }
                    
                    // Force the watch layer on immediately
                    if (watchLayerIndex < animator.layerCount)
                    {
                        Debug.LogError($"SETTING WATCH LAYER ({watchLayerIndex}) WEIGHT TO 1");
                        animator.SetLayerWeight(watchLayerIndex, 1f);
                        
                        // Also try to trigger the animation
                        animator.SetTrigger(watchTriggerHash);
                    }
                    else
                    {
                        Debug.LogError($"WATCH LAYER INDEX {watchLayerIndex} IS OUT OF RANGE. ANIMATOR HAS {animator.layerCount} LAYERS");
                    }
                }
                else
                {
                    Debug.LogError("ANIMATOR IS NULL!");
                }
            }
            
            // Toggle layer off with Y key
            if (Input.GetKeyDown(KeyCode.Y))
            {
                Debug.LogError("Y KEY PRESSED - ATTEMPTING TO DEACTIVATE WATCH LAYER");
                if (animator != null && watchLayerIndex < animator.layerCount)
                {
                    Debug.LogError($"SETTING WATCH LAYER ({watchLayerIndex}) WEIGHT TO 0");
                    animator.SetLayerWeight(watchLayerIndex, 0f);
                }
            }
            
            // Only check for pause, allow animations when game starts
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;
            
            bool isGroundedNow = playerController.IsGroundedOnCollider;
            
            // Check if we're close to ground
            isNearGround = Physics.Raycast(transform.position, Vector3.down, groundProximityThreshold);
            
            // Safety check - if we're grounded but still in air state, force exit air state
            if ((isGroundedNow || isNearGround))
            {
                if (animator.GetBool(airBoolHash))
                {
                    animator.SetBool(airBoolHash, false);
                }
                
                // Force reset all jump-related states
                animator.ResetTrigger(jumpTriggerHash);
                canJumpTrigger = true;
                
                // Stop any air state transitions immediately
                if (airStateCoroutine != null)
                {
                    StopCoroutine(airStateCoroutine);
                    airStateCoroutine = null;
                }
            }
            
            // Additional safety check - ensure we go to air state after jump animation
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (!isGroundedNow && !isNearGround && stateInfo.IsName("JumpStart") && stateInfo.normalizedTime >= 0.8f)
            {
                animator.SetBool(airBoolHash, true);
            }
            
            bool isFalling = !isGroundedNow && cc.velocity.y < 0;

            // Update falling state before handling jump
            animator.SetBool(IsFalling, isFalling);

            // Don't allow jump animations while falling
            if (isFalling)
            {
                animator.ResetTrigger(jumpTriggerHash);  // Clear any pending jump triggers
                canJumpTrigger = false;  // Prevent new jumps while falling
            }

            // Handle jump animation only if not falling
            if (wasGrounded != isGroundedNow)
            {
                if (!isGroundedNow && !isFalling) // Just left ground from a jump (not a fall)
                {
                    if (canJumpTrigger)
                    {
                        animator.SetTrigger(jumpTriggerHash);
                        canJumpTrigger = false;
                        
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
                    animator.SetBool(IsFalling, false);
                }
            }
            
            // Check if we just landed
            if (!wasGrounded && isGroundedNow)
            {
                // Start the delayed ground check when landing
                isDelayedGroundCheckActive = true;
                groundedFrameCounter = 0;
            }
            
            // Handle the delayed ground check
            if (isDelayedGroundCheckActive)
            {
                groundedFrameCounter++;
                
                // Force grounded state to be true for a few frames after landing
                isGroundedNow = true;
                
                // After threshold frames, return to normal ground detection
                if (groundedFrameCounter >= GROUNDED_FRAME_THRESHOLD)
                {
                    isDelayedGroundCheckActive = false;
                }
            }
            
            // Update animator parameters with potentially modified grounded state
            animator.SetBool(groundedBoolHash, isGroundedNow);
            
            // While airborne, ensure spine weight is zero.
            if (!isGroundedNow)
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

            // Update grounded and falling states
            bool isFallingNow = !isGroundedNow && cc.velocity.y < 0;  // Falling when not grounded and moving down
            
            animator.SetBool(IsGrounded, isGroundedNow);
            animator.SetBool(IsFalling, isFallingNow);

            // Reset falling state when grounded
            if (isGroundedNow)
            {
                animator.SetBool(IsFalling, false);
            }
        }

        void HandleJumpTrigger()
        {
            if (!InputManager.Exists()) return;
            
            bool isJumpPressed = InputManager.Instance.isJumpPressed;
            
            // Change Input.GetButtonDown check to use InputManager
            if (InputManager.Instance.isJumpPressed && playerController.IsGroundedOnCollider && !holdingJump && canJumpTrigger)
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
            
            // Add explicit ground state synchronization
            if (playerController.IsGroundedOnCollider)
            {
                animator.SetBool(airBoolHash, false);
                animator.SetBool(groundedBoolHash, true);
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
            airStateCoroutine = null;  // Clear the reference when done
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
            animator.SetBool(airBoolHash, false);  // Force exit air state
            animator.SetBool(groundedBoolHash, playerController.IsGroundedOnCollider);
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
            // Only trigger jump if grounded and not falling
            if (playerController.IsGroundedOnCollider && !animator.GetBool(IsFalling))
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

        void HandleLanding()
        {
            animator.SetBool(smallJumpBoolHash, false);
            animator.SetBool(groundedBoolHash, true);  // Force grounded state
            animator.SetBool(airBoolHash, false);
        }

        // Add this public method to trigger the air state from external scripts
        public void TriggerAirState()
        {
            if (airStateCoroutine != null)
            {
                StopCoroutine(airStateCoroutine);
            }
            airStateCoroutine = StartCoroutine(DelayAirState());
        }

        public void ResetAnimationStates()
        {
            // Check if animator is still valid and enabled
            if (animator == null || !animator.isActiveAndEnabled) return;

            // Reset all animation states
            animator.SetBool(airBoolHash, false);
            animator.SetBool(groundedBoolHash, true);
            animator.SetBool(IsFalling, false);
            animator.SetBool(smallJumpBoolHash, false);
            animator.ResetTrigger(jumpTriggerHash);
            animator.ResetTrigger(spineKnockTriggerHash);
            animator.ResetTrigger(watchTriggerHash);
            
            // Reset layer weights
            animator.SetLayerWeight(spineLayerIndex, 0f);
            animator.SetLayerWeight(armLayerIndex, 0f);
            animator.SetLayerWeight(fallLayerIndex, 0f);
            animator.SetLayerWeight(watchLayerIndex, 0f);
            
            // Reset variables
            canJumpTrigger = true;
            holdingJump = false;
            isResettingSpine = false;
            maxJumpHeight = 0f;
            jumpStartHeight = 0f;
            
            // Stop any running coroutines
            if (airStateCoroutine != null)
            {
                StopCoroutine(airStateCoroutine);
                airStateCoroutine = null;
            }

            animator.SetLayerWeight(dashLayerIndex, 0f);
        }

        // Add this method to trigger the turn
        public void TriggerTurn()
        {
            StartCoroutine(PerformTurn());
        }

        private IEnumerator PerformTurn()
        {
            yield return new WaitForSeconds(turnStartDelay);

            // Set layer weight to 1
            animator.SetLayerWeight(TurnLayerIndex, 1f);

            // Trigger the turn animation
            animator.SetTrigger(TurnTrigger);

            yield return new WaitForSeconds(turnDuration);

            // Reset layer weight
            animator.SetLayerWeight(TurnLayerIndex, 0f);
            animator.ResetTrigger(TurnTrigger);
        }

        // Add this near the other public methods
        public void ResetTurnState()
        {
            // Reset the turn layer weight and trigger
            if (animator != null)
            {
                animator.SetLayerWeight(TurnLayerIndex, 1f);
                animator.ResetTrigger(TurnTrigger);
                StartCoroutine(DelayedTurn());
            }
        }

        public void SetDashLayerWeight(float weight)
        {
            animator.SetLayerWeight(dashLayerIndex, Mathf.Clamp01(weight));
        }

        // Add this method to handle the watch animation sequence
        private IEnumerator PlayWatchAnimation()
        {
            Debug.Log("[Watch Animation] Starting sequence...");
            
            // Initial delay before showing watch
            Debug.Log($"[Watch Animation] Waiting for initial delay: {watchStartDelay} seconds");
            yield return new WaitForSeconds(watchStartDelay);
            
            // Trigger the watch animation
            Debug.Log("[Watch Animation] Triggering animation");
            animator.SetTrigger(watchTriggerHash);
            
            // Verify the layer exists
            if (watchLayerIndex >= animator.layerCount)
            {
                Debug.LogError($"[Watch Animation] Layer index {watchLayerIndex} is out of range! Animator only has {animator.layerCount} layers.");
                yield break;
            }
            
            Debug.Log($"[Watch Animation] Layer name at index {watchLayerIndex}: {animator.GetLayerName(watchLayerIndex)}");
            
            // Fade in the watch layer
            Debug.Log("[Watch Animation] Starting fade in");
            float elapsed = 0f;
            while (elapsed < watchFadeInTime)
            {
                elapsed += Time.deltaTime;
                float weight = Mathf.Lerp(0f, 1f, elapsed / watchFadeInTime);
                animator.SetLayerWeight(watchLayerIndex, weight);
                yield return null;
            }
            
            // Ensure it's at full weight
            Debug.Log("[Watch Animation] Reached full weight");
            animator.SetLayerWeight(watchLayerIndex, 1f);
            
            // Hold for the duration
            Debug.Log($"[Watch Animation] Holding for {watchDuration} seconds");
            yield return new WaitForSeconds(watchDuration);
            
            // Fade out the watch layer
            Debug.Log("[Watch Animation] Starting fade out");
            elapsed = 0f;
            while (elapsed < watchFadeOutTime)
            {
                elapsed += Time.deltaTime;
                float weight = Mathf.Lerp(1f, 0f, elapsed / watchFadeOutTime);
                animator.SetLayerWeight(watchLayerIndex, weight);
                yield return null;
            }
            
            // Ensure it's at zero weight
            Debug.Log("[Watch Animation] Animation complete, reset to zero weight");
            animator.SetLayerWeight(watchLayerIndex, 0f);
            animator.ResetTrigger(watchTriggerHash);
        }

        // Add this method to manually trigger the watch animation
        public void TriggerWatchAnimation()
        {
            StartCoroutine(PlayWatchAnimation());
        }
    }
} 