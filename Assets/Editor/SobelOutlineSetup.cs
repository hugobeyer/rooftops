using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SobelOutlineSetup : EditorWindow
{
    private Shader sobelShader;
    private UniversalRendererData rendererData;
    
    [MenuItem("Tools/Sobel Outline/Setup")]
    public static void ShowWindow()
    {
        GetWindow<SobelOutlineSetup>("Sobel Outline Setup");
    }
    
    private void OnEnable()
    {
        // Try to find the shader automatically
        sobelShader = Shader.Find("Custom/SobelOutline");
        
        // Try to find the renderer data
        var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (pipeline != null)
        {
            var rendererDataField = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                
            if (rendererDataField != null)
            {
                var rendererDatas = rendererDataField.GetValue(pipeline) as ScriptableRendererData[];
                if (rendererDatas != null && rendererDatas.Length > 0)
                {
                    rendererData = rendererDatas[0] as UniversalRendererData; // Default to first renderer
                }
            }
        }
    }
    
    private void OnGUI()
    {
        EditorGUILayout.LabelField("Sobel Outline Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        sobelShader = (Shader)EditorGUILayout.ObjectField("Sobel Shader", sobelShader, typeof(Shader), false);
        rendererData = (UniversalRendererData)EditorGUILayout.ObjectField("Renderer Data", rendererData, typeof(UniversalRendererData), false);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Create Sobel Manager"))
        {
            CreateSobelManager();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("This will create a SobelOutlineManager component on a new GameObject. " +
            "The manager will automatically set up the renderer feature.", MessageType.Info);
    }
    
    private void CreateSobelManager()
    {
        if (sobelShader == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign the Sobel Outline shader.", "OK");
            return;
        }
        
        if (rendererData == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign a Renderer Data asset.", "OK");
            return;
        }
        
        // Create a new GameObject with the manager
        GameObject go = new GameObject("Sobel Outline Manager");
        SobelOutlineManager manager = go.AddComponent<SobelOutlineManager>();
        
        // Use reflection to set private fields
        var shaderField = typeof(SobelOutlineManager).GetField("sobelShader", 
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var rendererDataField = typeof(SobelOutlineManager).GetField("rendererData", 
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            
        if (shaderField != null)
            shaderField.SetValue(manager, sobelShader);
            
        if (rendererDataField != null)
            rendererDataField.SetValue(manager, rendererData);
            
        // Force OnValidate to run
        EditorUtility.SetDirty(manager);
        
        // Select the new GameObject
        Selection.activeGameObject = go;
        
        EditorUtility.DisplayDialog("Success", "Sobel Outline Manager created successfully!", "OK");
    }
} 