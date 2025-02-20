using UnityEngine;
using TMPro;
using RoofTops;

public class GameplayUIController : MonoBehaviour
{
    [Header("In-Game UI")]
    public TMP_Text currentDistanceText;
    public TMP_Text currentBonusText;
    public TMP_Text speedRateText;
    
    void Update()
    {
        if (GameManager.Instance != null)
        {
            currentDistanceText.text = $"{GameManager.Instance.CurrentDistance:F1} m";
            currentBonusText.text = GameManager.Instance.gameData.lastRunBonusCollected.ToString();
            
            float currentSpeed = ModulePool.Instance.gameSpeed;
            speedRateText.text = $"Speed: {currentSpeed:F1}x";
        }
    }
} 