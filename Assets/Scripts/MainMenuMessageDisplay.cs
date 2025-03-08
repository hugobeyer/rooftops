using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

namespace RoofTops
{
    public class MainMenuMessageDisplay : MonoBehaviour
    {
        [Header("Text Display")]
        [SerializeField] private TextMeshProUGUI messageText; // Reference to your TextMeshProUGUI component
        
        [Header("Animation Settings")]
        [SerializeField] private float fadeInTime = 0.5f;
        [SerializeField] private float displayTime = 3.0f;
        [SerializeField] private float fadeOutTime = 0.5f;
        
        [Header("Timing Settings")]
        [SerializeField] private float delayBetweenMessages = 1.5f; // Delay between messages
        [SerializeField] private float cycleEndDelay = 4.0f; // Delay after all messages before repeating
        [SerializeField] private float initialStartDelay = 0.5f; // Delay before starting messages
        
        [Header("Messages")]
        [SerializeField] private List<string> messages = new List<string>();
        [SerializeField] private int numberOfMessagesToShow = 5; // How many random messages to show each cycle
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private bool loopMessages = true;
        [SerializeField] private bool playOnStart = true;
        
        [Header("Component Control")]
        [SerializeField] private MonoBehaviour componentToToggle; // Component to activate/deactivate
        [SerializeField] private bool deactivateComponentBetweenMessages = true; // Whether to deactivate between messages
        
        private Coroutine messageCoroutine;
        private bool isDisplaying = false;
        
        // Keep track of previously shown messages to avoid repetition
        private List<string> recentlyShownMessages = new List<string>();
        
        private void Start()
        {
            if (messageText == null)
            {
                Debug.LogError("MainMenuMessageDisplay: No TextMeshProUGUI component assigned!");
                return;
            }
            
            // Initialize text
            messageText.text = "";
            messageText.color = new Color(textColor.r, textColor.g, textColor.b, 0); // Start transparent
            
            // Disable the component at start
            if (componentToToggle != null)
            {
                componentToToggle.enabled = false;
            }
            
            // Seed the random number generator based on time for better randomization
            Random.InitState((int)System.DateTime.Now.Ticks);
            
            if (playOnStart && messages.Count > 0)
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
            
            // Make sure component is disabled when stopping
            if (componentToToggle != null)
            {
                componentToToggle.enabled = false;
            }
            
            isDisplaying = false;
        }
        
        /// <summary>
        /// Improved method to select random messages from the list with true randomization
        /// </summary>
        private List<string> GetRandomMessages(int count)
        {
            // If we don't have enough messages, return what we have in a random order
            if (messages.Count <= count)
            {
                List<string> shuffledMessages = new List<string>(messages);
                ShuffleList(shuffledMessages);
                return shuffledMessages;
            }
            
            // Create a list to hold available messages
            // Exclude recently shown messages to prevent immediate repetition
            List<string> availableMessages = new List<string>();
            foreach (string msg in messages)
            {
                if (!recentlyShownMessages.Contains(msg))
                {
                    availableMessages.Add(msg);
                }
            }
            
            // If we've excluded too many messages, reset and use all messages
            if (availableMessages.Count < count)
            {
                availableMessages = new List<string>(messages);
                recentlyShownMessages.Clear();
            }
            
            // Create a shuffled copy of the available messages
            ShuffleList(availableMessages);
            
            // Take the first 'count' messages from the shuffled list
            List<string> selectedMessages = new List<string>();
            for (int i = 0; i < count && i < availableMessages.Count; i++)
            {
                selectedMessages.Add(availableMessages[i]);
                
                // Track this message to avoid immediate repetition in future cycles
                if (!recentlyShownMessages.Contains(availableMessages[i]))
                {
                    recentlyShownMessages.Add(availableMessages[i]);
                }
                
                // Keep the recently shown list from growing too large
                if (recentlyShownMessages.Count > messages.Count / 2)
                {
                    recentlyShownMessages.RemoveAt(0);
                }
            }
            
            // Log the selected messages for debugging
            Debug.Log($"Selected {selectedMessages.Count} random messages:");
            foreach (string msg in selectedMessages)
            {
                Debug.Log($" - \"{msg}\"");
            }
            
            return selectedMessages;
        }
        
        /// <summary>
        /// Fisher-Yates shuffle algorithm for properly randomizing a list
        /// </summary>
        private void ShuffleList<T>(List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
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
                
                if (messages.Count > 0)
                {
                    // Get a new set of random messages for this cycle
                    List<string> randomMessages = GetRandomMessages(numberOfMessagesToShow);
                    Debug.Log($"Displaying {randomMessages.Count} messages for this cycle");
                    
                    for (int i = 0; i < randomMessages.Count; i++)
                    {
                        Debug.Log($"Showing message {i+1}/{randomMessages.Count}: \"{randomMessages[i]}\"");
                        
                        // Activate component before showing message
                        if (componentToToggle != null)
                        {
                            componentToToggle.enabled = true;
                        }
                        
                        yield return StartCoroutine(DisplaySingleMessage(randomMessages[i]));
                        
                        // Deactivate component during delay if configured to do so
                        if (componentToToggle != null && deactivateComponentBetweenMessages)
                        {
                            componentToToggle.enabled = false;
                        }
                        
                        // Add delay between messages, but not after the last one
                        if (i < randomMessages.Count - 1)
                        {
                            yield return new WaitForSeconds(delayBetweenMessages);
                        }
                    }
                }
                
                // Ensure component is disabled during end delay
                if (componentToToggle != null)
                {
                    componentToToggle.enabled = false;
                }
                
                // Add delay at the end of a complete cycle before repeating
                yield return new WaitForSeconds(cycleEndDelay);
                
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
        private IEnumerator DisplaySingleMessage(string message)
        {
            // Skip empty messages
            if (string.IsNullOrEmpty(message))
            {
                yield break;
            }
            
            // Set the text and base color
            messageText.text = message;
            messageText.color = new Color(textColor.r, textColor.g, textColor.b, 0);
            
            // Wait a frame to ensure text is updated
            yield return null;
            
            // Fade in
            DOTween.To(() => messageText.color, x => messageText.color = x, 
                new Color(textColor.r, textColor.g, textColor.b, 1), fadeInTime);
            yield return new WaitForSeconds(fadeInTime);
            
            // Display for duration
            yield return new WaitForSeconds(displayTime);
            
            // Fade out
            DOTween.To(() => messageText.color, x => messageText.color = x, 
                new Color(textColor.r, textColor.g, textColor.b, 0), fadeOutTime);
            yield return new WaitForSeconds(fadeOutTime);
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