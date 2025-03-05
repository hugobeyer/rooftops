using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Events; // Add this for UnityEvent

namespace RoofTops
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float jumpForce = 7f;
        public float maxJumpForce = 14f;  // Maximum force when fully charged
        public float jumpChargeRate = 7f;  // How fast the jump charges
        public float jumpForceGrowthRate = 0.1f;
        public float runSpeedMultiplier = 1f;

        [Header("Jump Settings")]
        public float jumpCutFactor = 0.5f;

        [Header("Animation")]
        public float runSpeed = 1f;

        [Header("AI Learning Events")]
        // Events for AI learning system
        public UnityEvent onJump = new UnityEvent();

        [Header("Debug")]
        [SerializeField] private bool showJumpMetrics = false;

        [Header("Charge Jump State")]
        public bool isChargingJump { get; private set; }
        public float currentChargedJumpForce { get; private set; }

        private float jumpStartTime;
        private float predictedFlightTime;
        private bool isDead = false;

        private CharacterController cc;
        //public bool isVaulting = false;
        public ModulePool modulePool;
        private Vector3 _velocity = Vector3.zero;
        private PlayerColorEffects colorEffects;

        // Jump tracking for metrics
        private Vector3 jumpStartPosition;
        private bool wasInAir = false;

        [Header("Jump Pad State")]
        private bool isOnJumpPad = false;
        private float jumpPadTimer = 0f;
        private const float JUMP_PAD_DURATION = 1.5f;  // Match this with JumpPad's effectDuration

        [Header("Ground Detection")]
        public float groundCheckRadius = 0.5f;
        public float groundCheckDistance = 0.2f;
        public LayerMask groundLayer;  // Set this in inspector to include ground layers
        
        // Coyote time (grace period for jumping after leaving a platform)
        public float coyoteTime = 0.2f;
        private float coyoteTimeCounter = 0f;
        private float timeSinceLastGrounded = 0f;
        
        [Header("Dash Settings")]
        public float dashSpeedMultiplier = 1.5f;
        public float dashDuration = 0.3f;      // Total dash duration
        public float dashCooldown = 1f;
        public int dashTridotCost = 1;          // tridots points required to dash
        //public float doubleTapThreshold = 0.3f;

        private float lastJumpPressTime;
        private bool canDash = true;
        private bool isDashing;
        private bool isJumping = false;
        private float dashCooldownTimer = 0f;
        private float originalGravity;
        private float dashTimer;
        private float originalGameSpeed;

        // Add cooldown for "not enough Tridots" message
        private float noTridotsMessageCooldown = 1.0f;  // 1 second cooldown
        private float lastNoTridotsMessageTime = -1.0f;  // Last time the message was shown

        [Header("Dash Visuals")]
        public Material dashMaterial;
        public Material secondaryDashMaterial;
        public Material tertiaryDashMaterial;
        private int dashLerpID;
        private float dashLerp;
        public string additionalShaderParam = "_AdditionalEffect";
        private int additionalShaderParamID;

        [Header("Dash Timing")]
        [Range(0f, 0.5f)] public float fadeInPortion = 0.2f; // First 20% of duration for fade in
        [Range(0f, 0.5f)] public float fadeOutPortion = 0.2f; // Last 20% for fade out

        // Calculated times
        private float fadeInTime;
        private float fadeOutTime;
        private float fullEffectTime;

        private PlayerAnimatorController playerAnimator;

        [Header("Dash Effects")]
        public GameObject dashEffectPrefab;
        public Vector3 effectOffset = new Vector3(0, 0.5f, 0);
        private GameObject activeDashEffect;
        public AudioClip dashSound;
        public AudioClip noDashSound;  // Sound to play when player can't dash
        public GameObject noDashEffectPrefab;  // Particle effect for failed dash attempt
        [Range(0f, 1f)]
        public float dashVolume = 0.7f;
        private AudioSource audioSource;

        public bool IsGrounded
        {
            get
            {
                // First check the CharacterController's built-in ground detection
                if (cc != null && cc.isGrounded) return true;

                // Additional sphere cast check for more reliable ground detection
                Vector3 origin = transform.position + Vector3.up * 0.1f; // Slight offset up
                return Physics.SphereCast(origin, groundCheckRadius, Vector3.down,
                    out RaycastHit hit, groundCheckDistance, groundLayer);
            }
        }

        // Keeping these for backward compatibility, but they now use the unified IsGrounded property
        public bool IsGroundedOnCollider => IsGrounded;
        public bool IsGroundedOnTrigger => IsGrounded;

        // Add public property to expose jump pad state
        public bool IsOnJumpPad => isOnJumpPad;
        
        public bool IsDead => isDead;

        public bool IsDashing => isDashing;

        void Awake()
        {
            cc = GetComponent<CharacterController>();

            // Add audio source component
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D sound

            var meshObject = GetComponentInChildren<MeshRenderer>()?.gameObject;
            if (meshObject != null)
            {
                colorEffects = meshObject.GetComponent<PlayerColorEffects>();
            }

            if (modulePool == null)
            {
                modulePool = ModulePool.Instance;
            }

            playerAnimator = GetComponent<PlayerAnimatorController>();

            // Initialize events if null
            if (onJump == null)
                onJump = new UnityEvent();
        }

        void Start()
        {
            // Register this player with the SceneReferenceManager
            SceneReferenceManager.Instance.RegisterPlayer(gameObject);
            
            dashLerpID = Shader.PropertyToID("_DashLerp");
            additionalShaderParamID = Shader.PropertyToID(additionalShaderParam);
            if (dashMaterial != null)
            {
                dashMaterial.SetFloat(dashLerpID, 0f);
                dashMaterial.SetFloat(additionalShaderParamID, 0f);
            }
            if (secondaryDashMaterial != null)
            {
                secondaryDashMaterial.SetFloat(dashLerpID, 0f);
                secondaryDashMaterial.SetFloat(additionalShaderParamID, 0f);
            }
            if (tertiaryDashMaterial != null)
            {
                tertiaryDashMaterial.SetFloat(dashLerpID, 0f);
                tertiaryDashMaterial.SetFloat(additionalShaderParamID, 0f);
            }

            SetupInputActions();
        }

        void Update()
        {
            // Prevent any jump or dash logic if the game hasn't started
            switch (GameManager.GamesState)
            {
                case GameStates.Playing:
                case GameStates.GameOver:
                break;
                default: return;
            }

            if (isDead) return;

            // Check if we just landed or just started falling
            bool isGroundedNow = IsGrounded;
            if (!isGroundedNow && isGroundedNow != wasInAir)
            {
                // Just took off
                jumpStartPosition = transform.position;
                // Fire jump event
                onJump.Invoke();

                // Check if we've shown the dash hint before using PlayerPrefs
                bool hasShownDashHint = PlayerPrefs.GetInt("HasShownDashInfo", 0) == 1;

                // Show dash hint if player has tridots points AND hasn't seen the hint before
                if (GameManager.Instance != null &&
                    GameManager.Instance.gameData != null &&
                    GameManager.Instance.gameData.lastRunTridotCollected >= dashTridotCost &&
                    !hasShownDashHint)
                {
                    ShowDashHint();

                    // Save to PlayerPrefs that we've shown the dash info
                    PlayerPrefs.SetInt("HasShownDashInfo", 1);
                    PlayerPrefs.Save();

                    // Also update GameManager's data for the current session
                    if (GameManager.Instance.gameData != null)
                    {
                        GameManager.Instance.gameData.hasShownDashInfo = true;
                    }
                }

                if (showJumpMetrics)
                    Debug.Log("Jump started at: " + jumpStartPosition);
            }
            else if (isGroundedNow && isGroundedNow != wasInAir)
            {
                // Just landed
                HandleJumpMetrics();
                HandleLanding();
            }
            wasInAir = !isGroundedNow;

            if ((transform.position.y < -7f || isDead) && modulePool.gameSpeed > 0)
            {
                DeathMessageDisplay.Instance?.ShowMessage();
                FindFirstObjectByType<DistanceTracker>()?.SaveDistance();
                StartCoroutine(DelayedReset());
            }

            if (isDead)
            {
                if (InputActionManager.Exists() && InputActionManager.Instance.IsJumping)
                {
                    StartCoroutine(DelayedReset());
                }

                if (!IsGrounded)
                {
                    _velocity.y += Physics.gravity.y * Time.deltaTime;
                }
                Vector3 deathMove = new Vector3(0, _velocity.y, 0);
                cc.Move(deathMove * Time.deltaTime);
                return;
            }

            if (modulePool != null)
            {
                runSpeedMultiplier = modulePool.gameSpeed / GameManager.Instance.normalGameSpeed;
            }

            // Apply gravity
            if (!IsGrounded)
            {
                _velocity.y += Physics.gravity.y * Time.deltaTime;
            }

            _velocity.x = 0;
            _velocity.z = 0;
            Vector3 verticalMove = new Vector3(0, _velocity.y, 0);
            cc.Move(verticalMove * Time.deltaTime);

            // Force position to X=0 and Z=0
            Vector3 pos = transform.position;
            pos.x = 0;
            pos.z = 0;  // Add this line to lock Z position
            transform.position = pos;

            // Handle jump pad timer
            if (isOnJumpPad)
            {
                jumpPadTimer -= Time.deltaTime;
                if (jumpPadTimer <= 0)
                {
                    isOnJumpPad = false;
                }
            }

            if (isDashing)
            {
                dashTimer -= Time.deltaTime;
                if (dashTimer <= 0) EndDash();
            }

            // If you still want local incremental logic, keep this line:
            jumpForce += jumpForceGrowthRate * Time.deltaTime;
        }

        private void OnDestroy()
        {
            TearDownInputActions();
        }

        private void HandleJumpMetrics()
        {
            // Calculate jump distance
            float jumpDistance = Vector3.Distance(new Vector3(jumpStartPosition.x, 0, jumpStartPosition.z),
                                                 new Vector3(transform.position.x, 0, transform.position.z));

            if (showJumpMetrics)
                Debug.Log($"Jump metrics - Distance: {jumpDistance:F2}m");
        }

       
        void HandleDashInput()
        {
            if (!IsGrounded)
            {
                // First check if we can dash
                if (CanDash())
                {
                    StartDash();
                    return;
                }

                // IMPORTANT: Only show the message when player explicitly tries to dash in air
                // by pressing jump while airborne AND all other dash conditions are met EXCEPT having enough TRIDOTS
                if (canDash && !isOnJumpPad && dashTimer <= 0)
                {
                    // Get the current tridots amount directly from gameData
                    float currentTridots = 0;
                    if (GameManager.Instance != null && GameManager.Instance.gameData != null)
                    {
                        currentTridots = GameManager.Instance.gameData.lastRunTridotCollected;
                    }

                    // ONLY show the message if we truly don't have enough TRIDOTS
                    // AND the player explicitly tried to dash by pressing jump in air
                    // AND we're not in the message cooldown period
                    if (currentTridots < dashTridotCost && Time.time - lastNoTridotsMessageTime >= noTridotsMessageCooldown)
                    {
                        ShowNotEnoughTridotsEffect();
                        lastNoTridotsMessageTime = Time.time;  // Update the last message time
                    }
                }
            }
        }

        void ShowNotEnoughTridotsEffect()
        {
            // We already checked that we don't have enough TRIDOTS in HandleDashInput,
            // so we can skip that check here and just show the effects

            // Visual feedback that player doesn't have enough tridots
            if (colorEffects != null)
            {
                colorEffects.StartSlowdownEffect();
            }

            // Play the "can't dash" sound
            if (audioSource != null && noDashSound != null)
            {
                audioSource.PlayOneShot(noDashSound, dashVolume);
            }

            // Spawn the "no dash" particle effect
            if (noDashEffectPrefab != null)
            {
                GameObject noDashEffect = Instantiate(noDashEffectPrefab, transform);
                noDashEffect.transform.localPosition = effectOffset;

                // Destroy the effect after a short time
                Destroy(noDashEffect, 1.0f);
            }

            // Use the new GameMessageDisplay if available
            if (GameMessageDisplay.Instance != null)
            {
                // Use the message ID system - the ZERO_TRIDOTS message is defined in the library
                GameMessageDisplay.Instance.ShowMessageByID("ZERO_TRIDOTS", dashTridotCost);
            }
            else
            {
                // Fallback to Debug.Log
                Debug.Log($"Not enough TRIDOTS to dash! Need {dashTridotCost}");
            }
        }

        void StartDash()
        {
            // Consume tridots points
            if (GameManager.Instance != null && GameManager.Instance.gameData != null)
            {
                // Log before consumption
                float beforeGameDataTridots = GameManager.Instance.gameData.lastRunTridotCollected;

                // Use negative value to consume tridots points
                EconomyManager.Instance.AddTridots(-dashTridotCost);

                // Log after consumption
                float afterGameDataTridots = GameManager.Instance.gameData.lastRunTridotCollected;

                Debug.Log($"DASH CONSUMED: Before={beforeGameDataTridots}, After={afterGameDataTridots}, Cost={dashTridotCost}");
            }

            isDashing = true;
            canDash = false;
            dashTimer = dashDuration;

            // Play dash sound
            if (dashSound != null)
            {
                // Try both methods to ensure sound plays
                audioSource.PlayOneShot(dashSound, dashVolume);
                AudioSource.PlayClipAtPoint(dashSound, Camera.main.transform.position, dashVolume);
                Debug.Log("Playing dash sound"); // Debug feedback
            }
            else
            {
                Debug.LogWarning("Dash sound is not assigned!"); // Debug warning
            }

            // Create dash effect
            if (dashEffectPrefab != null)
            {
                if (activeDashEffect != null) Destroy(activeDashEffect);
                activeDashEffect = Instantiate(dashEffectPrefab, transform);
                activeDashEffect.transform.localPosition = effectOffset;
                activeDashEffect.SetActive(true);
            }

            // Store original values
            originalGameSpeed = ModulePool.Instance.gameSpeed;
            originalGravity = Physics.gravity.y;

            // Apply dash effects
            ModulePool.Instance.SetGameSpeed(originalGameSpeed * dashSpeedMultiplier);
            Physics.gravity = Vector3.zero;
            _velocity.y = 0;
            dashLerp = 1f;
            StartCoroutine(UpdateDashVisual());
        }

        void EndDash()
        {
            isDashing = false;
            ModulePool.Instance.SetGameSpeed(originalGameSpeed);
            Physics.gravity = new Vector3(0, originalGravity, 0);
            if (dashMaterial != null)
            {
                dashMaterial.SetFloat(dashLerpID, 0f);
            }
        }

        bool CanDash()
        {
            bool isInAir = !IsGrounded;
            bool dashReady = canDash;
            bool notOnJumpPad = !isOnJumpPad;
            bool noDashInProgress = dashTimer <= 0;
            bool hasEnoughTridots = false;

            if (EconomyManager.Instance != null)
            {
                hasEnoughTridots = EconomyManager.Instance.GetCurrentTridots() >= dashTridotCost;
            }
            else if (GameManager.Instance != null && GameManager.Instance.gameData != null)
            {
                hasEnoughTridots = GameManager.Instance.gameData.lastRunTridotCollected >= dashTridotCost;
            }

            return isInAir && dashReady && notOnJumpPad && noDashInProgress && hasEnoughTridots;
        }

        void HandleLanding()
        {
            // Reset air time tracking
            timeSinceLastGrounded = 0;
            
            // Reset jump state
            isJumping = false;
            
            // Reset dash state if we landed
            if (dashCooldownTimer <= 0)
            {
                canDash = true;
            }
            
            // Reset jump pad state
            isOnJumpPad = false;
            
            // Reset jump charging
            isChargingJump = false;
            currentChargedJumpForce = jumpForce;
            
            // Reset coyote time
            coyoteTimeCounter = coyoteTime;
        }

        // void ComputePredictedFlightTime()
        // {
        //     float verticalVelocity = _velocity.y;
        //     float gravity = Physics.gravity.y;
        //     predictedFlightTime = verticalVelocity > 0 ? (verticalVelocity / -gravity) * 2 : 0f;
        // }



        private IEnumerator DelayedReset()
        {
            // Hide the death message
            DeathMessageDisplay.Instance?.HideMessage();

            // Reset camera
            FindFirstObjectByType<NoiseMovement>()?.ResetCamera();

            // Wait for a full second
            yield return new WaitForSeconds(1.0f);

            // Ask GameManager to perform the full reset
            GameManager.Instance.ResetGame();
        }

        public void SetRunSpeedMultiplier(float multiplier, float duration = 0f)
        {
            runSpeedMultiplier = multiplier;

            if (duration > 0f)
            {
                StartCoroutine(ResetSpeedMultiplierAfter(duration));
            }
        }

        private System.Collections.IEnumerator ResetSpeedMultiplierAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            runSpeedMultiplier = 1f; // Reset to default
        }

        // Removing dynamic toggling of the player's CapsuleCollider.
        // This ensures the inspector-assigned CapsuleCollider remains enabled,
        // so that IsGroundedOnTrigger correctly detects overlapping GroundedTrigger(s).
        // void UpdateColliderState()
        // {
        //     // No changes made. The CapsuleCollider remains enabled.
        // }

        // This method allows external scripts (like the vault script) to set the vaulting state.
        //public void SetVaultingState(bool vaulting)
        //{
        //isVaulting = vaulting;
        //}

        public (float distance, float airTime) PredictJumpTrajectory()
        {
            float timeToApex = jumpForce / -Physics.gravity.y;  // Time to reach highest point
            float totalAirTime = timeToApex * 2;  // Total time in air (up + down)

            // Distance covered = speed * time
            float speed = 0f;
            
            // Add null check for modulePool
            if (modulePool != null)
            {
                speed = modulePool.gameSpeed;
            }
            else if (GameManager.Instance != null)
            {
                // Fallback to GameManager's initial speed if modulePool is null
                speed = GameManager.Instance.initialGameSpeed;
                Debug.LogWarning("PlayerController: ModulePool instance not found, using GameManager.initialGameSpeed");
            }
            else
            {
                // Default fallback if both are null
                speed = 5f;
                Debug.LogWarning("PlayerController: Both ModulePool and GameManager instances not found, using default speed");
            }
            
            float predictedDistance = speed * totalAirTime;

            return (predictedDistance, totalAirTime);
        }

        // public float GetVerticalVelocity()
        // {
        //     return _velocity.y;
        // }

        public void HandleDeath()
        {
            if (!isDead)
            {
                isDead = true;
                // Immediately trigger camera transition
                FindFirstObjectByType<NoiseMovement>()?.TransitionToDeathView();
                // Disable input
                // For legacy input, just disable the controller
                this.enabled = false;
                GetComponent<PlayerAnimatorController>().ResetAnimationStates();

                // Only zero out horizontal velocity, keep vertical for falling
                _velocity.x = 0;
                _velocity.z = 0;

                // Retrieve the final distance directly from GameManager.
                float finalDistance = GameManager.Instance.CurrentDistance;

                // Show ad through GameAdsManager
                GameAdsManager.Instance?.OnPlayerDeath(() =>
                {
                    GameManager.Instance.HandlePlayerDeath(finalDistance);
                });
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == 19)  // DeathDetector layer
            {
                HandleDeath();
            }
        }

        // Add this method to be called from JumpPad
        public void OnJumpPadActivated()
        {
            isOnJumpPad = true;
            jumpPadTimer = JUMP_PAD_DURATION;
            isChargingJump = false;

            // Tell the animator to trigger a jump but with different parameters
            var animator = GetComponent<PlayerAnimatorController>();
            if (animator != null)
            {
                animator.TriggerJumpAnimation(_velocity.y);
            }
            GameManager.Instance.ActivateJumpPadAudioEffect(JUMP_PAD_DURATION, 0.5f);
        }

        public void Die()
        {
            if (!isDead)
            {
                isDead = true;
                // Immediately trigger camera transition
                FindFirstObjectByType<NoiseMovement>()?.TransitionToDeathView();
                // Disable input
                this.enabled = false;
                GetComponent<PlayerAnimatorController>().ResetAnimationStates();

                // Only zero out horizontal velocity, keep vertical for falling
                _velocity.x = 0;
                _velocity.z = 0;

                // Retrieve the final distance directly from GameManager.
                float finalDistance = GameManager.Instance.CurrentDistance;

                // Show ad through GameAdsManager
                GameAdsManager.Instance?.OnPlayerDeath(() =>
                {
                    GameManager.Instance.HandlePlayerDeath(finalDistance);
                });
            }
        }

        private IEnumerator UpdateDashVisual()
        {
            // Calculate timing based on main duration
            fadeInTime = dashDuration * fadeInPortion;
            fadeOutTime = dashDuration * fadeOutPortion;
            fullEffectTime = dashDuration - fadeInTime - fadeOutTime;

            float fadeOutStart = fadeInTime + fullEffectTime;
            float elapsed = 0f;

            while (elapsed < dashDuration && isDashing)
            {
                elapsed += Time.deltaTime;

                if (elapsed < fadeInTime)
                {
                    // Fade in
                    dashLerp = Mathf.SmoothStep(0f, 1f, elapsed / fadeInTime);
                }
                else if (elapsed < fadeOutStart)
                {
                    // Full effect
                    dashLerp = 1f;
                }
                else
                {
                    // Fade out
                    float fadeOutElapsed = elapsed - fadeOutStart;
                    dashLerp = Mathf.SmoothStep(1f, 0f, fadeOutElapsed / fadeOutTime);
                }

                // Update visuals
                if (dashMaterial != null)
                {
                    dashMaterial.SetFloat(dashLerpID, dashLerp);
                    dashMaterial.SetFloat(additionalShaderParamID, dashLerp);
                }
                if (secondaryDashMaterial != null)
                {
                    secondaryDashMaterial.SetFloat(dashLerpID, dashLerp);
                    secondaryDashMaterial.SetFloat(additionalShaderParamID, dashLerp);
                }
                if (tertiaryDashMaterial != null)
                {
                    tertiaryDashMaterial.SetFloat(dashLerpID, dashLerp);
                    tertiaryDashMaterial.SetFloat(additionalShaderParamID, dashLerp);
                }
                if (playerAnimator != null) playerAnimator.SetDashLayerWeight(dashLerp);

                yield return null;
            }

            // Final reset
            dashLerp = 0f;
            if (dashMaterial != null)
            {
                dashMaterial.SetFloat(dashLerpID, dashLerp);
                dashMaterial.SetFloat(additionalShaderParamID, dashLerp);
            }
            if (secondaryDashMaterial != null)
            {
                secondaryDashMaterial.SetFloat(dashLerpID, dashLerp);
                secondaryDashMaterial.SetFloat(additionalShaderParamID, dashLerp);
            }
            if (tertiaryDashMaterial != null)
            {
                tertiaryDashMaterial.SetFloat(dashLerpID, dashLerp);
                tertiaryDashMaterial.SetFloat(additionalShaderParamID, dashLerp);
            }
            if (playerAnimator != null) playerAnimator.SetDashLayerWeight(dashLerp);

            // Clean up effect
            if (activeDashEffect != null)
            {
                Destroy(activeDashEffect, 0.5f); // Optional delay for effect to finish
                activeDashEffect = null;
            }
        }

        void ShowDashHint()
        {
            // Use the GameMessageDisplay system to show the dash hint
            if (GameMessageDisplay.Instance != null)
            {
                // Show the dash hint using the message ID system
                GameMessageDisplay.Instance.ShowMessageByID("1ST_BONUS_DASH_INFO", dashTridotCost);
                Debug.Log($"[DASH] Showing dash hint message with ID: 1ST_BONUS_DASH_INFO (ONE TIME ONLY)");
            }
            else
            {
                // Fallback to Debug.Log if message system isn't available
                Debug.Log($"[DASH] Hint: Press Jump in mid-air to Dash (costs {dashTridotCost} tridots)");
            }
        }

        #region Input Action Logic

        private void SetupInputActions()
        {
            InputActionManager.Instance.OnJumpPressed.AddListener(OnJumpPressed);
            InputActionManager.Instance.OnDoubleJumpPressedActivated.AddListener(OnDashActivated);
            InputActionManager.Instance.OnJumpHeldStarted.AddListener(OnJumpHeldStarted);
            InputActionManager.Instance.OnJumpHeldUpdate.AddListener(OnJumpHeldUpdate);
            InputActionManager.Instance.OnJumpReleased.AddListener(OnJumpReleased);

        }

        private void TearDownInputActions()
        {
            InputActionManager.Instance.OnJumpPressed.RemoveListener(OnJumpPressed);
            InputActionManager.Instance.OnDoubleJumpPressedActivated.RemoveListener(OnDashActivated);
            InputActionManager.Instance.OnJumpHeldStarted.RemoveListener(OnJumpHeldStarted);
            InputActionManager.Instance.OnJumpHeldUpdate.RemoveListener(OnJumpHeldUpdate);
            InputActionManager.Instance.OnJumpReleased.RemoveListener(OnJumpReleased);
        }

        private void OnJumpPressed()
        {
            if(GameManager.GamesState != GameStates.Playing)
            {
                return;
            }
            if (isOnJumpPad || !IsGrounded)
            {
                return;
            }

            if (!wasInAir)
            {
                // Just took off
                jumpStartPosition = transform.position;
                // Fire jump event
                onJump.Invoke();
            }


                // Reset gravity when grounded
                GameManager.Instance.ResetGravity();

            _velocity.y = jumpForce;
            colorEffects?.StartSlowdownEffect();
            GameManager.Instance.IncreaseGravity();
            // Ensure we complete the jump animation
            var animator = GetComponent<PlayerAnimatorController>();
            if (animator != null)
            {
                animator.TriggerJumpAnimation(jumpForce);
            }

            isChargingJump = true;
            currentChargedJumpForce = jumpForce;
            colorEffects?.StartSlowdownEffect();
            jumpForce += jumpForceGrowthRate * Time.deltaTime;
        }

        /// <summary>
        /// Handle the dash input action can only be activated after a normal jump
        /// </summary>
        private void OnDashActivated()
        {
            if (GameManager.GamesState != GameStates.Playing)
            {
                return;
            }

            HandleDashInput();
        }

        private void OnJumpReleased()
        {  
            // If we were charging and started the charge grounded, apply the jump
            if (isChargingJump && IsGrounded)
            {
                _velocity.y = currentChargedJumpForce;
                GameManager.Instance.IncreaseGravity();
                // Ensure we complete the charged jump animation
                var animator = GetComponent<PlayerAnimatorController>();
                if (animator != null)
                {
                    animator.TriggerJumpAnimation(currentChargedJumpForce);
                }
            }
            // Apply jump cut if in air and moving upward
            if (!IsGrounded && _velocity.y > 0)
            {
                _velocity.y *= jumpCutFactor;
            }

            isChargingJump = false;
        }

        private void OnJumpHeldUpdate()
        {
            if (GameManager.GamesState != GameStates.Playing)
            {
                return;
            }
            if(isChargingJump)
            currentChargedJumpForce = Mathf.Min(currentChargedJumpForce + jumpChargeRate * Time.deltaTime, maxJumpForce);
        }

        private void OnJumpHeldStarted()
        {
            if (GameManager.GamesState != GameStates.Playing)
            {
                return;
            }

            OnJumpHeldUpdate();
        }


        #endregion // Input Action Logic
    }
}