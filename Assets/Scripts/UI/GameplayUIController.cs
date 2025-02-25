using UnityEngine;
using TMPro;
using RoofTops;

public class GameplayUIController : MonoBehaviour
{
    [Header("In-Game UI")]
    public TMP_Text currentDistanceText;
    public TMP_Text currentBonusText;
    public TMP_Text currentMemcardText;
    public TMP_Text speedRateText;
    
    void Update()
    {
        if (GameManager.Instance != null)
        {
            currentDistanceText.text = $"{GameManager.Instance.CurrentDistance:F1} m";
            currentBonusText.text = GameManager.Instance.gameData.lastRunBonusCollected.ToString();
            
            // Display memcard count if the text component exists
            if (currentMemcardText != null)
            {
                currentMemcardText.text = GameManager.Instance.gameData.lastRunMemcardsCollected.ToString();
            }
            
            float currentSpeed = ModulePool.Instance.gameSpeed;
            speedRateText.text = $"Speed: {currentSpeed:F1}x";
        }
    }
} 