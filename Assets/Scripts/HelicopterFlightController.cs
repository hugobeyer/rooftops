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

    [Header("Organic Movement")]
    [SerializeField] private float turbulence = 0.5f;
    [SerializeField] private float bobFrequency = 0.3f;

    [Header("Rotors")]
    [SerializeField] private Transform mainRotor;
    [SerializeField] private float mainRotorRPM = 1800f;

    [Header("Figure-8 Settings")]
    [SerializeField] private float pathScale = 50f;
    [SerializeField] private float pathStretch = 0.7f;
    [SerializeField] private float turnIntensity = 1.5f;

    [Header("World Boundaries")]
    [SerializeField] private float minX = -30f;
    [SerializeField] private float maxX = 30f;
    [SerializeField] private float boundaryForce = 8f;

    private Vector3 initialPosition;
    private float currentZ;
    private float turnDirection = 1f;
    private float targetYaw;
    private float currentYaw;
    private float yawVelocity;
    private float pathTime;
    private Vector3 velocity;
    private Vector3 angularVelocity;

    void Start()
    {
        initialPosition = transform.position;
        currentZ = initialPosition.z;
    }

    void Update()
    {
        UpdateMovement();
        UpdateRotors();
    }

    private void UpdateMovement()
    {
        pathTime += Time.deltaTime * patrolSpeed * 0.1f;
        
        // Figure-8 calculations
        float x = Mathf.Sin(pathTime) * pathScale * pathStretch;
        float z = Mathf.Sin(2 * pathTime) * pathScale;
        
        // Derivatives for movement
        float dx = Mathf.Cos(pathTime) * patrolSpeed * pathStretch;
        float dz = Mathf.Cos(2 * pathTime) * 2 * patrolSpeed;

        // Calculate base position
        Vector3 targetPosition = new Vector3(
            initialPosition.x + x,
            initialPosition.y + Mathf.PerlinNoise(pathTime * 0.5f, 0) * altitudeVariation,
            initialPosition.z + z
        );

        // Apply boundary forces
        float boundaryPush = 0f;
        float xOffset = targetPosition.x - initialPosition.x;
        
        if(xOffset > maxX || xOffset < minX)
        {
            float overflow = Mathf.Clamp(xOffset - maxX, minX - xOffset, 0);
            boundaryPush = -overflow * boundaryForce;
            
            // Blend back into figure-8 path
            x = Mathf.Lerp(x, Mathf.Clamp(x, minX, maxX), Time.deltaTime * 2f);
        }

        // Recalculate final position with boundary influence
        targetPosition.x = initialPosition.x + x + boundaryPush * Time.deltaTime;
        targetPosition.x = Mathf.Clamp(targetPosition.x, initialPosition.x + minX, initialPosition.x + maxX);

        // Calculate banking (combine natural and boundary banking)
        float naturalBank = -Mathf.Clamp(dx * turnIntensity, -maxBankAngle, maxBankAngle);
        float boundaryBank = -boundaryPush * 0.5f;
        float finalBank = Mathf.Clamp(naturalBank + boundaryBank, -maxBankAngle, maxBankAngle);

        // Calculate rotation
        float yaw = Mathf.Atan2(dx, dz) * Mathf.Rad2Deg;
        float pitch = Mathf.Clamp(-dz * 0.2f, -15f, 15f);

        // Apply movement and rotation
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, 1.5f);
        Quaternion targetRotation = Quaternion.Euler(pitch, yaw, finalBank);
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
        Vector3 leftBound = new Vector3(minX, initialPosition.y, initialPosition.z);
        Vector3 rightBound = new Vector3(maxX, initialPosition.y, initialPosition.z);
        Gizmos.DrawLine(leftBound + Vector3.up*50, leftBound + Vector3.down*50);
        Gizmos.DrawLine(rightBound + Vector3.up*50, rightBound + Vector3.down*50);
    }
} 