using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

namespace RoofTops
{
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

        [Header("Time Control")]
        [Range(0.1f, 32f)] public float timeSpeed = 1f;

        [Header("Speed Settings")]
        public float initialGameSpeed = 2f;    // Starting speed when game begins
        public float normalGameSpeed = 6f;     // Normal speed to ramp up to
        public float speedIncreaseRate = 0.1f; // Rate at which speed increases over time
        public float speedRampDuration = 1.5f; // How long it takes to reach normal speed

        [Header("Data")]
        public GameDataObject gameData;

        [Header("Player Settings")]
        public GameObject player;

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

        // Timer for updates
        private float uiUpdateTimer;
        private const float UI_UPDATE_INTERVAL = 0.05f; // 20fps is enough for smooth UI

        [Header("Physics Settings")]
        public float defaultGravity = -9.81f;
        public float increasedGravity = -20f;  // Adjust this value as needed
        private float currentGravity;

        [Header("Required Components")]
        public InputActionManager inputManager;  // Change to public so you can assign in inspector
        public FootstepController footstepController;  // Add this line to reference footsteps
        public Material targetMaterial;  // Material for path effects

        [Header("Audio Settings")]
        public UnityEngine.Audio.AudioMixer audioMixerForPitch;
        public string pitchParameter = "Pitch";
        public float normalPitch = 1f;
        public float lowPitch = 0.8f;

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

        [Header("Debug Settings")]
        public static bool EnableDetailedLogs = false; // Set this to false to reduce log spam
        public bool showDetailedLogging = false;

        // Flag to indicate scene changes in progress
        private bool isChangingScenes = false;
        public bool IsChangingScenes => isChangingScenes;

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

            // Initialize game data if null
            if (gameData == null)
            {
                gameData = ScriptableObject.CreateInstance<GameDataObject>();
            }

            // Load game data
            LoadGameData();
            
            // Achievement data is now handled by GoalAchievementManager

            // Try to find player if not assigned - will be done again in StartGame if needed
            if (player == null)
            {
                // Wait a frame to ensure SceneReferenceManager is initialized
                StartCoroutine(FindPlayerDelayed());
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

        private void OnEnable()
        {
            // Subscribe to the game state changed event
            OnGameStateChanged += HandleGameStateChanged;
            Debug.Log("GameManager: Subscribed to OnGameStateChanged event");
        }

        private void Start()
        {
            GamesState = GameStates.MainMenu;
            
            // Ensure input is enabled in MainMenu state
            if (InputActionManager.Exists())
            {
                InputActionManager.Instance.InputActionsActivate();
                // Add jump listener to start game
                InputActionManager.Instance.OnJumpPressed.AddListener(StartGame);
                Debug.Log("GameManager: Enabled input and added jump listener for MainMenu state");
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
            
            // Achievement data is now handled by GoalAchievementManager

            // Explicitly transition from StartingUp to MainMenu state
            if (currentState == GameStates.StartingUp)
            {
                RequestGameStateChange(GameStates.MainMenu);
            }

            Debug.Log("GameManager: Started successfully");
        }

        // Update is called once per frame
        void Update()
        {
            // Toggle detailed logs with F2 key
            // if (Input.GetKeyDown(KeyCode.F2))
            // {
            //     ToggleDetailedLogs();
            // }

            // Only update when not paused
            if (!IsPaused)
            {
                // Update accumulated distance
                if (ModulePool.Instance != null && HasGameStarted)
                {
                    accumulatedDistance += ModulePool.Instance.currentMoveSpeed * Time.deltaTime;
                }

                // Auto-save if enabled
                if (enableAutoSave && Time.time - lastAutoSaveTime > autoSaveInterval)
                {
                    lastAutoSaveTime = Time.time;
                    SaveGameData();
                }
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
            Debug.Log("GameManager: ResetGame - START");
            
            // Reset game state
            GamesState = GameStates.MainMenu;
            HasGameStarted = false;
            accumulatedDistance = 0f;
            Debug.Log("GameManager: ResetGame - Game state variables reset");
            
            // Reset ModulePool speed
            if (ModulePool.Instance != null)
            {
                ModulePool.Instance.ResetSpeed();
                Debug.Log("GameManager: ResetGame - ModulePool speed reset");
            }
            else
            {
                Debug.Log("GameManager: ResetGame - ModulePool.Instance is NULL");
            }
            
            // Reset player state if it exists and is active
            if (player != null && player.activeInHierarchy)
            {
                Debug.Log("GameManager: ResetGame - Player exists and is active");
                
                // Reset player position
                player.transform.position = Vector3.zero;
                
                // Reset player velocity if it has a CharacterController
                PlayerController playerController = player.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    Debug.Log("GameManager: ResetGame - PlayerController found");
                    // We can't call ResetPlayer directly, so we'll just reset what we can
                    playerController.enabled = true;
                }
                else
                {
                    Debug.Log("GameManager: ResetGame - PlayerController is NULL");
                }
            }
            else
            {
                Debug.Log("GameManager: ResetGame - Player is NULL or inactive");
            }
            
            // Disable UI elements that might interfere with scene transition
            
            // Reset any other game state variables
            if (VistaPool.Instance != null)
            {
                VistaPool.Instance.ResetVistas();
                Debug.Log("GameManager: ResetGame - VistaPool reset");
            }
            else
            {
                Debug.Log("GameManager: ResetGame - VistaPool.Instance is NULL");
            }
            
            Debug.Log("GameManager: ResetGame - COMPLETE");
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
                Debug.LogWarning("GameManager: audioMixerForPitch is null, cannot apply audio effects");
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
            Debug.Log("GameManager: StartGame method called");
            
            // Change game state to Playing
            GamesState = GameStates.Playing;
            
            if (!HasGameStarted)
            {
                Debug.Log("GameManager: First time starting game, initializing...");
                HasGameStarted = true;
                
                // Start speed ramping
                StartCoroutine(RampSpeed());

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
                Debug.Log("GameManager: Invoking onGameStarted event");
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
                GoalValuesManager goalValuesManager = FindFirstObjectByType<GoalValuesManager>();
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
            DelayedActivation[] delayedActivations = FindObjectsByType<DelayedActivation>(FindObjectsSortMode.None);
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
            Debug.Log($"GameManager: HandlePlayerDeath - START, CurrentState={GamesState}, finalDistance={finalDistance}");
            
            // Change game state to GameOver
            GamesState = GameStates.GameOver;
            Debug.Log($"GameManager: HandlePlayerDeath - Set state to GameOver");

            // Save stats
            RecordFinalDistance(finalDistance);

            // Make sure the player is properly marked as dead
            if (player != null)
            {
                PlayerController playerController = player.GetComponent<PlayerController>();
                if (playerController != null && !playerController.IsDead)
                {
                    // Force the player to be marked as dead if it isn't already
                    Debug.LogWarning("GameManager: Player not marked as dead in HandlePlayerDeath, forcing Die() call");
                    playerController.Die();
                    return; // Exit early to let the Die method handle the rest of the death sequence
                }
                
                // Continue with normal death handling
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

            // Switch to ragdoll but don't disable the player yet
            SwitchToRagdollWithoutDisablingPlayer();

            // Slow down time for dramatic effect
            Time.timeScale = 0.5f;
            
            // Wait a moment before transitioning to game over state
            Debug.Log("GameManager: Starting TransitionToGameOver coroutine");
            StartCoroutine(TransitionToGameOver());
        }

        // Modified version that doesn't disable the player
        private void SwitchToRagdollWithoutDisablingPlayer()
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

                // Make the player invisible instead of disabling it
                // This keeps the GameObject active for Unity Ads but visually hidden
                Renderer[] renderers = player.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    renderer.enabled = false;
                }
                
                // Disable colliders on the player to prevent further interactions
                Collider[] playerColliders = player.GetComponentsInChildren<Collider>();
                foreach (Collider collider in playerColliders)
                {
                    collider.enabled = false;
                }
            }
        }

        private IEnumerator TransitionToGameOver()
        {
            Debug.Log("GameManager: TransitionToGameOver - START");
            
            // Wait a moment for dramatic effect
            yield return new WaitForSeconds(0.5f);
            
            // Reset time scale
            Time.timeScale = 1.0f;
            Debug.Log("GameManager: TransitionToGameOver - Reset time scale to 1.0");
            
            // Don't disable the player here, it might cause issues with Unity Ads
            // The player is already visually hidden by SwitchToRagdollWithoutDisablingPlayer
        }

        // Call this from the UI button
        public void RestartGame()
        {
            Debug.Log($"GameManager: RestartGame - START, CurrentState={GamesState}");
            
            // Set the changing scenes flag to true
            isChangingScenes = true;
            Debug.Log("GameManager: RestartGame - Set isChangingScenes flag to true");
            
            // Ensure time scale is reset
            Time.timeScale = 1f;
            Debug.Log("GameManager: RestartGame - Time scale reset");
            
            // Reset game state
            ResetGame();
            Debug.Log("GameManager: RestartGame - Game state reset");
            
            // Reload the current scene immediately
            Debug.Log("GameManager: RestartGame - About to reload scene");
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

        private void HandleGameStateChanged(GameStates oldState, GameStates newState)
        {
            // Handle MainMenu state
            if (newState == GameStates.MainMenu)
            {
                // Ensure input is enabled and jump listener is added
                if (InputActionManager.Exists())
                {
                    InputActionManager.Instance.InputActionsActivate();
                    InputActionManager.Instance.OnJumpPressed.AddListener(StartGame);
                    Debug.Log("GameManager: Enabled input and added jump listener for MainMenu state");
                }
            }
            else if (oldState == GameStates.MainMenu)
            {
                // Remove jump listener when leaving MainMenu
                if (InputActionManager.Exists())
                {
                    InputActionManager.Instance.OnJumpPressed.RemoveListener(StartGame);
                    Debug.Log("GameManager: Removed jump listener when leaving MainMenu state");
                }
            }
            
            // Handle Playing state
            if (newState == GameStates.Playing)
            {
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
                PlayerController playerController = FindFirstObjectByType<PlayerController>();
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

        // Simple speed ramp coroutine
        private IEnumerator RampSpeed()
        {
            // Set initial speed
            if (ModulePool.Instance != null)
            {
                ModulePool.Instance.currentMoveSpeed = initialGameSpeed;
            }
            
            float startTime = Time.time;
            
            while (Time.time < startTime + speedRampDuration)
            {
                float t = (Time.time - startTime) / speedRampDuration;
                float currentSpeed = Mathf.Lerp(initialGameSpeed, normalGameSpeed, t);
                
                if (ModulePool.Instance != null)
                {
                    ModulePool.Instance.currentMoveSpeed = currentSpeed;
                }
                
                yield return null;
            }
            
            // Ensure we end at exactly the target speed
            if (ModulePool.Instance != null)
            {
                ModulePool.Instance.currentMoveSpeed = normalGameSpeed;
            }
        }

        // Add these methods back
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

        // Add this method to filter logs
        public static void FilteredLog(string message, bool isEssential = false)
        {
            if (isEssential || EnableDetailedLogs)
            {
                Debug.Log(message);
            }
        }

        public static void ToggleDetailedLogs()
        {
            EnableDetailedLogs = !EnableDetailedLogs;
            Debug.Log($"Detailed logs are now {(EnableDetailedLogs ? "ENABLED" : "DISABLED")}");
        }
    }

    // GameStates enum has been moved to a separate file: GameStates.cs
    // public enum GameStates
    // {
    //     StartingUp,
    //     MainMenu,
    //     Playing,
    //     Paused,
    //     GameOver,
    //     ShuttingDown
    // }
}