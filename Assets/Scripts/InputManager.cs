using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

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

    [Header("Keyboard Settings")]
    [Tooltip("Enable or disable keyboard input")]
    public bool enableKeyboardInput = true;

    [Tooltip("Key used for jumping")]
    public KeyCode jumpKey = KeyCode.Space;

    // Track the previous frame's key state
    private bool wasKeyPressed = false;

    // Dictionary to store registered input actions
    private Dictionary<string, InputAction> registeredActions = new Dictionary<string, InputAction>();

    // Class to represent an input action
    [System.Serializable]
    public class InputAction
    {
        public KeyCode key;
        public UnityEvent onPressed = new UnityEvent();
        public UnityEvent onReleased = new UnityEvent();
        public UnityEvent onHeld = new UnityEvent();
        public bool isPressed { get; private set; }
        public bool isHeld { get; private set; }
        public bool isReleased { get; private set; }

        public void UpdateState(bool pressed, bool held, bool released)
        {
            isPressed = pressed;
            isHeld = held;
            isReleased = released;

            if (pressed) onPressed.Invoke();
            if (held) onHeld.Invoke();
            if (released) onReleased.Invoke();
        }

        public void Reset()
        {
            isPressed = false;
            isHeld = false;
            isReleased = false;
        }
    }

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

    // Register a new input action
    public void RegisterAction(string actionName, KeyCode key)
    {
        if (!registeredActions.ContainsKey(actionName))
        {
            InputAction newAction = new InputAction();
            newAction.key = key;
            registeredActions.Add(actionName, newAction);
        }
        else
        {
            // Update existing action's key
            registeredActions[actionName].key = key;
        }
    }

    // Get an input action state
    public bool GetButtonDown(string actionName)
    {
        if (registeredActions.TryGetValue(actionName, out InputAction action))
        {
            return action.isPressed;
        }
        return false;
    }

    // Get an input action held state
    public bool GetButton(string actionName)
    {
        if (registeredActions.TryGetValue(actionName, out InputAction action))
        {
            return action.isHeld;
        }
        return false;
    }

    // Get an input action released state
    public bool GetButtonUp(string actionName)
    {
        if (registeredActions.TryGetValue(actionName, out InputAction action))
        {
            return action.isReleased;
        }
        return false;
    }

    // Subscribe to an action's events
    public void SubscribeToPressed(string actionName, UnityAction callback)
    {
        if (registeredActions.TryGetValue(actionName, out InputAction action))
        {
            action.onPressed.AddListener(callback);
        }
    }

    public void SubscribeToHeld(string actionName, UnityAction callback)
    {
        if (registeredActions.TryGetValue(actionName, out InputAction action))
        {
            action.onHeld.AddListener(callback);
        }
    }

    public void SubscribeToReleased(string actionName, UnityAction callback)
    {
        if (registeredActions.TryGetValue(actionName, out InputAction action))
        {
            action.onReleased.AddListener(callback);
        }
    }

    void LateUpdate()
    {
        // Reset states at end of frame
        isJumpPressed = false;
        isJumpHeld = false;
        isJumpReleased = false;

        // Reset all registered actions
        foreach (var action in registeredActions.Values)
        {
            action.Reset();
        }
    }

    public void EnableInput()
    {
        inputEnabled = true;
    }

    public void DisableInput()
    {
        inputEnabled = false;
    }

    // Update input checks to use this flag
    private void Update()
    {
        if (!inputEnabled) return;

        // Handle keyboard input if enabled
        if (enableKeyboardInput)
        {
            // Handle jump key (legacy support)
            if (Input.GetKeyDown(jumpKey))
            {
                SetJumpPressed();
                wasKeyPressed = true;
            }

            if (Input.GetKey(jumpKey))
            {
                SetJumpHeld();
            }

            if (Input.GetKeyUp(jumpKey))
            {
                SetJumpReleased();
                wasKeyPressed = false;
            }

            // Process all registered actions
            foreach (var entry in registeredActions)
            {
                string actionName = entry.Key;
                InputAction action = entry.Value;

                bool pressed = Input.GetKeyDown(action.key);
                bool held = Input.GetKey(action.key);
                bool released = Input.GetKeyUp(action.key);

                action.UpdateState(pressed, held, released);
            }
        }
    }
}