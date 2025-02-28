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
        public float tridotSpotFrequency = 0.5f;
        public float jumpPadFrequency = 0.5f;

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
                if (Random.value < tridotSpotFrequency)
                {
                    GameObject item = Instantiate(tridotSpotPrefab, spot.position, Quaternion.identity);
                    item.transform.SetParent(module.transform);
                }
                else if (Random.value < jumpPadFrequency)
                {
                    GameObject item = Instantiate(jumpPadPrefab, spot.position, Quaternion.identity);
                    item.transform.SetParent(module.transform);
                }
            }
        }
    }
}
