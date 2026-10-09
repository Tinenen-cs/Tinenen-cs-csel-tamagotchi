using System;
using System.IO;
using Tamagotchi.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>
/// Renders Main.unity at 1080x1920 into Docs/ without entering Play mode.
///
/// Editor:       menu Tamagotchi > Capture Screenshot          (Docs/screenshot.png)
///               menu Tamagotchi > Capture State Screenshots   (Docs/state_*.png)
///               menu Tamagotchi > Capture Landscape Check     (Docs/landscape_check.png, 1920x1080)
/// Command line: Unity -batchmode -quit -projectPath . -executeMethod ScreenshotCapture.Capture
///               (or ScreenshotCapture.CaptureStates; do not pass -nographics, rendering needs a GPU)
/// </summary>
public static class ScreenshotCapture
{
    private const string Hamster = "Assets/Art/Pet/Hamster/";
    private const string Backgrounds = "Assets/Art/Backgrounds/";

    [MenuItem("Tamagotchi/Capture Screenshot")]
    public static void Capture()
    {
        Render("Docs/screenshot.png", () => StartOfGame(90f, 60f));
    }

    /// <summary>
    /// Poses the scene in the activity's states (happy at 100, sad at 50 or below, exhausted with
    /// Play/Study locked, sleeping) plus eating/playing/sick, and renders each.
    /// </summary>
    [MenuItem("Tamagotchi/Capture State Screenshots")]
    public static void CaptureStates()
    {
        Render("Docs/state_happy.png", () =>
        {
            StartOfGame(100f, 58f);
            SetPet("happy_02");
            SetBubble("I'm SO HAPPY!");
        });
        Render("Docs/state_sad.png", () =>
        {
            StartOfGame(48.6f, 52f);
            SetPet("sad_02");
            SetBubble("I feel sad...");
        });
        Render("Docs/state_exhausted.png", () =>
        {
            StartOfGame(71.5f, 12.5f); // stamina below 20
            Lock("PlayButton", true);
            Lock("StudyButton", true);
            SetBubble("I'm exhausted...\nI need to SLEEP!");
        });
        Render("Docs/state_sleeping.png", () =>
        {
            StartOfGame(70.8f, 100f); // Sleep restored stamina to full
            SetPet("sleeping_02");
            Find<RectTransform>("Bed").gameObject.SetActive(true);
            var pet = Find<RectTransform>("Pet");
            pet.anchoredPosition = new Vector2(0, 0.20f * 536f); // lifted onto the bed (PetController.sleepLift)
            SetBackground("moonlit_bedroom");
            Find<Image>("DimOverlay").color = new Color(0.05f, 0.05f, 0.2f, 0.45f);
            Find<RectTransform>("SpeechBubble").gameObject.SetActive(false); // hidden while asleep
        });
        Render("Docs/state_eating.png", () =>
        {
            StartOfGame(88f, 65f);
            SetPet("eating_02");
            Find<RectTransform>("FoodBowl").gameObject.SetActive(true);
            SetBubble("Yum yum!");
        });
        Render("Docs/state_playing.png", () =>
        {
            StartOfGame(90.5f, 58f);
            SetPet("playing_02");
            SetBackground("beach");
            SetBubble("Wheee! Fun!");
        });
        Render("Docs/state_sick.png", () =>
        {
            StartOfGame(40f, 50f);
            SetPet("crying_02");
            Find<Image>("Pet").color = new Color(0.75f, 0.95f, 0.7f);
            SetBubble("I feel sick...");
            Find<StatBar>("HungerBar").SetValue(0);
            Find<StatBar>("HealthBar").SetValue(0);
            Find<RectTransform>("GameOverPanel").gameObject.SetActive(true);
        });
    }

    /// <summary>Bars at typical in-game values, with the button locks the game would show.</summary>
    private static void StartOfGame(float happiness, float stamina)
    {
        Find<StatBar>("HungerBar").SetValue(80);
        Find<StatBar>("HappinessBar").SetValue(happiness);
        Find<StatBar>("StaminaBar").SetValue(stamina);
        Find<StatBar>("IntelligenceBar").SetValue(10);
        Find<StatBar>("HealthBar").SetValue(100);
        bool exhausted = stamina < 20f;
        Lock("PlayButton", exhausted);
        Lock("StudyButton", exhausted);
    }

    /// <summary>Same look as UIManager.SetUnlocked: locked buttons are faded out.</summary>
    private static void Lock(string button, bool locked)
    {
        var go = Find<RectTransform>(button).gameObject;
        var group = go.GetComponent<CanvasGroup>();
        if (group == null) group = go.AddComponent<CanvasGroup>(); // (?? doesn't work on Unity objects)
        group.alpha = locked ? 0.4f : 1f;
    }

    // ---------- posing helpers ----------

    private static T Find<T>(string name) where T : Component
    {
        foreach (var c in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (c.gameObject.name == name) return c;
        throw new Exception("[ScreenshotCapture] Not found: " + name);
    }

    private static void SetPet(string frame) =>
        Find<Image>("Pet").sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Hamster + frame + ".png");

    private static void SetBubble(string text) =>
        Find<RectTransform>("SpeechBubble").GetComponentInChildren<TextMeshProUGUI>().text = text;

    private static void SetBackground(string name)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Backgrounds + name + ".png");
        var bg = Find<Image>("Background");
        bg.sprite = sprite;
        bg.GetComponent<AspectRatioFitter>().aspectRatio = sprite.rect.width / sprite.rect.height;
    }

    // ---------- rendering ----------

    /// <summary>Renders a wide 1920x1080 window to check the UI stays a centered phone column.</summary>
    [MenuItem("Tamagotchi/Capture Landscape Check")]
    public static void CaptureLandscape()
    {
        Render("Docs/landscape_check.png", null, 1920, 1080);
    }

    private static void Render(string outputPath, Action pose, int w = 0, int h = 0)
    {
        var scene = EditorSceneManager.OpenScene(ProjectSetup.MainScenePath, OpenSceneMode.Single);
        pose?.Invoke();

        if (w <= 0) w = (int)ProjectSetup.ReferenceResolution.x;
        if (h <= 0) h = (int)ProjectSetup.ReferenceResolution.y;
        var cam = Camera.main;
        var canvas = Object.FindAnyObjectByType<Canvas>();
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);

        // Temporarily render the overlay canvas through the camera into a texture.
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 5;
        cam.targetTexture = rt;
        Canvas.ForceUpdateCanvases();
        foreach (var f in Object.FindObjectsByType<PortraitFrame>(FindObjectsSortMode.None)) f.Refresh();
        cam.Render();
        Canvas.ForceUpdateCanvases(); // layout groups / fitters settle after the first pass
        foreach (var f in Object.FindObjectsByType<PortraitFrame>(FindObjectsSortMode.None)) f.Refresh();
        Canvas.ForceUpdateCanvases();
        cam.Render();

        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        File.WriteAllBytes(outputPath, tex.EncodeToPNG());

        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
        // Reload from disk so the temporary changes are never saved.
        EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
        Debug.Log("[ScreenshotCapture] Saved " + Path.GetFullPath(outputPath));
    }
}
