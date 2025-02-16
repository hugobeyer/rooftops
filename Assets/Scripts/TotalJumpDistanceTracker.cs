using TMPro;
using UnityEngine;

public class TotalJumpDistanceTracker : MonoBehaviour
{
    [SerializeField] private TMP_Text totalDistanceText;
    private float totalDistance;

    void Start()
    {
        UpdateDisplay();
    }

    public void AddJumpDistance(float distance)
    {
        totalDistance += Mathf.Abs(distance);
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (totalDistanceText != null)
            totalDistanceText.text = $"Total Jumps: {totalDistance:F1}m";
    }
} 