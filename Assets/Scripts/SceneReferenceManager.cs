using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace RoofTops
{
    /// <summary>
    /// Manages references between objects in different scenes.
    /// This helps solve the problem of objects in the Core scene needing references to objects in gameplay scenes.
    /// </summary>
    public class SceneReferenceManager : MonoBehaviour
    {
        private static SceneReferenceManager _instance;
        public static SceneReferenceManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<SceneReferenceManager>();
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("SceneReferenceManager");
                        _instance = obj.AddComponent<SceneReferenceManager>();
                        DontDestroyOnLoad(obj);
                    }
                }
                return _instance;
            }
        }

        // Dictionary to store references by type and ID
        private Dictionary<string, GameObject> gameObjectReferences = new Dictionary<string, GameObject>();
        private Dictionary<string, Component> componentReferences = new Dictionary<string, Component>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Subscribe to scene loaded event
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Clear references when a new scene is loaded
            // This prevents stale references to destroyed objects
            gameObjectReferences.Clear();
            componentReferences.Clear();
            
            Debug.Log($"SceneReferenceManager: References cleared for scene {scene.name}");
        }

        // Register a GameObject with a specific ID
        public void RegisterGameObject(string id, GameObject obj)
        {
            if (obj != null)
            {
                gameObjectReferences[id] = obj;
                Debug.Log($"SceneReferenceManager: Registered GameObject '{id}'");
            }
        }

        // Get a GameObject by ID
        public GameObject GetGameObject(string id)
        {
            if (gameObjectReferences.TryGetValue(id, out GameObject obj))
            {
                return obj;
            }
            
            Debug.LogWarning($"SceneReferenceManager: GameObject '{id}' not found");
            return null;
        }

        // Register a component with a specific ID
        public void RegisterComponent<T>(string id, T component) where T : Component
        {
            if (component != null)
            {
                componentReferences[id] = component;
                Debug.Log($"SceneReferenceManager: Registered {typeof(T).Name} '{id}'");
            }
        }

        // Get a component by ID
        public T GetComponent<T>(string id) where T : Component
        {
            if (componentReferences.TryGetValue(id, out Component component) && component is T typedComponent)
            {
                return typedComponent;
            }
            
            Debug.LogWarning($"SceneReferenceManager: Component '{id}' of type {typeof(T).Name} not found");
            return null;
        }

        // Helper method to register the player
        public void RegisterPlayer(GameObject player)
        {
            RegisterGameObject("Player", player);
            
            // Also register the PlayerController if it exists
            PlayerController controller = player.GetComponent<PlayerController>();
            if (controller != null)
            {
                RegisterComponent("PlayerController", controller);
            }
        }

        // Helper method to get the player
        public GameObject GetPlayer()
        {
            return GetGameObject("Player");
        }

        // Helper method to get the PlayerController
        public PlayerController GetPlayerController()
        {
            return GetComponent<PlayerController>("PlayerController");
        }

        // Helper method to register UI elements
        public void RegisterUI(string id, GameObject uiElement)
        {
            RegisterGameObject("UI_" + id, uiElement);
        }

        // Helper method to get UI elements
        public GameObject GetUI(string id)
        {
            return GetGameObject("UI_" + id);
        }
    }
} 