using UnityEngine;
using UnityEditor;
using System.IO;


namespace MobileMonetizationPro
{
    public class UpdatePluginFiles : EditorWindow
    {
        private TextAsset androidManifest;
        private string androidManifestPath = "";
        private string modifiedManifestText = "";

        private string gradlePropertiesPath = "";
        private string modifiedPropertiesText = "";

        private Vector2 scrollPositionManifest;
        private Vector2 scrollPositionProperties;

        [MenuItem("Tools/Mobile Monetization Pro/Update Android Plugins", false, 1)]
        public static void ShowWindow()
        {
            GetWindow<UpdatePluginFiles>("Modify Android Manifest and Gradle Properties");
        }

        private void OnGUI()
        {
            GUILayout.Label("Select your AndroidManifest.xml file:", EditorStyles.boldLabel);

            androidManifest = EditorGUILayout.ObjectField("AndroidManifest.xml", androidManifest, typeof(TextAsset), false) as TextAsset;

            if (GUILayout.Button("Modify Android Manifest"))
            {
                if (androidManifest != null)
                {
                    modifiedManifestText = androidManifest.text;
                }
            }


            if (GUILayout.Button("Open Android Setting To Copy"))
            {
                Application.OpenURL("https://developers.is.com/ironsource-mobile/unity/unity-plugin/#step-3:~:text=Step%204.-,Additional%20setup%20for%20Android,-Apps%20updating%20their");
            }

            scrollPositionManifest = EditorGUILayout.BeginScrollView(scrollPositionManifest);
            modifiedManifestText = EditorGUILayout.TextArea(modifiedManifestText, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            GUILayout.Label("Select your Gradle Template Properties file (e.g., gradletemplate.properties):", EditorStyles.boldLabel);

            if (GUILayout.Button("Select Gradle Properties File"))
            {
                gradlePropertiesPath = EditorUtility.OpenFilePanel("Select Gradle Template Properties", "", "properties");
                if (!string.IsNullOrEmpty(gradlePropertiesPath))
                {
                    modifiedPropertiesText = File.ReadAllText(gradlePropertiesPath);
                }
            }

            scrollPositionProperties = EditorGUILayout.BeginScrollView(scrollPositionProperties);
            modifiedPropertiesText = EditorGUILayout.TextArea(modifiedPropertiesText, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Update Manifest and Properties"))
            {
                if (androidManifest != null)
                {
                    string manifestPath = AssetDatabase.GetAssetPath(androidManifest);
                    File.WriteAllText(manifestPath, modifiedManifestText);
                    AssetDatabase.ImportAsset(manifestPath);
                }

                if (!string.IsNullOrEmpty(gradlePropertiesPath))
                {
                    File.WriteAllText(gradlePropertiesPath, modifiedPropertiesText);
                    AssetDatabase.Refresh();
                }

                EditorUtility.DisplayDialog("Success", "Android Manifest and Gradle Template Properties have been updated!", "OK");
            }

        }
    }
}