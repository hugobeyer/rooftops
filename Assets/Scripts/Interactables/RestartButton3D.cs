using UnityEngine;
using UnityEngine.SceneManagement;
using RoofTops;

public class RestartButton3D : MonoBehaviour
{
    void Awake()
    {
        // Register a restart action with InputManager if it exists
        if (InputManager.Exists())
        {
            InputManager.Instance.RegisterAction("RestartGame", KeyCode.R);
            InputManager.Instance.SubscribeToPressed("RestartGame", RestartGame);
        }
    }

    void Update()
    {
        // Only handle direct touch input if InputManager doesn't exist
        if (!InputManager.Exists())
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