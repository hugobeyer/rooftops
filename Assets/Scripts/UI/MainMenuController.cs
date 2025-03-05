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
    
    [Header("Achievement Display")]
    public TMP_Text currentGoalText; // Add this in the Inspector
    
    void Start()
    {
        UpdateDisplays();
    }

    void UpdateDisplays()
    {
        // Display basic game stats
        if (GameManager.Instance != null && GameManager.Instance.gameData != null)
        {
            bestScoreText.text = $"{GameManager.Instance.gameData.bestDistance:F1} m";
            lastRunText.text = $"{GameManager.Instance.gameData.lastRunDistance:F1} m";
            memcardText.text = GameManager.Instance.gameData.totalMemcardsCollected.ToString();
        }
        
        // Display current achievement goal if available
        if (currentGoalText != null && GoalAchievementManager.Instance != null)
        {
            // Get the current distance goal as an example
            var distanceCategory = GoalAchievementManager.Instance.GetGoalCategory("Distance");
            if (distanceCategory != null && distanceCategory.currentGoalValue != null)
            {
                currentGoalText.text = $"Current Goal: {distanceCategory.currentGoalValue} m";
            }
            else
            {
                currentGoalText.text = "No active goal";
            }
        }
    }
} 