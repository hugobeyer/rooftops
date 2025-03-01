using UnityEngine;

public class DistanceDisplay : MonoBehaviour
{
    // Assign a TextMesh component here in the Inspector.
    public TextMesh distanceText;

    // Assign the player's Transform (the one moving forward).
    public Transform player;

    // Store the starting z-position.
    private float startZ;

    void Start()
    {
        if (player != null)
        {
            startZ = player.position.z;
        }
        else
        {
            Debug.LogWarning("DistanceDisplay: Player Transform not assigned.");
        }
    }

    void Update()
    {
        if (player == null || distanceText == null)
            return;

        // Calculate the distance traveled along the Z axis.
        float traveledDistance = Mathf.Abs(player.position.z - startZ);

        // Format the distance and update the TextMesh.
        distanceText.text = "Distance: " + traveledDistance.ToString("F2") + " m";
    }
}