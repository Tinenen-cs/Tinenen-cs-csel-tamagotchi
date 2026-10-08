using Tamagotchi;
using Tamagotchi.Pet;
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
///   │      ( speech bubble )   │  pet area: nothing overlaps it
///   │          \/              │
///   │        (hamster)         │
///   ├──────────────────────────┤
///   │ FEED   DRINK   PLAY      │  3x2 thumb-sized action buttons
///   │ STUDY  SLEEP   SCENE     │
///   └──────────────────────────┘
///
/// The background fills the whole screen behind all of this.
/// Everything is wired in code, so no Inspector setup is needed.
/// </summary>
public static class MainSceneBuilder
{
    public const string PetName = "Hammy";

    private const string Art = "Assets/Art/";
    public const string FontAssetPath = Art + "Fonts/KenneyPixel SDF.asset";
    public const string StatsConfigPath = "Assets/Data/PetStatsConfig.asset";

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

        // Phone-shaped column for all UI (stays 9:16 in wide windows; full screen on phones).
        var frame = Rect("PhoneFrame", safeArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        frame.gameObject.AddComponent<PortraitFrame>();
        safeArea = frame;

        // ---------- Top panel: header + stat bars ----------
        var top = Rect("TopPanel", safeArea, new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -590), new Vector2(-30, -20));
        Sliced(top.gameObject, Sprite("UI/Generated/panel.png")).color = new Color(1f, 1f, 1f, 0.9f);

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

        // ---------- Bottom panel: action buttons (2 rows x 3) ----------
        var bottom = Rect("ActionBar", safeArea, Vector2.zero, new Vector2(1, 0), new Vector2(30, 20), new Vector2(-30, 440));
        var grid = bottom.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(316, 200);
        grid.spacing = new Vector2(24, 20);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.childAlignment = TextAnchor.MiddleCenter;

        var feed = ActionButton(bottom, "FeedButton", "FEED", Sprite("UI/Icons/icon_hunger.png"));
        var drink = ActionButton(bottom, "DrinkButton", "DRINK", Sprite("UI/Icons/icon_water.png"));
        var play = ActionButton(bottom, "PlayButton", "PLAY", Sprite("UI/Buttons/heart.png"));
        var study = ActionButton(bottom, "StudyButton", "STUDY", Sprite("UI/Buttons/star.png"));
        var sleep = ActionButton(bottom, "SleepButton", "SLEEP", Sprite("UI/Buttons/sleep_z.png"));
        var scene = ActionButton(bottom, "SceneButton", "SCENE", Sprite("UI/Buttons/home.png"));

        // ---------- Full-screen background (behind everything, also under the notch) ----------
        var bgLayer = Rect("BackgroundLayer", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        bgLayer.SetAsFirstSibling();
        var bg = Rect("Background", bgLayer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var bgImage = Img(bg.gameObject, Sprite("Backgrounds/cozy_home.png"));
        var bgFit = bg.gameObject.AddComponent<AspectRatioFitter>();
        bgFit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; // fill the screen, crop the sides
        bgFit.aspectRatio = 493f / 391f;

        var dim = Rect("DimOverlay", bgLayer, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var dimImage = Img(dim.gameObject, null);
        dimImage.color = new Color(0.05f, 0.05f, 0.2f, 0f);

        var switcher = bgLayer.gameObject.AddComponent<BackgroundSwitcher>();
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

        // ---------- Middle: free space between the panels for the pet ----------
        // Nothing overlaps this area, so the pet is never covered by UI.
        var petArea = Rect("PetArea", safeArea, Vector2.zero, Vector2.one, new Vector2(30, 460), new Vector2(-30, -610));

        // Pet stands near the bottom of the area; its size follows the area height.
        var pet = Rect("Pet", petArea, new Vector2(0.5f, 0.03f), new Vector2(0.5f, 0.6f), Vector2.zero, Vector2.zero);
        var petImage = Img(pet.gameObject, Sprite("Pet/Hamster/idle_01.png"), preserveAspect: true);
        var petAnimator = pet.gameObject.AddComponent<SpriteAnimator>();
        var petFit = pet.gameObject.AddComponent<AspectRatioFitter>();
        petFit.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
        petFit.aspectRatio = 208f / 187f;

        // Speech bubble above the pet's head: sized to its text, tail points down.
        var bubbleRt = Rect("SpeechBubble", petArea, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f), Vector2.zero, Vector2.zero);
        bubbleRt.pivot = new Vector2(0.5f, 0f);
        bubbleRt.sizeDelta = new Vector2(760, 0);
        Sliced(bubbleRt.gameObject, Sprite("UI/Generated/bubble.png")).raycastTarget = false;
        var layout = bubbleRt.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(40, 40, 26, 34);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        var fitter = bubbleRt.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize; // hug the text
        bubbleRt.gameObject.AddComponent<CanvasGroup>();

        var speech = Text(bubbleRt, "Text", "Hi! I'm " + PetName + "!", 60, Brown, TextAlignmentOptions.Center);
        // Messages are short (no wrapping); put a line break in a message for a second line.

        var tail = Rect("Tail", bubbleRt, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-27, -30), new Vector2(27, 6));
        Img(tail.gameObject, Sprite("UI/Generated/bubble_tail.png"));
        tail.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

        var bubble = bubbleRt.gameObject.AddComponent<SpeechBubble>();
        Set(bubble, "text", speech);

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
        Set(ui, "speechBubble", bubble);
        Set(ui, "backgrounds", switcher);

        // ---------- Game over (hidden until the pet gets sick) ----------
        var gameOver = Rect("GameOverPanel", safeArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var shade = Img(gameOver.gameObject, null);
        shade.color = new Color(0.12f, 0.06f, 0.04f, 0.75f);
        shade.raycastTarget = true; // blocks taps on the buttons behind it

        var card = Rect("Card", gameOver, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-440, -480), new Vector2(440, 480));
        Sliced(card.gameObject, Sprite("UI/Generated/panel.png"));
        var sickPet = Rect("SickPet", card, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-220, -470), new Vector2(220, -70));
        Img(sickPet.gameObject, Sprite("Pet/Hamster/crying_01.png"), preserveAspect: true);
        var sickAnimator = sickPet.gameObject.AddComponent<SpriteAnimator>();
        SetFrames(sickAnimator, Frames("crying"), 5f);
        var title = Text(card, "Title", PetName + " got sick!", 96, Brown, TextAlignmentOptions.Center);
        Place(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -590), new Vector2(-30, -480));
        var sub = Text(card, "Subtitle", "Keep the hunger bar up next time.", 52, SoftBrown, TextAlignmentOptions.Center);
        Place(sub.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -680), new Vector2(-30, -600));
        var restart = ActionButton(card, "RestartButton", "RESTART", Sprite("UI/Buttons/heart.png"));
        Place((RectTransform)restart.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-170, 40), new Vector2(170, 285));
        gameOver.gameObject.SetActive(false);

        Set(ui, "gameOverPanel", gameOver.gameObject);
        Set(ui, "restartButton", restart);

        // ---------- Gameplay objects ----------
        var game = new GameObject("Game");
        var stats = game.AddComponent<PetStats>();
        Set(stats, "config", EnsureStatsConfig());
        var controller = game.AddComponent<PetController>();
        Set(controller, "stats", stats);
        Set(controller, "animator", petAnimator);
        Set(controller, "petImage", petImage);
        Set(controller, "backgrounds", switcher);
        Set(controller, "sleepBackground", Sprite("Backgrounds/moonlit_bedroom.png"));
        Set(controller, "dimOverlay", dimImage);
        SetAnimations(controller, new[]
        {
            // state, frames (hamster sprite name), frames per second
            (PetState.Idle, "idle", 4f),
            (PetState.Eating, "eating", 8f),
            (PetState.Drinking, "eating", 8f),   // no drinking frames in the pack; sipping uses the eating loop
            (PetState.Studying, "studying", 5f),
            (PetState.Sleeping, "sleeping", 3f),
            (PetState.Playing, "playing", 8f),
            (PetState.Happy, "happy", 8f),
            (PetState.Sad, "sad", 4f),
            (PetState.Crying, "crying", 6f),
            (PetState.Hangry, "sad", 7f),        // + red pulse and shake (PetController)
            (PetState.Sick, "crying", 4f),       // + green tint (PetController)
        });

        var manager = game.AddComponent<GameManager>();
        Set(manager, "stats", stats);
        Set(manager, "ui", ui);
        Set(manager, "pet", controller);
    }

    /// <summary>All hamster frames for an animation name, e.g. "idle" -> idle_01..idle_04.</summary>
    private static Sprite[] Frames(string name)
    {
        var list = new System.Collections.Generic.List<Sprite>();
        for (int i = 1; i <= 9; i++)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{Art}Pet/Hamster/{name}_{i:00}.png");
            if (s == null) break;
            list.Add(s);
        }
        if (list.Count == 0) Debug.LogError("[MainSceneBuilder] No frames found for " + name);
        return list.ToArray();
    }

    private static void SetFrames(SpriteAnimator animator, Sprite[] frames, float fps)
    {
        SetArray(animator, "frames", frames);
        var so = new SerializedObject(animator);
        so.FindProperty("fps").floatValue = fps;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetAnimations(PetController controller, (PetState state, string frames, float fps)[] entries)
    {
        var so = new SerializedObject(controller);
        var list = so.FindProperty("animations");
        list.arraySize = entries.Length;
        for (int i = 0; i < entries.Length; i++)
        {
            var e = list.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("state").enumValueIndex = (int)entries[i].state;
            e.FindPropertyRelative("fps").floatValue = entries[i].fps;
            var frames = Frames(entries[i].frames);
            var fp = e.FindPropertyRelative("frames");
            fp.arraySize = frames.Length;
            for (int f = 0; f < frames.Length; f++) fp.GetArrayElementAtIndex(f).objectReferenceValue = frames[f];
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>Creates the stats tuning asset with default values (once; your edits are kept).</summary>
    public static PetStatsConfig EnsureStatsConfig()
    {
        var existing = AssetDatabase.LoadAssetAtPath<PetStatsConfig>(StatsConfigPath);
        if (existing != null) return existing;
        if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
        var config = ScriptableObject.CreateInstance<PetStatsConfig>();
        AssetDatabase.CreateAsset(config, StatsConfigPath);
        AssetDatabase.SaveAssets();
        return config;
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

        var iconRt = Rect("Icon", root, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-55, -122), new Vector2(55, -18));
        Img(iconRt.gameObject, icon, preserveAspect: true);

        var text = Text(root, "Label", label, 58, Brown, TextAlignmentOptions.Center);
        Place(text.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(0, 24), new Vector2(0, 84));
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
