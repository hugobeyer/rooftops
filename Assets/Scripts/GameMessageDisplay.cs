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
    public class MessageStyle
    {
        public string styleName = "Default Style";
        public Color messageColor = Color.white;
        [Range(0.5f, 5f)]
        public float displayDuration = 2.0f;
        public bool showVisualBar = false;
        public BarAnimationStyle barAnimationStyle = BarAnimationStyle.FadeInOut;
        [Tooltip("Color of the visual bar (uses the assigned material but tints it with this color)")]
        public Color barColor = Color.white;
        [Tooltip("Optional separate duration for the visual bar (if <= 0, uses the text duration)")]
        [Range(0f, 5f)]
        public float barDuration = 0f;
    }

    [System.Serializable]
    public class GameMessage
    {
        public string messageID;
        public string messageText = "Message text here";
        [Tooltip("Style to use for this message. Required field - message will use the specified style's settings.")]
        public string styleID = "";
        
        // All visual and timing settings are now handled by the MessageStyle system
    }

    public class GameMessageDisplay : MonoBehaviour
    {
        public static GameMessageDisplay Instance { get; private set; }

        [Header("References")]
        [SerializeField] private GameObject textObject;
        [SerializeField] private GameObject visualBarObject; // Visual bar element
        
        // Additional text and visual bar objects
        [Header("Additional Message Rows")]
        [SerializeField] private GameObject textObject2; // Second text object
        [SerializeField] private GameObject visualBarObject2; // Second visual bar element
        [SerializeField] private GameObject textObject3; // Third text object
        [SerializeField] private GameObject visualBarObject3; // Third visual bar element
        [SerializeField] private GameObject textObject4; // Fourth text object
        [SerializeField] private GameObject visualBarObject4; // Fourth visual bar element
        [SerializeField] private GameObject textObject5; // Fifth text object
        [SerializeField] private GameObject visualBarObject5; // Fifth visual bar element
        
        [Header("Visual Bar Settings")]
        [SerializeField] private Material visualBarMaterial; // Custom material for the visual bar
        
        [Header("Common Animation Settings")]
        [SerializeField] private float fadeInTime = 0.25f;
        [SerializeField] private float fadeOutTime = 0.5f;
        
        [Header("Pulse & Blink Animation Settings")]
        [Tooltip("How many times the bar should pulse or blink per second")]
        [SerializeField] private float pulseFrequency = 2f; // Pulses per second
        [Range(0.2f, 0.8f)]
        [SerializeField] private float pulseMinAlpha = 0.5f; // Minimum alpha during pulse
        [Range(0.8f, 1f)]
        [SerializeField] private float pulseMaxAlpha = 1f; // Maximum alpha during pulse
        
        [Header("Warning Animation Settings")]
        [Tooltip("How many times the warning effect should flash per second")]
        [SerializeField] private float warningFrequency = 4f; // Flashes per second
        [Range(0.0f, 0.5f)]
        [SerializeField] private float warningMinAlpha = 0.2f; // Minimum alpha during warning
        [Range(0.5f, 1f)]
        [SerializeField] private float warningMaxAlpha = 1f; // Maximum alpha during warning
        
        [Header("Pop And Transition Animation Settings")]
        [SerializeField] private float popBlinkDuration = 0.2f; // Duration of initial white blink
        [SerializeField] private float popBlinkFrequency = 15f; // Blinks per second during pop
        [SerializeField] private float popTransitionTime = 0.3f; // Time to transition from white to color
        
        [Header("Pop And Fade Animation Settings")]
        [SerializeField] private float popHoldTime = 0.2f; // Time to hold at full alpha
        [SerializeField] private float popFadeTime = 0.5f; // Time to fade to target alpha

        private TextMeshPro textMesh;
        private Renderer visualBarRenderer;
        private Material instancedBarMaterial; // Instanced material to avoid affecting other objects

        // Additional TextMeshPro and Renderer references
        private TextMeshPro textMesh2;
        private Renderer visualBarRenderer2;
        private Material instancedBarMaterial2;
        private TextMeshPro textMesh3;
        private Renderer visualBarRenderer3;
        private Material instancedBarMaterial3;
        private TextMeshPro textMesh4;
        private Renderer visualBarRenderer4;
        private Material instancedBarMaterial4;
        private TextMeshPro textMesh5;
        private Renderer visualBarRenderer5;
        private Material instancedBarMaterial5;

        [Header("Common Message Settings")]
        [SerializeField] private float defaultDisplayTime = 2.0f;
        [SerializeField] private Color defaultTextColor = Color.white;
        [SerializeField] private Color warningTextColor = new Color(1f, 0.5f, 0f); // Orange
        [SerializeField] private Color errorTextColor = Color.red;

        [Header("Message Styles")]
        [SerializeField] private List<MessageStyle> messageStyles = new List<MessageStyle>();

        [Header("Message Library")]
        [SerializeField] private List<GameMessage> messageLibrary = new List<GameMessage>();
        
        // Default messages for backward compatibility
        [Header("Default Messages")]
        [Tooltip("Message to show when player doesn't have enough tridots")]
        [SerializeField] private string notEnoughTridotsMessage = "Need {0} tridots to dash!";
        [Tooltip("Message to show when player collects a tridot")]
        [SerializeField] private string tridotCollectedMessage = "Tridot collected! ({0})";
        [Tooltip("Message to show when a new achievement tier is revealed")]
        [SerializeField] private GameMessage newTierRevealedMessage;

        // Track active coroutines for each message row
        private Coroutine activeMessageCoroutine;
        private Coroutine activeProgressBarCoroutine;
        private Coroutine activeMessageCoroutine2;
        private Coroutine activeProgressBarCoroutine2;
        private Coroutine activeMessageCoroutine3;
        private Coroutine activeProgressBarCoroutine3;
        private Coroutine activeMessageCoroutine4;
        private Coroutine activeProgressBarCoroutine4;
        private Coroutine activeMessageCoroutine5;
        private Coroutine activeProgressBarCoroutine5;
        
        // Track which rows are currently in use
        private bool isRow1Active = false;
        private bool isRow2Active = false;
        private bool isRow3Active = false;
        private bool isRow4Active = false;
        private bool isRow5Active = false;

        // Dictionary for quick message lookup by ID
        private Dictionary<string, GameMessage> messageDict = new Dictionary<string, GameMessage>();
        // Dictionary for quick style lookup by ID
        private Dictionary<string, MessageStyle> styleDict = new Dictionary<string, MessageStyle>();

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

            // Initialize additional text objects
            if (textObject2 != null)
            {
                textMesh2 = textObject2.GetComponent<TextMeshPro>();
                if (textMesh2 == null)
                {
                    Debug.LogWarning("TextMeshPro component not found on the assigned text object 2!");
                }
            }
            
            if (textObject3 != null)
            {
                textMesh3 = textObject3.GetComponent<TextMeshPro>();
                if (textMesh3 == null)
                {
                    Debug.LogWarning("TextMeshPro component not found on the assigned text object 3!");
                }
            }

            if (textObject4 != null)
            {
                textMesh4 = textObject4.GetComponent<TextMeshPro>();
                if (textMesh4 == null)
                {
                    Debug.LogWarning("TextMeshPro component not found on the assigned text object 4!");
                }
            }
            
            if (textObject5 != null)
            {
                textMesh5 = textObject5.GetComponent<TextMeshPro>();
                if (textMesh5 == null)
                {
                    Debug.LogWarning("TextMeshPro component not found on the assigned text object 5!");
                }
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
            
            // Initialize additional visual bar objects
            if (visualBarObject2 != null)
            {
                visualBarRenderer2 = visualBarObject2.GetComponent<Renderer>();
                if (visualBarRenderer2 == null)
                {
                    Debug.LogWarning("Renderer component not found on the assigned visual bar object 2!");
                }
                else
                {
                    // Apply custom material if assigned
                    if (visualBarMaterial != null)
                    {
                        // Create an instance of the material to avoid affecting other objects
                        instancedBarMaterial2 = new Material(visualBarMaterial);
                        visualBarRenderer2.material = instancedBarMaterial2;
                    }
                    
                    // Set initial alpha to 0 (invisible)
                    Color barColor = visualBarRenderer2.material.color;
                    barColor.a = 0f;
                    visualBarRenderer2.material.color = barColor;
                }
            }
            
            if (visualBarObject3 != null)
            {
                visualBarRenderer3 = visualBarObject3.GetComponent<Renderer>();
                if (visualBarRenderer3 == null)
                {
                    Debug.LogWarning("Renderer component not found on the assigned visual bar object 3!");
                }
                else
                {
                    // Apply custom material if assigned
                    if (visualBarMaterial != null)
                    {
                        // Create an instance of the material to avoid affecting other objects
                        instancedBarMaterial3 = new Material(visualBarMaterial);
                        visualBarRenderer3.material = instancedBarMaterial3;
                    }
                    
                    // Set initial alpha to 0 (invisible)
                    Color barColor = visualBarRenderer3.material.color;
                    barColor.a = 0f;
                    visualBarRenderer3.material.color = barColor;
                }
            }

            if (visualBarObject4 != null)
            {
                visualBarRenderer4 = visualBarObject4.GetComponent<Renderer>();
                if (visualBarRenderer4 == null)
                {
                    Debug.LogWarning("Renderer component not found on the assigned visual bar object 4!");
                }
                else
                {
                    // Apply custom material if assigned
                    if (visualBarMaterial != null)
                    {
                        // Create an instance of the material to avoid affecting other objects
                        instancedBarMaterial4 = new Material(visualBarMaterial);
                        visualBarRenderer4.material = instancedBarMaterial4;
                    }
                    
                    // Set initial alpha to 0 (invisible)
                    Color barColor = visualBarRenderer4.material.color;
                    barColor.a = 0f;
                    visualBarRenderer4.material.color = barColor;
                }
            }
            
            if (visualBarObject5 != null)
            {
                visualBarRenderer5 = visualBarObject5.GetComponent<Renderer>();
                if (visualBarRenderer5 == null)
                {
                    Debug.LogWarning("Renderer component not found on the assigned visual bar object 5!");
                }
                else
                {
                    // Apply custom material if assigned
                    if (visualBarMaterial != null)
                    {
                        // Create an instance of the material to avoid affecting other objects
                        instancedBarMaterial5 = new Material(visualBarMaterial);
                        visualBarRenderer5.material = instancedBarMaterial5;
                    }
                    
                    // Set initial alpha to 0 (invisible)
                    Color barColor = visualBarRenderer5.material.color;
                    barColor.a = 0f;
                    visualBarRenderer5.material.color = barColor;
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
            
            // Hide additional text and bars at start
            if (textMesh2 != null)
            {
                Color startColor = textMesh2.color;
                startColor.a = 0f;
                textMesh2.color = startColor;
            }

            if (visualBarRenderer2 != null)
            {
                Color barColor = visualBarRenderer2.material.color;
                barColor.a = 0f;
                visualBarRenderer2.material.color = barColor;
            }
            
            if (textMesh3 != null)
            {
                Color startColor = textMesh3.color;
                startColor.a = 0f;
                textMesh3.color = startColor;
            }

            if (visualBarRenderer3 != null)
            {
                Color barColor = visualBarRenderer3.material.color;
                barColor.a = 0f;
                visualBarRenderer3.material.color = barColor;
            }

            if (textMesh4 != null)
            {
                Color startColor = textMesh4.color;
                startColor.a = 0f;
                textMesh4.color = startColor;
            }

            if (visualBarRenderer4 != null)
            {
                Color barColor = visualBarRenderer4.material.color;
                barColor.a = 0f;
                visualBarRenderer4.material.color = barColor;
            }
            
            if (textMesh5 != null)
            {
                Color startColor = textMesh5.color;
                startColor.a = 0f;
                textMesh5.color = startColor;
            }

            if (visualBarRenderer5 != null)
            {
                Color barColor = visualBarRenderer5.material.color;
                barColor.a = 0f;
                visualBarRenderer5.material.color = barColor;
            }

            // Build message dictionary for quick lookup
            BuildMessageDictionary();
        }
                 /////////////////////////////////// DO NOT DARE TO CREATE ANY HARD CODED MESSAGES, DONT YOU DARE!!!!
        private void BuildMessageDictionary()
        {
            messageDict.Clear();
            styleDict.Clear();
                     /////////////////////////////////// DO NOT DARE TO CREATE ANY HARD CODED MESSAGES, DONT YOU DARE!!!!
            // Add all styles from the style library to the dictionary
            foreach (MessageStyle style in messageStyles)
            {
                if (!string.IsNullOrEmpty(style.styleName) && !styleDict.ContainsKey(style.styleName))
                {
                    styleDict.Add(style.styleName, style);
                }
                else if (string.IsNullOrEmpty(style.styleName))
                {
                    Debug.LogWarning("GameMessageDisplay: Style with empty name found in library");
                }
                else
                {
                    Debug.LogWarning($"GameMessageDisplay: Duplicate style name '{style.styleName}' found in library");
                }
            }
            

            
            
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

        #region Public Methods

        /// <summary>
        /// Checks if a message with the specified ID exists in the message library
        /// </summary>
        public bool HasMessage(string messageID)
        {
            return messageDict.ContainsKey(messageID);
        }
        
        /// <summary>
        /// Checks if a style with the specified name exists in the style library
        /// </summary>
        public bool HasStyle(string styleName)
        {
            return styleDict.ContainsKey(styleName);
        }
        
        /// <summary>
        /// Gets a style by name, returns null if not found
        /// </summary>
        public MessageStyle GetStyle(string styleName)
        {
            MessageStyle style;
            if (styleDict.TryGetValue(styleName, out style))
            {
                return style;
            }
            return null;
        }

        /// <summary>
        /// Shows a message by its ID from the message library
        /// </summary>
        public void ShowMessageByID(string messageID, params object[] formatArgs)
        {
            GameMessage message;
            
            // Try to get the message from the dictionary
            if (!messageDict.TryGetValue(messageID, out message))
            {
                // Message ID not found - log a warning and return without showing any message
                // DO NOT use the first message as fallback
                Debug.LogWarning($"GameMessageDisplay: Message ID '{messageID}' not found. No message will be shown.");
                return;
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
            
            // Get the style for this message
            MessageStyle style = null;
            
            // Try to get the specified style
            if (!string.IsNullOrEmpty(message.styleID))
            {
                styleDict.TryGetValue(message.styleID, out style);
            }
            
            // If no style was found, use the default style
            if (style == null)
            {
                // Try to get a style named "Default" first
                if (!styleDict.TryGetValue("Default", out style) && messageStyles.Count > 0)
                {
                    // If no "Default" style exists, use the first style in the list
                    style = messageStyles[0];
                    Debug.LogWarning($"GameMessageDisplay: Style '{message.styleID}' not found for message '{messageID}'. Using first style as fallback.");
                }
                
                // If we still don't have a style, use hardcoded default values
                if (style == null)
                {
                    Debug.LogWarning($"GameMessageDisplay: No styles available for message '{messageID}'. Using hardcoded defaults.");
                    ShowMessageInAvailableRow(formattedText, defaultDisplayTime, defaultTextColor);
                    return;
                }
            }
            
            // Apply the style settings
            if (style.showVisualBar)
            {
                // Use separate bar duration if specified, otherwise use text duration
                float barDuration = style.barDuration > 0 ? style.barDuration : style.displayDuration;
                ShowMessageWithVisualBarInAvailableRow(formattedText, style.barAnimationStyle, style.barColor, style.displayDuration, barDuration, style.messageColor);
            }
            else
            {
                ShowMessageInAvailableRow(formattedText, style.displayDuration, style.messageColor);
            }
        }

        /// <summary>
        /// Shows a message in the first available row
        /// </summary>
        private void ShowMessageInAvailableRow(string message, float duration, Color color)
        {
            // Try to use row 1 first
            if (!isRow1Active)
            {
                ShowMessageInRow1(message, duration, color);
            }
            // Then try row 2
            else if (!isRow2Active)
            {
                ShowMessageInRow2(message, duration, color);
            }
            // Then try row 3
            else if (!isRow3Active)
            {
                ShowMessageInRow3(message, duration, color);
            }
            // Then try row 4
            else if (!isRow4Active)
            {
                ShowMessageInRow4(message, duration, color);
            }
            // Then try row 5
            else if (!isRow5Active)
            {
                ShowMessageInRow5(message, duration, color);
            }
            // If all rows are active, queue the message to be shown after the first one finishes
            else
            {
                // Store the message to show it after row 1 is free
                StartCoroutine(QueueMessageAfterDelay(message, duration, color));
            }
        }
        
        private IEnumerator QueueMessageAfterDelay(string message, float duration, Color color)
        {
            // Wait until row 1 is free
            yield return new WaitUntil(() => !isRow1Active);
            
            // Show the message in row 1
            ShowMessageInRow1(message, duration, color);
        }

        /// <summary>
        /// Shows a message with visual bar in the first available row
        /// </summary>
        private void ShowMessageWithVisualBarInAvailableRow(string message, BarAnimationStyle animationStyle, Color barColor, float textDuration, float barDuration, Color? textColor)
        {
            // Try to use row 1 first
            if (!isRow1Active)
            {
                ShowMessageWithVisualBarInRow1(message, animationStyle, barColor, textDuration, barDuration, textColor);
            }
            // Then try row 2
            else if (!isRow2Active)
            {
                ShowMessageWithVisualBarInRow2(message, animationStyle, barColor, textDuration, barDuration, textColor);
            }
            // Then try row 3
            else if (!isRow3Active)
            {
                ShowMessageWithVisualBarInRow3(message, animationStyle, barColor, textDuration, barDuration, textColor);
            }
            // Then try row 4
            else if (!isRow4Active)
            {
                ShowMessageWithVisualBarInRow4(message, animationStyle, barColor, textDuration, barDuration, textColor);
            }
            // Then try row 5
            else if (!isRow5Active)
            {
                ShowMessageWithVisualBarInRow5(message, animationStyle, barColor, textDuration, barDuration, textColor);
            }
            // If all rows are active, queue the message to be shown after the first one finishes
            else
            {
                // Store the message to show it after row 1 is free
                StartCoroutine(QueueVisualBarMessageAfterDelay(message, animationStyle, barColor, textDuration, barDuration, textColor));
            }
        }
        
        private IEnumerator QueueVisualBarMessageAfterDelay(string message, BarAnimationStyle animationStyle, Color barColor, float textDuration, float barDuration, Color? textColor)
        {
            // Wait until row 1 is free
            yield return new WaitUntil(() => !isRow1Active);
            
            // Show the message in row 1
            ShowMessageWithVisualBarInRow1(message, animationStyle, barColor, textDuration, barDuration, textColor);
        }

        /// <summary>
        /// Shows a message to the player for the default duration
        /// </summary>
        public void ShowMessage(string message)
        {
            ShowMessageInAvailableRow(message, defaultDisplayTime, defaultTextColor);
        }

        /// <summary>
        /// Shows a message to the player for the specified duration
        /// </summary>
        public void ShowMessage(string message, float duration)
        {
            ShowMessageInAvailableRow(message, duration, defaultTextColor);
        }

        /// <summary>
        /// Shows a warning message (orange color)
        /// </summary>
        public void ShowWarning(string message, float duration = -1)
        {
            if (duration < 0) duration = defaultDisplayTime;
            ShowMessageInAvailableRow(message, duration, warningTextColor);
        }

        /// <summary>
        /// Shows an error message (red color)
        /// </summary>
        public void ShowError(string message, float duration = -1)
        {
            if (duration < 0) duration = defaultDisplayTime;
            ShowMessageInAvailableRow(message, duration, errorTextColor);
        }

        /// <summary>
        /// Shows a message with the specified color and duration
        /// </summary>
        public void ShowMessage(string message, float duration, Color color)
        {
            ShowMessageInAvailableRow(message, duration, color);
        }

        /// <summary>
        /// Shows a message with the specified color and duration in row 1
        /// </summary>
        public void ShowMessageInRow1(string message, float duration, Color color)
        {
            if (textMesh == null) return;
            
            // Stop any active message display for row 1
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
            isRow1Active = true;
            activeMessageCoroutine = StartCoroutine(DisplayMessageCoroutine(textMesh, message, duration, color, 1));
        }

        /// <summary>
        /// Shows a message with the specified color and duration in row 2
        /// </summary>
        public void ShowMessageInRow2(string message, float duration, Color color)
        {
            if (textMesh2 == null) return;
            
            // Stop any active message display for row 2
            if (activeMessageCoroutine2 != null)
            {
                StopCoroutine(activeMessageCoroutine2);
                activeMessageCoroutine2 = null;
            }

            // Reset text alpha to 0 to ensure proper fade-in even if already showing
            Color resetColor = color;
            resetColor.a = 0f;
            textMesh2.color = resetColor;

            // Start new message display
            isRow2Active = true;
            activeMessageCoroutine2 = StartCoroutine(DisplayMessageCoroutine(textMesh2, message, duration, color, 2));
        }

        /// <summary>
        /// Shows a message with the specified color and duration in row 3
        /// </summary>
        public void ShowMessageInRow3(string message, float duration, Color color)
        {
            if (textMesh3 == null) return;
            
            // Stop any active message display for row 3
            if (activeMessageCoroutine3 != null)
            {
                StopCoroutine(activeMessageCoroutine3);
                activeMessageCoroutine3 = null;
            }

            // Reset text alpha to 0 to ensure proper fade-in even if already showing
            Color resetColor = color;
            resetColor.a = 0f;
            textMesh3.color = resetColor;

            // Start new message display
            isRow3Active = true;
            activeMessageCoroutine3 = StartCoroutine(DisplayMessageCoroutine(textMesh3, message, duration, color, 3));
        }

        /// <summary>
        /// Shows a message with the specified color and duration in row 4
        /// </summary>
        public void ShowMessageInRow4(string message, float duration, Color color)
        {
            if (textMesh4 == null) return;
            
            // Stop any active message display for row 4
            if (activeMessageCoroutine4 != null)
            {
                StopCoroutine(activeMessageCoroutine4);
                activeMessageCoroutine4 = null;
            }

            // Reset text alpha to 0 to ensure proper fade-in even if already showing
            Color resetColor = color;
            resetColor.a = 0f;
            textMesh4.color = resetColor;

            // Start new message display
            isRow4Active = true;
            activeMessageCoroutine4 = StartCoroutine(DisplayMessageCoroutine(textMesh4, message, duration, color, 4));
        }

        /// <summary>
        /// Shows a message with the specified color and duration in row 5
        /// </summary>
        public void ShowMessageInRow5(string message, float duration, Color color)
        {
            if (textMesh5 == null) return;
            
            // Stop any active message display for row 5
            if (activeMessageCoroutine5 != null)
            {
                StopCoroutine(activeMessageCoroutine5);
                activeMessageCoroutine5 = null;
            }

            // Reset text alpha to 0 to ensure proper fade-in even if already showing
            Color resetColor = color;
            resetColor.a = 0f;
            textMesh5.color = resetColor;

            // Start new message display
            isRow5Active = true;
            activeMessageCoroutine5 = StartCoroutine(DisplayMessageCoroutine(textMesh5, message, duration, color, 5));
        }

        /// <summary>
        /// Shows the visual bar with the specified animation style and color
        /// </summary>
        public void ShowVisualBar(BarAnimationStyle animationStyle, Color barColor, float duration = -1)
        {
            ShowVisualBarInRow1(animationStyle, barColor, duration);
        }

        /// <summary>
        /// Shows the visual bar in row 1
        /// </summary>
        public void ShowVisualBarInRow1(BarAnimationStyle animationStyle, Color barColor, float duration = -1)
        {
            if (visualBarRenderer == null) return;
            if (duration < 0) duration = defaultDisplayTime;

            // Stop any active visual bar display for row 1
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
            activeProgressBarCoroutine = StartCoroutine(DisplayVisualBarCoroutine(visualBarRenderer, animationStyle, duration, 1));
        }

        /// <summary>
        /// Shows the visual bar in row 2
        /// </summary>
        public void ShowVisualBarInRow2(BarAnimationStyle animationStyle, Color barColor, float duration = -1)
        {
            if (visualBarRenderer2 == null) return;
            if (duration < 0) duration = defaultDisplayTime;

            // Stop any active visual bar display for row 2
            if (activeProgressBarCoroutine2 != null)
            {
                StopCoroutine(activeProgressBarCoroutine2);
                activeProgressBarCoroutine2 = null;
            }

            // Reset bar alpha to 0 to ensure proper fade-in even if already showing
            Color currentColor = visualBarRenderer2.material.color;
            // Apply the new RGB values but keep alpha at 0
            Color newColor = barColor;
            newColor.a = 0f;
            visualBarRenderer2.material.color = newColor;

            // Start new visual bar display with the selected animation style
            activeProgressBarCoroutine2 = StartCoroutine(DisplayVisualBarCoroutine(visualBarRenderer2, animationStyle, duration, 2));
        }

        /// <summary>
        /// Shows the visual bar in row 3
        /// </summary>
        public void ShowVisualBarInRow3(BarAnimationStyle animationStyle, Color barColor, float duration = -1)
        {
            if (visualBarRenderer3 == null) return;
            if (duration < 0) duration = defaultDisplayTime;

            // Stop any active visual bar display for row 3
            if (activeProgressBarCoroutine3 != null)
            {
                StopCoroutine(activeProgressBarCoroutine3);
                activeProgressBarCoroutine3 = null;
            }

            // Reset bar alpha to 0 to ensure proper fade-in even if already showing
            Color currentColor = visualBarRenderer3.material.color;
            // Apply the new RGB values but keep alpha at 0
            Color newColor = barColor;
            newColor.a = 0f;
            visualBarRenderer3.material.color = newColor;

            // Start new visual bar display with the selected animation style
            activeProgressBarCoroutine3 = StartCoroutine(DisplayVisualBarCoroutine(visualBarRenderer3, animationStyle, duration, 3));
        }

        /// <summary>
        /// Shows the visual bar in row 4
        /// </summary>
        public void ShowVisualBarInRow4(BarAnimationStyle animationStyle, Color barColor, float duration = -1)
        {
            if (visualBarRenderer4 == null) return;
            if (duration < 0) duration = defaultDisplayTime;

            // Stop any active visual bar display for row 4
            if (activeProgressBarCoroutine4 != null)
            {
                StopCoroutine(activeProgressBarCoroutine4);
                activeProgressBarCoroutine4 = null;
            }

            // Reset bar alpha to 0 to ensure proper fade-in even if already showing
            Color currentColor = visualBarRenderer4.material.color;
            // Apply the new RGB values but keep alpha at 0
            Color newColor = barColor;
            newColor.a = 0f;
            visualBarRenderer4.material.color = newColor;

            // Start new visual bar display with the selected animation style
            activeProgressBarCoroutine4 = StartCoroutine(DisplayVisualBarCoroutine(visualBarRenderer4, animationStyle, duration, 4));
        }

        /// <summary>
        /// Shows the visual bar in row 5
        /// </summary>
        public void ShowVisualBarInRow5(BarAnimationStyle animationStyle, Color barColor, float duration = -1)
        {
            if (visualBarRenderer5 == null) return;
            if (duration < 0) duration = defaultDisplayTime;

            // Stop any active visual bar display for row 5
            if (activeProgressBarCoroutine5 != null)
            {
                StopCoroutine(activeProgressBarCoroutine5);
                activeProgressBarCoroutine5 = null;
            }

            // Reset bar alpha to 0 to ensure proper fade-in even if already showing
            Color currentColor = visualBarRenderer5.material.color;
            // Apply the new RGB values but keep alpha at 0
            Color newColor = barColor;
            newColor.a = 0f;
            visualBarRenderer5.material.color = newColor;

            // Start new visual bar display with the selected animation style
            activeProgressBarCoroutine5 = StartCoroutine(DisplayVisualBarCoroutine(visualBarRenderer5, animationStyle, duration, 5));
        }

        /// <summary>
        /// Shows both a message and visual bar with custom colors and separate durations
        /// </summary>
        public void ShowMessageWithVisualBar(string message, BarAnimationStyle animationStyle, Color barColor, float textDuration = -1, float barDuration = -1, Color? textColor = null)
        {
            ShowMessageWithVisualBarInAvailableRow(message, animationStyle, barColor, textDuration, barDuration, textColor);
        }

        /// <summary>
        /// Shows both a message and visual bar with custom colors and separate durations in row 1
        /// </summary>
        public void ShowMessageWithVisualBarInRow1(string message, BarAnimationStyle animationStyle, Color barColor, float textDuration, float barDuration, Color? textColor = null)
        {
            if (textDuration < 0) textDuration = defaultDisplayTime;
            if (barDuration < 0) barDuration = textDuration; // Use text duration if bar duration not specified
            
            // Ensure durations include fade times
            float totalTextDuration = textDuration;
            float totalBarDuration = barDuration;
            
            Color messageTextColor = textColor ?? defaultTextColor;
            
            ShowMessageInRow1(message, totalTextDuration, messageTextColor);
            ShowVisualBarInRow1(animationStyle, barColor, totalBarDuration);
        }

        /// <summary>
        /// Shows both a message and visual bar with custom colors and separate durations in row 2
        /// </summary>
        public void ShowMessageWithVisualBarInRow2(string message, BarAnimationStyle animationStyle, Color barColor, float textDuration, float barDuration, Color? textColor = null)
        {
            if (textDuration < 0) textDuration = defaultDisplayTime;
            if (barDuration < 0) barDuration = textDuration; // Use text duration if bar duration not specified
            
            // Ensure durations include fade times
            float totalTextDuration = textDuration;
            float totalBarDuration = barDuration;
            
            Color messageTextColor = textColor ?? defaultTextColor;
            
            ShowMessageInRow2(message, totalTextDuration, messageTextColor);
            ShowVisualBarInRow2(animationStyle, barColor, totalBarDuration);
        }

        /// <summary>
        /// Shows both a message and visual bar with custom colors and separate durations in row 3
        /// </summary>
        public void ShowMessageWithVisualBarInRow3(string message, BarAnimationStyle animationStyle, Color barColor, float textDuration, float barDuration, Color? textColor = null)
        {
            if (textDuration < 0) textDuration = defaultDisplayTime;
            if (barDuration < 0) barDuration = textDuration; // Use text duration if bar duration not specified
            
            // Ensure durations include fade times
            float totalTextDuration = textDuration;
            float totalBarDuration = barDuration;
            
            Color messageTextColor = textColor ?? defaultTextColor;
            
            ShowMessageInRow3(message, totalTextDuration, messageTextColor);
            ShowVisualBarInRow3(animationStyle, barColor, totalBarDuration);
        }

        /// <summary>
        /// Shows both a message and visual bar with custom colors and separate durations in row 4
        /// </summary>
        public void ShowMessageWithVisualBarInRow4(string message, BarAnimationStyle animationStyle, Color barColor, float textDuration, float barDuration, Color? textColor = null)
        {
            if (textMesh4 == null || visualBarRenderer4 == null) return;
            
            // Use default text color if not specified
            Color messageColor = textColor ?? defaultTextColor;
            
            // Show the message
            ShowMessageInRow4(message, textDuration, messageColor);
            
            // Show the visual bar
            ShowVisualBarInRow4(animationStyle, barColor, barDuration > 0 ? barDuration : textDuration);
        }

        /// <summary>
        /// Shows both a message and visual bar with custom colors and separate durations in row 5
        /// </summary>
        public void ShowMessageWithVisualBarInRow5(string message, BarAnimationStyle animationStyle, Color barColor, float textDuration, float barDuration, Color? textColor = null)
        {
            if (textMesh5 == null || visualBarRenderer5 == null) return;
            
            // Use default text color if not specified
            Color messageColor = textColor ?? defaultTextColor;
            
            // Show the message
            ShowMessageInRow5(message, textDuration, messageColor);
            
            // Show the visual bar
            ShowVisualBarInRow5(animationStyle, barColor, barDuration > 0 ? barDuration : textDuration);
        }

        /// <summary>
        /// Shows both a message and visual bar with default bar color
        /// </summary>
        public void ShowMessageWithVisualBar(string message, BarAnimationStyle animationStyle, float textDuration = -1, Color? textColor = null)
        {
            ShowMessageWithVisualBarInAvailableRow(message, animationStyle, Color.white, textDuration, textDuration, textColor);
        }

        /// <summary>
        /// Immediately hides any active message and visual bar
        /// </summary>
        public void HideAll()
        {
            // Stop all active coroutines
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
            
            if (activeMessageCoroutine2 != null)
            {
                StopCoroutine(activeMessageCoroutine2);
                activeMessageCoroutine2 = null;
            }
            
            if (activeProgressBarCoroutine2 != null)
            {
                StopCoroutine(activeProgressBarCoroutine2);
                activeProgressBarCoroutine2 = null;
            }
            
            if (activeMessageCoroutine3 != null)
            {
                StopCoroutine(activeMessageCoroutine3);
                activeMessageCoroutine3 = null;
            }
            
            if (activeProgressBarCoroutine3 != null)
            {
                StopCoroutine(activeProgressBarCoroutine3);
                activeProgressBarCoroutine3 = null;
            }
            
            if (activeMessageCoroutine4 != null)
            {
                StopCoroutine(activeMessageCoroutine4);
                activeMessageCoroutine4 = null;
            }
            
            if (activeProgressBarCoroutine4 != null)
            {
                StopCoroutine(activeProgressBarCoroutine4);
                activeProgressBarCoroutine4 = null;
            }
            
            if (activeMessageCoroutine5 != null)
            {
                StopCoroutine(activeMessageCoroutine5);
                activeMessageCoroutine5 = null;
            }
            
            if (activeProgressBarCoroutine5 != null)
            {
                StopCoroutine(activeProgressBarCoroutine5);
                activeProgressBarCoroutine5 = null;
            }
            
            // Hide main text and bar
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
            
            // Hide additional text and bars
            if (textMesh2 != null)
            {
                Color hideColor = textMesh2.color;
                hideColor.a = 0f;
                textMesh2.color = hideColor;
            }
            
            if (visualBarRenderer2 != null)
            {
                Color barColor = visualBarRenderer2.material.color;
                barColor.a = 0f;
                visualBarRenderer2.material.color = barColor;
            }
            
            if (textMesh3 != null)
            {
                Color hideColor = textMesh3.color;
                hideColor.a = 0f;
                textMesh3.color = hideColor;
            }
            
            if (visualBarRenderer3 != null)
            {
                Color barColor = visualBarRenderer3.material.color;
                barColor.a = 0f;
                visualBarRenderer3.material.color = barColor;
            }
            
            if (textMesh4 != null)
            {
                Color hideColor = textMesh4.color;
                hideColor.a = 0f;
                textMesh4.color = hideColor;
            }
            
            if (visualBarRenderer4 != null)
            {
                Color barColor = visualBarRenderer4.material.color;
                barColor.a = 0f;
                visualBarRenderer4.material.color = barColor;
            }
            
            if (textMesh5 != null)
            {
                Color hideColor = textMesh5.color;
                hideColor.a = 0f;
                textMesh5.color = hideColor;
            }
            
            if (visualBarRenderer5 != null)
            {
                Color barColor = visualBarRenderer5.material.color;
                barColor.a = 0f;
                visualBarRenderer5.material.color = barColor;
            }
            
            // Reset row status
            isRow1Active = false;
            isRow2Active = false;
            isRow3Active = false;
            isRow4Active = false;
            isRow5Active = false;
        }

        #endregion

        #region Coroutines

        private IEnumerator DisplayMessageCoroutine(TextMeshPro textMesh, string message, float duration, Color color, int row)
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

            // Update row status
            if (row == 1)
            {
                isRow1Active = false;
                activeMessageCoroutine = null;
            }
            else if (row == 2)
            {
                isRow2Active = false;
                activeMessageCoroutine2 = null;
            }
            else if (row == 3)
            {
                isRow3Active = false;
                activeMessageCoroutine3 = null;
            }
            else if (row == 4)
            {
                isRow4Active = false;
                activeMessageCoroutine4 = null;
            }
            else if (row == 5)
            {
                isRow5Active = false;
                activeMessageCoroutine5 = null;
            }
        }

        private IEnumerator DisplayVisualBarCoroutine(Renderer renderer, BarAnimationStyle animationStyle, float duration, int row)
        {
            // Get the material
            Material barMaterial = renderer.material;
            
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
            
            // Update row status
            if (row == 1)
            {
                activeProgressBarCoroutine = null;
            }
            else if (row == 2)
            {
                activeProgressBarCoroutine2 = null;
            }
            else if (row == 3)
            {
                activeProgressBarCoroutine3 = null;
            }
            else if (row == 4)
            {
                activeProgressBarCoroutine4 = null;
            }
            else if (row == 5)
            {
                activeProgressBarCoroutine5 = null;
            }
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

            // Display for full duration (not subtracting fade times)
            yield return new WaitForSeconds(duration);

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
            
            // Ensure alpha is 0 at the end
            barColor = material.color;
            barColor.a = 0f;
            material.color = barColor;
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
            
            // Use full duration for pulse effect (not subtracting fade times)
            float pulseTime = duration;
            elapsedTime = 0f;
            
            while (elapsedTime < pulseTime)
            {
                // Calculate pulse alpha using sine wave
                float pulseAlpha = 0.5f + 0.5f * Mathf.Sin(elapsedTime * pulseFrequency * 2f * Mathf.PI);
                barColor = material.color;
                barColor.a = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha, pulseAlpha); // Pulse between min and max alpha
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
            
            // Ensure alpha is 0 at the end
            barColor = material.color;
            barColor.a = 0f;
            material.color = barColor;
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
            
            // Use full duration for blink effect (not subtracting fade times)
            float blinkTime = duration;
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
            
            // Ensure alpha is 0 at the end
            barColor = material.color;
            barColor.a = 0f;
            material.color = barColor;
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
            
            // Use full duration for warning effect (not subtracting fade times)
            float warningTime = duration;
            elapsedTime = 0f;
            
            while (elapsedTime < warningTime)
            {
                // Calculate warning flash alpha using sine wave
                float warningAlpha = 0.5f + 0.5f * Mathf.Sin(elapsedTime * warningFrequency * 2f * Mathf.PI);
                barColor = material.color;
                barColor.a = Mathf.Lerp(warningMinAlpha, warningMaxAlpha, warningAlpha); // Flash between min and max alpha
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
            
            // Ensure alpha is 0 at the end
            barColor = material.color;
            barColor.a = 0f;
            material.color = barColor;
        }

        private IEnumerator PopAndTransitionAnimation(Material material, float duration)
        {
            Color barColor = material.color;
            Color originalColor = barColor;
            
            // Pop in with white color
            barColor = Color.white;
            barColor.a = 1f;
            material.color = barColor;
            
            // Quick blink
            float blinkDuration = popBlinkDuration;
            float blinkElapsed = 0f;
            
            while (blinkElapsed < blinkDuration)
            {
                // Blink between white and original color
                float blinkValue = Mathf.Floor(Mathf.Repeat(blinkElapsed * popBlinkFrequency, 1f) + 0.5f);
                barColor = blinkValue > 0.5f ? Color.white : originalColor;
                barColor.a = 1f;
                material.color = barColor;
                
                blinkElapsed += Time.deltaTime;
                yield return null;
            }
            
            // Transition from white to original color
            float transitionTime = popTransitionTime;
            float transitionElapsed = 0f;
            
            while (transitionElapsed < transitionTime)
            {
                barColor = Color.Lerp(Color.white, originalColor, transitionElapsed / transitionTime);
                barColor.a = 1f;
                material.color = barColor;
                
                transitionElapsed += Time.deltaTime;
                yield return null;
            }
            
            // Set to original color
            barColor = originalColor;
            barColor.a = 1f;
            material.color = barColor;
            
            // Display for full duration (not subtracting previous effect times)
            yield return new WaitForSeconds(duration);
            
            // Fade out
            float elapsedTime = 0f;
            while (elapsedTime < fadeOutTime)
            {
                barColor = material.color;
                barColor.a = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutTime);
                material.color = barColor;
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // Ensure alpha is 0 at the end
            barColor = material.color;
            barColor.a = 0f;
            material.color = barColor;
        }

        private IEnumerator PopAndFadeAnimation(Material material, float duration)
        {
            Color barColor = material.color;
            
            // Pop in with full alpha
            barColor.a = 1f;
            material.color = barColor;
            
            // Hold for a moment
            yield return new WaitForSeconds(popHoldTime);
            
            // Fade to target alpha
            float fadeTime = popFadeTime;
            float fadeElapsed = 0f;
            float targetAlpha = 0.7f; // Target alpha value
            
            while (fadeElapsed < fadeTime)
            {
                barColor = material.color;
                barColor.a = Mathf.Lerp(1f, targetAlpha, fadeElapsed / fadeTime);
                material.color = barColor;
                
                fadeElapsed += Time.deltaTime;
                yield return null;
            }
            
            // Set to target alpha
            barColor = material.color;
            barColor.a = targetAlpha;
            material.color = barColor;
            
            // Display for full duration (not subtracting previous effect times)
            yield return new WaitForSeconds(duration);
            
            // Fade out
            float elapsedTime = 0f;
            while (elapsedTime < fadeOutTime)
            {
                barColor = material.color;
                barColor.a = Mathf.Lerp(targetAlpha, 0f, elapsedTime / fadeOutTime);
                material.color = barColor;
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // Ensure alpha is 0 at the end
            barColor = material.color;
            barColor.a = 0f;
            material.color = barColor;
        }

        #endregion

        private void OnDestroy()
        {
            // Clean up instanced material when destroyed
            if (instancedBarMaterial != null)
            {
                Destroy(instancedBarMaterial);
            }
            
            // Clean up additional instanced materials
            if (instancedBarMaterial2 != null)
            {
                Destroy(instancedBarMaterial2);
            }
            
            if (instancedBarMaterial3 != null)
            {
                Destroy(instancedBarMaterial3);
            }
            
            if (instancedBarMaterial4 != null)
            {
                Destroy(instancedBarMaterial4);
            }
            
            if (instancedBarMaterial5 != null)
            {
                Destroy(instancedBarMaterial5);
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