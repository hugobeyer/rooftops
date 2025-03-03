using UnityEngine;
using RoofTops;
using System.Collections;

public class NoiseMovement : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;
    public float baseLookSpeed = 5f;

    [Header("Rotation Response Speeds")]
    public float yawLookSpeed = 5f;
    public float pitchLookSpeed = 5f;
    public float rollLookSpeed = 15f;  // Faster for roll to make it more noticeable

    [Header("First Noise Layer")]
    public float firstFrequency = 1f;
    public float firstIntensity = 1f;
    public Vector3 firstNoiseOffset;

    [Header("Second Noise Layer")]
    public float secondFrequency = 2f;
    public float secondIntensity = 0.5f;
    public Vector3 firstNoiseOffset2;

    [Header("Y Axis Constraints")]
    public float minYOffset = -2f;
    public float maxYOffset = 2f;

    [Header("X Axis Constraints")]
    public float minXOffset = -2f;
    public float maxXOffset = 2f;

    [Header("Z Axis Constraints")]
    public float minZOffset = -2f;
    public float maxZOffset = 2f;

    [Header("Fit Settings")]
    public FitRange.FitType fitType = FitRange.FitType.MinMax;

    [Header("Rotation Noise")]
    [Header("Yaw (Y-axis)")]
    public float yawFrequency = 1f;
    public float minYawAngle = -15f;
    public float maxYawAngle = 15f;

    [Header("Pitch (X-axis)")]
    public float pitchFrequency = 1f;
    public float minPitchAngle = -15f;
    public float maxPitchAngle = 15f;

    [Header("Roll (Z-axis)")]
    public float rollFrequency = 1f;
    public float minRollAngle = -15f;
    public float maxRollAngle = 15f;

    [Header("Speed Noise")]
    public float speedNoiseFrequency = 0.5f;  // How fast the speed changes
    public float minSpeedMultiplier = 0.5f;   // Minimum speed multiplier
    public float maxSpeedMultiplier = 2.0f;   // Maximum speed multiplier
    private float speedNoiseOffset;           // For random starting point

    // New Blend Settings
    [Header("Blend Settings")]
    public bool useInitialBlend = false;         // Enable/Disable initial blending
    public Vector3 initialBlendPosition;         // Starting point for the blend
    public float blendDuration = 1f;               // Duration (in seconds) for the blend
    private float blendTimer = 0f;                 // Internal timer for position blending

    [Header("Blend FOV Settings")]
    public bool blendFOV = false;
    public float initialFOV = 60f;
    public float midFOV = 75f;
    public float finalFOV = 90f;

    [Header("Blend Timings")]
    public float midFOVDelay = 1f;          // How long to wait before starting mid blend
    public float finalFOVDelay = 2f;        // How long to wait before starting final blend
    public float midBlendDuration = 1f;     // How long the blend to mid FOV/offset takes
    public float finalBlendDuration = 2f;   // How long the blend to final FOV/offset takes

    [Header("Look At Settings")]
    public bool useInitialLookAtOffset = false;
    public Vector3 midLookAtOffset = new Vector3(0, 1f, 0);
    public Vector3 initialLookAtOffset = Vector3.zero;

    [Header("Initial Game Settings")]
    public float initialNoiseMultiplier = 0.5f;  // Controls noise intensity before game starts
    private float currentNoiseMultiplier = 1f;

    private Vector3 startingPosition;
    private Vector3 noisePosition;
    private float yawNoiseOffset;
    private float pitchNoiseOffset;
    private float rollNoiseOffset;
    
    // Add these new fields to store the calculated angles
    private float yawAngle;
    private float pitchAngle;
    private float rollAngle;

    // Cache these vectors to avoid allocations
    private Vector3 targetDirection;
    private readonly Vector3 upOffset = Vector3.up * 0.5f;
    
    // Cache quaternions to avoid allocations
    private Quaternion yawRotation;
    private Quaternion pitchRotation; 
    private Quaternion rollRotation;
    private Quaternion targetRotation;

    [Header("Death Camera")]
    public Transform deathCameraTarget;
    public float deathBlendDuration = 1.5f;
    public float deathFOV = 65f;
    public float deathNoiseIntensity = 0.5f;
    public Vector3 deathOffset = new Vector3(0, 2f, -4f);
    public GameObject deathVisualObject;

    private bool isPlayerDead = false;
    private bool isInDeathTransition = false;

    // Store these for the blend
    private Vector3 deathStartPosition;
    private Quaternion deathStartRotation;
    private float deathStartFOV;

    // Add these back - they're needed for the blending system
    private float fovBlendTimer = 0f;
    private float lookAtBlendTimer = 0f;
    private FOVStage currentFOVStage = FOVStage.Initial;

    private enum FOVStage
    {
        Initial,
        Mid,
        Final
    }

    private void Start()
    {
        // Save default starting position (will be used for noise offset)
        startingPosition = transform.position;
        
        // If using blend, override the starting position
        if(useInitialBlend)
        {
            transform.position = initialBlendPosition;
        }
        
        // Randomize noise offsets
        firstNoiseOffset = new Vector3(
            Random.Range(0f, 1000f),
            Random.Range(0f, 1000f),
            Random.Range(0f, 1000f)
        );

        firstNoiseOffset2 = new Vector3(
            Random.Range(0f, 1000f),
            Random.Range(0f, 1000f),
            Random.Range(0f, 1000f)
        );

        // Add random offsets for rotation
        yawNoiseOffset = Random.Range(0f, 1000f);
        pitchNoiseOffset = Random.Range(0f, 1000f);
        rollNoiseOffset = Random.Range(0f, 1000f);

        // Add random offset for speed noise
        speedNoiseOffset = Random.Range(0f, 1000f);

        // Hide death visual at start
        if (deathVisualObject != null)
        {
            deathVisualObject.SetActive(false);
        }
    }

    private void Update()
    {
        // If we're in the death transition or fully dead, skip normal rotation updates
        if (isPlayerDead || isInDeathTransition)
        {
            // You can still do your positional noise if you want, but skip rotation
            // If you need to keep *positional* noise, do it here but don't call transform.rotation below.
            return;
        }

        // Otherwise, do normal camera behavior
        if (!GameManager.Instance.HasGameStarted)
        {
            currentNoiseMultiplier = initialNoiseMultiplier;
        }
        else if (currentNoiseMultiplier < 1f)
        {
            currentNoiseMultiplier = Mathf.MoveTowards(currentNoiseMultiplier, 1f, Time.deltaTime);
        }

        // Calculate noise and position
        float time = Time.time;
        
        // Calculate speed multiplier from noise
        float speedMultiplier = FitRange.Fit(
            Mathf.PerlinNoise(time * speedNoiseFrequency + speedNoiseOffset, 0f),
            0f, 1f,
            minSpeedMultiplier, maxSpeedMultiplier,
            fitType
        );

        // Calculate time-based noise for first layer
        noisePosition.x = FitRange.Fit(Mathf.PerlinNoise(time * firstFrequency + firstNoiseOffset.x, 0f), 0f, 1f, minXOffset, maxXOffset, fitType);
        noisePosition.y = FitRange.Fit(Mathf.PerlinNoise(time * firstFrequency + firstNoiseOffset.y, 1f), 0f, 1f, minYOffset, maxYOffset, fitType);
        noisePosition.z = FitRange.Fit(Mathf.PerlinNoise(time * firstFrequency + firstNoiseOffset.z, 2f), 0f, 1f, minZOffset, maxZOffset, fitType);

        noisePosition *= firstIntensity * currentNoiseMultiplier;

        Vector3 noise2 = new Vector3(
            FitRange.Fit(Mathf.PerlinNoise(time * secondFrequency + firstNoiseOffset2.x, 3f), 0f, 1f, minXOffset, maxXOffset, fitType),
            FitRange.Fit(Mathf.PerlinNoise(time * secondFrequency + firstNoiseOffset2.y, 4f), 0f, 1f, minYOffset, maxYOffset, fitType),
            FitRange.Fit(Mathf.PerlinNoise(time * secondFrequency + firstNoiseOffset2.z, 5f), 0f, 1f, minZOffset, maxZOffset, fitType)
        ) * secondIntensity * currentNoiseMultiplier;

        // Combine noise layers
        noisePosition += noise2;

        // Simplify target position logic
        Vector3 targetPos = isPlayerDead ? 
            (deathCameraTarget != null ? deathCameraTarget.position : transform.position) + noisePosition :
            startingPosition + noisePosition;

        // Position blending: only start blending position after game has started.
        if (useInitialBlend && blendTimer < blendDuration)
        {
            if (GameManager.Instance != null && !GameManager.Instance.HasGameStarted)
            {
                // Hold position until game starts.
                transform.position = initialBlendPosition;
            }
            else
            {
                blendTimer += Time.deltaTime;
                float rawBlendFactor = Mathf.Clamp01(blendTimer / blendDuration);
                float blendFactor = Mathf.SmoothStep(0f, 1f, rawBlendFactor);
                transform.position = Vector3.Lerp(initialBlendPosition, targetPos, blendFactor);
            }
        }
        else
        {
            transform.position = targetPos;
        }
        
        // FOV blending: update only when the game has started.
        if (blendFOV)
        {
            Camera cam = GetComponent<Camera>();
            if (cam != null)
            {
                if (GameManager.Instance != null && !GameManager.Instance.HasGameStarted)
                {
                    cam.fieldOfView = initialFOV;
                    fovBlendTimer = 0f;
                    currentFOVStage = FOVStage.Initial;
                }
                else
                {
                    fovBlendTimer += Time.deltaTime;
                    
                    switch (currentFOVStage)
                    {
                        case FOVStage.Initial:
                            if (fovBlendTimer >= midFOVDelay)
                            {
                                currentFOVStage = FOVStage.Mid;
                                fovBlendTimer = 0f;
                                lookAtBlendTimer = 0f;
                            }
                            break;
                            
                        case FOVStage.Mid:
                            float midBlend = Mathf.SmoothStep(0f, 1f, fovBlendTimer / midBlendDuration);
                            cam.fieldOfView = Mathf.Lerp(initialFOV, midFOV, midBlend);
                            
                            if (fovBlendTimer >= midBlendDuration && fovBlendTimer >= finalFOVDelay)
                            {
                                currentFOVStage = FOVStage.Final;
                                fovBlendTimer = 0f;
                                lookAtBlendTimer = 0f;
                            }
                            break;
                            
                        case FOVStage.Final:
                            float finalBlend = Mathf.SmoothStep(0f, 1f, fovBlendTimer / finalBlendDuration);
                            cam.fieldOfView = Mathf.Lerp(midFOV, finalFOV, finalBlend);
                            break;
                    }
                }
            }
        }

        // Calculate rotation noise for each axis and store in class fields
        yawAngle = FitRange.Fit(
            Mathf.PerlinNoise(time * yawFrequency + yawNoiseOffset, 0f),
            0f, 1f,
            minYawAngle * currentNoiseMultiplier, maxYawAngle * currentNoiseMultiplier,
            fitType
        );

        pitchAngle = FitRange.Fit(
            Mathf.PerlinNoise(time * pitchFrequency + pitchNoiseOffset, 1f),
            0f, 1f,
            minPitchAngle * currentNoiseMultiplier, maxPitchAngle * currentNoiseMultiplier,
            fitType
        );

        rollAngle = FitRange.Fit(
            Mathf.PerlinNoise(time * rollFrequency + rollNoiseOffset, 2f),
            0f, 1f,
            minRollAngle * currentNoiseMultiplier, maxRollAngle * currentNoiseMultiplier,
            fitType
        );

        // Cache rotation calculations
        if (target != null)
        {
            Vector3 lookAtTargetPos = target.position;  // Default to player
            
            if (isPlayerDead)
            {
                // Priority 1: Death camera target
                if (deathCameraTarget != null)
                {
                    lookAtTargetPos = deathCameraTarget.position;
                }
                // Priority 2: Last known player position
                else
                {
                    lookAtTargetPos = transform.position;
                }
                
                // Remove vertical offset during death view
                targetDirection = lookAtTargetPos - transform.position;
            }
            else
            {
                // Normal gameplay look-at logic
                if (useInitialLookAtOffset && GameManager.Instance != null)
                {
                    if (!GameManager.Instance.HasGameStarted)
                    {
                        lookAtTargetPos = target.position + initialLookAtOffset;
                        lookAtBlendTimer = 0f;
                    }
                    else
                    {
                        switch (currentFOVStage)
                        {
                            case FOVStage.Initial:
                                lookAtTargetPos = target.position + initialLookAtOffset;
                                break;

                            case FOVStage.Mid:
                                lookAtBlendTimer += Time.deltaTime;
                                float toMidBlend = Mathf.SmoothStep(0f, 1f, lookAtBlendTimer / midBlendDuration);
                                lookAtTargetPos = target.position + Vector3.Lerp(initialLookAtOffset, midLookAtOffset, toMidBlend);
                                break;

                            case FOVStage.Final:
                                lookAtBlendTimer += Time.deltaTime;
                                float toFinalBlend = Mathf.SmoothStep(0f, 1f, lookAtBlendTimer / finalBlendDuration);
                                lookAtTargetPos = target.position + Vector3.Lerp(midLookAtOffset, Vector3.zero, toFinalBlend);
                                break;
                        }
                    }
                }
                targetDirection = lookAtTargetPos - transform.position + upOffset;
            }

            targetRotation = Quaternion.LookRotation(targetDirection);
            
            // Apply rotations (noise remains but could be modified)
            yawRotation.eulerAngles = new Vector3(0, yawAngle, 0);
            pitchRotation.eulerAngles = new Vector3(pitchAngle, 0, 0);
            rollRotation.eulerAngles = new Vector3(0, 0, rollAngle);
            transform.rotation = targetRotation * yawRotation * pitchRotation * rollRotation;
        }
        else
        {
            transform.rotation = Quaternion.Euler(pitchAngle, yawAngle, rollAngle);
        }
    }

    private IEnumerator BlendToDeathView()
    {
        float elapsed = 0f;
        isInDeathTransition = true;

        // 1) Capture our current transforms
        deathStartPosition = transform.position;
        deathStartRotation = transform.rotation;
        deathStartFOV = GetComponent<Camera>().fieldOfView;

        // 2) Final position/rotation
        Vector3 finalPosition;
        
        // Check if player is on a jump pad or has extreme velocity
        PlayerController playerController = deathCameraTarget?.GetComponent<PlayerController>();
        bool isOnJumpPad = playerController != null && playerController.IsOnJumpPad;
        
        if (isOnJumpPad)
        {
            // Use a more constrained offset for jump pad deaths
            Vector3 limitedOffset = new Vector3(
                Mathf.Clamp(deathOffset.x, -2f, 2f),
                Mathf.Clamp(deathOffset.y, -2f, 2f),
                Mathf.Clamp(deathOffset.z, -2f, 2f)
            );
            
            finalPosition = (deathCameraTarget != null) 
                ? deathCameraTarget.position + limitedOffset
                : transform.position;
        }
        else
        {
            // Normal death offset
            finalPosition = (deathCameraTarget != null) 
                ? deathCameraTarget.position + deathOffset
                : transform.position;
        }

        Vector3 finalLookAtPos = (deathCameraTarget != null)
            ? (deathCameraTarget.position + upOffset)
            : transform.position + transform.forward;

        Vector3 finalLookDirection = finalLookAtPos - finalPosition; 
        Quaternion finalRotation = Quaternion.LookRotation(finalLookDirection, Vector3.up);

        // 3) Blend smoothly
        while (elapsed < deathBlendDuration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / deathBlendDuration);

            // Lerp position
            transform.position = Vector3.Lerp(deathStartPosition, finalPosition, t);

            // Slerp rotation
            transform.rotation = Quaternion.Slerp(deathStartRotation, finalRotation, t);

            // Lerp FOV
            GetComponent<Camera>().fieldOfView = Mathf.Lerp(deathStartFOV, deathFOV, t);

            // If you want to keep rotation NOISE while transitioning, you can skip it or adjust it:
            firstIntensity  = Mathf.Lerp(firstIntensity,  deathNoiseIntensity, t);
            secondIntensity = Mathf.Lerp(secondIntensity, deathNoiseIntensity, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 4) Snap final
        transform.position = finalPosition;
        transform.rotation = finalRotation;
        GetComponent<Camera>().fieldOfView = deathFOV;
        firstIntensity  = deathNoiseIntensity;
        secondIntensity = deathNoiseIntensity;

        // End transition
        isInDeathTransition = false;
    }

    public void ResetCamera()
    {
        isPlayerDead = false;
        isInDeathTransition = false;

        // Hide death visual
        if (deathVisualObject != null)
        {
            deathVisualObject.SetActive(false);
        }

        // Restore target reference
        target = FindFirstObjectByType<PlayerController>()?.transform;
        
        // Reset noise offsets
        firstNoiseOffset = new Vector3(
            Random.Range(0f, 1000f),
            Random.Range(0f, 1000f),
            Random.Range(0f, 1000f)
        );

        firstNoiseOffset2 = new Vector3(
            Random.Range(0f, 1000f),
            Random.Range(0f, 1000f),
            Random.Range(0f, 1000f)
        );

        // Reset rotation noise offsets
        yawNoiseOffset = Random.Range(0f, 1000f);
        pitchNoiseOffset = Random.Range(0f, 1000f);
        rollNoiseOffset = Random.Range(0f, 1000f);

        // Reset turn animation
        if (target != null)
        {
            PlayerAnimatorController animController = target.GetComponent<PlayerAnimatorController>();
            if (animController != null)
            {
                animController.ResetTurnState();
            }
        }
    }

    public void TransitionToDeathView()
    {
        isPlayerDead = true;
        
        // Show death visual if you like
        if (deathVisualObject != null)
        {
            deathVisualObject.SetActive(true);
        }

        StartCoroutine(BlendToDeathView());
    }
} 