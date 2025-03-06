// using UnityEngine;
// using UnityEditor;
// using System.IO;
// using UnityEngine.Rendering;
// using UnityEngine.Rendering.Universal;
// using RoofTops;

// public class RoofTopsMenu : Editor
// {
//     [MenuItem("RoofTops/Reset All Game Data")]
//     private static void ResetAllGameData()
//     {
//         // Reset data in all GameDataObject assets
//         string[] guids = AssetDatabase.FindAssets("t:GameDataObject");
//         foreach (string guid in guids)
//         {
//             string path = AssetDatabase.GUIDToAssetPath(guid);
//             GameDataObject gameData = AssetDatabase.LoadAssetAtPath<GameDataObject>(path);
//             if (gameData != null)
//             {
//                 gameData.totalTridotCollected = 0;
//                 gameData.lastRunTridotCollected = 0;
//                 gameData.bestRunTridotCollected = 0;
//                 gameData.bestDistance = 0;
//                 gameData.lastRunDistance = 0;
//                 gameData.lastRunMemcardsCollected = 0;
//                 gameData.totalMemcardsCollected = 0;
//                 gameData.bestRunMemcardsCollected = 0;
//                 gameData.hasShownDashInfo = false;
//                 EditorUtility.SetDirty(gameData);
//                 Debug.Log($"Reset data in {path}");
//             }
//         }
//         AssetDatabase.SaveAssets();
//         // Reset GameManager's runtime data
//         GameManager gameManager = FindFirstObjectByType<GameManager>();
//         if (gameManager != null && gameManager.gameData != null)
//         {
//             gameManager.ClearGameData();
//             Debug.Log("Reset runtime game data using GameManager.ClearGameData()");
//         }
//         // Reset UI displays if they exist
//         if (TridotsTextDisplay.Instance != null)
//         {
//             TridotsTextDisplay.Instance.ResetTotal();
//             Debug.Log("Reset tridots display UI");
//         }
//         // Clear PlayerPrefs
//         PlayerPrefs.DeleteAll();
//         PlayerPrefs.Save();
//         Debug.Log("Cleared all PlayerPrefs data");
//         Debug.Log("All game data has been reset successfully.");
//     }

//     [MenuItem("RoofTops/Effects/Setup Sobel Outline")]
//     private static void SetupSobelOutline()
//     {
//         // Find the shader
//         Shader sobelShader = Shader.Find("Custom/SobelOutline");
//         if (sobelShader == null)
//         {
//             EditorUtility.DisplayDialog("Error", "Could not find 'Custom/SobelOutline' shader. Make sure the shader exists in your project.", "OK");
//             return;
//         }
        
//         // Create material
//         Material sobelMaterial = new Material(sobelShader);
//         sobelMaterial.name = "SobelOutlineMaterial";
        
//         // Save material to project
//         string materialPath = "Assets/Materials/SobelOutlineMaterial.mat";
        
//         // Make sure the directory exists
//         string directory = System.IO.Path.GetDirectoryName(materialPath);
//         if (!System.IO.Directory.Exists(directory))
//         {
//             System.IO.Directory.CreateDirectory(directory);
//         }
        
//         AssetDatabase.CreateAsset(sobelMaterial, materialPath);
//         AssetDatabase.SaveAssets();
        
//         // Find URP renderer data
//         UnityEngine.Rendering.Universal.UniversalRendererData rendererData = null;
        
//         // Try to find PC renderer first, then mobile
//         string[] rendererPaths = {
//             "Assets/Settings/PC_Renderer.asset",
//             "Assets/Settings/Mobile_Renderer.asset",
//             "Assets/Settings/EndsURP.asset"
//         };
        
//         foreach (string path in rendererPaths)
//         {
//             if (System.IO.File.Exists(path))
//             {
//                 rendererData = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>(path);
//                 if (rendererData != null)
//                     break;
//             }
//         }
        
//         if (rendererData == null)
//         {
//             EditorUtility.DisplayDialog("Error", "Could not find URP Renderer Data asset. Please set up the renderer feature manually.", "OK");
//             return;
//         }
        
//         // Check if feature already exists
//         bool featureExists = false;
//         foreach (var feature in rendererData.rendererFeatures)
//         {
//             if (feature is SobelOutlineFeature)
//             {
//                 featureExists = true;
//                 break;
//             }
//         }
        
//         // Add feature if it doesn't exist
//         if (!featureExists)
//         {
//             SobelOutlineFeature sobelFeature = ScriptableObject.CreateInstance<SobelOutlineFeature>();
//             sobelFeature.name = "Sobel Outline";
//             sobelFeature.settings.sobelMaterial = sobelMaterial;
            
//             // Set default values
//             sobelFeature.settings.outlineColor = Color.black;
//             sobelFeature.settings.outlineThickness = 2.0f;
//             sobelFeature.settings.outlineThreshold = 0.1f;
//             sobelFeature.settings.depthSensitivity = 0.5f;
//             sobelFeature.settings.colorSensitivity = 0.5f;
            
//             rendererData.rendererFeatures.Add(sobelFeature);
//             EditorUtility.SetDirty(rendererData);
//         }
        
//         // Create helper GameObject in scene
//         GameObject helperGO = new GameObject("Sobel Outline Helper");
//         SobelOutlineSetupHelper helper = helperGO.AddComponent<SobelOutlineSetupHelper>();
//         helper.sobelMaterial = sobelMaterial;
        
//         // Select the new GameObject
//         Selection.activeGameObject = helperGO;
        
//         // Make sure URP settings are correct
//         CheckURPSettings();
        
//         EditorUtility.DisplayDialog("Success", "Sobel Outline effect has been set up successfully!\n\nThe effect has been added to your URP renderer and a helper GameObject has been created in the scene.", "OK");
//     }

//     [MenuItem("RoofTops/Effects/Check URP Settings for Sobel")]
//     private static void CheckURPSettings()
//     {
//         // Find URP asset
//         UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset urpAsset = null;
        
//         string[] assetPaths = {
//             "Assets/Settings/PC_RPAsset.asset",
//             "Assets/Settings/Mobile_RPAsset.asset",
//             "Assets/Settings/EndsURP.asset"
//         };
        
//         foreach (string path in assetPaths)
//         {
//             if (System.IO.File.Exists(path))
//             {
//                 urpAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>(path);
//                 if (urpAsset != null)
//                     break;
//             }
//         }
        
//         if (urpAsset == null)
//         {
//             Debug.LogWarning("Could not find URP Asset. Please make sure Depth Texture and Normal Texture are enabled in your URP settings.");
//             return;
//         }
        
//         // Check if depth texture is enabled
//         var depthTextureField = typeof(UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset).GetField("m_DepthTexture", 
//             System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            
//         bool depthTextureEnabled = false;
//         if (depthTextureField != null)
//         {
//             depthTextureEnabled = (bool)depthTextureField.GetValue(urpAsset);
//         }
        
//         // Check if normal texture is enabled
//         var normalTextureField = typeof(UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset).GetField("m_NormalsTexture", 
//             System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            
//         bool normalTextureEnabled = false;
//         if (normalTextureField != null)
//         {
//             normalTextureEnabled = (bool)normalTextureField.GetValue(urpAsset);
//         }
        
//         // Show warning if settings need to be changed
//         if (!depthTextureEnabled || !normalTextureEnabled)
//         {
//             string message = "For the Sobel effect to work properly, please enable the following in your URP Asset:";
//             if (!depthTextureEnabled)
//                 message += "\n- Depth Texture";
//             if (!normalTextureEnabled)
//                 message += "\n- Normal Texture";
                
//             EditorUtility.DisplayDialog("URP Settings Warning", message, "OK");
//         }
//     }
// }