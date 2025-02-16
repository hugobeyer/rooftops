using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator), typeof(PlayerController))]
public class PlayerAnimatorController : MonoBehaviour
{
    private Animator animator;
    private PlayerController playerController;
    private bool canJumpTrigger = true;
    private bool canFallTrigger = true;
    private bool wasGrounded;

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
    private readonly int spineLayerIndex = 2;  // The index of your Spine layer (0 is Base, 1 is Arms)
    private readonly int spineKnockTriggerHash = Animator.StringToHash("spineKnockTrigger");
    private readonly int fallLayerIndex = 3;  // Fall layer index

    [Header("Animation Timing")]
    public float armAnimationDelay = 1f;  // Time to wait before starting arm animation

    private float jumpStartHeight;
    private float maxJumpHeight;

    void Awake()
    {
        animator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
        
        // Initialize the Spine layer with 0 weight
        animator.SetLayerWeight(spineLayerIndex, 0);
    }

    void Update()
    {
        if (GameManager.Instance.IsPaused) return;
        
        bool isGroundedNow = playerController.IsGroundedOnCollider;
        
        // Track max height during jump
        if (!isGroundedNow)
        {
            maxJumpHeight = Mathf.Max(maxJumpHeight, transform.position.y);
        }
        
        HandleJumpTrigger();
        UpdateRunSpeed();
        UpdateGroundedState();
        HandleLandingImpact();
        
        wasGrounded = isGroundedNow;  // Store for next frame
    }

    void HandleJumpTrigger()
    {
        if (Input.GetButtonDown("Jump") && playerController.IsGroundedOnCollider && canJumpTrigger)
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
            
            // Start coroutine to wait for peak of jump
            StartCoroutine(WaitForJumpPeakThenAnimate());
            
            canJumpTrigger = false;
        }
        
        if (!playerController.IsGroundedOnCollider)
        {
            canJumpTrigger = true;
        }
    }

    private IEnumerator WaitForJumpPeakThenAnimate()
    {
        // Wait configurable time before starting arm animation
        yield return new WaitForSeconds(armAnimationDelay);
        
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
            float spineWeight = Mathf.Clamp01(jumpHeight / 9f);  // 3 units height for full effect
            
            if (jumpHeight > 1.5f)  // Only trigger for jumps higher than 0.5 units
            {
                animator.SetLayerWeight(spineLayerIndex, spineWeight);
                animator.SetTrigger(spineKnockTriggerHash);
            }
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
        
        // Start coroutine to wait for peak of jump
        StartCoroutine(WaitForJumpPeakThenAnimate());
    }
} 