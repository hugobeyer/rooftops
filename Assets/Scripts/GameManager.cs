using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;
using RoofTops;
using System.Linq;

namespace RoofTops
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        
        [Header("Pause Indicator")]
        public GameObject pauseIndicator;
        
        [Header("Time Control")]
        [Range(0.1f, 2f)] public float timeSpeed = 1f;
        
        [Header("Speed Settings")]
        public float initialGameSpeed = 2f;    // Starting speed when game begins
        public float normalGameSpeed = 6f;     // Normal speed to ramp up to
        public float speedIncreaseRate = 0.1f; // Rate at which speed increases over time
        public float speedRampDuration = 1.5f; // How long it takes to reach normal speed

        [Header("Data")]
        public GameDataObject gameData;

        [Header("Audio")]
        public AudioSource musicSource;  // Main game music
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
            // Reset game state
            HasGameStarted = false;
            IsPaused = false;
            Time.timeScale = timeSpeed;
            
            // Reset components
            if (player != null)
            {
                PlayerAnimatorController animController = player.GetComponent<PlayerAnimatorController>();
                if (animController != null)
                {
                    animController.ResetAnimationStates();
                    animController.ResetTurnState();
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

                // Reveal the player now that the game has started.
                if (player != null)
                {
                    player.SetActive(true);
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
            }
        }

        // New method to update bonus information centrally
        public void AddBonus(int amount)
        {
            // Update the game data
            gameData.lastRunBonusCollected += amount;
            gameData.totalBonusCollected += amount;

            // Immediately update the bonus text
            // if (bonusText != null)
            // {
            //     bonusText.text = gameData.lastRunBonusCollected.ToString();
            // }
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
            
            // Spawn ragdoll for visual effect
            SwitchToRagdoll();
            
            // Hide gameplay UI
            gameplayUI?.gameObject.SetActive(false);
            
            // Stop music
            if (musicSource != null)
            {
                musicSource.Stop();
            }

            // Show death UI after a short delay
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
                    sourceAnimator.enabled = false;
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
    }
} 