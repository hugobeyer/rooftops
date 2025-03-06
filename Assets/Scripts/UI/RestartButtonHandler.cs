using UnityEngine;
using UnityEngine.SceneManagement;
using RoofTops;

public class RestartButtonHandler : MonoBehaviour
{
    public void RestartGame()
    {
        // Reset time scale in case game was paused
        Time.timeScale = 1f;
        
        // Let GameManager handle the reset
        if (GameManager.Instance != null)
        {
            Debug.Log("RestartButtonHandler: Calling GameManager.RestartGame()");
            GameManager.Instance.RestartGame();
        }
        else
        {
            // Fallback if GameManager is not available
            Debug.LogWarning("RestartButtonHandler: GameManager not found, reloading scene directly");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
} 