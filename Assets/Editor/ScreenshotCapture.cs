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
        Render("Docs/screenshot.png", null);
    }

    /// <summary>Poses the scene in a few states (eating, sleeping, hangry, playing, sick) and renders each.</summary>
    [MenuItem("Tamagotchi/Capture State Screenshots")]
    public static void CaptureStates()
    {
        Render("Docs/state_eating.png", () =>
        {
            SetPet("eating_02");
            Find<RectTransform>("FoodBowl").gameObject.SetActive(true);
            SetBubble("Yum yum!");
            Find<StatBar>("HungerBar").SetValue(72);
        });
        Render("Docs/state_sleeping.png", () =>
        {
            SetPet("sleeping_02");
            Find<RectTransform>("Bed").gameObject.SetActive(true);
            var pet = Find<RectTransform>("Pet");
            pet.anchoredPosition = new Vector2(0, 0.20f * 536f); // lifted onto the bed (PetController.sleepLift)
            SetBackground("moonlit_bedroom");
            var night = Find<RectTransform>("Background");
            night.pivot = new Vector2(1f, night.pivot.y); // same as PetController.sleepBackgroundAlign
            Find<Image>("DimOverlay").color = new Color(0.05f, 0.05f, 0.2f, 0.45f);
            Find<RectTransform>("SpeechBubble").gameObject.SetActive(false); // hidden while asleep
            Find<StatBar>("EnergyBar").SetValue(35);
        });
        Render("Docs/state_hangry.png", () =>
        {
            SetPet("sad_02");
            Find<Image>("Pet").color = new Color(1f, 0.6f, 0.55f);
            SetBubble("I'm HANGRY! Feed me!");
            Find<StatBar>("HungerBar").SetValue(14);
            Find<StatBar>("HappinessBar").SetValue(48);
        });
        Render("Docs/state_playing.png", () =>
        {
            SetPet("playing_02");
            SetBackground("beach");
            SetBubble("Wheee! Fun!");
        });
        Render("Docs/state_sick.png", () =>
        {
            SetPet("crying_02");
            Find<Image>("Pet").color = new Color(0.75f, 0.95f, 0.7f);
            SetBubble("I feel sick...");
            Find<StatBar>("HungerBar").SetValue(0);
            Find<StatBar>("HealthBar").SetValue(0);
            Find<RectTransform>("GameOverPanel").gameObject.SetActive(true);
        });
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
