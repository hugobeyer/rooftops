using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;
using RoofTops;
using System.Linq;

namespace RoofTops
{
    // Add the enum for different goal types
    public enum GoalType
    {
        Distance,    // Reach a specific distance
        Bonus,       // Collect a specific number of bonus items
        Survival,    // Survive for a specific time
        Jump         // Perform a specific number of jumps
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        
        [Header("Pause Indicator")]
        public GameObject pauseIndicator;
        
        [Header("Time Control")]
        [Range(0.1f, 32f)] public float timeSpeed = 1f;
        
        [Header("Speed Settings")]
        public float initialGameSpeed = 2f;    // Starting speed when game begins
        public float normalGameSpeed = 6f;     // Normal speed to ramp up to
        public float speedIncreaseRate = 0.1f; // Rate at which speed increases over time
        public float speedRampDuration = 1.5f; // How long it takes to reach normal speed

        [Header("Data")]
        public GameDataObject gameData;

        [Header("Audio")]
        public AudioSource musicSource;  // Main game music
        public AudioSource audioSource;  // General audio source
        public UnityEngine.Audio.AudioMixer audioMixerForPitch;  // Just assign the mixer with Master exposed
        public string pitchParameter = "Pitch";
        [Range(0.0f, 1.0f)] public float defaultMusicVolume = 0.8f;
        public float normalPitch = 1f;              // Normal music pitch
        public float lowPitch = 0.5f;              // Low pitch value

        [Header("Player Settings")]
        public GameObject player;
        
        [Header("Initial UI Group")]
        public GameObject initialUIGroup;

        [Header("UI Displays")]
        // public TMPro.TMP_Text distanceText;
        // public TMPro.TMP_Text bestText;
        // public TMPro.TMP_Text lastDistanceText;
        // public TMPro.TMP_Text bonusText;

        [Header("Helpers")]
        // We'll accumulate distance directly from ModulePool's currentMoveSpeed.
        private float accumulatedDistance = 0f;
        // Expose the accumulated distance as the current distance.
        public float CurrentDistance { get { return accumulatedDistance; } }

        private bool isPaused;
        private float storedGameSpeed;
        private float storedMovementSpeed;
        private Coroutine timeScaleCoroutine;
        private Coroutine audioEffectCoroutine;
        private float moduleSpeedStartTime;

        // New property to indicate the game hasn't started until the jump is pressed
        public bool HasGameStarted { get; private set; } = false;

        // Add the game started event
        public UnityEngine.Events.UnityEvent onGameStarted = new UnityEngine.Events.UnityEvent();

        public bool IsPaused 
        { 
            get => isPaused;
            set
            {
                isPaused = value;
                Time.timeScale = isPaused ? 0f : timeSpeed;
                
                // Pause/unpause music
                if (musicSource != null)
                {
                    if (isPaused)
                        musicSource.Pause();
                    else
                        musicSource.UnPause();
                }

                // Handle pause indicator
                if (pauseIndicator != null && HasGameStarted)
                {
                    pauseIndicator.SetActive(isPaused);
                }
            }
        }

        // Add this property to get the current speed
        public float CurrentSpeed 
        { 
            get 
            {
                if (ModulePool.Instance != null)
                    return ModulePool.Instance.gameSpeed;
                return initialGameSpeed;
            }
        }

        [Header("UI Controllers")]
        public GameplayUIController gameplayUI;
        public MainMenuController mainMenuUI;
        public GameOverUIController gameOverUI;

        private float currentUIHeight = 0f;  // Add this field to track current height
        public float uiHeightSmoothSpeed = 5f;  // Add this to control smoothing speed

        [Header("Shader Properties")]
        public Material targetMaterial;  // Material that uses the shader

        // Cache transform and use a timer for UI updates
        private Transform gameplayUITransform;
        private float uiUpdateTimer;
        private const float UI_UPDATE_INTERVAL = 0.05f; // 20fps is enough for smooth UI

        [Header("Physics Settings")]
        public float defaultGravity = -9.81f;
        public float increasedGravity = -20f;  // Adjust this value as needed
        private float currentGravity;

        [Header("Required Components")]
        public InputManager inputManager;  // Change to public so you can assign in inspector
        public FootstepController footstepController;  // Add this line to reference footsteps

        [Header("Death UI")]
        public GameObject deathUIPanel;          // Assign in inspector
        public float deathUIPanelDelay = 0.5f;   // Delay before showing UI

        [Header("Player References")]
        public GameObject playerRagdoll;  // Assign the ragdoll prefab in inspector

        [Header("Ragdoll Settings")]
        public float deathForce = 5f;         // Force applied on death
        public float upwardForce = 2f;        // Upward force component
        public float forwardForce = 2.5f;     // Forward force component
        public bool applyTorque = true;       // Option to add rotation force
        public float torqueForce = 2f;        // Amount of rotational force

        [Header("Ragdoll Physics")]
        public float colliderSkinWidth = 0.08f;     // Prevents clipping
        public float colliderBounciness = 0.3f;     // Makes ragdoll bounce slightly
        public bool useCCD = true;                  // Better collision detection

        // Store initial states
        private float initialTimeScale;
        private Vector3 initialGravity;

        [Header("UI Panel Settings")]
        public float initialPanelHideTime = 3f; // how long to hide panel at start

        private PanelController panelController; // reference to the panel script
        [Header("Popup After Panel Shows")]
        public GameObject popupMessage;      // The GameObject to show briefly
        public float popupDisplayTime = 2f;  // How many seconds it stays visible

        [Header("Goal Messages")]
        [SerializeField] private bool showGoalOnStart = true;
        [SerializeField] private float goalMessageDelay = 1.5f; // Delay before showing goal after game starts
        [SerializeField] private float goalAchievedMessageDelay = 0.5f; // Delay before showing the goal achieved message
        [SerializeField] private GoalType[] availableGoalTypes = new GoalType[] { GoalType.Distance, GoalType.Bonus }; // Available goal types
        [SerializeField] private float[] distanceGoalTiers = new float[] { 500f, 750f, 1000f, 1300f, 1600f, 2000f, 2500f, 3000f }; // Distance goal tiers
        [SerializeField] private float distanceGoalMultiplier = 1.2f; // Multiplier for distance goals based on best distance
        [SerializeField] private float minDistanceGoal = 50f; // Minimum distance goal
        [SerializeField] private float maxDistanceGoal = 3000f; // Maximum distance goal
        [SerializeField] private int minBonusGoal = 5; // Minimum bonus goal
        [SerializeField] private int maxBonusGoal = 30; // Maximum bonus goal
        [SerializeField] private float minSurvivalGoal = 30f; // Minimum survival time in seconds
        [SerializeField] private float maxSurvivalGoal = 120f; // Maximum survival time in seconds
        [SerializeField] private int minJumpGoal = 5; // Minimum jump goal
        [SerializeField] private int maxJumpGoal = 20; // Maximum jump goal

        // Current goal tracking
        private GoalType currentGoalType;
        private float currentGoalValue;
        private bool goalAchieved = false;

        [Header("Distance Achievements")]
        [SerializeField] private float[] distanceAchievementTiers = new float[] { 200f, 400f, 500f, 1000f, 2000f }; // Distance tiers in meters
        [SerializeField] private bool[] distanceAchievementsUnlocked; // Tracks which achievements have been unlocked

        void Awake()
        {
            Instance = this;
            
            // Store initial states
            initialTimeScale = Time.timeScale;
            initialGravity = Physics.gravity;
            
            // Find InputManager if not assigned
            if (inputManager == null)
            {
                inputManager = FindFirstObjectByType<InputManager>();
            }

            currentGravity = defaultGravity;
            Physics.gravity = new Vector3(0, currentGravity, 0);
            
            // Hide pause indicator immediately
            pauseIndicator?.SetActive(false);
            
            // Ensure gameData is initialized
            if (gameData == null)
            {
                gameData = ScriptableObject.CreateInstance<GameDataObject>();
            }

            // Setup music - only volume and playback settings
            if (musicSource == null)
            {
                musicSource = GetComponent<AudioSource>();
            }
            if (musicSource != null)
            {
                musicSource.volume = defaultMusicVolume;
                musicSource.loop = true;
                musicSource.playOnAwake = false;
                musicSource.Stop();
            }

            // Set initial mixer pitch
            if (audioMixerForPitch != null)
            {
                audioMixerForPitch.SetFloat(pitchParameter, normalPitch);
            }

            // Hide the player until the game starts (make sure to assign the player object in the Inspector)
            if (player != null)
            {
                player.SetActive(false);
            }
            
            // Hide the UI group until the game starts (assign the parent UI GameObject in the Inspector)
            if (initialUIGroup != null)
            {
                initialUIGroup.SetActive(false);
            }
            
            // Get the ads manager
            MonoBehaviour adsManager = FindFirstObjectByType<MonoBehaviour>();
            if (adsManager != null && adsManager.GetType().Name.Contains("UnityAdsManager"))
            {
                // Remove Debug.Log
                // Debug.Log("Found Unity Ads Manager");
            }

            // Disable the UsePath feature at startup
            if (targetMaterial != null)
            {
                targetMaterial.SetFloat("_UsePath", 0f);
            }

            // Initialize the achievement tracking array
            distanceAchievementsUnlocked = new bool[distanceAchievementTiers.Length];
        }

        void Start()
        {
            if (pauseIndicator != null)
            {
                pauseIndicator.SetActive(false);
            }

            if (gameplayUI != null)
            {
                gameplayUITransform = gameplayUI.transform;
            }

            // Keep or find the panel controller if needed
            if (panelController == null)
            {
                panelController = FindObjectOfType<PanelController>();
            }
        }

        void Update()
        {
            if (!HasGameStarted)
            {
                if (InputManager.Instance != null && InputManager.Instance.isJumpPressed)
                {
                    StartGame();
                }
                return;
            }

            if (isPaused) return;

            // Accumulate distance using ModulePool's currentMoveSpeed.
            if (ModulePool.Instance != null)
            {
                accumulatedDistance += ModulePool.Instance.currentMoveSpeed * Time.deltaTime;
            }

            // Update the distance text.
            // if (distanceText != null)
            // {
            //     distanceText.text = accumulatedDistance.ToString("F1") + " m";
            // }

            // You can update bonus (and other displays) if needed.
            if (!isPaused)
            {
                Time.timeScale = timeSpeed;
            }

            // Gradually increase module speed to normal using smooth blending
            if (ModulePool.Instance != null && Time.time < moduleSpeedStartTime + speedRampDuration)
            {
                float rawProgress = (Time.time - moduleSpeedStartTime) / speedRampDuration;
                float speedProgress = Mathf.SmoothStep(0f, 1f, rawProgress);
                float currentSpeed = Mathf.Lerp(2f, 6f, speedProgress);  // From slow to normal
                ModulePool.Instance.currentMoveSpeed = currentSpeed;
            }

            // Update UI position less frequently
            uiUpdateTimer += Time.deltaTime;
            if (uiUpdateTimer >= UI_UPDATE_INTERVAL && gameplayUITransform != null && ModulePool.Instance != null)
            {
                uiUpdateTimer = 0;
                float targetHeight = ModulePool.Instance.GetMaxModuleHeight();
                currentUIHeight = Mathf.Lerp(currentUIHeight, targetHeight, UI_UPDATE_INTERVAL * uiHeightSmoothSpeed);
                
                Vector3 uiPos = gameplayUITransform.position;
                uiPos.y = currentUIHeight + 0.5f; // Simplified UI height calculation
                gameplayUITransform.position = uiPos;
            }

            // Update shader with player position
            if (player != null && targetMaterial != null)
            {
                Vector4 playerPos = player.transform.position;
                targetMaterial.SetVector("_PlayerPosition", playerPos);
            }
            
            // Check if the goal has been achieved - only if we have a valid goal value
            if (showGoalOnStart && !goalAchieved && currentGoalValue > 0)
            {
                // Only start checking after a short delay to ensure the goal is properly set up
                if (Time.time > moduleSpeedStartTime + goalMessageDelay + 1.0f)
                {
                    CheckGoalProgress();
                }
            }
        }

        public void TogglePause()
        {
            if (!isPaused)
            {
                storedGameSpeed = timeSpeed;
                if (ModulePool.Instance != null)
                {
                    storedMovementSpeed = ModulePool.Instance.gameSpeed;
                }
            }

            IsPaused = !IsPaused;

            if (IsPaused)
            {
                Time.timeScale = 0f;
                ModulePool.Instance?.SetMovement(false);
            }
            else
            {
                Time.timeScale = storedGameSpeed;
                ModulePool.Instance?.SetMovement(true);
                if (ModulePool.Instance != null)
                {
                    ModulePool.Instance.currentMoveSpeed = storedMovementSpeed;
                }
            }
        }

        public void ResetGame()
        {
            // 4) Destroy the leftover player if it still exists
            if (player != null)
            {
                Destroy(player);
            }

            // Reset game state
            HasGameStarted = false;
            IsPaused = false;
            Time.timeScale = timeSpeed;
            
            // Reset goal tracking
            goalAchieved = false;
            currentGoalValue = 0f;
            accumulatedDistance = 0f;
            
            // Reset components
            if (player != null)
            {
                PlayerAnimatorController animController = player.GetComponent<PlayerAnimatorController>();
                if (animController != null)
                {
                    animController.ResetAnimationStates();
                    animController.ResetTurnState();
                    animController.SetBool("Jump", false);
                    
                    // Now get the actual Animator component
                    Animator anim = animController.GetComponent<Animator>();
                    if (anim != null)
                    {
                        anim.ResetTrigger("JumpTrigger");
                        anim.Play("Run", 0, 0f);
                    }
                }
            }

            if (VistaPool.Instance != null)
            {
                VistaPool.Instance.ResetVistas();
            }

            // Reload the scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void SlowTimeForJump(float targetScale = 0.25f, float duration = 0.75f)
        {
            if (timeScaleCoroutine != null)
            {
                StopCoroutine(timeScaleCoroutine);
            }
            timeScaleCoroutine = StartCoroutine(TimeScaleEffect(targetScale, duration));
        }

        private IEnumerator TimeScaleEffect(float targetScale, float duration)
        {
            float startScale = Time.timeScale;
            float elapsed = 0;

            while (elapsed < duration)
            {
                if (!isPaused)
                {
                    elapsed += Time.unscaledDeltaTime;
                    Time.timeScale = Mathf.Lerp(startScale, targetScale, elapsed / duration);
                }
                yield return null;
            }

            elapsed = 0;
            while (elapsed < duration)
            {
                if (!isPaused)
                {
                    elapsed += Time.unscaledDeltaTime;
                    Time.timeScale = Mathf.Lerp(targetScale, timeSpeed, elapsed / duration);
                }
                yield return null;
            }

            Time.timeScale = isPaused ? 0 : timeSpeed;
            timeScaleCoroutine = null;
        }

        public void ActivateJumpPadAudioEffect(float duration, float targetPitch)
        {
            if (audioEffectCoroutine != null)
            {
                StopCoroutine(audioEffectCoroutine);
            }
            audioEffectCoroutine = StartCoroutine(JumpPadAudioEffect(duration, targetPitch));
        }

        private IEnumerator JumpPadAudioEffect(float duration, float targetPitch)
        {
            if (audioMixerForPitch == null) 
            {
                yield break;
            }

            // Set to low pitch immediately
            audioMixerForPitch.SetFloat(pitchParameter, lowPitch);
            
            yield return new WaitForSeconds(duration);

            // Lerp back to normal pitch with fewer updates
            float elapsed = 0;
            float lerpDuration = 0.2f;
            
            while (elapsed < lerpDuration)
            {
                elapsed += Time.deltaTime;  // Use regular deltaTime instead of fixed intervals
                float t = elapsed / lerpDuration;
                float lerpedPitch = Mathf.Lerp(lowPitch, normalPitch, t);
                audioMixerForPitch.SetFloat(pitchParameter, lerpedPitch);
                yield return null;
            }

            // Ensure we end at exactly normal pitch
            audioMixerForPitch.SetFloat(pitchParameter, normalPitch);
            audioEffectCoroutine = null;
        }

        public void StartGame()
        {
            if (!HasGameStarted)
            {
                HasGameStarted = true;
                
                // Reset accumulated distance for the new run
                accumulatedDistance = 0f;
                
                // Reset goal achieved flag
                goalAchieved = false;
                
                // Hide the panel once the game actually starts
                if (panelController != null && panelController.gameObject != null)
                {
                    panelController.gameObject.SetActive(false);
                    StartCoroutine(ReEnablePanelAfterDelay());
                }

                Time.timeScale = timeSpeed;
                
                // Add this line to fire the event
                onGameStarted.Invoke();
                
                // Start playing the music if it isn't already playing.
                if (musicSource != null && !musicSource.isPlaying)
                {
                    musicSource.Play();
                }
                
                // Start blending to normal speed
                if (ModulePool.Instance != null)
                {
                    moduleSpeedStartTime = Time.time;
                }

                // 5) Make sure the player is enabled with default animation
                if (player != null)
                {
                    player.SetActive(true);

                    PlayerAnimatorController animController = player.GetComponent<PlayerAnimatorController>();
                    if (animController != null)
                    {
                        animController.ResetAnimationStates();
                        animController.ResetTurnState();
                        
                        // 1) Explicitly set the player to grounded
                        animController.SetBool("IsGrounded", true); 
                        // If you have a custom method like ForceGrounded(), you can call it here:
                        // animController.ForceGrounded();

                        animController.SetBool("Jump", false);
                        
                        // Now get the actual Animator component
                        Animator anim = animController.GetComponent<Animator>();
                        if (anim != null)
                        {
                            anim.ResetTrigger("JumpTrigger");
                            anim.Play("Run", 0, 0f);
                        }
                    }
                }
                
                // Reveal the UI group now that the game has started.
                if (initialUIGroup != null)
                {
                    initialUIGroup.SetActive(true);
                }

                if (VistaPool.Instance != null)
                {
                    VistaPool.Instance.ResetVistas();
                }

                // Now enable the UsePath feature
                if (targetMaterial != null)
                {
                    targetMaterial.SetFloat("_UsePath", 1f);
                }
                
                // Show goal message after a delay
                if (showGoalOnStart)
                {
                    StartCoroutine(StartMessageSequence());
                }
            }
        }

        /// <summary>
        /// Coordinates the sequence of storyline and goal messages
        /// </summary>
        private IEnumerator StartMessageSequence()
        {
            // Wait for initial delay after game start
            yield return new WaitForSeconds(goalMessageDelay);
            
            // Set distance goal based on player's progress
            currentGoalType = GoalType.Distance;
            
            // Find the appropriate distance goal tier based on player's best distance
            float bestDistance = gameData.bestDistance;
            float selectedGoal = distanceGoalTiers[0]; // Default to first tier
            
            // Find the next goal tier above player's best distance
            for (int i = 0; i < distanceGoalTiers.Length; i++)
            {
                if (bestDistance < distanceGoalTiers[i])
                {
                    selectedGoal = distanceGoalTiers[i];
                    break;
                }
                
                // If we've reached the end, use the highest tier
                if (i == distanceGoalTiers.Length - 1)
                {
                    selectedGoal = distanceGoalTiers[i];
                }
            }
            
            currentGoalValue = selectedGoal;
            
            // Make sure goal is not already achieved
            goalAchieved = false;
            
            // Log the goal setup
            Debug.Log($"Setting up goal: Type = {currentGoalType}, Value = {currentGoalValue}");
            
            // Display the goal message
            DisplayGoalMessage();
        }

        // New method to update bonus information centrally
        public void AddBonus(int amount)
        {
            // Check if this is a bonus collection (positive amount) or consumption (negative amount)
            bool isCollection = amount > 0;
            
            // Update the game data
            gameData.lastRunBonusCollected += amount;
            gameData.totalBonusCollected += amount;

            // Show dash info message if this is the first bonus collected and the message hasn't been shown before
            if (isCollection && gameData.lastRunBonusCollected == amount && !gameData.hasShownDashInfo && HasGameStarted)
            {
                // Show the dash info message
                if (GameMessageDisplay.Instance != null)
                {
                    GameMessageDisplay.Instance.ShowMessageByID("1ST_BONUS_DASH_INFO");
                    
                    // Mark the message as shown
                    gameData.hasShownDashInfo = true;
                    
                    // Log for debugging
                    Debug.Log("Showed dash info message for first bonus collection");
                }
            }

            // Immediately update the bonus text
            // if (bonusText != null)
            // {
            //     bonusText.text = gameData.lastRunBonusCollected.ToString();
            // }
        }

        // Method to handle memcard collection
        public void OnMemcardCollected(int amount)
        {
            // Update the memcard counts in game data
            gameData.lastRunMemcardsCollected += amount;
            gameData.totalMemcardsCollected += amount;
            
            // Update best run if current run is better
            if (gameData.lastRunMemcardsCollected > gameData.bestRunMemcardsCollected)
            {
                gameData.bestRunMemcardsCollected = gameData.lastRunMemcardsCollected;
            }
        }

        // New method to record final run distance.
        // It updates gameData (last run and best distance) and updates the corresponding UI displays.
        public void RecordFinalDistance(float finalDistance)
        {
            gameData.lastRunDistance = finalDistance;
            if (finalDistance > gameData.bestDistance)
            {
                gameData.bestDistance = finalDistance;
            }

            // Update UI displays if assigned
            // if (lastDistanceText != null)
            // {
            //     lastDistanceText.text = $"{finalDistance:F1} m";
            // }
            // if (bestText != null)
            // {
            //     bestText.text = $"{gameData.bestDistance:F1} m";
            // }
        }

        // New method to handle the overall game over (player death) logic.
        public void HandlePlayerDeath(float finalDistance)
        {
            // Save stats
            RecordFinalDistance(finalDistance);
            
            // Make sure the player's animator is completely reset before going ragdoll
            if (player != null)
            {
                PlayerAnimatorController animController = player.GetComponent<PlayerAnimatorController>();
                if (animController != null)
                {
                    // Reset all animation states
                    animController.ResetAnimationStates();
                    animController.ResetTurnState();
                    
                    // Reset all booleans to their default state
                    animController.SetBool("IsGrounded", true);
                    animController.SetBool("Jump", false);
                    animController.SetBool("IsRunning", false);
                    animController.SetBool("IsFalling", false);
                    
                    // Reset any other custom states
                    Animator anim = animController.GetComponent<Animator>();
                    if (anim != null)
                    {
                        // Reset all triggers
                        anim.ResetTrigger("JumpTrigger");
                        anim.ResetTrigger("LandTrigger");
                        
                        // Reset to idle state to ensure clean transition to ragdoll
                        anim.Play("Idle", 0, 0f);
                        
                        // Ensure the animator is in a stable state
                        anim.Update(0f);
                    }
                }
            }

            // Switch to ragdoll
            SwitchToRagdoll();
            
            // Hide gameplay UI
            gameplayUI?.gameObject.SetActive(false);
            
            // Stop music
            if (musicSource != null)
            {
                musicSource.Stop();
            }

            // Show death UI, then reload after a delay
            StartCoroutine(ShowDeathUI());
        }

        private IEnumerator ShowDeathUI()
        {
            // Wait for camera movement and ragdoll to settle
            yield return new WaitForSeconds(deathUIPanelDelay);
            
            // Fade in the death UI
            if (deathUIPanel != null)
            {
                // Optional: Add fade-in effect here
                deathUIPanel.SetActive(true);
            }
        }

        // Call this from the UI button
        public void RestartGame()
        {
            // Clean scene reload
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // void UpdateDisplays()
        // {
        //     if (GameManager.Instance != null && GameManager.Instance.gameData != null)
        //     {
        //         if (bestText != null)
        //         {
        //             bestText.text = $"{GameManager.Instance.gameData.bestDistance:F1} m";
        //         }
        //         if (lastDistanceText != null)
        //         {
        //             lastDistanceText.text = $"{GameManager.Instance.gameData.lastRunDistance:F1} m";
        //         }
        //     }
        // }

        public void SetGravity(float gravityValue)
        {
            currentGravity = gravityValue;
            Physics.gravity = new Vector3(0, currentGravity, 0);
        }

        public void IncreaseGravity()
        {
            SetGravity(increasedGravity);
        }

        public void ResetGravity()
        {
            SetGravity(defaultGravity);
        }

        void OnDisable()
        {
            // Remove disabling of gameActions:
        }

        private void SwitchToRagdoll()
        {
            if (playerRagdoll != null && player != null)
            {
                // Get source animator and disable it
                var sourceAnimator = player.GetComponent<Animator>();
                if (sourceAnimator != null)
                {
                    // Ensure the animator is completely disabled
                    sourceAnimator.enabled = false;
                    sourceAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    sourceAnimator.updateMode = AnimatorUpdateMode.Normal;
                }

                // Spawn ragdoll at player position
                GameObject ragdollInstance = Instantiate(playerRagdoll, 
                    player.transform.position, 
                    player.transform.rotation);

                // Copy pose from player to ragdoll
                if (sourceAnimator != null)
                {
                    // Get all the transforms from both hierarchies
                    Transform[] sourceTransforms = player.GetComponentsInChildren<Transform>();
                    Transform[] ragdollTransforms = ragdollInstance.GetComponentsInChildren<Transform>();

                    // Match rotations for each bone
                    foreach (Transform source in sourceTransforms)
                    {
                        // Find matching transform in ragdoll
                        Transform target = ragdollTransforms.FirstOrDefault(t => t.name == source.name);
                        if (target != null)
                        {
                            target.rotation = source.rotation;
                            target.position = source.position;
                        }
                    }
                }

                // Tell camera to transition to death view - DON'T change its target!
                var cameraController = FindFirstObjectByType<NoiseMovement>();
                if (cameraController != null)
                {
                    // Just trigger the transition, don't set the target
                    cameraController.TransitionToDeathView();
                }

                // Rest of the ragdoll setup...
                Rigidbody[] ragdollRBs = ragdollInstance.GetComponentsInChildren<Rigidbody>();
                Collider[] ragdollColliders = ragdollInstance.GetComponentsInChildren<Collider>();

                // Setup colliders to prevent clipping
                foreach (Collider col in ragdollColliders)
                {
                    if (col is CapsuleCollider capsule)
                    {
                        capsule.radius += colliderSkinWidth;
                    }
                    else if (col is SphereCollider sphere)
                    {
                        sphere.radius += colliderSkinWidth;
                    }
                    else if (col is BoxCollider box)
                    {
                        box.size += Vector3.one * colliderSkinWidth * 2;
                    }

                    // Add bouncy material if needed
                    if (colliderBounciness > 0)
                    {
                        PhysicsMaterial bouncyMaterial = new PhysicsMaterial
                        {
                            bounciness = colliderBounciness,
                            frictionCombine = PhysicsMaterialCombine.Multiply
                        };
                        col.material = bouncyMaterial;
                    }
                }

                // Setup rigidbodies with CCD if enabled
                foreach (Rigidbody rb in ragdollRBs)
                {
                    if (useCCD)
                    {
                        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                    }

                    Vector3 forceDirection = Vector3.back * deathForce + 
                                           Vector3.up * upwardForce + 
                                           Vector3.forward * forwardForce;

                    rb.AddForce(forceDirection, ForceMode.Impulse);

                    if (applyTorque)
                    {
                        rb.AddTorque(Random.insideUnitSphere * torqueForce, ForceMode.Impulse);
                    }
                }

                // Disable original player
                player.SetActive(false);
            }
        }

        // Wait for initialPanelHideTime seconds, then show the panel
        private IEnumerator ReEnablePanelAfterDelay()
        {
            yield return new WaitForSeconds(initialPanelHideTime);
            panelController.gameObject.SetActive(true);

            // Right after re-enabling the panel, show the extra popup
            if (popupMessage != null)
            {
                popupMessage.SetActive(true); 
                yield return new WaitForSeconds(popupDisplayTime);
                popupMessage.SetActive(false); 
            }
        }

        /// <summary>
        /// Displays a message when the goal is achieved
        /// </summary>
        private void DisplayGoalAchievedMessage()
        {
            if (GameMessageDisplay.Instance == null) return;
            
            // Add delay before showing the goal achieved message
            StartCoroutine(ShowGoalAchievedMessageWithDelay());
        }

        /// <summary>
        /// Shows the goal achieved message after a delay
        /// </summary>
        private IEnumerator ShowGoalAchievedMessageWithDelay()
        {
            yield return new WaitForSeconds(goalAchievedMessageDelay);
            GameMessageDisplay.Instance.ShowMessageByID("GOAL_ACHIEVED", "");
        }

        #region Goal System

        /// <summary>
        /// Calculates an appropriate goal value based on the player's progress
        /// </summary>
        private void CalculateGoalValue()
        {
            switch (currentGoalType)
            {
                case GoalType.Distance:
                    // Base goal on best distance, with a minimum
                    float bestDistance = gameData.bestDistance;
                    currentGoalValue = Mathf.Clamp(bestDistance * distanceGoalMultiplier, minDistanceGoal, maxDistanceGoal);
                    // Round to nearest 10
                    currentGoalValue = Mathf.Round(currentGoalValue / 10f) * 10f;
                    break;
                    
                case GoalType.Bonus:
                    // Set a reasonable bonus collection goal
                    currentGoalValue = Random.Range(minBonusGoal, maxBonusGoal + 1);
                    break;
                    
                case GoalType.Survival:
                    // Set a survival time goal
                    currentGoalValue = Random.Range(minSurvivalGoal, maxSurvivalGoal);
                    // Round to nearest 5 seconds
                    currentGoalValue = Mathf.Round(currentGoalValue / 5f) * 5f;
                    break;
                    
                case GoalType.Jump:
                    // Set a jump count goal
                    currentGoalValue = Random.Range(minJumpGoal, maxJumpGoal + 1);
                    break;
            }
        }
        
        /// <summary>
        /// Displays the goal message using the GameMessageDisplay
        /// </summary>
        private void DisplayGoalMessage()
        {
            if (GameMessageDisplay.Instance == null) return;
            
            switch (currentGoalType)
            {
                case GoalType.Distance:
                    GameMessageDisplay.Instance.ShowMessageByID("RUN_START_GOAL_DISTANCE", currentGoalValue);
                    break;
                    
                case GoalType.Bonus:
                    GameMessageDisplay.Instance.ShowMessageByID("RUN_START_GOAL_BONUS", (int)currentGoalValue);
                    break;
                    
                case GoalType.Survival:
                    int minutes = Mathf.FloorToInt(currentGoalValue / 60f);
                    int seconds = Mathf.FloorToInt(currentGoalValue % 60f);
                    string timeFormat = minutes > 0 ? $"{minutes}m {seconds}s" : $"{seconds}s";
                    GameMessageDisplay.Instance.ShowMessageByID("RUN_START_GOAL_SURVIVAL", timeFormat);
                    break;
                    
                case GoalType.Jump:
                    GameMessageDisplay.Instance.ShowMessageByID("RUN_START_GOAL_JUMP", (int)currentGoalValue);
                    break;
            }
        }
        
        /// <summary>
        /// Checks if the current goal has been achieved
        /// </summary>
        public void CheckGoalProgress()
        {
            if (goalAchieved) return;
            
            bool achieved = false;
            
            switch (currentGoalType)
            {
                case GoalType.Distance:
                    // Add debug logging to help diagnose the issue
                    Debug.Log($"Goal check: Current distance = {accumulatedDistance}, Goal = {currentGoalValue}");
                    achieved = accumulatedDistance >= currentGoalValue;
                    break;
                    
                case GoalType.Bonus:
                    achieved = gameData.lastRunBonusCollected >= currentGoalValue;
                    break;
                    
                case GoalType.Survival:
                    // You would need to track game time separately
                    // achieved = gameTime >= currentGoalValue;
                    break;
                    
                case GoalType.Jump:
                    // You would need to track jump count
                    // achieved = jumpCount >= currentGoalValue;
                    break;
            }
            
            if (achieved)
            {
                Debug.Log("Goal achieved!");
                goalAchieved = true;
                DisplayGoalAchievedMessage();
            }
        }

        /// <summary>
        /// Resets the goal system (for debugging)
        /// </summary>
        public void ResetGoalSystem()
        {
            Debug.Log("Resetting goal system");
            goalAchieved = false;
            currentGoalValue = 0f;
            accumulatedDistance = 0f;
        }

        #endregion

        /// <summary>
        /// Checks if any distance achievements have been reached
        /// </summary>
        private void CheckDistanceAchievements()
        {
            // Skip if no achievements defined
            if (distanceAchievementTiers == null || distanceAchievementTiers.Length == 0) return;
            
            // Make sure our tracking array is initialized
            if (distanceAchievementsUnlocked == null || distanceAchievementsUnlocked.Length != distanceAchievementTiers.Length)
            {
                distanceAchievementsUnlocked = new bool[distanceAchievementTiers.Length];
            }
            
            // Check each tier
            bool achievementUnlocked = false;
            string achievementValue = "";
            
            for (int i = 0; i < distanceAchievementTiers.Length; i++)
            {
                // Skip already unlocked achievements
                if (distanceAchievementsUnlocked[i]) continue;
                
                // Check if this tier has been reached
                if (accumulatedDistance >= distanceAchievementTiers[i])
                {
                    distanceAchievementsUnlocked[i] = true;
                    achievementUnlocked = true;
                    achievementValue = distanceAchievementTiers[i].ToString();
                    
                    // You could break here to only show one achievement at a time
                    // or continue to potentially show multiple achievements at once
                    break;
                }
            }
            
            // Show achievement message if any were unlocked
            if (achievementUnlocked && GameMessageDisplay.Instance != null)
            {
                // Show achievement message
                GameMessageDisplay.Instance.ShowMessageByID("DISTANCE_ACHIEVED", achievementValue);
            }
        }
    }
} 