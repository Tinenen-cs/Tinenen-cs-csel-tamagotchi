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
    }
}
