using UnityEngine;
using RoofTops;

namespace RoofTops {
    public enum EasingOption {
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
    public float maxHorizontalDistance = 2.0f;

    [Header("Altitude Limit")]
    public float maxAltitude = 20f;

    [Header("Initial Position")]
    public Vector3 initialPositionOffset = new Vector3(0, 2, 10);
    public float initialMoveSpeed = 3.0f;

    [Header("Arc Movement")]
    public float arcHeight = 2.0f;
    public Vector3 arcControlPoint = new Vector3(0, 3, 5);

    [Header("Movement Behavior")]
    public float repositionSpeed = 1.5f;
    public float randomMovementInterval = 3.0f;
    public float maxRepositionDistance = 1.0f;
    public float minMoveDuration = 0.5f;
    public AnimationCurve movementCurve = new AnimationCurve(
        new Keyframe(0, 0, 0, 0),
        new Keyframe(0.5f, 0.5f, 1, 1),
        new Keyframe(1, 1, 0, 0)
    );

    [Header("Banking")]
    public float maxBankAngle = 15f;
    public float bankSpeed = 1.5f;          // How quickly the drone banks
    public float minMovementForBanking = 0.01f;  // Minimum movement speed to trigger banking
    public float maxSpeedForBanking = 5f;   // Speed at which full bank is applied

    [Header("Exit Behavior")]
    public bool enableExitBehavior = false;
    public Vector3 exitVector = new Vector3(0, 5, -10);
    public float exitSpeed = 10f;
    public float exitDelay = 5f;
    public EasingOption exitEasing = EasingOption.Out;
    public float exitMovementDuration = 1.0f;

    [Header("Initial Movement")]
    public bool turnAroundDuringInitialMove = true;
    public float initialRotationSpeed = 2.0f;
    public EasingOption initialEasing = EasingOption.InOut;

    [Header("Visual Effect")]
    public Transform droneVisual;
    private Vector3 visualStartLocalPosition;

    private Vector3 targetPosition;
    private float nextPositionChangeTime;
    private Quaternion targetRotation;
    private bool isRepositioning = false;
    private Vector3 moveDirection;
    private bool isInitialMove = true;  // Flag for initial movement
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

    void Start()
    {
        transform.localPosition = initialPositionOffset;
        if (droneVisual != null)
        {
            visualStartLocalPosition = droneVisual.localPosition;
        }

        initialRotation = transform.rotation;
        targetInitialRotation = initialRotation * Quaternion.Euler(0, 180, 0);

        targetPosition = CalculateNewLocalTargetPosition();

        moveStartPosition = transform.localPosition;
        moveStartTime = Time.time;
        float distance = Vector3.Distance(moveStartPosition, targetPosition);
        moveDuration = Mathf.Max(distance / initialMoveSpeed, minMoveDuration);

        nextPositionChangeTime = Time.time + Random.Range(1.0f, randomMovementInterval);
        targetRotation = transform.rotation;

        moveDirection = targetPosition - transform.localPosition;
        isRepositioning = true;

        if (enableExitBehavior)
        {
            exitStartTime = Time.time + exitDelay;
        }
    }

    void Update()
    {
        if (enableExitBehavior && !isExiting && Time.time >= exitStartTime)
        {
            isExiting = true;
        }
        else if (!isExiting)
        {
            if (Time.time > nextPositionChangeTime && !isInitialMove)
            {
                Vector3 newTarget = CalculateNewLocalTargetPosition();

                Vector3 directionToTarget = newTarget - transform.localPosition;
                float distanceToTarget = directionToTarget.magnitude;

                if (distanceToTarget > maxRepositionDistance)
                {
                    directionToTarget = directionToTarget.normalized * maxRepositionDistance;
                    newTarget = transform.localPosition + directionToTarget;
                }

                moveStartPosition = transform.localPosition;
                moveStartTime = Time.time;
                float distance = Vector3.Distance(moveStartPosition, newTarget);
                moveDuration = Mathf.Max(distance / repositionSpeed, minMoveDuration);

                targetPosition = newTarget; nextPositionChangeTime = Time.time + Random.Range(randomMovementInterval * 0.8f, randomMovementInterval * 1.2f);
                moveDirection = targetPosition - transform.localPosition;
                isRepositioning = true;
            }

            if (isExiting)
            {
                if (!exitTweenStarted)
                {
                    exitTweenStarted = true;
                    exitStartPos = transform.localPosition;
                    Debug.Log("Drone exit animation started!");
                }
                float exitElapsed = Time.time - exitStartTime;
                float tExit = Mathf.Clamp01(exitElapsed / exitMovementDuration);
                float easedTExit = ApplyEasing(exitEasing, tExit);
                Vector3 targetExitPos = exitStartPos + exitVector.normalized * (exitSpeed * exitMovementDuration);
                transform.localPosition = Vector3.Lerp(exitStartPos, targetExitPos, easedTExit);
                
                Quaternion targetExitRotation = Quaternion.LookRotation(exitVector.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetExitRotation, bankSpeed * Time.deltaTime);
                return;
            }

            float elapsedTime = Time.time - moveStartTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / moveDuration);
            float curveValue = ApplyEasing(initialEasing, normalizedTime);

            Vector3 newPosition;
            if (isInitialMove)
            {
                newPosition = CubicBezier(
                    moveStartPosition,
                    moveStartPosition + Vector3.up * arcHeight + Vector3.forward * 2,
                    targetPosition + Vector3.up * arcHeight - Vector3.forward * 2,
                    targetPosition,
                    curveValue
                );

                if (turnAroundDuringInitialMove)
                {
                    targetRotation = Quaternion.Slerp(initialRotation, targetInitialRotation, curveValue);
                }
            }
            else
            {
                newPosition = Vector3.Lerp(moveStartPosition, targetPosition, curveValue);
            }

            Vector3 frameMovement = newPosition - transform.localPosition;
            float movementMagnitude = frameMovement.magnitude;
            currentSpeed = movementMagnitude; // Update current speed for banking

            // Clamp altitude: ensure the Y position does not exceed maxAltitude
            newPosition.y = Mathf.Min(newPosition.y, maxAltitude);
            transform.localPosition = newPosition;

            if (movementMagnitude > minMovementForBanking && (!isInitialMove || !turnAroundDuringInitialMove))
            {
                Vector3 normalizedMovement = frameMovement.normalized;

                turnRate = normalizedMovement.x;

                float speedFactor = Mathf.Clamp(currentSpeed / maxSpeedForBanking, 0f, 1f);
                float bankAngle = Mathf.Clamp(turnRate * maxBankAngle * speedFactor, -maxBankAngle, maxBankAngle);

                Quaternion bankRotation;
                if (!isInitialMove)
                {
                    bankRotation = targetInitialRotation * Quaternion.Euler(
                        Mathf.Sin(Time.time * wobbleSpeed * 0.5f) * wobbleAmount * 0.5f,
                        0,
                        bankAngle + (Mathf.Sin(Time.time * wobbleSpeed) * wobbleAmount * 0.3f)
                    );
                }
                else
                {
                    bankRotation = Quaternion.Euler(
                        Mathf.Sin(Time.time * wobbleSpeed * 0.5f) * wobbleAmount * 0.5f,
                        Mathf.Sin(Time.time * wobbleSpeed * 0.7f) * wobbleAmount * 0.5f,
                        bankAngle + (Mathf.Sin(Time.time * wobbleSpeed) * wobbleAmount * 0.3f)
                    );
                }

                if (!isInitialMove)
                {
                    targetRotation = bankRotation;
                }
            }
            else if (isRepositioning && normalizedTime >= 0.99f)
            {
                isRepositioning = false;

                if (isInitialMove)
                {
                    isInitialMove = false;

                    targetRotation = targetInitialRotation;
                }
                else
                {
                    targetRotation = targetInitialRotation * Quaternion.Euler(
                        Mathf.Sin(Time.time * wobbleSpeed * 0.5f) * wobbleAmount,
                        0,
                        Mathf.Sin(Time.time * wobbleSpeed) * wobbleAmount
                    );
                }
            }
            else if (!isRepositioning && !isInitialMove)
            {
                targetRotation = targetInitialRotation * Quaternion.Euler(
                    Mathf.Sin(Time.time * wobbleSpeed * 0.5f) * wobbleAmount,
                    0,
                    Mathf.Sin(Time.time * wobbleSpeed) * wobbleAmount
                );
            }

            float rotSpeed = isInitialMove ? initialRotationSpeed : bankSpeed;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotSpeed * Time.deltaTime);

            if (droneVisual != null)
            {
                hoverTime += Time.deltaTime;
                float hoverOffset = Mathf.Sin(hoverTime * hoverSpeed) * hoverAmplitude;
                droneVisual.localPosition = new Vector3(
                    visualStartLocalPosition.x,
                    visualStartLocalPosition.y + hoverOffset,
                    visualStartLocalPosition.z
                );
            }
        }
    }

    private float ApplyEasing(EasingOption easing, float t)
    {
        switch (easing)
        {
            case EasingOption.In:
                return t * t;
            case EasingOption.Out:
                return 1 - (1 - t) * (1 - t);
            case EasingOption.InOut:
                return t < 0.5f ? 2 * t * t : 1 - 2 * (1 - t) * (1 - t);
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

    Vector3 CalculateNewLocalTargetPosition()
    {
        float moduleHeight = GetCurrentModuleHeight();

        float newX = Random.Range(-0.25f, 0.25f);
        float newY = moduleHeight + Random.Range(minHeightAboveGround, maxHeightAboveGround);

        float zVariation = Random.Range(-1f, 1f);
        float newZ = forwardDistanceFromPlayer + zVariation;

        return new Vector3(newX, newY, newZ);
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

    void UpdateBanking() {
        // Simple banking update (if used separately)
        float bankAngle = Mathf.Clamp(turnRate * maxBankAngle, -maxBankAngle, maxBankAngle);
        Quaternion bankRot = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, bankAngle);
        transform.rotation = Quaternion.Slerp(transform.rotation, bankRot, bankSpeed * Time.deltaTime);
    }

    public void TriggerExitAfterDelay(float delay) {
        // Enable exit behavior if not already enabled
        enableExitBehavior = true;
        
        // Set the exit time to current time + delay
        exitStartTime = Time.time + delay;
        
        // Make sure we're not already exiting
        isExiting = false;
        exitTweenStarted = false;
        
        // Make the exit more dramatic
        exitVector = new Vector3(Random.Range(-5f, 5f), Random.Range(10f, 15f), Random.Range(-15f, -5f));
        exitSpeed = Random.Range(15f, 25f);
        
        // Destroy the gameObject after exit animation completes
        Destroy(gameObject, delay + exitMovementDuration + 0.5f); // Add a small buffer
        
        // Debug log to verify this is being called
        Debug.Log("Drone exit scheduled in " + delay + " seconds");
    }
}