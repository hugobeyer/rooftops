using UnityEngine;
using TMPro;
using RoofTops;

public class MainMenuController : MonoBehaviour
{
    [Header("Menu Elements")]
    public GameObject mainMenuPanel;
    public TMP_Text bestScoreText;
    public TMP_Text lastRunText;
    public TMP_Text memcardText;
    // Add other main menu elements
    
    void Start()
    {
        UpdateDisplays();
    }

    void UpdateDisplays()
    {
        if (GameManager.Instance != null && GameManager.Instance.gameData != null)
        {
            bestScoreText.text = $"{GameManager.Instance.gameData.bestDistance:F1} m";
            lastRunText.text = $"{GameManager.Instance.gameData.lastRunDistance:F1} m";
            memcardText.text = GameManager.Instance.gameData.totalMemcardsCollected.ToString();
        }
    }
} 