using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RoofTops;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;

namespace RoofTops.UI
{
    public class GameOverGUI : MonoBehaviour
    {
        [Header("Game Over Panel")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text newBestText;
        [SerializeField] private TMP_Text tridotCountText;
        [SerializeField] private TMP_Text memcardCountText;

        [Header("Action Buttons")]
        [SerializeField] private GameObject restartButton;
        [SerializeField] private GameObject skipPurchButton;
        [SerializeField] private GameObject passPurchButton;
        [SerializeField] private TMP_Text skipText;
        
        [Header("Raycast Settings")]
        [SerializeField] private Camera uiCamera;
        [SerializeField] private LayerMask buttonLayerMask;
        
        [SerializeField] private InputActionManager inputManager;
        
        private bool isProcessingClick = false;
        private GameObject highlightedButton = null;
        private float highlightDuration = 0.5f;
        private float highlightTimer = 0f;

        private void Awake()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
            
            if (uiCamera == null)
            {
                uiCamera = Camera.main;
            }
        }

        private void Start()
        {
            // Check if buttons have colliders
            CheckButtonColliders();
            
            // Set button layers
            SetButtonLayers();
            
            // Subscribe to game state changes
            GameManager.OnGameStateChanged += HandleGameStateChanged;
            
            // If we're already in game over state, set up the listeners
            if (GameManager.GamesState == GameStates.GameOver)
            {
                SetupButtonListeners();
            }
            
            // Add direct click handlers to buttons
            AddClickHandlers();
            
            // Set up input action listeners
            if (inputManager != null)
            {
                inputManager.OnJumpPressed.AddListener(ProcessClick);
            }
            else
            {
                Debug.LogWarning("GameOverGUI: InputActionManager not assigned, trying to find it");
                inputManager = FindFirstObjectByType<InputActionManager>();
                if (inputManager != null)
                {
                    inputManager.OnJumpPressed.AddListener(ProcessClick);
                }
                else
                {
                    Debug.LogError("GameOverGUI: InputActionManager not found, button clicks won't work!");
                }
            }
        }
        
        private void AddClickHandlers()
        {
            // Add direct click handlers to buttons
            if (restartButton != null)
            {
                Collider collider = restartButton.GetComponent<Collider>();
                if (collider != null)
                {
                    // Add a MonoBehaviour with OnMouseDown if needed
                    if (restartButton.GetComponent<ButtonClickHandler>() == null)
                    {
                        ButtonClickHandler handler = restartButton.AddComponent<ButtonClickHandler>();
                        handler.Initialize(this, "OnRooftopClick");
                    }
                }
            }
            
            if (skipPurchButton != null)
            {
                Collider collider = skipPurchButton.GetComponent<Collider>();
                if (collider != null)
                {
                    // Add a MonoBehaviour with OnMouseDown if needed
                    if (skipPurchButton.GetComponent<ButtonClickHandler>() == null)
                    {
                        ButtonClickHandler handler = skipPurchButton.AddComponent<ButtonClickHandler>();
                        handler.Initialize(this, "OnTridotSkipClick");
                    }
                }
            }
            
            if (passPurchButton != null)
            {
                Collider collider = passPurchButton.GetComponent<Collider>();
                if (collider != null)
                {
                    // Add a MonoBehaviour with OnMouseDown if needed
                    if (passPurchButton.GetComponent<ButtonClickHandler>() == null)
                    {
                        ButtonClickHandler handler = passPurchButton.AddComponent<ButtonClickHandler>();
                        handler.Initialize(this, "OnPassPurchaseClick");
                    }
                }
            }
        }

        private void CheckButtonColliders()
        {
            // Check restart button
            if (restartButton != null && restartButton.GetComponent<Collider>() == null)
            {
                Debug.LogError("Restart button has no collider! Adding BoxCollider.");
                restartButton.AddComponent<BoxCollider>();
            }
            
            // Check skip button
            if (skipPurchButton != null && skipPurchButton.GetComponent<Collider>() == null)
            {
                Debug.LogError("Skip button has no collider! Adding BoxCollider.");
                skipPurchButton.AddComponent<BoxCollider>();
            }
            
            // Check pass purchase button
            if (passPurchButton != null && passPurchButton.GetComponent<Collider>() == null)
            {
                Debug.LogError("Pass purchase button has no collider! Adding BoxCollider.");
                passPurchButton.AddComponent<BoxCollider>();
            }
        }

        private void SetButtonLayers()
        {
            // Get the layer from the layer mask (assuming only one layer is set)
            int layerNumber = 0;
            int mask = buttonLayerMask.value;
            for (int i = 0; i < 32; i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    layerNumber = i;
                    break;
                }
            }
            
            // Set the layer for all buttons
            if (restartButton != null) restartButton.layer = layerNumber;
            if (skipPurchButton != null) skipPurchButton.layer = layerNumber;
            if (passPurchButton != null) passPurchButton.layer = layerNumber;
            
            Debug.Log($"Set buttons to layer {LayerMask.LayerToName(layerNumber)}");
        }

        private void HandleGameStateChanged(GameStates oldState, GameStates newState)
        {
            if (newState == GameStates.GameOver)
            {
                SetupButtonListeners();
            }
            else
            {
                RemoveButtonListeners();
            }
        }

        private void SetupButtonListeners()
        {
            // Subscribe to jump input for all buttons
            if (InputActionManager.Instance != null)
            {
                InputActionManager.Instance.OnJumpPressed.AddListener(CheckButtonInteractions);
            }
        }

        private void RemoveButtonListeners()
        {
            // Unsubscribe from jump input
            if (InputActionManager.Instance != null)
            {
                InputActionManager.Instance.OnJumpPressed.RemoveListener(CheckButtonInteractions);
            }
        }

        private void CheckButtonInteractions()
        {
            // Only process if game over panel is active
            if (gameOverPanel == null || !gameOverPanel.activeSelf || isProcessingClick)
                return;

            // Debug log to verify this method is being called
            if (GameManager.EnableDetailedLogs)
            {
                Debug.Log("CheckButtonInteractions called");
            }
            
            isProcessingClick = true;

            // Get ray from pointer position
            Ray ray = InputActionManager.Instance.GetRayFromPointer();
            
            // Debug draw the ray to see where it's pointing
            Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 2f);
            
            // Don't use layer mask for now to ensure we're hitting something
            RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
            
            // if (GameManager.EnableDetailedLogs)
            // {
            //     Debug.Log($"Raycast hit {hits.Length} objects");
                
            //     // Log all hits to see what we're hitting
            //     foreach (RaycastHit hit in hits)
            //     {
            //         Debug.Log($"Hit: {hit.collider.gameObject.name}");
            //     }
            // }
            
            // Check all hits
            bool buttonHit = false;
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider.gameObject == restartButton)
                {
                    Debug.Log("Restart button hit!"); // Keep this as essential log
                    highlightedButton = restartButton;
                    highlightTimer = highlightDuration;
                    MakeColliderVisible(restartButton); // Make it visible
                    OnRooftopClick();
                    buttonHit = true;
                    break;
                }
                else if (hits[i].collider.gameObject == skipPurchButton)
                {
                    Debug.Log("Skip button hit!"); // Keep this as essential log
                    highlightedButton = skipPurchButton;
                    highlightTimer = highlightDuration;
                    MakeColliderVisible(skipPurchButton); // Make it visible
                    OnTridotSkipClick();
                    buttonHit = true;
                    break;
                }
                else if (hits[i].collider.gameObject == passPurchButton)
                {
                    Debug.Log("Pass purchase button hit!"); // Keep this as essential log
                    highlightedButton = passPurchButton;
                    highlightTimer = highlightDuration;
                    MakeColliderVisible(passPurchButton); // Make it visible
                    OnPassPurchaseClick();
                    buttonHit = true;
                    break;
                }
            }
            
            if (!buttonHit && GameManager.EnableDetailedLogs)
            {
                Debug.Log("No buttons were hit by the ray");
            }
            
            // Reset processing flag after a short delay
            Invoke("ResetProcessingFlag", 0.5f);
        }

        private void ResetProcessingFlag()
        {
            isProcessingClick = false;
        }

        private void OnDestroy()
        {
            // Unsubscribe from game state changes
            //GameManager.OnGameStateChanged -= HandleGameStateChanged;
            
            // Make sure to remove listeners
            RemoveButtonListeners();
            
            // Remove input action listeners
            if (inputManager != null)
            {
                inputManager.OnJumpPressed.RemoveListener(ProcessClick);
            }
        }

        private void Update()
        {
            // Update highlight timer
            if (highlightedButton != null && highlightTimer > 0)
            {
                highlightTimer -= Time.deltaTime;
            }
            
            // We don't need to check for mouse clicks here anymore
            // The Input System's Jump action (which includes LMB) will call ProcessClick
        }

        private void ProcessClick()
        {
            // Only process clicks in GameOver state
            if (GameManager.GamesState != GameStates.GameOver)
            {
                return;
            }
            
            // Get the ray from the camera to the mouse/touch position
            // Use InputActionManager to get the pointer position or fallback to Mouse.current
            Vector2 pointerPosition;
            
            if (inputManager != null && inputManager.enabled)
            {
                // Use the pointer position from input manager
                pointerPosition = inputManager.PointerPosition;
                Debug.Log($"GameOverGUI: Using InputActionManager pointer position: {pointerPosition}");
            }
            else if (Mouse.current != null)
            {
                // Fallback to direct Input System access
                pointerPosition = Mouse.current.position.ReadValue();
                Debug.Log($"GameOverGUI: Using Mouse.current position: {pointerPosition}");
            }
            else
            {
                Debug.LogWarning("GameOverGUI: No input method available for processing click");
                return;
            }
            
            Ray ray = Camera.main.ScreenPointToRay(pointerPosition);
            RaycastHit[] hits = Physics.RaycastAll(ray);

            if (GameManager.EnableDetailedLogs)
            {
                Debug.Log($"GameOverGUI: ProcessClick - Found {hits.Length} hits at position {pointerPosition}");
                foreach (RaycastHit hit in hits)
                {
                    Debug.Log($"Hit: {hit.collider.gameObject.name}");
                }
            }
            
            // Check all hits
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider.gameObject == restartButton)
                {
                    Debug.Log("Restart button hit!"); // Keep this as essential log
                    highlightedButton = restartButton;
                    highlightTimer = highlightDuration;
                    MakeColliderVisible(restartButton); // Make it visible
                    OnRooftopClick();
                    return;
                }
                else if (hits[i].collider.gameObject == skipPurchButton)
                {
                    Debug.Log("Skip button hit!"); // Keep this as essential log
                    highlightedButton = skipPurchButton;
                    highlightTimer = highlightDuration;
                    MakeColliderVisible(skipPurchButton); // Make it visible
                    OnTridotSkipClick();
                    return;
                }
                else if (hits[i].collider.gameObject == passPurchButton)
                {
                    Debug.Log("Pass purchase button hit!"); // Keep this as essential log
                    highlightedButton = passPurchButton;
                    highlightTimer = highlightDuration;
                    MakeColliderVisible(passPurchButton); // Make it visible
                    OnPassPurchaseClick();
                    return;
                }
            }
        }

        public void ShowGameOver(float distance, bool isNewRecord)
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
                
                // Update UI elements
                if (scoreText != null)
                {
                    scoreText.text = $"{distance:F1}m";
                }
                
                if (newBestText != null)
                {
                    newBestText.gameObject.SetActive(isNewRecord);
                }
                
                // Update collectible counts - using simple integers instead of properties
                if (tridotCountText != null)
                {
                    int tridotCount = 0; // Use the correct property or method here
                    tridotCountText.text = tridotCount.ToString();
                }
                
                if (memcardCountText != null)
                {
                    int memcardCount = 0; // Use the correct property or method here
                    memcardCountText.text = memcardCount.ToString();
                }
                
                // Update button states
                UpdateButtonStates();
                
                // Reset processing flag
            isProcessingClick = false;
            }
        }

        private void UpdateButtonStates()
        {
            // Update skip button text
            if (skipText != null)
            {
                int skipTokens = 0; // Use the correct property or method here
                skipText.text = skipTokens.ToString();
            }

            // Show/hide skip purchase button
            if (skipPurchButton != null)
            {
                // Always show for simplicity
                skipPurchButton.SetActive(true);
            }
            
            // Always show pass purchase button
            if (passPurchButton != null)
            {
                passPurchButton.SetActive(true);
            }
        }

        private void DisableAllButtons()
        {
            // Disable all buttons to prevent multiple clicks
            if (restartButton != null)
            {
                restartButton.SetActive(false);
            }
            
            if (skipPurchButton != null)
            {
                skipPurchButton.SetActive(false);
            }
            
            if (passPurchButton != null)
            {
                passPurchButton.SetActive(false);
            }
        }

        public void OnRooftopClick()
        {
            // Debug.Log($"GameOverGUI: OnRooftopClick called - GameState={GameManager.GamesState}");
            
            // PlayerController playerController = FindAnyObjectByType<PlayerController>();
            // if (playerController != null && !playerController.IsDead)
            // {
            //     Debug.LogWarning("GameOverGUI: Player is not dead, but restart button was clicked. Forcing player death.");
            //     playerController.Die();
            //     return; // Exit early to let the death sequence handle the restart
            // }
            
            // // Removed GameAdsManager references - just reload the scene directly
            // Debug.Log("GameOverGUI: Reloading scene directly (GameAdsManager removed)");
            
            // Set a fallback timer in case the reload doesn't happen
           // StartCoroutine(AdFallbackTimer(3f));
            
            // Introduce a small delay to allow previous resources to be properly cleaned up
           // StartCoroutine(ReloadAfterDelay(0.5f));
        }
        
        // private IEnumerator AdFallbackTimer(float timeout)
        // {
        //     Debug.Log($"GameOverGUI: Starting fallback timer for {timeout} seconds");
        //     yield return new WaitForSeconds(timeout);
        //     Debug.LogWarning("GameOverGUI: Fallback timer expired - forcing scene reload");
        //     ReloadScene();
        // }
        
        private void ReloadScene()
        {
            // Debug.Log("GameOverGUI: ReloadScene called - reloading current scene");
            
            // // Ensure time scale is reset
            // Time.timeScale = 1f;
            
            // // Use GameManager to restart the game
            // if (GameManager.Instance != null)
            // {
            //     Debug.Log("GameOverGUI: Calling GameManager.RestartGame()");
            //     GameManager.Instance.RestartGame();
            // }
            // else
            // {
            //     Debug.LogWarning("GameOverGUI: GameManager.Instance is NULL, reloading scene directly");
            //     SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            // }
        }

        public void OnTridotSkipClick()
        {
            // // Disable buttons to prevent multiple clicks
            //     DisableAllButtons();
                
            // // Skip ad and restart
            //     ReloadScene();
        }
        
        public void OnPassPurchaseClick()
        {
            Debug.Log("GameOverGUI: Opening store for pass purchase");
            
            // Just log the action for now
            Debug.Log("Pass purchase button clicked");
        }

        // private void OnDrawGizmos()
        // {
        //     // Draw highlighted button
        //     if (highlightedButton != null && highlightTimer > 0)
        //     {
        //         BoxCollider boxCollider = highlightedButton.GetComponent<BoxCollider>();
        //         if (boxCollider != null)
        //         {
        //             // Set the color to green
        //             Gizmos.color = Color.green;
                    
        //             // Draw a wire cube using the box collider's bounds
        //             Gizmos.DrawWireCube(boxCollider.bounds.center, boxCollider.bounds.size);
        //         }
        //     }
        // }

        public void DebugButtonRaycast()
        {
            if (uiCamera == null)
            {
                Debug.LogError("No UI camera assigned!");
                return;
            }
            
            // Create a ray from the center of the screen
            Ray ray = uiCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
            
            if (GameManager.EnableDetailedLogs)
            {
                Debug.Log($"Raycast from camera {uiCamera.name} hit {hits.Length} objects:");
                
                foreach (RaycastHit hit in hits)
                {
                    Debug.Log($"Hit: {hit.collider.gameObject.name} at distance {hit.distance}");
                }
            }
            
            // Draw a persistent line to show the ray
            // Debug.DrawLine(ray.origin, ray.direction * 100f, Color.red, 5f);
            
            // Draw a persistent sphere at the hit point
            // DebugDrawSphere(ray.origin + ray.direction * 100f, 0.1f, Color.green, 5f);
        }

        // private void DebugDrawSphere(Vector3 position, float radius, Color color, float duration)
        // {
        //     // Draw lines to approximate a sphere
        //     int segments = 8;
        //     float angle = 0f;
        //     float angleStep = 360f / segments;
            
        //     // Draw circles in all three planes
        //     for (int plane = 0; plane < 3; plane++)
        //     {
        //         Vector3 prevPoint = Vector3.zero;
                
        //         for (int i = 0; i <= segments; i++)
        //         {
        //             float rads = Mathf.Deg2Rad * angle;
        //             Vector3 point = Vector3.zero;
                    
        //             if (plane == 0) // XY plane
        //                 point = position + new Vector3(Mathf.Sin(rads) * radius, Mathf.Cos(rads) * radius, 0);
        //             else if (plane == 1) // XZ plane
        //                 point = position + new Vector3(Mathf.Sin(rads) * radius, 0, Mathf.Cos(rads) * radius);
        //             else // YZ plane
        //                 point = position + new Vector3(0, Mathf.Sin(rads) * radius, Mathf.Cos(rads) * radius);
                    
        //             if (i > 0)
        //                 Debug.DrawLine(prevPoint, point, color, duration);
                    
        //             prevPoint = point;
        //             angle += angleStep;
        //         }
        //     }
        // }

        private void MakeColliderVisible(GameObject button)
        {
            // Get the collider
            BoxCollider boxCollider = button.GetComponent<BoxCollider>();
            if (boxCollider == null) return;
            
            // Create a cube that matches the collider exactly
            GameObject visibleCollider = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visibleCollider.transform.position = boxCollider.bounds.center;
            visibleCollider.transform.localScale = boxCollider.bounds.size;
            
            // Make it green and semi-transparent
            Renderer renderer = visibleCollider.GetComponent<Renderer>();
            renderer.material.color = new Color(0, 1, 0, 0.5f); // Green with 50% transparency
            
            // Destroy it after 2 seconds
            Destroy(visibleCollider, 2f);
        }

        // Renamed from ShowAdWithDelay to ReloadAfterDelay since we're removing ads
        private IEnumerator ReloadAfterDelay(float delay)
        {
            Debug.Log($"GameOverGUI: Waiting {delay}s before reloading scene");
            yield return new WaitForSeconds(delay);
            
            try
            {
                Debug.Log("GameOverGUI: Reloading scene after delay");
                StopAllCoroutines(); // Stop the fallback timer
                ReloadScene();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"GameOverGUI: Error reloading scene: {e.Message}");
                ReloadScene();
            }
        }
    }
    
    // Helper class to handle button clicks directly
    public class ButtonClickHandler : MonoBehaviour
    {
        private GameOverGUI parentGUI;
        private string methodName;
        
        public void Initialize(GameOverGUI gui, string method)
        {
            parentGUI = gui;
            methodName = method;
        }
        
        private void OnMouseDown()
        {
            if (parentGUI != null)
            {
                if (GameManager.EnableDetailedLogs)
                {
                    Debug.Log($"Button clicked: {gameObject.name}, calling {methodName}");
                }
                parentGUI.SendMessage(methodName);
            }
        }
    }
}