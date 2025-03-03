using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using System.IO;
using System.Linq;

public class SobelOutlineV2Setup : EditorWindow
{
    [MenuItem("Tools/Sobel Outline V2/Setup")]
    public static void ShowWindow()
    {
        GetWindow<SobelOutlineV2Setup>("Sobel Outline V2 Setup");
    }

    [MenuItem("Tools/Sobel Outline V2/Quick Setup")]
    public static void QuickSetup()
    {
        SetupSobelOutline();
        EditorUtility.DisplayDialog("Sobel Outline V2", "Setup completed successfully!", "OK");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Sobel Outline V2 Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        EditorGUILayout.HelpBox(
            "This will set up the Sobel Outline V2 effect by:\n" +
            "1. Creating a material using the SobelOutlineV2 shader\n" +
            "2. Adding the Sobel Outline Renderer Feature to the URP Renderer\n" +
            "3. Ensuring depth texture is enabled in URP settings", 
            MessageType.Info);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Setup Sobel Outline V2"))
        {
            SetupSobelOutline();
            EditorUtility.DisplayDialog("Sobel Outline V2", "Setup completed successfully!", "OK");
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Toggle Debug Mode"))
        {
            ToggleDebugMode();
        }
    }
    
    private static void SetupSobelOutline()
    {
        // 1. Check if shader exists
        Shader sobelShader = Shader.Find("Custom/SobelOutlineV2");
        if (sobelShader == null)
        {
            Debug.LogError("SobelOutlineV2 shader not found! Make sure it's in your project.");
            EditorUtility.DisplayDialog("Error", "SobelOutlineV2 shader not found!", "OK");
            return;
        }
        
        // 2. Create material if it doesn't exist
        string materialPath = "Assets/Materials";
        string materialName = "SobelOutlineV2Material.mat";
        string fullPath = Path.Combine(materialPath, materialName);
        
        // Create Materials folder if it doesn't exist
        if (!Directory.Exists(materialPath))
        {
            Directory.CreateDirectory(materialPath);
            AssetDatabase.Refresh();
        }
        
        Material sobelMaterial;
        if (File.Exists(fullPath))
        {
            sobelMaterial = AssetDatabase.LoadAssetAtPath<Material>(fullPath);
            if (sobelMaterial.shader != sobelShader)
            {
                sobelMaterial.shader = sobelShader;
                EditorUtility.SetDirty(sobelMaterial);
            }
        }
        else
        {
            sobelMaterial = new Material(sobelShader);
            
            // Set default properties
            sobelMaterial.SetColor("_OutlineColor", Color.red);
            sobelMaterial.SetFloat("_OutlineThickness", 1.5f);
            sobelMaterial.SetFloat("_OutlineThreshold", 0.1f);
            sobelMaterial.SetFloat("_DepthSensitivity", 1.0f);
            sobelMaterial.SetFloat("_ColorSensitivity", 1.0f);
            sobelMaterial.SetFloat("_DebugMode", 0);
            
            AssetDatabase.CreateAsset(sobelMaterial, fullPath);
        }
        
        // 3. Set up URP
        SetupURPSettings();
        
        // 4. Create renderer feature if it doesn't exist
        CreateRendererFeature(sobelMaterial);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("Sobel Outline V2 setup completed successfully!");
    }
    
    private static void SetupURPSettings()
    {
        // Find URP Asset
        string[] guids = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset");
        if (guids.Length == 0)
        {
            Debug.LogError("No URP Asset found in the project!");
            EditorUtility.DisplayDialog("Error", "No URP Asset found in the project!", "OK");
            return;
        }
        
        string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        UniversalRenderPipelineAsset urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
        
        // Enable depth texture
        SerializedObject serializedObject = new SerializedObject(urpAsset);
        SerializedProperty depthTextureProperty = serializedObject.FindProperty("m_DepthPrimingMode");
        SerializedProperty opaqueTextureProperty = serializedObject.FindProperty("m_OpaqueDownsampling");
        SerializedProperty supportsDepthTextureProperty = serializedObject.FindProperty("m_SupportsDepthTexture");
        
        if (!supportsDepthTextureProperty.boolValue)
        {
            supportsDepthTextureProperty.boolValue = true;
            serializedObject.ApplyModifiedProperties();
            Debug.Log("Enabled depth texture in URP settings");
        }
    }
    
    private static void CreateRendererFeature(Material sobelMaterial)
    {
        // Find URP Renderer Data
        string[] guids = AssetDatabase.FindAssets("t:UniversalRendererData");
        if (guids.Length == 0)
        {
            Debug.LogError("No URP Renderer Data found in the project!");
            EditorUtility.DisplayDialog("Error", "No URP Renderer Data found in the project!", "OK");
            return;
        }
        
        string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        ScriptableRendererData rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(assetPath);
        
        // Check if SobelOutlineFeature already exists
        bool featureExists = false;
        foreach (var feature in rendererData.rendererFeatures)
        {
            if (feature != null && feature.name.Contains("SobelOutline"))
            {
                featureExists = true;
                Debug.Log("SobelOutline renderer feature already exists");
                break;
            }
        }
        
        if (!featureExists)
        {
            // Create a new renderer feature
            var feature = ScriptableObject.CreateInstance<SobelOutlineRendererFeature>();
            feature.name = "SobelOutlineV2";
            
            // Add the feature to the renderer
            SerializedObject serializedObject = new SerializedObject(rendererData);
            SerializedProperty featuresProperty = serializedObject.FindProperty("m_RendererFeatures");
            
            featuresProperty.arraySize++;
            var featureProperty = featuresProperty.GetArrayElementAtIndex(featuresProperty.arraySize - 1);
            featureProperty.objectReferenceValue = feature;
            
            // Save the changes
            serializedObject.ApplyModifiedProperties();
            
            // Add the feature asset to the renderer data asset
            AssetDatabase.AddObjectToAsset(feature, rendererData);
            
            Debug.Log("Added SobelOutlineV2 renderer feature to URP Renderer");
        }
    }
    
    private static void ToggleDebugMode()
    {
        // Find the material
        string materialPath = "Assets/Materials/SobelOutlineV2Material.mat";
        Material sobelMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        
        if (sobelMaterial == null)
        {
            Debug.LogError("SobelOutlineV2Material not found! Run the setup first.");
            EditorUtility.DisplayDialog("Error", "SobelOutlineV2Material not found! Run the setup first.", "OK");
            return;
        }
        
        // Toggle debug mode
        float currentDebugMode = sobelMaterial.GetFloat("_DebugMode");
        sobelMaterial.SetFloat("_DebugMode", currentDebugMode > 0.5f ? 0f : 1f);
        EditorUtility.SetDirty(sobelMaterial);
        AssetDatabase.SaveAssets();
        
        string mode = currentDebugMode > 0.5f ? "Normal" : "Debug";
        Debug.Log($"Switched to {mode} mode");
        EditorUtility.DisplayDialog("Debug Mode", $"Switched to {mode} mode", "OK");
    }
}

// Renderer Feature class
public class SobelOutlineRendererFeature : ScriptableRendererFeature
{
    class SobelOutlinePass : ScriptableRenderPass
    {
        private Material sobelMaterial;
        private RTHandle source;
        private RTHandle tempTexture;
        
        public SobelOutlinePass(Material material)
        {
            this.sobelMaterial = material;
            tempTexture = RTHandles.Alloc("_TempSobelTexture", name: "_TempSobelTexture");
        }
        
        public void Setup(RTHandle source)
        {
            this.source = source;
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (sobelMaterial == null)
            {
                Debug.LogError("Sobel material is null!");
                return;
            }
            
            CommandBuffer cmd = CommandBufferPool.Get("Sobel Outline V2");
            
            // Get camera color target descriptor
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            
            // Allocate temporary RT with RTHandle system
            RenderingUtils.ReAllocateIfNeeded(ref tempTexture, descriptor, name: "_TempSobelTexture");
            
            // Blit from source to temp with sobel material
            Blitter.BlitCameraTexture(cmd, source, tempTexture, sobelMaterial, 0);
            
            // Blit from temp back to source
            Blitter.BlitCameraTexture(cmd, tempTexture, source);
            
            // Execute command buffer
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        
        public override void FrameCleanup(CommandBuffer cmd)
        {
            // No need to release temporary RT as we're using RTHandles
        }

        public void Dispose()
        {
            tempTexture?.Release();
        }
    }
    
    private SobelOutlinePass sobelPass;
    private Material sobelMaterial;
    
    public override void Create()
    {
        // Find the material
        sobelMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SobelOutlineV2Material.mat");
        
        if (sobelMaterial == null)
        {
            Debug.LogWarning("SobelOutlineV2Material not found! The effect won't work until setup is complete.");
            return;
        }
        
        sobelPass = new SobelOutlinePass(sobelMaterial);
        sobelPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }
    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (sobelMaterial == null)
        {
            return;
        }
        
        sobelPass.Setup(renderer.cameraColorTargetHandle);
        renderer.EnqueuePass(sobelPass);
    }

    protected override void Dispose(bool disposing)
    {
        sobelPass?.Dispose();
        base.Dispose(disposing);
    }
} 