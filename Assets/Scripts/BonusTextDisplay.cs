using UnityEngine;
using TMPro;
using RoofTops;

public class BonusTextDisplay : MonoBehaviour
{
    public static BonusTextDisplay Instance { get; private set; }
    
    [Header("UI Settings")]
    public TMP_Text bonusText;
    
    private int allTimeTotal = 0;

    void Start()
    {
        Instance = this;
        
        // Auto-find text component if not assigned
        if (bonusText == null)
        {
            bonusText = GetComponent<TMP_Text>();
        }

        // Load the all-time total from GameData
        if (GameManager.Instance != null && GameManager.Instance.gameData != null)
        {
            allTimeTotal = GameManager.Instance.gameData.totalBonusCollected;
        }
        
        UpdateDisplay();
    }

    public void AddBonus(int amount)
    {
        // (Always update bonus; the UI parent handles hiding until game start.)
        allTimeTotal += amount;
        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        if (bonusText != null)
        {
            bonusText.text = $"{allTimeTotal}";
        }
    }

    public void ResetTotal()
    {
        UpdateDisplay();
    }
} 