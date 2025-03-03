using UnityEngine;
using RoofTops;

public class ConstantSpeedMover : MonoBehaviour
{
    public bool hasCollided = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (!hasCollided) // Only react to the *first* collision
        {
            hasCollided = true;
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                Destroy(rb); // Remove Rigidbody on first collision
            }
        }
    }

    void Update()
    {
        if (hasCollided)
        {
            // Move at constant speed along -Z, using ModulePool's speed.
            transform.position += Vector3.back * ModulePool.Instance.currentMoveSpeed * Time.deltaTime;
        }
    }

    public void StopMovement()
    {
        hasCollided = true; // Prevent further movement in Update
    }
} 