using UnityEngine;
using TMPro;
using RoofTops;  // Add this for GameAdsManager

public class GameOverUIController : MonoBehaviour
{
    [Header("Game Over Screen")]
    public GameObject gameOverPanel;
    public TMP_Text finalScoreText;
    public TMP_Text newBestText;
    public TMP_Text finalTridotText;
    public TMP_Text finalMemcardText;

    [Header("Death Buttons")]
    public GameObject rooftopButton;
    public GameObject smartAdvanceButton;
    public GameObject tridotSkipButton;
    public GameObject continueButton;
    public TMP_Text tridotSkipText;
    
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
        if (tridotSkipText != null && GameAdsManager.Instance != null)
        {
            tridotSkipText.text = $"tridots SKIP ({GameAdsManager.Instance.AdSkipsAvailable})";
            tridotSkipButton.SetActive(GameAdsManager.Instance.AdSkipsAvailable > 0);
        }

        // Other buttons are always enabled for now
        rooftopButton.SetActive(true);
        smartAdvanceButton.SetActive(true);
        continueButton.SetActive(false); // Disabled until we implement continue feature
    }

    // Button click handlers
    public void OnRooftopClick()
    {
        Debug.Log("GameOverUIController: OnRooftopClick - Showing ad then restarting");
        
        // Disable buttons to prevent multiple clicks
        DisableAllButtons();
        
        // Show ad before restarting
        if (GameAdsManager.Instance != null)
        {
            GameAdsManager.Instance.OnPlayerRestart(() => {
                GameManager.Instance.RestartGame();
            });
        }
        else
        {
            // Fallback if ad manager is not available
            GameManager.Instance.RestartGame();
        }
    }

    public void OnSmartAdvanceClick()
    {
        Debug.Log("GameOverUIController: OnSmartAdvanceClick - Restarting without ad");
        
        // Disable buttons to prevent multiple clicks
        DisableAllButtons();
        
        // Restart directly without showing an ad
        GameManager.Instance.RestartGame();
    }

    public void OnTridotSkipClick()
    {
        Debug.Log("GameOverUIController: OnTridotSkipClick - Using skip token and restarting");
        if (GameAdsManager.Instance != null && GameAdsManager.Instance.AdSkipsAvailable > 0)
        {
            // Use skip token and restart
            // Disable buttons to prevent multiple clicks
            DisableAllButtons();
            
            GameManager.Instance.RestartGame();
        }
    }

    public void OnContinueClick()
    {
        Debug.Log("GameOverUIController: OnContinueClick - Continuing from death point");
        // Future feature - continue from death point
        // Disable buttons to prevent multiple clicks
        DisableAllButtons();
        
        GameManager.Instance.RestartGame();
    }
    
    private void DisableAllButtons()
    {
        // Disable all buttons to prevent multiple clicks
        if (rooftopButton != null) rooftopButton.GetComponent<UnityEngine.UI.Button>().interactable = false;
        if (smartAdvanceButton != null) smartAdvanceButton.GetComponent<UnityEngine.UI.Button>().interactable = false;
        if (tridotSkipButton != null) tridotSkipButton.GetComponent<UnityEngine.UI.Button>().interactable = false;
        if (continueButton != null) continueButton.GetComponent<UnityEngine.UI.Button>().interactable = false;
    }
} 