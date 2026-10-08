using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Renders Main.unity at 1080x1920 into Docs/screenshot.png without entering Play mode.
///
/// Editor:       menu Tamagotchi > Capture Screenshot
/// Command line: Unity -batchmode -quit -projectPath . -executeMethod ScreenshotCapture.Capture
/// (do not pass -nographics; rendering needs a GPU)
/// </summary>
public static class ScreenshotCapture
{
    private const string OutputPath = "Docs/screenshot.png";

    [MenuItem("Tamagotchi/Capture Screenshot")]
    public static void Capture()
    {
        var scene = EditorSceneManager.OpenScene(ProjectSetup.MainScenePath, OpenSceneMode.Single);
        int w = (int)ProjectSetup.ReferenceResolution.x, h = (int)ProjectSetup.ReferenceResolution.y;

        var cam = Camera.main;
        var canvas = Object.FindAnyObjectByType<Canvas>();
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);

        // Temporarily render the overlay canvas through the camera into a texture.
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 5;
        cam.targetTexture = rt;
        Canvas.ForceUpdateCanvases();
        cam.Render();
        Canvas.ForceUpdateCanvases(); // layout groups / fitters settle after the first pass
        cam.Render();

        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;

        Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
        File.WriteAllBytes(OutputPath, tex.EncodeToPNG());

        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
        // Reload from disk so the temporary canvas change is never saved.
        EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
        Debug.Log("[ScreenshotCapture] Saved " + Path.GetFullPath(OutputPath));
    }
}
