using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Makes the project ready to play the moment it opens (e.g. right after cloning):
///  - opens Assets/Scenes/Main.unity if Unity started with the empty "Untitled" scene
///    (the last-opened scene is stored in Library/, which is not in git),
///  - always starts Play mode from Main,
///  - shows the Game view (phone size) and frames the phone layout in the Scene view.
/// Runs once per Editor session; never in batch mode.
/// </summary>
[InitializeOnLoad]
public static class OpenMainSceneOnLoad
{
    private const string SessionKey = "Tamagotchi.MainSceneOpened";

    static OpenMainSceneOnLoad()
    {
        if (Application.isBatchMode) return;
        EditorApplication.delayCall += Run;
    }

    private static void Run()
    {
        var main = AssetDatabase.LoadAssetAtPath<SceneAsset>(ProjectSetup.MainScenePath);
        if (main == null) return;

        // Pressing Play always starts the game from Main, whatever scene is being edited.
        EditorSceneManager.playModeStartScene = main;

        if (SessionState.GetBool(SessionKey, false) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetBool(SessionKey, true);

        Scene active = SceneManager.GetActiveScene();
        bool untitled = string.IsNullOrEmpty(active.path);
        if (untitled && !active.isDirty)
        {
            EditorSceneManager.OpenScene(ProjectSetup.MainScenePath, OpenSceneMode.Single);
            Debug.Log("[OpenMainSceneOnLoad] Opened the Main scene. Press Play to start.");
        }

        if (SceneManager.GetActiveScene().path == ProjectSetup.MainScenePath)
        {
            FrameCanvasInSceneView();
            ShowGameView();
        }
    }

    /// <summary>Zooms the Scene view onto the 1080x1920 phone layout.</summary>
    private static void FrameCanvasInSceneView()
    {
        var canvas = Object.FindAnyObjectByType<Canvas>();
        var view = SceneView.lastActiveSceneView;
        if (canvas == null || view == null) return;
        var corners = new Vector3[4];
        ((RectTransform)canvas.transform).GetWorldCorners(corners);
        var bounds = new Bounds(corners[0], Vector3.zero);
        foreach (var c in corners) bounds.Encapsulate(c);
        if (bounds.size.sqrMagnitude < 1f) return;
        view.in2DMode = true;
        view.Frame(bounds, instant: true);
    }

    /// <summary>Brings the Game tab to the front so the game is visible and ready for Play.</summary>
    private static void ShowGameView()
    {
        var gameViewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        if (gameViewType == null) return;
        var gameView = EditorWindow.GetWindow(gameViewType, false, null, true);
        gameView.Focus();
        PhoneGameViewSize.SelectPhone();
    }
}
