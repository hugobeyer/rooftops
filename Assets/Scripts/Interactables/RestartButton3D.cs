using UnityEngine;
using UnityEngine.SceneManagement;
using RoofTops;
using UnityEngine.InputSystem;

public class RestartButton3D : MonoBehaviour
{
    #region Properties
    [SerializeField]
    private InputAction restartInputAction;

    #endregion // Properties

    void Start()
    {
        restartInputAction.Enable();
        restartInputAction.performed += ctx => RestartGame();
    }

    void Update()
    {
        // Only handle direct touch input if InputManager doesn't exist
        if (!InputActionManager.Exists())
        {
            HandleTouchInput();
        }
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                CheckTouch(touch.position);
            }
        }
    }

    private void CheckTouch(Vector2 touchPosition)
    {
        Ray ray = Camera.main.ScreenPointToRay(touchPosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject == gameObject)
            {
                RestartGame();
            }
        }
    }

    void OnMouseDown()
    {
        RestartGame();
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
} 