using UnityEngine;

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
    private float fovBlendTimer = 0f;              // Separate timer for FOV blending

    [Header("Blend FOV Settings")]
    public bool blendFOV = false;                // Enable FOV blending
    public float initialFOV = 60f;               // Starting Field-of-View
    public float finalFOV = 90f;                 // Target Field-of-View

    [Header("Initial LookAt Settings")]
    public bool useInitialLookAtOffset = false;     // Enable the initial look-at offset
    public Vector3 initialLookAtOffset = Vector3.zero; // Offset to add to the target position for initial look

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
    }

    private void Update()
    {
        if (!GameManager.Instance.HasGameStarted)
        {
            currentNoiseMultiplier = initialNoiseMultiplier;
        }
        else if (currentNoiseMultiplier < 1f)
        {
            // Smoothly transition to full noise when game starts
            currentNoiseMultiplier = Mathf.MoveTowards(currentNoiseMultiplier, 1f, Time.deltaTime);
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
        Vector3 targetPos = startingPosition + noisePosition;

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
                    // While the game hasn't started, hold FOV at the initial value.
                    cam.fieldOfView = initialFOV;
                }
                else if (fovBlendTimer < blendDuration)
                {
                    fovBlendTimer += Time.deltaTime;
                    float fovRawBlend = Mathf.Clamp01(fovBlendTimer / blendDuration);
                    float fovBlendFactor = Mathf.SmoothStep(0f, 1f, fovRawBlend);
                    cam.fieldOfView = Mathf.Lerp(initialFOV, finalFOV, fovBlendFactor);
                }
                else
                {
                    cam.fieldOfView = finalFOV;
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
            Vector3 lookAtTargetPos;
            if (useInitialLookAtOffset && GameManager.Instance != null)
            {
                if (!GameManager.Instance.HasGameStarted)
                {
                    // Hold at initial offset position
                    lookAtTargetPos = target.position + initialLookAtOffset;
                }
                else
                {
                    // Use the same blend timer/duration from position blending
                    float blendFactor = Mathf.SmoothStep(0f, 1f, blendTimer / blendDuration);
                    lookAtTargetPos = Vector3.Lerp(target.position + initialLookAtOffset, target.position, blendFactor);
                }
            }
            else
            {
                lookAtTargetPos = target.position;
            }

            targetDirection = lookAtTargetPos - transform.position + upOffset;
            targetRotation = Quaternion.LookRotation(targetDirection);
            
            // Set cached quaternions using the calculated noise angles
            yawRotation.eulerAngles = new Vector3(0, yawAngle, 0);
            pitchRotation.eulerAngles = new Vector3(pitchAngle, 0, 0);
            rollRotation.eulerAngles = new Vector3(0, 0, rollAngle);

            // Apply the final rotation
            transform.rotation = targetRotation * yawRotation * pitchRotation * rollRotation;
        }
        else
        {
            transform.rotation = Quaternion.Euler(pitchAngle, yawAngle, rollAngle);
        }
    }

    public void ResetCamera()
    {
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
    }

    public void TransitionToDeathView()
    {
        // Optional: Modify camera behavior during death
        // For example, increase noise intensity or change offset
        firstIntensity *= 2f;  // Double the noise intensity
        secondIntensity *= 2f;
        
        // Could also modify rotation ranges for death view
        minYawAngle *= 1.5f;
        maxYawAngle *= 1.5f;
    }
} 