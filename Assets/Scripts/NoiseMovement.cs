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

    [Header("Death Settings")]
    private bool isPlayerDead = false;
    private Vector3 lastValidPosition;  // Store the last position before death
    public Transform deathCameraTarget;  // Assign this in inspector - the position to move to when player dies
    public GameObject deathVisualObject;  // Object to show during death (assign in inspector)
    public float deathTransitionSpeed = 2f;  // How fast to move to death position
    private float deathTransitionTimer = 0f;
    private Vector3 deathStartPosition;  // Position when death started

    // Add these back - they're needed for the blending system
    private float fovBlendTimer = 0f;
    private float lookAtBlendTimer = 0f;
    private FOVStage currentFOVStage = FOVStage.Initial;

    [Header("Death Camera Settings")]
    public float deathNoiseIntensity = 0.5f;    // How much noise during death
    public float deathBlendDuration = 1.5f;      // How long to blend to death view
    public float deathFOV = 65f;                 // FOV during death state
    private float deathBlendTimer = 0f;          // Track the death transition

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
        
        // If using blend, override the starting position of the transform with your custom point
        if(useInitialBlend)
        {
            transform.position = initialBlendPosition;
        }
        
        // Randomize starting noise offsets
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

        // Ensure death visual is hidden at start
        if (deathVisualObject != null)
        {
            deathVisualObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (isPlayerDead) return;

        if (!GameManager.Instance.HasGameStarted)
        {
            currentNoiseMultiplier = initialNoiseMultiplier;
        }
        else if (currentNoiseMultiplier < 1f)
        {
            // Smoothly transition to full noise when game starts
            currentNoiseMultiplier = Mathf.MoveTowards(currentNoiseMultiplier, 1f, Time.deltaTime);
        }

        // Check if player just died
        if (target != null && target.GetComponent<PlayerController>()?.IsDead() == true && !isPlayerDead)
        {
            isPlayerDead = true;
            lastValidPosition = target.position;
            deathStartPosition = transform.position;
            
            // Show the death visual object
            if (deathVisualObject != null)
            {
                deathVisualObject.SetActive(true);
            }
            
            deathTransitionTimer = 0f;  // Reset transition timer
        }

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

        // The default target position for noise movement
        Vector3 targetPos;
        if (isPlayerDead)
        {
            if (deathCameraTarget != null)
            {
                // Lerp to death camera position
                deathTransitionTimer += Time.deltaTime * deathTransitionSpeed;
                float t = Mathf.SmoothStep(0f, 1f, deathTransitionTimer);
                targetPos = Vector3.Lerp(deathStartPosition, deathCameraTarget.position, t) + noisePosition;
            }
            else
            {
                // Fallback to last valid position if no death target is set
                targetPos = lastValidPosition + noisePosition;
            }
        }
        else
        {
            targetPos = startingPosition + noisePosition;
        }

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
                    lookAtTargetPos = lastValidPosition;
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

    void LateUpdate()
    {
        // If in death state, skip normal rotation calculations
        if (isPlayerDead) return;
        
        // ... keep existing rotation code ...
    }

    public void ResetCamera()
    {
        isPlayerDead = false;
        deathTransitionTimer = 0f;
        
        // Restore target reference
        target = FindFirstObjectByType<PlayerController>()?.transform;
        
        // Hide the death visual object
        if (deathVisualObject != null)
        {
            deathVisualObject.SetActive(false);
        }
        
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
        StartCoroutine(BlendToDeathView());
    }

    private IEnumerator BlendToDeathView()
    {
        float duration = 1f;
        float elapsed = 0f;
        
        // Store initial noise values
        float startFirstIntensity = firstIntensity;
        float startSecondIntensity = secondIntensity;
        
        Quaternion startRotation = transform.rotation;
        Vector3 startPosition = transform.position;

        if (deathCameraTarget != null)
        {
            Vector3 lookPos = deathCameraTarget.position;
            Quaternion targetRotation = Quaternion.LookRotation(lookPos - startPosition);
            
            while (elapsed < duration)
            {
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                
                // Diminish noise intensity gradually
                firstIntensity = Mathf.Lerp(startFirstIntensity, 0f, t);
                secondIntensity = Mathf.Lerp(startSecondIntensity, 0f, t);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            transform.rotation = targetRotation;
            firstIntensity = 0f;
            secondIntensity = 0f;
        }
        
        // Just wait a frame to ensure everything is settled
        yield return null;
    }

    private IEnumerator DeathShake()
    {
        float duration = 1f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            // Add subtle positional noise
            transform.localPosition += new Vector3(
                Random.Range(-0.1f, 0.1f),
                Random.Range(-0.05f, 0.05f),
                Random.Range(-0.1f, 0.1f)
            );
            
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
} 