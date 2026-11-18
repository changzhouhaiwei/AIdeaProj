#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FishTilePlay
{
    internal sealed class AwardColorEggCoverAssetPostprocessor : AssetPostprocessor
    {
        const string AssetFolder = "Assets/CABRes/Sprites/AwardColorEggCover/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(AssetFolder, System.StringComparison.Ordinal))
                return;

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.spritePixelsPerUnit = 128f;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;

            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.format = TextureImporterFormat.ASTC_4x4;
            android.compressionQuality = 100;
            importer.SetPlatformTextureSettings(android);

            TextureImporterPlatformSettings ios = importer.GetPlatformTextureSettings("iPhone");
            ios.overridden = true;
            ios.format = TextureImporterFormat.ASTC_4x4;
            ios.compressionQuality = 100;
            importer.SetPlatformTextureSettings(ios);
        }
    }
}
#endif
