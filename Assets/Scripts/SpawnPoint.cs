using UnityEngine;

/// <summary>
/// Defines possible spawn priorities.
/// </summary>
public enum SpawnPriority
{
    Zero = 0,
    One = 1,
    Two = 2,
    Three = 3
}

/// <summary>
/// Attach this script to any GameObject that will act as a spawn point.
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    [Header("Spawn Priority (0=Lowest, 3=Highest)")]
    public SpawnPriority priority = SpawnPriority.Zero;

    // This variable tells if something is in this spawn point's collider.
    public bool isOccupied = false;

    // You can add other properties or references here,
    // for instance an object type to spawn, or potential item/breakable block info.

    // Example: public GameObject objectToSpawn;

    void OnDrawGizmos()
    {
        // This draws a small sphere in the Scene view for visualization
        Gizmos.color = Color.gray;
        Gizmos.DrawSphere(transform.position, 0.325f);
    }

    // 1) If a relevant object enters, set isOccupied to true.
    private void OnTriggerEnter(Collider other)
    {
        // Optionally filter only certain objects, e.g. "tridots", "JumpPad", "Prop"
        // if (other.CompareTag("tridots") || other.CompareTag("JumpPad") || other.CompareTag("Prop"))
        // {
        //     isOccupied = true;
        // }

        // For a simpler approach, treat *any* object as occupying.
        isOccupied = true;
    }

    // 2) Once the relevant object leaves, set isOccupied back to false.
    private void OnTriggerExit(Collider other)
    {
        // Same optional filtering logic if needed:
        // if (other.CompareTag("tridots") || other.CompareTag("JumpPad") || other.CompareTag("Prop"))
        // {
        //     isOccupied = false;
        // }

        isOccupied = false;
    }
} 