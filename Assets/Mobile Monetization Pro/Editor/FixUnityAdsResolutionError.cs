using UnityEngine;
using UnityEditor;
using System.IO;
using System.Xml;

namespace MobileMonetizationPro
{
    public class FixUnityAdsResolutionError : EditorWindow
    {
        private string selectedDirectory;

        [MenuItem("Tools/Mobile Monetization Pro/Solutions/Fix UnityAds Android Resolution Error")]
        public static void ShowWindow()
        {
            GetWindow<FixUnityAdsResolutionError>("Fix UnityAds Android Resolution Error");
        }
        private void OnGUI()
        {
            GUILayout.Label("Select Directory:", EditorStyles.boldLabel);
            selectedDirectory = EditorGUILayout.TextField(selectedDirectory);

            if (GUILayout.Button("Browse"))
            {
                selectedDirectory = EditorUtility.OpenFolderPanel("Select Directory", "", "");
            }

            GUILayout.Space(20);

            if (GUILayout.Button("Add Dependencies.xml"))
            {
                AddDependenciesXml(selectedDirectory);
            }
        }

        private void AddDependenciesXml(string directory)
        {
            if (string.IsNullOrEmpty(directory))
            {
                Debug.LogError("Please select a directory.");
                return;
            }

            string xmlFilePath = Path.Combine(directory, "Dependencies.xml");

            // Check if Dependencies.xml already exists
            if (File.Exists(xmlFilePath))
            {
                Debug.LogError("Dependencies.xml already exists in the selected directory.");
                return;
            }

            // Create XML document and root element
            XmlDocument xmlDoc = new XmlDocument();

            // Add XML declaration at the beginning
            XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null);
            xmlDoc.AppendChild(xmlDeclaration);

            XmlElement rootElement = xmlDoc.CreateElement("dependencies");
            xmlDoc.AppendChild(rootElement);

            // Create androidPackages element and androidPackage element
            XmlElement androidPackagesElement = xmlDoc.CreateElement("androidPackages");
            XmlElement androidPackageElement = xmlDoc.CreateElement("androidPackage");
            androidPackageElement.SetAttribute("spec", "androidx.lifecycle:lifecycle-process:2.6.1");
            androidPackagesElement.AppendChild(androidPackageElement);
            rootElement.AppendChild(androidPackagesElement);

            // Save XML document
            xmlDoc.Save(xmlFilePath);

            // Refresh the Asset Database to make the file immediately visible
            AssetDatabase.Refresh();

            Debug.Log("Dependencies.xml created successfully in the selected directory.");
        }
    }
}