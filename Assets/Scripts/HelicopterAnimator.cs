using UnityEngine;

public class HelicopterAnimator : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Local Z offset minimum relative to starting position.")]
    public float minZ = -50f;
    [Tooltip("Local Z offset maximum relative to starting position.")]
    public float maxZ = 50f;
    [Tooltip("Duration (in seconds) for a full cycle (from minZ to maxZ and back).")]
    public float cycleDuration = 10f;

    [Header("Banking Settings")]
    [Tooltip("Maximum banking (roll) angle in degrees.")]
    public float maxBankAngle = 15f;
    [Tooltip("Smoothing factor for banking transitions.")]
    public float rotationSmooth = 2f;

    [Header("Height Variation Settings")]
    [Tooltip("Local Y offset minimum relative to starting position.")]
    public float minYVariation = -2f;
    [Tooltip("Local Y offset maximum relative to starting position.")]
    public float maxYVariation = 2f;
    [Tooltip("Duration (in seconds) for a full vertical cycle (bob up and down).")]
    public float heightCycleDuration = 3f;

    [Header("Orientation Variation")]
    [Tooltip("Maximum yaw variation (degrees) offset from base rotation.")]
    public float maxYawVariation = 5f;
    [Tooltip("Maximum pitch variation (degrees) offset from base rotation.")]
    public float maxPitchVariation = 5f;
    [Tooltip("Speed multiplier for orientation variation.")]
    public float orientationVariationSpeed = 1f;

    // Store the starting position and base rotation.
    private Vector3 startPosition;
    private Quaternion baseRotation;

    // Current bank (roll) in degrees; updated smoothly.
    private float currentRoll = 0f;

    void Start()
    {
        // Save the starting world position and rotation.
        startPosition = transform.position;
        baseRotation = transform.rotation;
    }

    void Update()
    {
        float t = Time.time;

        // --- Z-Axis Movement ---
        float omega = 2 * Mathf.PI / cycleDuration;
        float sineValue = Mathf.Sin(omega * t - Mathf.PI / 2); // range [-1, 1]
        float factor = (sineValue + 1f) / 2f; // normalized to [0, 1]
        float newLocalZ = Mathf.Lerp(minZ, maxZ, factor);

        // --- Height (Y-Axis) Variation ---
        float heightOmega = 2 * Mathf.PI / heightCycleDuration;
        float heightSineValue = Mathf.Sin(heightOmega * t - Mathf.PI / 2); // range [-1, 1]
        float heightFactor = (heightSineValue + 1f) / 2f; // normalized to [0, 1]
        float newLocalY = Mathf.Lerp(minYVariation, maxYVariation, heightFactor);

        // Update the world position relative to the starting position.
        Vector3 newPos = startPosition;
        newPos.z += newLocalZ;
        newPos.y += newLocalY;
        transform.position = newPos;

        // --- Banking (Roll) Effect ---
        float accelerationZ = -(maxZ - minZ) * (omega * omega / 2f) * Mathf.Sin(omega * t - Mathf.PI / 2);
        float maxAcc = (maxZ - minZ) * (omega * omega / 2f);
        float multiplier = (maxAcc != 0) ? (maxBankAngle / maxAcc) : 0f;
        float desiredRoll = Mathf.Clamp(-accelerationZ * multiplier, -maxBankAngle, maxBankAngle);
        currentRoll = Mathf.Lerp(currentRoll, desiredRoll, Time.deltaTime * rotationSmooth);

        // --- Additional Orientation Variations (Yaw and Pitch) ---
        float yawOffset = maxYawVariation * Mathf.Sin(Time.time * orientationVariationSpeed);
        float pitchOffset = maxPitchVariation * Mathf.Sin(Time.time * orientationVariationSpeed + Mathf.PI / 2);

        // Combine the banking (roll) with yaw and pitch variation.
        Quaternion variationRotation = Quaternion.Euler(pitchOffset, yawOffset, currentRoll);
        transform.rotation = baseRotation * variationRotation;
    }
}