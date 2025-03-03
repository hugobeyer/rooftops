using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class SobelOutlineManager : MonoBehaviour
{
    [Header("Shader Reference")]
    [SerializeField] private Shader sobelShader;
    
    [Header("Runtime Material")]
    [SerializeField] private Material sobelMaterial;
    
    [Header("Renderer Feature Reference")]
    [SerializeField] private UniversalRendererData rendererData;
    
    private SobelOutlineFeature sobelFeature;
    
    private void OnEnable()
    {
        // Create material if needed
        if (sobelMaterial == null && sobelShader != null)
        {
            sobelMaterial = new Material(sobelShader);
            sobelMaterial.hideFlags = HideFlags.HideAndDontSave;
            
            // Find and setup the renderer feature
            SetupRendererFeature();
        }
    }
    
    private void OnValidate()
    {
        // Update material if shader changes
        if (sobelShader != null && (sobelMaterial == null || sobelMaterial.shader != sobelShader))
        {
            sobelMaterial = new Material(sobelShader);
            sobelMaterial.hideFlags = HideFlags.HideAndDontSave;
        }
        
        // Setup renderer feature
        SetupRendererFeature();
    }
    
    private void SetupRendererFeature()
    {
        if (rendererData == null || sobelMaterial == null)
            return;
            
        // Find existing Sobel feature or create a new one
        sobelFeature = null;
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
        
        // Force renderer data to update
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(rendererData);
        #endif
    }
    
    private void OnDisable()
    {
        // Clean up
        if (sobelMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(sobelMaterial);
            }
            else
            {
                DestroyImmediate(sobelMaterial);
            }
            sobelMaterial = null;
        }
    }
} 