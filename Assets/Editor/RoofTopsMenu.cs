using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using RoofTops;

public class RoofTopsMenu : Editor
{
    [MenuItem("RoofTops/Reset All Bonuses")]
    private static void ResetAllBonuses()
    {
        string[] guids = AssetDatabase.FindAssets("t:GameDataObject");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameDataObject gameData = AssetDatabase.LoadAssetAtPath<GameDataObject>(path);
            if (gameData != null)
            {
                gameData.totalBonusCollected = 0;
                gameData.lastRunBonusCollected = 0;
                EditorUtility.SetDirty(gameData);
                Debug.Log($"Reset bonuses in {path}");
            }
        }
        AssetDatabase.SaveAssets();

        if (GameManager.Instance != null && GameManager.Instance.gameData != null)
        {
            BonusTextDisplay.Instance?.ResetTotal();
        }
    }

    [MenuItem("RoofTops/Reset All Game Data")]
    private static void ResetAllGameData()
    {
        string[] guids = AssetDatabase.FindAssets("t:GameDataObject");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameDataObject gameData = AssetDatabase.LoadAssetAtPath<GameDataObject>(path);
            if (gameData != null)
            {
                gameData.totalBonusCollected = 0;
                gameData.lastRunBonusCollected = 0;
                gameData.bestDistance = 0;
                gameData.lastRunDistance = 0;
                EditorUtility.SetDirty(gameData);
                Debug.Log($"Reset all data in {path}");
            }
        }
        AssetDatabase.SaveAssets();

        if (GameManager.Instance != null && GameManager.Instance.gameData != null)
        {
            BonusTextDisplay.Instance?.ResetTotal();
        }
    }

    [MenuItem("RoofTops/Setup Ad Testing")]
    private static void SetupAdTesting()
    {
        // Create Resources folder if it doesn't exist
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
            Debug.Log("Created Resources folder");
        }

        string sourcePath = "Assets/Mobile Monetization Pro/Tools/MobileMonetization_UnityAdsManager/Prefab/MobileMonetizationPro_UnityAdsManager.prefab";
        string destPath = "Assets/Resources/MobileMonetizationPro_UnityAdsManager.prefab";
        
        if (AssetDatabase.CopyAsset(sourcePath, destPath))
        {
            Debug.Log($"Successfully copied prefab from {sourcePath} to {destPath}");
        }
        else
        {
            Debug.LogError($"Failed to copy prefab from {sourcePath} to {destPath}");
        }
        
        AssetDatabase.Refresh();
    }

    [MenuItem("RoofTops/Create Death Screen Test")]
    private static void CreateDeathScreenTest()
    {
        // Create test scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Create canvas
        var canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvasComp = canvas.GetComponent<Canvas>();
        canvasComp.renderMode = RenderMode.ScreenSpaceOverlay;

        // Create death screen panel
        var panel = new GameObject("Death Panel", typeof(CanvasGroup));
        panel.transform.SetParent(canvas.transform, false);

        // Create buttons
        CreateDeathButton(panel, "Restart Run", new Vector2(0, 100), true);
        CreateDeathButton(panel, "Smart Advance", new Vector2(0, 0), true);
        CreateDeathButton(panel, "Use Skip Token (0)", new Vector2(0, -100), false);
        CreateDeathButton(panel, "Continue from Here", new Vector2(0, -200), false);

        // Add camera
        var camera = new GameObject("Main Camera", typeof(Camera));
        camera.tag = "MainCamera";

        // Add ads manager
        var adsObj = new GameObject("Ads");
        adsObj.AddComponent<GameAdsManager>();
    }

    [MenuItem("RoofTops/Add Death Screen Buttons")]
    private static void AddDeathScreenButtons()
    {
        // Find existing canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No canvas found in scene. Please make sure you have a canvas.");
            return;
        }

        // Create death screen panel
        var panel = new GameObject("Death Panel", typeof(CanvasGroup));
        panel.transform.SetParent(canvas.transform, false);

        // Create buttons
        CreateDeathButton(panel, "Restart Run", new Vector2(0, 100), true);
        CreateDeathButton(panel, "Smart Advance", new Vector2(0, 0), true);
        CreateDeathButton(panel, "Use Skip Token (0)", new Vector2(0, -100), false);
        CreateDeathButton(panel, "Continue from Here", new Vector2(0, -200), false);

        // Add ads manager if not present
        if (FindFirstObjectByType<GameAdsManager>() == null)
        {
            var adsObj = new GameObject("Ads");
            adsObj.AddComponent<GameAdsManager>();
        }

        Debug.Log("Added death screen buttons to canvas");
    }

    [MenuItem("RoofTops/Add Death Screen Buttons 3D")]
    private static void AddDeathScreenButtons3D()
    {
        // Create parent object for all buttons
        var buttonHolder = new GameObject("Death Buttons");
        buttonHolder.transform.position = new Vector3(0, 2, 2); // Position in front of camera

        // Create 3D buttons
        Create3DButton(buttonHolder, "Restart Run", new Vector3(0, 0.4f, 0), true);
        Create3DButton(buttonHolder, "Smart Advance", new Vector3(0, 0, 0), true);
        Create3DButton(buttonHolder, "Use Skip Token (0)", new Vector3(0, -0.4f, 0), false);
        Create3DButton(buttonHolder, "Continue from Here", new Vector3(0, -0.8f, 0), false);

        // Add ads manager if not present
        if (FindFirstObjectByType<GameAdsManager>() == null)
        {
            var adsObj = new GameObject("Ads");
            adsObj.AddComponent<GameAdsManager>();
        }
    }

    [MenuItem("RoofTops/Add Game Over Buttons")]
    private static void AddGameOverButtons()
    {
        var controller = FindFirstObjectByType<GameOverUIController>();
        if (controller == null || controller.gameOverPanel == null)
        {
            Debug.LogError("GameOverUIController or gameOverPanel not found in scene");
            return;
        }

        // Create buttons with your style
        var rooftop = CreateDeathButton(controller.gameOverPanel, "ROOFTOP", new Vector2(0, -50), true);
        AddSubtext(rooftop, "play again");

        var smartAdvance = CreateDeathButton(controller.gameOverPanel, "SMART ADVANCE", new Vector2(0, -150), true);
        AddSubtext(smartAdvance, "want to watch in advance?");

        var bonusSkip = CreateDeathButton(controller.gameOverPanel, "BONUS SKIP", new Vector2(0, -250), false);
        AddSubtext(bonusSkip, "use your bonus to skip watching");

        var continueButton = CreateDeathButton(controller.gameOverPanel, "CONTINUE", new Vector2(0, -350), false);
        AddSubtext(continueButton, "last ad of this row, watch to continue");
    }

    private static void AddSubtext(GameObject button, string subtext)
    {
        var subtextObj = new GameObject("Subtext", typeof(TextMeshProUGUI));
        subtextObj.transform.SetParent(button.transform, false);
        
        var tmpText = subtextObj.GetComponent<TextMeshProUGUI>();
        tmpText.text = subtext;
        tmpText.fontSize = 16;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = Color.gray;

        var rect = subtextObj.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(0, -20);
        rect.sizeDelta = new Vector2(200, 20);
    }

    private static GameObject CreateButton(GameObject parent, string name, Vector2 position, string text)
    {
        var buttonObj = new GameObject(name, typeof(Button), typeof(Image));
        buttonObj.transform.SetParent(parent.transform, false);
        
        var textObj = new GameObject("Text", typeof(TextMeshProUGUI));
        textObj.transform.SetParent(buttonObj.transform, false);
        
        var buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchoredPosition = position;
        buttonRect.sizeDelta = new Vector2(160, 30);
        
        var tmpText = textObj.GetComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.fontSize = 24;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = Color.black;

        var buttonImage = buttonObj.GetComponent<Image>();
        buttonImage.color = Color.white;
        
        return buttonObj;
    }

    private static GameObject CreateDeathButton(GameObject parent, string text, Vector2 position, bool interactable)
    {
        var button = CreateButton(parent, text + "Button", position, text);
        button.GetComponent<Button>().interactable = interactable;
        
        if (!interactable)
        {
            var image = button.GetComponent<Image>();
            var color = image.color;
            color.a = 0.5f;
            image.color = color;
        }

        return button;
    }

    private static GameObject Create3DButton(GameObject parent, string text, Vector3 position, bool interactable)
    {
        // Create button object
        var button = GameObject.CreatePrimitive(PrimitiveType.Cube);
        button.name = text + "Button";
        button.transform.SetParent(parent.transform);
        button.transform.localPosition = position;
        button.transform.localScale = new Vector3(2f, 0.3f, 0.1f);

        // Add text
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(button.transform);
        textObj.transform.localPosition = new Vector3(0, 0, -0.06f);
        textObj.transform.localRotation = Quaternion.identity;

        var textMesh = textObj.AddComponent<TextMeshPro>();
        textMesh.text = text;
        textMesh.fontSize = 2;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.color = Color.black;

        // Set material based on interactable state
        var renderer = button.GetComponent<Renderer>();
        if (!interactable)
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            renderer.material = material;
        }
        else
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = Color.white;
            renderer.material = material;
        }

        return button;
    }
}