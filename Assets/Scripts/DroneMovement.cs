using UnityEngine;
using RoofTops;
using System.Collections;
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
    public Vector3 exitVector = new Vector3(0, 5, -10);
    public float exitSpeed = 10f;
    public EasingOption exitEasing = EasingOption.Out;
    public float exitMovementDuration = 1.0f;
    public float exitAnimDuration = 1.0f; // Duration of the exit animation before destruction
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
    [Header("Drop Control")]
    [Tooltip("Time before the drone exits automatically")]
    public float exitTime = 10f;
    [Tooltip("Random variation in exit time (±)")]
    public float exitTimeVariation = 2f;
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
    private float turnRate = 0f; // Initialize turnRate to 0
    private float currentSpeed = 0f; // Holds the current movement speed
    private bool exitTweenStarted = false;
    private Vector3 exitStartPos;
    private PlayerController playerController;
    private Vector3 dashBasePos; // Stores the drone's position when dash starts
    private bool dashBaseRecorded = false; // Flag to check if dash base position was recorded
    private float dashTimer = 0f; // Added for the new dash logic
    private float dashDuration = 1f; // Added for the new dash logic
    private float scheduledExitTime; // When the drone should exit
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
        // Schedule the exit time with some random variation
        float randomVariation = Random.Range(-exitTimeVariation, exitTimeVariation);
        scheduledExitTime = Time.time + exitTime + randomVariation;
        Debug.Log($"[DRONE EXIT DEBUG] Drone will exit automatically in {exitTime + randomVariation} seconds");
        // Start the exit timer
        StartCoroutine(ExitTimer());
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
        // The dash response is handled in FixedUpdate via HandleDashBehavior
        // Maintain altitude safety
        AltitudeSafetyCheck();
    }
    private void HandleExitMovement()
    {
        float exitTimeElapsed = Time.time - exitStartTime;
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
                Debug.Log("Disabled HookDropper during exit");
            }
        }
        // Move along exit vector
        transform.position = Vector3.Lerp(exitStartPos, exitStartPos + exitVector * 50f, exitTimeElapsed / exitMovementDuration);
        // Optional: Add some rotation during exit
        transform.Rotate(Vector3.up * exitTimeElapsed * 180f, Space.World);
        // We no longer destroy the drone here; instead, it will be destroyed by ExitAndDestroy coroutine
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
    public void TriggerExitAfterDelay(float delay)
    {
        Debug.Log($"[DRONE EXIT DEBUG] TriggerExitAfterDelay called with delay: {delay}");
        if (delay <= 0)
        {
            // Trigger exit immediately
            Debug.Log("[DRONE EXIT DEBUG] Triggering exit immediately");
            enableExitBehavior = true;
            // Also start the exit destruction coroutine
            StartCoroutine(ExitAndDestroy());
        }
        else
        {
            // Schedule exit after delay
            Debug.Log($"[DRONE EXIT DEBUG] Scheduling exit after {delay} seconds");
            Invoke("TriggerExit", delay);
        }
        // Make the exit more dramatic with random values
        exitVector = new Vector3(Random.Range(-5f, 5f), Random.Range(10f, 15f), Random.Range(-15f, -5f));
        exitSpeed = Random.Range(15f, 25f);
    }
    public void TriggerExit()
    {
        if (!enableExitBehavior)
        {
            enableExitBehavior = true;
            exitStartPos = transform.position;
            exitStartTime = Time.time;
            exitTweenStarted = false;
            // Log exit trigger
            Debug.Log("Drone exit triggered");
            // Start coroutine to wait for exit animation duration and then destroy the drone
            StartCoroutine(ExitAndDestroy());
        }
    }
    private IEnumerator ExitAndDestroy()
    {
        // Wait for the exit animation to complete
        yield return new WaitForSeconds(exitAnimDuration);
        Debug.Log("Exit animation completed. Destroying drone.");
        Destroy(gameObject);
    }
    private void FixedUpdate()
    {
        // DASH LOGIC: When the player is dashing, simulate the dash effect on the drone.
        if (playerController != null && playerController.IsDashing)
        {
            HandleDashBehavior();
        }
        else
        {
            // Not dashing—reset dash state.
            dashBaseRecorded = false;
        }
    }
    private void HandleDashBehavior()
    {
        // If already exiting, don't process dash hit logic again
        if (enableExitBehavior)
            return;
            
        // Compute vector from the player to the drone.
        Vector3 relative = transform.position - playerController.transform.position;
        float dot = Vector3.Dot(relative.normalized, playerController.transform.forward);
        // Thresholds: if the drone is very much in front (dot > 0.8) and within 5 units, it is "hit".
        float thresholdDot = 0.8f;
        float thresholdDistance = 5f;
        if (dot > thresholdDot && relative.magnitude < thresholdDistance)
        {
            // The drone is in the danger zone – instantiate the destruction particle effect if assigned.
            if (destructionParticleEffect != null)
            {
                GameObject effect = Instantiate(destructionParticleEffect, transform.position, transform.rotation);
                effect.transform.parent = null; // Ensure effect stays in world space
            }
            
            // Instead of destroying, trigger exit behavior
            DroneCollisionHandler collisionHandler = GetComponent<DroneCollisionHandler>();
            if (collisionHandler != null)
            {
                collisionHandler.TriggerExitAndDestroy();
            }
            else
            {
                // If no collision handler, just trigger exit
                TriggerExit();
            }
            return;
        }
        else
        {
            // Record the drone's base position once when dash starts.
            if (!dashBaseRecorded)
            {
                dashBasePos = transform.position;
                dashBaseRecorded = true;
                dashTimer = 0f; // Reset dash timer at the start.
            }

            dashTimer += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(dashTimer / dashDuration);
            
            // The offset is constant during the dash
            float currentOffset = playerController.dashSpeedMultiplier;
            
            // Move consistently in world space -Z direction with the same speed
            Vector3 newPos = dashBasePos + (Vector3.back * currentOffset);
            transform.position = newPos;
        }
    }
    private void AltitudeSafetyCheck()
    {
        if (!enableExitBehavior)
        {
            // Cast a ray from 10 units above the drone to detect the ground.
            Vector3 rayOrigin = transform.position + Vector3.up * 10f;
            RaycastHit hit;
            if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 20f))
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
        // When this drone is destroyed, only destroy the HookDropper on this drone
        HookDropper dropper = GetComponentInChildren<HookDropper>();
        if (dropper != null)
        {
            Debug.Log("Destroying HookDropper on this drone");
            Destroy(dropper.gameObject);
        }
        // Log that this drone was destroyed
        Debug.Log("Drone was destroyed: " + gameObject.name);
    }
    // Simple timer to trigger exit after a set time
    private IEnumerator ExitTimer()
    {
        // Wait until the scheduled exit time
        yield return new WaitForSeconds(exitTime + Random.Range(-exitTimeVariation, exitTimeVariation));
        // Only exit if we haven't already started exiting
        if (!enableExitBehavior)
        {
            Debug.Log("[DRONE EXIT DEBUG] Exit timer completed, triggering exit");
            TriggerExit();
        }
    }
}