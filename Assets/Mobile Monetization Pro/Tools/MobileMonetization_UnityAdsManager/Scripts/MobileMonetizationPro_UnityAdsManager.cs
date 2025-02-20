using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Reflection;
using UnityEngine.Advertisements;

namespace QuickMonetization
{
    public class MobileMonetizationPro_UnityAdsManager : MonoBehaviour
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
                if (MobileMonetizationPro_UnityAdsInitializer.instance != null)
                {
                    if (MobileMonetizationPro_UnityAdsInitializer.instance.ShowBannerAdsInStart == false)
                    {
                        if (ShowBannerAdButton != null)
                        {
                            ShowBannerAdButton.onClick.AddListener(() =>
                            {
                                MobileMonetizationPro_UnityAdsInitializer.instance.LoadBanner();
                            });
                        }

                    }
                }


                for (int i = 0; i < ActionButtonsToInvokeInterstitalAds.Length; i++)
                {
                    if (ActionButtonsToInvokeInterstitalAds[i] != null)
                    {
                        ActionButtonsToInvokeInterstitalAds[i].onClick.AddListener(() =>
                        {
                            // Call a function when the button is clicked
                            ShowInterstitial();
                        });

                    }

                }

                if (MobileMonetizationPro_UnityAdsInitializer.instance != null)
                {
                    MobileMonetizationPro_UnityAdsInitializer.instance.LoadInterstitial();
                    MobileMonetizationPro_UnityAdsInitializer.instance.LoadRewarded();
                }
            }

            List<Button> rewardedButtons = GetRewardedButtons();

            // Now you can work with the `rewardedButtons` list
            foreach (Button rewardedButton in rewardedButtons)
            {
                // Do something with each rewarded button
                // For example, you can add a click listener
                if (rewardedButton != null)
                {
                    rewardedButton.onClick.AddListener(() => ShowRewarded(rewardedButton));
                }

            }
        }
        public void ShowInterstitial()
        {

            if (PlayerPrefs.GetInt("AdsRemoved") == 0)
            {
                if (MobileMonetizationPro_UnityAdsInitializer.instance != null)
                {
                    if (MobileMonetizationPro_UnityAdsInitializer.instance.EnableTimedInterstitalAds == false)
                    {
                        MobileMonetizationPro_UnityAdsInitializer.instance.ShowInterstitialAd();
                    }
                    else
                    {

                        if (MobileMonetizationPro_UnityAdsInitializer.instance.CanShowAdsNow == true)
                        {
                            MobileMonetizationPro_UnityAdsInitializer.instance.ShowInterstitialAd();
                        }
                    }
                }
            }
        }
       
        public void ShowRewarded(Button clickedButton)
        {
            if (MobileMonetizationPro_UnityAdsInitializer.instance != null)
            {
                functionInfo = functions.Find(info => info.RewardedButton == clickedButton);
                MobileMonetizationPro_UnityAdsInitializer.instance.ShowRewardedAd();
            }
        }
        public void ResetAndReloadFullAds()
        {
            if (MobileMonetizationPro_UnityAdsInitializer.instance != null)
            {
                if (MobileMonetizationPro_UnityAdsInitializer.instance.ResetInterstitalAdTimerOnRewardedAd == true)
                {
                    MobileMonetizationPro_UnityAdsInitializer.instance.CanShowAdsNow = false;
                    MobileMonetizationPro_UnityAdsInitializer.instance.Timer = 0f;
                }

                if (MobileMonetizationPro_UnityAdsInitializer.instance.EnableTimedInterstitalAds == true)
                {
                    MobileMonetizationPro_UnityAdsInitializer.instance.CanShowAdsNow = false;
                    MobileMonetizationPro_UnityAdsInitializer.instance.Timer = 0f;
                }
                MobileMonetizationPro_UnityAdsInitializer.instance.IsAdCompleted = false;
                MobileMonetizationPro_UnityAdsInitializer.instance.IsAdSkipped = false;
                MobileMonetizationPro_UnityAdsInitializer.instance.IsAdUnknown = false;

                MobileMonetizationPro_UnityAdsInitializer.instance.LoadInterstitial();
                MobileMonetizationPro_UnityAdsInitializer.instance.LoadRewarded();
            }
        }
        //// Implement the Show Listener's OnUnityAdsShowComplete callback method to determine if the user gets a reward:
        //public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState showCompletionState)
        //{
        //    if (adUnitId.Equals(AdsInitializerUAds.instance.RewardedAdUnitID) && showCompletionState.Equals(UnityAdsShowCompletionState.COMPLETED))
        //    {
        //        ResetAndReloadFullAds();
        //        if (AdsInitializerUAds.instance != null)
        //        {
        //            OnButtonClick();
        //        }
        //    }
        //    else if (adUnitId.Equals(AdsInitializerUAds.instance.InterstitalAdUnitID) && showCompletionState.Equals(UnityAdsShowCompletionState.SKIPPED))
        //    {
        //        ResetAndReloadFullAds();
        //    }
        //    else if (adUnitId.Equals(AdsInitializerUAds.instance.InterstitalAdUnitID) && showCompletionState.Equals(UnityAdsShowCompletionState.UNKNOWN))
        //    {
        //        ResetAndReloadFullAds();
        //    }

        //}
        public void CheckForAdCompletion()
        {
            if (MobileMonetizationPro_UnityAdsInitializer.instance != null)
            {
                if (MobileMonetizationPro_UnityAdsInitializer.instance.IsAdCompleted == true)
                {
                    ResetAndReloadFullAds();
                    if (MobileMonetizationPro_UnityAdsInitializer.instance != null)
                    {
                        OnButtonClick();
                    }
                }
                else if (MobileMonetizationPro_UnityAdsInitializer.instance.IsAdSkipped == true)
                {
                    ResetAndReloadFullAds();
                }
                else if (MobileMonetizationPro_UnityAdsInitializer.instance.IsAdUnknown == true)
                {
                    ResetAndReloadFullAds();
                }
            }
        }
    }
}