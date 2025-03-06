using UnityEngine;
namespace RoofTops
{
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
            // Draw a yellow sphere at the transform's position
            Gizmos.color = isOccupied ? Color.red : Color.black;
            //Gizmos.DrawCube(transform.position, new Vector3(0.5f, 0.5f, 0.5f));
            Gizmos.DrawWireCube(transform.position + new Vector3(0.0f, 0.5f, 0.0f), new Vector3(1.0f, 1.0f, 1.0f));
        }

        private void OnTriggerEnter(Collider other)
        {
            // When something enters the spawn point's trigger collider, mark it as occupied
            isOccupied = true;
        }

        private void OnTriggerExit(Collider other)
        {
            // When something exits the spawn point's trigger collider, mark it as unoccupied
            isOccupied = false;
        }
    }
}