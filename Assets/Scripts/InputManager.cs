using UnityEngine;
using UnityEngine.Events;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    // Events that other scripts can subscribe to
    public UnityEvent onJumpPressed = new UnityEvent();
    public UnityEvent onJumpReleased = new UnityEvent();
    public UnityEvent onJumpHeld = new UnityEvent();

    // Input state properties
    public bool isJumpPressed { get; private set; }
    public bool isJumpHeld { get; private set; }
    public bool isJumpReleased { get; private set; }

    // Add this field at the top with other properties
    private bool inputEnabled = true;

    void Awake()
    {
        // Proper singleton pattern with DontDestroyOnLoad
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Add null check helper
    public static bool Exists()
    {
        return Instance != null;
    }

    // Methods that UI can call
    public void SetJumpPressed()
    {
        isJumpPressed = true;
        onJumpPressed.Invoke();
    }

    public void SetJumpHeld()
    {
        isJumpHeld = true;
        onJumpHeld.Invoke();
    }

    public void SetJumpReleased()
    {
        isJumpReleased = true;
        onJumpReleased.Invoke();
    }

    void LateUpdate()
    {
        // Reset states at end of frame
        isJumpPressed = false;
        isJumpHeld = false;
        isJumpReleased = false;
    }

    public void EnableInput()
    {
        inputEnabled = true;  // Now this will work
    }

    public void DisableInput()
    {
        inputEnabled = false;
    }

    // Update input checks to use this flag
    private void Update()
    {
        if (!inputEnabled) return;
        // ... existing input handling code ...
    }
} 