using UnityEngine;
using RoofTops;
using System.Collections;
using DG.Tweening;  // Add this at the top with other using statements
namespace RoofTops
{
    public enum EasingOption
    {
        In,
        Out,
        InOut
    }
}
public class DroneMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float hoverSpeed = 2.0f;
    public float hoverAmplitude = 0.3f;
    public float wobbleSpeed = 3.0f;
    public float wobbleAmount = 5.0f;
    [Header("Position Constraints")]
    public float minHeightAboveGround = 1.0f;
    public float maxHeightAboveGround = 2.0f;
    public float forwardDistanceFromPlayer = 6.0f;
    [Header("Altitude Limit")]
    public float maxAltitude = 20f;
    [Header("Initial Position")]
    public Vector3 initialPositionOffset = new Vector3(0, 2, 10);
    public float initialMoveSpeed = 3.0f;
    [Header("Arc Movement")]
    public float arcHeight = 2.0f;
    public Vector3 arcControlPoint = new Vector3(0, 3, 5);
    [Header("Banking")]
    public float maxBankAngle = 15f;
    public float bankSpeed = 1.5f; // How quickly the drone banks
    public float minMovementForBanking = 0.01f; // Minimum movement speed to trigger banking
    public float maxSpeedForBanking = 5f; // Speed at which full bank is applied
    [Header("Exit Behavior")]
    public bool enableExitBehavior = false;
    [Tooltip("Direction the drone will fly when exiting")]
    public Vector3 exitVector = new Vector3(0, 5, -10);
    [Tooltip("How long the exit animation plays before the drone is destroyed")]
    public float exitDuration = 2.0f;
    [Tooltip("Easing type for the exit movement")]
    public EasingOption exitEasing = EasingOption.Out;
    
    [Header("Initial Movement")]
    public bool turnAroundDuringInitialMove = true;
    public float initialRotationSpeed = 2.0f;
    public EasingOption initialEasing = EasingOption.InOut;
    [Header("Visual Effect")]
    public Transform droneVisual;
    private Vector3 visualStartLocalPosition;
    [Header("Altitude Safety")]
    [Tooltip("Minimum clearance above the detected surface.")]
    public float altitudeMargin = 2.0f;
    [Header("Effects")]
    public GameObject destructionParticleEffect;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool isInitialMove = true; // Flag for initial movement
    private float moveStartTime;
    private float moveDuration;
    private Vector3 moveStartPosition;
    private Quaternion initialRotation;
    private Quaternion targetInitialRotation;
    private float hoverTime = 0f;
    private bool isExiting = false;
    private float exitStartTime = 0f;
    private bool exitTweenStarted = false;
    private Vector3 exitStartPos;
    private PlayerController playerController;
    private Vector3 dashBasePos; // Stores the drone's position when dash starts
    private bool dashBaseRecorded = false; // Flag to check if dash base position was recorded
    private float dashTimer = 0f; // Added for the new dash logic
    private float dashDuration = 1f; // Added for the new dash logic
    void Start()
    {
        // Make sure exit behavior is disabled at start
        isExiting = false;
        enableExitBehavior = false;
        transform.localPosition = initialPositionOffset;
        if (droneVisual != null)
        {
            visualStartLocalPosition = droneVisual.localPosition;
        }
        initialRotation = transform.rotation;
        targetInitialRotation = initialRotation * Quaternion.Euler(0, 180, 0);
        targetPosition = CalculateTargetPosition();
        moveStartPosition = transform.localPosition;
        moveStartTime = Time.time;
        float distance = Vector3.Distance(moveStartPosition, targetPosition);
        moveDuration = Mathf.Max(distance / initialMoveSpeed, 0.5f);
        targetRotation = transform.rotation;
        
        // Find the PlayerController if not already assigned.
        if (playerController == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerController = playerObj.GetComponent<PlayerController>();
            }
        }
    }
    void Update()
    {
        // Handle initial movement if needed
        if (isInitialMove)
        {
            HandleInitialMovement();
            return;
        }
        // Handle exit behavior if enabled
        if (enableExitBehavior)
        {
            HandleExitMovement();
            return;
        }
        // Apply hover and wobble effects
        ApplyHoverAndWobble();
        // Maintain altitude safety
        AltitudeSafetyCheck();
    }
    private void HandleExitMovement()
    {
        float exitTimeElapsed = Time.time - exitStartTime;
        float normalizedTime = Mathf.Clamp01(exitTimeElapsed / exitDuration);
        
        if (!exitTweenStarted)
        {
            exitTweenStarted = true;
            // Log exit start
            Debug.Log("Drone starting exit movement");
            
            // Disable any HookDropper component
            HookDropper dropper = GetComponentInChildren<HookDropper>();
            if (dropper != null)
            {
                dropper.enabled = false;
            }
            
            // Disable collisions during exit
            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }
        }
        
        // Apply easing to the movement for a more natural exit
        float easedTime = ApplyEasing(exitEasing, normalizedTime);
        
        // Move along exit vector with easing
        transform.position = Vector3.Lerp(exitStartPos, exitStartPos + exitVector * 50f, easedTime);
        
        // Add some rotation during exit
        transform.Rotate(Vector3.up * Time.deltaTime * 180f, Space.World);
    }
    private void HandleInitialMovement()
    {
        float elapsedTime = Time.time - moveStartTime;
        float normalizedTime = Mathf.Clamp01(elapsedTime / moveDuration);
        float curveValue = ApplyEasing(initialEasing, normalizedTime);
        Vector3 newPosition = CubicBezier(
        moveStartPosition,
        moveStartPosition + Vector3.up * arcHeight + Vector3.forward * 2,
        targetPosition + Vector3.up * arcHeight - Vector3.forward * 2,
        targetPosition,
        curveValue
        );
        // Clamp altitude
        newPosition.y = Mathf.Min(newPosition.y, maxAltitude);
        transform.localPosition = newPosition;
        if (turnAroundDuringInitialMove)
        {
            targetRotation = Quaternion.Slerp(initialRotation, targetInitialRotation, curveValue);
        }
        // Update rotation
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, initialRotationSpeed * Time.deltaTime);
        // Check if initial movement is complete
        if (normalizedTime >= 0.99f)
        {
            isInitialMove = false;
            targetRotation = targetInitialRotation;
        }
    }
    private void ApplyHoverAndWobble()
    {
        // Apply gentle wobble to rotation when hovering
        targetRotation = targetInitialRotation * Quaternion.Euler(
        Mathf.Sin(Time.time * wobbleSpeed * 0.5f) * wobbleAmount,
        0,
        Mathf.Sin(Time.time * wobbleSpeed) * wobbleAmount
        );
        // Update rotation
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, bankSpeed * Time.deltaTime);
        // Maintain proper altitude
        AltitudeSafetyCheck();
    }
    private float ApplyEasing(EasingOption easing, float t)
    {
        switch (easing)
        {
            case EasingOption.In:
                return t * t;
            case EasingOption.Out:
                return 1 - Mathf.Pow(1 - t, 2);
            case EasingOption.InOut:
                return t < 0.5f ? 2 * t * t : 1 - 2 * Mathf.Pow(1 - t, 2);
            default:
                return t;
        }
    }
    Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;
        Vector3 p = uuu * p0;
        p += 3 * uu * t * p1;
        p += 3 * u * tt * p2;
        p += ttt * p3;
        return p;
    }
    Vector3 CalculateTargetPosition()
    {
        float moduleHeight = GetCurrentModuleHeight();
        float newY = moduleHeight + Random.Range(minHeightAboveGround, maxHeightAboveGround);
        return new Vector3(0, newY, forwardDistanceFromPlayer);
    }
    float GetCurrentModuleHeight()
    {
        if (ModulePool.Instance != null)
        {
            return ModulePool.Instance.GetMaxModuleHeight();
        }
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 20f))
        {
            return hit.point.y;
        }
        return 0f;
    }
    public void TriggerExit()
    {
        if (!enableExitBehavior)
        {
            enableExitBehavior = true;
            exitStartPos = transform.position;
            exitStartTime = Time.time;
            exitTweenStarted = false;
            
            // Start coroutine to wait for exit animation duration and then destroy the drone
            StartCoroutine(ExitAndDestroy());
        }
    }
    private IEnumerator ExitAndDestroy()
    {
        // Wait for the exit animation to complete
        yield return new WaitForSeconds(exitDuration);
        
        // Destroy the drone
        Destroy(gameObject);
    }
    private void FixedUpdate()
    {
        // DASH LOGIC: When the player is dashing, simulate the dash effect on the drone.
        // ONLY respond to ACTUAL dashes, not dash attempts
        if (playerController != null && playerController.IsDashing)
        {
            HandleDashBehavior();
        }
        else
        {
            // Player is no longer dashing
            if (dashBaseRecorded)
            {
                // If we recorded a dash position but didn't hit the drone, return to original position
                // Use a faster lerp speed for quick return
                transform.position = Vector3.Lerp(transform.position, dashBasePos, Time.fixedDeltaTime * 10f);
                
                // If we're close enough to the original position, consider it returned
                if (Vector3.Distance(transform.position, dashBasePos) < 0.1f)
                {
                    dashBaseRecorded = false;
                }
            }
            else
            {
                // Not dashing and not returning—reset dash state.
                dashBaseRecorded = false;
            }
        }
    }
    private void HandleDashBehavior()
    {
        if (enableExitBehavior)
            return;

        Vector3 relative = transform.position - playerController.transform.position;
        float dot = Vector3.Dot(relative.normalized, playerController.transform.forward);

        float thresholdDot = 0.8f;
        float thresholdDistance = 5f;

        if (dot > thresholdDot && relative.magnitude < thresholdDistance)
        {
            if (destructionParticleEffect != null)
            {
                GameObject effect = Instantiate(destructionParticleEffect, transform.position, transform.rotation);
                effect.transform.parent = null;
            }

            Destroy(gameObject); // Immediately destroy the drone
            return;
        }

        if (!dashBaseRecorded)
        {
            dashBasePos = transform.position;
            dashBaseRecorded = true;

            float currentOffset = playerController.dashSpeedMultiplier;
            Vector3 dodgeOffset = new Vector3(0, currentOffset * 0.3f, -currentOffset);
            Vector3 targetPosition = dashBasePos + dodgeOffset;

            transform.DOKill();
            transform.DOMove(targetPosition, 0.2f).SetEase(Ease.OutQuad);
        }
    }
    private void AltitudeSafetyCheck()
    {
        if (!enableExitBehavior)
        {
            // Cast a ray from 10 units above the drone to detect the ground.
            Vector3 rayOrigin = transform.position + Vector3.up * 10f;
            RaycastHit hit;
            
            // Create a layer mask that only includes the Ground layer (layer 12)
            int groundLayerMask = 1 << 12; // Layer 12 is Ground
            
            // Use the layer mask in the raycast
            if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 20f, groundLayerMask))
            {
                // Calculate base altitude from the ground plus an altitude margin.
                float baseAltitude = hit.point.y + altitudeMargin;
                // Calculate a normalized height range for oscillation.
                float minHeight = baseAltitude;
                float maxHeight = baseAltitude + hoverAmplitude * 2f; // Double amplitude for full range
                                                                      // Use a normalized sine wave (0-1) and fit it to our height range.
                float normalizedOscillation = (Mathf.Sin(Time.time * hoverSpeed) + 1f) * 0.5f; // Convert -1,1 to 0,1
                float targetAltitude = Mathf.Lerp(minHeight, maxHeight, normalizedOscillation);
                // Smoothly adjust the drone's vertical position toward the target altitude.
                Vector3 targetPos = new Vector3(transform.position.x, targetAltitude, transform.position.z);
                transform.position = Vector3.Lerp(transform.position, targetPos, Time.fixedDeltaTime * 2f);
            }
        }
    }
    private void OnDestroy()
    {
        // Clean up any DOTween animations to prevent memory leaks
        transform.DOKill();
    }
}