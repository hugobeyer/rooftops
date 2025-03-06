using UnityEngine;
using UnityEngine.Advertisements;
using UnityEngine.SceneManagement;
using QuickMonetization;

public class MobileMonetizationPro_UnityAdsInitializer : MonoBehaviour, IUnityAdsInitializationListener,IUnityAdsLoadListener,IUnityAdsShowListener
{
    public static MobileMonetizationPro_UnityAdsInitializer instance;

    [HideInInspector]
    public string GameID;

    [Header("Game ID's")]
    public string AndroidGameID;
    public string IOSGameID;

    [Header("Placement ID's")]
    public string BannerAndroidAdUnitId = "Banner_Android";
    public string BannerIOSAdUnitId = "Banner_iOS";
    [HideInInspector]
    public string BannerAdUnitID = null; // This will remain null for unsupported platforms.

    public string InterstitalAndroidAdUnitId = "Interstitial_Android";
    public string InterstitalIOSAdUnitId = "Interstitial_iOS";
    [HideInInspector]
    public string InterstitalAdUnitID;

    public string RewardedAndroidAdUnitId = "Rewarded_Android";
    public string RewardedIOSAdUnitId = "Rewarded_iOS";
    [HideInInspector]
    public string RewardedAdUnitID = null; // This will remain null for unsupported platforms

    [Header("Ads Settings")]
    public bool EnableTestMode = true;
    public bool ShowBannerAdsInStart = true;
    public BannerPosition ChooseBannerPosition = BannerPosition.BOTTOM_CENTER;
    public bool EnableTimedInterstitalAds = true;
    public int InterstitialAdIntervalSeconds = 10;
    public bool ResetInterstitalAdTimerOnRewardedAd = true;

    [HideInInspector]
    public bool CanShowAdsNow = false;
    [HideInInspector]
    public float Timer = 0;
    [HideInInspector]
    public bool IsAdSkipped = false;
    [HideInInspector]
    public bool IsAdCompleted = false;
    [HideInInspector]
    public bool IsAdUnknown = false;

    MobileMonetizationPro_UnityAdsManager AdsManagerUAdsScript;

    [HideInInspector]
    public bool IsBannerStartShowing = false;

    [HideInInspector]
    public bool IsAdsInitializationCompleted = false;

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

        InitializeAds();
    }
    public void InitializeAds()
    {
#if UNITY_IOS
            GameID = IOSGameID;
#elif UNITY_ANDROID
        GameID = AndroidGameID;
#elif UNITY_EDITOR
            GameID = AndroidGameID; //Only for testing the functionality in the Editor
#endif

        // Get the Ad Unit ID for the current platform:
#if UNITY_IOS
        BannerAdUnitID = BannerIOSAdUnitId;
#elif UNITY_ANDROID
        BannerAdUnitID = BannerAndroidAdUnitId;
#endif

        // Get the Ad Unit ID for the current platform:
        InterstitalAdUnitID = (Application.platform == RuntimePlatform.IPhonePlayer)
            ? InterstitalIOSAdUnitId
            : InterstitalAndroidAdUnitId;

        // Get the Ad Unit ID for the current platform:
#if UNITY_IOS
        RewardedAdUnitID = RewardedIOSAdUnitId;
#elif UNITY_ANDROID
        RewardedAdUnitID = RewardedAndroidAdUnitId;
#endif


        if (!Advertisement.isInitialized && Advertisement.isSupported)
        {
            Advertisement.Initialize(GameID, EnableTestMode, this);
            IsAdsInitializationCompleted = true;
        }
        
    }
    private void Start()
    {
        if (ShowBannerAdsInStart == true)
        {
            LoadBanner();
            ShowBannerAd();
        }
        LoadInterstitial();
        LoadRewarded();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsAdsInitializationCompleted == true)
        {
            if (ShowBannerAdsInStart == true || IsBannerStartShowing == true)
            {
                LoadBanner();
                ShowBannerAd();
            }
            LoadInterstitial();
            LoadRewarded();
        }
    }
    public void LoadBanner()
    {
        if (PlayerPrefs.GetInt("AdsRemoved") == 0)
        {
            // Set the banner position:
            Advertisement.Banner.SetPosition(ChooseBannerPosition);

            // Set up options to notify the SDK of load events:
            BannerLoadOptions options = new BannerLoadOptions
            {
                loadCallback = OnBannerLoaded,
                errorCallback = OnBannerError
            };

            // Load the Ad Unit with banner content:
            Advertisement.Banner.Load(BannerAdUnitID, options);
            Debug.Log("Banner is loading");
        }

    }
    void OnBannerLoaded()
    {
        // Set up options to notify the SDK of show events:
        BannerOptions options = new BannerOptions
        {
            clickCallback = OnBannerClicked,
            hideCallback = OnBannerHidden,
            showCallback = OnBannerShown
        };

        // Show the loaded Banner Ad Unit:
        Advertisement.Banner.Show(BannerAdUnitID, options);
    }

    // Implement code to execute when the load errorCallback event triggers:
    void OnBannerError(string message) { }
    void OnBannerClicked() { }
    void OnBannerShown() { }
    void OnBannerHidden() { }

    public void LoadInterstitial()
    {
        if (PlayerPrefs.GetInt("AdsRemoved") == 0)
        {
            Advertisement.Load(InterstitalAdUnitID,this);
        }
    }
    public void LoadRewarded()
    {
        // Check for a valid RewardedAdUnitID before attempting to load
        if (string.IsNullOrEmpty(RewardedAdUnitID))
        {
            Debug.LogWarning("[MobileMonetizationPro_UnityAdsInitializer] RewardedAdUnitID is nil or empty. Skipping rewarded ad load.");
            return;
        }

        Advertisement.Load(RewardedAdUnitID, this);
    }
    public void ShowBannerAd()
    {
        if (PlayerPrefs.GetInt("AdsRemoved") == 0)
        {
            // Set the banner position:
            Advertisement.Banner.SetPosition(ChooseBannerPosition);
            // Load the Ad Unit with banner content:
            Advertisement.Banner.Load(BannerAdUnitID);
            // Show the loaded Banner Ad Unit:
            Advertisement.Banner.Show(BannerAdUnitID);
            Debug.Log("Banner is Showing");
            IsBannerStartShowing = true;
        }
    }
    public void ShowInterstitialAd()
    {
        if (PlayerPrefs.GetInt("AdsRemoved") == 0)
        {
            LoadInterstitial();
            Advertisement.Show(InterstitalAdUnitID,this);
        }
    }
    public void ShowRewardedAd()
    {
        LoadRewarded();
        Advertisement.Show(RewardedAdUnitID,this);
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
                            Advertisement.Banner.Hide(true);
                            PlayerPrefs.SetInt("AdsRemovedSuccessfully", 1);
                        }
                    }
                }
            }
        }
    }
    public void OnInitializationComplete()
    {

    }
    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {

    }

    public void OnUnityAdsAdLoaded(string placementId)
    {
         
    }

    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
         
    }

    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    {
       
    }

    public void OnUnityAdsShowStart(string placementId)
    {
        
    }

    public void OnUnityAdsShowClick(string placementId)
    {
         
    }

    public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
    {
        IsAdCompleted = false;
        IsAdSkipped = false;
        IsAdUnknown = false;

        if (placementId.Equals(RewardedAdUnitID) && showCompletionState.Equals(UnityAdsShowCompletionState.COMPLETED))
        {
            IsAdCompleted = true;
        }
        else if (placementId.Equals(InterstitalAdUnitID) && showCompletionState.Equals(UnityAdsShowCompletionState.SKIPPED))
        {
            IsAdSkipped = true;
        }
        else if (placementId.Equals(InterstitalAdUnitID) && showCompletionState.Equals(UnityAdsShowCompletionState.UNKNOWN))
        {
            IsAdUnknown = true;
        }

        if (AdsManagerUAdsScript == null)
        {
            if(FindObjectOfType<MobileMonetizationPro_UnityAdsManager>() != null)
            {
                AdsManagerUAdsScript = FindObjectOfType<MobileMonetizationPro_UnityAdsManager>();
            }
            if(AdsManagerUAdsScript != null)
            {
                AdsManagerUAdsScript.CheckForAdCompletion();
            }
        }
        else
        {
            AdsManagerUAdsScript.CheckForAdCompletion();
        }
    }

}