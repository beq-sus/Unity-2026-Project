using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>
/// Generates the rounded UI sprites and builds Assets/Scenes/MainMenu.unity.
/// Run from: Tools > Doomspire > Build Main Menu Scene. Safe to run again (rebuilds the scene).
/// </summary>
public static class MainMenuBuilder
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string ArtDir = "Assets/UI/MainMenu";
    private const string GenDir = "Assets/UI/MainMenu/Generated";
    private const string FallbackFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    private const float ButtonWidth = 340f;
    private const float ButtonHeight = 100f;
    private const float Slant = 115f;

    private static readonly Color PanelPurple = Hex("#3A1446");
    private static readonly Color PanelEdge = Hex("#5E2470");
    private static readonly Color ButtonPurple = Hex("#9B30D9");
    private static readonly Color FrameOrange = Hex("#C8691E");
    private static readonly Color FrameDark = Hex("#4A2208");
    private static readonly Color Background = Hex("#12081A");
    private static readonly Color TextOutline = Hex("#3C0E57");
    private static readonly Color ControlDark = Hex("#1E0A26");
    private static readonly Color ItemNormal = Hex("#2E1239");

    private static Sprite pillShaded, pillFlat, roundedRect, arrowDown;
    private static Material buttonTextMaterial;
    private static TMP_FontAsset font;

    [MenuItem("Tools/Doomspire/Build Main Menu Scene")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Main Menu Builder", "Exit Play Mode first.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Sprite logo = LoadArtSprite("logo.png");
        Sprite tower = LoadArtSprite("tower_art.png");
        if (logo == null || tower == null)
        {
            EditorUtility.DisplayDialog("Main Menu Builder", $"logo.png / tower_art.png not found in {ArtDir}.", "OK");
            return;
        }

        GenerateAssets();

        // Build into the existing MainMenu scene (keeps its settings and other objects); create it only if missing.
        var scene = File.Exists(ScenePath)
            ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
            : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Re-running replaces only the menu canvas built last time.
        foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "MainMenuCanvas"))
            Object.DestroyImmediate(old);

        SetupCamera();
        if (Object.FindAnyObjectByType<EventSystem>() == null)
            CreateEventSystem();

        // ---- Canvas ----
        var canvasGo = new GameObject("MainMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.layer = LayerMask.NameToLayer("UI");
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f; // keep vertical layout stable; wider screens just get more art
        var menu = canvasGo.AddComponent<MainMenu>();
        Transform root = canvasGo.transform;

        // ---- Background ----
        var bg = AddImage("Background", root, null, Background);
        Stretch(bg.rectTransform);
        bg.raycastTarget = false;

        // ---- Right: tower art in orange rounded frame ----
        // Left edge sits under the slanted panel, so the frame appears to start at the slant.
        var frameOuter = AddImage("ArtFrame", root, roundedRect, FrameDark);
        frameOuter.type = Image.Type.Sliced;
        frameOuter.raycastTarget = false;
        SetAnchors(frameOuter.rectTransform, new Vector2(0.36f, 0f), Vector2.one, new Vector2(0, 16), new Vector2(-16, -16));

        var frame = AddImage("Border", frameOuter.transform, roundedRect, FrameOrange);
        frame.type = Image.Type.Sliced;
        frame.raycastTarget = false;
        Stretch(frame.rectTransform, 4f);

        var mask = AddImage("ArtMask", frame.transform, roundedRect, Color.white);
        mask.type = Image.Type.Sliced;
        mask.pixelsPerUnitMultiplier = 1.6f; // smaller inner corner radius
        mask.raycastTarget = false;
        mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        Stretch(mask.rectTransform, 12f);

        var art = AddImage("TowerArt", mask.transform, tower, Color.white);
        art.raycastTarget = false;
        Stretch(art.rectTransform);
        var fitter = art.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; // fill and crop, never letterbox
        fitter.aspectRatio = tower.rect.width / tower.rect.height;

        // ---- Left: slanted purple panel (edge strip behind it gives the lighter border line) ----
        var edge = AddSlanted("LeftPanelEdge", root, PanelEdge);
        SetAnchors(edge.rectTransform, Vector2.zero, new Vector2(0.45f, 1f), Vector2.zero, new Vector2(10f, 0f));
        edge.SetGradient(Color.white, new Color(0.8f, 0.8f, 0.8f, 1f));

        var panel = AddSlanted("LeftPanel", root, PanelPurple);
        SetAnchors(panel.rectTransform, Vector2.zero, new Vector2(0.45f, 1f), Vector2.zero, Vector2.zero);
        panel.SetGradient(new Color(1.25f, 1.25f, 1.25f, 1f), new Color(0.65f, 0.65f, 0.65f, 1f));

        // Content area = the part of the panel left of the slant.
        var content = NewRect("MenuContent", root);
        SetAnchors(content, Vector2.zero, new Vector2(0.42f, 1f), Vector2.zero, Vector2.zero);

        var logoImg = AddImage("Logo", content, logo, Color.white);
        logoImg.preserveAspect = true;
        logoImg.raycastTarget = false;
        var logoRt = logoImg.rectTransform;
        logoRt.anchorMin = new Vector2(0.04f, 1f);
        logoRt.anchorMax = new Vector2(0.98f, 1f);
        logoRt.pivot = new Vector2(0.5f, 1f);
        logoRt.sizeDelta = new Vector2(0f, 240f);
        logoRt.anchoredPosition = new Vector2(0f, -95f);

        var buttons = NewRect("Buttons", content);
        buttons.anchorMin = buttons.anchorMax = new Vector2(0.5f, 0.37f);
        buttons.sizeDelta = new Vector2(ButtonWidth + 40f, ButtonHeight * 4 + 50f * 3);
        var layout = buttons.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 50f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;

        AddMenuButton("PLAY", buttons, menu.Play);
        AddMenuButton("OPTIONS", buttons, menu.OpenOptions);
        AddMenuButton("CREDITS", buttons, menu.OpenCredits);
        AddMenuButton("EXIT", buttons, menu.Exit);

        // ---- Placeholder panels (on top, hidden) ----
        var options = AddOptionsPanel(root, menu);
        var credits = AddPlaceholderPanel("CreditsPanel", root, "CREDITS", "DOOMSPIRE\n\nMade by the team.", menu);

        var so = new SerializedObject(menu);
        so.FindProperty("optionsPanel").objectReferenceValue = options;
        so.FindProperty("creditsPanel").objectReferenceValue = credits;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings();
        Debug.Log($"[MainMenuBuilder] Built {ScenePath}");
    }

    // ------------------------------------------------------------------ UI pieces

    private static void AddMenuButton(string label, Transform parent, UnityAction onClick)
    {
        var img = AddImage(Capitalize(label) + "Button", parent, pillShaded, ButtonPurple);
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = pillShaded.rect.height / ButtonHeight; // caps = perfect half circles

        var le = img.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = ButtonWidth;
        le.preferredHeight = ButtonHeight;
        img.rectTransform.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);

        ConfigureButton(img, onClick);
    }

    private static Button ConfigureButton(Image img, UnityAction onClick)
    {
        var shadow = img.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.08f, 0.01f, 0.12f, 0.75f);
        shadow.effectDistance = new Vector2(0f, -6f);

        // Glossy highlight on the upper half.
        var gloss = AddImage("Gloss", img.transform, pillFlat, new Color(1f, 1f, 1f, 0.22f));
        gloss.type = Image.Type.Sliced;
        gloss.raycastTarget = false;
        SetAnchors(gloss.rectTransform, new Vector2(0.07f, 0.54f), new Vector2(0.93f, 0.88f), Vector2.zero, Vector2.zero);
        gloss.pixelsPerUnitMultiplier = pillFlat.rect.height / (ButtonHeight * 0.34f);

        var text = AddText("Label", img.transform, img.gameObject.name.Replace("Button", "").ToUpperInvariant(), 46f);
        text.fontSharedMaterial = buttonTextMaterial;
        text.characterSpacing = 2f;
        Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(0f, 4f); // sit slightly above the bottom lip

        var button = img.gameObject.AddComponent<Button>();
        button.targetGraphic = img;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        UnityEventTools.AddPersistentListener(button.onClick, onClick);

        img.gameObject.AddComponent<ButtonHoverScale>();
        return button;
    }

    // ------------------------------------------------------------------ panels

    /// <summary>Dimmed full-screen blocker + rounded window with a title. Returns the window.</summary>
    private static RectTransform AddPanelShell(string name, Transform parent, string title, Vector2 size, out GameObject panel)
    {
        var dim = AddImage(name, parent, null, new Color(0f, 0f, 0f, 0.65f)); // raycastTarget blocks clicks behind
        Stretch(dim.rectTransform);

        var window = AddImage("Window", dim.transform, roundedRect, PanelPurple);
        window.type = Image.Type.Sliced;
        var outline = window.gameObject.AddComponent<Outline>();
        outline.effectColor = FrameOrange;
        outline.effectDistance = new Vector2(4f, -4f);
        window.rectTransform.sizeDelta = size;

        var titleText = AddText("Title", window.transform, title, 64f);
        titleText.fontSharedMaterial = buttonTextMaterial;
        var trt = titleText.rectTransform;
        trt.anchorMin = new Vector2(0f, 1f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.sizeDelta = new Vector2(0f, 110f);
        trt.anchoredPosition = new Vector2(0f, -30f);

        panel = dim.gameObject;
        return window.rectTransform;
    }

    /// <summary>Pill button anchored to the bottom-center of its parent.</summary>
    private static Button AddPillButton(string name, Transform parent, Color color, Vector2 position, UnityAction onClick,
        float width = ButtonWidth)
    {
        var img = AddImage(name, parent, pillShaded, color);
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = pillShaded.rect.height / ButtonHeight;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(width, ButtonHeight);
        rt.anchoredPosition = position;
        return ConfigureButton(img, onClick);
    }

    private static GameObject AddPlaceholderPanel(string name, Transform parent, string title, string body, MainMenu menu)
    {
        var window = AddPanelShell(name, parent, title, new Vector2(900f, 560f), out var panel);

        var bodyText = AddText("Body", window, body, 36f);
        bodyText.fontStyle = FontStyles.Normal;
        SetAnchors(bodyText.rectTransform, new Vector2(0.08f, 0.32f), new Vector2(0.92f, 0.72f), Vector2.zero, Vector2.zero);

        AddPillButton("BackButton", window, ButtonPurple, new Vector2(0f, 45f), menu.ClosePanels);

        panel.SetActive(false);
        return panel;
    }

    private static GameObject AddOptionsPanel(Transform parent, MainMenu menu)
    {
        var window = AddPanelShell("OptionsPanel", parent, "OPTIONS", new Vector2(1000f, 760f), out var panel);
        var options = panel.AddComponent<OptionsMenu>();

        const float rowHeight = 72f, rowSpacing = 18f;
        var rows = NewRect("Rows", window);
        rows.anchorMin = new Vector2(0f, 1f);
        rows.anchorMax = new Vector2(1f, 1f);
        rows.pivot = new Vector2(0.5f, 1f);
        rows.sizeDelta = new Vector2(-160f, rowHeight * 4 + rowSpacing * 3); // 80 px side margins
        rows.anchoredPosition = new Vector2(0f, -160f);
        var layout = rows.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = rowSpacing;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fullscreen = AddToggleRow(rows, "Fullscreen", rowHeight, options.SetFullscreen);
        var resolution = AddDropdownRow(rows, "Resolution", rowHeight, options.SetResolution);
        var damage = AddToggleRow(rows, "Show Damage Numbers", rowHeight, options.SetShowDamageNumbers);
        var shake = AddToggleRow(rows, "Screen Shake", rowHeight, options.SetScreenShake);

        const float smallButton = 270f;
        AddPillButton("ResetButton", window, FrameOrange, new Vector2(-310f, 45f), options.ResetToDefaults, smallButton);
        var apply = AddPillButton("ApplyButton", window, ButtonPurple, new Vector2(0f, 45f), options.Apply, smallButton);
        AddPillButton("BackButton", window, ButtonPurple, new Vector2(310f, 45f), menu.ClosePanels, smallButton);

        var so = new SerializedObject(options);
        so.FindProperty("fullscreenToggle").objectReferenceValue = fullscreen;
        so.FindProperty("resolutionDropdown").objectReferenceValue = resolution;
        so.FindProperty("damageNumbersToggle").objectReferenceValue = damage;
        so.FindProperty("screenShakeToggle").objectReferenceValue = shake;
        so.FindProperty("applyButton").objectReferenceValue = apply;
        so.ApplyModifiedPropertiesWithoutUndo();

        panel.SetActive(false);
        return panel;
    }

    /// <summary>Row with a left-aligned label; the control goes on the right.</summary>
    private static RectTransform AddRow(Transform parent, string label, float height)
    {
        var row = NewRect(label.Replace(" ", "") + "Row", parent);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = height;

        var text = AddText("Label", row, label, 36f);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        SetAnchors(text.rectTransform, Vector2.zero, new Vector2(0.58f, 1f), Vector2.zero, Vector2.zero);
        return row;
    }

    private static void AnchorRight(RectTransform rt, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
    }

    private static Toggle AddToggleRow(Transform parent, string label, float height, UnityAction<bool> onChanged)
    {
        var row = AddRow(parent, label, height);

        var box = AddImage("Toggle", row, roundedRect, ControlDark);
        box.type = Image.Type.Sliced;
        box.pixelsPerUnitMultiplier = 2f;
        AnchorRight(box.rectTransform, new Vector2(60f, 60f));
        var outline = box.gameObject.AddComponent<Outline>();
        outline.effectColor = FrameOrange;
        outline.effectDistance = new Vector2(2f, -2f);

        var check = AddImage("Checkmark", box.transform, roundedRect, ButtonPurple);
        check.type = Image.Type.Sliced;
        check.pixelsPerUnitMultiplier = 3f;
        check.raycastTarget = false;
        Stretch(check.rectTransform, 9f);

        var toggle = box.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = box;
        toggle.graphic = check;
        toggle.isOn = true;
        var colors = toggle.colors;
        colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        toggle.colors = colors;
        UnityEventTools.AddPersistentListener(toggle.onValueChanged, onChanged);

        box.gameObject.AddComponent<ButtonHoverScale>();
        return toggle;
    }

    private static TMP_Dropdown AddDropdownRow(Transform parent, string label, float height, UnityAction<int> onChanged)
    {
        var row = AddRow(parent, label, height);

        var go = TMP_DefaultControls.CreateDropdown(new TMP_DefaultControls.Resources
        {
            standard = roundedRect,
            background = roundedRect,
            inputField = roundedRect,
            knob = roundedRect,
            checkmark = roundedRect,
            dropdown = arrowDown,
            mask = roundedRect,
        });
        go.name = label.Replace(" ", "") + "Dropdown";
        go.transform.SetParent(row, false);
        SetLayerRecursive(go, LayerMask.NameToLayer("UI"));

        var dd = go.GetComponent<TMP_Dropdown>();
        AnchorRight((RectTransform)go.transform, new Vector2(400f, 64f));

        var img = go.GetComponent<Image>();
        img.color = ControlDark;
        img.pixelsPerUnitMultiplier = 2f;
        var outline = go.AddComponent<Outline>();
        outline.effectColor = FrameOrange;
        outline.effectDistance = new Vector2(2f, -2f);

        StyleText(dd.captionText, 32f);
        dd.captionText.alignment = TextAlignmentOptions.MidlineLeft;
        dd.captionText.rectTransform.offsetMin = new Vector2(24f, 0f);
        dd.captionText.rectTransform.offsetMax = new Vector2(-64f, 0f);

        var arrow = (RectTransform)go.transform.Find("Arrow");
        if (arrow != null)
        {
            arrow.sizeDelta = new Vector2(28f, 28f);
            arrow.anchoredPosition = new Vector2(-30f, 0f);
            arrow.GetComponent<Image>().color = Color.white;
        }

        // Drop-down list
        var template = dd.template;
        template.sizeDelta = new Vector2(0f, 330f);
        var templateImg = template.GetComponent<Image>();
        templateImg.color = ControlDark;
        templateImg.pixelsPerUnitMultiplier = 2f;
        var viewportImg = template.Find("Viewport")?.GetComponent<Image>();
        if (viewportImg != null) viewportImg.pixelsPerUnitMultiplier = 2f;

        var item = (RectTransform)dd.itemText.transform.parent;
        item.sizeDelta = new Vector2(0f, 56f);
        ((RectTransform)item.parent).sizeDelta = new Vector2(0f, 56f); // Content

        var itemToggle = item.GetComponent<Toggle>();
        var itemColors = itemToggle.colors;
        itemColors.normalColor = ItemNormal;
        itemColors.highlightedColor = ButtonPurple;
        itemColors.selectedColor = ItemNormal;
        itemColors.pressedColor = new Color(ButtonPurple.r * 0.8f, ButtonPurple.g * 0.8f, ButtonPurple.b * 0.8f, 1f);
        itemToggle.colors = itemColors;
        if (itemToggle.targetGraphic != null) itemToggle.targetGraphic.color = Color.white;

        if (itemToggle.graphic is Image itemCheck)
        {
            itemCheck.color = FrameOrange;
            itemCheck.type = Image.Type.Sliced;
            itemCheck.pixelsPerUnitMultiplier = 6f;
            itemCheck.rectTransform.sizeDelta = new Vector2(16f, 16f);
            itemCheck.rectTransform.anchoredPosition = new Vector2(20f, 0f);
        }

        StyleText(dd.itemText, 28f);
        dd.itemText.alignment = TextAlignmentOptions.MidlineLeft;
        dd.itemText.rectTransform.offsetMin = new Vector2(44f, 0f);
        dd.itemText.rectTransform.offsetMax = new Vector2(-10f, 0f);

        var scrollbar = template.GetComponent<ScrollRect>()?.verticalScrollbar;
        if (scrollbar != null)
        {
            var sbImg = scrollbar.GetComponent<Image>();
            sbImg.color = Hex("#14061A");
            sbImg.pixelsPerUnitMultiplier = 4f;
            if (scrollbar.handleRect != null)
            {
                var handleImg = scrollbar.handleRect.GetComponent<Image>();
                handleImg.color = ButtonPurple;
                handleImg.pixelsPerUnitMultiplier = 4f;
            }
        }

        UnityEventTools.AddPersistentListener(dd.onValueChanged, onChanged);
        return dd;
    }

    private static void StyleText(TMP_Text t, float size)
    {
        t.font = font;
        t.fontSize = size;
        t.fontStyle = FontStyles.Bold;
        t.color = Color.white;
    }

    private static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    private static void SetupCamera()
    {
        // Reuse the scene's existing camera if there is one.
        var cam = Object.FindAnyObjectByType<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Background;
            return;
        }

        var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        go.tag = "MainCamera";
        cam = go.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Background;
        cam.orthographic = true;
        go.transform.position = new Vector3(0f, 0f, -10f);
    }

    private static void CreateEventSystem()
    {
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    // ------------------------------------------------------------------ helpers

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static Image AddImage(string name, Transform parent, Sprite sprite, Color color)
    {
        var img = NewRect(name, parent).gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        return img;
    }

    private static SlantedPanel AddSlanted(string name, Transform parent, Color color)
    {
        var p = NewRect(name, parent).gameObject.AddComponent<SlantedPanel>();
        p.color = color;
        p.Slant = Slant;
        p.raycastTarget = false;
        return p;
    }

    private static TextMeshProUGUI AddText(string name, Transform parent, string value, float size)
    {
        var t = NewRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        t.font = font;
        t.text = value;
        t.fontSize = size;
        t.fontStyle = FontStyles.Bold;
        t.color = Color.white;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.raycastTarget = false;
        return t;
    }

    private static void Stretch(RectTransform rt, float inset = 0f) =>
        SetAnchors(rt, Vector2.zero, Vector2.one, new Vector2(inset, inset), new Vector2(-inset, -inset));

    private static void SetAnchors(RectTransform rt, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    private static string Capitalize(string s) => s.Substring(0, 1) + s.Substring(1).ToLowerInvariant();

    private static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

    private static void AddSceneToBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true)); // main menu loads first
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // ------------------------------------------------------------------ asset generation

    private static Sprite LoadArtSprite(string file)
    {
        string path = $"{ArtDir}/{file}";
        if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) return null;
        if (ti.textureType != TextureImporterType.Sprite || ti.spriteImportMode != SpriteImportMode.Single)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void GenerateAssets()
    {
        Directory.CreateDirectory(GenDir);

        // Pill: 256x256 circle sliced at the middle -> stretches into a perfect capsule.
        // Grayscale so the Image color gives the exact purple; darker rim on the lower half = 3D lip.
        pillShaded = MakeRoundedSprite("MenuPill.png", 256, 256, 128f, 127, (v, inside) =>
        {
            float body = Mathf.Lerp(0.9f, 1f, v);
            float lipWeight = Mathf.InverseLerp(0.55f, 0.3f, v);
            float rim = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(16f, 7f, inside));
            return body * Mathf.Lerp(1f, 0.62f, rim * lipWeight);
        });
        pillFlat = MakeRoundedSprite("PillFlat.png", 256, 256, 128f, 127, null);
        roundedRect = MakeRoundedSprite("RoundedRect.png", 128, 128, 32f, 48, null);
        arrowDown = MakeArrowSprite("ArrowDown.png", 64);

        font = TMP_Settings.defaultFontAsset != null
            ? TMP_Settings.defaultFontAsset
            : AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackFontPath);

        string matPath = $"{GenDir}/MenuButtonText.mat";
        buttonTextMaterial = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (buttonTextMaterial == null)
        {
            buttonTextMaterial = new Material(font.material);
            AssetDatabase.CreateAsset(buttonTextMaterial, matPath);
        }
        buttonTextMaterial.CopyPropertiesFromMaterial(font.material);
        buttonTextMaterial.SetFloat("_FaceDilate", 0.1f);
        buttonTextMaterial.SetFloat("_OutlineWidth", 0.18f);
        buttonTextMaterial.SetColor("_OutlineColor", TextOutline);
        buttonTextMaterial.EnableKeyword("UNDERLAY_ON");
        buttonTextMaterial.SetColor("_UnderlayColor", new Color(0.05f, 0f, 0.1f, 0.6f));
        buttonTextMaterial.SetFloat("_UnderlayOffsetX", 0f);
        buttonTextMaterial.SetFloat("_UnderlayOffsetY", -0.9f);
        buttonTextMaterial.SetFloat("_UnderlaySoftness", 0.25f);
        EditorUtility.SetDirty(buttonTextMaterial);
        AssetDatabase.SaveAssets();
    }

    /// <param name="shade">(v = 0 bottom..1 top, insideDistancePx) -> brightness 0..1. Null = plain white.</param>
    private static Sprite MakeRoundedSprite(string file, int w, int h, float radius, int border, Func<float, float, float> shade)
    {
        string path = $"{GenDir}/{file}";
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var px = new Color32[w * h];
        var half = new Vector2(w, h) * 0.5f;

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            var p = new Vector2(x + 0.5f, y + 0.5f) - half;
            float d = SdRoundedRect(p, half, radius);
            float a = Mathf.Clamp01(0.5f - d); // 1px anti-aliased edge
            float s = shade?.Invoke((y + 0.5f) / h, -d) ?? 1f;
            px[y * w + x] = new Color(s, s, s, a);
        }

        return SaveSprite(path, tex, px, border);
    }

    /// <summary>Downward-pointing triangle, 4x4 supersampled for smooth edges.</summary>
    private static Sprite MakeArrowSprite(string file, int size)
    {
        string path = $"{GenDir}/{file}";
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px = new Color32[size * size];
        Vector2 a = new Vector2(size * 0.12f, size * 0.72f);
        Vector2 b = new Vector2(size * 0.88f, size * 0.72f);
        Vector2 c = new Vector2(size * 0.5f, size * 0.25f);

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            int hits = 0;
            for (int sy = 0; sy < 4; sy++)
            for (int sx = 0; sx < 4; sx++)
                if (InTriangle(new Vector2(x + (sx + 0.5f) / 4f, y + (sy + 0.5f) / 4f), a, b, c)) hits++;
            px[y * size + x] = new Color(1f, 1f, 1f, hits / 16f);
        }

        return SaveSprite(path, tex, px, 0);
    }

    private static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float Cross(Vector2 o, Vector2 u, Vector2 v) => (u.x - o.x) * (v.y - o.y) - (u.y - o.y) * (v.x - o.x);
        float d1 = Cross(a, b, p), d2 = Cross(b, c, p), d3 = Cross(c, a, p);
        bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
        return !(neg && pos);
    }

    private static Sprite SaveSprite(string path, Texture2D tex, Color32[] px, int border)
    {
        tex.SetPixels32(px);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.alphaIsTransparency = true;
        ti.mipmapEnabled = false;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.filterMode = FilterMode.Bilinear;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.spritePixelsPerUnit = 100f;
        ti.spriteBorder = new Vector4(border, border, border, border);
        ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static float SdRoundedRect(Vector2 p, Vector2 halfSize, float r)
    {
        var q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - (halfSize - new Vector2(r, r));
        var outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
        return outside.magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;
    }
}
