using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

namespace RoofTops
{
    public class SpawnerController : MonoBehaviour
    {
        [Header("Spawn Settings")]
        public GameObject tridotSpotPrefab;
        public GameObject jumpPadPrefab;
        [Tooltip("Array of prop prefabs to spawn randomly")]
        public GameObject[] propPrefabs;
        public float tridotSpotFrequency = 0.5f;
        public float jumpPadFrequency = 0.5f;
        [Tooltip("Probability of spawning a prop (0-1)")]
        public float propFrequency = 0.3f;

        private ModulePool modulePool;
        private GameObject lastProcessedModule;

        void Start()
        {
            modulePool = ModulePool.Instance;
            if (modulePool == null)
            {
                enabled = false;
                return;
            }

            StartCoroutine(WatchForNewModules());
        }

        private System.Collections.IEnumerator WatchForNewModules()
        {
            while (true)
            {
                if (modulePool != null && modulePool.activeModules != null && modulePool.activeModules.Count > 0)
                {
                    GameObject currentModule = modulePool.activeModules[modulePool.activeModules.Count - 1];
                    if (currentModule != null && currentModule != lastProcessedModule)
                    {
                        SpawnAtAllSpots(currentModule);
                        lastProcessedModule = currentModule;
                    }
                }
                yield return new WaitForSeconds(0.1f);
            }
        }

        private void SpawnAtAllSpots(GameObject module)
        {
            if (module == null) return;

            var spots = module.GetComponentsInChildren<Transform>()
                .Where(t => t.CompareTag("customSpots"));

            foreach (var spot in spots)
            {
                float randomValue = Random.value;
                float totalProbability = 0f;
                
                // Check for tridot spawn
                totalProbability += tridotSpotFrequency;
                if (randomValue < totalProbability)
                {
                    GameObject item = Instantiate(tridotSpotPrefab, spot.position, Quaternion.identity);
                    item.transform.SetParent(module.transform);
                }
                // Check for jumppad spawn
                else
                {
                    totalProbability += jumpPadFrequency;
                    if (randomValue < totalProbability)
                    {
                        GameObject item = Instantiate(jumpPadPrefab, spot.position, Quaternion.identity);
                        item.transform.SetParent(module.transform);
                    }
                    // Check for prop spawn
                    else if (propPrefabs != null && propPrefabs.Length > 0)
                    {
                        totalProbability += propFrequency;
                        if (randomValue < totalProbability)
                        {
                            int randomIndex = Random.Range(0, propPrefabs.Length);
                            GameObject item = Instantiate(propPrefabs[randomIndex], spot.position, Quaternion.identity);
                            item.transform.SetParent(module.transform);
                        }
                    }
                }
            }
        }
    }
}
