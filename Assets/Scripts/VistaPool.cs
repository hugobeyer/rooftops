using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RoofTops;

namespace RoofTops
{
    public class VistaPool : MonoBehaviour
    {
        public static VistaPool Instance { get; private set; }

        [Header("Vista Settings")]
        public List<GameObject> vistaPrefabs; // assign your vista prefabs here
        public int numberOfVistasOnScreen = 3; // how many vistas remain active at any time
        public float vistaSpanSize = 64f; // fixed size for vista modules

        [Header("Movement Settings")]
        public Transform vistaMovement; // node that will be moved to simulate environment motion
        public Transform vistaVolume;   // volume that defines the spawn/removal boundaries
        public float gameSpeed { get; private set; } // Current speed of vista movement

        [Header("Debug")]
        public bool showDebugVisuals = true;

        private List<GameObject> activeVistas = new List<GameObject>();
        private Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();
        private BoxCollider volumeCollider;
        private float volumeBackBoundary;
        private float volumeFrontBoundary;
        private bool isMoving = true;

        void Awake()
        {
            Instance = this;
            gameSpeed = GameManager.Instance.initialGameSpeed;
        }

        void OnValidate()
        {
            numberOfVistasOnScreen = Mathf.Max(2, numberOfVistasOnScreen);
            vistaSpanSize = Mathf.Max(1f, vistaSpanSize);
        }

        void Start()
        {
            if (!ValidateSetup()) return;

            InitializeVolumeBoundaries();
            SpawnInitialVistas();
        }

        bool ValidateSetup()
        {
            if (vistaPrefabs == null || vistaPrefabs.Count == 0)
            {
                Debug.LogError($"{gameObject.name}: No vista prefabs assigned!");
                return false;
            }

            if (!vistaVolume)
            {
                Debug.LogError($"{gameObject.name}: No vista volume assigned!");
                return false;
            }

            volumeCollider = vistaVolume.GetComponent<BoxCollider>();
            if (!volumeCollider)
            {
                Debug.LogError($"{gameObject.name}: Vista volume must have a BoxCollider!");
                return false;
            }

            if (!vistaMovement)
            {
                Debug.LogError($"{gameObject.name}: No vista movement transform assigned!");
                return false;
            }

            return true;
        }

        void InitializeVolumeBoundaries()
        {
            volumeBackBoundary = vistaVolume.transform.position.z - volumeCollider.size.z * vistaVolume.transform.localScale.z / 2;
            volumeFrontBoundary = vistaVolume.transform.position.z + volumeCollider.size.z * vistaVolume.transform.localScale.z / 2;
        }

        void SpawnInitialVistas()
        {
            // Spawn first vista at the back of the volume
            GameObject firstVista = GetVistaFromPool(vistaPrefabs[Random.Range(0, vistaPrefabs.Count)]);
            firstVista.transform.SetParent(vistaMovement);
            firstVista.transform.position = new Vector3(
                vistaMovement.position.x, 
                vistaMovement.position.y, 
                volumeBackBoundary
            );
            firstVista.SetActive(true);
            activeVistas.Add(firstVista);

            // Spawn remaining vistas forward
            float nextSpawnPoint = volumeBackBoundary + vistaSpanSize;
            for (int i = 1; i < numberOfVistasOnScreen; i++)
            {
                GameObject vista = GetVistaFromPool(vistaPrefabs[Random.Range(0, vistaPrefabs.Count)]);
                vista.transform.SetParent(vistaMovement);
                vista.transform.position = new Vector3(
                    vistaMovement.position.x,
                    vistaMovement.position.y,
                    nextSpawnPoint
                );
                vista.SetActive(true);
                activeVistas.Add(vista);
                nextSpawnPoint += vistaSpanSize;
            }
        }

        void Update()
        {
            if (GameManager.Instance.IsPaused) return;
            
            if (ModulePool.Instance != null)
            {
                // Always match ModulePool's speed, both in initial and game states
                gameSpeed = ModulePool.Instance.gameSpeed;
                vistaMovement.Translate(Vector3.back * gameSpeed * Time.deltaTime);
            }

            if (activeVistas.Count == 0) return;

            RecycleVistasIfNeeded();
            MaintainVistaCount();
        }

        void RecycleVistasIfNeeded()
        {
            GameObject firstVista = activeVistas[0];
            float vistaEnd = firstVista.transform.position.z + vistaSpanSize;

            // If the vista has moved completely past the volume's back boundary
            if (vistaEnd < volumeBackBoundary)
            {
                // Get the last vista's position
                GameObject lastVista = activeVistas[activeVistas.Count - 1];
                float lastVistaEnd = lastVista.transform.position.z + vistaSpanSize;

                // Position at the end of the last vista
                firstVista.transform.position = new Vector3(
                    vistaMovement.position.x,
                    vistaMovement.position.y,
                    lastVistaEnd
                );
                
                // Move to end of list
                activeVistas.RemoveAt(0);
                activeVistas.Add(firstVista);
            }
        }

        void MaintainVistaCount()
        {
            while (activeVistas.Count < numberOfVistasOnScreen)
            {
                SpawnVista();
            }
        }

        void SpawnVista()
        {
            if (activeVistas.Count == 0)
            {
                SpawnInitialVistas();
                return;
            }

            GameObject prefab = vistaPrefabs[Random.Range(0, vistaPrefabs.Count)];
            GameObject vista = GetVistaFromPool(prefab);

            vista.transform.SetParent(vistaMovement);
            vista.SetActive(true);

            // Position at the end of the last vista
            GameObject lastVista = activeVistas[activeVistas.Count - 1];
            float lastVistaEnd = lastVista.transform.position.z + vistaSpanSize;
            vista.transform.position = new Vector3(
                vistaMovement.position.x,
                vistaMovement.position.y,
                lastVistaEnd
            );

            activeVistas.Add(vista);
        }

        GameObject GetVistaFromPool(GameObject prefab)
        {
            string key = prefab.name;
            if (poolDictionary.ContainsKey(key) && poolDictionary[key].Count > 0)
            {
                return poolDictionary[key].Dequeue();
            }

            GameObject newVista = Instantiate(prefab);
            newVista.name = prefab.name; // Remove "(Clone)" suffix
            return newVista;
        }

        void ReturnVistaToPool(GameObject vista)
        {
            vista.SetActive(false);
            string key = vista.name;
            if (!poolDictionary.ContainsKey(key))
            {
                poolDictionary[key] = new Queue<GameObject>();
            }
            poolDictionary[key].Enqueue(vista);
        }

        void OnDrawGizmos()
        {
            if (!showDebugVisuals || !vistaVolume) return;

            // Draw volume boundaries
            BoxCollider vol = vistaVolume.GetComponent<BoxCollider>();
            if (!vol) return;

            // Draw volume bounds
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(vistaVolume.position, Vector3.Scale(vol.size, vistaVolume.localScale));

            // Draw active vistas
            if (!Application.isPlaying) return;

            foreach (var vista in activeVistas)
            {
                if (!vista) continue;

                // Draw vista bounds (fixed size)
                Gizmos.color = Color.green;
                Vector3 vistaCenter = vista.transform.position + new Vector3(0, 0, vistaSpanSize/2);
                Vector3 vistaSize = new Vector3(1, 1, vistaSpanSize); // Using 1 for X and Y as a visual reference
                Gizmos.DrawWireCube(vistaCenter, vistaSize);

                // Draw vista origin point
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(vista.transform.position, 0.1f);
            }
        }

        // Match ModulePool's movement control
        public void SetMovement(bool moving)
        {
            isMoving = moving;
            if(moving) gameSpeed = GameManager.Instance.initialGameSpeed;
        }

        public void ResetSpeed()
        {
            gameSpeed = GameManager.Instance.initialGameSpeed;
        }

        // Add backwards compatibility property to match ModulePool
        public float currentMoveSpeed { get { return gameSpeed; } set { gameSpeed = value; } }
    }
} 