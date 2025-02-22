using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

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

        [Header("Jump Tracking")]
        public float simulationTimeStep = 0.1f;
        public float simulationDuration = 2f;

        [Header("Animation Sync")]
        public float baseAnimationSpeed = 6f;

        [Header("Speed Sync")]
        public float baseMoveSpeed = 6f;

        [Header("Charge Jump State")]
        public bool isChargingJump { get; private set; }
        public float currentChargedJumpForce { get; private set; }
        private bool holdingJump = false;  // Track if jump button has been held since last jump

        private float jumpStartTime;
        private float predictedFlightTime;
        private bool isDead = false;

        private CharacterController cc;
        //public bool isVaulting = false;
        public ModulePool modulePool;
        private Vector3 _velocity = Vector3.zero;
        private PlayerColorEffects colorEffects;

        [Header("Jump Pad State")]
        private bool isOnJumpPad = false;
        private float jumpPadTimer = 0f;
        private const float JUMP_PAD_DURATION = 1.5f;  // Match this with JumpPad's effectDuration

        [Header("Ground Detection")]
        public float groundCheckRadius = 0.5f;
        public float groundCheckDistance = 0.2f;
        public LayerMask groundLayer;  // Set this in inspector to include ground layers

        public bool IsGroundedOnCollider
        {
            get
            {
                if (cc.isGrounded) return true;

                // Additional sphere cast check
                Vector3 origin = transform.position + Vector3.up * 0.1f; // Slight offset up
                return Physics.SphereCast(origin, groundCheckRadius, Vector3.down, 
                    out RaycastHit hit, groundCheckDistance, groundLayer);
            }
        }
        public bool IsGroundedOnTrigger { get { return cc != null && cc.isGrounded; } }

        public bool IsDead()
        {
            return isDead;
        }

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            
            var meshObject = GetComponentInChildren<MeshRenderer>()?.gameObject;
            if (meshObject != null)
            {
                colorEffects = meshObject.GetComponent<PlayerColorEffects>();
            }
            
            if (GetComponent<Rigidbody>() != null || 
                GetComponent<ParkourController>() != null || 
                GetComponent<SimpleParkourVault>() != null)
            {
                // No debug logs here
            }
            
            if (modulePool == null)
            {
                modulePool = ModulePool.Instance;
            }
        }

        void Update()
        {
            //if (isVaulting || GameManager.Instance.IsPaused)
            //    return;

            // Always handle jump input, even before game starts
            HandleJumpInput();

            if ((transform.position.y < -7f || isDead) && modulePool.gameSpeed > 0)
            {
                modulePool?.SetMovement(false);
                DeathMessageDisplay.Instance?.ShowMessage();
                FindFirstObjectByType<DistanceTracker>()?.SaveDistance();
                StartCoroutine(DelayedReset());
            }
            
            if (isDead)
            {
                if (Input.GetButtonDown("Jump"))
                {
                    StartCoroutine(DelayedReset());
                }
                
                if (!cc.isGrounded)
                {
                    _velocity.y += Physics.gravity.y * Time.deltaTime;
                }
                Vector3 deathMove = new Vector3(0, _velocity.y, 0);
                cc.Move(deathMove * Time.deltaTime);
                return;
            }
            
            jumpForce += jumpForceGrowthRate * Time.deltaTime;
            
            if (modulePool != null)
            {
                runSpeedMultiplier = modulePool.gameSpeed / GameManager.Instance.normalGameSpeed;
            }
            
            if (cc.isGrounded) HandleLanding();
            
            if (!cc.isGrounded)
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
        }

        void HandleJumpInput()
        {
            if (isOnJumpPad || !InputManager.Exists()) return;
            
            bool jumpTapped = InputManager.Instance.isJumpPressed;
            bool jumpHeld = InputManager.Instance.isJumpHeld;
            bool jumpReleased = InputManager.Instance.isJumpReleased;

            Debug.Log($"Input states - Tapped: {jumpTapped}, Held: {jumpHeld}, Released: {jumpReleased}");

            if (cc.isGrounded)
            {
                // Reset gravity when grounded
                GameManager.Instance.ResetGravity();
                
                // Handle tap jump - only if not holding from previous jump
                if (jumpTapped && !holdingJump)
                {
                    _velocity.y = jumpForce;
                    holdingJump = true;
                    colorEffects?.StartSlowdownEffect();
                    GameManager.Instance.IncreaseGravity();
                    // Ensure we complete the jump animation
                    var animator = GetComponent<PlayerAnimatorController>();
                    if (animator != null)
                    {
                        animator.TriggerJumpAnimation(jumpForce);
                    }
                }
                // Handle charge jump start
                else if (jumpTapped && !isChargingJump)
                {
                    isChargingJump = true;
                    holdingJump = true;
                    currentChargedJumpForce = jumpForce;
                    colorEffects?.StartSlowdownEffect();
                }
                // Handle charging
                else if (jumpHeld && isChargingJump)
                {
                    currentChargedJumpForce = Mathf.Min(currentChargedJumpForce + jumpChargeRate * Time.deltaTime, maxJumpForce);
                }
            }

            // Handle jump release
            if (jumpReleased)
            {
                // If we were charging and started the charge grounded, apply the jump
                if (isChargingJump && cc.isGrounded)
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
                if (!cc.isGrounded && _velocity.y > 0)
                {
                    _velocity.y *= jumpCutFactor;
                }
                
                isChargingJump = false;
                holdingJump = false;
            }
        }

        void HandleLanding()
        {
            isChargingJump = false;
            holdingJump = false;
        }

        void CheckFallingState()
        {
            bool isFalling = _velocity.y < 0 && WillFall();
        }

        void ComputePredictedFlightTime()
        {
            float verticalVelocity = _velocity.y;
            float gravity = Physics.gravity.y;
            predictedFlightTime = verticalVelocity > 0 ? (verticalVelocity / -gravity) * 2 : 0f;
        }

        bool WillFall()
        {
            Vector3 position = transform.position;
            Vector3 velocity = _velocity;
            Vector3 gravity = Physics.gravity;

            for (float t = 0; t < simulationDuration; t += simulationTimeStep)
            {
                Vector3 predictedPos = position + velocity * t + 0.5f * gravity * t * t;
                
                // Check for ground tags in predicted path
                if (Physics.Raycast(predictedPos, Vector3.down, out RaycastHit hit, 1f))
                {
                    if (hit.collider.CompareTag("GroundCollider")) 
                    {
                        return false; // Ground found in path - won't fall
                    }
                }
            }
            return true; // No ground detected in path - will fall
        }

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
        void UpdateColliderState()
        {
            // No changes made. The CapsuleCollider remains enabled.
        }

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
            float predictedDistance = modulePool.gameSpeed * totalAirTime;
            
            return (predictedDistance, totalAirTime);
        }

        public float GetVerticalVelocity()
        {
            return _velocity.y;
        }

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
                modulePool?.StopMovement();  // Use the new method instead of trying to set gameSpeed directly
                
                // Only zero out horizontal velocity, keep vertical for falling
                _velocity.x = 0;
                _velocity.z = 0;
                
                GetComponent<PlayerAnimatorController>().TriggerFallAnimation();
                
                // Retrieve the final distance directly from GameManager.
                float finalDistance = GameManager.Instance.CurrentDistance;
                
                // Show ad through GameAdsManager
                GameAdsManager.Instance?.OnPlayerDeath(() => {
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
            holdingJump = false;
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
                GetComponent<PlayerAnimatorController>().ResetAnimationStates();
                // ... rest of death handling
            }
        }
    }
} 