using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;
using UnityEngine.InputSystem;
using RoofTops.GameActions;

public class GameManager : MonoBehaviour, GameActions.IPlayerActions
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

    private GameActions gameActions;

    void Awake()
    {
        Instance = this;
        gameActions = new GameActions();
        gameActions.Player.AddCallbacks(this);
        gameActions.Enable();
        
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
        
        // UpdateDisplays();
    }

    void Start()
    {
        // Double-check pause indicator is hidden
        if (pauseIndicator != null)
        {
            pauseIndicator.SetActive(false);
        }

        if (gameplayUI != null)
        {
            gameplayUITransform = gameplayUI.transform;
        }
    }

    void Update()
    {
        if (!HasGameStarted)
        {
            // Remove this legacy input check
            // if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space))
            // {
            //     StartGame();
            // }
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
        gameData.lastRunBonusCollected = 0;
        // Reset the bonus text display
        BonusTextDisplay.Instance?.ResetTotal();
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
        HasGameStarted = true;
        Time.timeScale = timeSpeed;
        
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
        // Record final stats.
        RecordFinalDistance(finalDistance);
        
        // Show the global death message.
        DeathMessageDisplay.Instance?.ShowMessage();
        
        // Transition the camera or apply any global death effects.
        FindFirstObjectByType<NoiseMovement>()?.TransitionToDeathView();
        
        // Finally, trigger the delayed reset.
        StartCoroutine(DelayedReset());
    }

    // Coroutine to wait a moment before resetting the game.
    private IEnumerator DelayedReset()
    {
        // Optionally hide the death message.
        DeathMessageDisplay.Instance?.HideMessage();
        
        // Wait for 1 second (adjust the wait time if needed)
        yield return new WaitForSeconds(1.0f);
        
        // Reset the game (assuming ResetGame() performs a scene reload or similar reset)
        ResetGame();
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

    // Implement the input system callback
    public void OnJump(InputAction.CallbackContext context)
    {
        if (!HasGameStarted && context.performed)
        {
            StartGame();
        }
    }

    void OnDisable()
    {
        if (gameActions != null)
        {
            gameActions.Disable();
        }
    }
} 