using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;

public class SobelOutlineSetupFixed : EditorWindow
{
    [MenuItem("Tools/Sobel Outline/Setup Fixed Version")]
    public static void SetupSobelFixed()
    {
        // 1. Find the shader
        Shader sobelShader = Shader.Find("Custom/SobelOutline");
        if (sobelShader == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not find 'Custom/SobelOutline' shader. Make sure the shader exists in your project.", "OK");
            return;
        }
        
        // 2. Create material with high visibility settings
        Material sobelMaterial = new Material(sobelShader);
        sobelMaterial.name = "SobelOutlineMaterialFixed";
        
        // Set high visibility values for unlit games
        sobelMaterial.SetColor("_OutlineColor", Color.red);
        sobelMaterial.SetFloat("_OutlineThickness", 5.0f);
        sobelMaterial.SetFloat("_OutlineThreshold", 0.01f);
        sobelMaterial.SetFloat("_DepthSensitivity", 10.0f);
        sobelMaterial.SetFloat("_ColorSensitivity", 10.0f);
        
        // 3. Save material to project
        string materialPath = "Assets/Materials/SobelOutlineMaterialFixed.mat";
        
        // Make sure the directory exists
        string directory = Path.GetDirectoryName(materialPath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        AssetDatabase.CreateAsset(sobelMaterial, materialPath);
        AssetDatabase.SaveAssets();
        
        // 4. Find URP renderer data
        UniversalRendererData rendererData = null;
        
        // Try to find PC renderer first, then mobile
        string[] rendererPaths = {
            "Assets/Settings/PC_Renderer.asset",
            "Assets/Settings/Mobile_Renderer.asset",
            "Assets/Settings/EndsURP.asset"
        };
        
        foreach (string path in rendererPaths)
        {
            if (File.Exists(path))
            {
                rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
                if (rendererData != null)
                    break;
            }
        }
        
        if (rendererData == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not find URP Renderer Data asset. Please set up the renderer feature manually.", "OK");
            return;
        }
        
        // 5. Check if Depth Texture is enabled in URP asset
        UniversalRenderPipelineAsset urpAsset = null;
        string[] urpAssetPaths = {
            "Assets/Settings/PC_RPAsset.asset",
            "Assets/Settings/Mobile_RPAsset.asset",
            "Assets/Settings/EndsURP.asset"
        };
        
        foreach (string path in urpAssetPaths)
        {
            if (File.Exists(path))
            {
                urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                if (urpAsset != null)
                    break;
            }
        }
        
        if (urpAsset != null)
        {
            // Use SerializedObject to access the depth texture property
            SerializedObject serializedObject = new SerializedObject(urpAsset);
            
            if (serializedObject.FindProperty("m_DepthTexture") != null && 
                serializedObject.FindProperty("m_OpaqueTexture") != null)
            {
                bool depthTextureEnabled = serializedObject.FindProperty("m_DepthTexture").boolValue;
                bool opaqueTextureEnabled = serializedObject.FindProperty("m_OpaqueTexture").boolValue;
                
                if (!depthTextureEnabled || !opaqueTextureEnabled)
                {
                    bool enableTextures = EditorUtility.DisplayDialog(
                        "URP Settings",
                        "The Sobel outline effect requires Depth Texture and Opaque Texture to be enabled in the URP asset. Would you like to enable them now?",
                        "Yes", "No");
                    
                    if (enableTextures)
                    {
                        serializedObject.FindProperty("m_DepthTexture").boolValue = true;
                        serializedObject.FindProperty("m_OpaqueTexture").boolValue = true;
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(urpAsset);
                    }
                }
            }
        }
        
        // 6. Create and add the renderer feature
        SobelOutlineFeature sobelFeature = null;
        
        // Check if feature already exists
        foreach (var feature in rendererData.rendererFeatures)
        {
            if (feature is SobelOutlineFeature)
            {
                sobelFeature = (SobelOutlineFeature)feature;
                break;
            }
        }
        
        if (sobelFeature == null)
        {
            // Create new feature
            sobelFeature = ScriptableObject.CreateInstance<SobelOutlineFeature>();
            sobelFeature.name = "Sobel Outline";
            rendererData.rendererFeatures.Add(sobelFeature);
        }
        
        // Update feature settings
        sobelFeature.settings.sobelMaterial = sobelMaterial;
        sobelFeature.settings.renderPassEvent = UnityEngine.Rendering.Universal.RenderPassEvent.BeforeRenderingPostProcessing;
        sobelFeature.settings.outlineColor = Color.red;
        sobelFeature.settings.outlineThickness = 5.0f;
        sobelFeature.settings.outlineThreshold = 0.01f;
        sobelFeature.settings.depthSensitivity = 10.0f;
        sobelFeature.settings.colorSensitivity = 10.0f;
        
        // Force renderer data to update
        EditorUtility.SetDirty(rendererData);
        
        // 7. Create a GameObject with the helper component
        GameObject helperGO = new GameObject("Sobel Outline Helper");
        var helper = helperGO.AddComponent<SobelOutlineSetupHelper>();
        helper.sobelMaterial = sobelMaterial;
        helper.outlineColor = Color.red;
        helper.outlineThickness = 5.0f;
        helper.outlineThreshold = 0.01f;
        helper.depthSensitivity = 10.0f;
        helper.colorSensitivity = 10.0f;
        
        // 8. Select the new GameObject
        Selection.activeGameObject = helperGO;
        
        EditorUtility.DisplayDialog("Success", "Fixed Sobel Outline has been set up!\n\nA helper GameObject has been created in the scene.\n\nThe effect should be visible in play mode. If not, try adjusting the sensitivity values in the Sobel Outline Helper component.", "OK");
    }
} 