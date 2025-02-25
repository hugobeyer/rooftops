using UnityEngine;
using TMPro;
using RoofTops;  // Add this for GameAdsManager

public class GameOverUIController : MonoBehaviour
{
    [Header("Game Over Screen")]
    public GameObject gameOverPanel;
    public TMP_Text finalScoreText;
    public TMP_Text newBestText;
    public TMP_Text finalBonusText;
    public TMP_Text finalMemcardText;

    [Header("Death Buttons")]
    public GameObject rooftopButton;
    public GameObject smartAdvanceButton;
    public GameObject bonusSkipButton;
    public GameObject continueButton;
    public TMP_Text bonusSkipText;
    
    public void ShowGameOver(float finalDistance, bool isNewBest)
    {
        gameOverPanel.SetActive(true);
        finalScoreText.text = $"{finalDistance:F1} m";
        newBestText.gameObject.SetActive(isNewBest);
        finalBonusText.text = $"Bonus: {GameManager.Instance.gameData.lastRunBonusCollected}";
        
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
        if (bonusSkipText != null && GameAdsManager.Instance != null)
        {
            bonusSkipText.text = $"BONUS SKIP ({GameAdsManager.Instance.AdSkipsAvailable})";
            bonusSkipButton.SetActive(GameAdsManager.Instance.AdSkipsAvailable > 0);
        }

        // Other buttons are always enabled for now
        rooftopButton.SetActive(true);
        smartAdvanceButton.SetActive(true);
        continueButton.SetActive(false); // Disabled until we implement continue feature
    }

    // Button click handlers
    public void OnRooftopClick()
    {
        // Just restart without ad
        GameManager.Instance.ResetGame();
    }

    public void OnSmartAdvanceClick()
    {
        if (GameAdsManager.Instance != null)
        {
            GameAdsManager.Instance.OnPlayerDeath(() => {
                GameManager.Instance.ResetGame();
            });
        }
    }

    public void OnBonusSkipClick()
    {
        if (GameAdsManager.Instance != null && GameAdsManager.Instance.AdSkipsAvailable > 0)
        {
            // Use skip token and restart
            GameManager.Instance.ResetGame();
        }
    }

    public void OnContinueClick()
    {
        // Future feature - continue from death point
        GameManager.Instance.ResetGame();
    }
} 