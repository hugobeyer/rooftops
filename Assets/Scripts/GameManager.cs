using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

namespace RoofTops
{
    // Achievement-related classes are now handled by GoalAchievementManager
    // TODO: Move this enum to GoalAchievementManager in the future
    public enum GoalType
    {
        Distance,    // Reach a specific distance
        Tridots,     // Collect a specific number of tridots
        Memcard      // Collect a specific number of memory cards
    }

    // Renamed from LogType to avoid conflict with Unity's LogType
    public enum LogCategory
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

        // Audio will be handled by a separate system
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
        public InputActionManager inputManager;  // Change to public so you can assign in inspector
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

        // Property to track the player's distance traveled
        public float PlayerDistance { get; set; }

        // Achievement-related variables (kept for backward compatibility)
        private int tridotsGoalIndex = 0;
        private int memcardGoalIndex = 0;
        private GoalType currentGoalType = GoalType.Distance;
        private bool goalAchieved = false;
        private float currentGoalValue = 0f;

        void Awake()
        {
            // Singleton pattern with DontDestroyOnLoad
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Store initial states
            initialTimeScale = Time.timeScale;
            initialGravity = Physics.gravity;

            // Find InputManager if not assigned
            if (inputManager == null)
            {
                inputManager = FindFirstObjectByType<InputActionManager>();
            }

            currentGravity = defaultGravity;
            Physics.gravity = new Vector3(0, currentGravity, 0);

            // Hide pause indicator immediately
            if (pauseIndicator != null)
            {
                pauseIndicator.SetActive(false);
            }

            // Initialize game data if null
            if (gameData == null)
            {
                gameData = ScriptableObject.CreateInstance<GameDataObject>();
            }

            // Load game data
            LoadGameData();
            
            // Achievement data is now handled by GoalAchievementManager

            // Set initial mixer pitch
            if (audioMixerForPitch != null)
            {
                audioMixerForPitch.SetFloat(pitchParameter, normalPitch);
            }
            
            // Register for game state changes
            OnGameStateChanged += HandleGameStateChanged;
            
            // Try to find player if not assigned - will be done again in StartGame if needed
            if (player == null)
            {
                // Wait a frame to ensure SceneReferenceManager is initialized
                StartCoroutine(FindPlayerDelayed());
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

        private void Start()
        {
            GamesState = GameStates.MainMenu;
            
            if (pauseIndicator != null)
            {
                pauseIndicator.SetActive(false);
            }

            if (gameplayUI != null)
            {
                gameplayUITransform = gameplayUI.transform;
            }

            //// Keep or find the panel controller if needed
            //if (panelController == null)
            //{
            //    panelController = FindObjectOfType<PanelController>();
            //}

            if (VistaPool.Instance != null)
            {
                VistaPool.Instance.ResetVistas();
            }

            // Now enable the UsePath feature
            if (targetMaterial != null)
            {
                targetMaterial.SetFloat("_UsePath", 1f);
            }
            
            // Achievement data is now handled by GoalAchievementManager

            // Setup music - only volume and playback settings
            // if (musicClip != null)
            // {
            //     // AudioSource.PlayClipAtPoint(musicClip, Vector3.zero);
            // }
            
            // Explicitly transition from StartingUp to MainMenu state
            if (currentState == GameStates.StartingUp)
            {
                RequestGameStateChange(GameStates.MainMenu);
            }
        }

        private void Update()
        {
            if (GamesState != GameStates.Playing)
            {
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

        private void OnDestroy()
        {

            OnGameStateChanged -= HandleGameStateChanged;
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

            GamesState = IsPaused ? previousState : GameStates.Paused;
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
            // Reset game state variables first
            HasGameStarted = false;
            IsPaused = false;
            Time.timeScale = 1.0f; // Ensure time scale is reset to normal
            
            // Reset any other game state variables
            if (VistaPool.Instance != null)
            {
                VistaPool.Instance.ResetVistas();
            }
            
            // Clear any pending coroutines
            StopAllCoroutines();
            
            // Disable UI elements that might interfere with scene transition
            if (deathUIPanel != null)
            {
                deathUIPanel.SetActive(false);
            }
            
            if (gameplayUI != null)
            {
                gameplayUI.gameObject.SetActive(false);
            }
            
            // Log that we're restarting the game
            Debug.Log("Restarting game...");
            
            // Reload the scene - do this LAST
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
            // This method only controls audio mixer effects, not direct audio playback
            // It's retained to control pitch effects that will be used by the separate audio system
            if (audioEffectCoroutine != null)
            {
                StopCoroutine(audioEffectCoroutine);
            }
            audioEffectCoroutine = StartCoroutine(JumpPadAudioEffect(duration, targetPitch));
        }

        private IEnumerator JumpPadAudioEffect(float duration, float targetPitch)
        {
            // This coroutine only controls audio mixer effects, not direct audio playback
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
            // Change game state to Playing
            GamesState = GameStates.Playing;
            
            if (!HasGameStarted)
            {
                HasGameStarted = true;

                // Check if GoalAchievementManager is properly initialized
                CheckGoalAchievementManager();

                // Log all DelayedActivation configurations
                LogAllDelayedActivationConfigurations();

                // Trigger goal messages to start showing after the game starts
                if (GoalAchievementManager.Instance != null)
                {
                    GoalAchievementManager.Instance.OnGameStart();
                }

                // Reset accumulated distance for the new run
                accumulatedDistance = 0f;

                Time.timeScale = timeSpeed;

                // Fire game started event
                onGameStarted.Invoke();

                // Start blending to normal speed
                if (ModulePool.Instance != null)
                {
                    // ModulePool doesn't have StartMoving method
                    // It uses isMoving property and listens to onGameStarted event
                    // The onGameStarted event we're triggering below will handle this
                    moduleSpeedStartTime = Time.time;
                }
                else
                {
                    Debug.LogWarning("ModulePool.Instance is null in StartGame!");
                }

                // Player should be spawned by CoreSceneSetup when entering Playing state
                // We'll just make sure it's found
                if (player == null)
                {
                    Debug.Log("GameManager: Player not assigned, trying to find it");
                    StartCoroutine(FindPlayerDelayed());
                }
                else
                {
                    Debug.Log("GameManager: Player already assigned");
                    
                    // Make sure the player is enabled with default animation
                    player.SetActive(true);

                    PlayerAnimatorController animController = player.GetComponent<PlayerAnimatorController>();
                    if (animController != null)
                    {
                        animController.ResetAnimationStates();
                        animController.ResetTurnState();

                        // Explicitly set the player to grounded
                        animController.SetBool("groundedBool", true);
                        //animController.SetBool("Jump", false);

                        // Now get the actual Animator component
                        Animator anim = animController.GetComponent<Animator>();
                        if (anim != null)
                        {
                            //anim.ResetTrigger("jumpTrigger");
                            anim.Play("runState", 0, 0f);
                        }
                    }
                }

                // Reveal the UI group now that the game has started.
                if (initialUIGroup != null)
                {
                    initialUIGroup.SetActive(true);
                }

                // Now enable the UsePath feature
                if (targetMaterial != null)
                {
                    targetMaterial.SetFloat("_UsePath", 1f);
                }
            }
            
            // Achievement tracking is now handled by GoalAchievementManager
        }

        private void CheckGoalAchievementManager()
        {
            if (GoalAchievementManager.Instance == null)
            {
                Debug.LogWarning("GoalAchievementManager.Instance is null when starting game!");
                return;
            }

            if (GoalAchievementManager.Instance.goalValuesManager == null)
            {
                Debug.LogWarning("GoalAchievementManager.Instance.goalValuesManager is null!");
                
                // Try to find and assign it
                GoalValuesManager goalValuesManager = FindObjectOfType<GoalValuesManager>();
                if (goalValuesManager != null)
                {
                    GoalAchievementManager.Instance.goalValuesManager = goalValuesManager;
                    Debug.Log("Found and assigned GoalValuesManager to GoalAchievementManager");
                }
                else
                {
                    Debug.LogError("Could not find GoalValuesManager in the scene!");
                }
            }
            else
            {
                Debug.Log("GoalAchievementManager is properly initialized with GoalValuesManager");
            }
        }

        private void LogAllDelayedActivationConfigurations()
        {
            Debug.Log("=== Logging all DelayedActivation configurations ===");
            DelayedActivation[] delayedActivations = FindObjectsOfType<DelayedActivation>();
            Debug.Log($"Found {delayedActivations.Length} DelayedActivation components");
            
            foreach (var da in delayedActivations)
            {
                da.LogConfiguration();
            }
            Debug.Log("=== End of DelayedActivation configurations ===");
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

            // Save game data
            SaveGameData();

            // Log the final distance
        }

        // New method to handle the overall game over (player death) logic.
        public void HandlePlayerDeath(float finalDistance, GameObject collidingHook = null)
        {
            GamesState = GameStates.GameOver;

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
                    animController.SetBool("groundedBool", true);
                    animController.SetBool("jumpTrigger", false);
                    //animController.SetBool("running", false);
                    animController.SetBool("airBool", false);

                    // Reset any other custom states
                    Animator anim = animController.GetComponent<Animator>();
                    if (anim != null)
                    {
                        // Reset all triggers
                        anim.ResetTrigger("jumpTrigger");
                        //anim.ResetTrigger("LandTrigger");

                        // Reset to idle state to ensure clean transition to ragdoll
                        //anim.Play("Idle", 0, 0f);

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

            // Show death UI, then reload after a delay
            StartCoroutine(ShowDeathUI());
        }

        private IEnumerator ShowDeathUI()
        {
            GamesState = GameStates.GameOver;

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
            // Reset game state variables first
            HasGameStarted = false;
            IsPaused = false;
            Time.timeScale = 1.0f; // Ensure time scale is reset to normal
            
            // Reset any other game state variables
            if (VistaPool.Instance != null)
            {
                VistaPool.Instance.ResetVistas();
            }
            
            // Clear any pending coroutines
            StopAllCoroutines();
            
            // Disable UI elements that might interfere with scene transition
            if (deathUIPanel != null)
            {
                deathUIPanel.SetActive(false);
            }
            
            if (gameplayUI != null)
            {
                gameplayUI.gameObject.SetActive(false);
            }
            
            // Reset the camera if available
            if (NoiseMovement.Instance != null)
            {
                // Reset the camera
                NoiseMovement.Instance.ResetCamera();
            }
            
            // Log that we're restarting the game
            Debug.Log("Restarting game...");
            
            // Reset game state to MainMenu
            GamesState = GameStates.MainMenu;
            
            // Get the current scene
            Scene currentScene = SceneManager.GetActiveScene();
            
            // Unload the current scene (but not the Core scene)
            if (currentScene.name != "Core")
            {
                // Unload the current scene
                SceneManager.UnloadSceneAsync(currentScene);
                
                // Load the main menu scene additively
                SceneManager.LoadScene("Main", LoadSceneMode.Additive);
            }
            else
            {
                // If we're somehow in the Core scene, just load the main menu
                SceneManager.LoadScene("Main", LoadSceneMode.Additive);
            }
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
                        rb.AddTorque(UnityEngine.Random.insideUnitSphere * torqueForce, ForceMode.Impulse);
                    }
                }

                // Disable original player
                player.SetActive(false);
            }
        }

        // Wait for initialPanelHideTime seconds, then show the panel


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

            // Achievement data is now handled by GoalAchievementManager

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

            // Achievement data is now handled by GoalAchievementManager
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

            // Clear all PlayerPrefs data first
            PlayerPrefs.DeleteAll();

            // Achievement data is now handled by GoalAchievementManager

            // Save the cleared data
            SaveGameData();
            
            // Notify GoalAchievementManager to reset its data if it exists
            if (GoalAchievementManager.Instance != null)
            {
                GoalAchievementManager.Instance.ResetCurrentGoalIndices();
            }
        }

        // Add this to handle application quit/pause
        private void OnApplicationQuit()
        {
            // Save data when the application is closed
            SaveGameData();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            GamesState = pauseStatus ? GameStates.Paused : previousState;
            // Save data when the application is paused (e.g., when switching to another app on mobile)
            if (pauseStatus)
            {
                SaveGameData();
            }
        }

        // Add this method to GameManager

        private void HandleGameStateChanged(GameStates oldState, GameStates newState)
        {
            // Handle MainMenu state
            if(newState == GameStates.MainMenu)
            {
                InputActionManager.Instance.OnJumpPressed.AddListener(StartGame);
                
                // Make sure UI elements for main menu are visible
                if (mainMenuUI != null)
                {
                    mainMenuUI.gameObject.SetActive(true);
                }
                
                // Ensure game over UI is hidden
                if (gameOverUI != null)
                {
                    gameOverUI.gameObject.SetActive(false);
                }
            }
            else
            {
                InputActionManager.Instance.OnJumpPressed.RemoveListener(StartGame);
            }
            
            // Handle Playing state
            if (newState == GameStates.Playing)
            {
                // Hide main menu UI
                if (mainMenuUI != null)
                {
                    mainMenuUI.gameObject.SetActive(false);
                }
                
                // Make sure player is found and active
                if (player == null)
                {
                    Debug.Log("GameManager: Player not found when entering Playing state, searching...");
                    StartCoroutine(FindPlayerDelayed());
                }
                else
                {
                    // Make sure player is active
                    player.SetActive(true);
                    Debug.Log("GameManager: Player found and activated in Playing state");
                }
            }
            
            // Handle GameOver state
            if (newState == GameStates.GameOver)
            {
                // Show game over UI
                if (gameOverUI != null)
                {
                    gameOverUI.gameObject.SetActive(true);
                }
            }
            
            // Handle Paused state
            if (newState == GameStates.Paused)
            {
                // Show pause indicator
                if (pauseIndicator != null)
                {
                    pauseIndicator.SetActive(true);
                }
            }
            else if (oldState == GameStates.Paused)
            {
                // Hide pause indicator when leaving paused state
                if (pauseIndicator != null)
                {
                    pauseIndicator.SetActive(false);
                }
            }
            
            Debug.Log($"GameManager: Game state changed from {oldState} to {newState}");
        }

        // Method to register UI elements with SceneReferenceManager
        public void RegisterUIElement(string id, GameObject uiElement)
        {
            if (uiElement != null)
            {
                SceneReferenceManager.Instance.RegisterUI(id, uiElement);
                Debug.Log($"GameManager: Registered UI element '{id}'");
            }
        }

        // Method to get UI elements from SceneReferenceManager
        public GameObject GetUIElement(string id)
        {
            return SceneReferenceManager.Instance.GetUI(id);
        }

        // Method to set the pause indicator
        public void SetPauseIndicator(GameObject indicator)
        {
            pauseIndicator = indicator;
            RegisterUIElement("PauseIndicator", indicator);
        }

        #region Game State

        public delegate void GameStateChanged(GameStates oldState, GameStates newState);

        public static GameStateChanged OnGameStateChanged;
        private static GameStates previousState = GameStates.StartingUp;
        private static GameStates currentState = GameStates.StartingUp;

        public static GameStates GamesState
        {
            get
            {
                return currentState;
            }
            private set
            {
                if (currentState != value)
                {
                    previousState = currentState;
                    currentState = value;
                    Debug.Log($"Game state changed: {previousState} → {currentState}");
                    OnGameStateChanged?.Invoke(previousState, currentState);
                }
            }
        }


        public static bool RequestGameStateChange(GameStates state)
        {
            if (currentState == state)
            {
                return false;
            }
            GamesState = state;
            return true;
        }
        #endregion // Game State

        // Coroutine to find player after a short delay
        private IEnumerator FindPlayerDelayed()
        {
            // Wait a frame to ensure SceneReferenceManager is initialized
            yield return null;
            
            // First try to get from SceneReferenceManager
            if (SceneReferenceManager.Instance != null)
            {
                player = SceneReferenceManager.Instance.GetGameObject("Player");
                if (player != null)
                {
                    Debug.Log("GameManager: Found player from SceneReferenceManager");
                    SetupPlayerAfterFind();
                    yield break;
                }
            }
            
            // If still null, try to find by tag
            if (player == null)
            {
                player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    Debug.Log("GameManager: Found player by tag");
                    SetupPlayerAfterFind();
                    yield break;
                }
            }
            
            // If still null, try to find by type
            if (player == null)
            {
                PlayerController playerController = FindObjectOfType<PlayerController>();
                if (playerController != null)
                {
                    player = playerController.gameObject;
                    Debug.Log("GameManager: Found player by PlayerController component");
                    SetupPlayerAfterFind();
                    yield break;
                }
                else
                {
                    Debug.LogWarning("GameManager: Could not find player!");
                }
            }
        }
        
        private void SetupPlayerAfterFind()
        {
            if (player != null)
            {
                // Make sure the player is enabled with default animation
                player.SetActive(true);

                PlayerAnimatorController animController = player.GetComponent<PlayerAnimatorController>();
                if (animController != null)
                {
                    animController.ResetAnimationStates();
                    animController.ResetTurnState();

                    // Explicitly set the player to grounded
                    animController.SetBool("groundedBool", true);
                    //animController.SetBool("Jump", false);

                    // Now get the actual Animator component
                    Animator anim = animController.GetComponent<Animator>();
                    if (anim != null)
                    {
                        ///anim.ResetTrigger("jumpTrigger");
                        anim.Play("runState", 0, 0f);
                    }
                }
                
                Debug.Log("GameManager: Player setup complete after finding");
            }
        }
    }

    public enum GameStates
    {
        StartingUp,
        MainMenu,
        Playing,
        Paused,
        GameOver,
        ShuttingDown
    }
}