using System.Collections;
using System.Collections.Generic;
using com.unity3d.mediation;
using GoogleMobileAds.Ump.Api;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MobileMonetizationPro
{
    public class MobileMonetizationPro_LevelPlayInitializer : MonoBehaviour
    {
        public static MobileMonetizationPro_LevelPlayInitializer instance;

        public bool EnableGdprConsentMessage = true;

        [Header("App Key's")] 
        public string AndroidAppKey = "85460dcd";
        public string AndroidBannerAdUnitID = "thnfvcsog13bhn08";
        public string AndroidInterstitialAdUnitId = "aeyqi3vqlv6o8sh9";
        public string AndroidRewardedAdUnitId = "aeyqi3vqlv6o8sh9";

        public string iOSAppKey = "8545d445";
        public string iOSBannerAdUnitID = "iep3rxsyp9na3rw8";
        public string iOSInterstitialAdUnitId = "wmgt0712uuux8ju4";
        public string iOSRewardedAdUnitId = "aeyqi3vqlv6o8sh9";


        [Header("Ad Settings")]
        public bool ShowBannerAdsInStart = true;

        public enum BannerSize
        {
            BANNER,
            LARGE,
            RECTANGLE,
            SMART
        }

        public bool EnableTimedInterstitalAds = true;
        public int InterstitialAdIntervalSeconds = 10;
        public bool ResetInterstitalAdTimerOnRewardedAd = true;
        [HideInInspector]
        public bool CanShowAdsNow = false;
        [HideInInspector]
        public float Timer = 0;
        [HideInInspector]
        public bool IsBannerStartShowing = false;
        [HideInInspector]
        public bool IsAdsInitializationCompleted = false;
        private LevelPlayBannerAd bannerAd;

        [HideInInspector]
        public LevelPlayInterstitialAd interstitialAd;

        string AppKey;
        string BannerAdUnitID;
        string InterstitalAdUnit;

        [HideInInspector]
        public string RewardedAdUnit;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                // If an instance already exists, destroy this duplicate
                Destroy(gameObject);
            }

#if UNITY_ANDROID
            AppKey = AndroidAppKey;
            BannerAdUnitID = AndroidBannerAdUnitID;
            InterstitalAdUnit = AndroidInterstitialAdUnitId;
            RewardedAdUnit = AndroidRewardedAdUnitId;
#elif UNITY_IPHONE
            AppKey = iOSAppKey;
            BannerAdUnitID = iOSBannerAdUnitID;
            InterstitalAdUnit = iOSInterstitialAdUnitId;
            RewardedAdUnit = iOSRewardedAdUnitId;
#else
    string appKey = "unexpected_platform";
#endif


            //Dynamic config example
            IronSourceConfig.Instance.setClientSideCallbacks(true);


            IronSource.Agent.validateIntegration();

            // SDK init
            Debug.Log("unity-script: LevelPlay Init");
            // LevelPlay.Init(AppKey);        
          
            LevelPlay.Init(AppKey, "UserId", new[] { LevelPlayAdFormat.REWARDED });

            LevelPlay.OnInitSuccess += OnInitializationCompleted;
            LevelPlay.OnInitFailed += (error => Debug.Log("Initialization error: " + error));
        }
        void OnInitializationCompleted(LevelPlayConfiguration configuration)
        {
            Debug.Log("Initialization completed");
            LoadBanner();
            LoadInterstitial();
            LoadRewarded();
        }
        private void OnEnable()
        {

            if (EnableGdprConsentMessage == true)
            {
                // Create a ConsentRequestParameters object.
                ConsentRequestParameters request = new ConsentRequestParameters();

                // Check the current consent information status.
                ConsentInformation.Update(request, OnConsentInfoUpdated);
            }
            else
            {
                IronSourceEvents.onSdkInitializationCompletedEvent += SdkInitialized;
                LoadBanner();
                LoadInterstitial();
                LoadRewarded();
            }
        }
        private void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;

        }
        void OnConsentInfoUpdated(FormError consentError)
        {
            if (consentError != null)
            {
                // Handle the error.
                UnityEngine.Debug.LogError(consentError);
                return;
            }
            //if (ConsentInformation.IsConsentFormAvailable())
            //{
                // If the error is null, the consent information state was updated.
                // You are now ready to check if a form is available.
                ConsentForm.LoadAndShowConsentFormIfRequired((FormError formError) =>
               {
                if (formError != null)
                {
                    // Consent gathering failed.
                    UnityEngine.Debug.LogError(consentError);
                    return;
                }

                // Consent has been gathered.
                if (ConsentInformation.CanRequestAds())
                {
                    IronSourceEvents.onSdkInitializationCompletedEvent += SdkInitialized;
                    LoadBanner();
                    LoadInterstitial();
                    LoadRewarded();
                }
               });
            //}
            //else
            //{
            //    IronSourceEvents.onSdkInitializationCompletedEvent += SdkInitialized;
            //    LoadBanner();
            //    LoadInterstitial();
            //    LoadRewarded();
            //}

        }
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            //if (IsAdsInitializationCompleted == true)
            //{
                LoadInterstitial();
                LoadRewarded();
            //}
        }
        void SdkInitialized()
        {
            print("Sdk in initialized!!");
            IsAdsInitializationCompleted = true;
        }
        void OnApplicationPause(bool isPaused)
        {
            IronSource.Agent.onApplicationPause(isPaused);
        }

        public void LoadBanner()
        {
            if (PlayerPrefs.GetInt("AdsRemoved") == 0)
            {
                Debug.Log("Banner loaded");
                // Create object
                bannerAd = new LevelPlayBannerAd(BannerAdUnitID);

                bannerAd.OnAdLoaded += BannerOnAdLoadedEvent;
                bannerAd.OnAdLoadFailed += BannerOnAdLoadFailedEvent;
                bannerAd.OnAdDisplayed += BannerOnAdDisplayedEvent;
                bannerAd.OnAdDisplayFailed += BannerOnAdDisplayFailedEvent;
                bannerAd.OnAdClicked += BannerOnAdClickedEvent;
                bannerAd.OnAdCollapsed += BannerOnAdCollapsedEvent;
                bannerAd.OnAdLeftApplication += BannerOnAdLeftApplicationEvent;
                bannerAd.OnAdExpanded += BannerOnAdExpandedEvent;

                // Ad load
                bannerAd.LoadAd();
                IsBannerStartShowing = true;
            }

        }
        //Banner Events
        void BannerOnAdLoadedEvent(LevelPlayAdInfo adInfo)
        {
            Debug.Log("unity-script: I got BannerOnAdLoadedEvent With AdInfo " + adInfo);
        }

        void BannerOnAdLoadFailedEvent(LevelPlayAdError error)
        {
            Debug.Log("unity-script: I got BannerOnAdLoadFailedEvent With Error " + error);
        }

        void BannerOnAdClickedEvent(LevelPlayAdInfo adInfo)
        {
            Debug.Log("unity-script: I got BannerOnAdClickedEvent With AdInfo " + adInfo);
        }

        void BannerOnAdDisplayedEvent(LevelPlayAdInfo adInfo)
        {
            Debug.Log("unity-script: I got BannerOnAdDisplayedEvent With AdInfo " + adInfo);
        }

        void BannerOnAdDisplayFailedEvent(LevelPlayAdDisplayInfoError adInfoError)
        {
            Debug.Log("unity-script: I got BannerOnAdDisplayFailedEvent With AdInfoError " + adInfoError);
        }

        void BannerOnAdCollapsedEvent(LevelPlayAdInfo adInfo)
        {
            Debug.Log("unity-script: I got BannerOnAdCollapsedEvent With AdInfo " + adInfo);
        }

        void BannerOnAdLeftApplicationEvent(LevelPlayAdInfo adInfo)
        {
            Debug.Log("unity-script: I got BannerOnAdLeftApplicationEvent With AdInfo " + adInfo);
        }

        void BannerOnAdExpandedEvent(LevelPlayAdInfo adInfo)
        {
            Debug.Log("unity-script: I got BannerOnAdExpandedEvent With AdInfo " + adInfo);
        }

        //private void OnDestroy()
        //{
        //    bannerAd.DestroyAd();
        //    interstitialAd.DestroyAd();
        //}

        public void LoadInterstitial()
        {
            if (PlayerPrefs.GetInt("AdsRemoved") == 0)
            {
                // Create interstitial Ad
                interstitialAd = new LevelPlayInterstitialAd(InterstitalAdUnit);

                // Register to events
                interstitialAd.OnAdLoaded += InterstitialOnAdLoadedEvent;
                interstitialAd.OnAdLoadFailed += InterstitialOnAdLoadFailedEvent;
                interstitialAd.OnAdDisplayed += InterstitialOnAdDisplayedEvent;
                interstitialAd.OnAdDisplayFailed += InterstitialOnAdDisplayFailedEvent;
                interstitialAd.OnAdClicked += InterstitialOnAdClickedEvent;
                interstitialAd.OnAdClosed += InterstitialOnAdClosedEvent;
                interstitialAd.OnAdInfoChanged += InterstitialOnAdInfoChangedEvent;

                Debug.Log("unity-script: LoadInterstitialButtonClicked");
                interstitialAd.LoadAd();
            }
        }
        public void LoadRewarded()
        {
            Debug.Log("unity-script: LoadRewardedButtonClicked");
            IronSource.Agent.loadRewardedVideo();
        }
        private void Update()
        {
            if (PlayerPrefs.GetInt("AdsRemovedSuccessfully") == 0)
            {
                if (Timer >= InterstitialAdIntervalSeconds)
                {
                    Timer = 0;
                    CanShowAdsNow = true;
                }
                else
                {
                    if (EnableTimedInterstitalAds == true)
                    {
                        Timer += Time.deltaTime;
                        if (PlayerPrefs.GetInt("AdsRemoved") == 1)
                        {
                            if (PlayerPrefs.GetInt("AdsRemovedSuccessfully") == 0)
                            {
                                bannerAd.DestroyAd();
                                PlayerPrefs.SetInt("AdsRemovedSuccessfully", 1);
                            }
                        }
                    }
                }
            }
        }
        void InterstitialOnAdLoadedEvent(LevelPlayAdInfo adInfo)
        {
            Debug.Log("unity-script: I got InterstitialOnAdLoadedEvent With AdInfo " + adInfo);
        }

        void InterstitialOnAdLoadFailedEvent(LevelPlayAdError error)
        {
            Debug.Log("unity-script: I got InterstitialOnAdLoadFailedEvent With Error " + error);
        }

        void InterstitialOnAdDisplayedEvent(LevelPlayAdInfo adInfo)
        {
            Debug.Log("unity-script: I got InterstitialOnAdDisplayedEvent With AdInfo " + adInfo);
        }

        void InterstitialOnAdDisplayFailedEvent(LevelPlayAdDisplayInfoError infoError)
        {
            LoadInterstitial(); 
        }

        void InterstitialOnAdClickedEvent(LevelPlayAdInfo adInfo)
        {
            Debug.Log("unity-script: I got InterstitialOnAdClickedEvent With AdInfo " + adInfo);
        }

        void InterstitialOnAdClosedEvent(LevelPlayAdInfo adInfo)
        {
            if (EnableTimedInterstitalAds == true)
            {
                CanShowAdsNow = false;
                Timer = 0f;
            }
            LoadInterstitial();
        }
        void InterstitialOnAdInfoChangedEvent(LevelPlayAdInfo adInfo)
        {
            Debug.Log("unity-script: I got InterstitialOnAdInfoChangedEvent With AdInfo " + adInfo);
        }
    }
}