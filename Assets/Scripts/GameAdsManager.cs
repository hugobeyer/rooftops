using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Reflection;

namespace RoofTops
{
    public class GameAdsManager : MonoBehaviour
    {
        private static GameAdsManager _instance;
        public static GameAdsManager Instance { get { return _instance; } }

        [Header("Ad Settings")]
        [SerializeField] private float minTimeBetweenAds = 3f;  // Reduced from 30f to 3f for testing

        [Header("Skip Settings")]
#pragma warning disable 0414
        [SerializeField] private int maxAdSkips = 3;
#pragma warning restore 0414
        private int adSkipsAvailable = 0;
        public int AdSkipsAvailable => adSkipsAvailable;

        private MonoBehaviour adsManager;
        private float lastAdTime;

        // Change to MonoBehaviour to avoid direct dependency
        public MonoBehaviour adsInitializer;

        private void Awake()
        {
            // Singleton pattern with DontDestroyOnLoad
            if (_instance != null && _instance != this)
            {
                Debug.Log("[GameAdsManager] Instance already exists, destroying duplicate");
                Destroy(gameObject);
                return;
            }
            
            Debug.Log("[GameAdsManager] Initializing singleton instance");
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Find the ads manager component if available
            adsManager = FindAdsManager();
            Debug.Log($"[GameAdsManager] adsManager initialized: {(adsManager != null ? "success" : "failed")}");
            
            // Register for scene loaded events
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            // Initialize ad skips
            adSkipsAvailable = maxAdSkips;
        }

        private void OnDestroy()
        {
            // Unregister from scene loaded events
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[GameAdsManager] Scene loaded: {scene.name}, checking adsManager");
            
            // Refresh ads manager reference when a new scene is loaded
            if (adsManager == null)
            {
                Debug.Log("[GameAdsManager] adsManager is null, attempting to find it again");
                adsManager = FindAdsManager();
                Debug.Log($"[GameAdsManager] adsManager after search: {(adsManager != null ? "found" : "still null")}");
            }
            else
            {
                Debug.Log("[GameAdsManager] adsManager already exists");
            }
        }

        private MonoBehaviour FindAdsManager()
        {
            Debug.Log("[GameAdsManager] Searching for MobileMonetizationPro_UnityAdsInitializer");
            
            // Search for the type 'MobileMonetizationPro_UnityAdsInitializer' in all loaded assemblies
            System.Type adsType = null;
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                adsType = assembly.GetType("MobileMonetizationPro_UnityAdsInitializer");
                if (adsType != null)
                {
                    Debug.Log($"[GameAdsManager] Found type in assembly: {assembly.FullName}");
                    break;
                }
            }

            if (adsType == null)
            {
                Debug.LogWarning("[GameAdsManager] MobileMonetizationPro_UnityAdsInitializer not found in any loaded assemblies. Skipping ads manager setup.");
                return null;
            }

            var adsInitializerInstance = GameObject.FindObjectOfType(adsType);
            if (adsInitializerInstance == null)
            {
                Debug.LogWarning("[GameAdsManager] MobileMonetizationPro_UnityAdsInitializer instance not found in scene. Skipping ads manager setup.");
                return null;
            }

            Debug.Log($"[GameAdsManager] Found instance of {adsType.Name}");
            return adsInitializerInstance as MonoBehaviour;
        }

        public void OnPlayerDeath(System.Action onAdClosed)
        {
            Debug.Log("[GameAdsManager] OnPlayerDeath called");
            bool canShowAd = Time.time - lastAdTime >= minTimeBetweenAds;
            
            Debug.Log($"[GameAdsManager] canShowAd: {canShowAd}, timeSinceLastAd: {Time.time - lastAdTime}, minTimeBetweenAds: {minTimeBetweenAds}");
            
            if (canShowAd && adsManager != null)
            {
                try
                {
                    // Try to show an ad using reflection
                    var showAdMethod = adsManager.GetType().GetMethod("ShowInterstitialAd");
                    if (showAdMethod != null)
                    {
                        Debug.Log("[GameAdsManager] Calling ShowInterstitialAd method");
                        showAdMethod.Invoke(adsManager, null);
                        
                        // Update last ad time ONLY if we actually show an ad
                        lastAdTime = Time.time;
                        
                        // Always start the callback coroutine to ensure the game continues
                        // even if the ad is skipped or closed
                        StartCoroutine(DelayedCallback(onAdClosed));
                        return;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError("[GameAdsManager] Error showing ad: " + e.Message);
                }
            }
            else
            {
                Debug.Log($"[GameAdsManager] Skipping ad due to time constraint or missing adsManager. Time since last ad: {Time.time - lastAdTime}s");
            }
            
            // If we can't show an ad or there was an error, invoke the callback immediately
            onAdClosed?.Invoke();
        }
        
        private IEnumerator DelayedCallback(System.Action callback)
        {
            Debug.Log("[GameAdsManager] DelayedCallback started, waiting for ad to complete or be skipped");
            
            // Force a minimum viewing time for the ad (simulating the normal ad experience)
            // This ensures users can't immediately skip the ad
            float minimumAdViewTime = 5.0f; // 5 seconds minimum viewing time
            Debug.Log($"[GameAdsManager] Enforcing minimum ad view time of {minimumAdViewTime} seconds");
            yield return new WaitForSeconds(minimumAdViewTime);
            
            Debug.Log("[GameAdsManager] Minimum ad view time completed, invoking callback");
            callback?.Invoke();
        }

        // New method to show interstitial ad on player restart
        public void OnPlayerRestart(System.Action onAdClosed)
        {
            Debug.Log("[GameAdsManager] OnPlayerRestart called - Showing ad");
            
            // Check if enough time has passed since the last ad
            if (Time.realtimeSinceStartup - lastAdTime > minTimeBetweenAds)
            {
                if (adsInitializer != null)
                {
                    try
                    {
                        // Try to show an interstitial ad using reflection to avoid direct dependency
                        var showAdMethod = adsInitializer.GetType().GetMethod("ShowInterstitialAd");
                        if (showAdMethod != null)
                        {
                            showAdMethod.Invoke(adsInitializer, null);
                            lastAdTime = Time.realtimeSinceStartup;
                            
                            // Start a coroutine for delayed callback
                            StartCoroutine(DelayedCallback(onAdClosed));
                            return;
                        }
                        else
                        {
                            Debug.LogError("[GameAdsManager] ShowInterstitialAd method not found on adsInitializer");
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[GameAdsManager] Error showing ad: {e.Message}");
                    }
                }
            }
            else
            {
                Debug.Log($"[GameAdsManager] Skipping ad - not enough time passed since last ad ({Time.realtimeSinceStartup - lastAdTime} < {minTimeBetweenAds})");
            }
            
            // If we can't show an ad, invoke the callback immediately
            onAdClosed?.Invoke();
        }
    }
}