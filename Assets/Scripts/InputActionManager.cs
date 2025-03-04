using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System;
using System.Collections;

public class InputActionManager : MonoBehaviour
{
    #region Properties

    public static InputActionManager Instance { get; private set; }
    [Header("Input Actions")]
    [SerializeField]
    private InputActionAsset inputActions;

    [SerializeField]
    private InputAction jump_action = null;

    [SerializeField]
    private InputAction pointer_position_action = null;

    [Header("Jump Settings")]
    [SerializeField]
    private float jumpPressedTimeThreshold = 0.05f;

    float doubleJumpTime = 0f;
    [SerializeField]
    float doubleJumpActivationTime = 0.2f;

    [Header("Action Events")]
    // Events that other scripts can subscribe to
    public UnityEvent OnJumpPressed = new UnityEvent();
    public UnityEvent OnJumpReleased = new UnityEvent();
    public UnityEvent OnJumpHeldStarted = new UnityEvent();
    public UnityEvent OnJumpHeldUpdate = new UnityEvent();
    public UnityEvent OnDoubleJumpPressedActivated = new UnityEvent();
    
    private float jumpPressedTime = 0f;
    public float JumpPressedTime => jumpPressedTime;

    private bool isJumping = false;
    public bool IsJumping => isJumping;

    public bool IsHoldingJump => isJumping && jumpPressedTime >= jumpPressedTimeThreshold;

    private Coroutine jumpHeldCoroutine;


    public Vector2 PointerPosition { get; private set; } = Vector2.zero;

    #endregion // Properties

    #region Unity Actions

    private void Awake()
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

    private void Update()
    {
        if(doubleJumpTime > 0)
        {
            doubleJumpTime -= Time.deltaTime;
        }
    }

    private void OnEnable()
    {
        InputActionsActivate();
    }

    private void OnDisable()
    {
        InputActionsDeactivate();
    }

    private void OnDestroy()
    {
        this.StopAllCoroutines();
    }

    #endregion // Unity Actions

    #region Functions

    // Add null check helper
    public static bool Exists()
    {
        return Instance != null;
    }

    public Ray GetRayFromPointer(Camera camera = null)
    {
        if(camera == null)
        {
            camera = Camera.main;
        }
        return camera.ScreenPointToRay(PointerPosition);
    }


    #endregion // Functions

    #region Input Actions Logic

    private void HandlePointerPositionAction(InputAction.CallbackContext context)
    {
        switch (context.phase)
        {
            case InputActionPhase.Performed:
                PointerPosition = context.ReadValue<Vector2>();
                break;
            default: break;
        }
    }

    private void HandleJumpAction(InputAction.CallbackContext context)
    {
        bool doubleJumpActivated = false;
        switch (context.phase)
        {
            case InputActionPhase.Started:
                break;
            case InputActionPhase.Performed:

                jumpPressedTime = 0f;
                isJumping = true;
                if(jumpHeldCoroutine != null)
                {
                    this.StopCoroutine(jumpHeldCoroutine);
                    jumpHeldCoroutine = null;
                }
                jumpHeldCoroutine = this.StartCoroutine(JumpPressedCoroutine());
                if (!doubleJumpActivated && doubleJumpTime > 0)
                {
                    OnDoubleJumpPressedActivated.Invoke();
                    doubleJumpTime = 0;
                    doubleJumpActivated = true;
                }
                else
                {
                    OnJumpPressed.Invoke();
                }
                break;
            case InputActionPhase.Canceled:
                isJumping = false;
                this.StopCoroutine(jumpHeldCoroutine);
                OnJumpReleased.Invoke();
                doubleJumpTime = doubleJumpActivationTime;
                break;
            default:break;
        }
    }
   
    private void InputActionsActivate()
    {
        if(!inputActions.enabled)
        {
            inputActions.Enable();
        }

        if (jump_action.bindings.Count == 0)
        {
            jump_action = inputActions.FindAction("Jump", throwIfNotFound: true);
        }
        jump_action.performed += HandleJumpAction;
        jump_action.canceled += HandleJumpAction;
        jump_action.started += HandleJumpAction;
        jump_action.Enable();


        if (pointer_position_action.bindings.Count == 0)
        {
            pointer_position_action = inputActions.FindAction("Point", throwIfNotFound: true);
        }
        pointer_position_action.performed += HandlePointerPositionAction;
        pointer_position_action.canceled += HandlePointerPositionAction;
        pointer_position_action.started += HandlePointerPositionAction;
        pointer_position_action.Enable();


    }

    private void InputActionsDeactivate()
    {
        jump_action.Disable();
        jump_action.performed -= HandleJumpAction;
        jump_action.canceled -= HandleJumpAction;
        jump_action.started -= HandleJumpAction;
    }

    #endregion // Input Actions

    #region Coroutines
    private IEnumerator JumpPressedCoroutine()
    {
        bool startedHoldingJump = false;

        while (isJumping)
        {
            if (!startedHoldingJump && IsHoldingJump)
            {
                startedHoldingJump = true;
                OnJumpHeldStarted.Invoke();
            }

            if (IsHoldingJump)
            {
                OnJumpHeldUpdate.Invoke();
            }

            jumpPressedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
    }

    #endregion // Couroutines
}