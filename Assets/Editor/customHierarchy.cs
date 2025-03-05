using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary> Sets a background color for this game object in the Unity Hierarchy window </summary>
[InitializeOnLoad]
public class CustomHierarchy
{
    private static Vector2 offset = new Vector2(20, 1);
    private static HierarchyColorSettings settings;
    private static float leftMargin = 16f; // Left margin to avoid covering the foldout arrow
    
    // Flag to track if we've already tried to load settings
    private static bool settingsLoaded = false;

    static CustomHierarchy()
    {
        // Register for the event that happens BEFORE the hierarchy window draws items
        EditorApplication.hierarchyWindowItemOnGUI -= HandleHierarchyWindowItemOnGUI;
        EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
        
        // Delay the settings loading to ensure it runs on the main thread
        EditorApplication.delayCall += LoadSettings;
    }

    private static void LoadSettings()
    {
        settingsLoaded = true;
        if (settings == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:HierarchyColorSettings");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                settings = AssetDatabase.LoadAssetAtPath<HierarchyColorSettings>(path);
            }
        }
    }

    private static void HandleHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
    {
        // Make sure settings are loaded
        if (!settingsLoaded)
        {
            LoadSettings();
        }
        
        var obj = EditorUtility.InstanceIDToObject(instanceID);
        if (obj != null)
        {
            Color backgroundColor = Color.white;
            bool shouldColorBackground = false;

            GameObject gameObj = obj as GameObject;
            if (gameObj != null)
            {
                // Check settings first
                if (settings != null && settings.TryGetColor(gameObj.name, out backgroundColor))
                {
                    shouldColorBackground = true;
                }
                // Default rules
                else if (gameObj.name == "Player")
                {
                    backgroundColor = new Color(0.2f, 0.6f, 0.1f);
                    shouldColorBackground = true;
                }
                else if(gameObj.GetComponent<Canvas>())
                {
                    backgroundColor = new Color(0.7f, 0.45f, 0.0f);
                    shouldColorBackground = true;
                }
                else
                {
                    ColorInHierarchy colorComponent = gameObj.GetComponent<ColorInHierarchy>();
                    if (colorComponent != null)
                    {
                        backgroundColor = colorComponent.color;
                        shouldColorBackground = true;
                    }
                }

                // Draw the background ONLY if we should color it
                if (shouldColorBackground)
                {
                    // Create a rect that's aligned to the right side with a width of 10px
                    Rect bgRect = new Rect(
                        selectionRect.x + selectionRect.width - 10, // Position at the right side
                        selectionRect.y, 
                        10, // Fixed width of 10px
                        selectionRect.height
                    );

                    // Use the full color without transparency
                    EditorGUI.DrawRect(bgRect, backgroundColor);
                }
            }
        }
    }
}
