using UnityEngine;  // Add this at the top!

public class SpotController : MonoBehaviour 
{
    public Material spotMaterial;  // Assign your cone material
    private Transform playerTransform;  // Will find automatically
    private Camera mainCam;
    private NoiseMovement noiseMovement;  // Reference to noise script
    
    [Header("Spot Settings")]
    public float tiltAmount = 0.2f;        // Much smaller for subtle movement
    public float noiseSpeed = 0.2f;      // Much slower
    public float offsetAmount = 0.2f;      // Much smaller offset
    public float focusInterval = 3f;         // Every 3 seconds
    public float focusDuration = 0.5f;       // Half second focus

    [Header("Smoothing")]
    public float tiltDamping = 0.5f;       // Lower = faster response
    public float noiseDamping = 1f;        // Lower = faster response

    private Vector3 SpotTiltVector;
    private Vector3 SpotOffsetVector;
    private float nextFocusCheck;
    private float focusEndTime;
    private bool isFocusing;
    private float baseTime;

    // Cache property IDs
    private readonly int tiltID = Shader.PropertyToID("_SpotTiltVector");
    private readonly int offsetID = Shader.PropertyToID("_SpotOffsetVector");

    void Start()
    {
        if (spotMaterial == null)
        {
            enabled = false;  // Disable component if no material
            return;
        }

        mainCam = Camera.main;
        playerTransform = FindAnyObjectByType<PlayerController>()?.transform;
        noiseMovement = mainCam.GetComponent<NoiseMovement>();
        baseTime = Time.time;
        nextFocusCheck = baseTime;  // Start checking immediately
    }

    void Update()
    {
        if (Time.time > nextFocusCheck)
        {
            isFocusing = true;
            focusEndTime = Time.time + focusDuration;
            nextFocusCheck = Time.time + focusInterval;
        }

        if (Time.time > focusEndTime)
        {
            isFocusing = false;
        }

        float t = (Time.time - baseTime) * noiseSpeed;
        
        if (isFocusing)
        {
            SpotTiltVector = Vector3.zero;  // Look at origin (0,0,0)
        }
        else
        {
            SpotTiltVector = new Vector3(
                Mathf.Sin(t), 
                Mathf.Cos(t * 0.9f), 
                Mathf.Sin(t * 1.1f)
            ) * tiltAmount;
        }

        SpotOffsetVector = Vector3.one * Mathf.Sin(t * 0.5f) * offsetAmount;

        spotMaterial.SetVector(tiltID, SpotTiltVector);
        spotMaterial.SetVector(offsetID, SpotOffsetVector);
    }
}