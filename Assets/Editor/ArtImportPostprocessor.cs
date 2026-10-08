using UnityEditor;
using UnityEngine;

/// <summary>
/// Imports every texture under Assets/Art as a crisp pixel-art UI sprite,
/// following the asset pack's recommended settings (Sprite, Single, Point, no compression).
/// Runs automatically whenever an image is added or reimported.
/// </summary>
public class ArtImportPostprocessor : AssetPostprocessor
{
    private const string ArtRoot = "Assets/Art/";

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(ArtRoot)) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.spritePixelsPerUnit = 100;

        // 9-slice borders (left, bottom, right, top) for the generated frames,
        // so they stretch to any size without blurring their rounded corners.
        string file = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        if (assetPath.Contains("/UI/Generated/") && SliceBorders.TryGetValue(file, out Vector4 border))
            importer.spriteBorder = border;
    }

    private static readonly System.Collections.Generic.Dictionary<string, Vector4> SliceBorders = new()
    {
        { "bar_frame", new Vector4(4, 4, 4, 4) },
        { "bar_fill", new Vector4(2, 2, 2, 2) },
        { "button_tile", new Vector4(5, 6, 5, 5) },
        { "panel", new Vector4(6, 6, 6, 6) },
        { "bubble", new Vector4(6, 6, 6, 6) },
    };
}
