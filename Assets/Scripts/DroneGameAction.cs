using UnityEngine;
using System.Collections;

public class DroneGameAction : MonoBehaviour
{
    [Tooltip("The prefabs to instantiate as drone actions (the drone prefabs)")]
    public GameObject[] actionPrefabs;
    
    [Tooltip("Optional parent transform for the instantiated prefab")]
    public Transform spawnParent;
    
    [Tooltip("Base spawn position (world space) or relative to this object")]
    public Vector3 spawnPosition = Vector3.zero;
    
    [Tooltip("Minimum time delay before spawning next drone")]
    public float spawnDelayMin = 3f;
    
    [Tooltip("Maximum time delay before spawning next drone")]
    public float spawnDelayMax = 8f;
    
    [Tooltip("Layer to set on the drone for collision detection")]
    public string droneLayer = "Drone";
    
    [Tooltip("Reference to the player GameObject")]
    public GameObject player;

    private GameObject activeDrone;
    private bool isDroneActive => activeDrone != null;
    private bool isWaitingForNextSpawn = false;
    
    private void CheckForExistingDrones()
    {
        DroneMovement[] existingDrones = FindObjectsOfType<DroneMovement>();
        if (existingDrones.Length > 0)
        {
            activeDrone = existingDrones[0].gameObject;
        }
    }

    void Start()
    {
        // Find player if not assigned
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("Player not found! Please assign the player reference in the inspector.");
            }
        }
        
        StartCoroutine(SpawnActionCoroutine());
    }
    
    IEnumerator SpawnActionCoroutine()
    {
        while (true)
        {
            // Only spawn a new drone if there isn't one active and we're not in a waiting period
            if (!isDroneActive && !isWaitingForNextSpawn && actionPrefabs != null && actionPrefabs.Length > 0)
            {
                // Double-check for any drones in the scene before spawning
                CheckForExistingDrones();
                if (isDroneActive)
                {
                    yield return new WaitForSeconds(1.0f);
                    continue;
                }
                
                Debug.Log("Spawning new drone");
                
                // Randomly choose one prefab from the available array.
                GameObject chosenPrefab = actionPrefabs[Random.Range(0, actionPrefabs.Length)];
                GameObject instance = Instantiate(chosenPrefab, spawnPosition, Quaternion.identity, spawnParent);
                
                // Set the drone to the specified layer
                SetLayerRecursively(instance, LayerMask.NameToLayer(droneLayer));
                
                // Make sure all colliders are triggers
                SetCollidersAsTriggers(instance);
                
                DroneMovement droneMovement = instance.GetComponent<DroneMovement>();
                if (droneMovement != null) 
                {
                    activeDrone = instance;
                    StartCoroutine(WaitForDroneDestruction(instance));
                }
            }

            // Wait a frame to prevent tight loop
            yield return null;
            
            // If no drone is active and we're not waiting, start the delay for the next spawn
            if (!isDroneActive && !isWaitingForNextSpawn)
            {
                // Double-check for any drones in the scene before starting delay
                CheckForExistingDrones();
                if (isDroneActive)
                {
                    continue;
                }
                
                isWaitingForNextSpawn = true;
                float randomDelay = Random.Range(spawnDelayMin, spawnDelayMax);
                Debug.Log($"Waiting {randomDelay} seconds before spawning next drone");
                yield return new WaitForSeconds(randomDelay);
                isWaitingForNextSpawn = false;
            }
        }
    }
    
    private IEnumerator WaitForDroneDestruction(GameObject drone)
    {
        // First, wait until the drone starts its exit animation
        DroneMovement droneMovement = drone.GetComponent<DroneMovement>();
        bool exitStarted = false;
        
        // Wait for the drone to start exiting or be destroyed
        while (drone != null && !exitStarted)
        {
            if (droneMovement != null && droneMovement.enableExitBehavior)
            {
                exitStarted = true;
                Debug.Log("Drone exit animation started, waiting for animation to complete");
                
                // Wait for the exit animation to complete plus a small buffer
                float exitTime = droneMovement.exitDuration + 0.5f;
                yield return new WaitForSeconds(exitTime);
                
                // Even if the drone is still not destroyed (which it should be by now),
                // we'll consider it "done" for spawning purposes
                break;
            }
            
            yield return null;
        }
        
        // If the drone was destroyed before exit animation started, just wait a moment
        if (!exitStarted && drone == null)
        {
            Debug.Log("Drone was destroyed before exit animation started");
            yield return new WaitForSeconds(1.0f);
        }
        
        Debug.Log("Drone was destroyed or exit animation completed, can spawn a new one now");
        activeDrone = null;

        // Calculate a random delay before spawning the next drone
        float randomDelay = Random.Range(spawnDelayMin, spawnDelayMax);
        Debug.Log($"Waiting {randomDelay} seconds before spawning next drone");
        yield return new WaitForSeconds(randomDelay);

        // Allow the next drone to spawn
        isWaitingForNextSpawn = false;
    }
    
    // Set layer recursively for all children
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null) return;
        
        obj.layer = layer;
        
        foreach (Transform child in obj.transform)
        {
            if (child == null) continue;
            SetLayerRecursively(child.gameObject, layer);
        }
    }
    
    // Make sure all colliders are triggers
    private void SetCollidersAsTriggers(GameObject obj)
    {
        if (obj == null) return;
        
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.isTrigger = true;
        }
    }
}
