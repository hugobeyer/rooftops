using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
         /////////////////////////////////// DO NOT DARE TO CREATE ANY HARD CODED MESSAGES, DONT YOU DARE!!!!
namespace RoofTops
{
    [System.Serializable]
    public enum BarAnimationStyle
    {
        FadeInOut,       // Simple fade in and out
        Pulse,           // Fade in and pulse a few times before fading out
        Blink,           // Blink on and off
        Warning,         // Warning-style flashing
        PopAndTransition, // POP in white, blink quickly, then transition to selected color
        PopAndFade        // POP with full alpha, then fade to selected alpha
    }

    [System.Serializable]
    public class GameMessage
    {
        public string messageID;
        public string messageText = "Message text here";
        public Color messageColor = Color.white;
        [Range(0.5f, 5f)]
        public float displayDuration = 2.0f;
        public bool showVisualBar = false;
        public BarAnimationStyle barAnimationStyle = BarAnimationStyle.FadeInOut;
        [Tooltip("Color of the visual bar (uses the assigned material but tints it with this color)")]
        public Color barColor = Color.white; // New property for bar color
        [Tooltip("Optional separate duration for the visual bar (if <= 0, uses the text duration)")]
        [Range(0f, 5f)]
        public float barDuration = 0f; // Separate duration for the bar (0 means use text duration)
    }

    public class GameMessageDisplay : MonoBehaviour
    {
        public static GameMessageDisplay Instance { get; private set; }

        [Header("References")]
        [SerializeField] private GameObject textObject;
        [SerializeField] private GameObject visualBarObject; // Visual bar element
        
        [Header("Visual Bar Settings")]
        [SerializeField] private Material visualBarMaterial; // Custom material for the visual bar
        [Tooltip("How many times the bar should pulse or blink per second")]
        [SerializeField] private float pulseFrequency = 2f; // Pulses per second
        [Tooltip("How many times the warning effect should flash per second")]
        [SerializeField] private float warningFrequency = 4f; // Flashes per second

        private TextMeshPro textMesh;
        private Renderer visualBarRenderer;
        private Material instancedBarMaterial; // Instanced material to avoid affecting other objects

        [Header("Common Message Settings")]
        [SerializeField] private float defaultDisplayTime = 2.0f;
        [SerializeField] private float fadeInTime = 0.25f;
        [SerializeField] private float fadeOutTime = 0.5f;
        [SerializeField] private Color defaultTextColor = Color.white;
        [SerializeField] private Color warningTextColor = new Color(1f, 0.5f, 0f); // Orange
        [SerializeField] private Color errorTextColor = Color.red;

        [Header("Message Library")]
        [SerializeField] private List<GameMessage> messageLibrary = new List<GameMessage>();
        
        // Default messages for backward compatibility
        [Header("Default Messages")]
        [Tooltip("Message to show when player doesn't have enough tridots")]
        [SerializeField] private string notEnoughTridotsMessage = "Need {0} tridots to dash!";
        [Tooltip("Message to show when player collects a tridot")]
        [SerializeField] private string tridotCollectedMessage = "Tridot collected! ({0})";

        private Coroutine activeMessageCoroutine;
        private Coroutine activeProgressBarCoroutine;
        
        // Dictionary for quick message lookup by ID
        private Dictionary<string, GameMessage> messageDict = new Dictionary<string, GameMessage>();

        [Header("Debug Testing")]
        [Tooltip("Enable keyboard shortcuts for testing animations")]
        [SerializeField] private bool enableKeyboardTesting = true;
        [Tooltip("Test message to display when using keyboard shortcuts")]
        [SerializeField] private string testMessage = "Test Message";
         /////////////////////////////////// DO NOT DARE TO CREATE ANY HARD CODED MESSAGES, DONT YOU DARE!!!!
        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Get the TextMeshPro component from the textObject
            if (textObject != null)
            {
                textMesh = textObject.GetComponent<TextMeshPro>();
                if (textMesh == null)
                {
                    Debug.LogError("TextMeshPro component not found on the assigned text object!");
                }
            }
            else
            {
                Debug.LogError("Text object not assigned to GameMessageDisplay!");
            }

            // Get the Renderer component from the visualBarObject
            if (visualBarObject != null)
            {
                visualBarRenderer = visualBarObject.GetComponent<Renderer>();
                if (visualBarRenderer == null)
                {
                    Debug.LogError("Renderer component not found on the assigned visual bar object!");
                }
                else
                {
                    // Apply custom material if assigned
                    if (visualBarMaterial != null)
                    {
                        // Create an instance of the material to avoid affecting other objects using the same material
                        instancedBarMaterial = new Material(visualBarMaterial);
                        visualBarRenderer.material = instancedBarMaterial;
                    }
                    
                    // Set initial alpha to 0 (invisible)
                    Color barColor = visualBarRenderer.material.color;
                    barColor.a = 0f;
                    visualBarRenderer.material.color = barColor;
                }
            }

            // Hide message and visual bar at start
            if (textMesh != null)
            {
                Color startColor = textMesh.color;
                startColor.a = 0f;
                textMesh.color = startColor;
            }

            if (visualBarRenderer != null)
            {
                Color barColor = visualBarRenderer.material.color;
                barColor.a = 0f;
                visualBarRenderer.material.color = barColor;
            }
            
            // Build message dictionary for quick lookup
            BuildMessageDictionary();
        }
                 /////////////////////////////////// DO NOT DARE TO CREATE ANY HARD CODED MESSAGES, DONT YOU DARE!!!!
        private void BuildMessageDictionary()
        {
            messageDict.Clear();
                     /////////////////////////////////// DO NOT DARE TO CREATE ANY HARD CODED MESSAGES, DONT YOU DARE!!!!
            // Add all messages from the library to the dictionary
            foreach (GameMessage message in messageLibrary)
            {
                if (!string.IsNullOrEmpty(message.messageID) && !messageDict.ContainsKey(message.messageID))
                {
                    messageDict.Add(message.messageID, message);
                }
                else if (string.IsNullOrEmpty(message.messageID))
                {
                    Debug.LogWarning("GameMessageDisplay: Message with empty ID found in library");
                }
                else
                {
                    Debug.LogWarning($"GameMessageDisplay: Duplicate message ID '{message.messageID}' found in library");
                }
            }
/////////////////////////////////// DO NOT DARE TO CREATE ANY HARD CODED MESSAGES, DONT YOU DARE!!!!
         /////////////////////////////////// DO NOT DARE TO CREATE ANY HARD CODED MESSAGES, DONT YOU DARE!!!!
         /////////////////////////////////// DO NOT DARE TO CREATE ANY HARD CODED MESSAGES, DONT YOU DARE!!!!
////////////////////////////////// DO NOT DARE TO CREATE ANY HARD CODED MESSAGES, DONT YOU DARE!!!!           
/////////////////////////////////// DO NOT DARE TO CREATE ANY HARD CODED MESSAGES, DONT YOU DARE!!!!
/////////////////////////////////// DO NOT DARE TO CREATE ANY HARD CODED MESSAGES, DONT YOU DARE!!!!
/////////////////////////////////// DO NOT DARE TO CREATE ANY HARD CODED MESSAGES, DONT YOU DARE!!!!    
         /////////////////////////////////// DO NOT DARE TO CREATE ANY HARD CODED MESSAGES, DONT YOU DARE!!!!
         ///////////////////dont create any hardcoded messages, dont you dare!!!!
        }

        #region Message Display Methods

        /// <summary>
        /// Shows a message by its ID from the message library
        /// </summary>
        public void ShowMessageByID(string messageID, params object[] formatArgs)
        {
            GameMessage message;
            
            // Try to get the message from the dictionary
            if (!messageDict.TryGetValue(messageID, out message))
            {
                // Message ID not found, use the first message in the library as fallback
                if (messageLibrary.Count > 0)
                {
                    message = messageLibrary[0];
                    Debug.LogWarning($"GameMessageDisplay: Message ID '{messageID}' not found, using first message as fallback");
                }
                else
                {
                    // No messages in library, show a default error message
                    Debug.LogWarning($"GameMessageDisplay: Message ID '{messageID}' not found and no fallback available");
                    ShowMessage($"[{messageID}]", defaultDisplayTime, defaultTextColor);
                    return;
                }
            }
            
            string formattedText;
            
            // Check if formatArgs is empty or if the message doesn't contain any format placeholders
            if (formatArgs.Length == 0 || !message.messageText.Contains("{"))
            {
                formattedText = message.messageText;
            }
            else
            {
                try
                {
                    formattedText = string.Format(message.messageText, formatArgs);
                }
                catch (System.FormatException)
                {
                    Debug.LogWarning($"GameMessageDisplay: Format error for message ID '{messageID}'. Using unformatted text.");
                    formattedText = message.messageText;
                }
            }
            
            if (message.showVisualBar)
            {
                // Use separate bar duration if specified, otherwise use text duration
                float barDuration = message.barDuration > 0 ? message.barDuration : message.displayDuration;
                ShowMessageWithVisualBar(formattedText, message.barAnimationStyle, message.barColor, message.displayDuration, barDuration, message.messageColor);
            }
            else
            {
                ShowMessage(formattedText, message.displayDuration, message.messageColor);
            }
        }

        /// <summary>
        /// Shows a message to the player for the default duration
        /// </summary>
        public void ShowMessage(string message)
        {
            ShowMessage(message, defaultDisplayTime, defaultTextColor);
        }

        /// <summary>
        /// Shows a message to the player for the specified duration
        /// </summary>
        public void ShowMessage(string message, float duration)
        {
            ShowMessage(message, duration, defaultTextColor);
        }

        /// <summary>
        /// Shows a warning message (orange color)
        /// </summary>
        public void ShowWarning(string message, float duration = -1)
        {
            if (duration < 0) duration = defaultDisplayTime;
            ShowMessage(message, duration, warningTextColor);
        }

        /// <summary>
        /// Shows an error message (red color)
        /// </summary>
        public void ShowError(string message, float duration = -1)
        {
            if (duration < 0) duration = defaultDisplayTime;
            ShowMessage(message, duration, errorTextColor);
        }

        /// <summary>
        /// Shows a message with the specified color and duration
        /// </summary>
        public void ShowMessage(string message, float duration, Color color)
        {
            if (textMesh == null) return;
            
            // Stop any active message display
            if (activeMessageCoroutine != null)
            {
                StopCoroutine(activeMessageCoroutine);
                activeMessageCoroutine = null;
            }

            // Reset text alpha to 0 to ensure proper fade-in even if already showing
            Color resetColor = color;
            resetColor.a = 0f;
            textMesh.color = resetColor;

            // Start new message display
            activeMessageCoroutine = StartCoroutine(DisplayMessageCoroutine(message, duration, color));
        }

        #endregion

        #region Legacy Methods (for backward compatibility)

        /// <summary>
        /// Shows the "Not Enough Tridots" message
        /// </summary>
        public void ShowNotEnoughTridotsMessage(int requiredAmount, float duration = -1)
        {
            ShowMessageByID("ZERO_TRIDOTS", requiredAmount);
        }

        /// <summary>
        /// Shows the "Tridot Collected" message
        /// </summary>
        public void ShowTridotCollectedMessage(int totalAmount, float duration = -1)
        {
            ShowMessageByID("TRIDOT_COLLECTED", totalAmount);
        }

        #endregion

        #region Visual Bar Methods

        /// <summary>
        /// Shows the visual bar with the specified animation style and color
        /// </summary>
        public void ShowVisualBar(BarAnimationStyle animationStyle, Color barColor, float duration = -1)
        {
            if (visualBarRenderer == null) return;
            if (duration < 0) duration = defaultDisplayTime;

            // Stop any active visual bar display
            if (activeProgressBarCoroutine != null)
            {
                StopCoroutine(activeProgressBarCoroutine);
                activeProgressBarCoroutine = null;
            }

            // Reset bar alpha to 0 to ensure proper fade-in even if already showing
            Color currentColor = visualBarRenderer.material.color;
            // Apply the new RGB values but keep alpha at 0
            Color newColor = barColor;
            newColor.a = 0f;
            visualBarRenderer.material.color = newColor;

            // Start new visual bar display with the selected animation style
            activeProgressBarCoroutine = StartCoroutine(DisplayVisualBarCoroutine(animationStyle, duration));
        }

        /// <summary>
        /// Shows the visual bar with the default color
        /// </summary>
        public void ShowVisualBar(BarAnimationStyle animationStyle, float duration = -1)
        {
            ShowVisualBar(animationStyle, Color.white, duration);
        }

        /// <summary>
        /// Shows both a message and visual bar with custom colors and separate durations
        /// </summary>
        public void ShowMessageWithVisualBar(string message, BarAnimationStyle animationStyle, Color barColor, float textDuration = -1, float barDuration = -1, Color? textColor = null)
        {
            if (textDuration < 0) textDuration = defaultDisplayTime;
            if (barDuration < 0) barDuration = textDuration; // Use text duration if bar duration not specified
            Color messageTextColor = textColor ?? defaultTextColor;
            
            ShowMessage(message, textDuration, messageTextColor);
            ShowVisualBar(animationStyle, barColor, barDuration);
        }

        /// <summary>
        /// Shows both a message and visual bar with default bar color
        /// </summary>
        public void ShowMessageWithVisualBar(string message, BarAnimationStyle animationStyle, float textDuration = -1, Color? textColor = null)
        {
            ShowMessageWithVisualBar(message, animationStyle, Color.white, textDuration, textDuration, textColor);
        }

        #endregion

        #region Coroutines

        private IEnumerator DisplayMessageCoroutine(string message, float duration, Color color)
        {
            // Set the message text
            textMesh.text = message;
            
            // Set base color (with alpha 0)
            Color displayColor = color;
            displayColor.a = 0f;
            textMesh.color = displayColor;

            // Fade in
            float elapsedTime = 0f;
            while (elapsedTime < fadeInTime)
            {
                displayColor.a = Mathf.Lerp(0f, 1f, elapsedTime / fadeInTime);
                textMesh.color = displayColor;
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            displayColor.a = 1f;
            textMesh.color = displayColor;

            // Display for duration
            yield return new WaitForSeconds(duration);

            // Fade out
            elapsedTime = 0f;
            while (elapsedTime < fadeOutTime)
            {
                displayColor.a = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutTime);
                textMesh.color = displayColor;
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            displayColor.a = 0f;
            textMesh.color = displayColor;

            activeMessageCoroutine = null;
        }

        private IEnumerator DisplayVisualBarCoroutine(BarAnimationStyle animationStyle, float duration)
        {
            // Get the material
            Material barMaterial = visualBarRenderer.material;
            
            // Get current color but keep RGB values, only modify alpha
            Color barColor = barMaterial.color;
            barColor.a = 0f;
            barMaterial.color = barColor;

            // Animation based on selected style
            switch (animationStyle)
            {
                case BarAnimationStyle.FadeInOut:
                    yield return StartCoroutine(FadeInOutAnimation(barMaterial, duration));
                    break;
                    
                case BarAnimationStyle.Pulse:
                    yield return StartCoroutine(PulseAnimation(barMaterial, duration));
                    break;
                    
                case BarAnimationStyle.Blink:
                    yield return StartCoroutine(BlinkAnimation(barMaterial, duration));
                    break;
                    
                case BarAnimationStyle.Warning:
                    yield return StartCoroutine(WarningAnimation(barMaterial, duration));
                    break;
                    
                case BarAnimationStyle.PopAndTransition:
                    yield return StartCoroutine(PopAndTransitionAnimation(barMaterial, duration));
                    break;
                    
                case BarAnimationStyle.PopAndFade:
                    yield return StartCoroutine(PopAndFadeAnimation(barMaterial, duration));
                    break;
            }

            // Ensure alpha is 0 at the end
            barColor = barMaterial.color;
            barColor.a = 0f;
            barMaterial.color = barColor;
            
            activeProgressBarCoroutine = null;
        }

        private IEnumerator FadeInOutAnimation(Material material, float duration)
        {
            Color barColor = material.color;
            
            // Fade in
            float elapsedTime = 0f;
            while (elapsedTime < fadeInTime)
            {
                barColor = material.color;
                barColor.a = Mathf.Lerp(0f, 1f, elapsedTime / fadeInTime);
                material.color = barColor;
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            barColor = material.color;
            barColor.a = 1f;
            material.color = barColor;

            // Display for duration
            yield return new WaitForSeconds(duration - fadeInTime - fadeOutTime);

            // Fade out
            elapsedTime = 0f;
            while (elapsedTime < fadeOutTime)
            {
                barColor = material.color;
                barColor.a = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutTime);
                material.color = barColor;
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }
        
        private IEnumerator PulseAnimation(Material material, float duration)
        {
            Color barColor = material.color;
            
            // Initial fade in
            float elapsedTime = 0f;
            while (elapsedTime < fadeInTime)
            {
                barColor = material.color;
                barColor.a = Mathf.Lerp(0f, 1f, elapsedTime / fadeInTime);
                material.color = barColor;
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // Pulse during the main duration
            float pulseTime = duration - fadeInTime - fadeOutTime;
            elapsedTime = 0f;
            
            while (elapsedTime < pulseTime)
            {
                // Calculate pulse alpha using sine wave
                float pulseAlpha = 0.5f + 0.5f * Mathf.Sin(elapsedTime * pulseFrequency * 2f * Mathf.PI);
                barColor = material.color;
                barColor.a = Mathf.Lerp(0.5f, 1f, pulseAlpha); // Pulse between 50% and 100% alpha
                material.color = barColor;
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // Fade out
            elapsedTime = 0f;
            while (elapsedTime < fadeOutTime)
            {
                barColor = material.color;
                barColor.a = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutTime);
                material.color = barColor;
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }
        
        private IEnumerator BlinkAnimation(Material material, float duration)
        {
            Color barColor = material.color;
            
            // Initial fade in
            float elapsedTime = 0f;
            while (elapsedTime < fadeInTime)
            {
                barColor = material.color;
                barColor.a = Mathf.Lerp(0f, 1f, elapsedTime / fadeInTime);
                material.color = barColor;
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // Blink during the main duration
            float blinkTime = duration - fadeInTime - fadeOutTime;
            elapsedTime = 0f;
            
            while (elapsedTime < blinkTime)
            {
                // Calculate blink alpha (on or off)
                float blinkValue = Mathf.Floor(Mathf.Repeat(elapsedTime * pulseFrequency, 1f) + 0.5f);
                barColor = material.color;
                barColor.a = blinkValue; // Either 0 or 1
                material.color = barColor;
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // Fade out
            elapsedTime = 0f;
            while (elapsedTime < fadeOutTime)
            {
                barColor = material.color;
                barColor.a = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutTime);
                material.color = barColor;
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }
        
        private IEnumerator WarningAnimation(Material material, float duration)
        {
            Color barColor = material.color;
            
            // Initial fade in
            float elapsedTime = 0f;
            while (elapsedTime < fadeInTime)
            {
                barColor = material.color;
                barColor.a = Mathf.Lerp(0f, 1f, elapsedTime / fadeInTime);
                material.color = barColor;
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // Warning flash during the main duration
            float warningTime = duration - fadeInTime - fadeOutTime;
            elapsedTime = 0f;
            
            while (elapsedTime < warningTime)
            {
                // Fast flashing pattern for warning
                float warningAlpha = Mathf.Abs(Mathf.Sin(elapsedTime * warningFrequency * Mathf.PI));
                barColor = material.color;
                barColor.a = Mathf.Lerp(0.2f, 1f, warningAlpha); // Flash between 20% and 100% alpha
                material.color = barColor;
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // Fade out
            elapsedTime = 0f;
            while (elapsedTime < fadeOutTime)
            {
                barColor = material.color;
                barColor.a = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutTime);
                material.color = barColor;
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator PopAndTransitionAnimation(Material material, float duration)
        {
            // Store the original color to transition to later
            Color originalColor = material.color;
            Color whiteColor = Color.white;
            whiteColor.a = 1f; // Full alpha
            
            // POP in with white color
            material.color = whiteColor;
            
            // Quick blink (flash white a couple of times)
            float blinkDuration = 0.2f;
            float elapsedTime = 0f;
            
            while (elapsedTime < blinkDuration)
            {
                // Blink between white and transparent
                float blinkValue = Mathf.Floor(Mathf.Repeat(elapsedTime * 15f, 1f) + 0.5f); // Fast blink (15 times per second)
                whiteColor.a = blinkValue;
                material.color = whiteColor;
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // Transition from white to the selected color
            float transitionTime = 0.3f;
            elapsedTime = 0f;
            
            while (elapsedTime < transitionTime)
            {
                float t = elapsedTime / transitionTime;
                Color lerpedColor = Color.Lerp(whiteColor, originalColor, t);
                lerpedColor.a = 1f; // Keep full alpha during transition
                material.color = lerpedColor;
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // Set to the original color with full alpha
            originalColor.a = 1f;
            material.color = originalColor;
            
            // Display for the main duration
            yield return new WaitForSeconds(duration - blinkDuration - transitionTime - fadeOutTime);
            
            // Fade out
            elapsedTime = 0f;
            while (elapsedTime < fadeOutTime)
            {
                Color fadeColor = material.color;
                fadeColor.a = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutTime);
                material.color = fadeColor;
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator PopAndFadeAnimation(Material material, float duration)
        {
            // Store the original color
            Color originalColor = material.color;
            
            // POP in with full alpha
            Color fullAlphaColor = originalColor;
            fullAlphaColor.a = 1f; // Full alpha
            material.color = fullAlphaColor;
            
            // Hold for a moment with full alpha
            float holdTime = 0.2f;
            yield return new WaitForSeconds(holdTime);
            
            // Slowly fade to the selected alpha (which is stored in the original color)
            float fadeTime = 0.5f;
            float elapsedTime = 0f;
            float targetAlpha = originalColor.a; // The alpha value we want to fade to
            
            // Reset to full alpha before starting the fade
            fullAlphaColor.a = 1f;
            material.color = fullAlphaColor;
            
            while (elapsedTime < fadeTime)
            {
                Color fadeColor = material.color;
                // Fade from 1.0 to the target alpha
                fadeColor.a = Mathf.Lerp(1f, targetAlpha, elapsedTime / fadeTime);
                material.color = fadeColor;
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // Set to the target alpha
            Color finalColor = originalColor;
            finalColor.a = targetAlpha;
            material.color = finalColor;
            
            // Display for the main duration
            yield return new WaitForSeconds(duration - holdTime - fadeTime - fadeOutTime);
            
            // Fade out
            elapsedTime = 0f;
            while (elapsedTime < fadeOutTime)
            {
                Color fadeColor = material.color;
                fadeColor.a = Mathf.Lerp(targetAlpha, 0f, elapsedTime / fadeOutTime);
                material.color = fadeColor;
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }

        #endregion

        /// <summary>
        /// Immediately hides any active message and visual bar
        /// </summary>
        public void HideAll()
        {
            if (activeMessageCoroutine != null)
            {
                StopCoroutine(activeMessageCoroutine);
                activeMessageCoroutine = null;
            }
            
            if (activeProgressBarCoroutine != null)
            {
                StopCoroutine(activeProgressBarCoroutine);
                activeProgressBarCoroutine = null;
            }
            
            if (textMesh != null)
            {
                Color hideColor = textMesh.color;
                hideColor.a = 0f;
                textMesh.color = hideColor;
            }
            
            if (visualBarRenderer != null)
            {
                Color barColor = visualBarRenderer.material.color;
                barColor.a = 0f;
                visualBarRenderer.material.color = barColor;
            }
        }

        private void OnDestroy()
        {
            // Clean up instanced material when destroyed
            if (instancedBarMaterial != null)
            {
                Destroy(instancedBarMaterial);
            }
        }

        private void Update()
        {
            // Only process keyboard input if testing is enabled
            if (!enableKeyboardTesting) return;

            // Test different animation styles with keyboard shortcuts
            if (Input.GetKeyDown(KeyCode.T))
            {
                // T key - Test FadeInOut animation with white bar
                ShowMessageWithVisualBar(testMessage, BarAnimationStyle.FadeInOut, Color.white, 2.0f, 2.0f);
                Debug.Log("Testing FadeInOut animation with white bar");
            }
            else if (Input.GetKeyDown(KeyCode.Y))
            {
                // Y key - Test Pulse animation with green bar
                ShowMessageWithVisualBar(testMessage, BarAnimationStyle.Pulse, Color.green, 2.0f, 2.0f);
                Debug.Log("Testing Pulse animation with green bar");
            }
            else if (Input.GetKeyDown(KeyCode.U))
            {
                // U key - Test Blink animation with blue bar
                ShowMessageWithVisualBar(testMessage, BarAnimationStyle.Blink, Color.blue, 2.0f, 2.0f);
                Debug.Log("Testing Blink animation with blue bar");
            }
            else if (Input.GetKeyDown(KeyCode.I))
            {
                // I key - Test Warning animation with red bar
                ShowMessageWithVisualBar(testMessage, BarAnimationStyle.Warning, Color.red, 2.0f, 2.0f);
                Debug.Log("Testing Warning animation with red bar");
            }
            else if (Input.GetKeyDown(KeyCode.O))
            {
                // O key - Test "Zero Tridots" message
                ShowMessageByID("ZERO_TRIDOTS", 3);
                Debug.Log("Testing ZERO_TRIDOTS message");
            }
            else if (Input.GetKeyDown(KeyCode.P))
            {
                // P key - Test PopAndTransition animation (white to purple)
                ShowMessageWithVisualBar(testMessage, BarAnimationStyle.PopAndTransition, new Color(0.5f, 0f, 0.5f), 2.0f, 2.0f);
                Debug.Log("Testing PopAndTransition animation (white to purple)");
            }
            else if (Input.GetKeyDown(KeyCode.LeftBracket))
            {
                // [ key - Test PopAndFade animation with orange color
                Color orangeWithAlpha = new Color(1f, 0.5f, 0f, 0.7f); // Orange with 70% alpha
                ShowMessageWithVisualBar(testMessage, BarAnimationStyle.PopAndFade, orangeWithAlpha, 2.0f, 2.0f);
                Debug.Log("Testing PopAndFade animation with orange color");
            }
            else if (Input.GetKeyDown(KeyCode.RightBracket))
            {
                // ] key - Test different durations (text longer than bar)
                Color blueWithAlpha = new Color(0f, 0.5f, 1f, 0.7f); // Blue with 70% alpha
                ShowMessageWithVisualBar("Text stays longer than bar", BarAnimationStyle.FadeInOut, blueWithAlpha, 3.0f, 1.5f);
                Debug.Log("Testing different durations - text: 3.0s, bar: 1.5s");
            }
            else if (Input.GetKeyDown(KeyCode.Backslash))
            {
                // \ key - Test different durations (bar longer than text)
                Color greenWithAlpha = new Color(0f, 0.8f, 0.2f, 0.7f); // Green with 70% alpha
                ShowMessageWithVisualBar("Bar stays longer than text", BarAnimationStyle.FadeInOut, greenWithAlpha, 1.5f, 3.0f);
                Debug.Log("Testing different durations - text: 1.5s, bar: 3.0s");
            }
        }
    }
} 