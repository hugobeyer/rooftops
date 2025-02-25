using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using RoofTops;
using UnityEngine.Events;

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

    [MenuItem("RoofTops/Setup Achievement System")]
    private static void SetupAchievementSystem()
    {
        // Find canvas or create one if it doesn't exist
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.Log("No canvas found. Creating a new canvas.");
            canvas = CreateCanvas();
        }
        
        // Create Achievement System GameObject
        GameObject achievementSystemObj = new GameObject("AchievementSystem");
        AchievementSystem achievementSystem = achievementSystemObj.AddComponent<AchievementSystem>();
        Debug.Log("Created Achievement System GameObject");
        
        // Create Achievement Tracker
        GameObject trackerObj = new GameObject("AchievementTracker");
        trackerObj.AddComponent<AchievementTracker>();
        Debug.Log("Created Achievement Tracker GameObject");
        
        // Create Achievement UI Prefabs
        CreateAchievementUIPrefabs(canvas);
        
        // Select the Achievement System in the hierarchy
        Selection.activeGameObject = achievementSystemObj;
        
        // Save the prefabs
        SaveAchievementPrefabs();
        
        Debug.Log("Achievement System setup complete!");
    }
    
    private static Canvas CreateCanvas()
    {
        GameObject canvasObj = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        // Add EventSystem if it doesn't exist
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem", 
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }
        
        return canvas;
    }
    
    private static void CreateAchievementUIPrefabs(Canvas canvas)
    {
        // Create Achievement Panel
        GameObject achievementPanel = new GameObject("AchievementPanel", typeof(RectTransform), typeof(CanvasGroup));
        achievementPanel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = achievementPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.offsetMin = new Vector2(50, 50);
        panelRect.offsetMax = new Vector2(-50, -50);
        
        // Add background image
        Image panelBg = achievementPanel.AddComponent<Image>();
        panelBg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        
        // Create header
        GameObject header = new GameObject("Header", typeof(RectTransform));
        header.transform.SetParent(achievementPanel.transform, false);
        RectTransform headerRect = header.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 1);
        headerRect.anchorMax = new Vector2(1, 1);
        headerRect.pivot = new Vector2(0.5f, 1);
        headerRect.sizeDelta = new Vector2(0, 60);
        
        // Add header text
        GameObject headerText = new GameObject("HeaderText", typeof(TextMeshProUGUI));
        headerText.transform.SetParent(header.transform, false);
        TextMeshProUGUI headerTmp = headerText.GetComponent<TextMeshProUGUI>();
        headerTmp.text = "ACHIEVEMENTS";
        headerTmp.fontSize = 36;
        headerTmp.alignment = TextAlignmentOptions.Center;
        headerTmp.color = Color.white;
        RectTransform headerTextRect = headerText.GetComponent<RectTransform>();
        headerTextRect.anchorMin = Vector2.zero;
        headerTextRect.anchorMax = Vector2.one;
        headerTextRect.offsetMin = Vector2.zero;
        headerTextRect.offsetMax = Vector2.zero;
        
        // Create close button
        GameObject closeButton = CreateButton(header, "CloseButton", new Vector2(40, -30), "X");
        closeButton.GetComponent<RectTransform>().anchorMin = new Vector2(1, 0.5f);
        closeButton.GetComponent<RectTransform>().anchorMax = new Vector2(1, 0.5f);
        closeButton.GetComponent<RectTransform>().pivot = new Vector2(1, 0.5f);
        closeButton.GetComponent<RectTransform>().sizeDelta = new Vector2(40, 40);
        
        // Create scroll view for achievements
        GameObject scrollView = new GameObject("ScrollView", typeof(ScrollRect));
        scrollView.transform.SetParent(achievementPanel.transform, false);
        ScrollRect scrollRect = scrollView.GetComponent<ScrollRect>();
        RectTransform scrollRectTransform = scrollView.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0, 0);
        scrollRectTransform.anchorMax = new Vector2(1, 1);
        scrollRectTransform.offsetMin = new Vector2(10, 10);
        scrollRectTransform.offsetMax = new Vector2(-10, -70);
        
        // Create viewport
        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
        viewport.transform.SetParent(scrollView.transform, false);
        viewport.GetComponent<Image>().color = new Color(1, 1, 1, 0.05f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        
        // Create content container
        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        
        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 0);
        
        // Set up scroll rect references
        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        
        // Create achievement item prefab
        GameObject achievementItem = CreateAchievementItemPrefab();
        
        // Create notification prefab
        GameObject notificationPrefab = CreateNotificationPrefab();
        
        // Create notification container
        GameObject notificationContainer = new GameObject("NotificationContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
        notificationContainer.transform.SetParent(canvas.transform, false);
        RectTransform notificationContainerRect = notificationContainer.GetComponent<RectTransform>();
        notificationContainerRect.anchorMin = new Vector2(1, 1);
        notificationContainerRect.anchorMax = new Vector2(1, 1);
        notificationContainerRect.pivot = new Vector2(1, 1);
        notificationContainerRect.anchoredPosition = new Vector2(-20, -20);
        notificationContainerRect.sizeDelta = new Vector2(300, 400);
        
        VerticalLayoutGroup notificationLayout = notificationContainer.GetComponent<VerticalLayoutGroup>();
        notificationLayout.padding = new RectOffset(0, 0, 0, 0);
        notificationLayout.spacing = 10;
        notificationLayout.childAlignment = TextAnchor.UpperRight;
        notificationLayout.childControlHeight = false;
        notificationLayout.childControlWidth = false;
        notificationLayout.childForceExpandHeight = false;
        notificationLayout.childForceExpandWidth = false;
        
        // Create toggle button for achievement panel
        GameObject toggleButton = CreateButton(canvas.gameObject, "AchievementToggle", new Vector2(100, -50), "🏆");
        toggleButton.GetComponent<RectTransform>().anchorMin = new Vector2(1, 1);
        toggleButton.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
        toggleButton.GetComponent<RectTransform>().pivot = new Vector2(1, 1);
        toggleButton.GetComponent<RectTransform>().sizeDelta = new Vector2(60, 60);
        
        // Add AchievementUI component
        AchievementUI achievementUI = canvas.gameObject.AddComponent<AchievementUI>();
        achievementUI.achievementListContainer = content.transform;
        achievementUI.achievementItemPrefab = achievementItem;
        achievementUI.achievementNotificationPrefab = notificationPrefab;
        achievementUI.notificationContainer = notificationContainer.transform;
        achievementUI.achievementPanelToggleButton = toggleButton.GetComponent<Button>();
        achievementUI.achievementPanel = achievementPanel;
        
        // Hide panel by default
        achievementPanel.SetActive(false);
    }
    
    private static GameObject CreateAchievementItemPrefab()
    {
        GameObject item = new GameObject("AchievementItem", typeof(RectTransform), typeof(Image));
        RectTransform itemRect = item.GetComponent<RectTransform>();
        itemRect.sizeDelta = new Vector2(0, 80);
        
        Image itemBg = item.GetComponent<Image>();
        itemBg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
        // Title
        GameObject titleObj = new GameObject("Title", typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(item.transform, false);
        TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
        titleText.text = "Achievement Title";
        titleText.fontSize = 18;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = Color.white;
        
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.offsetMin = new Vector2(60, -30);
        titleRect.offsetMax = new Vector2(-10, -5);
        
        // Description
        GameObject descObj = new GameObject("Description", typeof(TextMeshProUGUI));
        descObj.transform.SetParent(item.transform, false);
        TextMeshProUGUI descText = descObj.GetComponent<TextMeshProUGUI>();
        descText.text = "Achievement description goes here";
        descText.fontSize = 14;
        descText.color = new Color(0.8f, 0.8f, 0.8f);
        
        RectTransform descRect = descObj.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0, 0);
        descRect.anchorMax = new Vector2(1, 1);
        descRect.pivot = new Vector2(0.5f, 0.5f);
        descRect.offsetMin = new Vector2(60, 5);
        descRect.offsetMax = new Vector2(-10, -30);
        
        // Icon background
        GameObject iconBg = new GameObject("IconBackground", typeof(RectTransform), typeof(Image));
        iconBg.transform.SetParent(item.transform, false);
        iconBg.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f);
        
        RectTransform iconBgRect = iconBg.GetComponent<RectTransform>();
        iconBgRect.anchorMin = new Vector2(0, 0.5f);
        iconBgRect.anchorMax = new Vector2(0, 0.5f);
        iconBgRect.pivot = new Vector2(0.5f, 0.5f);
        iconBgRect.anchoredPosition = new Vector2(30, 0);
        iconBgRect.sizeDelta = new Vector2(50, 50);
        
        // Progress bar background
        GameObject progressBg = new GameObject("ProgressBarBackground", typeof(RectTransform), typeof(Image));
        progressBg.transform.SetParent(item.transform, false);
        progressBg.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f);
        
        RectTransform progressBgRect = progressBg.GetComponent<RectTransform>();
        progressBgRect.anchorMin = new Vector2(0, 0);
        progressBgRect.anchorMax = new Vector2(1, 0);
        progressBgRect.pivot = new Vector2(0.5f, 0);
        progressBgRect.offsetMin = new Vector2(10, 2);
        progressBgRect.offsetMax = new Vector2(-10, 7);
        
        // Progress bar fill
        GameObject progressFill = new GameObject("ProgressBar", typeof(RectTransform), typeof(Image));
        progressFill.transform.SetParent(progressBg.transform, false);
        Image progressFillImage = progressFill.GetComponent<Image>();
        progressFillImage.color = new Color(0.2f, 0.7f, 1f);
        progressFillImage.type = Image.Type.Filled;
        progressFillImage.fillMethod = Image.FillMethod.Horizontal;
        progressFillImage.fillAmount = 0.5f;
        
        RectTransform progressFillRect = progressFill.GetComponent<RectTransform>();
        progressFillRect.anchorMin = Vector2.zero;
        progressFillRect.anchorMax = Vector2.one;
        progressFillRect.offsetMin = Vector2.zero;
        progressFillRect.offsetMax = Vector2.zero;
        
        // Completed icon
        GameObject completedIcon = new GameObject("CompletedIcon", typeof(RectTransform), typeof(Image));
        completedIcon.transform.SetParent(item.transform, false);
        completedIcon.GetComponent<Image>().color = Color.green;
        
        RectTransform completedIconRect = completedIcon.GetComponent<RectTransform>();
        completedIconRect.anchorMin = new Vector2(1, 1);
        completedIconRect.anchorMax = new Vector2(1, 1);
        completedIconRect.pivot = new Vector2(1, 1);
        completedIconRect.anchoredPosition = new Vector2(-5, -5);
        completedIconRect.sizeDelta = new Vector2(20, 20);
        
        return item;
    }
    
    private static GameObject CreateNotificationPrefab()
    {
        GameObject notification = new GameObject("AchievementNotification", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        notification.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        notification.GetComponent<CanvasGroup>().alpha = 1f;
        
        RectTransform notificationRect = notification.GetComponent<RectTransform>();
        notificationRect.sizeDelta = new Vector2(300, 80);
        
        // Header
        GameObject header = new GameObject("Header", typeof(RectTransform), typeof(Image));
        header.transform.SetParent(notification.transform, false);
        header.GetComponent<Image>().color = new Color(0.2f, 0.7f, 1f);
        
        RectTransform headerRect = header.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 1);
        headerRect.anchorMax = new Vector2(1, 1);
        headerRect.pivot = new Vector2(0.5f, 1);
        headerRect.sizeDelta = new Vector2(0, 25);
        
        // Achievement unlocked text
        GameObject unlockedText = new GameObject("UnlockedText", typeof(TextMeshProUGUI));
        unlockedText.transform.SetParent(header.transform, false);
        TextMeshProUGUI unlockedTmp = unlockedText.GetComponent<TextMeshProUGUI>();
        unlockedTmp.text = "ACHIEVEMENT UNLOCKED!";
        unlockedTmp.fontSize = 14;
        unlockedTmp.fontStyle = FontStyles.Bold;
        unlockedTmp.alignment = TextAlignmentOptions.Center;
        unlockedTmp.color = Color.white;
        
        RectTransform unlockedRect = unlockedText.GetComponent<RectTransform>();
        unlockedRect.anchorMin = Vector2.zero;
        unlockedRect.anchorMax = Vector2.one;
        unlockedRect.offsetMin = Vector2.zero;
        unlockedRect.offsetMax = Vector2.zero;
        
        // Title
        GameObject title = new GameObject("Title", typeof(TextMeshProUGUI));
        title.transform.SetParent(notification.transform, false);
        TextMeshProUGUI titleTmp = title.GetComponent<TextMeshProUGUI>();
        titleTmp.text = "Achievement Title";
        titleTmp.fontSize = 18;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = Color.white;
        
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -30);
        titleRect.sizeDelta = new Vector2(-20, 25);
        
        // Description
        GameObject description = new GameObject("Description", typeof(TextMeshProUGUI));
        description.transform.SetParent(notification.transform, false);
        TextMeshProUGUI descTmp = description.GetComponent<TextMeshProUGUI>();
        descTmp.text = "Achievement description goes here";
        descTmp.fontSize = 14;
        descTmp.alignment = TextAlignmentOptions.Center;
        descTmp.color = new Color(0.8f, 0.8f, 0.8f);
        
        RectTransform descRect = description.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0, 0);
        descRect.anchorMax = new Vector2(1, 0);
        descRect.pivot = new Vector2(0.5f, 0);
        descRect.anchoredPosition = new Vector2(0, 10);
        descRect.sizeDelta = new Vector2(-20, 40);
        
        return notification;
    }
    
    private static void SaveAchievementPrefabs()
    {
        // Create prefabs directory if it doesn't exist
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
        }
        
        // Find the objects we created
        AchievementUI achievementUI = FindFirstObjectByType<AchievementUI>();
        if (achievementUI == null)
        {
            Debug.LogError("Could not find AchievementUI component");
            return;
        }
        
        // Save achievement item prefab
        if (achievementUI.achievementItemPrefab != null)
        {
            string itemPath = "Assets/Prefabs/UI/AchievementItem.prefab";
            PrefabUtility.SaveAsPrefabAsset(achievementUI.achievementItemPrefab, itemPath);
            Debug.Log($"Saved achievement item prefab to {itemPath}");
        }
        
        // Save notification prefab
        if (achievementUI.achievementNotificationPrefab != null)
        {
            string notificationPath = "Assets/Prefabs/UI/AchievementNotification.prefab";
            PrefabUtility.SaveAsPrefabAsset(achievementUI.achievementNotificationPrefab, notificationPath);
            Debug.Log($"Saved achievement notification prefab to {notificationPath}");
        }
    }
}