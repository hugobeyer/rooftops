// using UnityEngine;
// using System.Collections;
// using UnityEngine.SceneManagement;
// using System.Reflection;

// namespace RoofTops
// {
//     public class GameAdsManager : MonoBehaviour
//     {
//         private static GameAdsManager _instance;
//         public static GameAdsManager Instance { get { return _instance; } }

//         [Header("Ad Settings")]
//         [SerializeField] private float minTimeBetweenAds = 3f;  // Reduced from 30f to 3f for testing

//         [Header("Skip Settings")]
// #pragma warning disable 0414
//         [SerializeField] private int maxAdSkips = 3;
// #pragma warning restore 0414
//         private int adSkipsAvailable = 0;
//         public int AdSkipsAvailable => adSkipsAvailable;

//         // Add flag to track when an ad is currently being shown
//         private bool isShowingAd = false;
//         public bool IsShowingAd => isShowingAd;

//         // Add a cooldown timer to prevent rapid ad attempts
//         private float adCooldownTime = 0f;
//         private const float MIN_AD_COOLDOWN = 1.5f; // Seconds to wait after an ad completes before allowing another

//         private MonoBehaviour adsManager;
//         private float lastAdTime;

//         // Change to MonoBehaviour to avoid direct dependency
//         public MonoBehaviour adsInitializer;

//         // Add the exact type name to find
//         private const string ADS_INITIALIZER_TYPE_NAME = "MobileMonetizationPro_UnityAdsInitializer";

//         private void Awake()
//         {
//             // Singleton pattern with DontDestroyOnLoad
//             if (_instance != null && _instance != this)
//             {
//                 Debug.Log("[GameAdsManager] Instance already exists, destroying duplicate");
//                 Destroy(gameObject);
//                 return;
//             }
            
//             Debug.Log("[GameAdsManager] Initializing singleton instance");
//             _instance = this;
//             DontDestroyOnLoad(gameObject);
            
//             // Find the ads manager component if available
//             adsManager = FindAdsManager();
//             Debug.Log($"[GameAdsManager] adsManager initialized: {(adsManager != null ? "success" : "failed")}");
            
//             // Find ads initializer
//             adsInitializer = FindAdsInitializer();
//             Debug.Log($"[GameAdsManager] adsInitializer initialized: {(adsInitializer != null ? "success" : "failed")}");
            
//             // Register for scene loaded events
//             SceneManager.sceneLoaded += OnSceneLoaded;
            
//             // Initialize ad skips
//             adSkipsAvailable = maxAdSkips;
//         }

//         private void OnDestroy()
//         {
//             // Unregister from scene loaded events
//             SceneManager.sceneLoaded -= OnSceneLoaded;
            
//             // Stop all ad-related coroutines and reset flags
//             StopAllAdCoroutines();
            
//             Debug.Log("[GameAdsManager] OnDestroy completed");
//         }

//         private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
//         {
//             Debug.Log($"[GameAdsManager] Scene loaded: {scene.name}, checking adsManager");
            
//             // Reset flags for safety when a new scene loads
//             isShowingAd = false;
//             adCooldownTime = 0f;
            
//             // Refresh ads manager reference when a new scene is loaded
//             if (adsManager == null)
//             {
//                 Debug.Log("[GameAdsManager] adsManager is null, attempting to find it again");
//                 adsManager = FindAdsManager();
//                 Debug.Log($"[GameAdsManager] adsManager after search: {(adsManager != null ? "found" : "still null")}");
//             }
//             else
//             {
//                 Debug.Log("[GameAdsManager] adsManager already exists");
//             }
            
//             // Always refresh the adsInitializer reference on scene load
//             adsInitializer = FindAdsInitializer();
//             Debug.Log($"[GameAdsManager] adsInitializer after scene load: {(adsInitializer != null ? adsInitializer.name : "NULL")}");
//         }

//         private MonoBehaviour FindAdsManager()
//         {
//             Debug.Log("[GameAdsManager] Searching for MobileMonetizationPro_UnityAdsInitializer");
            
//             // Search for the type 'MobileMonetizationPro_UnityAdsInitializer' in all loaded assemblies
//             System.Type adsType = null;
//             foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
//             {
//                 adsType = assembly.GetType("MobileMonetizationPro_UnityAdsInitializer");
//                 if (adsType != null)
//                 {
//                     Debug.Log($"[GameAdsManager] Found type in assembly: {assembly.FullName}");
//                     break;
//                 }
//             }

//             if (adsType == null)
//             {
//                 Debug.LogWarning("[GameAdsManager] MobileMonetizationPro_UnityAdsInitializer not found in any loaded assemblies. Skipping ads manager setup.");
//                 return null;
//             }

//             var adsInitializerInstance = FindFirstObjectByType(adsType);
//             if (adsInitializerInstance == null)
//             {
//                 Debug.LogWarning("[GameAdsManager] MobileMonetizationPro_UnityAdsInitializer instance not found in scene. Skipping ads manager setup.");
//                 return null;
//             }

//             Debug.Log($"[GameAdsManager] Found instance of {adsType.Name}");
//             return adsInitializerInstance as MonoBehaviour;
//         }

//         // Add a dedicated method to find the ads initializer
//         private MonoBehaviour FindAdsInitializer()
//         {
//             Debug.Log("[GameAdsManager] Searching for MobileMonetizationPro_UnityAdsInitializer");
            
//             // First try to find it using direct static reference
//             var staticInstance = GetStaticInstance(ADS_INITIALIZER_TYPE_NAME);
//             if (staticInstance != null)
//             {
//                 Debug.Log($"[GameAdsManager] Found ads initializer via static instance: {staticInstance.name}");
//                 return staticInstance;
//             }
            
//             // If static reference fails, try to find by type
//             foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
//             {
//                 var adsType = assembly.GetType(ADS_INITIALIZER_TYPE_NAME);
//                 if (adsType != null)
//                 {
//                     Debug.Log($"[GameAdsManager] Found type in assembly: {assembly.FullName}");
//                     var instance = FindFirstObjectByType(adsType) as MonoBehaviour;
//                     if (instance != null)
//                     {
//                         Debug.Log($"[GameAdsManager] Found ads initializer via FindFirstObjectByType: {instance.name}");
//                         return instance;
//                     }
//                 }
//             }
            
//             // Try one more approach - find by name
//             var adObj = GameObject.Find(ADS_INITIALIZER_TYPE_NAME);
//             if (adObj != null)
//             {
//                 var component = adObj.GetComponent<MonoBehaviour>();
//                 if (component != null)
//                 {
//                     Debug.Log($"[GameAdsManager] Found ads initializer via GameObject.Find: {component.name}");
//                     return component;
//                 }
//             }
            
//             Debug.LogWarning("[GameAdsManager] Could not find MobileMonetizationPro_UnityAdsInitializer");
//             return null;
//         }

//         // Helper to get static instance via reflection
//         private MonoBehaviour GetStaticInstance(string typeName)
//         {
//             foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
//             {
//                 var type = assembly.GetType(typeName);
//                 if (type != null)
//                 {
//                     var instanceField = type.GetField("instance", BindingFlags.Public | BindingFlags.Static);
//                     if (instanceField != null)
//                     {
//                         var instance = instanceField.GetValue(null) as MonoBehaviour;
//                         if (instance != null)
//                         {
//                             return instance;
//                         }
//                     }
//                 }
//             }
//             return null;
//         }

//         private void Update()
//         {
//             // Update ad cooldown timer
//             if (adCooldownTime > 0)
//             {
//                 adCooldownTime -= Time.deltaTime;
//                 if (adCooldownTime <= 0)
//                 {
//                     Debug.Log("[GameAdsManager] Ad cooldown period ended");
//                 }
//             }
//         }

//         // Add the helper methods as standalone class methods so they can be called from anywhere
//         private void ResetCameraAfterAd(NoiseMovement cameraController, Vector3 originalPosition, Quaternion originalRotation)
//         {
//             Debug.Log("[GameAdsManager] ResetCameraAfterAd called");
            
//             try
//             {
//                 // If no camera controller provided, try to find it
//                 if (cameraController == null && Camera.main != null)
//                 {
//                     Debug.Log("[GameAdsManager] cameraController is null, getting from Camera.main");
//                     cameraController = Camera.main.GetComponent<NoiseMovement>();
//                 }
                
//                 // Safety check - if we couldn't get camera controller, log and return
//                 if (cameraController == null)
//                 {
//                     Debug.LogWarning("[GameAdsManager] No camera controller found, cannot reset camera");
//                     return;
//                 }
                
//                 Debug.Log($"[GameAdsManager] Enabling camera controller: {cameraController.name}");
                
//                 // Re-enable camera controller if it exists
//                 if (cameraController != null)
//                 {
//                     cameraController.enabled = true;
//                 }
                
//                 // Only use the provided position if it's not zero (which is our default value)
//                 if (originalPosition != Vector3.zero)
//                 {
//                     Debug.Log($"[GameAdsManager] Resetting camera position to: {originalPosition}");
//                     if (cameraController != null && cameraController.transform != null)
//                     {
//                         cameraController.transform.position = originalPosition;
//                     }
//                 }
//                 else
//                 {
//                     Debug.LogWarning("[GameAdsManager] Original position was zero, not resetting position");
//                 }
                
//                 // Only use the provided rotation if it's not identity (which is our default value)
//                 if (originalRotation != Quaternion.identity)
//                 {
//                     Debug.Log($"[GameAdsManager] Resetting camera rotation to: {originalRotation.eulerAngles}");
//                     if (cameraController != null && cameraController.transform != null)
//                     {
//                         cameraController.transform.rotation = originalRotation;
//                     }
//                 }
//                 else 
//                 {
//                     Debug.LogWarning("[GameAdsManager] Original rotation was identity, not resetting rotation");
//                 }
//             }
//             catch (System.Exception e)
//             {
//                 Debug.LogError($"[GameAdsManager] Error resetting camera after ad: {e.Message}");
//             }
//         }

//         private void ResetPlayerAfterAd()
//         {
//             Debug.Log("[GameAdsManager] ResetPlayerAfterAd called");
            
//             try
//             {
//                 // Find the player controller
//                 PlayerController playerController = FindFirstObjectByType<PlayerController>();
//                 if (playerController != null)
//                 {
//                     Debug.Log("[GameAdsManager] Found player controller, enabling input");
                    
//                     // Enable the player controller itself
//                     playerController.enabled = true;
                    
//                     // Make sure player is visible
//                     if (playerController.GetComponentInChildren<Renderer>() != null)
//                     {
//                         foreach (var renderer in playerController.GetComponentsInChildren<Renderer>())
//                         {
//                             renderer.enabled = true;
//                         }
//                     }
                    
//                     // If player has a rigidbody, ensure it's not kinematic
//                     if (playerController.GetComponent<Rigidbody>() != null)
//                     {
//                         Rigidbody rb = playerController.GetComponent<Rigidbody>();
//                         if (rb.isKinematic)
//                         {
//                             Debug.Log("[GameAdsManager] Resetting player Rigidbody from kinematic state");
//                             rb.isKinematic = false;
//                         }
//                     }
//                 }
//                 else
//                 {
//                     Debug.LogWarning("[GameAdsManager] Player controller not found, cannot reset player");
//                 }
                
//                 // Find the Game Manager and reset any necessary state
//                 if (GameManager.Instance != null)
//                 {
//                     Debug.Log("[GameAdsManager] Setting game state to allow input");
                    
//                     // Make sure we use GameStates enum and not GameState
//                     if (GameManager.GamesState != GameStates.GameOver)
//                     {
//                         // Use RequestGameStateChange and not direct assignment
//                         // Don't try to set GameManager.GamesState directly
//                         GameManager.RequestGameStateChange(GameStates.Playing);
//                     }
//                 }
//             }
//             catch (System.Exception e)
//             {
//                 Debug.LogError($"[GameAdsManager] Error resetting player after ad: {e.Message}");
//             }
//         }

//         public void OnPlayerDeath(System.Action onAdClosed)
//         {
//             Debug.Log("[GameAdsManager] OnPlayerDeath called");
            
//             // Check if we're in cooldown period
//             if (adCooldownTime > 0)
//             {
//                 Debug.LogWarning($"[GameAdsManager] Still in ad cooldown period ({adCooldownTime:F1}s remaining). Skipping ad.");
//                 onAdClosed?.Invoke();
//                 return;
//             }
            
//             bool canShowAd = Time.time - lastAdTime >= minTimeBetweenAds;
            
//             Debug.Log($"[GameAdsManager] canShowAd: {canShowAd}, timeSinceLastAd: {Time.time - lastAdTime}, minTimeBetweenAds: {minTimeBetweenAds}");
            
//             // Check if we're already showing an ad - if so, just invoke callback and exit
//             if (isShowingAd)
//             {
//                 Debug.LogWarning("[GameAdsManager] Ad is already being shown! Skipping new ad request.");
//                 onAdClosed?.Invoke();
//                 return;
//             }
            
//             if (canShowAd && adsManager != null)
//             {
//                 try
//                 {
//                     // Set flag to indicate we're showing an ad
//                     isShowingAd = true;
                    
//                     // Try to show an ad using reflection
//                     var showAdMethod = adsManager.GetType().GetMethod("ShowInterstitialAd");
//                     if (showAdMethod != null)
//                     {
//                         Debug.Log("[GameAdsManager] Calling ShowInterstitialAd method");
//                         showAdMethod.Invoke(adsManager, null);
                        
//                         // Update last ad time ONLY if we actually show an ad
//                         lastAdTime = Time.time;
                        
//                         // Start the cooldown
//                         adCooldownTime = MIN_AD_COOLDOWN;
                        
//                         // Always start the callback coroutine to ensure the game continues
//                         // even if the ad is skipped or closed
//                         StartCoroutine(DelayedCallback(onAdClosed));
//                         return;
//                     }
//                 }
//                 catch (System.Exception e)
//                 {
//                     Debug.LogError("[GameAdsManager] Error showing ad: " + e.Message);
//                     isShowingAd = false;
//                 }
//             }
//             else
//             {
//                 Debug.Log($"[GameAdsManager] Skipping ad due to time constraint or missing adsManager. Time since last ad: {Time.time - lastAdTime}s");
//             }
            
//             // If we can't show an ad or there was an error, invoke the callback immediately
//             onAdClosed?.Invoke();
//         }
        
//         private IEnumerator DelayedCallback(System.Action callback)
//         {
//             Debug.Log("[GameAdsManager] DelayedCallback started, waiting for ad to complete or be skipped");
            
//             // Wait a short amount of time to ensure the ad has started
//             yield return new WaitForSeconds(0.5f);
            
//             bool adFinished = false;
//             float timeoutDuration = 30f; // Timeout after 30 seconds maximum
//             float startTime = Time.realtimeSinceStartup;
            
//             // Keep checking until the ad is finished or we time out
//             while (!adFinished && Time.realtimeSinceStartup - startTime < timeoutDuration)
//             {
//                 // Poll the ad initialization object for completion status
//                 if (adsInitializer != null)
//                 {
//                     try
//                     {
//                         // Check the flag properties on MobileMonetizationPro_UnityAdsInitializer
//                         var isAdCompletedProp = adsInitializer.GetType().GetField("IsAdCompleted");
//                         var isAdSkippedProp = adsInitializer.GetType().GetField("IsAdSkipped");
//                         var isAdUnknownProp = adsInitializer.GetType().GetField("IsAdUnknown");
                        
//                         if (isAdCompletedProp != null && isAdSkippedProp != null && isAdUnknownProp != null)
//                         {
//                             bool isCompleted = (bool)isAdCompletedProp.GetValue(adsInitializer);
//                             bool isSkipped = (bool)isAdSkippedProp.GetValue(adsInitializer);
//                             bool isUnknown = (bool)isAdUnknownProp.GetValue(adsInitializer);
                            
//                             // If any of the completion flags are true, the ad is finished
//                             if (isCompleted || isSkipped || isUnknown)
//                             {
//                                 Debug.Log($"[GameAdsManager] Ad completion detected: completed={isCompleted}, skipped={isSkipped}, unknown={isUnknown}");
//                                 adFinished = true;
//                                 break;
//                             }
//                         }
//                         else
//                         {
//                             Debug.LogWarning("[GameAdsManager] Could not find ad completion properties");
//                         }
//                     }
//                     catch (System.Exception e)
//                     {
//                         Debug.LogError($"[GameAdsManager] Error checking ad completion status: {e.Message}");
//                     }
//                 }
                
//                 // Wait a short time before checking again
//                 yield return new WaitForSeconds(0.5f);
//             }
            
//             // If we timed out, log it
//             if (!adFinished)
//             {
//                 Debug.LogWarning($"[GameAdsManager] Timed out waiting for ad completion after {timeoutDuration} seconds");
//             }
            
//             // No matter what happened, reset our state
//             isShowingAd = false;
            
//             // Reset camera after ad (since we can't know when the ad actually closed otherwise)
//             ResetCameraAfterAd(Camera.main?.GetComponent<NoiseMovement>(), Vector3.zero, Quaternion.identity);
            
//             // Reset player after ad
//             ResetPlayerAfterAd();
            
//             // Invoke the callback that was provided
//             Debug.Log("[GameAdsManager] Invoking callback after ad");
            
//             if (callback != null)
//             {
//                 callback.Invoke();
//             }
//             else
//             {
//                 Debug.LogError("[GameAdsManager] Callback is null in DelayedCallback!");
//             }
//         }

//         // Add a method to forcibly create an ads initializer if one doesn't exist
//         private bool RecreateAdsInitializerIfNeeded()
//         {
//             // First try to find it using our existing methods
//             if (EnsureAdsInitializer())
//             {
//                 Debug.Log("[GameAdsManager] Successfully found existing ads initializer");
//                 return true;
//             }
            
//             Debug.LogWarning("[GameAdsManager] Could not find ads initializer, attempting to recreate it");
            
//             // Try to find the prefab in Resources or directly in scene
//             GameObject prefab = null;
            
//             // Check if there's a prefab in Resources first
//             prefab = Resources.Load<GameObject>("MobileMonetizationPro_UnityAdsManager");
            
//             if (prefab == null)
//             {
//                 // If not in Resources, try to find it in the scene (might be disabled)
//                 var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
//                 foreach (var obj in allObjects)
//                 {
//                     if (obj.name.Contains("MobileMonetizationPro_UnityAdsManager") || 
//                         obj.name.Contains("MobileMonetizationPro_UnityAdsInitializer"))
//                     {
//                         prefab = obj;
//                         Debug.Log($"[GameAdsManager] Found ads manager/initializer in scene: {obj.name}");
//                         break;
//                     }
//                 }
//             }
            
//             if (prefab != null)
//             {
//                 // Instantiate the prefab
//                 GameObject adsObj = Instantiate(prefab);
//                 adsObj.name = "MobileMonetizationPro_UnityAdsInitializer";
//                 DontDestroyOnLoad(adsObj);
//                 Debug.Log("[GameAdsManager] Recreated ads initializer from prefab");
                
//                 // Find the initializer component
//                 adsInitializer = adsObj.GetComponent<MonoBehaviour>();
//                 if (adsInitializer == null)
//                 {
//                     // Try to find it in children
//                     var components = adsObj.GetComponentsInChildren<MonoBehaviour>();
//                     foreach (var comp in components)
//                     {
//                         if (comp.GetType().Name == ADS_INITIALIZER_TYPE_NAME)
//                         {
//                             adsInitializer = comp;
//                             Debug.Log($"[GameAdsManager] Found initializer component in child: {comp.name}");
//                             break;
//                         }
//                     }
//                 }
                
//                 return adsInitializer != null;
//             }
            
//             // If we can't find or create it, try one last resort - wait a frame and try to find it again
//             Debug.LogWarning("[GameAdsManager] Could not find prefab, will try to find initializer after a delay");
//             StartCoroutine(FindInitializerAfterDelay());
            
//             return false;
//         }

//         private IEnumerator FindInitializerAfterDelay()
//         {
//             // Wait a few frames to allow any initialization to complete
//             yield return new WaitForSeconds(0.5f);
            
//             adsInitializer = FindAdsInitializer();
//             if (adsInitializer != null)
//             {
//                 Debug.Log($"[GameAdsManager] Found ads initializer after delay: {adsInitializer.name}");
//             }
//             else
//             {
//                 Debug.LogError("[GameAdsManager] Failed to find ads initializer even after delay");
//             }
//         }

//         // Update the EnsureAdsInitializer to try the recreation method if needed
//         private bool EnsureAdsInitializer()
//         {
//             if (adsInitializer == null)
//             {
//                 Debug.Log("[GameAdsManager] adsInitializer is null, attempting to find it");
//                 adsInitializer = FindAdsInitializer();
//             }
            
//             // Return whether we have a valid initializer
//             return adsInitializer != null;
//         }

//         public void OnPlayerRestart(System.Action onAdClosed)
//         {
//             Debug.Log($"GameAdsManager: OnPlayerRestart called - GameState={GameManager.GamesState}, timeSinceLastAd={Time.realtimeSinceStartup - lastAdTime}s");
            
//             // Check if callback is null
//             if (onAdClosed == null)
//             {
//                 Debug.LogError("[GameAdsManager] OnPlayerRestart - onAdClosed callback is NULL!");
//                 return;
//             }
            
//             // Check if we're about to change scenes
//             if (GameManager.Instance != null && GameManager.Instance.IsChangingScenes)
//             {
//                 Debug.LogWarning("[GameAdsManager] Scene change in progress! Skipping ad.");
//                 onAdClosed?.Invoke();
//                 return;
//             }
            
//             // Check if we're in a cooldown period after showing an ad
//             if (adCooldownTime > 0)
//             {
//                 Debug.LogWarning($"[GameAdsManager] Still in ad cooldown period ({adCooldownTime:F1}s remaining). Skipping ad.");
//                 onAdClosed.Invoke();
//                 return;
//             }
            
//             // Check if we're already showing an ad - if so, just invoke callback and exit
//             if (isShowingAd)
//             {
//                 Debug.LogWarning("[GameAdsManager] Ad is already being shown! Skipping new ad request.");
//                 onAdClosed.Invoke();
//                 return;
//             }
            
//             // Check if enough time has passed since the last ad
//             if (Time.realtimeSinceStartup - lastAdTime > minTimeBetweenAds)
//             {
//                 Debug.Log($"[GameAdsManager] Time since last ad: {Time.realtimeSinceStartup - lastAdTime}s > {minTimeBetweenAds}s");
                
//                 // Try a more aggressive approach to ensure we have a valid ads initializer
//                 if (!RecreateAdsInitializerIfNeeded())
//                 {
//                     Debug.LogError("[GameAdsManager] Could not find or recreate ads initializer, skipping ad");
//                     onAdClosed.Invoke();
//                     return;
//                 }
                
//                 Debug.Log($"[GameAdsManager] adsInitializer is not null: {adsInitializer.name}");
//                 try
//                 {
//                     // First, try to load the interstitial ad to ensure it's ready
//                     var loadAdMethod = adsInitializer.GetType().GetMethod("LoadInterstitial");
//                     if (loadAdMethod != null)
//                     {
//                         Debug.Log("[GameAdsManager] LoadInterstitial method found, invoking");
//                         loadAdMethod.Invoke(adsInitializer, null);
                        
//                         // Wait a short time to ensure the ad is loaded
//                         Debug.Log("[GameAdsManager] Starting ShowAdAfterDelay coroutine");
//                         StartCoroutine(ShowAdAfterDelay(onAdClosed));
//                         return;
//                     }
//                     else
//                     {
//                         Debug.LogError("[GameAdsManager] LoadInterstitial method not found on adsInitializer");
//                     }
//                 }
//                 catch (System.Exception e)
//                 {
//                     Debug.LogError($"[GameAdsManager] Error loading ad: {e.Message}");
//                 }
//             }
//             else
//             {
//                 Debug.Log($"[GameAdsManager] Skipping ad - not enough time passed since last ad ({Time.realtimeSinceStartup - lastAdTime}s < {minTimeBetweenAds}s)");
//             }
            
//             // If we can't show an ad, invoke the callback immediately
//             Debug.Log("[GameAdsManager] Invoking callback immediately (no ad shown)");
//             onAdClosed.Invoke();
//         }
        
//         private IEnumerator ShowAdAfterDelay(System.Action onAdClosed)
//         {
//             Debug.Log("[GameAdsManager] ShowAdAfterDelay - Waiting a moment before showing ad");
            
//             // Safety check - implement ad cooldown to prevent multiple ad attempts
//             adCooldownTime = MIN_AD_COOLDOWN;
            
//             // Set flag to indicate we're showing an ad
//             isShowingAd = true;
            
//             // Add a delay to ensure the ad is properly loaded
//             yield return new WaitForSeconds(0.5f);
            
//             try
//             {
//                 // Check if the ads initializer is still valid
//                 if (adsInitializer == null)
//                 {
//                     Debug.LogError("[GameAdsManager] Ads initializer became null during delay!");
//                     isShowingAd = false;
//                     onAdClosed?.Invoke();
//                     yield break;
//                 }
                
//                 // Reset any previous ad completion flags
//                 try
//                 {
//                     var isAdCompletedField = adsInitializer.GetType().GetField("IsAdCompleted");
//                     var isAdSkippedField = adsInitializer.GetType().GetField("IsAdSkipped");
//                     var isAdUnknownField = adsInitializer.GetType().GetField("IsAdUnknown");
                    
//                     if (isAdCompletedField != null && isAdSkippedField != null && isAdUnknownField != null)
//                     {
//                         isAdCompletedField.SetValue(adsInitializer, false);
//                         isAdSkippedField.SetValue(adsInitializer, false);
//                         isAdUnknownField.SetValue(adsInitializer, false);
//                         Debug.Log("[GameAdsManager] Reset all ad completion flags");
//                     }
//                 }
//                 catch (System.Exception e)
//                 {
//                     Debug.LogWarning($"[GameAdsManager] Could not reset ad completion flags: {e.Message}");
//                 }
                
//                 // Store references to the camera and player for reset after the ad
//                 NoiseMovement cameraController = null;
//                 Vector3 originalCameraPosition = Vector3.zero;
//                 Quaternion originalCameraRotation = Quaternion.identity;
                
//                 if (Camera.main != null)
//                 {
//                     cameraController = Camera.main.GetComponent<NoiseMovement>();
//                     originalCameraPosition = Camera.main.transform.position;
//                     originalCameraRotation = Camera.main.transform.rotation;
                    
//                     // Disable camera movement during ad
//                     if (cameraController != null)
//                     {
//                         Debug.Log("[GameAdsManager] Disabling camera controller for ad");
//                         cameraController.enabled = false;
//                     }
//                 }
                
//                 // Check which ad to show based on game state
//                 bool adShown = false;
//                 MethodInfo showAdMethod = null;
                
//                 // Make sure to use GameStates instead of GameState
//                 if (GameManager.GamesState == GameStates.GameOver)
//                 {
//                     showAdMethod = adsInitializer.GetType().GetMethod("ShowRewardedAd");
//                     Debug.Log("[GameAdsManager] Using ShowRewardedAd for player death");
//                 }
//                 else
//                 {
//                     showAdMethod = adsInitializer.GetType().GetMethod("ShowInterstitialAd");
//                     Debug.Log("[GameAdsManager] Using ShowInterstitialAd for player restart");
//                 }
                
//                 if (showAdMethod != null)
//                 {
//                     try
//                     {
//                         Debug.Log($"[GameAdsManager] Showing ad via method: {showAdMethod.Name}");
//                         showAdMethod.Invoke(adsInitializer, null);
//                         lastAdTime = Time.realtimeSinceStartup;
//                         adShown = true;
//                     }
//                     catch (System.Exception e)
//                     {
//                         Debug.LogError($"[GameAdsManager] Error showing ad: {e.Message}");
//                         // If we fail to show the ad, reset everything and continue
//                         isShowingAd = false;
//                         if (cameraController != null) cameraController.enabled = true;
//                         onAdClosed?.Invoke();
//                         yield break;
//                     }
//                 }
//                 else
//                 {
//                     Debug.LogError("[GameAdsManager] Could not find appropriate ad method on initializer");
//                     isShowingAd = false;
//                     if (cameraController != null) cameraController.enabled = true;
//                     onAdClosed?.Invoke();
//                     yield break;
//                 }
                
//                 if (adShown)
//                 {
//                     // Start the delayed callback to wait for ad completion
//                     Debug.Log("[GameAdsManager] Ad shown successfully, starting DelayedCallback");
                    
//                     // Set originalCameraPosition and originalCameraRotation in class members for use in DelayedCallback
//                     if (cameraController != null)
//                     {
//                         // Note: We pass these values to DelayedCallback indirectly via ResetCameraAfterAd
//                         StartCoroutine(DelayedCallback(onAdClosed));
//                     }
//                     else
//                     {
//                         // If no camera controller, just use the callback directly
//                         StartCoroutine(DelayedCallback(onAdClosed));
//                     }
//                 }
//                 else
//                 {
//                     Debug.LogError("[GameAdsManager] Failed to show ad, continuing without ad");
//                     isShowingAd = false;
//                     if (cameraController != null) cameraController.enabled = true;
//                     onAdClosed?.Invoke();
//                 }
//             }
//             catch (System.Exception e)
//             {
//                 Debug.LogError($"[GameAdsManager] Error in ShowAdAfterDelay: {e.Message}");
//                 isShowingAd = false;
//                 onAdClosed?.Invoke();
//             }
//         }

//         public void StopAllAdCoroutines()
//         {
//             // Stop all ad-related coroutines to prevent callbacks after scene changes
//             StopAllCoroutines();
//             Debug.Log("[GameAdsManager] Stopped all ad coroutines");
            
//             // Reset flags
//             isShowingAd = false;
//             adCooldownTime = 0f;
//         }
//     }
// }