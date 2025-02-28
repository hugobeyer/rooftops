using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace RoofTops
{
    /// <summary>
    /// AI-driven difficulty manager that controls game difficulty based on distance traveled.
    /// Implements a chunk-based approach where difficulty parameters change at specific distance milestones.
    /// </summary>
    public class DifficultyManager : MonoBehaviour
    {
        public static DifficultyManager Instance { get; private set; }

        [Header("Difficulty Chunks")]
        [Tooltip("Size of each difficulty chunk in meters")]
        [SerializeField] private float chunkSize = 200f;
        
        [Header("Base Difficulty Parameters")]
        [SerializeField] private float baseGameSpeed = 6f;
        [SerializeField] private float baseGapSize = 2.5f;
        [SerializeField] private float baseHeightVariation = 1.0f;
        [SerializeField] private float baseTridotFrequency = 0.25f;
        [SerializeField] private float baseJumpPadFrequency = 0.005f;
        [SerializeField] private float basePropFrequency = 0.001f;
        
        [Header("Progression Curves")]
        [Tooltip("How game speed increases with distance")]
        [SerializeField] private AnimationCurve speedProgressionCurve = AnimationCurve.EaseInOut(0, 1, 10, 1.8f);
        
        [Tooltip("How gap size increases with distance")]
        [SerializeField] private AnimationCurve gapProgressionCurve = AnimationCurve.EaseInOut(0, 1, 10, 3.0f);
        
        [Tooltip("How height variation increases with distance")]
        [SerializeField] private AnimationCurve heightVariationCurve = AnimationCurve.EaseInOut(0, 1, 10, 2.5f);
        
        [Tooltip("How tridots frequency changes with distance")]
        [SerializeField] private AnimationCurve tridotFrequencyCurve = AnimationCurve.EaseInOut(0, 1, 10, 0.7f);
        
        [Tooltip("How jump pad frequency changes with distance")]
        [SerializeField] private AnimationCurve jumpPadFrequencyCurve = AnimationCurve.EaseInOut(0, 1, 10, 1.5f);
        
        [Tooltip("How prop frequency changes with distance")]
        [SerializeField] private AnimationCurve propFrequencyCurve = AnimationCurve.EaseInOut(0, 1, 10, 1.2f);
        
        [Header("AI Learning Settings")]
        [Tooltip("How quickly the AI adapts to player performance (0-1)")]
        [Range(0, 1)]
        [SerializeField] private float adaptationRate = 0.2f;
        
        [Tooltip("Whether AI should automatically adjust difficulty")]
        [SerializeField] private bool enableAIAdjustment = true;
        
        [Tooltip("Maximum number of deaths before AI eases difficulty")]
        [SerializeField] private int maxDeathsBeforeEasing = 3;

        [Header("Advanced Learning")]
        [Tooltip("Whether the system should self-tune its own parameters")]
        [SerializeField] private bool enableSelfTuning = true;
        
        [Tooltip("How often to analyze historical data (in seconds)")]
        [SerializeField] private float analysisInterval = 300f; // 5 minutes
        
        [Tooltip("Whether to use global patterns from all players")]
        [SerializeField] private bool useGlobalPatterns = true;
        
        [Header("Distance Tracking")]
        [SerializeField] private bool showDebugInfo = true;
        
        // Current state
        private float currentDistance = 0f;
        private int currentChunkIndex = 0;
        private int deathsInCurrentChunk = 0;
        private Dictionary<int, ChunkDifficultyData> chunkData = new Dictionary<int, ChunkDifficultyData>();
        
        // Difficulty parameters for the current chunk
        private float currentGameSpeed;
        private float currentGapSize;
        private float currentHeightVariation;
        private float currentTridotFrequency;
        private float currentJumpPadFrequency;
        private float currentPropFrequency;
        
        // References
        private ModulePool modulePool;
        private UnifiedSpawnManager spawnManager;
        private GameManager gameManager;
        
        // Player performance metrics
        private List<PlayerPerformanceData> performanceHistory = new List<PlayerPerformanceData>();
        private List<PlayerPerformanceData> currentSessionPerformance = new List<PlayerPerformanceData>();
        private float lastDeathDistance = 0f;
        private float sessionStartTime;
        private float lastChunkTransitionTime;
        private float lastAnalysisTime;
        
        // Advanced metrics tracking
        private int nearMisses = 0;
        private int perfectJumps = 0;
        private float totalJumpDistance = 0f;
        private int jumpCount = 0;
        private float lastJumpStartX = 0f;
        private Dictionary<string, float> playerSkillMetrics = new Dictionary<string, float>();

        // Database file paths
        private string performanceDbPath;
        private string learningDbPath;
        private string globalPatternsPath;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Setup database paths
            InitializeDatabasePaths();
            
            // Load any saved difficulty data
            LoadAllPerformanceData();
        }

        private void InitializeDatabasePaths()
        {
            string baseDir = Path.Combine(Application.persistentDataPath, "DifficultyData");
            if (!Directory.Exists(baseDir))
            {
                Directory.CreateDirectory(baseDir);
            }
            
            performanceDbPath = Path.Combine(baseDir, "performance_history.json");
            learningDbPath = Path.Combine(baseDir, "learning_parameters.json");
            globalPatternsPath = Path.Combine(baseDir, "global_patterns.json");
            
            if (showDebugInfo)
            {
                Debug.Log($"Database initialized at: {baseDir}");
            }
        }

        private void Start()
        {
            modulePool = ModulePool.Instance;
            if (modulePool == null)
            {
                Debug.LogError("DifficultyManager: ModulePool not found!");
                enabled = false;
                return;
            }
            
            gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                Debug.LogError("DifficultyManager: GameManager not found!");
                enabled = false;
                return;
            }
            
            // Try to find UnifiedSpawnManager
            spawnManager = FindObjectOfType<UnifiedSpawnManager>();
            
            // Initialize session
            sessionStartTime = Time.time;
            lastChunkTransitionTime = Time.time;
            lastAnalysisTime = Time.time;
            
            // Initialize with default values
            InitializeBaseParameters();
            
            // Subscribe to events
            gameManager.onGameStarted.AddListener(OnGameStarted);
            
            // Load optimized parameters if available
            LoadOptimizedParameters();
            
            // Try to find and subscribe to player input/jump events
            StartCoroutine(SubscribeToPlayerEvents());
        }

        private IEnumerator SubscribeToPlayerEvents()
        {
            // Wait a frame to ensure player is initialized
            yield return null;
            
            PlayerController player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                // Try to find jump events using reflection or direct reference
                // This is a placeholder - adjust based on your PlayerController implementation
                var jumpEvent = player.GetType().GetField("onJump")?.GetValue(player) as UnityEngine.Events.UnityEvent;
                if (jumpEvent != null)
                {
                    jumpEvent.AddListener(OnPlayerJump);
                }
                
                var landEvent = player.GetType().GetField("onLand")?.GetValue(player) as UnityEngine.Events.UnityEvent<float>;
                if (landEvent != null)
                {
                    landEvent.AddListener(OnPlayerLand);
                }
                
                // Alternatively, you can integrate with your existing events in PlayerController
                // player.onJump.AddListener(OnPlayerJump);
                // player.onLand.AddListener(OnPlayerLand);
                
                if (showDebugInfo)
                {
                    Debug.Log("Successfully subscribed to player events");
                }
            }
        }

        private void OnPlayerJump()
        {
            // Record jump start position
            PlayerController player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                lastJumpStartX = player.transform.position.x;
            }
        }

        private void OnPlayerLand(float landQuality)
        {
            // landQuality should be a value from 0-1 indicating how well the player landed
            // 1 = perfect center landing, 0 = edge landing
            
            PlayerController player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                float jumpDistance = Mathf.Abs(player.transform.position.x - lastJumpStartX);
                
                // Track jump statistics
                totalJumpDistance += jumpDistance;
                jumpCount++;
                
                // Detect perfect jumps
                if (landQuality > 0.8f)
                {
                    perfectJumps++;
                }
                
                // Detect near misses
                if (landQuality < 0.2f)
                {
                    nearMisses++;
                }
            }
        }

        private void InitializeBaseParameters()
        {
            currentGameSpeed = baseGameSpeed;
            currentGapSize = baseGapSize;
            currentHeightVariation = baseHeightVariation;
            currentTridotFrequency = baseTridotFrequency;
            currentJumpPadFrequency = baseJumpPadFrequency;
            currentPropFrequency = basePropFrequency;
            
            // Apply initial values
            ApplyCurrentParameters();
        }

        private void OnGameStarted()
        {
            // Reset values when game starts
            currentDistance = 0f;
            currentChunkIndex = 0;
            deathsInCurrentChunk = 0;
            lastDeathDistance = 0f;
            
            // Reset session performance
            currentSessionPerformance.Clear();
            
            // Reset jump metrics
            nearMisses = 0;
            perfectJumps = 0;
            totalJumpDistance = 0f;
            jumpCount = 0;
            
            // Start with base parameters
            InitializeBaseParameters();
        }

        private void Update()
        {
            if (gameManager == null || !gameManager.HasGameStarted)
                return;
            
            // Update current distance from GameManager
            currentDistance = gameManager.CurrentDistance;
            
            // Check if we've moved to a new chunk
            int newChunkIndex = Mathf.FloorToInt(currentDistance / chunkSize);
            if (newChunkIndex > currentChunkIndex)
            {
                // We've entered a new chunk
                OnEnterNewChunk(newChunkIndex);
            }
            
            // Check for player death
            PlayerController player = FindObjectOfType<PlayerController>();
            if (player != null && player.IsDead())
            {
                HandlePlayerDeath();
            }
            
            // Periodically analyze performance data
            if (enableSelfTuning && Time.time - lastAnalysisTime > analysisInterval)
            {
                AnalyzeHistoricalPerformance();
                SelfTuneParameters();
                lastAnalysisTime = Time.time;
            }
        }

        private void OnApplicationQuit()
        {
            // Save all data when application quits
            SaveAllPerformanceData();
        }

        private void OnEnterNewChunk(int newChunkIndex)
        {
            float previousChunkTime = Time.time - lastChunkTransitionTime;
            lastChunkTransitionTime = Time.time;
            
            // Log previous chunk performance
            LogChunkPerformance(currentChunkIndex, previousChunkTime, deathsInCurrentChunk);
            
            // Update chunk index
            currentChunkIndex = newChunkIndex;
            deathsInCurrentChunk = 0;
            
            // Generate or retrieve difficulty for this chunk
            ChunkDifficultyData chunkDifficulty;
            if (chunkData.ContainsKey(currentChunkIndex))
            {
                // Use existing data
                chunkDifficulty = chunkData[currentChunkIndex];
            }
            else
            {
                // Generate new difficulty data
                chunkDifficulty = GenerateChunkDifficulty(currentChunkIndex);
                chunkData[currentChunkIndex] = chunkDifficulty;
            }
            
            // Apply the chunk's difficulty settings
            ApplyChunkDifficulty(chunkDifficulty);
            
            // Log the transition
            if (showDebugInfo)
            {
                Debug.Log($"Entered new difficulty chunk {currentChunkIndex} at distance {currentDistance}m with speed {currentGameSpeed}, gap {currentGapSize}, height variation {currentHeightVariation}");
            }
        }

        private ChunkDifficultyData GenerateChunkDifficulty(int chunkIndex)
        {
            // Calculate base progression values based on curves
            float normalizedChunk = chunkIndex * 0.1f; // Map chunk index to curve's x-axis
            
            float speedMultiplier = speedProgressionCurve.Evaluate(normalizedChunk);
            float gapMultiplier = gapProgressionCurve.Evaluate(normalizedChunk);
            float heightMultiplier = heightVariationCurve.Evaluate(normalizedChunk);
            float tridotMultiplier = tridotFrequencyCurve.Evaluate(normalizedChunk);
            float jumpPadMultiplier = jumpPadFrequencyCurve.Evaluate(normalizedChunk);
            float propMultiplier = propFrequencyCurve.Evaluate(normalizedChunk);
            
            // Apply AI adjustments if enabled
            if (enableAIAdjustment && chunkIndex > 0)
            {
                (float speedAdjust, float gapAdjust, float heightAdjust) = CalculateAIAdjustments(chunkIndex);
                
                speedMultiplier *= speedAdjust;
                gapMultiplier *= gapAdjust;
                heightMultiplier *= heightAdjust;
            }
            
            // Apply learned patterns from historical data if available
            if (useGlobalPatterns && playerSkillMetrics.Count > 0)
            {
                AdjustBasedOnPlayerSkill(ref speedMultiplier, ref gapMultiplier, ref heightMultiplier);
            }
            
            // Calculate expected player jump capability at this point
            float expectedJumpForce = 7f + (chunkIndex * 0.5f); // Base jump force + growth over time
            
            // Ensure gap size never exceeds what the player can reasonably jump
            // A jump force of 7 can cross ~3 units, and 14 can cross ~6 units
            float maxJumpableGap = expectedJumpForce * 0.42f; // Conservative estimate of jump distance
            
            // Create chunk data
            ChunkDifficultyData newChunk = new ChunkDifficultyData
            {
                chunkIndex = chunkIndex,
                gameSpeed = baseGameSpeed * speedMultiplier,
                gapSize = Mathf.Min(baseGapSize * gapMultiplier, maxJumpableGap), // Cap based on jump capability
                heightVariation = baseHeightVariation * heightMultiplier,
                tridotFrequency = Mathf.Clamp(baseTridotFrequency * tridotMultiplier, 0.05f, 0.95f),
                jumpPadFrequency = Mathf.Clamp(baseJumpPadFrequency * jumpPadMultiplier, 0.005f, 0.05f),
                propFrequency = Mathf.Clamp(basePropFrequency * propMultiplier, 0.001f, 0.05f)
            };
            
            // Add some slight randomization to make each chunk feel unique
            ApplyRandomVariation(ref newChunk);
            
            if (showDebugInfo && chunkIndex % 3 == 0) // Only log every 3rd chunk to reduce spam
            {
                Debug.Log($"Generated chunk {chunkIndex} - Gap: {newChunk.gapSize:F2}m, Height: {newChunk.heightVariation:F2}m");
            }
            
            return newChunk;
        }

        private void AdjustBasedOnPlayerSkill(ref float speedMultiplier, ref float gapMultiplier, ref float heightMultiplier)
        {
            // Get player skill level
            float skillLevel = 0.5f; // Default medium
            
            if (playerSkillMetrics.TryGetValue("skillLevel", out float storedSkill))
            {
                skillLevel = storedSkill;
            }
            
            // Use player's typical jump distance to adjust gap sizes
            if (playerSkillMetrics.TryGetValue("avgJumpDistance", out float jumpDist))
            {
                // If player typically makes long jumps, slightly increase gap size
                float jumpFactor = Mathf.InverseLerp(2f, 5f, jumpDist);
                gapMultiplier *= (1.0f + (jumpFactor * 0.2f));
            }
            
            // Use player skill level to adjust overall difficulty
            float skillFactor = skillLevel - 0.5f; // -0.5 to 0.5
            
            // Advanced players get slightly harder, beginners get slightly easier
            speedMultiplier *= (1.0f + (skillFactor * 0.2f));
            gapMultiplier *= (1.0f + (skillFactor * 0.3f));
            heightMultiplier *= (1.0f + (skillFactor * 0.25f));
        }

        private void ApplyRandomVariation(ref ChunkDifficultyData chunk)
        {
            // Add small random variations to prevent predictability
            // Use ChunkIndex as seed to ensure consistent results for the same chunk
            System.Random random = new System.Random(chunk.chunkIndex * 1000);
            
            float RandomValue() => (float)random.NextDouble() * 0.2f + 0.9f; // 0.9 to 1.1 range
            
            chunk.gameSpeed *= RandomValue();
            chunk.gapSize *= RandomValue();
            chunk.heightVariation *= RandomValue();
            chunk.tridotFrequency = Mathf.Clamp(chunk.tridotFrequency * RandomValue(), 0.05f, 0.95f);
            chunk.jumpPadFrequency = Mathf.Clamp(chunk.jumpPadFrequency * RandomValue(), 0.05f, 0.95f);
            chunk.propFrequency = Mathf.Clamp(chunk.propFrequency * RandomValue(), 0.05f, 0.95f);
        }

        private (float speedAdjust, float gapAdjust, float heightAdjust) CalculateAIAdjustments(int chunkIndex)
        {
            // Start with neutral adjustment
            float speedAdjust = 1.0f;
            float gapAdjust = 1.0f;
            float heightAdjust = 1.0f;
            
            // Check if we have enough performance data
            if (performanceHistory.Count < 1)
                return (speedAdjust, gapAdjust, heightAdjust);
            
            // Analyze recent performance
            int deathsInRecentChunks = 0;
            float averageTimePerChunk = 0f;
            int samplesUsed = 0;
            
            // Look at the last few chunks
            int samplesToUse = Mathf.Min(3, performanceHistory.Count);
            for (int i = performanceHistory.Count - 1; i >= performanceHistory.Count - samplesToUse; i--)
            {
                deathsInRecentChunks += performanceHistory[i].deathCount;
                averageTimePerChunk += performanceHistory[i].completionTime;
                samplesUsed++;
            }
            
            averageTimePerChunk /= samplesUsed;
            
            // Adjust based on deaths
            if (deathsInRecentChunks > maxDeathsBeforeEasing)
            {
                // Player is struggling, make it easier BUT NEVER SLOW DOWN
                float easeFactor = Mathf.Clamp01(1.0f - (adaptationRate * (deathsInRecentChunks - maxDeathsBeforeEasing) / 3f));
                
                // Don't adjust speed downward, only other parameters
                // speedAdjust *= easeFactor; // REMOVED: Never slow down
                gapAdjust *= easeFactor;
                heightAdjust *= easeFactor;
            }
            else if (deathsInRecentChunks == 0 && averageTimePerChunk < 15f)
            {
                // Player is doing well, make it slightly harder
                float challengeFactor = 1.0f + (adaptationRate * 0.5f);
                
                speedAdjust *= challengeFactor; // Speed can increase
                gapAdjust *= challengeFactor;
                heightAdjust *= challengeFactor;
            }
            
            // Clamp adjustments to reasonable ranges
            // Only allow speed to increase, never decrease
            speedAdjust = Mathf.Clamp(speedAdjust, 1.0f, 1.3f); // Changed minimum from 0.7 to 1.0
            gapAdjust = Mathf.Clamp(gapAdjust, 0.7f, 1.3f);
            heightAdjust = Mathf.Clamp(heightAdjust, 0.7f, 1.3f);
            
            return (speedAdjust, gapAdjust, heightAdjust);
        }

        private void ApplyChunkDifficulty(ChunkDifficultyData chunk)
        {
            // Update current parameters, but NEVER decrease game speed
            // Only use the new speed if it's higher than current speed
            if (chunk.gameSpeed > currentGameSpeed)
            {
                currentGameSpeed = chunk.gameSpeed;
            }
            // For all other parameters, always use the new values
            currentGapSize = chunk.gapSize;
            currentHeightVariation = chunk.heightVariation;
            currentTridotFrequency = chunk.tridotFrequency;
            currentJumpPadFrequency = chunk.jumpPadFrequency;
            currentPropFrequency = chunk.propFrequency;
            
            // Apply parameters to game systems
            ApplyCurrentParameters();
        }

        private void ApplyCurrentParameters()
        {
            // Apply to ModulePool
            if (modulePool != null)
            {
                modulePool.SetGameSpeed(currentGameSpeed);
                modulePool.maxGapSize = currentGapSize;
                modulePool.maxHeightVariation = currentHeightVariation;
                
                // Add visible debug logging for these crucial parameters
                if (showDebugInfo)
                {
                    Debug.Log($"<color=yellow>DIFFICULTY UPDATE:</color> Gap Size = {currentGapSize:F2}m, Height Variation = {currentHeightVariation:F2}m");
                }
            }
            
            // Apply to SpawnManager if available
            if (spawnManager != null)
            {
                spawnManager.UpdateSpawnFrequencies(
                    currentTridotFrequency,
                    currentJumpPadFrequency,
                    currentPropFrequency
                );
            }
        }

        private void HandlePlayerDeath()
        {
            deathsInCurrentChunk++;
            lastDeathDistance = currentDistance;
            
            // Consider immediate difficulty adjustment for the current chunk if players die too much
            if (deathsInCurrentChunk >= maxDeathsBeforeEasing && enableAIAdjustment)
            {
                EaseDifficultyAfterDeaths();
            }
        }

        private void EaseDifficultyAfterDeaths()
        {
            // Get current chunk data
            if (!chunkData.TryGetValue(currentChunkIndex, out ChunkDifficultyData chunk))
                return;
                
            // Apply easing factor
            float easeFactor = Mathf.Clamp01(1.0f - (adaptationRate * 0.5f));
            
            // NEVER slow down game speed
            // chunk.gameSpeed *= easeFactor; // REMOVED: Never slow down
            chunk.gapSize *= easeFactor;
            chunk.heightVariation *= easeFactor;
            
            // Save changes
            chunkData[currentChunkIndex] = chunk;
            
            // Apply new settings
            ApplyChunkDifficulty(chunk);
            
            if (showDebugInfo)
            {
                Debug.Log($"Eased difficulty after {deathsInCurrentChunk} deaths. Speed UNCHANGED at {currentGameSpeed}, new gap: {currentGapSize}");
            }
        }

        private void LogChunkPerformance(int chunkIndex, float completionTime, int deaths)
        {
            // Calculate advanced metrics for this chunk
            float avgJumpDistance = (jumpCount > 0) ? totalJumpDistance / jumpCount : 0f;
            
            PlayerPerformanceData performance = new PlayerPerformanceData
            {
                chunkIndex = chunkIndex,
                completionTime = completionTime,
                deathCount = deaths,
                chunkDistance = chunkSize,
                timestamp = DateTime.UtcNow.ToString("o"),
                nearMisses = nearMisses,
                perfectJumps = perfectJumps,
                averageJumpDistance = avgJumpDistance
            };
            
            performanceHistory.Add(performance);
            currentSessionPerformance.Add(performance);
            
            // Reset counters for next chunk
            nearMisses = 0;
            perfectJumps = 0;
            totalJumpDistance = 0f;
            jumpCount = 0;
            
            // Keep history to a reasonable size
            if (performanceHistory.Count > 100)
            {
                performanceHistory.RemoveAt(0);
            }
            
            // Save performance data periodically
            if (currentSessionPerformance.Count % 5 == 0)
            {
                SaveAllPerformanceData();
            }
        }

        private Dictionary<int, float> GetSuccessScores(Dictionary<int, List<PlayerPerformanceData>> chunkPerformance)
        {
            Dictionary<int, float> successScores = new Dictionary<int, float>();
            foreach (var kvp in chunkPerformance)
            {
                // Score based on completion time and deaths
                float avgDeaths = (float)kvp.Value.Average(d => d.deathCount);
                float avgTime = (float)kvp.Value.Average(d => d.completionTime);
                
                // Lower deaths and faster times = better score
                successScores[kvp.Key] = 100f / (avgDeaths + 1) * (30f / (avgTime + 10f));
            }
            return successScores;
        }

        private void AnalyzeHistoricalPerformance()
        {
            if (performanceHistory.Count < 10)
                return; // Need enough data
                
            // Group performance data by chunk indices
            Dictionary<int, List<PlayerPerformanceData>> chunkPerformance = new Dictionary<int, List<PlayerPerformanceData>>();
            
            foreach (var data in performanceHistory)
            {
                if (!chunkPerformance.ContainsKey(data.chunkIndex))
                    chunkPerformance[data.chunkIndex] = new List<PlayerPerformanceData>();
                
                chunkPerformance[data.chunkIndex].Add(data);
            }
            
            // Find most successful chunk configurations using helper method
            Dictionary<int, float> successScores = GetSuccessScores(chunkPerformance);
            
            // Calculate player skill metrics
            CalculatePlayerSkillMetrics();
            
            // Use this data to adjust progression curves over time
            OptimizeProgressionCurves(successScores);
            
            if (showDebugInfo)
            {
                Debug.Log($"Performed learning analysis on {performanceHistory.Count} data points. Player skill level: {playerSkillMetrics["skillLevel"]:F2}");
            }
        }

        private void CalculatePlayerSkillMetrics()
        {
            if (performanceHistory.Count < 5)
                return;
                
            // Calculate overall skill level (0-1)
            float avgDeaths = (float)performanceHistory.Average(d => d.deathCount);
            float perfectJumpRatio = performanceHistory.Sum(d => d.perfectJumps) / 
                                    (float)Mathf.Max(1, performanceHistory.Sum(d => d.perfectJumps + d.nearMisses + d.deathCount));
            float avgJumpDistance = (float)performanceHistory.Average(d => d.averageJumpDistance);
            
            // Combine metrics - lower deaths, higher perfect jumps, higher jump distance = higher skill
            float skillLevel = Mathf.Clamp01(
                (1f / (avgDeaths + 1)) * 0.4f +
                perfectJumpRatio * 0.3f +
                Mathf.InverseLerp(2f, 5f, avgJumpDistance) * 0.3f
            );
            
            // Store metrics
            playerSkillMetrics["skillLevel"] = skillLevel;
            playerSkillMetrics["avgDeaths"] = avgDeaths;
            playerSkillMetrics["perfectJumpRatio"] = perfectJumpRatio;
            playerSkillMetrics["avgJumpDistance"] = avgJumpDistance;
        }

        private void OptimizeProgressionCurves(Dictionary<int, float> successScores)
        {
            if (successScores.Count < 3)
                return;
                
            // Find chunks with highest success scores
            var topChunks = successScores.OrderByDescending(kvp => kvp.Value).Take(3);
            
            // For each top chunk, try to replicate its difficulty pattern in future chunks
            foreach (var chunk in topChunks)
            {
                int chunkIndex = chunk.Key;
                
                // Get difficulty data for this successful chunk
                if (chunkData.TryGetValue(chunkIndex, out ChunkDifficultyData chunkDiff))
                {
                    // Record this pattern as a successful one for future reference
                    RecordSuccessfulPattern(chunkIndex, chunkDiff);
                }
            }
        }

        private void RecordSuccessfulPattern(int chunkIndex, ChunkDifficultyData chunkDiff)
        {
            GlobalPatternData patternData;
            
            // Load existing patterns if available
            if (File.Exists(globalPatternsPath))
            {
                try
                {
                    string json = File.ReadAllText(globalPatternsPath);
                    patternData = JsonConvert.DeserializeObject<GlobalPatternData>(json);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error loading pattern data: {e.Message}");
                    patternData = new GlobalPatternData();
                }
            }
            else
            {
                patternData = new GlobalPatternData();
            }
            
            // Add this pattern
            SuccessfulPattern pattern = new SuccessfulPattern
            {
                chunkIndex = chunkIndex,
                gameSpeed = chunkDiff.gameSpeed,
                gapSize = chunkDiff.gapSize,
                heightVariation = chunkDiff.heightVariation,
                timestamp = DateTime.UtcNow.ToString("o"),
                skillLevel = playerSkillMetrics.ContainsKey("skillLevel") ? playerSkillMetrics["skillLevel"] : 0.5f
            };
            
            patternData.successfulPatterns.Add(pattern);
            
            // Keep a reasonable size
            if (patternData.successfulPatterns.Count > 50)
            {
                patternData.successfulPatterns.RemoveAt(0);
            }
            
            // Save updated patterns
            try
            {
                string json = JsonConvert.SerializeObject(patternData, Formatting.Indented);
                File.WriteAllText(globalPatternsPath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving pattern data: {e.Message}");
            }
        }

        private void SelfTuneParameters()
        {
            if (performanceHistory.Count < 10)
                return; // Need enough data
                
            // Calculate variance in death counts across chunks
            float[] deathCounts = performanceHistory.Select(p => (float)p.deathCount).ToArray();
            float variance = CalculateVariance(deathCounts);
            
            // If variance is high, increase adaptation rate to respond faster
            // If variance is low (consistent experience), decrease adaptation rate
            if (variance > 3.0f)
                adaptationRate = Mathf.Min(adaptationRate + 0.02f, 0.8f);
            else if (variance < 1.0f)
                adaptationRate = Mathf.Max(adaptationRate - 0.01f, 0.1f);
                
            // Similarly adjust maxDeathsBeforeEasing based on player skill level
            float avgDeaths = (float)deathCounts.Average();
            maxDeathsBeforeEasing = Mathf.RoundToInt(Mathf.Clamp(avgDeaths * 0.6f, 1, 5));
            
            // Save the updated learning parameters
            SaveLearningParameters();
            
            if (showDebugInfo)
            {
                Debug.Log($"Self-tuned parameters: adaptationRate={adaptationRate:F2}, maxDeathsBeforeEasing={maxDeathsBeforeEasing}");
            }
        }

        private float CalculateVariance(float[] values)
        {
            float avg = values.Average();
            float sumSquaredDiff = values.Sum(v => (v - avg) * (v - avg));
            return sumSquaredDiff / values.Length;
        }

        private void SaveAllPerformanceData()
        {
            SavePerformanceData();
            SaveLearningParameters();
        }

        private void SavePerformanceData()
        {
            if (currentSessionPerformance.Count == 0)
                return;
            
            try
            {
                // Load existing performance database if available
                PerformanceDatabase database;
                
                if (File.Exists(performanceDbPath))
                {
                    string existingJson = File.ReadAllText(performanceDbPath);
                    database = JsonConvert.DeserializeObject<PerformanceDatabase>(existingJson);
                }
                else
                {
                    database = new PerformanceDatabase();
                }
                
                // Add current session data
                string sessionId = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                
                SessionData sessionData = new SessionData
                {
                    sessionId = sessionId,
                    startTime = sessionStartTime.ToString(),
                    endTime = Time.time.ToString(),
                    performanceData = currentSessionPerformance
                };
                
                database.sessions.Add(sessionData);
                
                // Keep database to a reasonable size (max 20 sessions)
                if (database.sessions.Count > 20)
                {
                    database.sessions.RemoveAt(0);
                }
                
                // Save to file
                string json = JsonConvert.SerializeObject(database, Formatting.Indented);
                File.WriteAllText(performanceDbPath, json);
                
                if (showDebugInfo)
                {
                    Debug.Log($"Saved performance data: {currentSessionPerformance.Count} entries for session {sessionId}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to save performance data: " + e.Message);
            }
        }

        private void SaveLearningParameters()
        {
            try
            {
                LearningParameters parameters = new LearningParameters
                {
                    adaptationRate = adaptationRate,
                    maxDeathsBeforeEasing = maxDeathsBeforeEasing,
                    skillMetrics = playerSkillMetrics,
                    timestamp = DateTime.UtcNow.ToString("o")
                };
                
                string json = JsonConvert.SerializeObject(parameters, Formatting.Indented);
                File.WriteAllText(learningDbPath, json);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to save learning parameters: " + e.Message);
            }
        }

        private void LoadAllPerformanceData()
        {
            LoadPerformanceData();
            LoadOptimizedParameters();
            LoadPlayerSkillMetrics();
        }

        private void LoadPerformanceData()
        {
            try
            {
                if (File.Exists(performanceDbPath))
                {
                    string json = File.ReadAllText(performanceDbPath);
                    PerformanceDatabase database = JsonConvert.DeserializeObject<PerformanceDatabase>(json);
                    
                    if (database != null && database.sessions != null && database.sessions.Count > 0)
                    {
                        // Get the most recent 5 sessions
                        int sessionsToUse = Mathf.Min(5, database.sessions.Count);
                        
                        performanceHistory.Clear();
                        
                        for (int i = database.sessions.Count - 1; i >= database.sessions.Count - sessionsToUse; i--)
                        {
                            performanceHistory.AddRange(database.sessions[i].performanceData);
                        }
                        
                        if (showDebugInfo)
                        {
                            Debug.Log($"Loaded performance data: {performanceHistory.Count} entries from {sessionsToUse} sessions");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to load performance data: " + e.Message);
                // Reset to empty if loading fails
                performanceHistory = new List<PlayerPerformanceData>();
            }
        }

        private void LoadOptimizedParameters()
        {
            try
            {
                if (File.Exists(learningDbPath))
                {
                    string json = File.ReadAllText(learningDbPath);
                    LearningParameters parameters = JsonConvert.DeserializeObject<LearningParameters>(json);
                    
                    if (parameters != null)
                    {
                        // Use the saved parameters
                        adaptationRate = parameters.adaptationRate;
                        maxDeathsBeforeEasing = parameters.maxDeathsBeforeEasing;
                        
                        if (showDebugInfo)
                        {
                            Debug.Log($"Loaded optimized parameters: adaptationRate={adaptationRate:F2}, maxDeathsBeforeEasing={maxDeathsBeforeEasing}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to load optimized parameters: " + e.Message);
            }
        }

        private void LoadPlayerSkillMetrics()
        {
            try
            {
                if (File.Exists(learningDbPath))
                {
                    string json = File.ReadAllText(learningDbPath);
                    LearningParameters parameters = JsonConvert.DeserializeObject<LearningParameters>(json);
                    
                    if (parameters != null && parameters.skillMetrics != null)
                    {
                        playerSkillMetrics = parameters.skillMetrics;
                        
                        // Initialize default skill metrics if they don't exist
                        if (!playerSkillMetrics.ContainsKey("skillLevel"))
                            playerSkillMetrics["skillLevel"] = 0.5f;
                        if (!playerSkillMetrics.ContainsKey("avgDeaths"))
                            playerSkillMetrics["avgDeaths"] = 1f;
                        if (!playerSkillMetrics.ContainsKey("perfectJumpRatio"))
                            playerSkillMetrics["perfectJumpRatio"] = 0f;
                        if (!playerSkillMetrics.ContainsKey("avgJumpDistance"))
                            playerSkillMetrics["avgJumpDistance"] = 3f;
                        
                        if (showDebugInfo)
                        {
                            Debug.Log($"Loaded player skill metrics, skill level: {playerSkillMetrics["skillLevel"]:F2}");
                        }
                    }
                    else
                    {
                        // Initialize with default values if parameters are null
                        InitializeDefaultSkillMetrics();
                    }
                }
                else
                {
                    // No existing data file, initialize with defaults
                    InitializeDefaultSkillMetrics();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to load player skill metrics: " + e.Message);
                // Reset to empty if loading fails then add default values
                InitializeDefaultSkillMetrics();
            }
        }

        private void InitializeDefaultSkillMetrics()
        {
            playerSkillMetrics = new Dictionary<string, float>
            {
                { "skillLevel", 0.5f },     // Medium skill
                { "avgDeaths", 1f },        // 1 death per chunk average
                { "perfectJumpRatio", 0f }, // No perfect jumps yet
                { "avgJumpDistance", 3f }   // Average jump distance
            };
            
            if (showDebugInfo)
            {
                Debug.Log("Initialized default player skill metrics");
            }
        }

        public float GetCurrentGameSpeed()
        {
            return currentGameSpeed;
        }

        public float GetCurrentGapSize()
        {
            return currentGapSize;
        }

        public float GetCurrentHeightVariation()
        {
            return currentHeightVariation;
        }

        public float GetCurrentTridotFrequency()
        {
            return currentTridotFrequency;
        }

        public float GetCurrentJumpPadFrequency()
        {
            return currentJumpPadFrequency;
        }

        public float GetCurrentPropFrequency()
        {
            return currentPropFrequency;
        }

        // Data structures
        [System.Serializable]
        public class ChunkDifficultyData
        {
            public int chunkIndex;
            public float gameSpeed;
            public float gapSize;
            public float heightVariation;
            public float tridotFrequency;
            public float jumpPadFrequency;
            public float propFrequency;
        }

        [System.Serializable]
        public class PlayerPerformanceData
        {
            public int chunkIndex;
            public float completionTime;
            public int deathCount;
            public float chunkDistance;
            public string timestamp;
            
            // Advanced metrics
            public int nearMisses;
            public int perfectJumps;
            public float averageJumpDistance;
        }
        
        [System.Serializable]
        public class PerformanceDatabase
        {
            public List<SessionData> sessions = new List<SessionData>();
        }
        
        [System.Serializable]
        public class SessionData
        {
            public string sessionId;
            public string startTime;
            public string endTime;
            public List<PlayerPerformanceData> performanceData = new List<PlayerPerformanceData>();
        }
        
        [System.Serializable]
        public class LearningParameters
        {
            public float adaptationRate;
            public int maxDeathsBeforeEasing;
            public Dictionary<string, float> skillMetrics = new Dictionary<string, float>();
            public string timestamp;
        }
        
        [System.Serializable]
        public class GlobalPatternData
        {
            public List<SuccessfulPattern> successfulPatterns = new List<SuccessfulPattern>();
        }
        
        [System.Serializable]
        public class SuccessfulPattern
        {
            public int chunkIndex;
            public float gameSpeed;
            public float gapSize;
            public float heightVariation;
            public string timestamp;
            public float skillLevel;
        }
    }

    // Extension method for UnifiedSpawnManager
    public static class SpawnManagerExtensions
    {
        public static void UpdateSpawnFrequencies(this UnifiedSpawnManager spawnManager, float tridotFrequency, float jumpPadFrequency, float propFrequency)
        {
            // Access the private fields via reflection (only if necessary)
            var tridotField = spawnManager.GetType().GetField("tridotFrequency", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            var jumpPadField = spawnManager.GetType().GetField("jumpPadFrequency", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            var propField = spawnManager.GetType().GetField("propFrequency", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);

            if (tridotField != null) tridotField.SetValue(spawnManager, tridotFrequency);
            if (jumpPadField != null) jumpPadField.SetValue(spawnManager, jumpPadFrequency);
            if (propField != null) propField.SetValue(spawnManager, propFrequency);
        }
    }
} 
