using UnityEngine;
using System.Collections;

public class DroneCollisionHandler : MonoBehaviour
{
    [Tooltip("Reference to the DroneMovement component")]
    private DroneMovement droneMovement;
    
    [Tooltip("Whether to destroy the drone on collision with player")]
    public bool destroyOnPlayerCollision = true;
    
    [Tooltip("Optional effect to spawn when drone is destroyed by player collision")]
    public GameObject collisionEffectPrefab;
    
    [Tooltip("Tag of the player object")]
    public string playerTag = "Player";
    
    [Tooltip("Time to wait before destroying the drone after exit is triggered")]
    public float destroyDelayAfterExit = 3f;
    
    [Tooltip("Distance from origin at which to automatically destroy the drone")]
    public float autoDestroyDistance = 100f;
    
    private bool exitTriggered = false;
    
    private void Awake()
    {
        droneMovement = GetComponent<DroneMovement>();
        if (droneMovement == null)
        {
            droneMovement = GetComponentInParent<DroneMovement>();
            if (droneMovement == null)
            {
                Debug.LogError("DroneCollisionHandler: No DroneMovement component found on this object or its parents!");
            }
        }
    }
    
    private void Update()
    {
        // Check if drone has gone too far from the origin
        if (Vector3.Distance(transform.position, Vector3.zero) > autoDestroyDistance)
        {
            Debug.Log("Drone auto-destroyed due to distance from origin");
            Destroy(gameObject);
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Check if the collision is with the player
        if (other.CompareTag(playerTag))
        {
            Debug.Log("Drone collided with player!");
            
            // Spawn collision effect if specified
            if (collisionEffectPrefab != null)
            {
                Instantiate(collisionEffectPrefab, transform.position, Quaternion.identity);
            }
            
            // Destroy the drone if specified
            if (destroyOnPlayerCollision)
            {
                // If we have a DroneMovement component, trigger its exit behavior
                if (droneMovement != null && !exitTriggered)
                {
                    exitTriggered = true;
                    droneMovement.TriggerExit();
                    StartCoroutine(DestroyAfterDelay(destroyDelayAfterExit));
                }
                else
                {
                    // Otherwise just destroy the GameObject
                    Destroy(gameObject);
                }
            }
        }
    }
    
    // Method to trigger exit and destruction from external scripts
    public void TriggerExitAndDestroy()
    {
        if (droneMovement != null && !exitTriggered)
        {
            exitTriggered = true;
            droneMovement.TriggerExit();
            StartCoroutine(DestroyAfterDelay(destroyDelayAfterExit));
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log("Destroying drone after exit animation");
        Destroy(gameObject);
    }
} 