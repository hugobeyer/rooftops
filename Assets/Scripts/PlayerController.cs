using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float jumpForce = 7f; // Upward force when jumping
    public float jumpForceGrowthRate = 0.1f; // Added growth rate parameter
    public float runSpeedMultiplier = 1f;
    [Header("Jump Settings")]
    public float jumpCutFactor = 0.5f; // Multiplier applied to upward velocity when jump is cut (released early)

    [Header("Animation")]
    public float runSpeed = 1f;  // Divided by 6 from original 1f

    [Header("Jump Tracking")]
    public Vector3 JumpStartPosition { get; private set; }
    public float LastJumpDistance { get; private set; }

    [Header("Fall Detection")]
    public float simulationTimeStep = 0.1f;
    public float simulationDuration = 2f;


    [Header("Animation Sync")]
    public float baseAnimationSpeed = 6f; // Should match ModulePool's baseMoveSpeed

    [Header("Speed Sync")]
    public float baseMoveSpeed = 6f;

    private float jumpStartTime;
    private float predictedFlightTime;
    private bool isDead = false;

    private CharacterController cc;
    public bool isVaulting = false; // Public flag – can be referenced by other scripts
    
    // Add a public ModulePool field so that you can assign it via the Inspector.
    public ModulePool modulePool;

    private Vector3 _velocity = Vector3.zero;

    // Added for backward compatibility.
    public bool IsGroundedOnCollider { get { return cc != null && cc.isGrounded; } }
    public bool IsGroundedOnTrigger { get { return cc != null && cc.isGrounded; } }

    private PlayerColorEffects colorEffects;

    public bool IsDead()
    {
        return isDead;
    }

    void Start()
    {
        cc = GetComponent<CharacterController>();
        
        // Find the color effects on the mesh object
        var meshObject = GetComponentInChildren<MeshRenderer>()?.gameObject;
        if (meshObject != null)
        {
            colorEffects = meshObject.GetComponent<PlayerColorEffects>();
        }
        
        // Check for conflicting components
        if (GetComponent<Rigidbody>() != null || 
            GetComponent<ParkourController>() != null || 
            GetComponent<SimpleParkourVault>() != null)
        {
            Debug.LogWarning("Found conflicting components (Rigidbody/ParkourController/SimpleParkourVault) on player!");
        }
        
        // Auto-assign modulePool if not set manually in the Inspector.
        if (modulePool == null)
        {
            modulePool = ModulePool.Instance;
        }
        
        JumpStartPosition = transform.position;
    }

    void Update()
    {
        // Don't process standard movement if vaulting is active.
        if (isVaulting)
            return;

        // Simple pause check – with CharacterController we just return on pause.
        if (GameManager.Instance.IsPaused)
        {
            return;
        }
        
        // Check for death by falling or wall hit
        if ((transform.position.y < -7f || isDead) && modulePool.currentMoveSpeed > 0)  // Check if still moving
        {
            modulePool?.SetMovement(false);
            DeathMessageDisplay.Instance?.ShowMessage();
            FindFirstObjectByType<DistanceTracker>()?.SaveDistance();
            StartCoroutine(DelayedReset());
        }
        
        // Check for restart input when dead
        if (isDead)
        {
            if (Input.GetButtonDown("Jump"))
            {
                StartCoroutine(DelayedReset());
            }
            
            // Still process gravity and movement when dead
            if (!cc.isGrounded)
            {
                _velocity.y += Physics.gravity.y * Time.deltaTime;
            }
            Vector3 deathMove = new Vector3(0, _velocity.y, 0);  // Renamed to deathMove
            cc.Move(deathMove * Time.deltaTime);
            return;  // Don't process other input while dead
        }
        
        jumpForce += jumpForceGrowthRate * Time.deltaTime;
        
        // Get speed from ModulePool
        if (modulePool != null)
        {
            // Direct m/s matching
            runSpeedMultiplier = modulePool.currentMoveSpeed / 6f;  // 6 m/s is our base animation speed
        }
        
        HandleJumpInput();

        if (cc.isGrounded) HandleLanding();
        
        // If the character is not grounded, apply gravity.
        if (!cc.isGrounded)
        {
            _velocity.y += Physics.gravity.y * Time.deltaTime;
        }
        else
        {
            // Optionally reset vertical velocity when grounded
            // _velocity.y = 0;
        }

        // Ensure the player remains at its horizontal position.
        _velocity.x = 0;
        _velocity.z = 0;
        // Only use the vertical component for movement.
        Vector3 verticalMove = new Vector3(0, _velocity.y, 0);
        cc.Move(verticalMove * Time.deltaTime);
        
        // Force the player's horizontal (X and Z) positions to remain fixed.
        Vector3 pos = transform.position;
        pos.x = 0;
        //pos.z = 0;
        transform.position = pos;
    }

    void HandleLanding()
    {
        LastJumpDistance = CalculateJumpDistance();
        JumpStartPosition = transform.position;
    }

    float CalculateJumpDistance()
    {
        return Vector3.Distance(
            new Vector3(0, 0, JumpStartPosition.z),
            new Vector3(0, 0, transform.position.z)
        );
    }

    void HandleJumpInput()
    {
        bool jumpPressed = Input.GetButtonDown("Jump");
        
        if (cc.isGrounded && jumpPressed)
        {
            _velocity.y = jumpForce;

            // Removed GameManager-based slowdown;
            // Simply trigger the color effect on jump.
            colorEffects?.StartSlowdownEffect();
        }
        
        if (Input.GetButtonUp("Jump") && _velocity.y > 0)
        {
            _velocity.y *= jumpCutFactor;
        }
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
        FindFirstObjectByType<CameraFollow>()?.ResetCamera();
        
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
    public void SetVaultingState(bool vaulting)
    {
        isVaulting = vaulting;
    }

    public (float distance, float airTime) PredictJumpTrajectory()
    {
        float timeToApex = jumpForce / -Physics.gravity.y;  // Time to reach highest point
        float totalAirTime = timeToApex * 2;  // Total time in air (up + down)
        
        // Distance covered = speed * time
        float predictedDistance = modulePool.currentMoveSpeed * totalAirTime;
        
        return (predictedDistance, totalAirTime);
    }

    public float GetVerticalVelocity()
    {
        return _velocity.y;
    }

    public void HandleDeath()
    {
        isDead = true;
        modulePool?.SetMovement(false);
        modulePool.currentMoveSpeed = 0;
        
        // Only zero out horizontal velocity, keep vertical for falling
        _velocity.x = 0;
        _velocity.z = 0;
        // Don't zero out _velocity.y so they can keep falling
        
        GetComponent<PlayerAnimatorController>().TriggerFallAnimation();
        
        DeathMessageDisplay.Instance?.ShowMessage();
        
        float finalDistance = DistanceTextDisplay.GetDistance();
        GameManager.Instance.gameData.lastRunDistance = finalDistance;
        if (finalDistance > GameManager.Instance.gameData.bestDistance)
        {
            GameManager.Instance.gameData.bestDistance = finalDistance;
        }
        
        FindFirstObjectByType<CameraFollow>()?.TransitionToDeathView();
        StartCoroutine(DelayedReset());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == 19)  // DeathDetector layer
        {
            HandleDeath();
        }
    }
} 