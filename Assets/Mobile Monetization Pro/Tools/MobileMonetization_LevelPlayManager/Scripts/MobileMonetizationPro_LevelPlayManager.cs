using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Reflection;


namespace MobileMonetizationPro
{
    public class MobileMonetizationPro_LevelPlayManager : MonoBehaviour
    {
        public Button ShowBannerAdButton;

        [Serializable]
        public class FunctionInfo
        {
            public Button RewardedButton;
            public MonoBehaviour script;
            public string scriptName;
            public List<string> functionNames;
            public int selectedFunctionIndex;      
        }

        public Button[] ActionButtonsToInvokeInterstitalAds;

        public List<Button> rewardedButtons = new List<Button>();

        public List<FunctionInfo> functions = new List<FunctionInfo>();

        FunctionInfo functionInfo;

      
        private void OnValidate()
        {
            foreach (var function in functions)
            {
                function.functionNames = GetFunctionNames(function.script);
            }
        }
        public List<Button> GetRewardedButtons()
        {
            foreach (var functionInfo in functions)
            {
                rewardedButtons.Add(functionInfo.RewardedButton);
            }
            return rewardedButtons;
        }
        public void OnButtonClick()
        {
            if (functionInfo != null)
            {
                // Call the selected function when the button is clicked
                string selectedFunctionName = functionInfo.functionNames[functionInfo.selectedFunctionIndex];
                MethodInfo method = functionInfo.script.GetType().GetMethod(selectedFunctionName);
                if (method != null)
                {
                    method.Invoke(functionInfo.script, null);
                    functionInfo = null;
                }
            }
        }
        private List<string> GetFunctionNames(MonoBehaviour script)
        {
            List<string> functionNames = new List<string>();
            if (script != null)
            {
                Type type = script.GetType();
                MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
                foreach (MethodInfo method in methods)
                {
                    functionNames.Add(method.Name);
                }
            }
            return functionNames;
        }
        private void Start()
        {
            if (PlayerPrefs.GetInt("AdsRemoved") == 0)
            {
                if(MobileMonetizationPro_LevelPlayInitializer.instance != null)
                {
                    if (MobileMonetizationPro_LevelPlayInitializer.instance.ShowBannerAdsInStart == false)
                    {
                        if(ShowBannerAdButton != null)
                        {
                            ShowBannerAdButton.onClick.AddListener(() =>
                            {
                                MobileMonetizationPro_LevelPlayInitializer.instance.LoadBanner();
                            });
                        }
                      
                    }
                }
               

                for (int i = 0; i < ActionButtonsToInvokeInterstitalAds.Length; i++)
                {
                    if(ActionButtonsToInvokeInterstitalAds[i] != null)
                    {
                        ActionButtonsToInvokeInterstitalAds[i].onClick.AddListener(() =>
                        {
                            // Call a function when the button is clicked
                            ShowInterstitial();
                        });

                    }
                  
                }
            }

            List<Button> rewardedButtons = GetRewardedButtons();

            // Now you can work with the `rewardedButtons` list
            foreach (Button rewardedButton in rewardedButtons)
            {
                // Do something with each rewarded button
                // For example, you can add a click listener
                if(rewardedButton != null)
                {
                    rewardedButton.onClick.AddListener(() => ShowRewarded(rewardedButton));
                }
              
            }
        }
        private void OnEnable()
        {
            //Add AdInfo Rewarded Video Events
            // Add Rewarded Video Events
            IronSourceRewardedVideoEvents.onAdOpenedEvent += RewardedVideoOnAdOpenedEvent;
            IronSourceRewardedVideoEvents.onAdClosedEvent += RewardedVideoOnAdClosedEvent;
            IronSourceRewardedVideoEvents.onAdAvailableEvent += RewardedVideoOnAdAvailable;
            IronSourceRewardedVideoEvents.onAdUnavailableEvent += RewardedVideoOnAdUnavailable;
            IronSourceRewardedVideoEvents.onAdShowFailedEvent += RewardedVideoOnAdShowFailedEvent;
            IronSourceRewardedVideoEvents.onAdRewardedEvent += RewardedVideoOnAdRewardedEvent;
            IronSourceRewardedVideoEvents.onAdClickedEvent += RewardedVideoOnAdClickedEvent;


        }

       
        public void ShowInterstitial()
        {
            if (PlayerPrefs.GetInt("AdsRemoved") == 0)
            {
                if (MobileMonetizationPro_LevelPlayInitializer.instance != null)
                {
                    if (MobileMonetizationPro_LevelPlayInitializer.instance.EnableTimedInterstitalAds == false)
                    {
                        if (MobileMonetizationPro_LevelPlayInitializer.instance.interstitialAd.IsAdReady())
                        {
                            MobileMonetizationPro_LevelPlayInitializer.instance.interstitialAd.ShowAd();
                        }
                        else
                        {
                            MobileMonetizationPro_LevelPlayInitializer.instance.LoadInterstitial();
                        }
                    }
                    else
                    {

                        if (MobileMonetizationPro_LevelPlayInitializer.instance.CanShowAdsNow == true)
                        {
                            if (MobileMonetizationPro_LevelPlayInitializer.instance.interstitialAd.IsAdReady())
                            {
                                MobileMonetizationPro_LevelPlayInitializer.instance.interstitialAd.ShowAd();
                            }
                            else
                            {
                                MobileMonetizationPro_LevelPlayInitializer.instance.LoadInterstitial();
                            }
                        }
                    }
                }
            }
        }
        public void ShowRewarded(Button clickedButton)
        {
            if (IronSource.Agent.isRewardedVideoAvailable())
            {
                functionInfo = functions.Find(info => info.RewardedButton == clickedButton);
                IronSource.Agent.showRewardedVideo(MobileMonetizationPro_LevelPlayInitializer.instance.RewardedAdUnit);
            }
            else
            {
                MobileMonetizationPro_LevelPlayInitializer.instance.LoadRewarded();
            }
        }
        /************* RewardedVideo API *************/
        public void ShowRewardedVideoButtonClicked()
        {
            Debug.Log("unity-script: ShowRewardedVideoButtonClicked");
            if (IronSource.Agent.isRewardedVideoAvailable())
            {
                IronSource.Agent.showRewardedVideo();
            }
            else
            {
                Debug.Log("unity-script: IronSource.Agent.isRewardedVideoAvailable - False");
            }
        }

        void RewardedVideoOnAdOpenedEvent(IronSourceAdInfo adInfo)
        {
            Debug.Log("unity-script: I got RewardedVideoOnAdOpenedEvent With AdInfo " + adInfo);
        }

        void RewardedVideoOnAdClosedEvent(IronSourceAdInfo adInfo)
        {
            if (MobileMonetizationPro_LevelPlayInitializer.instance != null)
            {
                if (MobileMonetizationPro_LevelPlayInitializer.instance.ResetInterstitalAdTimerOnRewardedAd == true)
                {
                    MobileMonetizationPro_LevelPlayInitializer.instance.CanShowAdsNow = false;
                    MobileMonetizationPro_LevelPlayInitializer.instance.Timer = 0f;
                }
                MobileMonetizationPro_LevelPlayInitializer.instance.LoadRewarded();
            }
        }

        void RewardedVideoOnAdAvailable(IronSourceAdInfo adInfo)
        {
            Debug.Log("unity-script: I got RewardedVideoOnAdAvailable With AdInfo " + adInfo);
        }

        void RewardedVideoOnAdUnavailable()
        {
            Debug.Log("unity-script: I got RewardedVideoOnAdUnavailable");
        }

        void RewardedVideoOnAdShowFailedEvent(IronSourceError ironSourceError, IronSourceAdInfo adInfo)
        {
            Debug.Log("unity-script: I got RewardedVideoAdOpenedEvent With Error" + ironSourceError + "And AdInfo " + adInfo);
        }

        void RewardedVideoOnAdRewardedEvent(IronSourcePlacement ironSourcePlacement, IronSourceAdInfo adInfo)
        {
            Debug.Log("unity-script: I got RewardedVideoOnAdRewardedEvent With Placement" + ironSourcePlacement + "And AdInfo " + adInfo);
            OnButtonClick();
        }

        void RewardedVideoOnAdClickedEvent(IronSourcePlacement ironSourcePlacement, IronSourceAdInfo adInfo)
        {
            Debug.Log("unity-script: I got RewardedVideoOnAdClickedEvent With Placement" + ironSourcePlacement + "And AdInfo " + adInfo);
        }



    }
}