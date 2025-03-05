using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;
using System.Text.RegularExpressions;

public class IconBrowser : EditorWindow
{
    private Vector2 scrollPosition;
    private string searchString = "";
    private List<string> iconNames = new List<string>();
    private GUIContent[] iconContent;
    private int iconSize = 40;
    private bool showOnlyValidIcons = true;
    private bool hasLoadedExtendedIcons = false;
    private bool isLoadingIcons = false;

    [MenuItem("Tools/Icon Browser")]
    public static void ShowWindow()
    {
        GetWindow<IconBrowser>("Icon Browser");
    }

    private void OnEnable()
    {
        // Start with common icon names
        iconNames = new List<string>
        {
            "Prefab Icon", "PrefabVariant Icon", "GameObject Icon", "d_Prefab Icon", 
            "d_PrefabVariant Icon", "d_GameObject Icon", "Animation Icon", "Camera Icon",
            "Light Icon", "AudioSource Icon", "Favorite Icon", "Settings Icon",
            "Folder Icon", "cs Script Icon", "Image Icon", "ScriptableObject Icon",
            "Material Icon", "Mesh Icon", "Texture Icon", "Sprite Icon",
            "Outline Icon", "Toggle Icon" // Added the requested icons
        };

        // Try to discover more icons using reflection
        TryDiscoverMoreIcons();
        
        // Remove duplicates
        iconNames = iconNames.Distinct().ToList();
        
        // Create GUIContent array
        RefreshIconContent();
    }

    private void RefreshIconContent()
    {
        iconContent = new GUIContent[iconNames.Count];
        for (int i = 0; i < iconNames.Count; i++)
        {
            iconContent[i] = EditorGUIUtility.IconContent(iconNames[i]);
        }
    }

    private void TryDiscoverMoreIcons()
    {
        try
        {
            // Try to get icons from EditorGUIUtility using reflection
            var editorAssetBundle = typeof(EditorGUIUtility)
                .GetMethod("GetEditorAssetBundle", BindingFlags.NonPublic | BindingFlags.Static);

            if (editorAssetBundle != null)
            {
                // Get common icon name patterns
                var commonPatterns = new List<string>
                {
                    "Icon", "icon", "Gizmo", "gizmo", "Image", "image", 
                    "Texture", "texture", "Asset", "asset"
                };

                // Add more potential icon names based on Unity's component types
                foreach (var type in GetAllUnityComponentTypes())
                {
                    iconNames.Add(type.Name + " Icon");
                    iconNames.Add("d_" + type.Name + " Icon"); // Dark theme version
                }

                // Add common editor icons
                AddCommonEditorIcons();
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error discovering icons: " + e.Message);
        }
    }

    private void AddCommonEditorIcons()
    {
        // Add common editor icons by pattern
        string[] commonPrefixes = { "", "d_" }; // Regular and dark theme
        string[] commonTypes = {
            "Prefab", "GameObject", "Scene", "SceneAsset", "Material", "Mesh", "Texture", "Sprite",
            "Animation", "AnimationClip", "Animator", "AudioClip", "AudioMixer", "Avatar",
            "Font", "GUISkin", "Script", "Shader", "TerrainData", "TextAsset", "Cubemap",
            "Flare", "LightingData", "LightmapParameters", "LightProbes", "NavMeshData",
            "PhysicMaterial", "Preset", "RenderTexture", "ScriptableObject", "Tilemap",
            "TrueTypeFont", "WebCamTexture", "ComputeShader", "Brush", "TerrainLayer",
            "Timeline", "VideoClip", "WindZone", "Lighting", "Occlusion", "Reflection",
            "Collider", "Rigidbody", "Camera", "Light", "Canvas", "EventSystem",
            "ParticleSystem", "LineRenderer", "TrailRenderer", "Transform", "RectTransform",
            "Button", "Text", "Image", "RawImage", "Slider", "Toggle", "Dropdown",
            "InputField", "ScrollRect", "ScrollView", "Scrollbar", "Mask", "Outline",
            "Shadow", "GridLayoutGroup", "HorizontalLayoutGroup", "VerticalLayoutGroup",
            "LayoutElement", "ContentSizeFitter", "AspectRatioFitter", "CanvasScaler",
            "GraphicRaycaster", "CanvasGroup", "CanvasRenderer", "WorldSpace", "ScreenSpace",
            "Favorite", "Settings", "Project", "Hierarchy", "Console", "Inspector",
            "Game", "Scene", "Animation", "Profiler", "AssetStore", "Services",
            "Package", "Build", "Collab", "Test", "Timeline", "Version Control",
            // Additional UI elements
            "Checkmark", "Check", "Toggle", "Dropdown", "Arrow", "ArrowDown", "ArrowUp",
            "ArrowLeft", "ArrowRight", "Menu", "Hamburger", "Options", "More", "Add",
            "Remove", "Delete", "Trash", "Edit", "Pencil", "Save", "Disk", "Load",
            "Refresh", "Reload", "Update", "Sync", "Link", "Unlink", "Connect", "Disconnect",
            "Lock", "Unlock", "Visible", "Hidden", "Eye", "EyeClosed", "View", "Preview",
            "Search", "Find", "Zoom", "ZoomIn", "ZoomOut", "Filter", "Sort", "List",
            "Grid", "Table", "Warning", "Error", "Info", "Help", "Question", "Success",
            "Fail", "Star", "Heart", "Like", "Bookmark", "Pin", "Flag", "Tag",
            "Label", "Badge", "Clock", "Time", "Calendar", "Date", "Notification", "Alert",
            "Bell", "Sound", "Volume", "Mute", "Play", "Pause", "Stop", "Record",
            "Forward", "Backward", "Skip", "Repeat", "Shuffle", "Next", "Previous", "First",
            "Last", "Upload", "Download", "Import", "Export", "Share", "Send", "Receive",
            "Attach", "Detach", "Copy", "Paste", "Cut", "Undo", "Redo", "Select",
            "SelectAll", "Deselect", "Group", "Ungroup", "Align", "Distribute", "Arrange",
            "Order", "BringToFront", "SendToBack", "FlipHorizontal", "FlipVertical", "Rotate",
            "Scale", "Move", "Resize", "Transform", "Crop", "Color", "Fill", "Stroke",
            "Brush", "Eraser", "Eyedropper", "Palette", "Layer", "Layers", "Folder",
            "File", "Document", "Page", "Book", "Library", "Collection", "Gallery",
            "Album", "Playlist", "Queue", "Stack", "Heap", "Tree", "Graph", "Chart",
            "Diagram", "Map", "Location", "Pin", "Marker", "Target", "Crosshair", "Aim",
            "Focus", "Blur", "Sharp", "Soft", "Light", "Dark", "Day", "Night",
            "Sun", "Moon", "Cloud", "Weather", "Temperature", "Thermometer", "Wind", "Rain",
            "Snow", "Storm", "Lightning", "Thunder", "Fire", "Water", "Earth", "Air",
            "Nature", "Plant", "Tree", "Flower", "Leaf", "Fruit", "Vegetable", "Food",
            "Drink", "Cup", "Glass", "Bottle", "Plate", "Bowl", "Utensil", "Fork",
            "Knife", "Spoon", "Cook", "Bake", "Grill", "Fry", "Boil", "Steam",
            "Microwave", "Oven", "Stove", "Refrigerator", "Freezer", "Dishwasher", "Sink", "Faucet",
            "Toilet", "Shower", "Bath", "Towel", "Soap", "Shampoo", "Toothbrush", "Toothpaste",
            "Comb", "Brush", "Razor", "Scissors", "Nail", "Hair", "Face", "Eye",
            "Ear", "Nose", "Mouth", "Tooth", "Tongue", "Lip", "Chin", "Cheek",
            "Forehead", "Head", "Neck", "Shoulder", "Arm", "Elbow", "Wrist", "Hand",
            "Finger", "Thumb", "Leg", "Knee", "Ankle", "Foot", "Toe", "Heel",
            "Chest", "Back", "Spine", "Hip", "Waist", "Stomach", "Heart", "Lung",
            "Brain", "Muscle", "Bone", "Joint", "Skin", "Blood", "Vein", "Artery",
            "Nerve", "Cell", "DNA", "RNA", "Gene", "Chromosome", "Protein", "Enzyme",
            "Hormone", "Vitamin", "Mineral", "Nutrient", "Calorie", "Fat", "Carbohydrate", "Protein",
            "Sugar", "Salt", "Spice", "Herb", "Seasoning", "Flavor", "Taste", "Smell",
            "Touch", "Feel", "Sense", "Perception", "Thought", "Idea", "Concept", "Theory",
            "Hypothesis", "Experiment", "Test", "Result", "Conclusion", "Summary", "Abstract", "Introduction",
            "Method", "Discussion", "Reference", "Citation", "Quote", "Paraphrase", "Summarize", "Analyze",
            "Synthesize", "Evaluate", "Compare", "Contrast", "Describe", "Explain", "Define", "Illustrate",
            "Demonstrate", "Prove", "Argue", "Persuade", "Convince", "Inform", "Educate", "Teach",
            "Learn", "Study", "Research", "Investigate", "Explore", "Discover", "Find", "Locate",
            "Identify", "Recognize", "Remember", "Recall", "Forget", "Understand", "Comprehend", "Interpret",
            "Translate", "Apply", "Use", "Implement", "Execute", "Perform", "Do", "Make",
            "Create", "Build", "Construct", "Design", "Develop", "Plan", "Organize", "Manage",
            "Lead", "Direct", "Guide", "Mentor", "Coach", "Train", "Instruct", "Demonstrate"
        };

        string[] commonSuffixes = { " Icon", "Icon", "" };

        foreach (var prefix in commonPrefixes)
        {
            foreach (var type in commonTypes)
            {
                foreach (var suffix in commonSuffixes)
                {
                    iconNames.Add(prefix + type + suffix);
                }
            }
        }
    }

    private Type[] GetAllUnityComponentTypes()
    {
        // Get all types that inherit from Component
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => {
                try {
                    return assembly.GetTypes();
                } catch {
                    return new Type[0];
                }
            })
            .Where(type => type.IsSubclassOf(typeof(Component)) && !type.IsAbstract)
            .ToArray();
    }

    private void LoadExtendedIcons()
    {
        if (hasLoadedExtendedIcons || isLoadingIcons) return;
        
        isLoadingIcons = true;
        
        // Try to find all editor icons using reflection
        try
        {
            // Get all static properties from EditorGUIUtility
            var editorGUIUtilityType = typeof(EditorGUIUtility);
            var properties = editorGUIUtilityType.GetProperties(BindingFlags.Public | BindingFlags.Static);
            
            foreach (var prop in properties)
            {
                if (prop.PropertyType == typeof(Texture2D) || prop.PropertyType == typeof(Texture))
                {
                    string name = prop.Name;
                    if (!iconNames.Contains(name))
                    {
                        iconNames.Add(name);
                    }
                }
            }
            
            // Try to access internal icon methods
            var getIconMethod = editorGUIUtilityType.GetMethod("GetIconForObject", 
                BindingFlags.NonPublic | BindingFlags.Static);
            
            if (getIconMethod != null)
            {
                // This is a heuristic approach - try common naming patterns
                string[] prefixes = { "", "d_" };
                string[] suffixes = { "", " Icon", "Icon", ".png", ".asset" };
                
                // Common Unity editor window names
                string[] editorWindows = {
                    "Inspector", "Scene", "Game", "Hierarchy", "Project", "Console", 
                    "Animation", "Profiler", "AssetStore", "Preferences", "Build", 
                    "Timeline", "AudioMixer", "Animator", "LightingSettings"
                };
                
                foreach (var window in editorWindows)
                {
                    foreach (var prefix in prefixes)
                    {
                        foreach (var suffix in suffixes)
                        {
                            iconNames.Add(prefix + window + suffix);
                        }
                    }
                }
            }
            
            // Try to find icons in the editor resources
            var resourcesType = typeof(EditorGUIUtility).Assembly.GetType("UnityEditor.EditorResources");
            if (resourcesType != null)
            {
                var getPathMethod = resourcesType.GetMethod("GetPath", 
                    BindingFlags.Public | BindingFlags.Static);
                
                if (getPathMethod != null)
                {
                    // Common icon paths
                    string[] iconPaths = {
                        "icons", "gizmos", "builtin skins", "packages"
                    };
                    
                    foreach (var path in iconPaths)
                    {
                        try
                        {
                            var fullPath = getPathMethod.Invoke(null, new object[] { path }) as string;
                            if (!string.IsNullOrEmpty(fullPath) && System.IO.Directory.Exists(fullPath))
                            {
                                var files = System.IO.Directory.GetFiles(fullPath, "*.png", 
                                    System.IO.SearchOption.AllDirectories);
                                
                                foreach (var file in files)
                                {
                                    var fileName = System.IO.Path.GetFileNameWithoutExtension(file);
                                    if (!string.IsNullOrEmpty(fileName))
                                    {
                                        iconNames.Add(fileName);
                                        iconNames.Add(fileName + " Icon");
                                    }
                                }
                            }
                        }
                        catch (Exception) { /* Ignore errors */ }
                    }
                }
            }
            
            // Remove duplicates
            iconNames = iconNames.Distinct().ToList();
            RefreshIconContent();
            hasLoadedExtendedIcons = true;
        }
        catch (Exception e)
        {
            Debug.LogError("Error loading extended icons: " + e.Message);
        }
        finally
        {
            isLoadingIcons = false;
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Unity Built-in Icons", EditorStyles.boldLabel);
        
        // Search field
        EditorGUILayout.BeginHorizontal();
        searchString = EditorGUILayout.TextField("Search:", searchString);
        if (GUILayout.Button("Clear", GUILayout.Width(60)))
        {
            searchString = "";
            GUI.FocusControl(null);
        }
        EditorGUILayout.EndHorizontal();
        
        // Icon size slider
        iconSize = EditorGUILayout.IntSlider("Icon Size:", iconSize, 20, 100);
        
        // Option to show only valid icons
        bool newShowOnlyValidIcons = EditorGUILayout.Toggle("Show Only Valid Icons", showOnlyValidIcons);
        if (newShowOnlyValidIcons != showOnlyValidIcons)
        {
            showOnlyValidIcons = newShowOnlyValidIcons;
        }
        
        // Button to load extended icons
        if (!hasLoadedExtendedIcons)
        {
            if (GUILayout.Button("Load Extended Icons (May Take Time)"))
            {
                LoadExtendedIcons();
            }
        }
        
        EditorGUILayout.Space();
        
        // Begin scroll view
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // Calculate grid layout
        float windowWidth = position.width - 20; // Subtract some padding
        int columns = Mathf.FloorToInt(windowWidth / (iconSize + 10));
        columns = Mathf.Max(1, columns);
        
        // Start grid
        EditorGUILayout.BeginVertical();
        int currentColumn = 0;
        
        // Filter icons based on search
        var filteredIcons = iconNames
            .Where(name => string.IsNullOrEmpty(searchString) || 
                          name.ToLower().Contains(searchString.ToLower()))
            .ToList();
        
        if (filteredIcons.Count == 0)
        {
            EditorGUILayout.HelpBox("No icons match your search.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
            
            for (int i = 0; i < filteredIcons.Count; i++)
            {
                string iconName = filteredIcons[i];
                GUIContent content = EditorGUIUtility.IconContent(iconName);
                
                // Skip if icon doesn't exist and we're only showing valid icons
                if (showOnlyValidIcons && (content == null || content.image == null))
                    continue;
                
                // Create a button with the icon
                EditorGUILayout.BeginVertical(GUILayout.Width(iconSize + 10));
                
                // Icon button
                if (GUILayout.Button(content, GUILayout.Width(iconSize), GUILayout.Height(iconSize)))
                {
                    // Copy icon name to clipboard
                    EditorGUIUtility.systemCopyBuffer = iconName;
                    Debug.Log($"Copied icon name to clipboard: {iconName}");
                }
                
                // Icon name label
                GUILayout.Label(iconName, EditorStyles.miniLabel, GUILayout.Width(iconSize + 10));
                
                EditorGUILayout.EndVertical();
                
                // Handle grid layout
                currentColumn++;
                if (currentColumn >= columns)
                {
                    currentColumn = 0;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView();
        
        // Show count of icons
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Total Icons: {iconNames.Count}, Filtered: {filteredIcons.Count}");
    }
} 