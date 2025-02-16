#if UNITY_EDITOR && UNITY_IOS
using UnityEngine;
using UnityEditor;
using UnityEditor.iOS.Xcode;
using System.IO;


namespace MobileMonetizationPro
{
    public class UpdateInfoPlistWindow : EditorWindow
    {
        private string plistPath;
        private string skAdNetworkIdentifier = "su67r6k2v3.skadnetwork";
        private string advertisingAttributionReportEndpoint = "https://postbacks-is.com";

        [MenuItem("Tools/Mobile Monetization Pro/Update Xcode Info.Plist For LevelPlay", false, 2)]
        public static void ShowWindow()
        {
            GetWindow<UpdateInfoPlistWindow>("Update Info.plist");
        }

        private void OnGUI()
        {
            GUILayout.Label("Select your Info.plist file:", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            plistPath = EditorGUILayout.TextField(plistPath);
            if (GUILayout.Button("Browse", GUILayout.Width(80)))
            {
                plistPath = EditorUtility.OpenFilePanel("Select Info.plist", Application.dataPath, "plist");
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            if (GUILayout.Button("Update Info.plist"))
            {
                if (string.IsNullOrEmpty(plistPath) || !File.Exists(plistPath))
                {
                    ShowError("Please select a valid Info.plist file.");
                }
                else
                {
                    UpdatePlistFile();
                }
            }
        }

        private void UpdatePlistFile()
        {
            PlistDocument plist = new PlistDocument();
            plist.ReadFromFile(plistPath);

            // Modify SKAdNetworkItems and NSAdvertisingAttributionReportEndpoint
            PlistElementDict rootDict = plist.root;

            // SKAdNetworkItems array
            PlistElementArray skAdNetworkItemsArray = rootDict.CreateArray("SKAdNetworkItems");
            PlistElementDict skAdNetworkItemDict = skAdNetworkItemsArray.AddDict();
            skAdNetworkItemDict.SetString("SKAdNetworkIdentifier", skAdNetworkIdentifier);

            // NSAdvertisingAttributionReportEndpoint
            rootDict.SetString("NSAdvertisingAttributionReportEndpoint", advertisingAttributionReportEndpoint);

            // Add NSAppTransportSecurity dictionary with NSAllowsArbitraryLoads
            PlistElementDict appTransportSecurityDict;
            if (rootDict.values.ContainsKey("NSAppTransportSecurity"))
            {
                appTransportSecurityDict = rootDict.values["NSAppTransportSecurity"] as PlistElementDict;
            }
            else
            {
                appTransportSecurityDict = rootDict.CreateDict("NSAppTransportSecurity");
            }

            appTransportSecurityDict.SetBoolean("NSAllowsArbitraryLoads", true);

            // Save the modified Info.plist
            plist.WriteToFile(plistPath);

            ShowMessage("Info.plist has been updated successfully.");
        }

        private void ShowMessage(string message)
        {
            EditorUtility.DisplayDialog("Success", message, "OK");
        }

        private void ShowError(string message)
        {
            EditorUtility.DisplayDialog("Error", message, "OK");
        }
    }
}
#endif