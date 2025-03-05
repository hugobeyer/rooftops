using UnityEngine;
using RoofTops;
using System.Collections;

public class NoiseMovement : MonoBehaviour
{
    // Singleton instance
    private static NoiseMovement _instance;
    public static NoiseMovement Instance { get { return _instance; } }
    
    [Header("Camera States")]
    [Space(5)]
    [Header("Initial Camera (Start State)")]
    public Vector3 initialCameraPosition = new Vector3(108.4f, 22.5f, -20.6f); // World position
    public Vector3 initialLookAtPosition = new Vector3(0, 0, 10); // World position to look at
    public float initialFOV = 16f;

    [Header("Mid Camera (Transition State)")]
    public Vector3 midCameraPosition = new Vector3(50f, 15f, 0f); // World position
    public Vector3 midLookAtPosition = new Vector3(0, 0, 20); // World position to look at
    public float midFOV = 12f;

    [Header("Final Camera (Default/Gameplay State)")]
    public Vector3 finalCameraPosition = new Vector3(0f, 10f, -10f); // World position
    public Vector3 finalLookAtPosition = new Vector3(0, 0, 10); // World position to look at
    public float finalFOV = 32f;

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
    public bool useInitialBlend = true;
    public float blendDuration = 3f;
    private float blendTimer = 0f;

    [Header("Blend Timings")]
    public float midFOVDelay = 1.5f;
    public float finalFOVDelay = 4f;
    public float midBlendDuration = 0.5f;
    public float finalBlendDuration = 2f;
    
    [Header("Look At Settings")]
    public bool useInitialLookAtOffset = true;
    
    // Target tracking
    public float baseLookSpeed = 5f;
    private Vector3 targetPosition = Vector3.zero;

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
    public float deathBlendDuration = 1.5f;
    public float deathFOV = 65f;
    public float deathNoiseIntensity = 0.5f;
    public Vector3 deathCameraPosition = new Vector3(0, 5, -10); // World position
    public Vector3 deathLookAtPosition = new Vector3(0, 0, 0); // World position
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

    [Header("Initial Game Settings")]
    public float initialNoiseMultiplier = 0.5f;  // Controls noise intensity before game starts
    private float currentNoiseMultiplier = 1f;

    private enum FOVStage
    {
        Initial,
        Mid,
        Final
    }

    private void Awake()
    {
        // Singleton pattern with DontDestroyOnLoad
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Initialize other components
        InitializeComponents();
    }
    
    private void InitializeComponents()
    {
        // Initialize any required components here
        Camera cam = GetComponent<Camera>();
        if (cam != null)
        {
            // No need to manually set the tag - this should be set in the editor
            // cam.tag = "MainCamera";
        }
    }

    private void Start()
    {
        // Initialize camera to initial state
        transform.position = initialCameraPosition;
        
        // Set initial rotation to look at the initial look target
        Vector3 lookDirection = initialLookAtPosition - transform.position;
        if (lookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection);
            Debug.Log("NoiseMovement: Set initial rotation to look at: " + initialLookAtPosition);
        }
        
        // Set initial FOV
        Camera cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.fieldOfView = initialFOV;
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
            // Skip normal updates during death
            return;
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

        // Determine camera position and look target based on game state
        Vector3 currentCameraPosition;
        Vector3 currentLookAtPosition;
        float currentFOV;

        if (!GameManager.Instance.HasGameStarted)
        {
            // Initial state
            currentCameraPosition = initialCameraPosition;
            currentLookAtPosition = initialLookAtPosition;
            currentFOV = initialFOV;
            currentNoiseMultiplier = initialNoiseMultiplier;
        }
        else
        {
            // Game has started - determine which phase we're in
            if (currentNoiseMultiplier < 1f)
            {
                currentNoiseMultiplier = Mathf.MoveTowards(currentNoiseMultiplier, 1f, Time.deltaTime);
            }

            switch (currentFOVStage)
            {
                case FOVStage.Initial:
                    currentCameraPosition = initialCameraPosition;
                    currentLookAtPosition = initialLookAtPosition;
                    currentFOV = initialFOV;
                    
                    // Check if it's time to transition to mid stage
                    fovBlendTimer += Time.deltaTime;
                    if (fovBlendTimer >= midFOVDelay)
                    {
                        currentFOVStage = FOVStage.Mid;
                        fovBlendTimer = 0f;
                        lookAtBlendTimer = 0f;
                    }
                    break;
                    
                case FOVStage.Mid:
                    // Blend from initial to mid
                    fovBlendTimer += Time.deltaTime;
                    float toMidBlend = Mathf.SmoothStep(0f, 1f, fovBlendTimer / midBlendDuration);
                    
                    currentCameraPosition = Vector3.Lerp(initialCameraPosition, midCameraPosition, toMidBlend);
                    currentLookAtPosition = Vector3.Lerp(initialLookAtPosition, midLookAtPosition, toMidBlend);
                    currentFOV = Mathf.Lerp(initialFOV, midFOV, toMidBlend);
                    
                    // Check if it's time to transition to final stage
                    if (fovBlendTimer >= midBlendDuration && fovBlendTimer >= finalFOVDelay)
                    {
                        currentFOVStage = FOVStage.Final;
                        fovBlendTimer = 0f;
                        lookAtBlendTimer = 0f;
                    }
                    break;
                    
                case FOVStage.Final:
                    // Blend from mid to final
                    fovBlendTimer += Time.deltaTime;
                    float toFinalBlend = Mathf.SmoothStep(0f, 1f, fovBlendTimer / finalBlendDuration);
                    
                    currentCameraPosition = Vector3.Lerp(midCameraPosition, finalCameraPosition, toFinalBlend);
                    currentLookAtPosition = Vector3.Lerp(midLookAtPosition, finalLookAtPosition, toFinalBlend);
                    currentFOV = Mathf.Lerp(midFOV, finalFOV, toFinalBlend);
                    break;
                    
                default:
                    // Fallback to final values
                    currentCameraPosition = finalCameraPosition;
                    currentLookAtPosition = finalLookAtPosition;
                    currentFOV = finalFOV;
                    break;
            }
        }

        // Apply noise to camera position
        transform.position = currentCameraPosition + noisePosition;
        
        // Set FOV
        Camera cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.fieldOfView = currentFOV;
        }

        // Calculate rotation to look at target
        Vector3 lookDirection = currentLookAtPosition - transform.position;
        if (lookDirection != Vector3.zero)
        {
            targetRotation = Quaternion.LookRotation(lookDirection);
            
            // Apply rotation noise
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
            
            // Apply rotations (noise remains but could be modified)
            yawRotation.eulerAngles = new Vector3(0, yawAngle, 0);
            pitchRotation.eulerAngles = new Vector3(pitchAngle, 0, 0);
            rollRotation.eulerAngles = new Vector3(0, 0, rollAngle);
            
            // Apply rotation with noise
            transform.rotation = targetRotation * yawRotation * pitchRotation * rollRotation;
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

        // 2) Use fixed world positions
        // Fixed world position for the death camera
        Vector3 finalPosition = deathCameraPosition;
        
        // Fixed world position for the look target
        Vector3 lookTargetPosition = deathLookAtPosition;
        
        // Calculate rotation to look at the target
        Vector3 lookDirection = lookTargetPosition - finalPosition;
        Quaternion finalRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        
        // Final FOV
        float finalFOV = deathFOV;

        // 3) Blend smoothly
        while (elapsed < deathBlendDuration)
        {
            float t = elapsed / deathBlendDuration;
            
            // Apply easing
            t = Mathf.SmoothStep(0, 1, t);
            
            // Lerp position
            transform.position = Vector3.Lerp(deathStartPosition, finalPosition, t);
            
            // Lerp rotation
            transform.rotation = Quaternion.Slerp(deathStartRotation, finalRotation, t);
            
            // Lerp FOV
            GetComponent<Camera>().fieldOfView = Mathf.Lerp(deathStartFOV, finalFOV, t);
            
            // If you want to keep rotation NOISE while transitioning, you can skip it or adjust it:
            // Apply reduced noise during transition
            float transitionNoiseMultiplier = Mathf.Lerp(1f, 0.1f, t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Ensure we end at exactly the target values
        transform.position = finalPosition;
        transform.rotation = finalRotation;
        GetComponent<Camera>().fieldOfView = finalFOV;
        currentNoiseMultiplier = deathNoiseIntensity;
        secondIntensity = deathNoiseIntensity;
        
        isInDeathTransition = false;
    }

    public void ResetCamera()
    {
        // Reset flags
        isPlayerDead = false;
        isInDeathTransition = false;
        
        // Hide death visual
        if (deathVisualObject != null)
        {
            deathVisualObject.SetActive(false);
        }
        
        // Restore target reference - use Vector3 position instead of Transform
        targetPosition = Vector3.zero;
        
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
        
        // Reset noise intensities
        currentNoiseMultiplier = 1f;
        secondIntensity = 0.5f;
        
        // Reset camera FOV to default
        Camera cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.fieldOfView = initialFOV;
        }
        
        Debug.Log("Camera reset complete");
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

    private Transform FindPlayer()
    {
        // First try to get the player from SceneReferenceManager
        GameObject playerObj = SceneReferenceManager.Instance.GetPlayer();
        if (playerObj != null)
        {
            Debug.Log("NoiseMovement: Found player through SceneReferenceManager");
            return playerObj.transform;
        }
        
        // Try to find the player in the scene by tag
        playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            Debug.Log("NoiseMovement: Found player by tag");
            return playerObj.transform;
        }
        
        // If not found by tag, try to find by PlayerController component
        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            Debug.Log("NoiseMovement: Found player by PlayerController component");
            return playerController.transform;
        }
        
        Debug.LogWarning("NoiseMovement: Could not find player in the scene");
        return null;
    }

    // Update the GetTarget method to return a Vector3 instead of Transform
    public Vector3 GetTargetPosition()
    {
        // Return the appropriate look target based on current state
        if (!GameManager.Instance.HasGameStarted)
        {
            return initialLookAtPosition;
        }
        else
        {
            switch (currentFOVStage)
            {
                case FOVStage.Initial:
                    return initialLookAtPosition;
                case FOVStage.Mid:
                    return midLookAtPosition;
                case FOVStage.Final:
                    return finalLookAtPosition;
                default:
                    return finalLookAtPosition;
            }
        }
    }

    // Replace the old GetTarget method that returned a Transform
    // This is just a compatibility method that returns null
    // to ensure existing code doesn't break
    public Transform GetTarget()
    {
        return null;
    }
} 