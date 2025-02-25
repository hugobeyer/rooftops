using UnityEngine;

public class HelicopterFlightController : MonoBehaviour
{
    [Header("Z-Axis Patrol")]
    [SerializeField] private float patrolSpeed = 25f;
    //[SerializeField] private float patrolDistance = 200f;
    [SerializeField] private float altitudeVariation = 10f;

    [Header("Banking & Rotation")]
    [SerializeField] private float maxBankAngle = 45f;
    [SerializeField] private float bankResponse = 3f;
    [SerializeField] private float yawSpeed = 25f;
    [SerializeField] private float maxYawAngle = 45f;
    [Tooltip("Controls how much helicopter turns to 'look' at sides (0-1)")]
    [SerializeField] private float searchlightFactor = 0.35f;

    [Header("Organic Movement")]
    [SerializeField] private float turbulence = 0.5f;
    [SerializeField] private float bobFrequency = 0.3f;

    [Header("Rotors")]
    [SerializeField] private Transform mainRotor;
    [SerializeField] private float mainRotorRPM = 1800f;

    [Header("Figure-8 Settings")]
    [Tooltip("Controls the Z-axis amplitude of the figure-8")]
    [SerializeField] private float pathScale = 50f;
    [Tooltip("Controls how wide the figure-8 is relative to its length (higher = wider X movement)")]
    [SerializeField] private float pathStretch = 0.7f;
    [SerializeField] private float turnIntensity = 1.5f;
    [Tooltip("If enabled, will automatically center the path between boundaries")]
    [SerializeField] private bool centerPathInBoundaries = true;
    [Tooltip("If enabled, will apply scaling to keep figure-8 within boundaries")]
    [SerializeField] private bool autoScaleToFitBoundaries = false;

    // Private variable to store boundary scaling factor (preserves user settings)
    private float boundaryScalingFactor = 1.0f;

    [Header("World Boundaries")]
    [SerializeField] private float minX = -30f;
    [SerializeField] private float maxX = 30f;
    [SerializeField] private float boundaryForce = 8f;
    [Tooltip("If enabled, enforces strict boundary limits regardless of path width")]
    [SerializeField] private bool enforceStrictBoundaries = true;

    private Vector3 initialPosition;
    private float currentZ;
    private float turnDirection = 1f;
    private float targetYaw;
    private float currentYaw;
    private float yawVelocity;
    private float pathTime;
    private Vector3 velocity;
    private Vector3 angularVelocity;

    private float currentYawVelocity = 0f; // For smooth yaw dampening
    private float currentPitchVelocity = 0f; // For smooth pitch dampening
    private float smoothedYaw = 0f;
    private float smoothedPitch = 0f;

    void Start()
    {
        initialPosition = transform.position;
        currentZ = initialPosition.z;
        
        // Adjust the path scale to fit within world boundaries
        AdjustPathToFitBoundaries();
    }

    void Update()
    {
        UpdateMovement();
        UpdateRotors();
    }

    // Position figure-8 path within world boundaries
    private void AdjustPathToFitBoundaries()
    {
        // Calculate the center point between boundaries
        float centerX = (maxX + minX) / 2f;
        
        // Only reposition if enabled AND the helicopter is outside boundaries
        if (centerPathInBoundaries)
        {
            bool outsideBoundaries = transform.position.x > maxX || transform.position.x < minX;
            
            // Only center if outside boundaries or placement requested
            if (outsideBoundaries)
            {
                // Position the helicopter at the center X only if outside boundaries
                Vector3 centeredPosition = transform.position;
                centeredPosition.x = centerX;
                transform.position = centeredPosition;
                initialPosition = centeredPosition;
                
                Debug.Log($"Helicopter repositioned to center at X: {centerX}");
            }
            else
            {
                // Keep original position but ensure we remember it
                initialPosition = transform.position;
                Debug.Log($"Helicopter keeping original position at X: {transform.position.x}");
            }
        }
        
        // Reset scaling factor to default
        boundaryScalingFactor = 1.0f;
        
        // Only calculate scaling factor if auto-scaling is enabled
        if (autoScaleToFitBoundaries)
        {
            // Calculate the available width between boundaries
            float availableWidth = (maxX - minX);
            
            // Calculate the current X amplitude based on user settings
            float currentXAmplitude = pathScale * pathStretch;
            
            // Calculate how much we need to scale to fit within boundaries
            // We divide by 2 because the sin function goes from -1 to 1 (total range of 2)
            float safetyMargin = 0.9f; // 90% of available space to stay safely within bounds
            float maxAllowedXAmplitude = (availableWidth / 2f) * safetyMargin;
            
            // Calculate scaling factor needed (if any)
            if (currentXAmplitude > maxAllowedXAmplitude && currentXAmplitude > 0)
            {
                boundaryScalingFactor = maxAllowedXAmplitude / currentXAmplitude;
                Debug.Log($"Figure-8 auto-scaling applied. Scaling factor: {boundaryScalingFactor:F2}");
            }
        }
    }

    private void UpdateMovement()
    {
        pathTime += Time.deltaTime * patrolSpeed * 0.1f;
        
        // Modified figure-8 calculations to create smoother transitions at edges
        // Using a more balanced sin wave formula that spends less time at extremes
        float t = pathTime;
        // Use adjusted sine function that spends less time at extremes
        float x = Mathf.Sin(t) / (1.05f + 0.2f * Mathf.Pow(Mathf.Cos(t), 2)) * pathScale * pathStretch;
        float z = Mathf.Sin(2 * t) * pathScale;
        
        // Update derivatives accordingly for correct banking/turning
        float dx = (Mathf.Cos(t) * (1.05f + 0.2f * Mathf.Pow(Mathf.Cos(t), 2)) - 
                   Mathf.Sin(t) * 0.4f * Mathf.Cos(t) * Mathf.Sin(t)) / 
                   Mathf.Pow(1.05f + 0.2f * Mathf.Pow(Mathf.Cos(t), 2), 2) * patrolSpeed * pathStretch;
        float dz = Mathf.Cos(2 * t) * 2 * patrolSpeed;

        // Calculate base position using original initial position (for better panning)
        Vector3 targetPosition = new Vector3(
            initialPosition.x + x,
            initialPosition.y + (Mathf.PerlinNoise(pathTime *1.5f, 0) - .5f) * altitudeVariation * 2,
            initialPosition.z + z
        );
        
        // Apply boundary forces - stronger enforcement to ensure containment
        float boundaryPush = 0f;
        
        // Strict boundary enforcement - limit position directly if enabled
        if (enforceStrictBoundaries)
        {
            // Check if helicopter would be outside boundaries
            if (targetPosition.x > maxX || targetPosition.x < minX)
            {
                // Calculate how far outside the boundary we are
                float overflow = 0;
                if (targetPosition.x > maxX)
                    overflow = targetPosition.x - maxX;
                else if (targetPosition.x < minX)
                    overflow = targetPosition.x - minX;
                
                // Apply stronger force to keep in bounds
                boundaryPush = -overflow * boundaryForce * 1.5f;
                
                // STRICT ENFORCEMENT: Ensure position stays within bounds
                // Clamp the target position
                float allowedX = Mathf.Clamp(targetPosition.x, minX, maxX);
                
                // Apply smooth transition toward allowed area - stronger than before
                targetPosition.x = Mathf.Lerp(targetPosition.x, allowedX, Time.deltaTime * 5f);
                
                // Final safety check - hard clamp if still outside bounds
                if (targetPosition.x > maxX || targetPosition.x < minX)
                {
                    targetPosition.x = allowedX;
                }
            }
        }
        else
        {
            // Original boundary push logic (softer)
            if (targetPosition.x > maxX || targetPosition.x < minX)
            {
                float overflow = 0;
                if (targetPosition.x > maxX)
                    overflow = targetPosition.x - maxX;
                else if (targetPosition.x < minX)
                    overflow = targetPosition.x - minX;
                    
                boundaryPush = -overflow * boundaryForce;
                
                // Blend back toward allowed area
                float targetX = Mathf.Clamp(targetPosition.x, minX, maxX);
                targetPosition.x = Mathf.Lerp(targetPosition.x, targetX, Time.deltaTime * 2f);
            }
        }
        
        // Apply boundary push
        targetPosition.x += boundaryPush * Time.deltaTime;

        // Calculate turn direction based on velocity to ensure proper banking
        // Positive means turning right, negative means turning left
        float turnDirection = dx;
        
        // Calculate banking that always tilts INTO the turn
        // Positive bank value tilts right (clockwise), negative tilts left (counter-clockwise)
        // Reverse the sign to fix the banking direction
        float naturalBank = -Mathf.Sign(turnDirection) * Mathf.Min(Mathf.Abs(turnDirection * turnIntensity), maxBankAngle);
        
        // Add stronger boundary banking when near boundaries
        float boundaryBank = boundaryPush * 0.5f;
        
        // Ensure boundary banking agrees with the turn direction
        if (boundaryBank != 0 && Mathf.Sign(boundaryBank) != Mathf.Sign(naturalBank))
        {
            boundaryBank = Mathf.Abs(boundaryBank) * Mathf.Sign(naturalBank);
        }
        
        float finalBank = Mathf.Clamp(naturalBank + boundaryBank, -maxBankAngle, maxBankAngle);

        // Calculate rotation
        // Modified yaw calculation to create police helicopter searchlight effect
        // Helicopter mainly follows path but slightly turns to "look" at sides
        float rawYaw = Mathf.Atan2(dx, dz) * Mathf.Rad2Deg;
        
        // Use more forward-facing orientation as base
        float forwardYaw = 0f;
        
        // Calculate a "searchlight" yaw that turns slightly toward direction of travel
        // This creates the effect of helicopter investigating buildings while maintaining path
        float searchlightYaw = Mathf.Lerp(forwardYaw, rawYaw, searchlightFactor);
        
        // Extra logic to make helicopter look more toward sides during lateral movement
        // Strengthens the "investigating" feel when moving sideways
        float lateralFactor = Mathf.Abs(dx) / (Mathf.Abs(dx) + Mathf.Abs(dz) + 0.01f);
        lateralFactor = Mathf.Pow(lateralFactor, 0.5f); // Soften the effect
        
        // Calculate final yaw - stronger turn when moving laterally
        float investigationBoost = lateralFactor * 0.3f; // Adjust this to control extra turning
        float desiredYaw = Mathf.Lerp(searchlightYaw, rawYaw, investigationBoost);
        
        // RESPECT MAX YAW ANGLE: Only restrict the yaw if maxYawAngle is less than 180 degrees
        float targetYaw;
        if (maxYawAngle < 180f)
        {
            // Calculate the yaw relative to forward direction
            float yawRelativeToForward = Mathf.DeltaAngle(forwardYaw, desiredYaw);
            // Clamp it to the maxYawAngle
            yawRelativeToForward = Mathf.Clamp(yawRelativeToForward, -maxYawAngle, maxYawAngle);
            // Convert back to world yaw
            targetYaw = forwardYaw + yawRelativeToForward;
        }
        else
        {
            // When maxYawAngle is 180 or greater, allow full rotation in any direction
            targetYaw = desiredYaw;
        }
        
        // Calculate pitch - add slight downward tilt when investigating
        float basePitch = Mathf.Clamp(-dz * 0.2f, -15f, 15f);
        float targetPitch = Mathf.Lerp(basePitch, -5f, lateralFactor * 0.5f);
        
        // Apply extra smoothing to yaw and pitch to prevent abrupt turns
        // RESPECT YAW SPEED: Calculate the max rotation that can occur in this frame based on yawSpeed
        float maxYawDelta = yawSpeed * Time.deltaTime;
        // Calculate the current desired delta
        float desiredYawDelta = Mathf.DeltaAngle(smoothedYaw, targetYaw);
        // Clamp the delta to respect the yawSpeed
        float clampedYawDelta = Mathf.Clamp(desiredYawDelta, -maxYawDelta, maxYawDelta);
        // Apply the clamped delta to the current yaw
        smoothedYaw = Mathf.MoveTowardsAngle(smoothedYaw, smoothedYaw + clampedYawDelta, maxYawDelta);
        
        // For pitch, continue using SmoothDamp
        float pitchSmoothTime = 0.3f; // Higher = smoother but slower pitch changes
        smoothedPitch = Mathf.SmoothDamp(smoothedPitch, targetPitch, ref currentPitchVelocity, pitchSmoothTime);
        
        // Apply movement and rotation
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, 1.5f);
        Quaternion targetRotation = Quaternion.Euler(smoothedPitch, smoothedYaw, finalBank);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * bankResponse * 2f);
    }

    void UpdateRotors()
    {
        if(mainRotor != null)
            mainRotor.Rotate(Vector3.up, mainRotorRPM * Time.deltaTime);
    }

    // Add to Inspector for easy testing
    public void SetYawInput(float yawInput)
    {
        targetYaw = Mathf.Clamp(yawInput * maxYawAngle, -maxYawAngle, maxYawAngle);
    }

    // Add to OnDrawGizmos for visualization
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        
        // Draw world space boundary lines
        Vector3 leftBound = new Vector3(minX, transform.position.y, transform.position.z);
        Vector3 rightBound = new Vector3(maxX, transform.position.y, transform.position.z);
        
        // Draw vertical lines extending up and down from current height
        Gizmos.DrawLine(leftBound + Vector3.up*50, leftBound + Vector3.down*50);
        Gizmos.DrawLine(rightBound + Vector3.up*50, rightBound + Vector3.down*50);
        
        // Draw top and bottom connecting lines
        Gizmos.DrawLine(leftBound + Vector3.up*50, rightBound + Vector3.up*50);
        Gizmos.DrawLine(leftBound + Vector3.down*50, rightBound + Vector3.down*50);
    }
} 