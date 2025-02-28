using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;
using RoofTops;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace RoofTops
{
    /// <summary>
    /// Container for all achievement save data
    /// </summary>
    [System.Serializable]
    public class AchievementSaveData
    {
        public string lastSaveTime;
        public Dictionary<string, float> playerMetrics = new Dictionary<string, float>();
        public int currentGoalType;
        public float currentGoalValue;
        public bool goalAchieved;
        public bool hasShownDashInfo = false; // Add this field to track tutorial message state
        public List<CompletedGoal> completedGoals = new List<CompletedGoal>();
    }

    // Add this class to store completed goal information
    [System.Serializable]
    public class CompletedGoal
    {
        public int goalType;
        public float goalValue;
        public string completionTime;
        
        public CompletedGoal(GoalType type, float value)
        {
            goalType = (int)type;
            goalValue = value;
            completionTime = System.DateTime.UtcNow.ToString("o");
        }
    }

    // Add the enum for different goal types
    public enum GoalType
    {
        Distance,    // Reach a specific distance
        Tridots,     // Collect a specific number of tridots
        Memcard      // Collect a specific number of memory cards
    }

    // At the top of the file, outside the class
    public enum LogCategory  // Renamed from LogType
    {
        Log,
        Warning,
        Error
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
        // (No UI Text fields needed)

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

        [Header("Auto-Save Settings")]
        [SerializeField] private float autoSaveInterval = 30f; // Save every 30 seconds
        [SerializeField] public bool enableAutoSave = true;
        private float lastAutoSaveTime = 0f;

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

        // Current goal tracking
        private GoalType currentGoalType;
        private float currentGoalValue;
        private bool goalAchieved = false;

        [Header("Achievement Settings")]
        [SerializeField] private bool enableAchievementMessages = true;
        [SerializeField] public bool saveAchievementsToJson = true;
        [SerializeField] private string achievementSaveFileName = "achievements.json";
        [SerializeField] private GoalType[] availableGoalTypes = new GoalType[] 
        { 
            GoalType.Distance, 
            GoalType.Tridots, 
            GoalType.Memcard
        };
        
        // Player metrics dictionary for tracking various stats
        private Dictionary<string, float> playerMetrics = new Dictionary<string, float>();
        
        // Events for achievement system
        public UnityEngine.Events.UnityEvent<string, float> onAchievementUnlocked = new UnityEngine.Events.UnityEvent<string, float>();
        public UnityEngine.Events.UnityEvent<string, float, float> onGoalProgress = new UnityEngine.Events.UnityEvent<string, float, float>();
        
        // Achievement save data
        private AchievementSaveData saveData = new AchievementSaveData();

        private int tridotsGoalIndex = 0;
        private int memcardGoalIndex = 0;

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
            
            // Initialize goal indices
            tridotsGoalIndex = PlayerPrefs.GetInt("TridotsGoalIndex", 0);
            memcardGoalIndex = PlayerPrefs.GetInt("MemcardGoalIndex", 0);
            
            // Set initial goal type to Distance (default)
            currentGoalType = GoalType.Distance;
            
            // Load game data and achievements
            LoadGameData();

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

            if (VistaPool.Instance != null)
            {
                VistaPool.Instance.ResetVistas();
            }

            // Now enable the UsePath feature
            if (targetMaterial != null)
            {
                targetMaterial.SetFloat("_UsePath", 1f);
            }
        }

        public void Update()
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
                
                // Update EconomyManager with current distance
                if (EconomyManager.Instance != null)
                {
                    EconomyManager.Instance.UpdateDistance(accumulatedDistance);
                }
            }

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
            
            // Auto-save game data periodically
            if (enableAutoSave && Time.time - lastAutoSaveTime > autoSaveInterval)
            {
                SaveGameData();
                
                lastAutoSaveTime = Time.time;
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
                
                // Save game data when pausing
                if (HasGameStarted)
                {
                    SaveGameData();
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
                
                // Trigger goal messages to start showing after the game starts
                if (GoalAchievementManager.Instance != null)
                {
                    GoalAchievementManager.Instance.OnGameStart();
                }
                
                // Reset accumulated distance for the new run
                accumulatedDistance = 0f;
                
                // Reset goal achieved flag
                goalAchieved = false;
                
                // Reset session-specific metrics for the new run
                
            
                
                // Reset game data
                if (gameData != null)
                {
                    gameData.lastRunDistance = 0f;
                    gameData.lastRunTridotCollected = 0;
                    gameData.lastRunMemcardsCollected = 0;
                }
                
                // Hide the panel once the game actually starts
                if (panelController != null && panelController.gameObject != null)
                {
                    panelController.gameObject.SetActive(false);
                    StartCoroutine(ReEnablePanelAfterDelay());
                }

                Time.timeScale = timeSpeed;
                
                // Set initial distance metric
                
                // Fire game started event
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
                

            }
        }

        public void RecordFinalDistance(float finalDistance)
        {
            // Update GameDataObject
            if (gameData != null)
            {
                gameData.lastRunDistance = finalDistance;
                
                // Update best distance if this run was better
                if (finalDistance > gameData.bestDistance)
                {
                    gameData.bestDistance = finalDistance;
                }
            }
            
            // Update player metrics

            
            // Save game data
            SaveGameData();

            // Log the final distance
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

            // CENTRALIZED MOVEMENT CONTROL: This is the single place where all movement systems are stopped
            // Stop all movement systems
            if (ModulePool.Instance != null)
            {
                ModulePool.Instance.StopMovement();
            }
            
            // Also stop the UnifiedSpawnManager if it exists
            var unifiedSpawnManager = FindFirstObjectByType<UnifiedSpawnManager>();
            if (unifiedSpawnManager != null)
            {
                unifiedSpawnManager.StopMovement();
            }
            
            // Also stop the PatternSpawning if it exists
            var patternSpawning = FindFirstObjectByType<PatternSpawning>();
            if (patternSpawning != null)
            {
                patternSpawning.StopMovement();
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

        public void SaveGameData()
        {
            // Save distance records
            PlayerPrefs.SetFloat("BestDistance", gameData.bestDistance);
            PlayerPrefs.SetFloat("LastRunDistance", gameData.lastRunDistance);
            
            // Save tridots collection
            PlayerPrefs.SetInt("TotalTridotCollected", gameData.totalTridotCollected);
            PlayerPrefs.SetInt("LastRunTridotCollected", gameData.lastRunTridotCollected);
            PlayerPrefs.SetInt("BestRunTridotCollected", gameData.bestRunTridotCollected);
            
            // Save memcard collection
            PlayerPrefs.SetInt("TotalMemcardsCollected", gameData.totalMemcardsCollected);
            PlayerPrefs.SetInt("LastRunMemcardsCollected", gameData.lastRunMemcardsCollected);
            PlayerPrefs.SetInt("BestRunMemcardsCollected", gameData.bestRunMemcardsCollected);
            
            // Save tutorial flags
            PlayerPrefs.SetInt("HasShownDashInfo", gameData.hasShownDashInfo ? 1 : 0);
            
            // Save current goal data
            PlayerPrefs.SetInt("CurrentGoalType", (int)currentGoalType);
            PlayerPrefs.SetFloat("CurrentGoalValue", currentGoalValue);
            PlayerPrefs.SetInt("GoalAchieved", goalAchieved ? 1 : 0);
            
            // Ensure data is written to disk
            PlayerPrefs.Save();
            
        }

        /// <summary>
        /// Loads all game data from PlayerPrefs
        /// </summary>
        public void LoadGameData()
        {
            // Load distance records
            gameData.bestDistance = PlayerPrefs.GetFloat("BestDistance", 0f);
            gameData.lastRunDistance = PlayerPrefs.GetFloat("LastRunDistance", 0f);
            
            // Load tridots collection
            gameData.totalTridotCollected = PlayerPrefs.GetInt("TotalTridotCollected", 0);
            gameData.lastRunTridotCollected = PlayerPrefs.GetInt("LastRunTridotCollected", 0);
            gameData.bestRunTridotCollected = PlayerPrefs.GetInt("BestRunTridotCollected", 0);
            
            // Load memcard collection
            gameData.totalMemcardsCollected = PlayerPrefs.GetInt("TotalMemcardsCollected", 0);
            gameData.lastRunMemcardsCollected = PlayerPrefs.GetInt("LastRunMemcardsCollected", 0);
            gameData.bestRunMemcardsCollected = PlayerPrefs.GetInt("BestRunMemcardsCollected", 0);
            
            // Load tutorial flags
            gameData.hasShownDashInfo = PlayerPrefs.GetInt("HasShownDashInfo", 0) == 1;
            
            // Load current goal data
            currentGoalType = (GoalType)PlayerPrefs.GetInt("CurrentGoalType", 0);
            currentGoalValue = PlayerPrefs.GetFloat("CurrentGoalValue", 0f);
            goalAchieved = PlayerPrefs.GetInt("GoalAchieved", 0) == 1;
            
        }

        /// <summary>
        /// Clears all saved game data (for testing or reset functionality)
        /// </summary>
        public void ClearGameData()
        {
            // Reset all game data
            gameData.bestDistance = 0f;
            gameData.lastRunDistance = 0f;
            gameData.totalTridotCollected = 0;
            gameData.lastRunTridotCollected = 0;
            gameData.bestRunTridotCollected = 0;
            gameData.totalMemcardsCollected = 0;
            gameData.lastRunMemcardsCollected = 0;
            gameData.bestRunMemcardsCollected = 0;
            gameData.hasShownDashInfo = false;
            
            // Reset goal data
            currentGoalType = GoalType.Distance;
            currentGoalValue = 0f;
            goalAchieved = false;
            
            // Clear all PlayerPrefs data first
            PlayerPrefs.DeleteAll();
            
            // Reset goal indices
            tridotsGoalIndex = 0;
            memcardGoalIndex = 0;
            
            // Save the reset indices to PlayerPrefs
            PlayerPrefs.SetInt("DistanceGoalIndex", 0);
            PlayerPrefs.SetInt("TridotsGoalIndex", tridotsGoalIndex);
            PlayerPrefs.SetInt("MemcardGoalIndex", memcardGoalIndex);
            PlayerPrefs.Save();
            
            // Clear player metrics
            playerMetrics.Clear();
            
            // Reset achievement save data
            saveData = new AchievementSaveData();
            
            // Save the cleared data
            SaveGameData();
        }

        // Add this to handle application quit/pause
        private void OnApplicationQuit()
        {
            // Save data when the application is closed
            SaveGameData();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            // Save data when the application is paused (e.g., when switching to another app on mobile)
            if (pauseStatus)
            {
                SaveGameData();
            }
        }

        
       


    }
} 