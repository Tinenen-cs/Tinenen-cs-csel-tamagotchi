using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Adds a "Phone" 1080x1920 entry to the Game view's resolution dropdown and selects it
/// (once per Editor session), so the game is previewed in portrait without manual setup.
/// Menu: Tamagotchi > Game View: Phone 1080x1920 (re-selects it any time).
///
/// Unity has no public API for Game view sizes, so this uses reflection on Editor internals.
/// If a future Unity version changes them, it logs a warning and you can add the size by hand
/// (Game view > resolution dropdown > + > 1080 x 1920).
/// </summary>
[InitializeOnLoad]
public static class PhoneGameViewSize
{
    private const string Label = "Phone";
    private const int Width = 1080, Height = 1920;
    private const string SessionKey = "Tamagotchi.PhoneGameViewSelected";

    static PhoneGameViewSize()
    {
        if (Application.isBatchMode) return;
        EditorApplication.delayCall += () =>
        {
            bool selectNow = !SessionState.GetBool(SessionKey, false);
            if (Ensure(selectNow) && selectNow) SessionState.SetBool(SessionKey, true);
        };
    }

    [MenuItem("Tamagotchi/Game View: Phone 1080x1920")]
    public static void SelectPhone() => Ensure(select: true);

    /// <summary>Adds the size without touching any window (used by command-line checks).</summary>
    public static void AddOnly()
    {
        if (!Ensure(select: false)) throw new Exception("PhoneGameViewSize failed");
        Debug.Log("[PhoneGameViewSize] Phone 1080x1920 size is available.");
    }

    /// <summary>Adds the size if missing; optionally selects it. Returns false on failure.</summary>
    private static bool Ensure(bool select)
    {
        try
        {
            Assembly editor = typeof(Editor).Assembly;
            Type sizesType = editor.GetType("UnityEditor.GameViewSizes");
            object sizes = typeof(ScriptableSingleton<>).MakeGenericType(sizesType)
                .GetProperty("instance").GetValue(null);
            object group = sizesType.GetMethod("GetGroup")
                .Invoke(sizes, new object[] { (int)CurrentGroup(sizesType, sizes) });
            Type groupType = group.GetType();

            int index = FindSize(group, groupType);
            if (index < 0)
            {
                Type sizeType = editor.GetType("UnityEditor.GameViewSize");
                Type kindType = editor.GetType("UnityEditor.GameViewSizeType");
                object fixedRes = Enum.Parse(kindType, "FixedResolution");
                object newSize = sizeType.GetConstructor(new[] { kindType, typeof(int), typeof(int), typeof(string) })
                    .Invoke(new[] { fixedRes, Width, Height, (object)Label });
                groupType.GetMethod("AddCustomSize").Invoke(group, new[] { newSize });
                sizesType.GetMethod("SaveToHDD").Invoke(sizes, null);
                index = FindSize(group, groupType);
            }

            if (select && index >= 0)
            {
                Type gameViewType = editor.GetType("UnityEditor.GameView");
                var gameView = EditorWindow.GetWindow(gameViewType, false, null, false);
                gameViewType.GetMethod("SizeSelectionCallback",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Invoke(gameView, new object[] { index, null });
            }
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[PhoneGameViewSize] Could not set the Game view to Phone automatically (" +
                             e.GetBaseException().Message + "). Add it by hand: Game view > resolution dropdown > + > 1080 x 1920.");
            return false;
        }
    }

    private static GameViewSizeGroupType CurrentGroup(Type sizesType, object sizes)
    {
        var prop = sizesType.GetProperty("currentGroupType",
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        return prop != null ? (GameViewSizeGroupType)prop.GetValue(prop.GetGetMethod(true).IsStatic ? null : sizes)
                            : GameViewSizeGroupType.Standalone;
    }

    private static int FindSize(object group, Type groupType)
    {
        int total = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null);
        MethodInfo get = groupType.GetMethod("GetGameViewSize");
        for (int i = 0; i < total; i++)
        {
            object size = get.Invoke(group, new object[] { i });
            Type t = size.GetType();
            if ((int)t.GetProperty("width").GetValue(size) == Width &&
                (int)t.GetProperty("height").GetValue(size) == Height &&
                (string)t.GetProperty("baseText").GetValue(size) == Label)
                return i;
        }
        return -1;
    }
}
