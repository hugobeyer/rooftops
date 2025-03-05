using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoofTops
{
    /// <summary>
    /// Helper script to set up the Core scene with all necessary components.
    /// Attach this to a GameObject in the Core scene.
    /// </summary>
    public class CoreSceneSetup : MonoBehaviour
    {
        [Header("Scene Loading")]
        [SerializeField] private string initialSceneToLoad = "Main";
        [SerializeField] private LoadSceneMode loadMode = LoadSceneMode.Additive;
        
        [Header("Manager Prefabs")]
        [SerializeField] private GameObject gameManagerPrefab;
        [SerializeField] private GameObject economyManagerPrefab;
        [SerializeField] private GameObject gameAdsManagerPrefab;
        [SerializeField] private GameObject goalAchievementManagerPrefab;
        [SerializeField] private GameObject goalValuesManagerPrefab;
        [SerializeField] private GameObject inputActionManagerPrefab;
        [SerializeField] private GameObject cameraPrefab;
        
        [Header("Gameplay Systems")]
        [SerializeField] private GameObject modulePoolPrefab;
        [SerializeField] private GameObject vistaPoolPrefab;
        
        [Header("Player")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Vector3 playerSpawnPosition = new Vector3(0, 0, 0);
        [SerializeField] private bool spawnPlayerOnStart = true;
        
        [Header("Setup")]
        [SerializeField] private bool setupOnStart = true;
        
        private void Start()
        {
            // Ensure we start in the StartingUp state
            if (GameManager.GamesState != GameStates.StartingUp)
            {
                GameManager.RequestGameStateChange(GameStates.StartingUp);
            }
            
            // Setup the core scene
            SetupCoreScene();
            
            // Subscribe to game state changes to spawn player when entering Playing state
            GameManager.OnGameStateChanged += HandleGameStateChanged;
        }
        
        private void HandleGameStateChanged(GameStates oldState, GameStates newState)
        {
            // Handle player when entering Playing state
            if (newState == GameStates.Playing && spawnPlayerOnStart)
            {
                // Check if player already exists
                PlayerController existingPlayer = FindObjectOfType<PlayerController>();
                if (existingPlayer == null)
                {
                    Debug.Log("CoreSceneSetup: Game state changed to Playing, spawning player");
                    SpawnPlayer();
                }
                else
                {
                    // Ensure the player is active when entering Playing state
                    if (!existingPlayer.gameObject.activeSelf)
                    {
                        existingPlayer.gameObject.SetActive(true);
                        Debug.Log("CoreSceneSetup: Activating existing player");
                    }
                    else
                    {
                        Debug.Log("CoreSceneSetup: Player already exists and is active");
                    }
                }
            }
            // Handle player when exiting Playing state
            else if (oldState == GameStates.Playing && newState != GameStates.Playing)
            {
                // Optionally deactivate player when leaving Playing state
                PlayerController existingPlayer = FindObjectOfType<PlayerController>();
                if (existingPlayer != null && existingPlayer.gameObject.activeSelf)
                {
                    existingPlayer.gameObject.SetActive(false);
                    Debug.Log("CoreSceneSetup: Deactivating player when leaving Playing state");
                }
            }
        }
        
        public void SetupCoreScene()
        {
            Debug.Log("Setting up Core Scene...");
            
            // Create SceneReferenceManager if it doesn't exist
            if (FindObjectOfType<SceneReferenceManager>() == null)
            {
                GameObject obj = new GameObject("SceneReferenceManager");
                obj.AddComponent<SceneReferenceManager>();
                DontDestroyOnLoad(obj);
                Debug.Log("Created SceneReferenceManager");
            }
            
            // Instantiate GameManager if needed
            if (FindObjectOfType<GameManager>() == null && gameManagerPrefab != null)
            {
                Instantiate(gameManagerPrefab);
                Debug.Log("GameManager instantiated from prefab");
            }
            
            // Instantiate EconomyManager if needed
            if (FindObjectOfType<EconomyManager>() == null && economyManagerPrefab != null)
            {
                Instantiate(economyManagerPrefab);
                Debug.Log("EconomyManager instantiated from prefab");
            }
            
            // Instantiate all other manager prefabs if they don't exist
            if (gameAdsManagerPrefab != null && FindObjectOfType<GameAdsManager>() == null)
            {
                // Create as root GameObject to avoid DontDestroyOnLoad issues
                GameObject adsManager = Instantiate(gameAdsManagerPrefab);
                // Ensure it's a root object
                adsManager.transform.SetParent(null);
                Debug.Log("Game Ads Manager instantiated");
            }
            
            // Instantiate GoalValuesManager first
            GameObject goalValuesManagerObj = null;
            if (goalValuesManagerPrefab != null && FindObjectOfType<GoalValuesManager>() == null)
            {
                goalValuesManagerObj = Instantiate(goalValuesManagerPrefab);
                Debug.Log("GoalValuesManager instantiated from prefab");
            }
            else
            {
                goalValuesManagerObj = FindObjectOfType<GoalValuesManager>()?.gameObject;
            }
            
            // Then instantiate GoalAchievementManager and set the reference
            if (goalAchievementManagerPrefab != null && FindObjectOfType<GoalAchievementManager>() == null)
            {
                GameObject goalAchievementObj = Instantiate(goalAchievementManagerPrefab);
                Debug.Log("GoalAchievementManager instantiated from prefab");
                
                // Set the reference to GoalValuesManager
                if (goalValuesManagerObj != null)
                {
                    GoalAchievementManager goalAchievementManager = goalAchievementObj.GetComponent<GoalAchievementManager>();
                    GoalValuesManager goalValuesManager = goalValuesManagerObj.GetComponent<GoalValuesManager>();
                    
                    if (goalAchievementManager != null && goalValuesManager != null)
                    {
                        goalAchievementManager.goalValuesManager = goalValuesManager;
                        Debug.Log("Set GoalValuesManager reference on GoalAchievementManager");
                    }
                }
            }
            
            if (inputActionManagerPrefab != null && FindObjectOfType<InputActionManager>() == null)
            {
                Instantiate(inputActionManagerPrefab);
                Debug.Log("Input Action Manager instantiated");
            }
            
            if (cameraPrefab != null && FindObjectOfType<NoiseMovement>() == null)
            {
                Instantiate(cameraPrefab);
                Debug.Log("Camera instantiated");
            }
            
            // Instantiate gameplay systems
            if (modulePoolPrefab != null && FindObjectOfType<ModulePool>() == null)
            {
                Instantiate(modulePoolPrefab);
                Debug.Log("Module Pool instantiated");
            }
            
            if (vistaPoolPrefab != null && FindObjectOfType<VistaPool>() == null)
            {
                Instantiate(vistaPoolPrefab);
                Debug.Log("Vista Pool instantiated");
            }
            
            // After all components are created, initialize the GoalAchievementManager
            InitializeGoalManagers();
            
            // Load the initial scene if not already in it
            LoadInitialScene();
            
            // Listen for scene loaded event to spawn player and gameplay systems
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            Debug.Log("Core Scene setup complete");
        }
        
        private void InitializeGoalManagers()
        {
            Debug.Log("Initializing Goal Managers...");
            
            // Find the managers
            GoalValuesManager goalValuesManager = FindObjectOfType<GoalValuesManager>();
            GoalAchievementManager goalAchievementManager = FindObjectOfType<GoalAchievementManager>();
            
            if (goalValuesManager == null)
            {
                Debug.LogError("GoalValuesManager not found in scene!");
                return;
            }
            
            if (goalAchievementManager == null)
            {
                Debug.LogError("GoalAchievementManager not found in scene!");
                return;
            }
            
            // Set the reference
            goalAchievementManager.goalValuesManager = goalValuesManager;
            Debug.Log("Set GoalValuesManager reference on GoalAchievementManager");
            
            // Apply the values
            goalValuesManager.ApplyToGoalAchievementManager();
            Debug.Log("Applied values from GoalValuesManager to GoalAchievementManager");
        }
        
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == initialSceneToLoad)
            {
                // Instantiate ModulePool if needed
                if (FindObjectOfType<ModulePool>() == null && modulePoolPrefab != null)
                {
                    Instantiate(modulePoolPrefab);
                    Debug.Log("ModulePool instantiated from prefab");
                }
                
                // Instantiate VistaPool if needed
                if (FindObjectOfType<VistaPool>() == null && vistaPoolPrefab != null)
                {
                    Instantiate(vistaPoolPrefab);
                    Debug.Log("VistaPool instantiated from prefab");
                }
                
                // Remove the listener after setup is complete
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
        }
        
        public void SpawnPlayer()
        {
            // Only spawn if no player exists
            if (FindObjectOfType<PlayerController>() == null && playerPrefab != null)
            {
                GameObject player = Instantiate(playerPrefab, playerSpawnPosition, Quaternion.identity);
                Debug.Log("Player instantiated from prefab");
                
                // Set player active state based on game state
                if (GameManager.GamesState == GameStates.Playing)
                {
                    player.SetActive(true);
                    Debug.Log("Player set active (game is in Playing state)");
                }
                else
                {
                    player.SetActive(false);
                    Debug.Log("Player set inactive (game is not in Playing state)");
                }
                
                // Register with SceneReferenceManager
                if (SceneReferenceManager.Instance != null)
                {
                    SceneReferenceManager.Instance.RegisterGameObject("Player", player);
                    Debug.Log("Player registered with SceneReferenceManager");
                }
                else
                {
                    Debug.LogError("SceneReferenceManager instance not found when spawning player!");
                }
                
                // If GameManager exists, assign the player reference
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.player = player;
                    Debug.Log("Player assigned to GameManager");
                }
            }
            else
            {
                Debug.Log("Player already exists or playerPrefab not assigned");
            }
        }
        
        private void LoadInitialScene()
        {
            // Check if we're already in the initial scene
            if (SceneManager.GetActiveScene().name != initialSceneToLoad)
            {
                Debug.Log($"Loading initial scene: {initialSceneToLoad}");
                SceneManager.LoadScene(initialSceneToLoad, loadMode);
            }
        }
        
        // Add a button in the inspector to manually set up the scene
        [ContextMenu("Setup Core Scene")]
        public void ManualSetupCoreScene()
        {
            SetupCoreScene();
        }
        
        // Add a button to manually spawn the player
        [ContextMenu("Spawn Player")]
        public void ManualSpawnPlayer()
        {
            SpawnPlayer();
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            SceneManager.sceneLoaded -= OnSceneLoaded;
            GameManager.OnGameStateChanged -= HandleGameStateChanged;
        }
    }
} 