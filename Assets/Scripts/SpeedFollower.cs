using UnityEngine;
using RoofTops;

/// <summary>
/// Makes any spawned item move at the same forward speed as the game manager.
/// </summary>
public class SpeedFollower : MonoBehaviour
{
    private float despawnZ;

    // Called right after instantiating, so you know where to destroy the item
    public void InitializeDespawnPlane(float planeZ)
    {
        despawnZ = planeZ;
    }

    void Update()
    {
        // Example: Move the item backwards at the same speed the player moves forward
        // Adjust direction or logic as needed to match your game's coordinate system.
        if (GameManager.Instance != null)
        {
            // If your player moves in the +z direction, you might want the item to move -z
            float speed = GameManager.Instance.CurrentSpeed;
            transform.position -= Vector3.forward * speed * Time.deltaTime;
        }

        // 2) Destroy once it crosses the despawn plane
        if (transform.position.z < despawnZ)
        {
            Destroy(gameObject);
        }
    }
} 