using UnityEngine;

/// <summary>
/// Allows a spawned object to follow a given target's movement,
/// preserving the initial offset.
/// </summary>
public class TagAlongMovement : MonoBehaviour
{
    private Transform target;
    private Vector3 localOffset;
    public float speed;

    /// <summary>
    /// Call this right after instantiating to set the target spawn point.
    /// </summary>
    public void Initialize(Transform followTarget)
    {
        target = followTarget;
        // Record offset in the target's local space:
        localOffset = target.InverseTransformPoint(transform.position);
    }

    private void Update()
    {
        if (target != null)
        {
            // Keep the same local offset, so it "tags along"
            transform.position = target.TransformPoint(localOffset);

            // OPTIONAL: If you want to match rotation, you could also do:
            // transform.rotation = target.rotation;

            // Move the object in the -Z direction each frame
            transform.position -= Vector3.forward * speed * Time.deltaTime;
        }
    }

    private void SpawnItemAtPoint(Transform spawnPoint)
    {
        if (spawnPoint == null)
        {
            Debug.LogWarning("SpawnItemAtPoint called with a null spawnPoint!");
            return;
        }

        float roll = Random.value;
        
    }
} 