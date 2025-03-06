using UnityEngine;
using TMPro;
using RoofTops;  // Add this to access GameManager and GameStates

namespace RoofTops.UI
{
    public class GameOverUIController : MonoBehaviour
    {
        [Header("Game Over Screen")]
        public GameObject gameOverPanel;
        public TMP_Text finalScoreText;
        public TMP_Text newBestText;
        public TMP_Text finalTridotText;
        public TMP_Text finalMemcardText;

        [Header("Death Buttons")]
        public GameObject rooftopButton;      // Restart button
        public GameObject tridotSkipButton;   // Skip button using tridots
        public TMP_Text tridotSkipText;
        
        private void Start()
        {
            // Check if restart button is properly set up
            if (rooftopButton != null)
            {
                UnityEngine.UI.Button button = rooftopButton.GetComponent<UnityEngine.UI.Button>();
                if (button != null)
                {
                    Debug.Log("GameOverUIController: Restart button found and has Button component");
                    
                    // Add a listener programmatically as a backup
                    button.onClick.AddListener(OnRestartClick);
                    Debug.Log("GameOverUIController: Added backup click listener to Restart button");
                }
                else
                {
                    Debug.LogError("GameOverUIController: Restart button does not have a Button component!");
                }
            }
            else
            {
                Debug.LogError("GameOverUIController: Restart button reference is NULL!");
            }
        }

        public void ShowGameOver(float finalDistance, bool isNewBest)
        {
            GameManager.RequestGameStateChange(GameStates.GameOver);
            gameOverPanel.SetActive(true);
            finalScoreText.text = $"{finalDistance:F1} m";
            newBestText.gameObject.SetActive(isNewBest);
            finalTridotText.text = $"tridots: {GameManager.Instance.gameData.lastRunTridotCollected}";
            
            // Display memcard count if the text component exists
            if (finalMemcardText != null)
            {
                finalMemcardText.text = $"Memcards: {GameManager.Instance.gameData.lastRunMemcardsCollected}";
            }

            // Update button states
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            // Update skip token count if available
            if (tridotSkipText != null)
            {
                tridotSkipText.text = "tridots SKIP (0)";
                tridotSkipButton.SetActive(false); // Disabled for now
            }

            // Restart button is always enabled
            rooftopButton.SetActive(true);
        }

        // Button click handlers
        public void OnRestartClick()
        {
            Debug.Log("GameOverUIController: OnRestartClick - Restarting game");
            
            // Disable buttons to prevent multiple clicks
            DisableAllButtons();
            
            // Restart the game
            GameManager.Instance.RestartGame();
        }

        public void OnSkipClick()
        {
            Debug.Log("GameOverUIController: OnSkipClick - Using skip token and restarting");
            
            // Disable buttons to prevent multiple clicks
            DisableAllButtons();
            
            // Add direct call to restart
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
            }
        }
        
        private void DisableAllButtons()
        {
            // Disable all buttons to prevent multiple clicks
            if (rooftopButton != null) rooftopButton.GetComponent<UnityEngine.UI.Button>().interactable = false;
            if (tridotSkipButton != null) tridotSkipButton.GetComponent<UnityEngine.UI.Button>().interactable = false;
        }
    }
}