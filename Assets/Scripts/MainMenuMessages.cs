using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

namespace RoofTops
{
    public class MainMenuMessages : MonoBehaviour
    {
        [Header("Text Display")]
        [SerializeField] private TextMeshProUGUI messageText; // Reference to your TextMeshProUGUI component
        
        [Header("Animation Settings")]
        [SerializeField] private float fadeInTime = 0.5f;
        [SerializeField] private float displayTime = 3.0f;
        [SerializeField] private float fadeOutTime = 0.5f;
        [SerializeField] private float delayBetweenMessages = 0.2f;
        
        [Header("Timing Settings")]
        [SerializeField] private float delayBetweenSections = 2.0f; // Delay between messages and goals
        [SerializeField] private float globalEndDelay = 4.0f; // Delay after all messages before repeating
        [SerializeField] private float initialStartDelay = 0.5f; // Delay before starting messages
        [SerializeField] private float customMessagesDelay = 1.5f; // Specific delay between custom messages
        
        [Header("Messages")]
        [SerializeField] private List<string> messages = new List<string>();
        [SerializeField] private int numberOfMessagesToShow = 5; // How many random messages to show each cycle
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private bool loopMessages = true;
        [SerializeField] private bool playOnStart = true;
        
        [Header("Goal Integration")]
        [SerializeField] private bool showGoals = true;
        [SerializeField] private Color goalTextColor = Color.yellow;
        [SerializeField] private string goalPrefix = "GOAL: ";
        [SerializeField] private GameObject goalVisualEffect; // Object to activate when showing goals
        [SerializeField] private float visualEffectDeactivationDelay = 0.5f; // How long to wait after goal message ends before deactivating
        
        private Coroutine messageCoroutine;
        private bool isDisplaying = false;
        
        // Goal categories to display
        private readonly string[] goalCategories = new string[] { "Distance", "Tridots", "Memcard" };
        
        private void Start()
        {
            if (messageText == null)
            {
                Debug.LogError("MainMenuMessages: No TextMeshProUGUI component assigned!");
                return;
            }
            
            // Initialize text
            messageText.text = "";
            messageText.color = new Color(textColor.r, textColor.g, textColor.b, 0); // Start transparent
            
            // Make sure visual effect is initially disabled
            if (goalVisualEffect != null)
            {
                goalVisualEffect.SetActive(false);
            }
            
            if (playOnStart && (messages.Count > 0 || showGoals))
            {
                StartMessageDisplay();
            }
        }
        
        /// <summary>
        /// Starts or restarts the message display sequence
        /// </summary>
        public void StartMessageDisplay()
        {
            StopMessageDisplay();
            messageCoroutine = StartCoroutine(DisplayMessagesCoroutine());
        }
        
        /// <summary>
        /// Stops the message display sequence
        /// </summary>
        public void StopMessageDisplay()
        {
            if (messageCoroutine != null)
            {
                StopCoroutine(messageCoroutine);
                messageCoroutine = null;
            }
            
            // Make sure text is hidden when stopping
            if (messageText != null)
            {
                DOTween.Kill(messageText);
                messageText.color = new Color(textColor.r, textColor.g, textColor.b, 0);
            }
            
            // Ensure visual effect is disabled
            if (goalVisualEffect != null)
            {
                goalVisualEffect.SetActive(false);
            }
            
            isDisplaying = false;
        }
        
        /// <summary>
        /// Selects random messages from the list
        /// </summary>
        private List<string> GetRandomMessages(int count)
        {
            // If we don't have enough messages, return what we have
            if (messages.Count <= count)
            {
                return new List<string>(messages);
            }
            
            // Create a copy of the messages list that we can modify
            List<string> messagesCopy = new List<string>(messages);
            List<string> selectedMessages = new List<string>();
            
            // Select 'count' random messages
            for (int i = 0; i < count; i++)
            {
                if (messagesCopy.Count == 0)
                    break;
                
                int randomIndex = Random.Range(0, messagesCopy.Count);
                selectedMessages.Add(messagesCopy[randomIndex]);
                messagesCopy.RemoveAt(randomIndex);
            }
            
            return selectedMessages;
        }
        
        /// <summary>
        /// Coroutine that displays each message in sequence with fading effects
        /// </summary>
        private IEnumerator DisplayMessagesCoroutine()
        {
            isDisplaying = true;
            
            // Add a small delay before starting to ensure everything is initialized
            yield return new WaitForSeconds(initialStartDelay);
            yield return null; // Wait one frame to ensure everything is ready
            
            while (isDisplaying)
            {
                // Make sure text is reset to empty at the start of each cycle
                messageText.text = "";
                messageText.color = new Color(textColor.r, textColor.g, textColor.b, 0);
                yield return null; // Wait one frame to ensure the empty text is applied
                
                // FIRST show random custom messages
                if (messages.Count > 0)
                {
                    List<string> randomMessages = GetRandomMessages(numberOfMessagesToShow);
                    
                    for (int i = 0; i < randomMessages.Count; i++)
                    {
                        yield return StartCoroutine(DisplaySingleMessage(randomMessages[i], textColor, false));
                        
                        // Add custom delay between messages, but not after the last one
                        if (i < randomMessages.Count - 1)
                        {
                            yield return new WaitForSeconds(customMessagesDelay);
                        }
                    }
                    
                    // Add delay between custom messages and goals, but only if we showed any messages
                    if (randomMessages.Count > 0 && showGoals && GoalAchievementManager.Instance != null)
                    {
                        yield return new WaitForSeconds(delayBetweenSections);
                    }
                }
                
                // THEN show goal messages if enabled
                if (showGoals && GoalAchievementManager.Instance != null)
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
                            yield return StartCoroutine(DisplaySingleMessage(goalText, goalTextColor, true));
                            hasShownAnyGoals = true;
                        }
                    }
                    
                    // Deactivate goal visual effect after all goals
                    if (goalVisualEffect != null)
                    {
                        yield return new WaitForSeconds(visualEffectDeactivationDelay);
                        goalVisualEffect.SetActive(false);
                    }
                }
                
                // Add global delay at the end of a complete cycle before repeating
                yield return new WaitForSeconds(globalEndDelay);
                
                // If not looping, break after displaying all messages once
                if (!loopMessages)
                {
                    isDisplaying = false;
                    break;
                }
            }
        }
        
        /// <summary>
        /// Displays a single message with fade effects
        /// </summary>
        private IEnumerator DisplaySingleMessage(string message, Color color, bool isGoalMessage)
        {
            // Skip empty messages
            if (string.IsNullOrEmpty(message))
            {
                yield break;
            }
            
            // Set the text and base color
            messageText.text = message;
            messageText.color = new Color(color.r, color.g, color.b, 0);
            
            // Wait a frame to ensure text is updated
            yield return null;
            
            // Fade in
            DOTween.To(() => messageText.color, x => messageText.color = x, 
                new Color(color.r, color.g, color.b, 1), fadeInTime);
            yield return new WaitForSeconds(fadeInTime);
            
            // Display for duration
            yield return new WaitForSeconds(displayTime);
            
            // Fade out
            DOTween.To(() => messageText.color, x => messageText.color = x, 
                new Color(color.r, color.g, color.b, 0), fadeOutTime);
            yield return new WaitForSeconds(fadeOutTime);
            
            // General delay between messages is now handled separately for goals and custom messages
            if (isGoalMessage)
            {
                // For goal messages, use the original delay
                yield return new WaitForSeconds(delayBetweenMessages);
            }
            // Custom message delays are handled in the main loop
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
        
        /// <summary>
        /// Adds a new message to the list
        /// </summary>
        public void AddMessage(string message)
        {
            messages.Add(message);
        }
        
        /// <summary>
        /// Clears all messages
        /// </summary>
        public void ClearMessages()
        {
            messages.Clear();
        }
        
        /// <summary>
        /// Sets a completely new list of messages
        /// </summary>
        public void SetMessages(List<string> newMessages)
        {
            messages = new List<string>(newMessages);
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