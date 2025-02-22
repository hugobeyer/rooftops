using UnityEngine;
using UnityEngine.SceneManagement;
using RoofTops;

public class RestartButton3D : MonoBehaviour
{
    void Update()
    {
        HandleTouchInput();
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
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetGame();
        }
    }
} 