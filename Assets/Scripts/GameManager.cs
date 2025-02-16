using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Controls")]
    public KeyCode pauseKey = KeyCode.P;
    
    [Header("Pause Indicator")]
    public GameObject pauseIndicator;  // Assign in inspector
    
    [Header("Time Control")]
    [Range(0.1f, 2f)] public float gameSpeed = 1f;
    
    [Header("Data")]
    public GameDataObject gameData;  // Assign your GameData.asset here
    
    private bool isPaused;
    // Add storage for game speed and module movement speed
    private float storedGameSpeed;
    private float storedMovementSpeed;
    public bool IsPaused 
    { 
        get => isPaused;
        set
        {
            isPaused = value;
            Time.timeScale = isPaused ? 0f : gameSpeed;
        }
    }

    private Coroutine timeScaleCoroutine;

    void Awake()
    {
        Instance = this;

        // Make sure indicator starts invisible
        if (pauseIndicator != null)
        {
            pauseIndicator.SetActive(false);
        }
    }

    void Update()
    {
        HandleTimeScale();
        HandlePauseInput();
    }

    void HandleTimeScale()
    {
        if (!isPaused)  // Only update time scale when not paused
        {
            Time.timeScale = gameSpeed;
        }
    }

    void HandlePauseInput()
    {
        bool pausePressed = Input.GetKeyDown(pauseKey) || 
                           UnityEngine.InputSystem.Keyboard.current.pKey.wasPressedThisFrame;
        
        if (pausePressed)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        // When pausing, store the current values so they can be restored on unpause.
        if (!isPaused)
        {
            storedGameSpeed = gameSpeed;
            if (ModulePool.Instance != null)
            {
                storedMovementSpeed = ModulePool.Instance.currentMoveSpeed;
            }
        }

        // Toggle pause state
        IsPaused = !IsPaused;

        if (IsPaused)
        {
            Time.timeScale = 0f;
            ModulePool.Instance?.SetMovement(false);
        }
        else
        {
            // Restore the saved speeds.
            Time.timeScale = storedGameSpeed;
            ModulePool.Instance?.SetMovement(true);
            if (ModulePool.Instance != null)
            {
                ModulePool.Instance.currentMoveSpeed = storedMovementSpeed;
            }
        }

        UpdatePauseIndicator();
    }

    void UpdatePauseIndicator()
    {
        if (pauseIndicator != null)
        {
            pauseIndicator.SetActive(IsPaused);
        }
    }

    public void ResetGame()
    {
        // Reset the current run counter
        gameData.lastRunBonusCollected = 0;
        
        // Fully reload the current scene
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
        // Slow down
        float startScale = Time.timeScale;
        float elapsed = 0;

        while (elapsed < duration)
        {
            if (!isPaused)  // Only modify time if not paused
            {
                elapsed += Time.unscaledDeltaTime;
                Time.timeScale = Mathf.Lerp(startScale, targetScale, elapsed / duration);
            }
            yield return null;
        }

        // Return to normal
        elapsed = 0;
        while (elapsed < duration)
        {
            if (!isPaused)  // Only modify time if not paused
            {
                elapsed += Time.unscaledDeltaTime;
                Time.timeScale = Mathf.Lerp(targetScale, gameSpeed, elapsed / duration);
            }
            yield return null;
        }

        Time.timeScale = isPaused ? 0 : gameSpeed;
        timeScaleCoroutine = null;
    }
} 