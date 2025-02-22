using UnityEngine;
using UnityEngine.SceneManagement;
using RoofTops;

public class RestartButtonHandler : MonoBehaviour
{
    public void RestartGame()
    {
        // Reset time scale in case game was paused
        Time.timeScale = 1f;
        
        // Reload current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        
        // Optional: Add any additional reset logic through GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetGame();
        }
    }
} 