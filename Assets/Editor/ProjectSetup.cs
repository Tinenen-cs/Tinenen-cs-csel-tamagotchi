using Tamagotchi.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// One-click project setup. Applies portrait player settings and (re)generates
/// Assets/Scenes/Main.unity from code, so nothing has to be wired by hand.
///
/// Editor:       menu Tamagotchi > Setup Project
/// Command line: Unity -batchmode -quit -projectPath . -executeMethod ProjectSetup.SetupAll
/// </summary>
public static class ProjectSetup
{
    public const string MainScenePath = "Assets/Scenes/Main.unity";

    // Portrait 9:16 reference resolution used by the Canvas Scaler.
    public static readonly Vector2 ReferenceResolution = new Vector2(1080, 1920);

    [MenuItem("Tamagotchi/Setup Project")]
    public static void SetupAll()
    {
        ApplyPlayerSettings();
        AssetDatabase.ImportAsset("Assets/Art", ImportAssetOptions.ImportRecursive);
        BuildMainScene();
        Debug.Log("[ProjectSetup] Done.");
    }

    /// <summary>Locks the app to portrait and sets names / bundle IDs for every platform.</summary>
    public static void ApplyPlayerSettings()
    {
        PlayerSettings.companyName = "Tinenen-cs";
        PlayerSettings.productName = "CSEL Tamagotchi";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.tinenencs.cseltamagotchi");
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.tinenencs.cseltamagotchi");

        // Portrait only.
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;

        // Desktop / browser windows use a 9:16 phone-shaped window.
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 540;
        PlayerSettings.defaultScreenHeight = 960;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.defaultWebScreenWidth = 405;
        PlayerSettings.defaultWebScreenHeight = 720;

        AssetDatabase.SaveAssets();
    }

    /// <summary>Creates Main.unity from scratch: camera, event system, scaled canvas, safe-area root.</summary>
    public static void BuildMainScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera: everything is drawn by the UI canvas, so it only clears the screen.
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(0xFF, 0xF1, 0xE0, 0xFF);
        camGo.transform.position = new Vector3(0, 0, -10);

        // Event system using the new Input System (touch + mouse).
        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<InputSystemUIInputModule>();

        // Canvas: Scale With Screen Size, 1080x1920, match 0.5.
        var canvasGo = new GameObject("Canvas", typeof(RectTransform));
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // Safe-area root: all gameplay UI goes under this.
        var safe = CreateRect("SafeArea", canvasGo.transform);
        Stretch(safe);
        safe.gameObject.AddComponent<SafeArea>();

        EditorSceneManager.SaveScene(scene, MainScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MainScenePath, true) };
    }

    // ---------- helpers ----------

    public static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
