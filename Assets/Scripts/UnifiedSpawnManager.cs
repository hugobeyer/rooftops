using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RoofTops
{
    /// <summary>
    /// A unified manager to spawn bonuses, jump pads, and props
    /// at any transform tagged "SpawnPoints" inside a module.
    /// </summary>
    public class UnifiedSpawnManager : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private GameObject bonusPrefab;
        [SerializeField] private GameObject jumpPadPrefab;
        [SerializeField] private GameObject[] propPrefabs;

        [Tooltip("Probability of spawning a bonus (0 to 1).")]
        [SerializeField] private float bonusFrequency = 0.25f;

        [Tooltip("Probability of spawning a jump pad (0 to 1).")]
        [SerializeField] private float jumpPadFrequency = 0.25f;

        [Tooltip("Probability of spawning a prop (0 to 1).")]
        [SerializeField] private float propFrequency = 0.5f;

        [SerializeField] private float spawnStartDelay = 2f;

        private ModulePool modulePool;

        private void Start()
        {
            modulePool = ModulePool.Instance;
            if (modulePool == null)
            {
                Debug.LogError("UnifiedSpawnManager: No ModulePool found in scene.");
                enabled = false;
                return;
            }

            StartCoroutine(DelayedSpawningRoutine());
        }

        private IEnumerator DelayedSpawningRoutine()
        {
            while (GameManager.Instance == null || !GameManager.Instance.HasGameStarted)
            {
                yield return null;
            }

            yield return new WaitForSeconds(spawnStartDelay);

            StartCoroutine(WatchForNewModules());
        }

        private IEnumerator WatchForNewModules()
        {
            GameObject lastProcessedModule = null;

            while (true)
            {
                if (modulePool != null
                    && modulePool.activeModules != null
                    && modulePool.activeModules.Count > 0)
                {
                    GameObject currentModule =
                      modulePool.activeModules[modulePool.activeModules.Count - 1];
                    if (currentModule != null && currentModule != lastProcessedModule)
                    {
                        SpawnAtAllPoints(currentModule);
                        lastProcessedModule = currentModule;
                    }
                }

                yield return new WaitForSeconds(0.25f);
            }
        }

        /// <summary>
        /// Spawns bonuses, jump pads, or props at the spawn points found within the given module.
        /// </summary>
        /// <param name="module">The module in which to spawn items.</param>
        private void SpawnAtAllPoints(GameObject module)
        {
            if (!module) return;

            // 1) Find all transforms tagged "SpawnPoints"
            var spawnPoints = module.GetComponentsInChildren<Transform>()
                .Where(t => t.CompareTag("SpawnPoints"));

            // 2) For each point, decide what to spawn
            foreach (var point in spawnPoints)
            {
                float roll = Random.value; // random between 0 and 1

                if (roll < bonusFrequency && bonusPrefab != null)
                {
                    // Spawn bonus
                    GameObject bonus = Instantiate(bonusPrefab, point.position, point.rotation);
                    bonus.transform.SetParent(module.transform);
                }
                else if (roll < (bonusFrequency + jumpPadFrequency) && jumpPadPrefab != null)
                {
                    // Spawn jump pad
                    GameObject jumpPad = Instantiate(jumpPadPrefab, point.position, point.rotation);
                    jumpPad.transform.SetParent(module.transform);
                }
                else if (propPrefabs != null && propPrefabs.Length > 0)
                {
                    // Spawn props
                    float propRoll = Random.value;
                    if (propRoll < propFrequency)
                    {
                        // Pick a random prop from the list
                        int randomIndex = Random.Range(0, propPrefabs.Length);
                        GameObject chosenProp = propPrefabs[randomIndex];
                        GameObject propInstance = Instantiate(chosenProp, point.position, point.rotation);
                        // Parent to module so it moves with it
                        propInstance.transform.SetParent(module.transform);
                    }
                }
            }
        }
    }
} 