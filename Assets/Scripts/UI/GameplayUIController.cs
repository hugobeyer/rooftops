using UnityEngine;
using TMPro;
using RoofTops;

public class GameplayUIController : MonoBehaviour
{
    [Header("In-Game UI")]
    public TMP_Text currentDistanceText;
    public TMP_Text currentTridotText;
    public TMP_Text currentMemcardText;
    public TMP_Text speedRateText;
    
    void Update()
    {
        // Use EconomyManager as primary source if available
        if (EconomyManager.Instance != null)
        {
            // Get values directly from EconomyManager
            float distance = EconomyManager.Instance.GetCurrentDistance();
            int tridots = EconomyManager.Instance.GetCurrentTridots();
            int memcards = EconomyManager.Instance.GetCurrentMemcards();
            
            // Update UI
            if (currentDistanceText != null)
            {
                currentDistanceText.text = $"{distance:F1} m";
            }
            
            if (currentTridotText != null)
            {
                currentTridotText.text = tridots.ToString();
            }
            
            if (currentMemcardText != null)
            {
                currentMemcardText.text = memcards.ToString();
            }
        }
        // Fallback to GameManager if EconomyManager is not available
        else if (GameManager.Instance != null && GameManager.Instance.gameData != null)
        {
            if (currentDistanceText != null)
            {
                currentDistanceText.text = $"{GameManager.Instance.CurrentDistance:F1} m";
            }
            
            if (currentTridotText != null)
            {
                currentTridotText.text = GameManager.Instance.gameData.lastRunTridotCollected.ToString();
            }
            
            if (currentMemcardText != null)
            {
                currentMemcardText.text = GameManager.Instance.gameData.lastRunMemcardsCollected.ToString();
            }
        }
        else
        {
            if (currentDistanceText != null)
            {
                currentDistanceText.text = "0.0 m";
            }
            
            if (currentTridotText != null)
            {
                currentTridotText.text = "0";
            }
            
            if (currentMemcardText != null)
            {
                currentMemcardText.text = "0";
            }
            
            Debug.LogWarning("[GameplayUIController] No data source found!");
        }
        
        // Add null check for ModulePool.Instance and speedRateText
        if (ModulePool.Instance != null && speedRateText != null)
        {
            float currentSpeed = ModulePool.Instance.gameSpeed;
            speedRateText.text = $"Speed: {currentSpeed:F1}x";
        }
        else if (speedRateText != null && GameManager.Instance != null)
        {
            // Fallback to GameManager's initial speed if ModulePool is not available
            float fallbackSpeed = GameManager.Instance.initialGameSpeed;
            speedRateText.text = $"Speed: {fallbackSpeed:F1}x";
        }
    }
} 
