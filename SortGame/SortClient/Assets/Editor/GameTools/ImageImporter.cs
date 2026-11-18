using UnityEngine;
using UnityEditor;

/// <summary>
/// 仅处理 <see cref="TargetFolder"/> 下纹理（参考 bg_1_3）。
/// PPU 每次导入（含 Reimport）固定为 100；bg_1_3 其余项仅在 importSettingsMissing 时套用。
/// User Data 以 GR_TexManual 开头则只保留 PPU，不再改其它项。
/// </summary>
public class ImageImporter : AssetPostprocessor
{
	private const string TargetFolder = "Assets/CABRes/Sprites/SceneBg/";

	private void OnPreprocessTexture()
	{
		if (!assetPath.StartsWith(TargetFolder, System.StringComparison.OrdinalIgnoreCase))
			return;

		var importer = (TextureImporter)assetImporter;
		importer.spritePixelsPerUnit = 100f;

		if (IsManualTextureUserData(importer))
			return;

		if (!assetImporter.importSettingsMissing)
			return;

		bool hasSavedPlatformFormat = HasAnySavedPlatformOverride(importer);

		importer.textureType = TextureImporterType.Sprite;
		importer.spriteImportMode = SpriteImportMode.Single;
		ApplySpriteMeshFieldsLikeBg13(importer);

		importer.sRGBTexture = true;
		importer.mipmapEnabled = false;
		importer.maxTextureSize = 2048;
		importer.filterMode = FilterMode.Bilinear;
		importer.anisoLevel = 1;
		importer.wrapModeU = TextureWrapMode.Clamp;
		importer.wrapModeV = TextureWrapMode.Clamp;
		importer.npotScale = TextureImporterNPOTScale.None;
		importer.alphaSource = TextureImporterAlphaSource.FromInput;
		importer.compressionQuality = 50;

		bool isJpeg = assetPath.EndsWith(".jpg", System.StringComparison.OrdinalIgnoreCase)
			|| assetPath.EndsWith(".jpeg", System.StringComparison.OrdinalIgnoreCase);
		if (isJpeg)
			importer.alphaIsTransparency = false;
		else
			importer.alphaIsTransparency = importer.DoesSourceTextureHaveAlpha();

		if (!hasSavedPlatformFormat)
			ApplyPlatformsLikeBg13(importer);
	}

	private static bool IsManualTextureUserData(TextureImporter importer)
	{
		return !string.IsNullOrEmpty(importer.userData)
		       && importer.userData.StartsWith("GR_TexManual", System.StringComparison.Ordinal);
	}

	/// <summary>meta 中已有任意平台 Override，视为已保存过压缩/格式（含人工修改），不再套用 bg_1_3 平台表。</summary>
	private static bool HasAnySavedPlatformOverride(TextureImporter importer)
	{
		if (importer.GetDefaultPlatformTextureSettings().overridden)
			return true;
		foreach (var platform in new[] { "Standalone", "WebGL", "Android" })
		{
			if (importer.GetPlatformTextureSettings(platform).overridden)
				return true;
		}
		return false;
	}

	/// <summary>对应 meta 中 spriteMeshType:1 / spriteExtrude:1 / spriteGenerateFallbackPhysicsShape:1（部分 Unity 版本无公开 C# 属性）。</summary>
	private static void ApplySpriteMeshFieldsLikeBg13(TextureImporter importer)
	{
		var so = new SerializedObject(importer);
		var meshType = so.FindProperty("m_SpriteMeshType");
		if (meshType != null)
			meshType.intValue = (int)SpriteMeshType.FullRect;
		var extrude = so.FindProperty("m_SpriteExtrude");
		if (extrude != null)
			extrude.intValue = 1;
		var physicsShape = so.FindProperty("m_SpriteGenerateFallbackPhysicsShape");
		if (physicsShape != null)
			physicsShape.boolValue = true;
		so.ApplyModifiedPropertiesWithoutUndo();
	}

	/// <summary>与 bg_1_3.jpg.meta 中 platformSettings 一致。</summary>
	private static void ApplyPlatformsLikeBg13(TextureImporter importer)
	{
		var def = importer.GetDefaultPlatformTextureSettings();
		def.maxTextureSize = 2048;
		def.format = TextureImporterFormat.Automatic;
		def.textureCompression = TextureImporterCompression.Uncompressed;
		def.compressionQuality = 50;
		def.crunchedCompression = false;
		def.overridden = false;
		importer.SetPlatformTextureSettings(def);

		foreach (var platform in new[] { "Standalone", "WebGL" })
		{
			var s = importer.GetPlatformTextureSettings(platform);
			s.maxTextureSize = 2048;
			s.format = TextureImporterFormat.Automatic;
			s.textureCompression = TextureImporterCompression.Compressed;
			s.compressionQuality = 50;
			s.crunchedCompression = false;
			s.overridden = false;
			importer.SetPlatformTextureSettings(s);
		}

		var android = importer.GetPlatformTextureSettings("Android");
		android.overridden = true;
		android.maxTextureSize = 1024;
		android.format = TextureImporterFormat.ASTC_6x6;
		android.textureCompression = TextureImporterCompression.Compressed;
		android.compressionQuality = 50;
		android.crunchedCompression = false;
		importer.SetPlatformTextureSettings(android);

		var iphone = importer.GetPlatformTextureSettings("iPhone");
		iphone.overridden = true;
		iphone.maxTextureSize = 2048;
		iphone.format = TextureImporterFormat.ASTC_6x6;
		iphone.textureCompression = TextureImporterCompression.Compressed;
		iphone.compressionQuality = 50;
		iphone.crunchedCompression = false;
		importer.SetPlatformTextureSettings(iphone);
	}
}
