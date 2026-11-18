using System;
using System.Collections.Generic;
using System.Linq;
using cfg.Game;
using FishFramework;
using SimpleJSON;
using TileGameCore;
using UnityEngine;
using UnityEngine.U2D;

namespace FishTilePlay
{
    public sealed class TileSkinRuntimeData
    {
        public string Name { get; }
        public string Color { get; }
        public string Type { get; }
        public int Number { get; }
        public string AssetPath { get; }
        public string AtlasPath { get; }

        private static readonly Dictionary<string, string> ColorAtlasPath = new()
        {
            { "black", "Assets/CABRes/SpriteAtlas/TilesNew/black/blackAtlas.spriteatlasv2" },
            { "blue", "Assets/CABRes/SpriteAtlas/TilesNew/blue/BlueAtlas.spriteatlasv2" },
            { "brown", "Assets/CABRes/SpriteAtlas/TilesNew/brown/BrownAtlas.spriteatlasv2" },
            { "gray", "Assets/CABRes/SpriteAtlas/TilesNew/gray/GrayAtlas.spriteatlasv2" },
            { "green", "Assets/CABRes/SpriteAtlas/TilesNew/green/GreenAtlas.spriteatlasv2" },
            { "orange", "Assets/CABRes/SpriteAtlas/TilesNew/orange/OrangeAtals.spriteatlasv2" },
            { "pink", "Assets/CABRes/SpriteAtlas/TilesNew/pink/PinkAtlas.spriteatlasv2" },
            { "purple", "Assets/CABRes/SpriteAtlas/TilesNew/purple/PurpleAtlas.spriteatlasv2" },
            { "red", "Assets/CABRes/SpriteAtlas/TilesNew/red/RedAtlas.spriteatlasv2" },
            { "white", "Assets/CABRes/SpriteAtlas/TilesNew/white/WhiteAtlas.spriteatlasv2" },
            { "yellow", "Assets/CABRes/SpriteAtlas/TilesNew/yellow/YellowAtlas.spriteatlasv2" },
        };

        private static readonly Dictionary<string, SpriteAtlas> AtlasCache = new();

        private Sprite sprite;
        private bool spriteLoaded;

        public TileSkinRuntimeData(string name, string color, string type, int number)
        {
            Name = name;
            Color = color;
            Type = type;
            Number = number;
            AssetPath = $"Assets/CABRes/SpriteAtlas/TilesNew/{color}/{name}.png";
            AtlasPath = ColorAtlasPath.TryGetValue(color, out string atlasPath) ? atlasPath : "";
        }

        public Sprite LoadSprite()
        {
            if (!spriteLoaded)
            {
                sprite = LoadSpriteFromAtlas();
                if (sprite == null)
                {
                    sprite = UILoadHelper.LoadAsset<Sprite>(AssetPath);
                }
                spriteLoaded = true;
            }

            return sprite;
        }

        private Sprite LoadSpriteFromAtlas()
        {
            SpriteAtlas atlas = LoadAtlas();
            return atlas != null ? atlas.GetSprite(Name) : null;
        }

        private SpriteAtlas LoadAtlas()
        {
            if (string.IsNullOrEmpty(AtlasPath))
            {
                return null;
            }

            if (!AtlasCache.TryGetValue(AtlasPath, out SpriteAtlas atlas))
            {
                atlas = UILoadHelper.LoadAsset<SpriteAtlas>(AtlasPath);
                AtlasCache[AtlasPath] = atlas;
            }

            return atlas;
        }

        public static void ReleaseCachedAssets()
        {
            AtlasCache.Clear();
        }

        public void ReleaseSpriteCache()
        {
            sprite = null;
            spriteLoaded = false;
        }
    }

    public static class TileSkinCatalog
    {
        private const int DisabledOpenLevel = -1;
        private const string TileEffectAtlasPath = "Assets/CABRes/SpriteAtlas/TileEffect/TileEffectAtlas.spriteatlasv2";

        private const string TileEffectFolderPath = "Assets/CABRes/SpriteAtlas/TileEffect/";
        private const string LegacyBackgroundName = "tile_qizi";

        private const string EffectPrefabFolderPath = "Assets/Arts/NewEff/Prefabs/";
        private const string BrokenEffectFolderPath = "Assets/Arts/NewEff/Prefabs/BrokenEff/";
        private const string EffectMaterialFolderPath = "Assets/Arts/NewEff/Material/";
        private const string TileBaseSoundFolderPath = "Assets/FishTileExplorer/Game/Audio/Sounds/";
        private const string TileBaseSoundFolderPathSoEasy = "Assets/FishTileSoEasy/Game/Audio/Sounds/";
        private const string DefaultBrokenSoundName = "white_broken_sound";
        private const string DefaultCollectSoundName = "white_collect_sound";
        public const string DefaultEffectPrefabName = "white_broken_effect";
        public const string CoinRewardEffectPrefabName = "Slot_Highlight_Coin";
        public const string CoinRewardBrokenSoundName = "coin_broken";
        private static readonly string[] EffectMaterialNodeNames = { "Tile_Par01", "Tile_Par_L", "Tile_Par_R" };
        private static readonly string[] TileBaseSoundExtensions = { ".wav", ".mp3", ".WAV" };
        private static readonly Dictionary<string, string> TileBaseSoundAliases = new()
        {
            { "Wodd broken", "wood_break" },
            { "Wodd collect", "wood_collect" },
        };

        private static SpriteAtlas tileEffectAtlas;
        private static readonly Dictionary<int, Sprite> TileBackgroundSpriteCache = new();
        private static readonly Dictionary<int, Sprite> UnknownTileBaseSpriteCache = new();
        private static readonly Dictionary<int, Sprite> StyleSpriteCache = new();
        private static readonly Dictionary<int, Sprite> StyleUnknownSpriteCache = new();
        private static readonly Dictionary<string, GameObject> EffectPrefabCache = new();
        private static readonly Dictionary<string, Material> EffectMaterialCache = new();
        private static readonly Dictionary<string, Dictionary<string, Material>> EffectPrefabOriginalMaterials = new();
        private static readonly Dictionary<string, AudioClip> TileBaseSoundCache = new();
        private static Dictionary<string, AudioClip> tileBaseSoundRegistry;
        private static bool tileBaseSoundRegistryBuilt;

        /// <summary>
        /// 丢掉主题图、皮肤、特效和音效的缓存引用。画面上还挂着的资源不会被这里拆掉。
        /// </summary>
        public static void ReleaseCachedAssets()
        {
            TileSkinRuntimeData.ReleaseCachedAssets();
            if (allSkins != null)
            {
                for (int i = 0; i < allSkins.Count; i++)
                {
                    allSkins[i].ReleaseSpriteCache();
                }
            }

            tileEffectAtlas = null;
            TileBackgroundSpriteCache.Clear();
            UnknownTileBaseSpriteCache.Clear();
            StyleSpriteCache.Clear();
            StyleUnknownSpriteCache.Clear();
            EffectPrefabCache.Clear();
            EffectMaterialCache.Clear();
            EffectPrefabOriginalMaterials.Clear();
            TileBaseSoundCache.Clear();
            tileBaseSoundRegistry = new Dictionary<string, AudioClip>();
            tileBaseSoundRegistryBuilt = false;
        }

        /// <summary>
        /// 按 tilebaseId 加载棋子普通底板（配置 TileBase_UI 字段）。
        /// </summary>
        public static Sprite LoadTileBackgroundSprite(int tilebaseId)
        {
            if (TryLoadStyleSprite(unknown: false, tilebaseId, out Sprite styleSprite))
            {
                return styleSprite;
            }

            return LoadThemeTileBackgroundSprite(tilebaseId);
        }

        public static Sprite LoadUnknownTileBaseSprite(int tilebaseId)
        {
            if (TryLoadStyleSprite(unknown: true, tilebaseId, out Sprite styleSprite))
            {
                return styleSprite;
            }

            return LoadThemeUnknownSprite(tilebaseId);
        }

        static bool TryLoadStyleSprite(bool unknown, int themeFallbackId, out Sprite sprite)
        {
            sprite = null;
            int styleId = LevelDirector.LoadedTileBaseStyle;
            if (styleId < 0)
            {
                return false;
            }

            Dictionary<int, Sprite> cache = unknown ? StyleUnknownSpriteCache : StyleSpriteCache;
            if (cache.TryGetValue(styleId, out sprite))
            {
                return true;
            }

            TileBaseStyleCatalog.Entry row = TileBaseStyleTable.Get(styleId);
            UnityEngine.Sprite direct = row == null ? null : (unknown ? row.unknown : row.tileBase);
            if (direct != null)
            {
                cache[styleId] = direct;
                sprite = direct;
                return true;
            }

            sprite = unknown
                ? LoadThemeUnknownSprite(themeFallbackId)
                : LoadThemeTileBackgroundSprite(themeFallbackId);

            cache[styleId] = sprite;
            return true;
        }

        static Sprite LoadThemeTileBackgroundSprite(int tilebaseId)
        {
            if (TileBackgroundSpriteCache.TryGetValue(tilebaseId, out Sprite cached))
            {
                return cached;
            }

            TileBaseThemePack pack = ThemeUiApplier.Get(tilebaseId);
            Sprite backgroundSprite = pack != null ? pack.tileBase : null;
            if (backgroundSprite == null)
            {
                backgroundSprite = LoadTileEffectSprite(LegacyBackgroundName);
            }

            TileBackgroundSpriteCache[tilebaseId] = backgroundSprite;
            return backgroundSprite;
        }

        static Sprite LoadThemeUnknownSprite(int tilebaseId)
        {
            if (UnknownTileBaseSpriteCache.TryGetValue(tilebaseId, out Sprite cached))
            {
                return cached;
            }

            TileBaseThemePack pack = ThemeUiApplier.Get(tilebaseId);
            Sprite unknownSprite = pack != null ? pack.unknown : null;
            UnknownTileBaseSpriteCache[tilebaseId] = unknownSprite;
            return unknownSprite;
        }

        private static GameTileBaseControlCfg GetTileBaseCfg(int tilebaseId)
        {
            return ConfigHelper.Tables?.TbGameTileBaseControlCfg?.GetOrDefault(tilebaseId);
        }

        private const string IceThemeName = "ice";
        private const string IceExcludedColor = "blue";

        static bool IsIceTheme()
        {
            GameTileBaseControlCfg cfg = GetTileBaseCfg(LevelDirector.LoadedLevelTheme);
            return cfg != null && string.Equals(cfg.Theme, IceThemeName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Ice 主题尽量不出 blue；去掉后池子为空则不滤，避免关卡无法生成。
        /// </summary>
        static List<TileSkinRuntimeData> ExcludeBlueSkinsForIceTheme(List<TileSkinRuntimeData> availableSkins)
        {
            if (availableSkins == null || availableSkins.Count == 0 || !IsIceTheme())
            {
                return availableSkins;
            }

            List<TileSkinRuntimeData> withoutBlue = availableSkins
                .Where(skin => skin != null && skin.Color != IceExcludedColor)
                .ToList();
            return withoutBlue.Count > 0 ? withoutBlue : availableSkins;
        }

        static void AssertNoBlueWhenIceHasOtherColors(
            List<TileSkinRuntimeData> availableSkins,
            List<TileSkinRuntimeData> result)
        {
            if (!IsIceTheme() || availableSkins == null || result == null)
            {
                return;
            }

            if (availableSkins.Exists(skin => skin != null && skin.Color != IceExcludedColor))
            {
                Debug.Assert(result.All(skin => skin == null || skin.Color != IceExcludedColor));
            }
        }

        /// <summary>
        /// 按 tilebaseId 取三消破碎音效名（配置 tile_baroken_sound 字段），为空时回退默认。
        /// 金币棋固定 coin_broken，不走底板表。
        /// </summary>
        public static string GetBrokenSoundName(int tilebaseId, bool isCoinReward = false)
        {
            if (isCoinReward)
            {
                return CoinRewardBrokenSoundName;
            }

            TileBaseThemePack pack = ThemeUiApplier.Get(tilebaseId);
            return pack != null && pack.brokenSound != null ? pack.brokenSound.name : DefaultBrokenSoundName;
        }

        /// <summary>
        /// 按 tilebaseId 取点击收集音效名（配置 Effect_collect_sound 字段），为空时回退默认。
        /// </summary>
        public static string GetCollectSoundName(int tilebaseId)
        {
            TileBaseThemePack pack = ThemeUiApplier.Get(tilebaseId);
            return pack != null && pack.collectSound != null ? pack.collectSound.name : DefaultCollectSoundName;
        }

        /// <summary>
        /// 按配置音效名加载 AudioClip（带缓存），加载失败时回退 fallbackClip。
        /// </summary>
        public static AudioClip LoadTileBaseSound(string soundName, AudioClip fallbackClip = null)
        {
            if (string.IsNullOrEmpty(soundName))
            {
                return fallbackClip;
            }

            if (TileBaseSoundCache.TryGetValue(soundName, out AudioClip cached) && cached != null)
            {
                return cached;
            }

            AudioClip clip = TryGetRegisteredTileBaseSound(soundName);
            if (clip == null
                && TileBaseSoundAliases.TryGetValue(soundName, out string alias)
                && !string.IsNullOrEmpty(alias))
            {
                clip = TryGetRegisteredTileBaseSound(alias);
            }

            if (clip == null)
            {
                string resolvedName = TileBaseSoundAliases.TryGetValue(soundName, out string loadAlias)
                    ? loadAlias
                    : soundName;

                clip = LoadTileBaseSoundFromFolders(resolvedName);

                if (clip == null && resolvedName != soundName)
                {
                    clip = LoadTileBaseSoundFromFolders(soundName);
                }
            }

            if (clip == null)
            {
                Debug.LogWarning($"[TileSkinCatalog] Failed to load tile base sound '{soundName}', fallback will be used.");
            }

            TileBaseSoundCache[soundName] = clip;
            return clip ?? fallbackClip;
        }

        static AudioClip LoadTileBaseSoundFromFolders(string soundName)
        {
            string[] folders = { TileBaseSoundFolderPath, TileBaseSoundFolderPathSoEasy };
            for (int i = 0; i < folders.Length; i++)
            {
                for (int j = 0; j < TileBaseSoundExtensions.Length; j++)
                {
                    AudioClip clip = UILoadHelper.LoadAsset<AudioClip>($"{folders[i]}{soundName}{TileBaseSoundExtensions[j]}");
                    if (clip != null)
                    {
                        return clip;
                    }
                }
            }

            return null;
        }

        private static AudioClip TryGetRegisteredTileBaseSound(string soundName)
        {
            EnsureTileBaseSoundRegistry();
            return tileBaseSoundRegistry.TryGetValue(soundName, out AudioClip clip) && clip != null ? clip : null;
        }

        private static void EnsureTileBaseSoundRegistry()
        {
            if (tileBaseSoundRegistryBuilt)
            {
                return;
            }

            AudioClips clips = AudioController.AudioClips;
            if (clips == null)
            {
                return;
            }

            List<AudioClip> list = clips.tileBaseSounds;
            tileBaseSoundRegistryBuilt = true;
            tileBaseSoundRegistry = new Dictionary<string, AudioClip>();
            if (list == null)
            {
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                AudioClip clip = list[i];
                if (clip == null)
                {
                    continue;
                }

                RegisterTileBaseSound(clip.name, clip);
            }

            foreach (KeyValuePair<string, string> pair in TileBaseSoundAliases)
            {
                if (tileBaseSoundRegistry.TryGetValue(pair.Value, out AudioClip aliasClip) && aliasClip != null)
                {
                    RegisterTileBaseSound(pair.Key, aliasClip);
                }
            }
        }

        private static void RegisterTileBaseSound(string soundName, AudioClip clip)
        {
            if (string.IsNullOrEmpty(soundName) || clip == null)
            {
                return;
            }

            tileBaseSoundRegistry[soundName] = clip;
        }

        /// <summary>
        /// 按 tilebaseId 取消除特效预制体名（配置 tile_baroken_effect 字段），为空时回退默认。
        /// </summary>
        public static string GetEffectPrefabName(int tilebaseId)
        {
            GameObject styleEffect = TileBaseStyleTable.Current?.brokenEffect;
            if (styleEffect != null)
            {
                return styleEffect.name;
            }

            TileBaseThemePack pack = ThemeUiApplier.Get(tilebaseId);
            return pack != null && pack.brokenEffect != null ? pack.brokenEffect.name : DefaultEffectPrefabName;
        }

        /// <summary>
        /// 按名字加载消除特效预制体（带缓存），加载失败时回退默认预制体。
        /// </summary>
        public static GameObject LoadEffectPrefab(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName))
            {
                prefabName = DefaultEffectPrefabName;
            }

            if (EffectPrefabCache.TryGetValue(prefabName, out GameObject cached))
            {
                return cached;
            }

            GameObject prefab = UILoadHelper.LoadAsset<GameObject>($"{EffectPrefabFolderPath}{prefabName}.prefab");
            if (prefab == null)
            {
                prefab = UILoadHelper.LoadAsset<GameObject>($"{BrokenEffectFolderPath}{prefabName}.prefab");
            }
            if (prefab == null)
            {
                GameObject styleEffect = TileBaseStyleTable.Current?.brokenEffect;
                if (styleEffect != null && styleEffect.name == prefabName)
                {
                    prefab = styleEffect;
                }
            }
            if (prefab == null)
            {
                TileBaseThemePack pack = ThemeUiApplier.Get(LevelDirector.LoadedLevelTheme);
                if (pack != null && pack.brokenEffect != null && pack.brokenEffect.name == prefabName)
                {
                    prefab = pack.brokenEffect;
                }
            }
            if (prefab == null && prefabName != DefaultEffectPrefabName)
            {
                prefab = LoadEffectPrefab(DefaultEffectPrefabName);
            }

            EffectPrefabCache[prefabName] = prefab;
            CacheOriginalMaterials(prefabName, prefab);
            return prefab;
        }

        /// <summary>
        /// 按主题取消除特效材质。经典底板直接换预制体，不再改材质。
        /// </summary>
        public static Material LoadTileBaseMaterial(int tilebaseId)
        {
            TileBaseThemePack pack = ThemeUiApplier.Get(tilebaseId);
            Material material = pack != null ? pack.brokenMat : null;
            if (material == null)
            {
                return null;
            }

            if (EffectMaterialCache.TryGetValue(material.name, out Material cached))
            {
                return cached;
            }

            EffectMaterialCache[material.name] = material;
            return material;
        }

        /// <summary>
        /// 将 tilebaseId 对应的材质应用到特效实例的 Tile_Par01/Tile_Par_L/Tile_Par_R 节点。
        /// 配置材质为空/缺失时，恢复预制体原始材质（避免池化复用时残留上一次的材质）。
        /// </summary>
        public static void ApplyTileBaseMaterial(GameObject effectRoot, string effectName, int tilebaseId)
        {
            if (effectRoot == null)
            {
                return;
            }

            Material configMaterial = LoadTileBaseMaterial(tilebaseId);
            EffectPrefabOriginalMaterials.TryGetValue(
                string.IsNullOrEmpty(effectName) ? DefaultEffectPrefabName : effectName,
                out Dictionary<string, Material> originals);

            ParticleSystemRenderer[] renderers = effectRoot.GetComponentsInChildren<ParticleSystemRenderer>(true);
            foreach (ParticleSystemRenderer renderer in renderers)
            {
                if (Array.IndexOf(EffectMaterialNodeNames, renderer.gameObject.name) < 0)
                {
                    continue;
                }

                if (configMaterial != null)
                {
                    renderer.sharedMaterial = configMaterial;
                }
                else if (originals != null && originals.TryGetValue(renderer.gameObject.name, out Material original))
                {
                    renderer.sharedMaterial = original;
                }
            }
        }

        private static void CacheOriginalMaterials(string prefabName, GameObject prefab)
        {
            if (prefab == null || string.IsNullOrEmpty(prefabName) || EffectPrefabOriginalMaterials.ContainsKey(prefabName))
            {
                return;
            }

            Dictionary<string, Material> originals = new();
            ParticleSystemRenderer[] renderers = prefab.GetComponentsInChildren<ParticleSystemRenderer>(true);
            foreach (ParticleSystemRenderer renderer in renderers)
            {
                if (Array.IndexOf(EffectMaterialNodeNames, renderer.gameObject.name) < 0)
                {
                    continue;
                }

                originals[renderer.gameObject.name] = renderer.sharedMaterial;
            }

            EffectPrefabOriginalMaterials[prefabName] = originals;
        }

        private static Sprite LoadTileEffectSprite(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName))
            {
                return null;
            }

            SpriteAtlas atlas = LoadTileEffectAtlas();
            Sprite sprite = atlas != null ? atlas.GetSprite(spriteName) : null;
            if (sprite == null)
            {
                sprite = UILoadHelper.LoadAsset<Sprite>($"{TileEffectFolderPath}{spriteName}.png");
            }

            return sprite;
        }

        public static Sprite LoadCoinRewardSprite()
        {
            return LoadTileEffectSprite(LevelDirector.CoinRewardSkinName);
        }

        private static SpriteAtlas LoadTileEffectAtlas()
        {
            if (tileEffectAtlas == null)
            {
                tileEffectAtlas = UILoadHelper.LoadAsset<SpriteAtlas>(TileEffectAtlasPath);
            }

            return tileEffectAtlas;
        }

        private static readonly Dictionary<string, int> DefaultColorPriority = new()
        {
            { "yellow", 1 },
            { "green", 1 },
            { "blue", 1 },
            { "red", 2 },
            { "orange", 2 },
            { "purple", 2 },
            { "pink", 3 },
            { "white", 3 },
            { "black", 3 },
        };

        private static readonly HashSet<string> CoinRewardExcludedColors = new()
        {
            "yellow",
            "brown",
            "orange",
        };

        private static readonly Dictionary<string, TileSkinRuntimeData> SkinByName = new();
        private static readonly Dictionary<string, int> ColorPriority = new();
        private static readonly Dictionary<string, int> ColorPriorityIn = new();
        private static readonly Dictionary<string, int> DefaultColorOrder = new();
        private static List<TileSkinRuntimeData> allSkins;
        private static bool priorityLoaded;

        public static IReadOnlyList<TileSkinRuntimeData> GetAvailableSkins(int levelNumber)
        {
            EnsureSkins();

            var openLevelCfgs = ConfigHelper.Tables?.TbGameTileOpenLevelCfg?.DataList;
            if (openLevelCfgs == null)
            {
                return Array.Empty<TileSkinRuntimeData>();
            }

            List<TileSkinRuntimeData> result = new();
            foreach (GameTileOpenLevelCfg cfg in openLevelCfgs)
            {
                if (cfg.Level == DisabledOpenLevel)
                {
                    continue;
                }

                if (cfg.Level > levelNumber)
                {
                    continue;
                }

                if (SkinByName.TryGetValue(cfg.Name, out TileSkinRuntimeData skin))
                {
                    result.Add(skin);
                }
            }

            return result;
        }

        public static IReadOnlyList<TileSkinRuntimeData> GetSkinsUnlockedAtLevel(int levelNumber)
        {
            EnsureSkins();

            var openLevelCfgs = ConfigHelper.Tables?.TbGameTileOpenLevelCfg?.DataList;
            if (openLevelCfgs == null)
            {
                return Array.Empty<TileSkinRuntimeData>();
            }

            List<TileSkinRuntimeData> result = new();
            foreach (GameTileOpenLevelCfg cfg in openLevelCfgs)
            {
                if (cfg.Level == DisabledOpenLevel || cfg.Level != levelNumber)
                {
                    continue;
                }

                if (SkinByName.TryGetValue(cfg.Name, out TileSkinRuntimeData skin))
                {
                    result.Add(skin);
                }
            }

            return result;
        }

        public static TileSkinRuntimeData GetSkin(string skinName)
        {
            if (string.IsNullOrEmpty(skinName))
            {
                return null;
            }

            EnsureSkins();
            return SkinByName.TryGetValue(skinName, out TileSkinRuntimeData skin) ? skin : null;
        }

        public static int GetRuntimeColorPriority(string color)
        {
            return GetColorPriority(color);
        }

        public static int GetRuntimeColorOrder(string color)
        {
            return GetColorPriorityIn(color);
        }

        /// <summary>
        /// 金币关独立花色：不读 colorAllocate / 连胜连败 / 首次保底。
        /// 花色名不重复；颜色先互不重复，用尽后再允许同色不同花色。
        /// </summary>
        public static List<TileSkinRuntimeData> BuildCoinLevelSkins(int otherGroupCount, int levelNumber, int coinTileCount = -1)
        {
            EnsureSkins();

            List<TileSkinRuntimeData> availableSkins = ExcludeBlueSkinsForIceTheme(
                GetAvailableSkins(levelNumber)
                    .Where(skin => skin != null && !CoinRewardExcludedColors.Contains(skin.Color))
                    .ToList()
            );
            if (availableSkins.Count == 0 || otherGroupCount <= 0)
            {
                Debug.Log(
                    $"[TileSkin] coinLevel={levelNumber}, coinGroups={coinTileCount / 3}, " +
                    $"otherGroups={otherGroupCount}, generated=0, colors=none"
                );
                return new List<TileSkinRuntimeData>();
            }

            List<string> colors = availableSkins
                .Select(skin => skin.Color)
                .Distinct()
                .OrderBy(_ => UnityEngine.Random.value)
                .ToList();

            HashSet<string> usedSkins = new();
            HashSet<string> usedColors = new();
            List<TileSkinRuntimeData> result = new();
            for (int i = 0; i < otherGroupCount; i++)
            {
                TileSkinRuntimeData skin = PickCoinLevelSkin(availableSkins, usedSkins, colors, usedColors);
                if (skin == null)
                {
                    break;
                }

                usedSkins.Add(skin.Name);
                usedColors.Add(skin.Color);
                result.Add(skin);
            }

            Debug.Assert(result.All(skin => !CoinRewardExcludedColors.Contains(skin.Color)));
            AssertNoBlueWhenIceHasOtherColors(availableSkins, result);
            Debug.Assert(result.Select(skin => skin.Name).Distinct().Count() == result.Count);
            int availableColorCount = colors.Count;
            int resultColorCount = result.Select(skin => skin.Color).Distinct().Count();
            if (result.Count <= availableColorCount)
            {
                Debug.Assert(resultColorCount == result.Count);
            }
            else
            {
                Debug.Assert(resultColorCount == availableColorCount);
            }

            string colorsText = result.Count > 0
                ? string.Join(",", result.Select(skin => skin.Color).Distinct())
                : "none";
            Debug.Log(
                $"[TileSkin] coinLevel={levelNumber}, coinGroups={coinTileCount / 3}, " +
                $"otherGroups={otherGroupCount}, generated={result.Count}, colors={colorsText}"
            );

            return result;
        }

        static TileSkinRuntimeData PickCoinLevelSkin(
            List<TileSkinRuntimeData> availableSkins,
            HashSet<string> usedSkins,
            List<string> colors,
            HashSet<string> usedColors)
        {
            foreach (string color in colors)
            {
                if (usedColors.Contains(color))
                {
                    continue;
                }

                TileSkinRuntimeData unusedColorSkin = PickFromCandidates(
                    availableSkins,
                    usedSkins,
                    item => item.Color == color
                );
                if (unusedColorSkin != null)
                {
                    return unusedColorSkin;
                }
            }

            return PickFromCandidates(availableSkins, usedSkins, _ => true);
        }

        /// <summary>
        /// 该颜色可用花色数。NotAllowed==1 不计。已释出、更晚关卡、不再释出都计入。同色即可，类型可重复。
        /// </summary>
        public static int CountColorChallengeSkins(string color, int levelNumber)
        {
            SplitColorChallengeSkins(color, levelNumber, out List<TileSkinRuntimeData> inRange, out List<TileSkinRuntimeData> outRange, out _);
            return CountNames(inRange, outRange);
        }

        /// <summary>
        /// 颜色挑战关：只从 colorchallenge 指定颜色里随机取 targetCount 个花色。
        /// 先取已释出且 NotAllowed!=1 的棋子。已释出花色不够时，再用更晚关卡或不再释出的同色花色补。
        /// 同色花色总数仍不够时返回空表。
        /// </summary>
        public static List<TileSkinRuntimeData> BuildColorChallengeSkins(
            string color,
            int levelNumber,
            int targetCount,
            string reservedColor = "",
            int reservedColorCount = 0)
        {
            List<TileSkinRuntimeData> result = new();
            SplitColorChallengeSkins(color, levelNumber, out List<TileSkinRuntimeData> inRange, out List<TileSkinRuntimeData> outRange, out HashSet<string> blockedNames);
            if (targetCount <= 0)
            {
                return result;
            }

            int skinTotal = CountNames(inRange, outRange);
            if (skinTotal < targetCount)
            {
                return result;
            }

            HashSet<string> usedNames = new();
            TakeSkins(inRange, usedNames, result, targetCount);
            if (result.Count < targetCount)
            {
                TakeSkins(outRange, usedNames, result, targetCount);
            }

            int beforeReserve = result.Count;
            bool filledFromInRange = result.TrueForAll(skin => inRange.Contains(skin));
            List<string> goldtreeReplaceDetails = new();
            RemoveReservedColorSkins(
                usedNames,
                result,
                reservedColor,
                reservedColorCount,
                goldtreeReplaceDetails
            );

            string want = string.IsNullOrEmpty(color) ? "" : color.Trim();
            Debug.Assert(beforeReserve == targetCount);
            Debug.Assert(result.All(skin => string.Equals(skin.Color, want, StringComparison.OrdinalIgnoreCase)));
            Debug.Assert(result.All(skin => !blockedNames.Contains(skin.Name)));
            Debug.Assert(result.Select(skin => skin.Name).Distinct().Count() == result.Count);
            if (CountNames(inRange, null) >= targetCount)
            {
                Debug.Assert(filledFromInRange);
            }

            string namesText = result.Count > 0
                ? string.Join(",", result.Select(skin => skin.Name))
                : "none";
            Debug.Log(
                $"[TileSkin] colorChallenge level={levelNumber}, color={want}, " +
                $"groupCount={targetCount}, generated={result.Count}, " +
                $"inRange={inRange.Count}, outRange={outRange.Count}, blocked={blockedNames.Count}, " +
                $"skins={namesText}"
            );

            return result;
        }

        static int CountNames(List<TileSkinRuntimeData> first, List<TileSkinRuntimeData> second)
        {
            HashSet<string> names = new();
            if (first != null)
            {
                for (int i = 0; i < first.Count; i++)
                {
                    names.Add(first[i].Name);
                }
            }

            if (second != null)
            {
                for (int i = 0; i < second.Count; i++)
                {
                    names.Add(second[i].Name);
                }
            }

            return names.Count;
        }

        static void TakeSkins(
            List<TileSkinRuntimeData> source,
            HashSet<string> usedNames,
            List<TileSkinRuntimeData> result,
            int targetCount)
        {
            List<TileSkinRuntimeData> picked = source
                .Where(skin => skin != null && !usedNames.Contains(skin.Name))
                .OrderBy(_ => UnityEngine.Random.value)
                .ToList();
            for (int i = 0; i < picked.Count && result.Count < targetCount; i++)
            {
                usedNames.Add(picked[i].Name);
                result.Add(picked[i]);
            }
        }

        static void SplitColorChallengeSkins(
            string color,
            int levelNumber,
            out List<TileSkinRuntimeData> inRange,
            out List<TileSkinRuntimeData> outRange,
            out HashSet<string> blockedNames)
        {
            EnsureSkins();
            inRange = new List<TileSkinRuntimeData>();
            outRange = new List<TileSkinRuntimeData>();
            blockedNames = new HashSet<string>();
            string want = string.IsNullOrEmpty(color) ? "" : color.Trim();
            if (want.Length == 0)
            {
                return;
            }

            var openLevelCfgs = ConfigHelper.Tables?.TbGameTileOpenLevelCfg?.DataList;
            if (openLevelCfgs == null)
            {
                return;
            }

            foreach (GameTileOpenLevelCfg cfg in openLevelCfgs)
            {
                if (!SkinByName.TryGetValue(cfg.Name, out TileSkinRuntimeData skin))
                {
                    continue;
                }

                if (!string.Equals(skin.Color, want, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (cfg.NotAllowed == 1)
                {
                    blockedNames.Add(skin.Name);
                    continue;
                }

                if (cfg.Level != DisabledOpenLevel && cfg.Level <= levelNumber)
                {
                    inRange.Add(skin);
                }
                else
                {
                    outRange.Add(skin);
                }
            }
        }

        public static List<TileSkinRuntimeData> BuildLevelSkins(
            int cfgValue,
            int levelNumber,
            int targetCount,
            int totalTileCount = -1,
            string reservedColor = "",
            int reservedColorCount = 0,
            bool isFirstEnter = false)
        {
            EnsureSkins();
            EnsureColorPriority();

            List<TileSkinRuntimeData> availableSkins = ExcludeBlueSkinsForIceTheme(GetAvailableSkins(levelNumber).ToList());
            if (availableSkins.Count == 0 || targetCount <= 0)
            {
                Debug.Log($"[TileSkin] level={levelNumber}, cfg={cfgValue}, no available TilesNew skin. available={availableSkins.Count}, groupCount={targetCount}, tileTotal={totalTileCount}");
                return new List<TileSkinRuntimeData>();
            }

            List<string> allColors = availableSkins
                .Select(skin => skin.Color)
                .Distinct()
                .OrderBy(GetColorPriority)
                .ThenBy(GetColorPriorityIn)
                .ThenBy(color => GetDefaultColorOrder(color))
                .ToList();
            List<string> allTypes = availableSkins
                .Select(skin => skin.Type)
                .Distinct()
                .OrderBy(type => type)
                .ToList();

            List<string> selectedTypes = PickTypes(cfgValue, allTypes);
            if (selectedTypes.Count == 0)
            {
                Debug.Log($"[TileSkin] level={levelNumber}, cfg={cfgValue}, no selected tile type. availableTypes={string.Join(",", allTypes)}, groupCount={targetCount}, tileTotal={totalTileCount}");
                return new List<TileSkinRuntimeData>();
            }

            List<int> colorWeights = GetColorWeights(cfgValue, targetCount);
            int colorCount = Mathf.Clamp(GetColorPickCount(cfgValue), 1, Mathf.Min(allColors.Count, colorWeights.Count));
            List<string> selectedColors = PickColorsByPriority(allColors, colorCount);
            List<ColorAllocation> allocations = BuildAllocations(selectedColors, colorWeights, targetCount);
            List<string> typeSequence = BuildTypeSequence(selectedTypes, targetCount);

            HashSet<string> usedSkins = new();
            List<TileSkinRuntimeData> result = new();
            List<string> fallbackDetails = new();
            List<string> goldtreeReplaceDetails = new();
            List<TileSkinRuntimeData> mandatorySkins = ApplyFirstEnterMandatorySkins(
                isFirstEnter,
                levelNumber,
                selectedColors,
                selectedTypes,
                allocations,
                usedSkins,
                result
            );
            foreach (ColorAllocation allocation in allocations)
            {
                for (int i = 0; i < allocation.Count && result.Count < targetCount; i++)
                {
                    string preferredType = result.Count < typeSequence.Count
                        ? typeSequence[result.Count]
                        : GetRandomItem(selectedTypes);
                    TileSkinRuntimeData skin = PickSkin(
                        availableSkins,
                        usedSkins,
                        allocation.Color,
                        preferredType,
                        selectedTypes,
                        out string fallbackDetail
                    );
                    if (skin == null)
                    {
                        break;
                    }

                    if (!string.IsNullOrEmpty(fallbackDetail))
                    {
                        fallbackDetails.Add(fallbackDetail);
                    }

                    usedSkins.Add(skin.Name);
                    result.Add(skin);
                }
            }

            while (result.Count < targetCount)
            {
                TileSkinRuntimeData skin = PickAnySkin(availableSkins, usedSkins);
                if (skin == null)
                {
                    break;
                }

                fallbackDetails.Add($"分配数量不足: groupIndex={result.Count}, skin={skin.Name}, actualColor={skin.Color}, actualType={skin.Type}");
                usedSkins.Add(skin.Name);
                result.Add(skin);
            }

            RemoveReservedColorSkins(
                usedSkins,
                result,
                reservedColor,
                reservedColorCount,
                goldtreeReplaceDetails
            );

            List<string> missingTypes = selectedTypes
                .Where(type => result.All(skin => skin.Type != type))
                .ToList();
            List<string> resultColors = BuildLogColorOrder(result, selectedColors);
            List<string> resultTypes = result
                .Select(skin => skin.Type)
                .Distinct()
                .OrderBy(type => type)
                .ToList();

            int winCnt = 0;
            int failCnt = 0;
            if (GPUserDataManager.Instance != null && GPUserDataManager.Instance.UserData != null)
            {
                winCnt = GPUserDataManager.Instance.UserData.continusWinCnt;
                failCnt = GPUserDataManager.Instance.UserData.continusFailCnt;
            }

            string fallbackText = fallbackDetails.Count > 0
                ? string.Join(" | ", fallbackDetails)
                : "none";
            string goldtreeReplaceText = goldtreeReplaceDetails.Count > 0
                ? string.Join(" | ", goldtreeReplaceDetails)
                : "none";
            string firstEnterMandatoryText = mandatorySkins.Count > 0
                ? string.Join(",", mandatorySkins.Select(skin => skin.Name))
                : "none";
            List<TileSkinRuntimeData> newUnlockedSkins = GetSkinsUnlockedAtLevel(levelNumber).ToList();
            string newUnlockedSkinsText = newUnlockedSkins.Count > 0
                ? string.Join("\n", newUnlockedSkins.Select(skin => $"  {skin.Name}"))
                : "none";

            // [TileSkin] 日志字段说明：
            // level            当前关卡号（1-based）
            // cfg              动态配置第一段花色数（连胜/连败会变化）
            // winCnt / failCnt 当前连胜、连败次数
            // groupCount       本局计划生成的棋子组数（1组=3颗）
            // generatedGroups  实际生成的棋子组数
            // tileTotal        关卡棋盘总棋子数
            // colors           本局最终棋盘出现的颜色，按 colorAllocate 主色槽位顺序
            // types            本局最终棋盘出现的棋子类型
            // missingTypes     计划选中但未出现在结果中的类型
            // fallback         兜底选棋记录（资源不足时跨颜色/类型替换），none 表示无
            // goldtreeReplace  goldtree 活动占用黄色组时移除的普通棋子信息
            // firstEnterMandatory 首次进关时强制保底进局的新解锁棋子名（skin.Name）
            // newUnlockedSkins 配置表定义的本关新开放棋子列表（CSVGameTileOpenLevelCfg.level==当前关）
            // tilebaseId       本局随机/配置确定的棋子底板 id（LoadedLevelTheme）
            Debug.Log(
                $"[TileSkin] level={levelNumber}, cfg={cfgValue}, winCnt={winCnt}, failCnt={failCnt}, " +
                $"groupCount={targetCount}, generatedGroups={result.Count}, tileTotal={totalTileCount}, " +
                $"colors={string.Join(",", resultColors)}, types={string.Join(",", resultTypes)}, " +
                $"missingTypes={string.Join(",", missingTypes)}, fallback={fallbackText}, " +
                $"goldtreeReplace={goldtreeReplaceText}, firstEnterMandatory={firstEnterMandatoryText}\n " +
                $"tilebaseId={LevelDirector.LoadedLevelTheme}," +
                $"newUnlockedSkins={newUnlockedSkinsText}"
            );

            AssertNoBlueWhenIceHasOtherColors(availableSkins, result);
            LogMissingTextureResources(levelNumber, result);

            return result;
        }

        /// <summary>
        /// 汇总日志之后：校验本局已选皮肤是否能加载到纹理，缺失则单独打 Error。
        /// </summary>
        private static void LogMissingTextureResources(int levelNumber, List<TileSkinRuntimeData> skins)
        {
            if (skins == null || skins.Count == 0)
            {
                return;
            }

            List<string> missingDetails = new();
            foreach (TileSkinRuntimeData skin in skins)
            {
                if (skin == null)
                {
                    missingDetails.Add("skin=null");
                    continue;
                }

                if (skin.LoadSprite() != null)
                {
                    continue;
                }

                missingDetails.Add(
                    $"skin={skin.Name}, color={skin.Color}, type={skin.Type}, " +
                    $"assetPath={skin.AssetPath}, atlasPath={skin.AtlasPath}"
                );
            }

            if (missingDetails.Count == 0)
            {
                return;
            }

            Debug.LogWarning(
                $"[TileSkin] 缺少纹理资源 level={levelNumber}, count={missingDetails.Count}, " +
                $"details={string.Join(" | ", missingDetails)}"
            );
        }

        private static void EnsureSkins()
        {
            // Allow rebuild when first call happened before ConfigHelper.EnsureInitialized (Test Level).
            if (allSkins != null && (allSkins.Count > 0 || ConfigHelper.Tables?.TbGameTileOpenLevelCfg == null))
            {
                return;
            }

            allSkins = new List<TileSkinRuntimeData>();
            SkinByName.Clear();
            DefaultColorOrder.Clear();

            var openLevelCfgs = ConfigHelper.Tables?.TbGameTileOpenLevelCfg?.DataList;
            if (openLevelCfgs == null)
            {
                return;
            }

            foreach (GameTileOpenLevelCfg cfg in openLevelCfgs)
            {
                if (!TryParseSkinName(cfg.Name, out string color, out string type, out int number))
                {
                    continue;
                }

                if (SkinByName.ContainsKey(cfg.Name))
                {
                    continue;
                }

                TileSkinRuntimeData skin = new(cfg.Name, color, type, number);
                SkinByName.Add(cfg.Name, skin);
                allSkins.Add(skin);

                if (!DefaultColorOrder.ContainsKey(color))
                {
                    DefaultColorOrder[color] = DefaultColorOrder.Count;
                }
            }
        }

        private static bool TryParseSkinName(string skinName, out string color, out string type, out int number)
        {
            color = "";
            type = "";
            number = 0;

            if (string.IsNullOrEmpty(skinName))
            {
                return false;
            }

            string[] parts = skinName.Split('_');
            if (parts.Length != 3 || !int.TryParse(parts[2], out number))
            {
                return false;
            }

            color = parts[0];
            type = parts[1];
            return !string.IsNullOrEmpty(color) && !string.IsNullOrEmpty(type);
        }

        private static void EnsureColorPriority()
        {
            if (priorityLoaded)
            {
                return;
            }

            priorityLoaded = true;
            ColorPriority.Clear();
            ColorPriorityIn.Clear();

            const string colorPriorityPath = "Assets/CABRes/Configs/game_tbgamecolorprioritycfg.json";
            TextAsset textAsset = ResourceModule.LoadAsset<TextAsset>(colorPriorityPath);
#if UNITY_EDITOR
            if (textAsset == null)
            {
                textAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(colorPriorityPath);
            }
#endif
            if (textAsset != null)
            {
                JSONNode json = JSON.Parse(System.Text.Encoding.UTF8.GetString(textAsset.bytes));
                if (json != null)
                {
                    foreach (JSONNode row in json.Children)
                    {
                        string color = ReadString(row, "color", "Color", "name", "Name", "colorName", "ColorName");
                        int priority = ReadInt(row, 0, "priority", "Priority", "level", "Level", "sort", "Sort");
                        int priorityIn = ReadInt(row, 0, "Priority_in", "priority_in", "PriorityIn", "priorityIn");
                        if (!string.IsNullOrEmpty(color) && priority > 0)
                        {
                            ColorPriority[color] = priority;
                        }
                        if (!string.IsNullOrEmpty(color) && priorityIn > 0)
                        {
                            ColorPriorityIn[color] = priorityIn;
                        }
                    }
                }
            }

            foreach (var item in DefaultColorPriority)
            {
                if (!ColorPriority.ContainsKey(item.Key))
                {
                    ColorPriority[item.Key] = item.Value;
                }
                if (!ColorPriorityIn.ContainsKey(item.Key))
                {
                    ColorPriorityIn[item.Key] = GetDefaultColorOrder(item.Key);
                }
            }
        }

        private static string ReadString(JSONNode row, params string[] keys)
        {
            foreach (string key in keys)
            {
                if (row[key] != null && row[key].IsString)
                {
                    string value = row[key].Value;
                    int underlineIndex = value.IndexOf('_');
                    return underlineIndex > 0 ? value.Substring(0, underlineIndex) : value;
                }
            }

            return "";
        }

        private static int ReadInt(JSONNode row, int defaultValue, params string[] keys)
        {
            foreach (string key in keys)
            {
                if (row[key] != null && row[key].IsNumber)
                {
                    return row[key].AsInt;
                }
            }

            return defaultValue;
        }

        private static List<string> PickTypes(int cfgValue, List<string> allTypes)
        {
            int count = cfgValue <= 4 ? cfgValue : 4;
            count = Mathf.Clamp(count, 1, allTypes.Count);
            return allTypes
                .OrderBy(_ => UnityEngine.Random.value)
                .Take(count)
                .ToList();
        }

        private static GameColorAllocateLevelCfg GetAllocateCfg(int cfgValue)
        {
            GameColorAllocateLevelCfg allocateCfg = ConfigHelper.Tables?.TbGameColorAllocateLevelCfg?.GetOrDefault(cfgValue);
            if (allocateCfg == null)
            {
                allocateCfg = ConfigHelper.Tables?.TbGameColorAllocateLevelCfg?.DataList
                    .FirstOrDefault(item => item.Config == cfgValue);
            }

            return allocateCfg;
        }

        private static int GetColorPickCount(int cfgValue)
        {
            GameColorAllocateLevelCfg allocateCfg = GetAllocateCfg(cfgValue);
            if (allocateCfg != null)
            {
                if (allocateCfg.ColorPick > 0)
                {
                    return allocateCfg.ColorPick;
                }

                List<int> weights = ParseIntList(allocateCfg.ColorAllocate);
                if (weights.Count > 0)
                {
                    return weights.Count;
                }
            }

            return Mathf.Max(1, cfgValue);
        }

        private static List<int> GetColorWeights(int cfgValue, int targetCount)
        {
            GameColorAllocateLevelCfg allocateCfg = GetAllocateCfg(cfgValue);
            if (allocateCfg != null)
            {
                List<int> weights = ParseIntList(allocateCfg.ColorAllocate);
                if (weights.Count > 0)
                {
                    return weights;
                }
            }

            int colorCount = GetColorPickCount(cfgValue);
            List<int> fallback = Enumerable.Repeat(1, colorCount).ToList();
            for (int i = 0; i < Mathf.Max(0, targetCount - colorCount); i++)
            {
                fallback[i % fallback.Count]++;
            }

            return fallback;
        }

        private static List<int> ParseIntList(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return new List<int>();
            }

            return value.Split(',')
                .Select(item => item.Trim())
                .Where(item => !string.IsNullOrEmpty(item))
                .Select(item => int.TryParse(item, out int number) ? number : 0)
                .Where(number => number > 0)
                .ToList();
        }

        private static List<string> PickColorsByPriority(List<string> colors, int count)
        {
            List<string> result = new();
            foreach (IGrouping<int, string> group in colors.GroupBy(GetColorPriority).OrderBy(group => group.Key))
            {
                // 同 Priority 档内等概率随机，不再按 Priority_in 固定先后
                List<string> samePriorityColors = group
                    .OrderBy(_ => UnityEngine.Random.value)
                    .ToList();
                foreach (string color in samePriorityColors)
                {
                    result.Add(color);
                    if (result.Count >= count)
                    {
                        return result;
                    }
                }
            }

            return result;
        }

        private static List<ColorAllocation> BuildAllocations(List<string> selectedColors, List<int> weights, int targetCount)
        {
            List<int> sortedWeights = weights.OrderByDescending(weight => weight).ToList();
            // 同 Priority 档内等概率随机后再与降序权重配对；跨档仍按 Priority 升序
            List<string> sortedColors = selectedColors
                .GroupBy(GetColorPriority)
                .OrderBy(group => group.Key)
                .SelectMany(group => group.OrderBy(_ => UnityEngine.Random.value))
                .ToList();

            List<ColorAllocation> allocations = new();
            for (int i = 0; i < sortedColors.Count && i < sortedWeights.Count; i++)
            {
                allocations.Add(new ColorAllocation(sortedColors[i], sortedWeights[i]));
            }

            int allocatedCount = allocations.Sum(item => item.Count);
            int colorIndex = 0;
            while (allocatedCount < targetCount && allocations.Count > 0)
            {
                allocations[colorIndex % allocations.Count].Count++;
                allocatedCount++;
                colorIndex++;
            }

            return allocations;
        }

        private static List<TileSkinRuntimeData> ApplyFirstEnterMandatorySkins(
            bool isFirstEnter,
            int levelNumber,
            List<string> selectedColors,
            List<string> selectedTypes,
            List<ColorAllocation> allocations,
            HashSet<string> usedSkins,
            List<TileSkinRuntimeData> result)
        {
            List<TileSkinRuntimeData> mandatorySkins = new();
            if (!isFirstEnter)
            {
                return mandatorySkins;
            }

            HashSet<string> selectedColorSet = new(selectedColors);
            HashSet<string> selectedTypeSet = new(selectedTypes);
            List<TileSkinRuntimeData> candidateSkins = GetSkinsUnlockedAtLevel(levelNumber)
                .Where(skin => selectedColorSet.Contains(skin.Color) && selectedTypeSet.Contains(skin.Type))
                .ToList();
            if (candidateSkins.Count == 0)
            {
                return mandatorySkins;
            }

            int mandatoryCount = Mathf.Min(candidateSkins.Count, selectedColors.Count);
            List<TileSkinRuntimeData> pickedSkins = candidateSkins
                .OrderBy(_ => UnityEngine.Random.value)
                .Take(mandatoryCount)
                .ToList();

            foreach (TileSkinRuntimeData skin in pickedSkins)
            {
                ColorAllocation allocation = allocations.FirstOrDefault(item => item.Color == skin.Color && item.Count > 0);
                if (allocation == null)
                {
                    continue;
                }

                usedSkins.Add(skin.Name);
                result.Add(skin);
                mandatorySkins.Add(skin);
                allocation.Count--;
            }

            return mandatorySkins;
        }

        private static List<string> BuildLogColorOrder(List<TileSkinRuntimeData> result, List<string> selectedColors)
        {
            HashSet<string> resultColorSet = new(result.Select(skin => skin.Color));
            HashSet<string> loggedColorSet = new();
            List<string> colors = new();

            foreach (string color in selectedColors)
            {
                if (string.IsNullOrEmpty(color) || loggedColorSet.Contains(color) || !resultColorSet.Contains(color))
                {
                    continue;
                }

                colors.Add(color);
                loggedColorSet.Add(color);
            }

            List<string> fallbackColors = resultColorSet
                .Where(color => !loggedColorSet.Contains(color))
                .OrderBy(GetColorPriority)
                .ThenBy(GetColorPriorityIn)
                .ThenBy(color => GetDefaultColorOrder(color))
                .ToList();

            colors.AddRange(fallbackColors);
            return colors;
        }

        private static TileSkinRuntimeData PickSkin(
            List<TileSkinRuntimeData> availableSkins,
            HashSet<string> usedSkins,
            string color,
            string preferredType,
            List<string> selectedTypes,
            out string fallbackDetail)
        {
            fallbackDetail = "";
            TileSkinRuntimeData skin = PickFromCandidates(availableSkins, usedSkins, item => item.Color == color && item.Type == preferredType);
            if (skin != null)
            {
                return skin;
            }

            HashSet<string> selectedTypeSet = new(selectedTypes);
            skin = PickFromCandidates(availableSkins, usedSkins, item => item.Color == color && item.Type != preferredType);
            if (skin != null)
            {
                fallbackDetail = $"不足={color}_{preferredType}, fallback=目标颜色其他类型, skin={skin.Name}, actualColor={skin.Color}, actualType={skin.Type}";
                return skin;
            }

            skin = PickFromCandidates(availableSkins, usedSkins, item => item.Color != color && selectedTypeSet.Contains(item.Type));
            if (skin != null)
            {
                fallbackDetail = $"不足={color}_{preferredType}, fallback=其他颜色选中类型, skin={skin.Name}, actualColor={skin.Color}, actualType={skin.Type}";
                return skin;
            }

            skin = PickFromCandidates(availableSkins, usedSkins, item => item.Color != color);
            if (skin != null)
            {
                fallbackDetail = $"不足={color}_{preferredType}, fallback=其他颜色任意类型, skin={skin.Name}, actualColor={skin.Color}, actualType={skin.Type}";
            }

            return skin;
        }

        private static TileSkinRuntimeData PickAnySkin(List<TileSkinRuntimeData> availableSkins, HashSet<string> usedSkins)
        {
            return PickFromCandidates(availableSkins, usedSkins, _ => true)
                ?? GetRandomItem(availableSkins);
        }

        private static void RemoveReservedColorSkins(
            HashSet<string> usedSkins,
            List<TileSkinRuntimeData> result,
            string reservedColor,
            int reservedColorCount,
            List<string> goldtreeReplaceDetails)
        {
            if (string.IsNullOrEmpty(reservedColor) || reservedColorCount <= 0)
            {
                return;
            }

            List<string> removedSkinNames = new();
            for (int i = 0; i < reservedColorCount; i++)
            {
                int reservedColorIndex = result.FindIndex(skin => skin.Color == reservedColor);
                if (reservedColorIndex < 0)
                {
                    break;
                }

                TileSkinRuntimeData oldSkin = result[reservedColorIndex];
                usedSkins.Remove(oldSkin.Name);
                result.RemoveAt(reservedColorIndex);
                removedSkinNames.Add(oldSkin.Name);
            }

            if (removedSkinNames.Count > 0)
            {
                goldtreeReplaceDetails.Add(
                    $"removed={string.Join(",", removedSkinNames)}, " +
                    $"removedGroups={removedSkinNames.Count}, goldtreeTiles={reservedColorCount * 3}"
                );
            }

            if (removedSkinNames.Count < reservedColorCount)
            {
                int needFallbackCount = reservedColorCount - removedSkinNames.Count;
                List<string> fallbackRemovedSkinNames = new();
                for (int i = 0; i < needFallbackCount; i++)
                {
                    TileSkinRuntimeData fallbackSkin = GetRandomItem(result);
                    if (fallbackSkin == null)
                    {
                        break;
                    }

                    usedSkins.Remove(fallbackSkin.Name);
                    result.Remove(fallbackSkin);
                    fallbackRemovedSkinNames.Add(fallbackSkin.Name);
                }

                string fallbackRemoveText = fallbackRemovedSkinNames.Count > 0
                    ? string.Join(",", fallbackRemovedSkinNames)
                    : "none";
                goldtreeReplaceDetails.Add(
                    $"黄色不足: randomRemovedSkins={fallbackRemoveText}"
                );
            }
        }

        private static TileSkinRuntimeData PickFromCandidates(
            List<TileSkinRuntimeData> availableSkins,
            HashSet<string> usedSkins,
            Func<TileSkinRuntimeData, bool> predicate)
        {
            List<TileSkinRuntimeData> candidates = availableSkins
                .Where(item => !usedSkins.Contains(item.Name) && predicate(item))
                .ToList();
            return candidates.Count > 0 ? GetRandomItem(candidates) : null;
        }

        private static List<string> BuildTypeSequence(List<string> selectedTypes, int targetCount)
        {
            List<string> sequence = selectedTypes
                .OrderBy(_ => UnityEngine.Random.value)
                .Take(targetCount)
                .ToList();

            while (sequence.Count < targetCount)
            {
                sequence.Add(GetRandomItem(selectedTypes));
            }

            return sequence;
        }

        private static int GetColorPriority(string color)
        {
            if (color == null) return 100;
            EnsureColorPriority();
            return ColorPriority.TryGetValue(color, out int priority) ? priority : 100;
        }

        private static int GetColorPriorityIn(string color)
        {
            if (color == null) return int.MaxValue;
            EnsureColorPriority();
            return ColorPriorityIn.TryGetValue(color, out int priorityIn) ? priorityIn : int.MaxValue;
        }

        private static int GetDefaultColorOrder(string color)
        {
            if (color == null) return int.MaxValue;
            return DefaultColorOrder.TryGetValue(color, out int order) ? order : int.MaxValue;
        }

        private static T GetRandomItem<T>(IList<T> list)
        {
            return list == null || list.Count == 0 ? default(T) : list[UnityEngine.Random.Range(0, list.Count)];
        }

        private sealed class ColorAllocation
        {
            public string Color { get; }
            public int Count;

            public ColorAllocation(string color, int count)
            {
                Color = color;
                Count = count;
            }
        }
    }
}
