using UnityEngine;

namespace RoofTops
{
    public class GameAdsManager : MonoBehaviour
    {
        private static GameAdsManager _instance;
        public static GameAdsManager Instance { get { return _instance; } }

        [Header("Ad Settings")]
        [SerializeField] private int showAdEveryXDeaths = 2;
        [SerializeField] private float minTimeBetweenAds = 30f;  // Minimum seconds between ads
        
        [Header("Skip Settings")]
        [SerializeField] private int maxAdSkips = 3;
        private int adSkipsAvailable = 0;
        public int AdSkipsAvailable => adSkipsAvailable;

        private MonoBehaviour adsManager;
        private float lastAdTime;
        private int deathCount = 0;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Get the ads manager
            adsManager = FindFirstObjectByType<MonoBehaviour>();
            if (adsManager != null && adsManager.GetType().Name.Contains("UnityAdsManager"))
            {
                Debug.Log("Found Unity Ads Manager");
            }

            lastAdTime = -minTimeBetweenAds; // Allow first ad immediately
        }

        public void OnPlayerDeath(System.Action onAdClosed)
        {
            deathCount++;
            bool canShowAd = Time.time - lastAdTime >= minTimeBetweenAds;
            
            if (deathCount >= showAdEveryXDeaths && canShowAd)
            {
                deathCount = 0;
                var showMethod = adsManager?.GetType().GetMethod("ShowInterstitial");
                showMethod?.Invoke(adsManager, null);
                lastAdTime = Time.time;
            }
            
            onAdClosed?.Invoke();
        }
    }
} 