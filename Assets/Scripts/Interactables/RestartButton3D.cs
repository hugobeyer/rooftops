using UnityEngine;
using UnityEngine.SceneManagement;
using RoofTops;
using UnityEngine.InputSystem;

public class RestartButton3D : MonoBehaviour
{
    #region Properties
    [SerializeField]
    private InputActionReference restartInputActionReference;
    private InputAction restartInputAction => restartInputActionReference?.action;
    #endregion // Properties

    #region Unity Methods

    private void Start()
    {
        if(restartInputActionReference != null)
        {
            restartInputAction.Enable();
            restartInputAction.performed += ctx => RestartGame();
        }
        
        GameManager.OnGameStateChanged += HandleGameStateChanged;
        if(GameManager.GamesState == GameStates.GameOver)
        {
            InputActionManager.Instance.OnJumpPressed.AddListener(CheckInteraction);
        }
    }

    private void OnDestroy()
    {
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
        restartInputAction.Disable();
        restartInputAction.performed -= ctx => RestartGame();
    }

    #endregion // Unity Methods

    #region Functions

    private void CheckInteraction()
    {
        Ray ray = InputActionManager.Instance.GetRayFromPointer();
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject == gameObject)
            {
                RestartGame();
            }
        }
    }

    private void HandleGameStateChanged(GameStates oldState, GameStates newState)
    {
        if (newState == GameStates.GameOver)
        {
           InputActionManager.Instance.OnJumpPressed.AddListener(CheckInteraction);
        }
        else
        {
            InputActionManager.Instance.OnJumpPressed.RemoveListener(CheckInteraction);
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        GameManager.Instance.ResetGame();
    }
    #endregion // Functions
}