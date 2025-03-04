using UnityEngine;

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
                // Debug.Log("Found Unity Ads Manager");
            }

            lastAdTime = -minTimeBetweenAds; // Allow first ad immediately
        }

        public void OnPlayerDeath(System.Action onAdClosed)
        {
            // Show ad on every death, but still respect the minimum time between ads
            bool canShowAd = Time.time - lastAdTime >= minTimeBetweenAds;

            if (canShowAd)
            {
                var showMethod = adsManager?.GetType().GetMethod("ShowInterstitial");
                showMethod?.Invoke(adsManager, null);
                lastAdTime = Time.time;
            }

            onAdClosed?.Invoke();
        }
    }
}