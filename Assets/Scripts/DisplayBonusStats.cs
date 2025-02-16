using UnityEngine;
using TMPro;

public class DisplayBonusStats : MonoBehaviour
{
    public TMP_Text bonusText;
    private static int currentRunBonuses = 0;  // Just for this run
    
    void Start()
    {
        if (bonusText == null)
        {
            bonusText = GetComponent<TMP_Text>();
        }
        currentRunBonuses = 0;  // Reset at start
    }

    void Update()
    {
        // Just show current run's bonus count
        bonusText.text = $"{currentRunBonuses}";
    }

    public static void AddBonus()
    {
        currentRunBonuses++;
    }
} 