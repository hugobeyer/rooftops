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
        

        // Animation parameter hashes
        private static readonly int jumpTriggerHash = Animator.StringToHash("jumpTrigger");
        private readonly int runSpeedMultiplierHash = Animator.StringToHash("runSpeedMultiplier");
        private static readonly int groundedBoolHash = Animator.StringToHash("groundedBool");
        private readonly int jumpSpeedMultiplierHash = Animator.StringToHash("jumpSpeedMultiplier");
        private readonly int armActionTriggerHash = Animator.StringToHash("armActionTrigger");
        private readonly int armLayerIndex = 1;  // The index of your Arms layer (0 is Base layer)
        private readonly int armJumpTriggerHash = Animator.StringToHash("armsJumpTrigger");
        private readonly int armJumpStateHash = Animator.StringToHash("ArmsJumpAction");  // Add this with your exact state name
        private readonly int spineLayerIndex = 2;  // The index of your Spine layer
        private readonly int spineKnockTriggerHash = Animator.StringToHash("spineKnockTrigger");
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
        private static readonly int isFalling = Animator.StringToHash("isFalling");  // Keep as bool

        // Add these with your existing hash definitions
        private static readonly int TurnLayerIndex = 4;  // Adjust this to match your Turn layer index
        private static readonly int TurnTrigger = Animator.StringToHash("turnTrigger");

        // Add with your other serialized fields
        [Header("Turn Animation")]
        [SerializeField] public float turnDuration = 2f;
        [SerializeField] public float turnStartDelay = 0.5f;

        private readonly int dashLayerIndex = 5; // Add with other layer indices
        private int watchLayerIndex = 6; // Removed readonly to allow runtime updates

        [Header("Watch Animation")]
        [SerializeField] private float watchStartDelay = 1.0f;
        [SerializeField] private float watchDuration = 3.0f;
        [SerializeField] private float watchFadeInTime = 0.5f;
        [SerializeField] private float watchFadeOutTime = 0.5f;
        [SerializeField] public GameObject watchParticleEffect; // Reference to particle effect in the scene

        // Add a flag to track when watch animation is playing
        // private bool isWatchAnimationPlaying = false;

        // Add these variables at the class level, near the other private fields
        private bool isDelayedGroundCheckActive = false;
        private int groundedFrameCounter = 0;
        private const int GROUNDED_FRAME_THRESHOLD = 3; // Number of frames to maintain grounded state

        // Add a reference to track the active watch animation coroutine
        private Coroutine activeWatchCoroutine = null;

        void Awake()
        {
            animator = GetComponent<Animator>();
            playerController = GetComponent<PlayerController>();
            cc = GetComponent<CharacterController>();

            // Check if components are missing
            if (animator == null)
            {
                Debug.LogError("PlayerAnimatorController: Animator component is missing on " + gameObject.name);
                return;
            }

            // Log all animator parameters for debugging
            Debug.Log("PlayerAnimatorController: Listing all animator parameters:");
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                Debug.Log($"Parameter: {param.name}, Type: {param.type}, Hash: {param.nameHash}");
            }

            if (playerController == null)
            {
                Debug.LogError("PlayerAnimatorController: PlayerController component is missing on " + gameObject.name);
                return;
            }

            if (cc == null)
            {
                Debug.LogError("PlayerAnimatorController: CharacterController component is missing on " + gameObject.name);
                return;
            }

            // Initialize the Spine layer with 0 weight
            animator.SetLayerWeight(spineLayerIndex, 0);

            // Initialize Turn layer with 0 weight (but don't start the animation yet)
            animator.SetLayerWeight(TurnLayerIndex, 0f);

            // Initialize Watch layer with 0 weight
            animator.SetLayerWeight(watchLayerIndex, 0f);

            // Verify layer indices by checking layer names
            VerifyLayerIndices();
        }

        private void VerifyLayerIndices()
        {
            // Log all available layers for debugging
            Debug.Log($"[LAYERS] Animator has {animator.layerCount} layers:");

            for (int i = 0; i < animator.layerCount; i++)
            {
                string layerName = animator.GetLayerName(i);
                Debug.Log($"[LAYERS] Layer {i}: {layerName}");

                // Check if this is the Watch layer
                if (layerName.Contains("Watch"))
                {
                    if (i != watchLayerIndex)
                    {
                        Debug.LogWarning($"[WATCH] Watch layer found at index {i}, but watchLayerIndex is set to {watchLayerIndex}. Updating to correct index.");
                        watchLayerIndex = i;
                    }
                    else
                    {
                        Debug.Log($"[WATCH] Watch layer confirmed at index {watchLayerIndex}");
                    }
                }
            }
        }

        private IEnumerator DelayedTurn()
        {
            // Initial delay before showing turn
            yield return new WaitForSeconds(turnStartDelay);  // Use the public variable instead of hardcoded value

            // Set layer weight to 1 (turn it on)
            animator.SetLayerWeight(TurnLayerIndex, 1f);

            // Wait a frame to ensure the layer weight is applied
            yield return null;

            // ONLY NOW trigger the turn animation (after the layer is visible)
            animator.SetTrigger(TurnTrigger);

            // Wait for the animation to play
            yield return new WaitForSeconds(turnDuration);

            // Fade out the turn layer smoothly
            float fadeOutDuration = 0.5f; // How long the fade out takes
            float elapsed = 0f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float weight = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
                animator.SetLayerWeight(TurnLayerIndex, weight);
                yield return null;
            }

            // Ensure it's completely off
            animator.SetLayerWeight(TurnLayerIndex, 0f);
        }

        void Start()
        {
            // Subscribe to game start event
            if (GameManager.Instance != null)
            {
                Debug.Log("[WATCH] Subscribing to GameManager.onGameStarted event");
                GameManager.Instance.onGameStarted.AddListener(OnGameStart);

                // Start the watch animation with proper timing
                StartCoroutine(DelayedWatch());
            }
            else
            {
                Debug.LogError("[WATCH] GameManager.Instance is null, cannot subscribe to onGameStarted event");
            }
        }

        void OnGameStart()
        {
            Debug.Log("[WATCH] OnGameStart called - game has officially started");

            // Use a delayed call to start the turn animation
            // This ensures it doesn't happen immediately when pressing play
            StartCoroutine(DelayedStartTurn());

            // Trigger the watch animation
            TriggerWatchAnimation();
        }

        private IEnumerator DelayedWatch()
        {
            // Set the flag to indicate animation is playing
            // isWatchAnimationPlaying = true;

            // Initial delay before showing watch
            yield return new WaitForSeconds(watchStartDelay);

            Debug.Log("[WATCH] Setting watch layer weight to 1");

            // Enable the particle effect if assigned
            if (watchParticleEffect != null)
            {
                Debug.Log("[WATCH] Enabling watch particle effect");
                watchParticleEffect.SetActive(true);

                // Make sure any particle system is enabled and playing
                ParticleSystem particleSystem = watchParticleEffect.GetComponent<ParticleSystem>();
                if (particleSystem != null)
                {
                    particleSystem.Play();
                    Debug.Log("[WATCH] Started particle system");
                }
            }

            // Fade in the watch layer smoothly
            float fadeInDuration = watchFadeInTime;
            float elapsed = 0f;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float weight = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                animator.SetLayerWeight(watchLayerIndex, weight);
                yield return null;
            }

            // Ensure it's at full weight
            animator.SetLayerWeight(watchLayerIndex, 1f);

            // Wait for the animation to play
            yield return new WaitForSeconds(watchDuration);

            Debug.Log("[WATCH] Fading out watch layer");

            // Fade out the watch layer smoothly
            float fadeOutDuration = watchFadeOutTime;
            elapsed = 0f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float weight = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
                animator.SetLayerWeight(watchLayerIndex, weight);
                yield return null;
            }

            // Ensure it's completely off
            animator.SetLayerWeight(watchLayerIndex, 0f);

            // Disable the particle effect if assigned
            if (watchParticleEffect != null)
            {
                Debug.Log("[WATCH] Disabling watch particle effect");

                // Stop any particle system first
                ParticleSystem particleSystem = watchParticleEffect.GetComponent<ParticleSystem>();
                if (particleSystem != null)
                {
                    particleSystem.Stop();
                    Debug.Log("[WATCH] Stopped particle system");
                }

                watchParticleEffect.SetActive(false);
            }

            // Reset the flag when animation is complete
            // isWatchAnimationPlaying = false;

            // Clear the active coroutine reference
            activeWatchCoroutine = null;

            Debug.Log("[WATCH] Watch animation complete");
        }

        private IEnumerator DelayedStartTurn()
        {
            // Add a delay before even starting the turn animation sequence
            // This prevents it from appearing immediately when pressing play
            yield return new WaitForSeconds(2.0f);

            // Now start the actual turn animation sequence
            StartCoroutine(DelayedTurn());
        }

        void Update()
        {
            // Remove all key input detection

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
            if (!isGroundedNow && !isNearGround && stateInfo.normalizedTime >= 0.8f)
            {
                animator.SetBool(airBoolHash, true);
            }

            bool isCurrentlyFalling = !isGroundedNow && cc.velocity.y < 0;

            // Update falling state before handling jump
            animator.SetBool(isFalling, isCurrentlyFalling);

            // Don't allow jump animations while falling
            if (isCurrentlyFalling)
            {
                animator.ResetTrigger(jumpTriggerHash);  // Clear any pending jump triggers
                canJumpTrigger = false;  // Prevent new jumps while falling
            }

            // Handle jump animation only if not falling
            if (wasGrounded != isGroundedNow)
            {
                if (!isGroundedNow) // Just left ground
                {
                    if (!isCurrentlyFalling && canJumpTrigger) // It's a jump (not a fall)
                    {
                        animator.SetTrigger(jumpTriggerHash);
                        canJumpTrigger = false;

                        if (airStateCoroutine != null)
                        {
                            StopCoroutine(airStateCoroutine);
                        }
                        airStateCoroutine = StartCoroutine(DelayAirState());
                    }
                    else // It's a fall
                    {
                        // Explicitly set falling state when going off an edge
                        animator.SetBool(airBoolHash, true);
                        animator.SetBool(isFalling, true);
                    }
                }
                else // Just landed
                {
                    canJumpTrigger = true;
                    animator.SetBool(airBoolHash, false);
                    animator.SetBool(isFalling, false);
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
            // MODIFIED: Add a check to prevent falling state during active jump
            bool isFallingNow = !isGroundedNow && cc.velocity.y < 0;  // Falling when not grounded and moving down
            
            // Don't set falling state if we're in the middle of a jump animation
            // Instead of checking for specific state names, check if we're in a jump-related state
            // by using the airBool parameter which should be true during jumps
            bool isInJumpAnimation = animator.GetBool(airBoolHash) && !isFallingNow;
            
            // Only set falling if we're truly falling and not in a jump animation
            if (isFallingNow && !isInJumpAnimation)
            {
                animator.SetBool(isFalling, true);
            }
            else if (isGroundedNow)
            {
                animator.SetBool(isFalling, false);
            }
            
            animator.SetBool(IsGrounded, isGroundedNow);
            
            // Reset falling state when grounded
            if (isGroundedNow)
            {
                animator.SetBool(isFalling, false);
            }
        }

        void HandleJumpTrigger()
        {
            if (!InputActionManager.Exists()) return;

            bool isJumpPressed = InputActionManager.Instance.IsJumping;
            bool isHoldingJump = InputActionManager.Instance.IsHoldingJump;

            // MODIFIED: Allow jump trigger even when holding jump button if we're grounded
            // This prevents issues when the player holds down the jump button
            if (InputActionManager.Instance.IsJumping && playerController.IsGroundedOnCollider && canJumpTrigger)
            {
                jumpStartTime = Time.time;
                
                // Check if parameters exist before setting them
                if (HasParameter(smallJumpBoolHash))
                {
                    animator.SetBool(smallJumpBoolHash, false); // Reset at start of jump
                }
                else
                {
                    Debug.LogWarning("Parameter 'smallJumpBool' does not exist in the Animator Controller");
                }

                // Randomize mirror state for this jump
                if (HasParameter(jumpMirrorBoolHash))
                {
                    animator.SetBool(jumpMirrorBoolHash, Random.value > 0.5f);
                }
                else
                {
                    Debug.LogWarning("Parameter 'jumpMirrorBool' does not exist in the Animator Controller");
                }

                // Record jump start height
                jumpStartHeight = transform.position.y;
                maxJumpHeight = jumpStartHeight;

                float animationLength = 1f;
                float airTime = 1f; // Default value
                float distance = 5f; // Default value

                // Add null check for playerController
                if (playerController != null)
                {
                    try
                    {
                        (distance, airTime) = playerController.PredictJumpTrajectory();
                    }
                    catch (System.NullReferenceException e)
                    {
                        Debug.LogWarning($"PlayerAnimatorController: Error in PredictJumpTrajectory: {e.Message}");
                        // Keep using default values if prediction fails
                    }
                }
                else
                {
                    Debug.LogWarning("PlayerAnimatorController: playerController is null, using default jump values");
                }

                float jumpSpeedRatio = animationLength / airTime;

                // Set the main jump animation speed (base layer)
                animator.SetFloat(jumpSpeedMultiplierHash, jumpSpeedRatio);
                animator.SetTrigger(jumpTriggerHash);
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
            }

            wasJumpButtonPressed = isJumpPressed;
        }

        private IEnumerator DelayAirState()
        {
            // Wait a short time before transitioning to air state
            yield return new WaitForSeconds(0.3f);

            // MODIFIED: Add a check to prevent immediate falling state after jump
            // If we're not grounded after the delay, go to air state
            if (!playerController.IsGroundedOnCollider)
            {
                animator.SetBool(airBoolHash, true);
                
                // Add a small additional delay before allowing falling state
                // This prevents the character from immediately transitioning to falling
                yield return new WaitForSeconds(0.2f);
                
                // Now check if we're actually falling (negative Y velocity)
                if (!playerController.IsGroundedOnCollider && cc.velocity.y < 0)
                {
                    animator.SetBool(isFalling, true);
                }
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
            // Check if animator is null and try to get it if needed
            if (animator == null)
            {
                animator = GetComponent<Animator>();
                
                // If still null, log an error and return
                if (animator == null)
                {
                    Debug.LogError("PlayerAnimatorController: Animator component is missing on " + gameObject.name);
                    return;
                }
            }
            
            animator.SetTrigger(triggerName);
        }

        public void SetBool(string boolName, bool value)
        {
            // Check if animator is null and try to get it if needed
            if (animator == null)
            {
                animator = GetComponent<Animator>();
                
                // If still null, log an error and return
                if (animator == null)
                {
                    Debug.LogError("PlayerAnimatorController: Animator component is missing on " + gameObject.name);
                    return;
                }
            }
            
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

        public void TriggerJumpAnimation(float jumpForce)
        {
            // Only trigger jump if grounded and not falling
            if (playerController.IsGroundedOnCollider && !animator.GetBool(isFalling))
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
            animator.SetBool(isFalling, false);
            animator.SetBool(smallJumpBoolHash, false);
            animator.ResetTrigger(jumpTriggerHash);
            animator.ResetTrigger(spineKnockTriggerHash);

            // Reset layer weights
            animator.SetLayerWeight(spineLayerIndex, 0f);
            animator.SetLayerWeight(armLayerIndex, 0f);
            animator.SetLayerWeight(watchLayerIndex, 0f);

            // Reset variables
            canJumpTrigger = true;
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

            // Fade out the turn layer smoothly
            float fadeOutDuration = 0.5f; // How long the fade out takes
            float elapsed = 0f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float weight = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
                animator.SetLayerWeight(TurnLayerIndex, weight);
                yield return null;
            }

            // Ensure it's completely off
            animator.SetLayerWeight(TurnLayerIndex, 0f);
            animator.ResetTrigger(TurnTrigger);
        }

        // Add this near the other public methods
        public void ResetTurnState()
        {
            // Reset the turn layer weight and trigger
            if (animator != null)
            {
                // Don't set the weight to 1 immediately - this was causing the immediate visibility
                animator.SetLayerWeight(TurnLayerIndex, 0f);
                animator.ResetTrigger(TurnTrigger);
                // Start the delayed turn which will handle the weight properly
                if (isActiveAndEnabled)
                {
                    StartCoroutine(DelayedTurn());
                }
            }
        }

        public void SetDashLayerWeight(float weight)
        {
            animator.SetLayerWeight(dashLayerIndex, Mathf.Clamp01(weight));
        }

        // Simple method to trigger the watch animation
        public void TriggerWatchAnimation()
        {
            Debug.Log("[WATCH] Manually triggering watch animation");

            // Stop any existing watch animation
            if (activeWatchCoroutine != null)
            {
                StopCoroutine(activeWatchCoroutine);
                activeWatchCoroutine = null;
            }

            // Start the watch animation
            activeWatchCoroutine = StartCoroutine(DelayedWatch());
        }

        private bool HasParameter(int hash)
        {
            // Make sure animator is not null
            if (animator == null)
            {
                animator = GetComponent<Animator>();
                if (animator == null)
                {
                    Debug.LogError("Animator component is missing on " + gameObject.name);
                    return false;
                }
            }

            // Check if the parameter exists
            AnimatorControllerParameter[] parameters = animator.parameters;
            foreach (AnimatorControllerParameter parameter in parameters)
            {
                if (parameter.nameHash == hash)
                {
                    return true;
                }
            }
            return false;
        }
    }
}