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

    // Events that other scripts can subscribe to
    public UnityEvent OnJumpPressed = new UnityEvent();
    public UnityEvent OnJumpReleased = new UnityEvent();
    public UnityEvent OnJumpHeldStarted = new UnityEvent();
    public UnityEvent OnJumpHeldUpdate = new UnityEvent();
    public UnityEvent OnDoubleJumpPressedActivated = new UnityEvent();

    [Header("Input Actions")]
    [SerializeField]
    private InputActionAsset inputActions;

    [Header("Jump Settings")]
    [SerializeField]
    private float jumpPressedTimeThreshold = 0.1f;

    //private float lastJumpActionStarted = 0.0f;
    //private float jumpActionStarted = 0.0f;
    //private bool resetDoubleJump = false;

    [SerializeField]
    private float doubleJumpTimeThreshold = 0.1f;

    private InputAction jump_action;

    private float jumpPressedTime = 0f;
    public float JumpPressedTime => jumpPressedTime;

    private bool isJumping = false;
    public bool IsJumping => isJumping;

    public bool IsHoldingJump => isJumping && jumpPressedTime >= jumpPressedTimeThreshold;

    private Coroutine jumpHeldCoroutine;
    //private Coroutine doubleJumpActivatedCoroutine;

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

    private void Start()
    {
       // doubleJumpActivatedCoroutine = this.StartCoroutine(DoubleJumpActivatedCoroutine());
    }

    private void Update()
    {
        if(multiTapTime > 0)
        {
            multiTapTime -= Time.deltaTime;
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

    #endregion // Functions

    #region Input Actions Logic

    float multiTapTime = 0f;
    [SerializeField]
    float multiTapTimeDistance = 0.1f;

    private void HandleJumpAction(InputAction.CallbackContext context)
    {
        switch (context.phase)
        {
            case InputActionPhase.Started:
             //   jumpActionStarted = Time.realtimeSinceStartup;
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
                if (multiTapTime > 0)
                {
                    OnDoubleJumpPressedActivated.Invoke();
                    multiTapTime = 0;
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
                multiTapTime = multiTapTimeDistance;
                //if(resetDoubleJump)
                //{
                //    resetDoubleJump = false;
                //    lastJumpActionStarted = Time.realtimeSinceStartup;
                //}
                break;
        }
    }
   
    private void InputActionsActivate()
    {
        if(!inputActions.enabled)
        {
            inputActions.Enable();
        }

        jump_action = inputActions.FindAction("Jump", throwIfNotFound: true);
        jump_action.performed += HandleJumpAction;
        jump_action.canceled += HandleJumpAction;
        jump_action.started += HandleJumpAction;
        jump_action.Enable();
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

    //private IEnumerator DoubleJumpActivatedCoroutine()
    //{
    //    for (; ; )
    //    {
    //        if(!resetDoubleJump && 
    //            (jumpActionStarted - lastJumpActionStarted) >= doubleJumpTimeThreshold)
    //        {
    //            OnDoubleJumpActivated.Invoke();
    //            resetDoubleJump = true;
    //        }
    //        yield return new WaitForEndOfFrame();
    //    }
    //}
    #endregion // Couroutines
}