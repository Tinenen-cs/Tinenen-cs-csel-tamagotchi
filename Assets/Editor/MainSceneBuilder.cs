using Tamagotchi.UI;
using Tamagotchi.World;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

/// <summary>
/// Builds the game's UI inside the Main scene's SafeArea, in the 1080x1920 reference space:
///
///   ┌──────────────────────────┐
///   │ Hammy               [♪]  │  header + mute
///   │ [🍴] HUNGER ███████ 100% │  big hunger bar (green → yellow → red)
///   │ [💧]Thirst  [❤]Health    │  small bars, 2 columns
///   │ [☺]Happy    [z]Energy    │
///   │ [★]Smart                 │
///   ├──────────────────────────┤
///   │   framed background      │  pet area (scene + hamster + dim overlay)
///   │        (hamster)         │
///   │  mood text               │
///   ├──────────────────────────┤
///   │ FEED   DRINK   PLAY      │  3x2 thumb-sized action buttons
///   │ STUDY  SLEEP   SCENE     │
///   └──────────────────────────┘
///
/// Everything is wired in code, so no Inspector setup is needed.
/// </summary>
public static class MainSceneBuilder
{
    public const string PetName = "Hammy";

    private const string Art = "Assets/Art/";
    public const string FontAssetPath = Art + "Fonts/KenneyPixel SDF.asset";

    // Pixel-art scale for 9-sliced frames: 1 source pixel = 6 canvas units.
    private const float PixelScale = 6f;

    // Palette (matches the asset pack).
    private static readonly Color Brown = new Color32(90, 48, 29, 255);
    private static readonly Color SoftBrown = new Color32(145, 87, 67, 255);

    private static readonly Color ThirstColor = new Color32(82, 168, 240, 255);
    private static readonly Color HappinessColor = new Color32(250, 190, 70, 255);
    private static readonly Color EnergyColor = new Color32(150, 120, 220, 255);
    private static readonly Color IntelligenceColor = new Color32(70, 190, 165, 255);
    private static readonly Color HealthColor = new Color32(240, 110, 125, 255);

    private static TMP_FontAsset _font;

    /// <summary>Hunger bar colors: red when empty, yellow at half, green when full.</summary>
    public static Gradient HungerGradient()
    {
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color32(230, 70, 60, 255), 0f),
                new GradientColorKey(new Color32(250, 205, 60, 255), 0.5f),
                new GradientColorKey(new Color32(110, 200, 80, 255), 1f),
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        return g;
    }

    public static void Build(GameObject canvas, RectTransform safeArea)
    {
        _font = EnsureFontAsset();

        // ---------- Top panel: header + stat bars ----------
        var top = Rect("TopPanel", safeArea, new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -590), new Vector2(-30, -20));
        Sliced(top.gameObject, Sprite("UI/Generated/panel.png"));

        var nameIcon = Rect("PetIcon", top, new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -125), new Vector2(130, -25));
        Img(nameIcon.gameObject, Sprite("UI/Buttons/hamster_face.png"), preserveAspect: true);
        var nameText = Text(top, "PetName", PetName, 84, Brown, TextAlignmentOptions.Left);
        Place(nameText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(150, -125), new Vector2(-170, -25));

        var mute = IconButton(top, "MuteButton", Sprite("UI/Buttons/music.png"), out Image muteIcon);
        Place((RectTransform)mute.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-140, -140), new Vector2(-20, -20));

        var hunger = CreateStatBar(top, "HungerBar", "HUNGER", Sprite("UI/Icons/icon_hunger.png"),
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -280), new Vector2(-20, -150), big: true);
        SetGradient(hunger, HungerGradient());

        // Small bars: 2 columns x 3 rows.
        StatBar Small(string name, string label, string icon, Color color, int col, int row)
        {
            float y0 = -295 - row * 90;
            var min = new Vector2(col == 0 ? 0f : 0.5f, 1);
            var max = new Vector2(col == 0 ? 0.5f : 1f, 1);
            var offMin = new Vector2(col == 0 ? 20 : 10, y0 - 80);
            var offMax = new Vector2(col == 0 ? -10 : -20, y0);
            var bar = CreateStatBar(top, name, label, Sprite(icon), min, max, offMin, offMax, big: false);
            SetFixedColor(bar, color);
            return bar;
        }

        var thirst = Small("ThirstBar", "THIRST", "UI/Icons/icon_water.png", ThirstColor, 0, 0);
        var health = Small("HealthBar", "HEALTH", "UI/Icons/icon_health.png", HealthColor, 1, 0);
        var happiness = Small("HappinessBar", "HAPPY", "UI/Icons/icon_happiness.png", HappinessColor, 0, 1);
        var energy = Small("EnergyBar", "ENERGY", "UI/Buttons/sleep_z.png", EnergyColor, 1, 1);
        var intelligence = Small("IntelligenceBar", "SMART", "UI/Buttons/star.png", IntelligenceColor, 0, 2);

        // ---------- Bottom panel: action buttons ----------
        var bottom = Rect("ActionBar", safeArea, Vector2.zero, new Vector2(1, 0), new Vector2(30, 20), new Vector2(-30, 540));
        var grid = bottom.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(316, 245);
        grid.spacing = new Vector2(24, 30);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.childAlignment = TextAnchor.MiddleCenter;

        var feed = ActionButton(bottom, "FeedButton", "FEED", Sprite("UI/Icons/icon_hunger.png"));
        var drink = ActionButton(bottom, "DrinkButton", "DRINK", Sprite("UI/Icons/icon_water.png"));
        var play = ActionButton(bottom, "PlayButton", "PLAY", Sprite("UI/Buttons/heart.png"));
        var study = ActionButton(bottom, "StudyButton", "STUDY", Sprite("UI/Buttons/star.png"));
        var sleep = ActionButton(bottom, "SleepButton", "SLEEP", Sprite("UI/Buttons/sleep_z.png"));
        var scene = ActionButton(bottom, "SceneButton", "SCENE", Sprite("UI/Buttons/home.png"));

        // ---------- Middle: framed scene with the pet ----------
        var petArea = Rect("PetArea", safeArea, Vector2.zero, Vector2.one, new Vector2(30, 560), new Vector2(-30, -610));

        var mood = Text(petArea, "MoodText", PetName + " is happy to see you!", 56, Brown, TextAlignmentOptions.Center);
        Place(mood.rectTransform, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 70));

        var frameHolder = Rect("FrameHolder", petArea, Vector2.zero, Vector2.one, new Vector2(0, 80), Vector2.zero);
        var frame = Rect("SceneFrame", frameHolder, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var frameFit = frame.gameObject.AddComponent<AspectRatioFitter>();
        frameFit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        frameFit.aspectRatio = 493f / 391f; // indoor scene shape
        Sliced(frame.gameObject, Sprite("UI/Generated/panel.png"));

        // Viewport clips wider (outdoor) backgrounds to the frame.
        var viewport = Rect("Viewport", frame, Vector2.zero, Vector2.one, new Vector2(14, 14), new Vector2(-14, -14));
        viewport.gameObject.AddComponent<RectMask2D>();

        var bg = Rect("Background", viewport, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var bgImage = Img(bg.gameObject, Sprite("Backgrounds/cozy_home.png"));
        var bgFit = bg.gameObject.AddComponent<AspectRatioFitter>();
        bgFit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        bgFit.aspectRatio = 493f / 391f;

        var dim = Rect("DimOverlay", viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Img(dim.gameObject, null).color = new Color(0.05f, 0.05f, 0.2f, 0f);

        var pet = Rect("Pet", viewport, new Vector2(0.28f, 0.04f), new Vector2(0.72f, 0.56f), Vector2.zero, Vector2.zero);
        Img(pet.gameObject, Sprite("Pet/Hamster/idle_01.png"), preserveAspect: true);

        var switcher = frame.gameObject.AddComponent<BackgroundSwitcher>();
        Set(switcher, "target", bgImage);
        Set(switcher, "fitter", bgFit);
        SetArray(switcher, "backgrounds", new Object[]
        {
            Sprite("Backgrounds/cozy_home.png"),
            Sprite("Backgrounds/sunny_garden.png"),
            Sprite("Backgrounds/beach.png"),
            Sprite("Backgrounds/forest_stream.png"),
            Sprite("Backgrounds/sunset_rooftop.png"),
            Sprite("Backgrounds/moonlit_bedroom.png"),
        });

        // ---------- UIManager wiring ----------
        var ui = canvas.AddComponent<UIManager>();
        Set(ui, "hungerBar", hunger);
        Set(ui, "thirstBar", thirst);
        Set(ui, "happinessBar", happiness);
        Set(ui, "energyBar", energy);
        Set(ui, "intelligenceBar", intelligence);
        Set(ui, "healthBar", health);
        Set(ui, "feedButton", feed);
        Set(ui, "drinkButton", drink);
        Set(ui, "studyButton", study);
        Set(ui, "sleepButton", sleep);
        Set(ui, "playButton", play);
        Set(ui, "sceneButton", scene);
        Set(ui, "muteButton", mute);
        Set(ui, "muteIcon", muteIcon);
        Set(ui, "moodText", mood);
        Set(ui, "backgrounds", switcher);
    }

    // =====================================================================
    // Widgets
    // =====================================================================

    private static StatBar CreateStatBar(RectTransform parent, string name, string label, Sprite icon,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, bool big)
    {
        var root = Rect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
        float iconSize = big ? 130 : 80;
        float barHeight = big ? 84 : 56;
        float fontSize = big ? 56 : 40;

        var iconRt = Rect("Icon", root, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(0, -iconSize / 2), new Vector2(iconSize, iconSize / 2));
        Img(iconRt.gameObject, icon, preserveAspect: true);

        var barRt = Rect("Bar", root, new Vector2(0, 0.5f), new Vector2(1, 0.5f),
            new Vector2(iconSize + 12, -barHeight / 2), new Vector2(0, barHeight / 2));
        Sliced(barRt.gameObject, Sprite("UI/Generated/bar_frame.png"));
        var slider = barRt.gameObject.AddComponent<Slider>();

        var fillArea = Rect("Fill Area", barRt, Vector2.zero, Vector2.one, new Vector2(10, 10), new Vector2(-10, -10));
        var fillRt = Rect("Fill", fillArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var fill = Sliced(fillRt.gameObject, Sprite("UI/Generated/bar_fill.png"));

        slider.fillRect = fillRt;
        slider.handleRect = null;
        slider.targetGraphic = null;
        slider.transition = Selectable.Transition.None;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        slider.direction = Slider.Direction.LeftToRight;
        slider.interactable = false;
        slider.minValue = 0;
        slider.maxValue = 100;
        slider.value = 100;

        var labelText = Text(barRt, "Label", label, fontSize, Brown, TextAlignmentOptions.Left);
        Place(labelText.rectTransform, Vector2.zero, Vector2.one, new Vector2(22, 0), new Vector2(-20, 0));
        var valueText = Text(barRt, "Value", "100%", fontSize, Brown, TextAlignmentOptions.Right);
        Place(valueText.rectTransform, Vector2.zero, Vector2.one, new Vector2(20, 0), new Vector2(-22, 0));

        var bar = root.gameObject.AddComponent<StatBar>();
        Set(bar, "slider", slider);
        Set(bar, "fill", fill);
        Set(bar, "valueText", valueText);
        return bar;
    }

    private static Button ActionButton(RectTransform parent, string name, string label, Sprite icon)
    {
        var root = Rect(name, parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        var bg = Sliced(root.gameObject, Sprite("UI/Generated/button_tile.png"));
        var button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = bg;
        root.gameObject.AddComponent<PressBounce>();

        var iconRt = Rect("Icon", root, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-65, -150), new Vector2(65, -24));
        Img(iconRt.gameObject, icon, preserveAspect: true);

        var text = Text(root, "Label", label, 64, Brown, TextAlignmentOptions.Center);
        Place(text.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(0, 28), new Vector2(0, 100));
        return button;
    }

    private static Button IconButton(RectTransform parent, string name, Sprite icon, out Image iconImage)
    {
        var root = Rect(name, parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        var bg = Sliced(root.gameObject, Sprite("UI/Generated/button_tile.png"));
        var button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = bg;
        root.gameObject.AddComponent<PressBounce>();

        var iconRt = Rect("Icon", root, Vector2.zero, Vector2.one, new Vector2(22, 26), new Vector2(-22, -18));
        iconImage = Img(iconRt.gameObject, icon, preserveAspect: true);
        return button;
    }

    // =====================================================================
    // Font
    // =====================================================================

    /// <summary>Creates (once) a TextMesh Pro font asset from the Kenney Pixel TTF.</summary>
    public static TMP_FontAsset EnsureFontAsset()
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (existing != null) return existing;

        var ttf = AssetDatabase.LoadAssetAtPath<Font>(Art + "Fonts/KenneyPixel.ttf");
        var fa = TMP_FontAsset.CreateFontAsset(ttf, 64, 6, GlyphRenderMode.SDFAA, 1024, 1024,
            AtlasPopulationMode.Dynamic, false);
        fa.name = "KenneyPixel SDF";
        // Bake all printable ASCII so the atlas is fixed (no runtime changes to the asset).
        fa.TryAddCharacters(" !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~", out _);
        fa.atlasPopulationMode = AtlasPopulationMode.Static;

        AssetDatabase.CreateAsset(fa, FontAssetPath);
        fa.atlasTexture.name = "KenneyPixel Atlas";
        AssetDatabase.AddObjectToAsset(fa.atlasTexture, fa);
        fa.material.name = "KenneyPixel Material";
        AssetDatabase.AddObjectToAsset(fa.material, fa);
        EditorUtility.SetDirty(fa);
        AssetDatabase.SaveAssets();
        return fa;
    }

    // =====================================================================
    // Helpers
    // =====================================================================

    private static Sprite Sprite(string pathUnderArt)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(Art + pathUnderArt);
        if (s == null) Debug.LogError("[MainSceneBuilder] Missing sprite: " + Art + pathUnderArt);
        return s;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        var rt = ProjectSetup.CreateRect(name, parent);
        Place(rt, anchorMin, anchorMax, offsetMin, offsetMax);
        return rt;
    }

    private static void Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    private static Image Img(GameObject go, Sprite sprite, bool preserveAspect = false)
    {
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = preserveAspect;
        img.raycastTarget = false;
        return img;
    }

    private static Image Sliced(GameObject go, Sprite sprite)
    {
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 1f / PixelScale;
        img.raycastTarget = true;
        return img;
    }

    private static TextMeshProUGUI Text(Transform parent, string name, string text, float size, Color color,
        TextAlignmentOptions align)
    {
        var rt = ProjectSetup.CreateRect(name, parent);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.font = _font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void Set(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(field);
        if (prop == null) { Debug.LogError($"[MainSceneBuilder] {target.GetType().Name} has no field '{field}'"); return; }
        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetArray(Object target, string field, Object[] values)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(field);
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetGradient(StatBar bar, Gradient gradient)
    {
        var so = new SerializedObject(bar);
        so.FindProperty("useGradient").boolValue = true;
        so.FindProperty("colorByValue").gradientValue = gradient;
        so.ApplyModifiedPropertiesWithoutUndo();
        bar.Fill.color = gradient.Evaluate(1f); // show the full-bar color in the editor too
    }

    private static void SetFixedColor(StatBar bar, Color color)
    {
        var so = new SerializedObject(bar);
        so.FindProperty("useGradient").boolValue = false;
        so.FindProperty("fixedColor").colorValue = color;
        so.ApplyModifiedPropertiesWithoutUndo();
        bar.Fill.color = color;
    }
}
