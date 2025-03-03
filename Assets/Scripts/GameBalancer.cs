using UnityEngine;
using RoofTops;

public class GameBalancer : MonoBehaviour
{
    [Header("Difficulty Control")]
    [Range(0.005f, 0.05f)]
    [Tooltip("How quickly difficulty increases per chunk")]
    public float progressionRate = 0.01f;
    
    [Header("Multiplier")]
    public float progressionMultiplier = 1f; // Use this to scale progressionRate
    
    [Header("Progression Curve")]
    [Range(0.1f, 1.0f)]
    [Tooltip("Controls how quickly difficulty ramps up. Lower values = slower initial progression")]
    public float progressionCurve = 0.5f; // Default to 0.5 for moderate progression
    
    [Header("Base Game Values")]
    public float initialSpeed = 6f;
    public float initialJumpForce = 8f;
    public float initialJumpGrowthRate = 0.1f; // growth rate for player jump
    
    [Header("Module Pool Settings")]
    public float initialGapSize = 2f;
    public float initialHeightVariation = 1f;
    
    [Header("Gap & Height Scaling")]
    [Range(0f, 100f)]
    [Tooltip("How much gaps should scale with difficulty (0% = no increase, 100% = full increase)")]
    public float gapScalingPercentage = 50f; // Default to 50% scaling
    
    [Range(0f, 100f)]
    [Tooltip("How much height variation should scale with difficulty (0% = no increase, 100% = full increase)")]
    public float heightScalingPercentage = 50f; // Default to 50% scaling
    
    [Header("Relief Settings")]
    // Percentage to reduce gaps, height, and jump growth rate for a margin (e.g., 0.1 = 10% reduction)
    public float reliefPercentage = 0.1f;

    [Header("Jump Force Tuning")]
    [Tooltip("Additional multiplier for jump force to ensure jumps are possible")]
    public float jumpForceMultiplier = 1.5f; // Boost jump force by 50% to ensure jumps are possible
    
    // References
    private UnifiedSpawnManager spawnManager;
    private GameManager gameManager;
    private PlayerController playerController;
    private ModulePool modulePool;
    
    private void Awake()
    {
        // Get references
        gameManager = GameManager.Instance;
        spawnManager = FindObjectOfType<UnifiedSpawnManager>();
        playerController = FindObjectOfType<PlayerController>();
        modulePool = ModulePool.Instance;
    }
    
    private void Start()
    {
        if (spawnManager != null)
        {
            // Optionally subscribe to chunk changes
            // spawnManager.onChunkChanged.AddListener(RecalculateGameBalance);
        }
        
        // Set initial values
        RecalculateGameBalance(0);
    }
    
    public void RecalculateGameBalance(int currentChunk)
    {
        // 1) Calculate difficulty factor with curved progression
        float rawDifficulty = currentChunk * progressionRate * progressionMultiplier;
        float curvedDifficulty = Mathf.Pow(rawDifficulty, progressionCurve);
        float difficultyFactor = 1f + curvedDifficulty;
        
        // 2) Calculate gap and height values using percentage-based scaling
        float gapScaleFactor = gapScalingPercentage / 100f; // Convert percentage to 0-1 range
        float heightScaleFactor = heightScalingPercentage / 100f; // Convert percentage to 0-1 range
        
        // Calculate how much to increase from initial values
        float gapIncrease = (difficultyFactor - 1f) * gapScaleFactor;
        float heightIncrease = (difficultyFactor - 1f) * heightScaleFactor;
        
        // Apply the scaled increases and relief percentage
        float newGapSize = initialGapSize * (1f + gapIncrease) * (1f - reliefPercentage);
        float newHeightVariation = initialHeightVariation * (1f + heightIncrease) * (1f - reliefPercentage);
        
        // 3) Calculate jump force with a more aggressive scaling to ensure jumps are possible
        float newJumpForce = initialJumpForce * difficultyFactor * jumpForceMultiplier;
        
        // 4) Scale other variables
        float newSpeed = initialSpeed * difficultyFactor;
        float newJumpGrowthRate = initialJumpGrowthRate * difficultyFactor * (1f - reliefPercentage);
        
        // 5) Apply the new values
        ApplyGameValues(newSpeed, newJumpForce, newGapSize, newHeightVariation, newJumpGrowthRate);
        
        // Detailed debug log to help diagnose scaling issues
        Debug.Log($"Chunk {currentChunk}: " +
            $"Raw={rawDifficulty:F2}, " +
            $"Curved={curvedDifficulty:F2}, " +
            $"Difficulty={difficultyFactor:F2}, " +
            $"Speed={newSpeed:F2}, " +
            $"Jump={newJumpForce:F2}, " +
            $"GrowthRate={newJumpGrowthRate:F2}, " +
            $"Gap={newGapSize:F2} (Initial={initialGapSize:F2}, Scaling={gapScalingPercentage}%, Increase={gapIncrease:F2}), " +
            $"Height={newHeightVariation:F2} (Initial={initialHeightVariation:F2}, Scaling={heightScalingPercentage}%, Increase={heightIncrease:F2})");
    }
    
    private void ApplyGameValues(float speed, float jumpForce, float gapSize, float heightVar, float jumpGrowthRate)
    {
        // Apply to GameManager
        if (gameManager != null)
        {
            gameManager.normalGameSpeed = speed;
        }
        
        // Apply to player
        if (playerController != null)
        {
            playerController.jumpForce = jumpForce;
            playerController.jumpForceGrowthRate = jumpGrowthRate;
        }
        
        // Apply to ModulePool
        if (modulePool != null)
        {
            modulePool.SetGameSpeed(speed);
            modulePool.constantGapRate = gapSize;
            modulePool.constantHeightRate = heightVar;
        }
    }
    
    // Removed UpdateDifficultyCurvesToMax since curves have been removed
} 