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

    private Vector3 startingPosition;
    private Vector3 noisePosition;
    private float yawNoiseOffset;
    private float pitchNoiseOffset;
    private float rollNoiseOffset;

    private void Start()
    {
        startingPosition = transform.position;
        
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
        float time = Time.time;
        
        // Calculate speed multiplier from noise
        float speedMultiplier = FitRange.Fit(
            Mathf.PerlinNoise(time * speedNoiseFrequency + speedNoiseOffset, 0f),
            0f, 1f,
            minSpeedMultiplier, maxSpeedMultiplier,
            fitType
        );

        // Calculate time-based noise for first layer
        Vector3 noise1 = new Vector3(
            FitRange.Fit(Mathf.PerlinNoise(time * firstFrequency + firstNoiseOffset.x, 0f), 0f, 1f, minXOffset, maxXOffset, fitType),
            FitRange.Fit(Mathf.PerlinNoise(time * firstFrequency + firstNoiseOffset.y, 1f), 0f, 1f, minYOffset, maxYOffset, fitType),
            FitRange.Fit(Mathf.PerlinNoise(time * firstFrequency + firstNoiseOffset.z, 2f), 0f, 1f, minZOffset, maxZOffset, fitType)
        ) * firstIntensity;

        Vector3 noise2 = new Vector3(
            FitRange.Fit(Mathf.PerlinNoise(time * secondFrequency + firstNoiseOffset2.x, 3f), 0f, 1f, minXOffset, maxXOffset, fitType),
            FitRange.Fit(Mathf.PerlinNoise(time * secondFrequency + firstNoiseOffset2.y, 4f), 0f, 1f, minYOffset, maxYOffset, fitType),
            FitRange.Fit(Mathf.PerlinNoise(time * secondFrequency + firstNoiseOffset2.z, 5f), 0f, 1f, minZOffset, maxZOffset, fitType)
        ) * secondIntensity;

        // Combine noises and add to starting position
        noisePosition = startingPosition + noise1 + noise2;
        transform.position = noisePosition;

        // Calculate rotation noise for each axis
        float yawAngle = FitRange.Fit(
            Mathf.PerlinNoise(time * yawFrequency + yawNoiseOffset, 0f),
            0f, 1f,
            minYawAngle, maxYawAngle,
            fitType
        );

        float pitchAngle = FitRange.Fit(
            Mathf.PerlinNoise(time * pitchFrequency + pitchNoiseOffset, 1f),
            0f, 1f,
            minPitchAngle, maxPitchAngle,
            fitType
        );

        float rollAngle = FitRange.Fit(
            Mathf.PerlinNoise(time * rollFrequency + rollNoiseOffset, 2f),
            0f, 1f,
            minRollAngle, maxRollAngle,
            fitType
        );

        // Look at target if assigned and apply rotations
        if (target != null)
        {
            Quaternion targetRotation = Quaternion.LookRotation(target.position - transform.position);
            
            // Create separate rotations for each axis
            Quaternion yawRotation = Quaternion.Euler(0, yawAngle, 0);
            Quaternion pitchRotation = Quaternion.Euler(pitchAngle, 0, 0);
            Quaternion rollRotation = Quaternion.Euler(0, 0, rollAngle);

            // Apply each rotation with speed affected by noise multiplier
            Quaternion currentRotation = transform.rotation;
            currentRotation = Quaternion.Slerp(currentRotation, targetRotation * yawRotation, 
                Time.deltaTime * yawLookSpeed * speedMultiplier);
            currentRotation = Quaternion.Slerp(currentRotation, currentRotation * pitchRotation, 
                Time.deltaTime * pitchLookSpeed * speedMultiplier);
            currentRotation = Quaternion.Slerp(currentRotation, currentRotation * rollRotation, 
                Time.deltaTime * rollLookSpeed * speedMultiplier);

            transform.rotation = currentRotation;
        }
        else
        {
            // If no target, just apply noise rotation
            transform.rotation = Quaternion.Euler(pitchAngle, yawAngle, rollAngle);
        }
    }
} 