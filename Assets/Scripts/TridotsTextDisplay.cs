using UnityEngine;
using TMPro;
using RoofTops;

public class TridotsTextDisplay : MonoBehaviour
{
    public static TridotsTextDisplay Instance { get; private set; }
    
    [Header("UI Settings")]
    public TMP_Text tridotsText;
    
    private int allTimeTotal = 0;

    void Start()
    {
        Instance = this;
        
        // Auto-find text component if not assigned
        if (tridotsText == null)
        {
            tridotsText = GetComponent<TMP_Text>();
        }

        // Load the all-time total from GameData
        if (GameManager.Instance != null && GameManager.Instance.gameData != null)
        {
            allTimeTotal = GameManager.Instance.gameData.totalTridotCollected;
        }
        
        UpdateDisplay();
    }

    public void AddTridots(int amount)
    {
        // (Always update tridots; the UI parent handles hiding until game start.)
        allTimeTotal += amount;
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        if (tridotsText != null)
        {
            tridotsText.text = $"{allTimeTotal}";
        }
    }

    public void ResetTotal()
    {
        UpdateDisplay();
    }
} 