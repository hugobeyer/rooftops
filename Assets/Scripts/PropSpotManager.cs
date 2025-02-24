using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// A simple manager to find modules (now called PropSpots) and spawn objects 
/// based on SpawnPoint priorities.
/// </summary>
public class PropSpotManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject[] propPrefabs;

    [SerializeField] private List<GameObject> modules;

    private void Start()
    {
        if (modules == null || modules.Count == 0)
        {
            modules = GameObject.FindGameObjectsWithTag("PropSpots").ToList();
        }

        SpawnCubesOnModules();
    }

    private void SpawnCubesOnModules()
    {
        for (int i = 0; i < modules.Count; i++)
        {
            if (i % 4 != 0)
            {
                continue;
            }

            GameObject module = modules[i];
            SpawnPoint[] moduleSpawnPoints = module.GetComponentsInChildren<SpawnPoint>();

            if (moduleSpawnPoints.Length == 0)
            {
                continue;
            }

            SpawnPoint chosenSpawnPoint = moduleSpawnPoints
                .OrderByDescending(sp => sp.priority)
                .FirstOrDefault(sp => !sp.isOccupied);

            if (chosenSpawnPoint == null)
            {
                continue;
            }

            int randomIndex = Random.Range(0, propPrefabs.Length);
            GameObject chosenPrefab = propPrefabs[randomIndex];

            GameObject propInstance = Instantiate(
                chosenPrefab,
                chosenSpawnPoint.transform.position,
                chosenSpawnPoint.transform.rotation
            );

            propInstance.transform.SetParent(module.transform);
        }
    }
} 