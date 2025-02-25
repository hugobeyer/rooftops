using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RoofTops
{
    /// <summary>
    /// Tracks in-game events and reports them to the AchievementSystem
    /// Attach this to the PlayerController or a dedicated GameObject
    /// </summary>
    public class AchievementTracker : MonoBehaviour
    {
        [Header("Achievement Tracking")]
        [SerializeField] private bool enableTracking = true;
        [SerializeField] private bool showDebugInfo = true;
        
        // Cached references
        private AchievementSystem achievementSystem;
        private PlayerController playerController;
        private GameManager gameManager;
        
        // Tracking data
        private bool isGrounded = true;
        private int airborneCollectibles = 0;
        private float distanceTracker = 0f;
        private int memoryCardsThisRun = 0;
        private float lastJumpStartX = 0f;
        private float maxJumpDistance = 0f;
        private float runStartDistance = 0f;
        
        private void Start()
        {
            // Find necessary components
            achievementSystem = AchievementSystem.Instance;
            playerController = GetComponent<PlayerController>();
            gameManager = GameManager.Instance;
            
            if (achievementSystem == null)
            {
                Debug.LogWarning("AchievementTracker: AchievementSystem not found, creating one...");
                GameObject achievementObj = new GameObject("AchievementSystem");
                achievementSystem = achievementObj.AddComponent<AchievementSystem>();
            }
            
            // Subscribe to game events
            if (gameManager != null)
            {
                gameManager.onGameStarted.AddListener(OnGameStarted);
            }
            
            // Connect player events if we're attached to the player
            ConnectPlayerEvents();
            
            if (showDebugInfo)
            {
                Debug.Log("AchievementTracker initialized and connected to achievement system");
            }
        }
        
        private void ConnectPlayerEvents()
        {
            // If we're not attached to the player, find it
            if (playerController == null)
            {
                playerController = FindFirstObjectByType<PlayerController>();
            }
            
            if (playerController != null)
            {
                // Try to find events via reflection since we might not have direct access
                // These should match the events you added to PlayerController
                var jumpEvent = playerController.GetType().GetField("onJump")?.GetValue(playerController) as UnityEngine.Events.UnityEvent;
                if (jumpEvent != null)
                {
                    jumpEvent.AddListener(OnPlayerJump);
                }
                
                var landEvent = playerController.GetType().GetField("onLand")?.GetValue(playerController) as UnityEngine.Events.UnityEvent<float>;
                if (landEvent != null)
                {
                    landEvent.AddListener(OnPlayerLand);
                }
                
                // If the player has direct methods to check state, use those
                // Otherwise we'll rely on our own logic
            }
        }
        
        private void Update()
        {
            if (!enableTracking || achievementSystem == null || gameManager == null) return;
            
            // Track distance-based metrics
            if (gameManager.HasGameStarted)
            {
                float currentDistance = gameManager.CurrentDistance;
                
                // Update max distance
                if (currentDistance > distanceTracker)
                {
                    // Report the new max distance
                    achievementSystem.SetMetric("max_distance", currentDistance);
                    distanceTracker = currentDistance;
                    
                    // Track distance milestones (every 100m)
                    if (Mathf.FloorToInt(currentDistance / 100) > Mathf.FloorToInt((currentDistance - 1) / 100))
                    {
                        int milestone = Mathf.FloorToInt(currentDistance / 100) * 100;
                        
                        // Track memory cards collected per 500m
                        if (milestone % 500 == 0 && milestone > 0)
                        {
                            achievementSystem.SetMetric("memcards_per_500m", memoryCardsThisRun);
                            memoryCardsThisRun = 0; // Reset for next 500m
                        }
                        
                        if (showDebugInfo)
                        {
                            Debug.Log($"Distance milestone reached: {milestone}m");
                        }
                    }
                }
            }
        }
        
        // Event handlers
        
        private void OnGameStarted()
        {
            runStartDistance = 0;
            distanceTracker = 0;
            memoryCardsThisRun = 0;
            maxJumpDistance = 0;
            airborneCollectibles = 0;
            
            if (showDebugInfo)
            {
                Debug.Log("Achievement tracking started for new run");
            }
        }
        
        private void OnPlayerJump()
        {
            // Track jump start position
            if (playerController != null)
            {
                lastJumpStartX = playerController.transform.position.x;
                isGrounded = false;
            }
        }
        
        private void OnPlayerLand(float landQuality)
        {
            // Calculate jump distance
            if (playerController != null)
            {
                float jumpDistance = Mathf.Abs(playerController.transform.position.x - lastJumpStartX);
                
                // Update max jump distance if this is a new record
                if (jumpDistance > maxJumpDistance)
                {
                    maxJumpDistance = jumpDistance;
                    achievementSystem.SetMetric("max_jump_distance", maxJumpDistance);
                    
                    if (showDebugInfo && jumpDistance > 3.0f)
                    {
                        Debug.Log($"New max jump distance: {maxJumpDistance:F2}m");
                    }
                }
                
                isGrounded = true;
                airborneCollectibles = 0; // Reset airborne collectibles counter
            }
        }
        
        // Methods for game objects to call when certain actions happen
        
        public void OnCollectibleCollected(string collectibleType)
        {
            // Track overall collectibles
            achievementSystem.IncrementMetric("total_collectibles");
            
            // Track specific collectible types
            switch (collectibleType.ToLower())
            {
                case "memorycard":
                case "memory":
                    memoryCardsThisRun++;
                    achievementSystem.IncrementMetric("total_memory_cards");
                    break;
                    
                case "bonus":
                case "coin":
                    if (!isGrounded)
                    {
                        // Player collected a bonus while in the air
                        airborneCollectibles++;
                        achievementSystem.SetMetric("consecutive_air_bonuses", airborneCollectibles);
                    }
                    break;
            }
        }
        
        public void OnObjectBroken(string objectType)
        {
            // Track broken objects
            achievementSystem.IncrementMetric("total_objects_broken");
            
            // Track specific object types
            switch (objectType.ToLower())
            {
                case "airconditioner":
                case "ac":
                    achievementSystem.IncrementMetric("air_conditioner_breaks");
                    break;
                    
                case "antenna":
                    achievementSystem.IncrementMetric("antennas_broken");
                    break;
                    
                case "window":
                    achievementSystem.IncrementMetric("windows_broken");
                    break;
            }
        }
        
        public void OnSpecialMove(string moveType)
        {
            // Track special moves like dash, wall-run, etc.
            switch (moveType.ToLower())
            {
                case "dash":
                    achievementSystem.IncrementMetric("dash_count");
                    break;
                    
                case "wallrun":
                    achievementSystem.IncrementMetric("wallrun_count");
                    break;
                    
                case "doublejump":
                    achievementSystem.IncrementMetric("double_jump_count");
                    break;
            }
        }
    }
} 