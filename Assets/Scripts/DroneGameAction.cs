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
        // Wait until the drone is null (destroyed)
        while (drone != null)
        {
            yield return null;
        }
        
        Debug.Log("Drone was destroyed, can spawn a new one now");
        activeDrone = null;
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
