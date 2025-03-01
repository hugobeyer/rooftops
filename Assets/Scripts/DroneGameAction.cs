using UnityEngine;
using System.Collections;

public enum DroneActionType {
    Vanish,
    Die,
    Trap
}

public class DroneGameAction : MonoBehaviour
{
    [Header("Spawn Interval Settings")]
    [Tooltip("Minimum time interval between spawns (in seconds)")]
    public float minInterval = 5f;
    
    [Tooltip("Maximum time interval between spawns (in seconds)")]
    public float maxInterval = 10f;

    [Header("Action Rules")]
    [Tooltip("Select the rule for the game action: Vanish, Die, or Trap")]
    public DroneActionType actionType = DroneActionType.Vanish;

    [Tooltip("Minimum lifetime for vanish rule (in seconds)")]
    public float vanishMinLifetime = 3f;

    [Tooltip("Maximum lifetime for vanish rule (in seconds)")]
    public float vanishMaxLifetime = 6f;

    [Header("Prefab and Spawn Settings")]
    [Tooltip("The prefab to instantiate as a game action")]
    public GameObject actionPrefab;
    
    [Tooltip("Optional parent transform for the instantiated prefab")]
    public Transform spawnParent;
    
    [Tooltip("Base spawn position (world space) or relative to this object")]
    public Vector3 spawnPosition = Vector3.zero;
    
    [Tooltip("Optional random offset range applied to the base spawn position")]
    public Vector3 spawnPositionOffset = new Vector3(1f, 1f, 1f);
    
    [Tooltip("Toggle random offset on spawn position")]
    public bool useRandomOffset = true;

    void Start()
    {
        StartCoroutine(SpawnActionCoroutine());
    }
    
    IEnumerator SpawnActionCoroutine()
    {
        while (true)
        {
            // Wait for a random time between min and max intervals
            float waitTime = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(waitTime);
            
            // Determine the final spawn position
            Vector3 finalSpawnPos = spawnPosition;
            if (useRandomOffset)
            {
                finalSpawnPos += new Vector3(
                    Random.Range(-spawnPositionOffset.x, spawnPositionOffset.x),
                    Random.Range(-spawnPositionOffset.y, spawnPositionOffset.y),
                    Random.Range(-spawnPositionOffset.z, spawnPositionOffset.z)
                );
            }
            
            // Instantiate the action prefab if assigned and apply rule-specific behavior
            if (actionPrefab != null)
            {
                GameObject instance = Instantiate(actionPrefab, finalSpawnPos, Quaternion.identity, spawnParent);
                switch (actionType)
                {
                    case DroneActionType.Vanish:
                        // Instead of destroying immediately, trigger exit animation
                        DroneMovement droneMovement = instance.GetComponent<DroneMovement>();
                        if (droneMovement != null) {
                            float lifetime = Random.Range(vanishMinLifetime, vanishMaxLifetime);
                            droneMovement.TriggerExitAfterDelay(lifetime);
                        } else {
                            // Fallback if no DroneMovement component
                            float lifetime = Random.Range(vanishMinLifetime, vanishMaxLifetime);
                            Destroy(instance, lifetime);
                        }
                        break;
                    case DroneActionType.Die:
                        // TODO: Implement the drone dying by hitting a wall.
                        break;
                    case DroneActionType.Trap:
                        // TODO: Implement trap spawning logic.
                        break;
                }
            }
            else
            {
                Debug.LogWarning("Action Prefab is not assigned in DroneGameAction.");
            }
        }
    }
} 