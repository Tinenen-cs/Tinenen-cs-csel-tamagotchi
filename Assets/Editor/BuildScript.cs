using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// One-click / command-line builds. Output goes to Builds/ (not committed to git).
///
/// Editor:       menu Tamagotchi > Build > WebGL | Android APK | Windows | macOS
/// Command line: Tools/build.sh webgl|android|windows|mac
/// Then:         python Tools/package_release.py   (ready-to-download files in Builds/Release/)
///               (or Unity -batchmode -quit -projectPath . -buildTarget WebGL -executeMethod BuildScript.BuildWebGL)
/// </summary>
public static class BuildScript
{
    public const string WebGLPath = "Builds/WebGL";
    public const string AndroidPath = "Builds/Android/CSEL-Tamagotchi.apk";
    public const string WindowsPath = "Builds/Windows/CSEL-Tamagotchi.exe";
    public const string MacPath = "Builds/macOS/CSEL-Tamagotchi.app";

    private static string[] Scenes => new[] { ProjectSetup.MainScenePath };

    /// <summary>Browser build for GitHub Pages (works on phones and computers).</summary>
    [MenuItem("Tamagotchi/Build/WebGL (browser)")]
    public static void BuildWebGL()
    {
        // GitHub Pages can't send "Content-Encoding" headers, so let the loader unpack gzip itself.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        Build(BuildTarget.WebGL, WebGLPath);
    }

    /// <summary>Installable APK for Android phones (sideload; no Play Store needed).</summary>
    [MenuItem("Tamagotchi/Build/Android APK")]
    public static void BuildAndroid()
    {
        EditorUserBuildSettings.buildAppBundle = false; // .apk, not .aab
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
        Build(BuildTarget.Android, AndroidPath);
    }

    /// <summary>Windows desktop build (portrait 540x960 window).</summary>
    [MenuItem("Tamagotchi/Build/Windows")]
    public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, WindowsPath);

    /// <summary>macOS app (portrait window). Build it on a Mac, or install "Mac Build Support" on Windows.</summary>
    [MenuItem("Tamagotchi/Build/macOS")]
    public static void BuildMac() => Build(BuildTarget.StandaloneOSX, MacPath);

    private static void Build(BuildTarget target, string path)
    {
        ProjectSetup.ApplyPlayerSettings();
        ProjectSetup.ApplyAppIcon();
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".");

        var options = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = path,
            target = target,
            targetGroup = BuildPipeline.GetBuildTargetGroup(target),
            options = BuildOptions.None,
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildScript] {target} build succeeded: {Path.GetFullPath(path)} " +
                      $"({summary.totalSize / (1024f * 1024f):F1} MB, {summary.totalTime.TotalMinutes:F1} min)");
        }
        else
        {
            Debug.LogError($"[BuildScript] {target} build {summary.result} with {summary.totalErrors} error(s).");
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}
