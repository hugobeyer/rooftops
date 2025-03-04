using UnityEngine;
using UnityEditor;

/// <summary> Sets a background color for this game object in the Unity Hierarchy window </summary>
[InitializeOnLoad]
public class CustomHierarchy
{
    private static Vector2 offset = new Vector2(20, 1);
    private static HierarchyColorSettings settings;

    static CustomHierarchy()
    {
        EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
        LoadSettings();
    }

    private static void LoadSettings()
    {
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
        var obj = EditorUtility.InstanceIDToObject(instanceID);
        if (obj != null)
        {
            Color backgroundColor = Color.white;
            Color textColor = Color.white;
            Texture2D texture = null;

            GameObject gameObj = obj as GameObject;
            if (gameObj != null)
            {
                // Check settings first
                if (settings != null && settings.TryGetColor(gameObj.name, out backgroundColor))
                {
                    textColor = new Color(0.9f, 0.9f, 0.9f);
                }
                // Default rules
                else if (gameObj.name == "Player")
                {
                    backgroundColor = new Color(0.2f, 0.6f, 0.1f);
                    textColor = new Color(0.9f, 0.9f, 0.9f);
                }
                else if(gameObj.GetComponent<Canvas>())
                {
                    backgroundColor = new Color(0.7f, 0.45f, 0.0f);
                    textColor = new Color(0.9f, 0.9f, 0.9f);
                }
                else
                {
                    ColorInHierarchy colorComponent = gameObj.GetComponent<ColorInHierarchy>();
                    if (colorComponent != null)
                    {
                        backgroundColor = colorComponent.color;
                        textColor = new Color(0.9f, 0.9f, 0.9f);
                    }
                }

                if (backgroundColor != Color.white)
                {
                    Rect offsetRect = new Rect(selectionRect.position + offset, selectionRect.size);
                    Rect bgRect = new Rect(selectionRect.x, selectionRect.y, selectionRect.width + 50, selectionRect.height);

                    EditorGUI.DrawRect(bgRect, backgroundColor);
                    EditorGUI.LabelField(offsetRect, obj.name, new GUIStyle()
                    {
                        normal = new GUIStyleState() { textColor = textColor },
                        fontStyle = FontStyle.Bold
                    });

                    if (texture != null)
                    {
                        EditorGUI.DrawPreviewTexture(new Rect(selectionRect.position, new Vector2(selectionRect.height, selectionRect.height)), texture);
                    }
                }
            }
        }
    }
}
