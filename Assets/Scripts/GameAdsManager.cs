using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

namespace RoofTops
{
    public class GameAdsManager : MonoBehaviour
    {
        private static GameAdsManager _instance;
        public static GameAdsManager Instance { get { return _instance; } }

        [Header("Ad Settings")]
        [SerializeField] private float minTimeBetweenAds = 30f;  // Minimum seconds between ads

        [Header("Skip Settings")]
#pragma warning disable 0414
        [SerializeField] private int maxAdSkips = 3;
#pragma warning restore 0414
        private int adSkipsAvailable = 0;
        public int AdSkipsAvailable => adSkipsAvailable;

        private MonoBehaviour adsManager;
        private float lastAdTime;

        private void Awake()
        {
            // Singleton pattern with DontDestroyOnLoad
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Find the ads manager component if available
            adsManager = FindAdsManager();
            
            // Initialize with a negative time to allow first ad immediately
            lastAdTime = -minTimeBetweenAds;
            
            // Subscribe to scene loaded events to reinitialize if needed
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from scene loaded events
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Reinitialize ads manager if needed
            if (adsManager == null)
            {
                adsManager = FindAdsManager();
            }
            
            Debug.Log($"GameAdsManager: Scene loaded - {scene.name}, Ads manager found: {adsManager != null}");
        }
        
        private MonoBehaviour FindAdsManager()
        {
            // Don't use tags since they might not be defined
            // Instead, try to find by type name (using reflection to avoid direct dependencies)
            foreach (var obj in FindObjectsOfType<MonoBehaviour>())
            {
                string typeName = obj.GetType().Name;
                if (typeName.Contains("AdsManager") || typeName.Contains("AdManager") || 
                    typeName.Contains("UnityAdsInitializer") || typeName.Contains("MobileMonetizationPro"))
                {
                    Debug.Log($"Found ads manager: {typeName}");
                    return obj;
                }
            }
            
            Debug.LogWarning("No ads manager found in the scene. Ad functionality will be limited.");
            return null;
        }

        public void OnPlayerDeath(System.Action onAdClosed)
        {
            // Show ad on every death, but still respect the minimum time between ads
            bool canShowAd = Time.time - lastAdTime >= minTimeBetweenAds;

            if (canShowAd && adsManager != null)
            {
                try
                {
                    var showMethod = adsManager?.GetType().GetMethod("ShowInterstitial");
                    if (showMethod != null)
                    {
                        showMethod.Invoke(adsManager, null);
                        lastAdTime = Time.time;
                        
                        // Give a small delay before invoking the callback to ensure ad has time to display
                        StartCoroutine(DelayedCallback(onAdClosed));
                        return;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Error showing ad: " + e.Message);
                }
            }
            
            // If we can't show an ad or there was an error, invoke the callback immediately
            onAdClosed?.Invoke();
        }
        
        private IEnumerator DelayedCallback(System.Action callback)
        {
            // Wait a short time to ensure ad has time to display
            yield return new WaitForSeconds(0.5f);
            callback?.Invoke();
        }
    }
}