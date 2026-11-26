using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Собирает сцену LevelSelect.unity по референсу одним нажатием:
//     меню Tools > Doomspire > Build Level Select Scene
// После сборки сцену можно править руками как обычно. Повторный запуск пересоздаёт сцену с нуля.
public static class LevelSelectSceneBuilder
{
    const string ScenePath = "Assets/Scenes/LevelSelect.unity";
    const string GameScenePath = "Assets/Scenes/Game.unity";
    const string SpriteFolder = "Assets/Sprites/UI/LevelSelect";
    const string BackgroundPath = "Assets/Sprites/Backgrounds/ForestBackground.jpg";

    // Референсное разрешение интерфейса.
    const float RefW = 1920f;
    const float RefH = 1080f;

    // Позиции замков (центр основания башни) в пикселях референса 1920x1080, отсчёт от левого верхнего угла.
    static readonly Vector2[] CastleBase =
    {
        new Vector2(250, 800),
        new Vector2(560, 545),
        new Vector2(880, 800),
        new Vector2(1180, 545),
        new Vector2(1480, 730),
    };

    static readonly Vector2 TowerSize = new Vector2(210, 250);

    [MenuItem("Tools/Doomspire/Build Level Select Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
            !EditorUtility.DisplayDialog("LevelSelect уже существует",
                "Сцена " + ScenePath + " будет пересоздана. Ручные изменения в ней пропадут. Продолжить?",
                "Пересоздать", "Отмена"))
            return;

        ConfigureSprites();

        Sprite background = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
        Sprite towerOpen = LoadSprite("TowerOpen");
        Sprite towerLocked = LoadSprite("TowerLocked");
        Sprite starGold = LoadSprite("StarGold");
        Sprite starEmpty = LoadSprite("StarEmpty");
        Sprite flag = LoadSprite("Flag");
        Sprite ring = LoadSprite("GlowRing");
        Sprite dash = LoadSprite("PathDash");
        Sprite backButton = LoadSprite("BackButton");
        Sprite xpFrame = LoadSprite("XPBarFrame");
        Sprite xpFill = LoadSprite("XPBarFill");
        Sprite skillTreeButton = LoadSprite("SkillTreeButton");
        Sprite skillTreeIcon = LoadSprite("SkillTreeIcon");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateCamera();
        CreateEventSystem();

        // --- Canvas ---
        GameObject canvasGO = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(RefW, RefH);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        RectTransform canvasRT = (RectTransform)canvasGO.transform;

        // Фон растягивается на весь экран.
        Image bg = CreateImage("Background", canvasRT, background, false);
        Stretch(bg.rectTransform);
        bg.raycastTarget = false;
        bg.preserveAspect = false;

        // Карта фиксированного размера 1920x1080 по центру — замки не разъезжаются при другом разрешении.
        RectTransform map = CreateRect("Map", canvasRT);
        map.anchorMin = map.anchorMax = new Vector2(0.5f, 0.5f);
        map.sizeDelta = new Vector2(RefW, RefH);

        // --- Пунктирные дорожки (рисуются под замками) ---
        RectTransform paths = CreateRect("Paths", map);
        Stretch(paths);
        for (int i = 0; i < CastleBase.Length - 1; i++)
            CreateDashedPath(paths, dash, i);

        // --- Замки ---
        RectTransform levelsRoot = CreateRect("Levels", map);
        Stretch(levelsRoot);
        List<LevelNode> nodes = new List<LevelNode>();
        for (int i = 0; i < CastleBase.Length; i++)
            nodes.Add(CreateLevelNode(levelsRoot, i + 1, CastleBase[i], towerOpen, towerLocked, starGold, starEmpty, flag, ring));

        // Декоративная иконка дерева навыков в правом нижнем углу.
        if (skillTreeIcon != null)
        {
            Image icon = CreateImage("SkillTreeIcon", map, skillTreeIcon, true);
            Place(icon.rectTransform, new Vector2(1715, 800), new Vector2(250, 128));
            icon.raycastTarget = false;
        }

        // --- HUD ---
        RectTransform hud = CreateRect("HUD", canvasRT);
        Stretch(hud);

        // Back — левый верхний угол.
        Button back = CreateButton("BackButton", hud, backButton);
        RectTransform backRT = (RectTransform)back.transform;
        backRT.anchorMin = backRT.anchorMax = backRT.pivot = new Vector2(0f, 1f);
        backRT.sizeDelta = new Vector2(110, 108);
        backRT.anchoredPosition = new Vector2(48, -44);

        // XP-бар — правый верхний угол.
        Image frame = CreateImage("XPBar", hud, xpFrame, true);
        RectTransform frameRT = frame.rectTransform;
        frameRT.anchorMin = frameRT.anchorMax = frameRT.pivot = new Vector2(1f, 1f);
        frameRT.sizeDelta = new Vector2(470, 131);
        frameRT.anchoredPosition = new Vector2(-50, -44);
        frame.raycastTarget = false;

        // Область дорожки внутри рамки (в пикселях спрайта 502x140: x 133..466, y 31..64 сверху).
        RectTransform track = CreateRect("Track", frameRT);
        SetAnchorsFromPixels(track, 502, 140, 133, 31, 466, 64);

        Image fill = CreateImage("Fill", track, xpFill, false);
        fill.type = Image.Type.Sliced;
        fill.raycastTarget = false;
        RectTransform fillRT = fill.rectTransform;
        fillRT.anchorMin = new Vector2(0f, 0f);
        fillRT.anchorMax = new Vector2(0.2f, 1f);
        fillRT.offsetMin = new Vector2(1f, 2f);
        fillRT.offsetMax = new Vector2(-1f, -2f);

        RectTransform textArea = CreateRect("XPText", frameRT);
        SetAnchorsFromPixels(textArea, 502, 140, 250, 70, 470, 116);
        TextMeshProUGUI xpText = textArea.gameObject.AddComponent<TextMeshProUGUI>();
        xpText.text = "200/1000";
        xpText.fontSize = 30;
        xpText.fontStyle = FontStyles.Bold;
        xpText.color = Color.white;
        xpText.alignment = TextAlignmentOptions.Right;
        xpText.raycastTarget = false;

        // SKILL TREE — снизу справа.
        Button skill = CreateButton("SkillTreeButton", hud, skillTreeButton);
        RectTransform skillRT = (RectTransform)skill.transform;
        skillRT.anchorMin = skillRT.anchorMax = skillRT.pivot = new Vector2(1f, 0f);
        skillRT.sizeDelta = new Vector2(330, 102);
        skillRT.anchoredPosition = new Vector2(-420, 56);

        // --- Менеджер ---
        GameObject managerGO = new GameObject("LevelSelectManager");
        LevelSelectManager manager = managerGO.AddComponent<LevelSelectManager>();
        manager.levels = nodes.ToArray();
        manager.xpFill = fillRT;
        manager.xpText = xpText;
        manager.backButton = back;
        manager.skillTreeButton = skill;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddScenesToBuildList();

        Debug.Log("LevelSelect собрана и сохранена: " + ScenePath);
    }

    // ---------------- helpers ----------------

    static LevelNode CreateLevelNode(RectTransform parent, int level, Vector2 basePx,
        Sprite open, Sprite locked, Sprite starFull, Sprite starEmpty, Sprite flagSprite, Sprite ringSprite)
    {
        RectTransform root = CreateRect("Level_" + level, parent);
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0f);
        root.sizeDelta = TowerSize;
        root.anchoredPosition = ToMap(basePx);

        // Кольцо под башней.
        Image ring = CreateImage("GlowRing", root, ringSprite, true);
        RectTransform ringRT = ring.rectTransform;
        ringRT.anchorMin = ringRT.anchorMax = new Vector2(0.5f, 0f);
        ringRT.sizeDelta = new Vector2(260, 98);
        ringRT.anchoredPosition = new Vector2(0, 12);
        ring.raycastTarget = false;

        // Башня — она же кнопка.
        Image tower = CreateImage("Tower", root, open, true);
        Stretch(tower.rectTransform);
        Button button = tower.gameObject.AddComponent<Button>();
        button.targetGraphic = tower;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 1f, 0.85f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
        button.colors = colors;

        // Флаг над текущим уровнем.
        Image flag = CreateImage("Flag", root, flagSprite, true);
        RectTransform flagRT = flag.rectTransform;
        flagRT.anchorMin = flagRT.anchorMax = new Vector2(0.5f, 1f);
        flagRT.pivot = new Vector2(0.2f, 0f);
        flagRT.sizeDelta = new Vector2(92, 114);
        flagRT.anchoredPosition = new Vector2(-10, -8);
        flag.raycastTarget = false;

        // Три звезды над пройденным уровнем.
        RectTransform starsRT = CreateRect("Stars", root);
        starsRT.anchorMin = starsRT.anchorMax = new Vector2(0.5f, 1f);
        starsRT.pivot = new Vector2(0.5f, 0f);
        starsRT.sizeDelta = new Vector2(180, 70);
        starsRT.anchoredPosition = new Vector2(0, 4);

        Image[] stars = new Image[3];
        float[] xs = { -58f, 0f, 58f };
        float[] ys = { 0f, 14f, 0f };
        for (int s = 0; s < 3; s++)
        {
            Image star = CreateImage("Star_" + (s + 1), starsRT, starFull, true);
            RectTransform srt = star.rectTransform;
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0f);
            srt.pivot = new Vector2(0.5f, 0f);
            srt.sizeDelta = new Vector2(54, 54);
            srt.anchoredPosition = new Vector2(xs[s], ys[s]);
            star.raycastTarget = false;
            stars[s] = star;
        }

        LevelNode node = root.gameObject.AddComponent<LevelNode>();
        node.levelNumber = level;
        node.button = button;
        node.towerImage = tower;
        node.openSprite = open;
        node.lockedSprite = locked;
        node.starsRoot = starsRT.gameObject;
        node.stars = stars;
        node.starFull = starFull;
        node.starEmpty = starEmpty;
        node.flag = flag.gameObject;
        node.glowRing = ring.gameObject;

        // Превью в редакторе: первый уровень — текущий, остальные закрыты.
        node.Refresh(level == 1, 0, level == 1);

        return node;
    }

    static void CreateDashedPath(RectTransform parent, Sprite dash, int index)
    {
        // Соединяем середины башен, отступая от краёв.
        Vector2 a = ToMap(CastleBase[index]) + new Vector2(0, TowerSize.y * 0.4f);
        Vector2 b = ToMap(CastleBase[index + 1]) + new Vector2(0, TowerSize.y * 0.4f);
        Vector2 dir = (b - a).normalized;
        float length = Vector2.Distance(a, b);
        const float margin = 120f;
        const float step = 46f;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        RectTransform group = CreateRect("Path_" + (index + 1) + "_" + (index + 2), parent);
        Stretch(group);

        for (float d = margin; d <= length - margin; d += step)
        {
            Image img = CreateImage("Dash", group, dash, true);
            RectTransform rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(38, 22);
            rt.anchoredPosition = a + dir * d;
            rt.localRotation = Quaternion.Euler(0, 0, angle);
            img.raycastTarget = false;
        }
    }

    static Vector2 ToMap(Vector2 px)
    {
        return new Vector2(px.x - RefW / 2f, RefH / 2f - px.y);
    }

    static void Place(RectTransform rt, Vector2 centerPx, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = ToMap(centerPx);
    }

    // Якоря дочернего элемента по прямоугольнику в пикселях спрайта (y отсчитывается сверху).
    static void SetAnchorsFromPixels(RectTransform rt, float w, float h, float x0, float yTop0, float x1, float yTop1)
    {
        rt.anchorMin = new Vector2(x0 / w, 1f - yTop1 / h);
        rt.anchorMax = new Vector2(x1 / w, 1f - yTop0 / h);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static Image CreateImage(string name, Transform parent, Sprite sprite, bool preserveAspect)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = preserveAspect;
        return img;
    }

    static Button CreateButton(string name, Transform parent, Sprite sprite)
    {
        Image img = CreateImage(name, parent, sprite, true);
        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        ColorBlock colors = b.colors;
        colors.highlightedColor = new Color(1f, 0.95f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        b.colors = colors;
        return b;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void CreateCamera()
    {
        GameObject camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        Camera cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.18f, 0.08f);
        camGO.transform.position = new Vector3(0, 0, -10);
    }

    static void CreateEventSystem()
    {
        GameObject es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }

    static Sprite LoadSprite(string name)
    {
        string path = SpriteFolder + "/" + name + ".png";
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null)
            Debug.LogWarning("Не найден спрайт " + path);
        return s;
    }

    // Все PNG в папке LevelSelect импортируются как UI-спрайты; у полосы XP — 9-slice границы.
    static void ConfigureSprites()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SpriteFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;

            bool changed = false;
            if (ti.textureType != TextureImporterType.Sprite) { ti.textureType = TextureImporterType.Sprite; changed = true; }
            if (ti.spriteImportMode != SpriteImportMode.Single) { ti.spriteImportMode = SpriteImportMode.Single; changed = true; }
            if (ti.mipmapEnabled) { ti.mipmapEnabled = false; changed = true; }
            if (!ti.alphaIsTransparency) { ti.alphaIsTransparency = true; changed = true; }

            if (path.EndsWith("XPBarFill.png"))
            {
                Vector4 border = new Vector4(14, 0, 14, 0);
                if (ti.spriteBorder != border) { ti.spriteBorder = border; changed = true; }
            }

            if (changed)
                ti.SaveAndReimport();
        }
    }

    static void AddScenesToBuildList()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        EnsureInBuildList(scenes, ScenePath);
        EnsureInBuildList(scenes, GameScenePath);
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    static void EnsureInBuildList(List<EditorBuildSettingsScene> scenes, string path)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) return;

        foreach (EditorBuildSettingsScene s in scenes)
        {
            if (s.path == path)
            {
                s.enabled = true;
                return;
            }
        }

        scenes.Add(new EditorBuildSettingsScene(path, true));
    }
}
