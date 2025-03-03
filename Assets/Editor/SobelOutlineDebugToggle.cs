using UnityEngine;
using UnityEditor;
using System.IO;

public class SobelOutlineDebugToggle : EditorWindow
{
    [MenuItem("Tools/Sobel Outline/Toggle Debug Mode")]
    public static void ToggleDebugMode()
    {
        // Find all materials using the Sobel Outline shader
        string[] guids = AssetDatabase.FindAssets("t:Material");
        bool foundAny = false;
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            
            if (material != null && material.shader != null && 
                material.shader.name == "Custom/SobelOutline")
            {
                foundAny = true;
                
                // Check current debug mode state
                bool isDebugMode = material.GetFloat("_DebugMode") > 0.5f;
                
                // Toggle debug mode
                material.SetFloat("_DebugMode", isDebugMode ? 0.0f : 1.0f);
                
                // Mark material as dirty
                EditorUtility.SetDirty(material);
                
                Debug.Log($"Toggled debug mode for material: {material.name} - Debug mode is now {(isDebugMode ? "OFF" : "ON")}");
            }
        }
        
        if (foundAny)
        {
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Debug Mode Toggled", 
                "Debug mode has been toggled for all Sobel Outline materials.\n\n" +
                "In debug mode, you'll see edges as white lines on a black background.\n\n" +
                "Run the game to see the effect.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("No Materials Found", 
                "No materials using the 'Custom/SobelOutline' shader were found.\n\n" +
                "Please set up the Sobel Outline effect first.", "OK");
        }
    }
} 