using UnityEngine;
using RoofTops;

public class ProceduralFootIK : MonoBehaviour
{
    public Animator animator;
    public LayerMask groundLayers;
    public float distanceToGround = 0.05f;
    public float maxStepHeight = 0.5f;        // Maximum step height for stability
    public float raycastDistance = 1.5f;      // Increased raycast distance
    public bool forceFullWeight = false;      // Option to force full weight if animations don't have params
    
    // Names of the animation curve parameters
    public string leftFootWeightParam = "IKLeftFootWeight";
    public string rightFootWeightParam = "IKRightFootWeight";
    
    // Store previous positions for smoothing
    private Vector3 lastLeftFootPosition;
    private Vector3 lastRightFootPosition;
    private bool initialized = false;
    
    void Start()
    {
        if (!animator) animator = GetComponent<Animator>();
    }
    
    void OnAnimatorIK(int layerIndex)
    {
        if (!animator) return;
        
        if (!initialized)
        {
            // Initialize foot positions on first update
            lastLeftFootPosition = animator.GetIKPosition(AvatarIKGoal.LeftFoot);
            lastRightFootPosition = animator.GetIKPosition(AvatarIKGoal.RightFoot);
            initialized = true;
        }
        
        // Get weights - handle case where animation parameters might not exist
        float leftFootWeight, rightFootWeight;
        
        if (forceFullWeight)
        {
            leftFootWeight = rightFootWeight = 1f;
        }
        else
        {
            // Try to get animation parameters, default to 0 if they don't exist
            try
            {
                leftFootWeight = animator.GetFloat(leftFootWeightParam);
                rightFootWeight = animator.GetFloat(rightFootWeightParam);
            }
            catch
            {
                // If parameters don't exist, use defaults
                leftFootWeight = rightFootWeight = 1f;
            }
        }
        
        // Apply weights
        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, leftFootWeight);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, leftFootWeight);
        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, rightFootWeight);
        animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, rightFootWeight);
        
        // Process feet
        AdjustFoot(AvatarIKGoal.LeftFoot, ref lastLeftFootPosition);
        AdjustFoot(AvatarIKGoal.RightFoot, ref lastRightFootPosition);
    }
    
    void AdjustFoot(AvatarIKGoal foot, ref Vector3 lastPosition)
    {
        // Get position
        Vector3 footPos = animator.GetIKPosition(foot);
        
        // Start raycast from a higher position for better ground detection
        Vector3 rayStart = footPos + Vector3.up * raycastDistance * 0.5f;
        
        // Raycast
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, raycastDistance, groundLayers))
        {
            // Get target position
            Vector3 targetPos = hit.point;
            targetPos.y += distanceToGround;
            
            // Check if the step is too high (for stability)
            float stepHeight = Mathf.Abs(targetPos.y - lastPosition.y);
            if (stepHeight > maxStepHeight)
            {
                // Limit the step height for stability
                float direction = (targetPos.y > lastPosition.y) ? 1f : -1f;
                targetPos.y = lastPosition.y + (direction * maxStepHeight);
            }
            
            // Smooth the transition for stability
            targetPos = Vector3.Lerp(lastPosition, targetPos, Time.deltaTime * 15f);
            
            // Apply IK
            animator.SetIKPosition(foot, targetPos);
            
            // Update last position
            lastPosition = targetPos;
            
            // Rotation - preserve x rotation for stability
            Quaternion currentRotation = animator.GetIKRotation(foot);
            Quaternion hitRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            
            // Blend the rotations
            Vector3 currentEuler = currentRotation.eulerAngles;
            Vector3 targetEuler = (hitRotation * Quaternion.Euler(0, currentEuler.y, 0)).eulerAngles;
            
            // Create final rotation without affecting X (prevents tilting)
            Quaternion finalRotation = Quaternion.Euler(currentEuler.x, targetEuler.y, targetEuler.z);
            animator.SetIKRotation(foot, finalRotation);
        }
        else
        {
            // If no ground found, use the last position for stability
            lastPosition = footPos;
        }
    }
    
    // Keep empty events for compatibility
    public void LeftFootStep() { }
    public void RightFootStep() { }
    public void FootL() { }
    public void FootR() { }
    public void PlayLeftFootstep() { }
    public void PlayRightFootstep() { }
}