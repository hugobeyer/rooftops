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
        [SerializeField] private bool showDebugInfo = true;
        
        // Achievement collections
        private List<Achievement> staticAchievements = new List<Achievement>();
        private List<Achievement> completedAchievements = new List<Achievement>();
        
        // Player metrics for achievement tracking
        private Dictionary<string, float> playerMetrics = new Dictionary<string, float>();
        
        // Events
        [HideInInspector] public UnityEvent<Achievement> onAchievementUnlocked = new UnityEvent<Achievement>();
        [HideInInspector] public UnityEvent<Achievement, float> onAchievementProgress = new UnityEvent<Achievement, float>();
        
        // References
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
                "Death Defier",
                "Jump across a gap of at least 5m",
                AchievementType.Skill,
                "max_jump_distance",
                5
            ));
            
            // Add more static achievements here
            AddAchievement(new Achievement(
                "Super Jump",
                "Jump a gap of at least 7m",
                AchievementType.Skill,
                "max_jump_distance",
                7
            ));
            
            AddAchievement(new Achievement(
                "Demolition Pro",
                "Break 10 air conditioners in a single run",
                AchievementType.Action,
                "air_conditioner_breaks",
                10
            ));
            
            AddAchievement(new Achievement(
                "Dash Master",
                "Use dash 15 times in a single run",
                AchievementType.Action,
                "dash_count",
                15
            ));
            
            AddAchievement(new Achievement(
                "Distance Challenger",
                "Reach 1500m without dying more than twice",
                AchievementType.Progression,
                "flawless_distance",
                1500
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
            // Check static achievements
            foreach (var achievement in staticAchievements)
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
        
        private void ResetSessionMetrics()
        {
            // Reset session-only counters
            playerMetrics["consecutive_air_bonuses"] = 0;
            playerMetrics["memcards_per_500m"] = 0;
            playerMetrics["max_distance"] = 0;
            
            // Persistent metrics can stay
            // playerMetrics["total_air_conditioner_breaks"] remains unchanged
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
            return new List<Achievement>(staticAchievements);
        }
        
        public List<Achievement> GetCompletedAchievements()
        {
            return completedAchievements;
        }
        
        public Dictionary<string, float> GetPlayerMetrics()
        {
            return new Dictionary<string, float>(playerMetrics);
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
        Hidden       // Not shown until unlocked
    }
} 