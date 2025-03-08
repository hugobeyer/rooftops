using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

namespace RoofTops
{
    public class GoalMessageDisplay : MonoBehaviour
    {
        [Header("Text Display")]
        [SerializeField] private TextMeshProUGUI messageText; // Reference to your TextMeshProUGUI component
        
        [Header("Animation Settings")]
        [SerializeField] private float fadeInTime = 0.5f;
        [SerializeField] private float displayTime = 3.0f;
        [SerializeField] private float fadeOutTime = 0.5f;
        [SerializeField] private float delayBetweenGoals = 0.2f;
        
        [Header("Timing Settings")]
        [SerializeField] private float cycleEndDelay = 4.0f; // Delay after showing all goals before repeating
        [SerializeField] private float initialStartDelay = 0.5f; // Delay before starting
        
        [Header("Goal Display Settings")]
        [SerializeField] private Color goalTextColor = Color.yellow;
        [SerializeField] private string goalPrefix = "GOAL: ";
        [SerializeField] private GameObject goalVisualEffect; // Object to activate when showing goals
        [SerializeField] private float visualEffectDeactivationDelay = 0.5f; // How long to wait after goals end before deactivating
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private bool loopGoals = true;
        
        private Coroutine goalCoroutine;
        private bool isDisplaying = false;
        
        // Goal categories to display
        [SerializeField] private string[] goalCategories = new string[] { "Reach the distance of ", "The Tridots, catch ", "Memory Cards, collect " };
        
        private void Start()
        {
            if (messageText == null)
            {
                Debug.LogError("GoalMessageDisplay: No TextMeshProUGUI component assigned!");
                return;
            }
            
            // Initialize text
            messageText.text = "";
            messageText.color = new Color(goalTextColor.r, goalTextColor.g, goalTextColor.b, 0); // Start transparent
            
            // Make sure visual effect is initially disabled
            if (goalVisualEffect != null)
            {
                goalVisualEffect.SetActive(false);
            }
            
            if (playOnStart)
            {
                StartGoalDisplay();
            }
        }
        
        /// <summary>
        /// Starts or restarts the goal display sequence
        /// </summary>
        public void StartGoalDisplay()
        {
            StopGoalDisplay();
            goalCoroutine = StartCoroutine(DisplayGoalsCoroutine());
        }
        
        /// <summary>
        /// Stops the goal display sequence
        /// </summary>
        public void StopGoalDisplay()
        {
            if (goalCoroutine != null)
            {
                StopCoroutine(goalCoroutine);
                goalCoroutine = null;
            }
            
            // Make sure text is hidden when stopping
            if (messageText != null)
            {
                DOTween.Kill(messageText);
                messageText.color = new Color(goalTextColor.r, goalTextColor.g, goalTextColor.b, 0);
            }
            
            // Ensure visual effect is disabled
            if (goalVisualEffect != null)
            {
                goalVisualEffect.SetActive(false);
            }
            
            isDisplaying = false;
        }
        
        /// <summary>
        /// Coroutine that displays each goal in sequence with fading effects
        /// </summary>
        private IEnumerator DisplayGoalsCoroutine()
        {
            isDisplaying = true;
            
            // Add a small delay before starting to ensure everything is initialized
            yield return new WaitForSeconds(initialStartDelay);
            yield return null; // Wait one frame to ensure everything is ready
            
            while (isDisplaying)
            {
                // Make sure text is reset to empty at the start of each cycle
                messageText.text = "";
                messageText.color = new Color(goalTextColor.r, goalTextColor.g, goalTextColor.b, 0);
                yield return null; // Wait one frame to ensure the empty text is applied
                
                if (GoalAchievementManager.Instance != null)
                {
                    // Activate goal visual effect for ALL goal messages
                    if (goalVisualEffect != null)
                    {
                        goalVisualEffect.SetActive(true);
                    }
                    
                    bool hasShownAnyGoals = false;
                    
                    foreach (string category in goalCategories)
                    {
                        GoalCategory goalCategory = GoalAchievementManager.Instance.GetGoalCategory(category);
                        if (goalCategory != null && goalCategory.isEnabled)
                        {
                            string goalText = FormatGoalMessage(goalCategory);
                            yield return StartCoroutine(DisplaySingleGoal(goalText));
                            hasShownAnyGoals = true;
                        }
                    }
                    
                    // Deactivate goal visual effect after all goals
                    if (goalVisualEffect != null && hasShownAnyGoals)
                    {
                        yield return new WaitForSeconds(visualEffectDeactivationDelay);
                        goalVisualEffect.SetActive(false);
                    }
                }
                
                // Add delay at the end of a complete cycle before repeating
                yield return new WaitForSeconds(cycleEndDelay);
                
                // If not looping, break after displaying all goals once
                if (!loopGoals)
                {
                    isDisplaying = false;
                    break;
                }
            }
        }
        
        /// <summary>
        /// Displays a single goal message with fade effects
        /// </summary>
        private IEnumerator DisplaySingleGoal(string message)
        {
            // Skip empty messages
            if (string.IsNullOrEmpty(message))
            {
                yield break;
            }
            
            // Set the text and base color
            messageText.text = message;
            messageText.color = new Color(goalTextColor.r, goalTextColor.g, goalTextColor.b, 0);
            
            // Wait a frame to ensure text is updated
            yield return null;
            
            // Fade in
            DOTween.To(() => messageText.color, x => messageText.color = x, 
                new Color(goalTextColor.r, goalTextColor.g, goalTextColor.b, 1), fadeInTime);
            yield return new WaitForSeconds(fadeInTime);
            
            // Display for duration
            yield return new WaitForSeconds(displayTime);
            
            // Fade out
            DOTween.To(() => messageText.color, x => messageText.color = x, 
                new Color(goalTextColor.r, goalTextColor.g, goalTextColor.b, 0), fadeOutTime);
            yield return new WaitForSeconds(fadeOutTime);
            
            // Wait between goals
            yield return new WaitForSeconds(delayBetweenGoals);
        }
        
        /// <summary>
        /// Formats a goal message based on the goal category
        /// </summary>
        private string FormatGoalMessage(GoalCategory category)
        {
            object goalValue = category.GetCurrentGoalValue();
            if (goalValue == null) return goalPrefix + category.displayName + ": No goal set";
            
            string valueText = goalValue.ToString();
            
            // Add unit suffix if available
            if (!string.IsNullOrEmpty(category.unitSuffix))
            {
                valueText += category.unitSuffix;
            }
            
            return goalPrefix + category.displayName + ": " + valueText;
        }
        
        private void OnDestroy()
        {
            // Clean up DOTween animations
            if (messageText != null)
            {
                DOTween.Kill(messageText);
            }
        }
    }
} 