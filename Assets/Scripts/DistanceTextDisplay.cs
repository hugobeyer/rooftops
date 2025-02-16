using UnityEngine;
using TMPro;

public class DistanceTextDisplay : MonoBehaviour
{
    public TMP_Text distanceText;
    private static DistanceTextDisplay instance;

    // Add a field to store the current distance.
    private float currentDistance = 0f;

    void Awake()
    {
        instance = this;
        if (distanceText == null)
        {
            distanceText = GetComponent<TMP_Text>();
        }
    }

    // This is called whenever you want to update the displayed distance.
    public static void UpdateDistance(float distance)
    {
        if (instance != null && instance.distanceText != null)
        {
            // Update the on-screen text.
            instance.distanceText.text = distance.ToString("F1") + " m";

            // Also store the distance internally.
            instance.currentDistance = distance;
        }
        else
        {
            Debug.LogWarning("DistanceTextDisplay not set up!");
        }
    }

    // Provide a way for other scripts to retrieve the last stored distance.
    public static float GetDistance()
    {
        if (instance != null)
        {
            return instance.currentDistance;
        }
        return 0f;
    }
} 