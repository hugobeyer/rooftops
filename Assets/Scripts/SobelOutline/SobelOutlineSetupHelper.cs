using UnityEngine;

/// <summary>
/// Simple helper script to set up the Sobel outline effect in your game.
/// Attach this to a GameObject in your scene.
/// </summary>
public class SobelOutlineSetupHelper : MonoBehaviour
{
    [Header("Sobel Effect Settings")]
    [Tooltip("The material created from the Sobel shader")]
    public Material sobelMaterial;
    
    [Header("Outline Appearance")]
    [Tooltip("Color of the outline")]
    [ColorUsage(true, true)]
    public Color outlineColor = Color.red;
    
    [Tooltip("Thickness of the outline")]
    [Range(1f, 10f)]
    public float outlineThickness = 5.0f;
    
    [Tooltip("Threshold for edge detection")]
    [Range(0.0f, 1.0f)]
    public float outlineThreshold = 0.01f;
    
    [Header("Edge Detection Sensitivity")]
    [Tooltip("How much depth differences affect the outline")]
    [Range(0.0f, 10.0f)]
    public float depthSensitivity = 10.0f;
    
    [Tooltip("How much color differences affect the outline")]
    [Range(0.0f, 10.0f)]
    public float colorSensitivity = 10.0f;
    
    [Tooltip("When enabled, shows edges as white lines on black background")]
    public bool debugMode = false;
    
    private void Update()
    {
        if (sobelMaterial != null)
        {
            // Update material properties
            sobelMaterial.SetColor("_OutlineColor", outlineColor);
            sobelMaterial.SetFloat("_OutlineThickness", outlineThickness);
            sobelMaterial.SetFloat("_OutlineThreshold", outlineThreshold);
            sobelMaterial.SetFloat("_DepthSensitivity", depthSensitivity);
            sobelMaterial.SetFloat("_ColorSensitivity", colorSensitivity);
            sobelMaterial.SetFloat("_DebugMode", debugMode ? 1.0f : 0.0f);
        }
    }
} 