using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MobileMonetizationPro
{
    public class IntegrationWizard : EditorWindow
    {

        private int selectedIntegration = 0;
        private int selectedAdNetwork = 0;
        private string[] integrationOptions = new string[] { "Mobile Ads", "In App Purchase", "Firebase Analytics", "Mobile Notifications", "App Tracking Transparency Popup",
                                                            "User Ratings Popup","Cross Promotion" };
        private string[] adNetworkOptions = new string[] { "Admob", "Unity Ads", "LevelPlay", "Applovin", "LiftOff(Vungle)" };
        private string[] adNetworkUrls = new string[] { "https://developers.google.com/admob/unity/quick-start", "https://unityads.unity3d.com/help/unity/integration-guide-unity", "https://developers.is.com/ironsource-mobile/unity/unity-plugin/#step-1",
            "https://dash.applovin.com/documentation/mediation/unity/getting-started/integration","https://support.vungle.com/hc/en-us/articles/360003455452-Integrate-Vungle-SDK-for-Unity" };

        [MenuItem("Tools/Mobile Monetization Pro/Integration Tool", false, 0)]
        public static void OpenGettingStartedWindow()
        {
            IntegrationWizard window = GetWindow<IntegrationWizard>(true, "Mobile Monetization Pro", true);
            window.ShowUtility();
        }

        private void OnGUI()
        {
            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 25,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            GUILayout.Label("Integration Tool", labelStyle);

            selectedIntegration = EditorGUILayout.Popup("Choose Integration", selectedIntegration, integrationOptions);

            switch (selectedIntegration)
            {
                case 0: // Mobile Ads
                    DrawMobileAdsIntegration();
                    break;
                case 1: // In App Purchase
                    DrawInAppPurchaseIntegration();
                    break;
                case 2: // Firebase Analytics
                    DrawFirebaseAnalyticsIntegration();
                    break;
                case 3: // Mobile Notifications
                    DrawMobileNotificationsIntegration();
                    break;
                //case 4: // In App Update
                //    DrawInAppUpdatePackageImportButton();
                //    break;
                case 4: // App Tracking Transparency Popup
                    DrawATTIntegration();
                    break;
                case 5: // App Tracking Transparency Popup
                    DrawUserRatingPopupIntegration();
                    break;
                case 6: // App Tracking Transparency Popup
                    DrawCrossPromotionIntegration();
                    break;
            }

            GUILayout.Space(10);
        }
        private Dictionary<string, string> adNetworkUnityPackages = new Dictionary<string, string>()
     {
    { "Admob", "Assets/Mobile Monetization Pro/Unity Packages/MobileMonetizationPro_AdmobAdsImplementation.unitypackage" },
    { "Unity Ads", "Assets/Mobile Monetization Pro/Unity Packages/MobileMonetizationPro_UnityAdsImplementation.unitypackage" },
    { "LevelPlay", "Assets/Mobile Monetization Pro/Unity Packages/MobileMonetizationPro_LevelPlayAdsImplementation.unitypackage" },
    { "Applovin", "Assets/Mobile Monetization Pro/Unity Packages/MobileMonetizationPro_ApplovinAdsImplementation.unitypackage" },
    { "LiftOff(Vungle)", "Assets/Mobile Monetization Pro/Unity Packages/MobileMonetizationPro_LiftOffAdsImplementation.unitypackage" }
    };
        private Dictionary<string, string> TutorialLinks = new Dictionary<string, string>()
     {
    { "Admob", "google.com" },
    { "Unity Ads", "google.com" },
    { "LevelPlay", "google.com" },
    { "Applovin", "google.com" },
    { "LiftOff(Vungle)", "google.com" }
    };

        private void DrawMobileAdsIntegration()
        {
            selectedAdNetwork = EditorGUILayout.Popup("Choose AdNetwork", selectedAdNetwork, adNetworkOptions);

            GUILayout.Label("Step 1: Download" + " " + adNetworkOptions[selectedAdNetwork] + " " + "SDK");

            if (selectedAdNetwork != 0)
            {
                if (GUILayout.Button("Download" + " " + adNetworkOptions[selectedAdNetwork] + " " + "SDK"))
                {
                    if (selectedAdNetwork == 1)
                    {
                        OpenPackageManagerAndSearch();
                    }
                    else
                    {
                        if (selectedAdNetwork > 0 && selectedAdNetwork < adNetworkUrls.Length)
                        {
                            string documentationLink = adNetworkUrls[selectedAdNetwork];
                            Application.OpenURL(documentationLink);
                        }
                        else
                        {
                            Debug.LogWarning("Please choose an Ad Network.");
                        }
                    }

                }
            }


            if (selectedAdNetwork == 0) // Assuming "Admob(GDPR)" is the first option in adNetworkOptions
            {
                if (GUILayout.Button("Download" + " " + adNetworkOptions[selectedAdNetwork] + " " + "SDK"))
                {
                    string documentationLink = adNetworkUrls[selectedAdNetwork];
                    Application.OpenURL(documentationLink);
                }

                GUILayout.Label("Step 2: Download Admob Native SDK");

                if (GUILayout.Button("Download Admob Native SDK"))
                {
                    Application.OpenURL("https://developers.google.com/admob/unity/native");
                }

                GUILayout.Label("Step 3: Download Adapters For Mediation (Ignore this step if you only want to use Admob)");

                if (GUILayout.Button("Download Adapters"))
                {
                    Application.OpenURL("https://developers.google.com/admob/unity/choose-networks");
                }

                GUILayout.Label("Step 4: Import Mobile Monetization Pro Required Scripts");

                if (GUILayout.Button("Import Required Scripts"))
                {
                    string selectedAdNetworkOption = adNetworkOptions[selectedAdNetwork];
                    if (adNetworkUnityPackages.ContainsKey(selectedAdNetworkOption))
                    {
                        string packagePath = adNetworkUnityPackages[selectedAdNetworkOption];
                        AssetDatabase.ImportPackage(packagePath, true);
                        Debug.Log($"Imported {selectedAdNetworkOption} Unity package.");
                    }
                    else
                    {
                        Debug.LogWarning($"No Unity package found for {selectedAdNetworkOption}.");
                    }
                }
            }
            else
            {
                GUILayout.Label("Step 2: Import Mobile Monetization Pro Required Scripts");

                if (GUILayout.Button("Import Required Scripts"))
                {
                    if (selectedAdNetwork > 0 && selectedAdNetwork < adNetworkOptions.Length)
                    {
                        string selectedAdNetworkOption = adNetworkOptions[selectedAdNetwork];
                        if (adNetworkUnityPackages.ContainsKey(selectedAdNetworkOption))
                        {
                            string packagePath = adNetworkUnityPackages[selectedAdNetworkOption];
                            AssetDatabase.ImportPackage(packagePath, true);
                            Debug.Log($"Imported {selectedAdNetworkOption} Unity package.");
                        }
                        else
                        {
                            Debug.LogWarning($"No Unity package found for {selectedAdNetworkOption}.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Please choose an Ad Network.");
                    }
                }
            }

            GUILayout.Space(10);

            if (selectedAdNetwork == 0 || selectedAdNetwork == 2 || selectedAdNetwork == 3)
            {
                if (GUILayout.Button("Download Google Mobile Ads SDK For GDPR"))
                {
                    Application.OpenURL("https://developers.google.com/admob/unity/quick-start");
                }

            }

            GUILayout.Space(10);

            if (GUILayout.Button("Watch Tutorials"))
            {
                Application.OpenURL("https://www.youtube.com/playlist?list=PLijV8trSDlm5sVV4rYX5Y6i399DN6_FGp");
            }
            if (GUILayout.Button("Read Documentation"))
            {
                Application.OpenURL("https://sites.google.com/view/mobilemonetizationpro/documentation");
            }


            //if (selectedAdNetwork == 0) // Assuming "Admob(GDPR)" is the first option in adNetworkOptions
            //{
            //    GUILayout.Label("Step 3: Download Adapters For Mediation");

            //    if (GUILayout.Button("Download Adapters"))
            //    {
            //        Application.OpenURL("https://developers.google.com/admob/unity/choose-networks");
            //    }
            //}
        }

        private void DrawInAppPurchaseIntegration()
        {
            GUILayout.Label("Step 1: Download IAP from Package manager");
            if (GUILayout.Button("Download IAP from Package manager"))
            {
                OpenPackageManagerAndSearch();
            }
            GUILayout.Label("Step 2: Import Mobile Monetization Pro Required Scripts");

            if (GUILayout.Button("Import Required script"))
            {
                ImportQuickMonetizationIAPManager();
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Watch Tutorials"))
            {
                Application.OpenURL("https://www.youtube.com/playlist?list=PLijV8trSDlm5sVV4rYX5Y6i399DN6_FGp");
            }
            if (GUILayout.Button("Read Documentation"))
            {
                Application.OpenURL("https://sites.google.com/view/mobilemonetizationpro/documentation");
            }
        }
        private void DrawUserRatingPopupIntegration()
        {
            GUILayout.Label("Step 1: Download Android In App Review");
            if (GUILayout.Button("Download Android In App Review Unity Package"))
            {
                Application.OpenURL("https://developer.android.com/guide/playcore/in-app-review/unity");
            }
            GUILayout.Label("Step 2: Import User Rating Popup Scripts");

            if (GUILayout.Button("Import Required script"))
            {
                ImportQuickMonetizationUserRatingManager();
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Watch Tutorials"))
            {
                Application.OpenURL("https://www.youtube.com/playlist?list=PLijV8trSDlm5sVV4rYX5Y6i399DN6_FGp");
            }
            if (GUILayout.Button("Read Documentation"))
            {
                Application.OpenURL("https://sites.google.com/view/mobilemonetizationpro/documentation");
            }
        }
        private void DrawCrossPromotionIntegration()
        {
            GUILayout.Label("Already exist in project.");

            GUILayout.Space(10);
            if (GUILayout.Button("Watch Tutorials"))
            {
                Application.OpenURL("https://www.youtube.com/playlist?list=PLijV8trSDlm5sVV4rYX5Y6i399DN6_FGp");
            }
            if (GUILayout.Button("Read Documentation"))
            {
                Application.OpenURL("https://sites.google.com/view/mobilemonetizationpro/documentation");
            }
        }

        private void DrawFirebaseAnalyticsIntegration()
        {
            GUILayout.Label("Step 1: Download Firebase Sdk");
            if (GUILayout.Button("Download Firebase Sdk"))
            {
                Application.OpenURL("https://firebase.google.com/docs/unity/setup");
            }
            GUILayout.Label("Step 2: Import Mobile Monetization Pro Required Scripts");

            if (GUILayout.Button("Import Required script"))
            {
                ImportFirebaseUnityPackage();
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Watch Tutorials"))
            {
                Application.OpenURL("https://www.youtube.com/playlist?list=PLijV8trSDlm5sVV4rYX5Y6i399DN6_FGp");
            }
            if (GUILayout.Button("Read Documentation"))
            {
                Application.OpenURL("https://sites.google.com/view/mobilemonetizationpro/documentation");
            }
        }

        private void DrawATTIntegration()
        {
            GUILayout.Label("Step 1: Download ATT From package manager");
            if (GUILayout.Button("Download ATT From package manager"))
            {
                OpenPackageManagerAndSearch();
            }
            GUILayout.Label("Step 2: Import Mobile Monetization Pro Required Scripts");

            if (GUILayout.Button("Import Required script"))
            {
                ImportATTUnityPackage();
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Watch Tutorials"))
            {
                Application.OpenURL("https://www.youtube.com/playlist?list=PLijV8trSDlm5sVV4rYX5Y6i399DN6_FGp");
            }
            if (GUILayout.Button("Read Documentation"))
            {
                Application.OpenURL("https://sites.google.com/view/mobilemonetizationpro/documentation");
            }
        }

        private void DrawMobileNotificationsIntegration()
        {
            GUILayout.Label("Step 1: Download Mobile Notifications from Package manager");
            if (GUILayout.Button("Download Mobile Notifications from Package manager"))
            {
                OpenPackageManagerAndSearch();
            }
            GUILayout.Label("Step 2: Import Mobile Monetization Pro Required Scripts");

            if (GUILayout.Button("Import Required script"))
            {
                ImportMobileNotifUnityPackage();
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Watch Tutorials"))
            {
                Application.OpenURL("https://www.youtube.com/playlist?list=PLijV8trSDlm5sVV4rYX5Y6i399DN6_FGp");
            }
            if (GUILayout.Button("Read Documentation"))
            {
                Application.OpenURL("https://sites.google.com/view/mobilemonetizationpro/documentation");
            }
        }
        //private void DrawInAppUpdatePackageImportButton()
        //{
        //    GUILayout.Label("Import Mobile Monetization Pro Required Scripts");
        //    if (GUILayout.Button("Import Required script"))
        //    {
        //        string packagePath = "Assets/Mobile Monetization Pro/Unity Packages/MobileMonetizationPro_InAppUpdateImplementation.unitypackage";

        //        // Import QuickMonetization IAPManager package
        //        AssetDatabase.ImportPackage(packagePath, true);
        //    }

        //    GUILayout.Space(10);
        //    if (GUILayout.Button("Watch Tutorials (RECOMMENDED)"))
        //    {
        //        Application.OpenURL("");
        //    }
        //    if (GUILayout.Button("Read Documentation"))
        //    {
        //        Application.OpenURL("");
        //    }
        //}    
        private static void OpenPackageManagerAndSearch()
        {
            // Open Unity Package Manager
            EditorApplication.ExecuteMenuItem("Window/Package Manager");
        }
        private static void ImportQuickMonetizationIAPManager()
        {
            string packagePath = "Assets/Mobile Monetization Pro/Unity Packages/MobileMonetizationPro_InAppPurchasesImplementation.unitypackage";

            // Import QuickMonetization IAPManager package
            AssetDatabase.ImportPackage(packagePath, true);
        }
        private static void ImportQuickMonetizationUserRatingManager()
        {
            string packagePath = "Assets/Mobile Monetization Pro/Unity Packages/MobileMonetizationPro_UserRatingsPopup.unitypackage";

            // Import QuickMonetization IAPManager package
            AssetDatabase.ImportPackage(packagePath, true);
        }

        private static void ImportFirebaseUnityPackage()
        {
            string packagePath = "Assets/Mobile Monetization Pro/Unity Packages/MobileMonetizationPro_FirebaseImplementation.unitypackage";

            // Import Firebase Unity package
            AssetDatabase.ImportPackage(packagePath, true);
        }
        private static void ImportMobileNotifUnityPackage()
        {
            string packagePath = "Assets/Mobile Monetization Pro/Unity Packages/MobileMonetizationPro_MobileNotificationsImplementation.unitypackage";

            // Import Firebase Unity package
            AssetDatabase.ImportPackage(packagePath, true);
        }

        private static void ImportATTUnityPackage()
        {
            string packagePath = "Assets/Mobile Monetization Pro/Unity Packages/MobileMonetizationPro_ATTScreenManager.unitypackage";

            // Import ATT Unity package
            AssetDatabase.ImportPackage(packagePath, true);
        }
    }
}





//using UnityEditor;
//using UnityEngine;

//namespace QuickMonetization
//{
//    public class IntegrationWizard : EditorWindow
//    {
//        private static bool hasShownGettingStarted = false;
//        private static bool isInitialized = false;
//        private int selectedAdNetwork = 0;
//        private string[] adNetworkOptions = new string[] { "Admob", "Unity Ads", "LevelPlay", "Applovin" };
//        private string[] adNetworkUrls = new string[] { "https://developers.google.com/admob/unity/quick-start", "https://apple.com", "https://developers.is.com/ironsource-mobile/unity/unity-plugin/#step-1",
//            "https://dash.applovin.com/documentation/mediation/unity/getting-started/integration" };

//        [MenuItem("Tools/Quick Monetization/Integration Wizard", false, 0)]
//        public static void OpenGettingStartedWindow()
//        {
//            IntegrationWizard window = GetWindow<IntegrationWizard>(true, "Quick Monetization", true);
//            window.ShowUtility();
//        }

//        private void OnGUI()
//        {
//            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
//            {
//                fontSize = 25,
//                alignment = TextAnchor.MiddleCenter,
//                normal = { textColor = Color.white }
//            };

//            GUILayout.Label("Integration Wizard", labelStyle);

//            selectedAdNetwork = EditorGUILayout.Popup("Choose AdNetwork", selectedAdNetwork, adNetworkOptions);

//            GUILayout.Label("Step 1: Download Ad Network Sdk");

//            if (GUILayout.Button("Download Ad Network Sdk"))
//            {
//                if (selectedAdNetwork > 0 && selectedAdNetwork < adNetworkUrls.Length)
//                {
//                    string documentationLink = adNetworkUrls[selectedAdNetwork];
//                    Application.OpenURL(documentationLink);
//                }
//                else
//                {
//                    Debug.LogWarning("Please choose an Ad Network.");
//                }
//            }

//            GUILayout.Label("Step 2: Import QuickMonetizationAdsManager");

//            if (GUILayout.Button("Import QuickMonetization AdsManager"))
//            {
//                ImportQuickMonetizationAdsManager();
//            }

//            GUILayout.Label("Step 3: Install In App Purchases Package from the package Manager");

//            if (GUILayout.Button("Open Package Manager"))
//            {
//                OpenPackageManagerAndSearch();
//            }

//            GUILayout.Label("Step 4: Import QuickMonetizationIAPManager");

//            if (GUILayout.Button("Import QuickMonetization IAPManager"))
//            {
//                ImportQuickMonetizationIAPManager();
//            }

//            GUILayout.Space(10);

//            if (GUILayout.Button("Close"))
//            {
//                hasShownGettingStarted = true;
//                SaveHasShownFlag();
//                Close();
//            }
//        }

//        [InitializeOnLoadMethod]
//        private static void OnLoad()
//        {
//            EditorApplication.update += CheckIfFirstTime;
//        }

//        private static void CheckIfFirstTime()
//        {
//            if (!isInitialized)
//            {
//                LoadHasShownFlag();
//                if (!hasShownGettingStarted)
//                {
//                    OpenGettingStartedWindow();
//                }

//                isInitialized = true;
//                EditorApplication.update -= CheckIfFirstTime; // Unsubscribe to avoid unnecessary checks
//            }
//        }

//        private static void SaveHasShownFlag()
//        {
//            PlayerPrefs.SetInt("HasShownGettingStarted", hasShownGettingStarted ? 1 : 0);
//            PlayerPrefs.Save();
//        }

//        private static void LoadHasShownFlag()
//        {
//            hasShownGettingStarted = PlayerPrefs.GetInt("HasShownGettingStarted", 0) == 1;
//        }

//        private static void OpenPackageManagerAndSearch()
//        {
//            // Open Unity Package Manager
//            EditorApplication.ExecuteMenuItem("Window/Package Manager");
//        }

//        private static void ImportQuickMonetizationAdsManager()
//        {
//            string packagePath = "Assets/QuickMonetization/Unity Packages/QuickMonetizationAdsManager.unitypackage";

//            // Import QuickMonetization AdsManager package
//            AssetDatabase.ImportPackage(packagePath, true);
//        }

//        private static void ImportQuickMonetizationIAPManager()
//        {
//            string packagePath = "Assets/QuickMonetization/Unity Packages/QuickMonetizationIAPManager.unitypackage";

//            // Import QuickMonetization AdsManager package
//            AssetDatabase.ImportPackage(packagePath, true);
//        }
//    }
//}







////using UnityEditor;
////using UnityEngine;

////namespace QuickMonetization
////{
////    public class GettingStartedWindow : EditorWindow
////    {
////        private static bool hasShownGettingStarted = false;
////        private static bool isInitialized = false;

////        [MenuItem("Tools/Quick Monetization/Get Started", false, 0)]
////        public static void OpenGettingStartedWindow()
////        {
////            GettingStartedWindow window = GetWindow<GettingStartedWindow>(true, "Quick Monetization", true);
////            window.ShowUtility();
////        }

////        private void OnGUI()
////        {
////            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
////            {
////                fontSize = 25,
////                alignment = TextAnchor.MiddleCenter,
////                normal = { textColor = Color.white }
////            };

////            GUILayout.Label("GET STARTED", labelStyle);

////            GUILayout.Label("Step 1: Download Ad Network Sdk");
////            if (GUILayout.Button("Download Ad Network Sdk"))
////            {
////                string documentationLink = "https://developers.is.com/ironsource-mobile/unity/unity-plugin/#step-1";
////                Application.OpenURL(documentationLink);
////            }

////            GUILayout.Label("Step 2: Import QuickMonetizationAdsManager");

////            if (GUILayout.Button("Import QuickMonetization AdsManager"))
////            {
////                ImportQuickMonetizationAdsManager();
////            }

////            GUILayout.Label("Step 3: Install In App Purchases Package from the package Manager");

////            if (GUILayout.Button("Open Package Manager"))
////            {
////                OpenPackageManagerAndSearch();
////            }
////            GUILayout.Label("Step 4: Import QuickMonetizationIAPManager");

////            if (GUILayout.Button("Import QuickMonetization IAPManager"))
////            {
////                ImportQuickMonetizationIAPManager();
////            }


////            GUILayout.Space(10);

////            if (GUILayout.Button("Close"))
////            {
////                hasShownGettingStarted = true;
////                SaveHasShownFlag();
////                Close();
////            }
////        }

////        [InitializeOnLoadMethod]
////        private static void OnLoad()
////        {
////            EditorApplication.update += CheckIfFirstTime;
////        }

////        private static void CheckIfFirstTime()
////        {
////            if (!isInitialized)
////            {
////                LoadHasShownFlag();
////                if (!hasShownGettingStarted)
////                {
////                    OpenGettingStartedWindow();
////                }

////                isInitialized = true;
////                EditorApplication.update -= CheckIfFirstTime; // Unsubscribe to avoid unnecessary checks
////            }
////        }

////        private static void SaveHasShownFlag()
////        {
////            PlayerPrefs.SetInt("HasShownGettingStarted", hasShownGettingStarted ? 1 : 0);
////            PlayerPrefs.Save();
////        }

////        private static void LoadHasShownFlag()
////        {
////            hasShownGettingStarted = PlayerPrefs.GetInt("HasShownGettingStarted", 0) == 1;
////        }
////        private static void OpenPackageManagerAndSearch()
////        {
////            // Open Unity Package Manager
////            EditorApplication.ExecuteMenuItem("Window/Package Manager");
////        }
////        private static void ImportQuickMonetizationAdsManager()
////        {
////            string packagePath = "Assets/QuickMonetization/Unity Packages/QuickMonetizationAdsManager.unitypackage";

////            // Import QuickMonetization AdsManager package
////            AssetDatabase.ImportPackage(packagePath, true);
////        }
////        private static void ImportQuickMonetizationIAPManager()
////        {
////            string packagePath = "Assets/QuickMonetization/Unity Packages/QuickMonetizationIAPManager.unitypackage";

////            // Import QuickMonetization AdsManager package
////            AssetDatabase.ImportPackage(packagePath, true);
////        }
////    }
////}
