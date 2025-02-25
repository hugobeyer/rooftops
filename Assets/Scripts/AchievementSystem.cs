using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace RoofTops
{
    /// <summary>
    /// Central system for tracking, analyzing, and generating game achievements
    /// </summary>
    public class AchievementSystem : MonoBehaviour
    {
        public static AchievementSystem Instance { get; private set; }
        
        [Header("Achievement Settings")]
        [SerializeField] private bool enableDynamicAchievements = true;
        [SerializeField] private int maxDynamicAchievements = 5;
        [SerializeField] private bool showDebugInfo = true;
        
        // Achievement collections
        private List<Achievement> staticAchievements = new List<Achievement>();
        private List<Achievement> dynamicAchievements = new List<Achievement>();
        private List<Achievement> completedAchievements = new List<Achievement>();
        
        // Player metrics for achievement tracking
        private Dictionary<string, float> playerMetrics = new Dictionary<string, float>();
        
        // Events
        [HideInInspector] public UnityEvent<Achievement> onAchievementUnlocked = new UnityEvent<Achievement>();
        [HideInInspector] public UnityEvent<Achievement, float> onAchievementProgress = new UnityEvent<Achievement, float>();
        
        // References
        private DifficultyManager difficultyManager;
        private GameManager gameManager;
        
        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Initialize pre-defined achievements
            InitializeStaticAchievements();
        }
        
        private void Start()
        {
            // Get references to other systems
            difficultyManager = DifficultyManager.Instance;
            gameManager = GameManager.Instance;
            
            if (gameManager != null)
            {
                gameManager.onGameStarted.AddListener(OnGameStarted);
            }
            
            // Find and subscribe to player events
            StartCoroutine(ConnectToPlayerEvents());
            
            // Load saved achievements
            LoadAchievements();
            
            if (showDebugInfo)
            {
                Debug.Log("Achievement System initialized with " + staticAchievements.Count + " static achievements");
            }
        }
        
        private System.Collections.IEnumerator ConnectToPlayerEvents()
        {
            // Wait a frame to ensure player is initialized
            yield return null;
            
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                // Connect to player events
                var jumpEvent = player.GetType().GetField("onJump")?.GetValue(player) as UnityEvent;
                if (jumpEvent != null)
                {
                    jumpEvent.AddListener(OnPlayerJump);
                }
                
                var landEvent = player.GetType().GetField("onLand")?.GetValue(player) as UnityEvent<float>;
                if (landEvent != null)
                {
                    landEvent.AddListener(OnPlayerLand);
                }
                
                if (showDebugInfo)
                {
                    Debug.Log("Achievement System connected to player events");
                }
            }
        }
        
        private void InitializeStaticAchievements()
        {
            // Action-based achievements
            AddAchievement(new Achievement(
                "Triple Airborne",
                "Collect 3 bonus items without touching the ground",
                AchievementType.Action,
                "consecutive_air_bonuses",
                3
            ));
            
            AddAchievement(new Achievement(
                "Demolition Expert",
                "Break 5 air conditioners with Dash",
                AchievementType.Action,
                "air_conditioner_breaks",
                5
            ));
            
            // Progression-based achievements
            AddAchievement(new Achievement(
                "Memory Hunter",
                "Collect 200 memory cards in a single 500m run",
                AchievementType.Progression,
                "memcards_per_500m",
                200
            ));
            
            AddAchievement(new Achievement(
                "Marathon Runner",
                "Reach 1000m in a single run",
                AchievementType.Progression,
                "max_distance",
                1000
            ));
            
            // Skill-based achievements
            AddAchievement(new Achievement(
                "Precision Master",
                "Land perfectly (center) on 5 consecutive platforms",
                AchievementType.Skill,
                "consecutive_perfect_landings",
                5
            ));
            
            AddAchievement(new Achievement(
                "Death Defier",
                "Jump across a gap of at least 5m",
                AchievementType.Skill,
                "max_jump_distance",
                5
            ));
        }
        
        // Core achievement tracking methods
        
        public void IncrementMetric(string metricKey, float amount = 1)
        {
            if (!playerMetrics.ContainsKey(metricKey))
                playerMetrics[metricKey] = 0;
                
            playerMetrics[metricKey] += amount;
            
            // Check for achievement progress
            CheckAchievementProgress(metricKey, playerMetrics[metricKey]);
            
            if (showDebugInfo && metricKey != "distance") // Don't spam with distance updates
            {
                Debug.Log($"Metric {metricKey} increased to {playerMetrics[metricKey]}");
            }
        }
        
        public void SetMetric(string metricKey, float value)
        {
            playerMetrics[metricKey] = value;
            
            // Check for achievement progress
            CheckAchievementProgress(metricKey, value);
        }
        
        private void CheckAchievementProgress(string metricKey, float currentValue)
        {
            // Check both static and dynamic achievements
            foreach (var achievement in staticAchievements.Concat(dynamicAchievements))
            {
                if (achievement.IsCompleted) continue;
                
                if (achievement.MetricKey == metricKey)
                {
                    // Update progress
                    achievement.CurrentValue = currentValue;
                    
                    // Calculate progress percentage
                    float progressPercent = achievement.GetProgress();
                    
                    // Fire progress event
                    onAchievementProgress.Invoke(achievement, progressPercent);
                    
                    // Check if completed
                    if (currentValue >= achievement.TargetValue)
                    {
                        UnlockAchievement(achievement);
                    }
                }
            }
        }
        
        private void UnlockAchievement(Achievement achievement)
        {
            if (achievement.IsCompleted) return;
            
            // Mark as completed
            achievement.Complete();
            
            // Move to completed list
            if (staticAchievements.Contains(achievement))
                staticAchievements.Remove(achievement);
            else if (dynamicAchievements.Contains(achievement))
                dynamicAchievements.Remove(achievement);
                
            completedAchievements.Add(achievement);
            
            // Trigger events
            onAchievementUnlocked.Invoke(achievement);
            
            if (showDebugInfo)
            {
                Debug.Log($"<color=yellow>ACHIEVEMENT UNLOCKED:</color> {achievement.Title} - {achievement.Description}");
            }
            
            // Save achievements
            SaveAchievements();
        }
        
        // Event handlers
        
        private void OnGameStarted()
        {
            // Reset session-specific metrics
            ResetSessionMetrics();
        }
        
        private void OnPlayerJump()
        {
            // You can track jump-related metrics here
        }
        
        private void OnPlayerLand(float landQuality)
        {
            // Track landing quality for achievements
            if (landQuality > 0.8f)
            {
                IncrementMetric("consecutive_perfect_landings");
            }
            else
            {
                SetMetric("consecutive_perfect_landings", 0); // Reset counter for non-perfect landings
            }
        }
        
        private void ResetSessionMetrics()
        {
            // Reset session-only counters
            playerMetrics["consecutive_air_bonuses"] = 0;
            playerMetrics["consecutive_perfect_landings"] = 0;
            playerMetrics["memcards_per_500m"] = 0;
            playerMetrics["max_distance"] = 0;
            
            // Persistent metrics can stay
            // playerMetrics["total_air_conditioner_breaks"] remains unchanged
        }
        
        // Dynamic Achievement Generation
        
        private void GenerateDynamicAchievements()
        {
            // Clear previous dynamic achievements that weren't completed
            dynamicAchievements.RemoveAll(a => !a.IsCompleted);
            
            if (!enableDynamicAchievements || difficultyManager == null)
            {
                return;
            }
            
            // Gather player skill data
            Dictionary<string, float> skillData = GatherPlayerSkillData();
            
            // Analyze player's strengths and weaknesses
            (Dictionary<string, float> strengths, Dictionary<string, float> weaknesses) = AnalyzePlayerProfile(skillData);
            
            // Generate achievements based on player profile
            GenerateAchievementsBasedOnProfile(strengths, weaknesses);
            
            if (showDebugInfo)
            {
                Debug.Log($"Generated {dynamicAchievements.Count} dynamic achievements based on player profile");
            }
        }
        
        private Dictionary<string, float> GatherPlayerSkillData()
        {
            Dictionary<string, float> skillData = new Dictionary<string, float>();
            
            // Get metrics from DifficultyManager's skill metrics
            if (difficultyManager != null)
            {
                // Try to access playerSkillMetrics using reflection
                var metricsField = difficultyManager.GetType().GetField("playerSkillMetrics", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                
                if (metricsField != null)
                {
                    var metrics = metricsField.GetValue(difficultyManager) as Dictionary<string, float>;
                    if (metrics != null)
                    {
                        foreach (var kvp in metrics)
                        {
                            skillData[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }
            
            // Add our own tracked metrics
            foreach (var kvp in playerMetrics)
            {
                // Skip session-specific metrics
                if (kvp.Key.StartsWith("consecutive_") || kvp.Key == "max_distance")
                    continue;
                    
                // Only use metrics that are useful for analysis
                if (kvp.Value > 0)
                {
                    skillData[kvp.Key] = kvp.Value;
                }
            }
            
            return skillData;
        }
        
        private (Dictionary<string, float>, Dictionary<string, float>) AnalyzePlayerProfile(Dictionary<string, float> skillData)
        {
            Dictionary<string, float> strengths = new Dictionary<string, float>();
            Dictionary<string, float> weaknesses = new Dictionary<string, float>();
            
            // Define baseline expectations
            Dictionary<string, float> baselines = new Dictionary<string, float>
            {
                { "avgJumpDistance", 3.5f },
                { "perfectJumpRatio", 0.4f },
                { "avgDeaths", 1.0f },
                { "skillLevel", 0.5f },
                { "max_jump_distance", 4.5f },
                { "consecutive_perfect_landings", 3.0f },
                { "dash_count", 10.0f },
                { "total_objects_broken", 15.0f },
                { "air_conditioner_breaks", 3.0f }
            };
            
            // Compare player metrics against baselines
            foreach (var baseline in baselines)
            {
                string key = baseline.Key;
                float expectedValue = baseline.Value;
                
                if (skillData.TryGetValue(key, out float actualValue))
                {
                    // Calculate how much the player differs from baseline (normalized)
                    float ratio = actualValue / expectedValue;
                    
                    // For metrics where lower is better (like deaths), invert the ratio
                    if (key == "avgDeaths")
                    {
                        ratio = expectedValue / Mathf.Max(0.1f, actualValue);
                    }
                    
                    // Determine if this is a strength or weakness
                    if (ratio >= 1.2f) // 20% better than baseline
                    {
                        strengths[key] = ratio;
                    }
                    else if (ratio <= 0.8f) // 20% worse than baseline
                    {
                        weaknesses[key] = ratio;
                    }
                }
            }
            
            return (strengths, weaknesses);
        }
        
        private void GenerateAchievementsBasedOnProfile(Dictionary<string, float> strengths, Dictionary<string, float> weaknesses)
        {
            int achievementsAdded = 0;
            
            // Generate achievements based on strengths (challenge the player to excel even more)
            foreach (var strength in strengths)
            {
                if (achievementsAdded >= maxDynamicAchievements) break;
                
                switch (strength.Key)
                {
                    case "max_jump_distance":
                        // Player is good at long jumps, challenge them to jump even further
                        float currentBest = playerMetrics.ContainsKey("max_jump_distance") ? 
                            playerMetrics["max_jump_distance"] : 5.0f;
                        
                        float targetDistance = Mathf.Ceil(currentBest * 1.2f * 10) / 10; // 20% further, rounded to 1 decimal
                        
                        dynamicAchievements.Add(new Achievement(
                            "Super Jump",
                            $"Jump a gap of at least {targetDistance}m",
                            AchievementType.Dynamic,
                            "max_jump_distance",
                            targetDistance
                        ));
                        achievementsAdded++;
                        break;
                        
                    case "consecutive_perfect_landings":
                        // Player is precise, challenge them to be even more precise
                        float currentStreak = playerMetrics.ContainsKey("consecutive_perfect_landings") ? 
                            playerMetrics["consecutive_perfect_landings"] : 3.0f;
                        
                        int targetStreak = Mathf.RoundToInt(currentStreak * 1.5f); // 50% more precision landings
                        
                        dynamicAchievements.Add(new Achievement(
                            "Precision Expert",
                            $"Land perfectly on {targetStreak} consecutive platforms",
                            AchievementType.Dynamic,
                            "consecutive_perfect_landings",
                            targetStreak
                        ));
                        achievementsAdded++;
                        break;
                        
                    case "air_conditioner_breaks":
                        // Player enjoys breaking things, encourage more destruction
                        float currentBreaks = playerMetrics.ContainsKey("air_conditioner_breaks") ? 
                            playerMetrics["air_conditioner_breaks"] : 3.0f;
                        
                        int targetBreaks = Mathf.RoundToInt(currentBreaks * 1.3f); // 30% more breakage
                        
                        dynamicAchievements.Add(new Achievement(
                            "Demolition Pro",
                            $"Break {targetBreaks} air conditioners in a single run",
                            AchievementType.Dynamic,
                            "air_conditioner_breaks",
                            targetBreaks
                        ));
                        achievementsAdded++;
                        break;
                }
            }
            
            // Generate achievements based on weaknesses (help player improve areas they struggle with)
            foreach (var weakness in weaknesses)
            {
                if (achievementsAdded >= maxDynamicAchievements) break;
                
                switch (weakness.Key)
                {
                    case "avgJumpDistance":
                        // Player doesn't jump far, encourage them to try
                        dynamicAchievements.Add(new Achievement(
                            "Distance Jumper",
                            "Jump across a gap of at least 4m",
                            AchievementType.Dynamic,
                            "max_jump_distance",
                            4.0f
                        ));
                        achievementsAdded++;
                        break;
                        
                    case "dash_count":
                        // Player doesn't use dash much, encourage them to try it
                        dynamicAchievements.Add(new Achievement(
                            "Dash Master",
                            "Use dash 15 times in a single run",
                            AchievementType.Dynamic,
                            "dash_count",
                            15
                        ));
                        achievementsAdded++;
                        break;
                        
                    case "perfectJumpRatio":
                        // Player doesn't land perfectly often, encourage precision
                        dynamicAchievements.Add(new Achievement(
                            "Perfect Landing",
                            "Land perfectly on 3 consecutive platforms",
                            AchievementType.Dynamic,
                            "consecutive_perfect_landings",
                            3
                        ));
                        achievementsAdded++;
                        break;
                }
            }
            
            // Always add at least one "stretch goal" achievement based on the player's skill level
            if (achievementsAdded < maxDynamicAchievements)
            {
                float skillLevel = 0.5f;
                if (playerMetrics.ContainsKey("skillLevel"))
                {
                    skillLevel = playerMetrics["skillLevel"];
                }
                
                // Difficulty scales with skill
                int targetDistance = skillLevel < 0.3f ? 500 : 
                                    skillLevel < 0.6f ? 750 : 
                                    skillLevel < 0.8f ? 1000 : 1500;
                
                dynamicAchievements.Add(new Achievement(
                    "Distance Challenger",
                    $"Reach {targetDistance}m without dying more than twice",
                    AchievementType.Dynamic,
                    "flawless_distance",
                    targetDistance
                ));
            }
        }
        
        // Helper methods for dynamic achievements
        
        public float GetPlayerSkillLevel()
        {
            if (playerMetrics.ContainsKey("skillLevel"))
                return playerMetrics["skillLevel"];
                
            return 0.5f; // Default medium skill
        }
        
        public Dictionary<string, float> GetPlayerMetrics()
        {
            return new Dictionary<string, float>(playerMetrics);
        }
        
        // Helper methods
        
        private void AddAchievement(Achievement achievement)
        {
            staticAchievements.Add(achievement);
        }
        
        // Save/Load functionality (placeholder - will expand later)
        
        private void SaveAchievements()
        {
            // This will be implemented later
        }
        
        private void LoadAchievements()
        {
            // This will be implemented later
        }
        
        // Public getters for UI and other systems
        
        public List<Achievement> GetActiveAchievements()
        {
            return staticAchievements.Concat(dynamicAchievements).ToList();
        }
        
        public List<Achievement> GetCompletedAchievements()
        {
            return completedAchievements;
        }
    }
    
    // Achievement data classes
    
    [System.Serializable]
    public class Achievement
    {
        public string Id;
        public string Title;
        public string Description;
        public AchievementType Type;
        public string MetricKey;
        public float TargetValue;
        public float CurrentValue;
        public bool IsCompleted;
        public string CompletionDate;
        
        public Achievement(string title, string description, AchievementType type, string metricKey, float targetValue)
        {
            Id = Guid.NewGuid().ToString();
            Title = title;
            Description = description;
            Type = type;
            MetricKey = metricKey;
            TargetValue = targetValue;
            CurrentValue = 0;
            IsCompleted = false;
        }
        
        public void Complete()
        {
            IsCompleted = true;
            CurrentValue = TargetValue;
            CompletionDate = DateTime.UtcNow.ToString("o");
        }
        
        public float GetProgress()
        {
            return Mathf.Clamp01(CurrentValue / TargetValue);
        }
    }
    
    public enum AchievementType
    {
        Action,      // Based on specific actions (break 5 things)
        Progression, // Based on game progress (reach 500m)
        Skill,       // Based on player skill (perfect landings)
        Hidden,      // Not shown until unlocked
        Dynamic      // Generated by AI based on player behavior
    }
} 