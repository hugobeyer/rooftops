using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;

public class SobelOutlineSetupSimple : EditorWindow
{
    [MenuItem("Tools/Sobel Outline/Simple Setup")]
    public static void SetupSobel()
    {
        // 1. Find the shader
        Shader sobelShader = Shader.Find("Custom/SobelOutline");
        if (sobelShader == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not find 'Custom/SobelOutline' shader. Make sure the shader exists in your project.", "OK");
            return;
        }
        
        // 2. Create material
        Material sobelMaterial = new Material(sobelShader);
        sobelMaterial.name = "SobelOutlineMaterial";
        
        // 3. Save material to project
        string materialPath = "Assets/Materials/SobelOutlineMaterial.mat";
        
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
        
        // 5. Create a GameObject with the helper component
        GameObject helperGO = new GameObject("Sobel Outline Helper");
        var helper = helperGO.AddComponent<SobelOutlineSetupHelper>();
        helper.sobelMaterial = sobelMaterial;
        
        // 6. Select the new GameObject
        Selection.activeGameObject = helperGO;
        
        EditorUtility.DisplayDialog("Success", "Sobel Outline effect has been set up successfully!\n\nA helper GameObject has been created in the scene.\n\nYou'll need to manually add the Sobel Outline Feature to your URP renderer.", "OK");
    }

    [MenuItem("Tools/Sobel Outline/Simple Setup (High Visibility)")]
    public static void SetupSobelHighVisibility()
    {
        // 1. Find the shader
        Shader sobelShader = Shader.Find("Custom/SobelOutline");
        if (sobelShader == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not find 'Custom/SobelOutline' shader. Make sure the shader exists in your project.", "OK");
            return;
        }
        
        // 2. Create material
        Material sobelMaterial = new Material(sobelShader);
        sobelMaterial.name = "SobelOutlineMaterialHighVis";
        
        // Set high visibility values
        sobelMaterial.SetColor("_OutlineColor", Color.red);
        sobelMaterial.SetFloat("_OutlineThickness", 3.0f);
        sobelMaterial.SetFloat("_OutlineThreshold", 0.1f);
        sobelMaterial.SetFloat("_DepthSensitivity", 0.5f);
        sobelMaterial.SetFloat("_ColorSensitivity", 0.5f);
        
        // 3. Save material to project
        string materialPath = "Assets/Materials/SobelOutlineMaterialHighVis.mat";
        
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
        
        // 5. Create and add the renderer feature
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
        
        // Force renderer data to update
        EditorUtility.SetDirty(rendererData);
        
        // 6. Create a GameObject with the helper component
        GameObject helperGO = new GameObject("Sobel Outline Helper");
        var helper = helperGO.AddComponent<SobelOutlineSetupHelper>();
        helper.sobelMaterial = sobelMaterial;
        helper.outlineColor = Color.red;
        helper.outlineThickness = 3.0f;
        helper.outlineThreshold = 0.1f;
        helper.depthSensitivity = 0.5f;
        helper.colorSensitivity = 0.5f;
        
        // 7. Select the new GameObject
        Selection.activeGameObject = helperGO;
        
        EditorUtility.DisplayDialog("Success", "High Visibility Sobel Outline effect has been set up!\n\nA helper GameObject has been created in the scene.\n\nThe effect should be visible immediately in play mode.", "OK");
    }
} 