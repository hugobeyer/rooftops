using UnityEngine;
using System.Collections;

public class DroneGameAction : MonoBehaviour
{
    [Tooltip("The prefab to instantiate as a game action")]
    public GameObject actionPrefab;
    
    [Tooltip("Optional parent transform for the instantiated prefab")]
    public Transform spawnParent;
    
    [Tooltip("Base spawn position (world space) or relative to this object")]
    public Vector3 spawnPosition = Vector3.zero;
    
    private GameObject activeDrone;
    private bool isDroneActive => activeDrone != null;

    void Start()
    {
        StartCoroutine(SpawnActionCoroutine());
    }
    
    IEnumerator SpawnActionCoroutine()
    {
        while (true)
        {
            if (!isDroneActive && actionPrefab != null)
            {
                GameObject instance = Instantiate(actionPrefab, spawnPosition, Quaternion.identity, spawnParent);
                DroneMovement droneMovement = instance.GetComponent<DroneMovement>();
                if (droneMovement != null) 
                {
                    activeDrone = instance;
                    StartCoroutine(WaitForDroneDestruction(instance));
                }
            }

            yield return new WaitForSeconds(5f);
        }
    }

    private IEnumerator WaitForDroneDestruction(GameObject drone)
    {
        yield return new WaitUntil(() => drone == null);
        activeDrone = null;
    }
}
