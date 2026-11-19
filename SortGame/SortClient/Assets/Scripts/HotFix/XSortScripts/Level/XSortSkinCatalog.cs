using System;
using System.Collections.Generic;
using System.Linq;
using cfg.Game;
using FishFramework;
using SimpleJSON;
using XSortGameCore;
using UnityEngine;
using UnityEngine.U2D;

namespace FishXSortPlay
{
    public sealed class XSortSkinRuntimeData
    {
        public string Name { get; }
        public string Color { get; }
        public string Type { get; }
        public int Number { get; }
        public string AssetPath { get; }
        public string AtlasPath { get; }

        private static readonly Dictionary<string, string> ColorAtlasPath = new()
        {
            { "black", "Assets/CABRes/SpriteAtlas/XSortsNew/black/blackAtlas.spriteatlasv2" },
            { "blue", "Assets/CABRes/SpriteAtlas/XSortsNew/blue/BlueAtlas.spriteatlasv2" },
            { "brown", "Assets/CABRes/SpriteAtlas/XSortsNew/brown/BrownAtlas.spriteatlasv2" },
            { "gray", "Assets/CABRes/SpriteAtlas/XSortsNew/gray/GrayAtlas.spriteatlasv2" },
            { "green", "Assets/CABRes/SpriteAtlas/XSortsNew/green/GreenAtlas.spriteatlasv2" },
            { "orange", "Assets/CABRes/SpriteAtlas/XSortsNew/orange/OrangeAtals.spriteatlasv2" },
            { "pink", "Assets/CABRes/SpriteAtlas/XSortsNew/pink/PinkAtlas.spriteatlasv2" },
            { "purple", "Assets/CABRes/SpriteAtlas/XSortsNew/purple/PurpleAtlas.spriteatlasv2" },
            { "red", "Assets/CABRes/SpriteAtlas/XSortsNew/red/RedAtlas.spriteatlasv2" },
            { "white", "Assets/CABRes/SpriteAtlas/XSortsNew/white/WhiteAtlas.spriteatlasv2" },
            { "yellow", "Assets/CABRes/SpriteAtlas/XSortsNew/yellow/YellowAtlas.spriteatlasv2" },
        };

        private static readonly Dictionary<string, SpriteAtlas> AtlasCache = new();

        private Sprite sprite;
        private bool spriteLoaded;

        public XSortSkinRuntimeData(string name, string color, string type, int number)
        {
            Name = name;
            Color = color;
            Type = type;
            Number = number;
            AssetPath = $"Assets/CABRes/SpriteAtlas/XSortsNew/{color}/{name}.png";
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

    public static class XSortSkinCatalog
    {
        private const int DisabledOpenLevel = -1;
        private const string XSortEffectAtlasPath = "Assets/CABRes/SpriteAtlas/XSortEffect/XSortEffectAtlas.spriteatlasv2";

        private const string XSortEffectFolderPath = "Assets/CABRes/SpriteAtlas/XSortEffect/";
        private const string LegacyBackgroundName = "XSort_qizi";

        private const string EffectPrefabFolderPath = "Assets/Arts/NewEff/Prefabs/";
        private const string BrokenEffectFolderPath = "Assets/Arts/NewEff/Prefabs/BrokenEff/";
        private const string EffectMaterialFolderPath = "Assets/Arts/NewEff/Material/";
        private const string XSortBaseSoundFolderPath = "Assets/FishXSortExplorer/Game/Audio/Sounds/";
        private const string XSortBaseSoundFolderPathSoEasy = "Assets/FishXSortSoEasy/Game/Audio/Sounds/";
        private const string DefaultBrokenSoundName = "white_broken_sound";
        private const string DefaultCollectSoundName = "white_collect_sound";
        public const string DefaultEffectPrefabName = "white_broken_effect";
        public const string CoinRewardEffectPrefabName = "Slot_Highlight_Coin";
        public const string CoinRewardBrokenSoundName = "coin_broken";
        private static readonly string[] EffectMaterialNodeNames = { "XSort_Par01", "XSort_Par_L", "XSort_Par_R" };
        private static readonly string[] XSortBaseSoundExtensions = { ".wav", ".mp3", ".WAV" };
        private static readonly Dictionary<string, string> XSortBaseSoundAliases = new()
        {
            { "Wodd broken", "wood_break" },
            { "Wodd collect", "wood_collect" },
        };

        private static SpriteAtlas XSortEffectAtlas;
        private static readonly Dictionary<int, Sprite> XSortBackgroundSpriteCache = new();
        private static readonly Dictionary<int, Sprite> UnknownXSortBaseSpriteCache = new();
        private static readonly Dictionary<int, Sprite> StyleSpriteCache = new();
        private static readonly Dictionary<int, Sprite> StyleUnknownSpriteCache = new();
        private static readonly Dictionary<string, GameObject> EffectPrefabCache = new();
        private static readonly Dictionary<string, Material> EffectMaterialCache = new();
        private static readonly Dictionary<string, Dictionary<string, Material>> EffectPrefabOriginalMaterials = new();
        private static readonly Dictionary<string, AudioClip> XSortBaseSoundCache = new();
        private static Dictionary<string, AudioClip> XSortBaseSoundRegistry;
        private static bool XSortBaseSoundRegistryBuilt;

        /// <summary>
        /// 丢掉主题图、皮肤、特效和音效的缓存引用。画面上还挂着的资源不会被这里拆掉。
        /// </summary>
        public static void ReleaseCachedAssets()
        {
            XSortSkinRuntimeData.ReleaseCachedAssets();
            if (allSkins != null)
            {
                for (int i = 0; i < allSkins.Count; i++)
                {
                    allSkins[i].ReleaseSpriteCache();
                }
            }

            XSortEffectAtlas = null;
            XSortBackgroundSpriteCache.Clear();
            UnknownXSortBaseSpriteCache.Clear();
            StyleSpriteCache.Clear();
            StyleUnknownSpriteCache.Clear();
            EffectPrefabCache.Clear();
            EffectMaterialCache.Clear();
            EffectPrefabOriginalMaterials.Clear();
            XSortBaseSoundCache.Clear();
            XSortBaseSoundRegistry = new Dictionary<string, AudioClip>();
            XSortBaseSoundRegistryBuilt = false;
        }

        /// <summary>
        /// 按 XSortbaseId 加载棋子普通底板（配置 XSortBase_UI 字段）。
        /// </summary>
        public static Sprite LoadXSortBackgroundSprite(int XSortbaseId)
        {
            if (TryLoadStyleSprite(unknown: false, XSortbaseId, out Sprite styleSprite))
            {
                return styleSprite;
            }

            return LoadThemeXSortBackgroundSprite(XSortbaseId);
        }

        public static Sprite LoadUnknownXSortBaseSprite(int XSortbaseId)
        {
            if (TryLoadStyleSprite(unknown: true, XSortbaseId, out Sprite styleSprite))
            {
                return styleSprite;
            }

            return LoadThemeUnknownSprite(XSortbaseId);
        }

        static bool TryLoadStyleSprite(bool unknown, int themeFallbackId, out Sprite sprite)
        {
            sprite = null;
            int styleId = LevelDirector.LoadedXSortBaseStyle;
            if (styleId < 0)
            {
                return false;
            }

            Dictionary<int, Sprite> cache = unknown ? StyleUnknownSpriteCache : StyleSpriteCache;
            if (cache.TryGetValue(styleId, out sprite))
            {
                return true;
            }

            XSortBaseStyleCatalog.Entry row = XSortBaseStyleTable.Get(styleId);
            UnityEngine.Sprite direct = row == null ? null : (unknown ? row.unknown : row.XSortBase);
            if (direct != null)
            {
                cache[styleId] = direct;
                sprite = direct;
                return true;
            }

            sprite = unknown
                ? LoadThemeUnknownSprite(themeFallbackId)
                : LoadThemeXSortBackgroundSprite(themeFallbackId);

            cache[styleId] = sprite;
            return true;
        }

        static Sprite LoadThemeXSortBackgroundSprite(int XSortbaseId)
        {
            if (XSortBackgroundSpriteCache.TryGetValue(XSortbaseId, out Sprite cached))
            {
                return cached;
            }

            XSortBaseThemePack pack = ThemeUiApplier.Get(XSortbaseId);
            Sprite backgroundSprite = pack != null ? pack.XSortBase : null;
            if (backgroundSprite == null)
            {
                backgroundSprite = LoadXSortEffectSprite(LegacyBackgroundName);
            }

            XSortBackgroundSpriteCache[XSortbaseId] = backgroundSprite;
            return backgroundSprite;
        }

        static Sprite LoadThemeUnknownSprite(int XSortbaseId)
        {
            if (UnknownXSortBaseSpriteCache.TryGetValue(XSortbaseId, out Sprite cached))
            {
                return cached;
            }

            XSortBaseThemePack pack = ThemeUiApplier.Get(XSortbaseId);
            Sprite unknownSprite = pack != null ? pack.unknown : null;
            UnknownXSortBaseSpriteCache[XSortbaseId] = unknownSprite;
            return unknownSprite;
        }

        private static GameXSortBaseControlCfg GetXSortBaseCfg(int XSortbaseId)
        {
            return ConfigHelper.Tables?.TbGameXSortBaseControlCfg?.GetOrDefault(XSortbaseId);
        }

        private const string IceThemeName = "ice";
        private const string IceExcludedColor = "blue";

        static bool IsIceTheme()
        {
            GameXSortBaseControlCfg cfg = GetXSortBaseCfg(LevelDirector.LoadedLevelTheme);
            return cfg != null && string.Equals(cfg.Theme, IceThemeName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Ice 主题尽量不出 blue；去掉后池子为空则不滤，避免关卡无法生成。
        /// </summary>
        static List<XSortSkinRuntimeData> ExcludeBlueSkinsForIceTheme(List<XSortSkinRuntimeData> availableSkins)
        {
            if (availableSkins == null || availableSkins.Count == 0 || !IsIceTheme())
            {
                return availableSkins;
            }

            List<XSortSkinRuntimeData> withoutBlue = availableSkins
                .Where(skin => skin != null && skin.Color != IceExcludedColor)
                .ToList();
            return withoutBlue.Count > 0 ? withoutBlue : availableSkins;
        }

        static void AssertNoBlueWhenIceHasOtherColors(
            List<XSortSkinRuntimeData> availableSkins,
            List<XSortSkinRuntimeData> result)
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
        /// 按 XSortbaseId 取三消破碎音效名（配置 XSort_baroken_sound 字段），为空时回退默认。
        /// 金币棋固定 coin_broken，不走底板表。
        /// </summary>
        public static string GetBrokenSoundName(int XSortbaseId, bool isCoinReward = false)
        {
            if (isCoinReward)
            {
                return CoinRewardBrokenSoundName;
            }

            XSortBaseThemePack pack = ThemeUiApplier.Get(XSortbaseId);
            return pack != null && pack.brokenSound != null ? pack.brokenSound.name : DefaultBrokenSoundName;
        }

        /// <summary>
        /// 按 XSortbaseId 取点击收集音效名（配置 Effect_collect_sound 字段），为空时回退默认。
        /// </summary>
        public static string GetCollectSoundName(int XSortbaseId)
        {
            XSortBaseThemePack pack = ThemeUiApplier.Get(XSortbaseId);
            return pack != null && pack.collectSound != null ? pack.collectSound.name : DefaultCollectSoundName;
        }

        /// <summary>
        /// 按配置音效名加载 AudioClip（带缓存），加载失败时回退 fallbackClip。
        /// </summary>
        public static AudioClip LoadXSortBaseSound(string soundName, AudioClip fallbackClip = null)
        {
            if (string.IsNullOrEmpty(soundName))
            {
                return fallbackClip;
            }

            if (XSortBaseSoundCache.TryGetValue(soundName, out AudioClip cached) && cached != null)
            {
                return cached;
            }

            AudioClip clip = TryGetRegisteredXSortBaseSound(soundName);
            if (clip == null
                && XSortBaseSoundAliases.TryGetValue(soundName, out string alias)
                && !string.IsNullOrEmpty(alias))
            {
                clip = TryGetRegisteredXSortBaseSound(alias);
            }

            if (clip == null)
            {
                string resolvedName = XSortBaseSoundAliases.TryGetValue(soundName, out string loadAlias)
                    ? loadAlias
                    : soundName;

                clip = LoadXSortBaseSoundFromFolders(resolvedName);

                if (clip == null && resolvedName != soundName)
                {
                    clip = LoadXSortBaseSoundFromFolders(soundName);
                }
            }

            if (clip == null)
            {
                Debug.LogWarning($"[XSortSkinCatalog] Failed to load XSort base sound '{soundName}', fallback will be used.");
            }

            XSortBaseSoundCache[soundName] = clip;
            return clip ?? fallbackClip;
        }

        static AudioClip LoadXSortBaseSoundFromFolders(string soundName)
        {
            string[] folders = { XSortBaseSoundFolderPath, XSortBaseSoundFolderPathSoEasy };
            for (int i = 0; i < folders.Length; i++)
            {
                for (int j = 0; j < XSortBaseSoundExtensions.Length; j++)
                {
                    AudioClip clip = UILoadHelper.LoadAsset<AudioClip>($"{folders[i]}{soundName}{XSortBaseSoundExtensions[j]}");
                    if (clip != null)
                    {
                        return clip;
                    }
                }
            }

            return null;
        }

        private static AudioClip TryGetRegisteredXSortBaseSound(string soundName)
        {
            EnsureXSortBaseSoundRegistry();
            return XSortBaseSoundRegistry.TryGetValue(soundName, out AudioClip clip) && clip != null ? clip : null;
        }

        private static void EnsureXSortBaseSoundRegistry()
        {
            if (XSortBaseSoundRegistryBuilt)
            {
                return;
            }

            AudioClips clips = AudioController.AudioClips;
            if (clips == null)
            {
                return;
            }

            List<AudioClip> list = clips.XSortBaseSounds;
            XSortBaseSoundRegistryBuilt = true;
            XSortBaseSoundRegistry = new Dictionary<string, AudioClip>();
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

                RegisterXSortBaseSound(clip.name, clip);
            }

            foreach (KeyValuePair<string, string> pair in XSortBaseSoundAliases)
            {
                if (XSortBaseSoundRegistry.TryGetValue(pair.Value, out AudioClip aliasClip) && aliasClip != null)
                {
                    RegisterXSortBaseSound(pair.Key, aliasClip);
                }
            }
        }

        private static void RegisterXSortBaseSound(string soundName, AudioClip clip)
        {
            if (string.IsNullOrEmpty(soundName) || clip == null)
            {
                return;
            }

            XSortBaseSoundRegistry[soundName] = clip;
        }

        /// <summary>
        /// 按 XSortbaseId 取消除特效预制体名（配置 XSort_baroken_effect 字段），为空时回退默认。
        /// </summary>
        public static string GetEffectPrefabName(int XSortbaseId)
        {
            GameObject styleEffect = XSortBaseStyleTable.Current?.brokenEffect;
            if (styleEffect != null)
            {
                return styleEffect.name;
            }

            XSortBaseThemePack pack = ThemeUiApplier.Get(XSortbaseId);
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
                GameObject styleEffect = XSortBaseStyleTable.Current?.brokenEffect;
                if (styleEffect != null && styleEffect.name == prefabName)
                {
                    prefab = styleEffect;
                }
            }
            if (prefab == null)
            {
                XSortBaseThemePack pack = ThemeUiApplier.Get(LevelDirector.LoadedLevelTheme);
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
        public static Material LoadXSortBaseMaterial(int XSortbaseId)
        {
            XSortBaseThemePack pack = ThemeUiApplier.Get(XSortbaseId);
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
        /// 将 XSortbaseId 对应的材质应用到特效实例的 XSort_Par01/XSort_Par_L/XSort_Par_R 节点。
        /// 配置材质为空/缺失时，恢复预制体原始材质（避免池化复用时残留上一次的材质）。
        /// </summary>
        public static void ApplyXSortBaseMaterial(GameObject effectRoot, string effectName, int XSortbaseId)
        {
            if (effectRoot == null)
            {
                return;
            }

            Material configMaterial = LoadXSortBaseMaterial(XSortbaseId);
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

        private static Sprite LoadXSortEffectSprite(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName))
            {
                return null;
            }

            SpriteAtlas atlas = LoadXSortEffectAtlas();
            Sprite sprite = atlas != null ? atlas.GetSprite(spriteName) : null;
            if (sprite == null)
            {
                sprite = UILoadHelper.LoadAsset<Sprite>($"{XSortEffectFolderPath}{spriteName}.png");
            }

            return sprite;
        }

        public static Sprite LoadCoinRewardSprite()
        {
            return LoadXSortEffectSprite(LevelDirector.CoinRewardSkinName);
        }

        private static SpriteAtlas LoadXSortEffectAtlas()
        {
            if (XSortEffectAtlas == null)
            {
                XSortEffectAtlas = UILoadHelper.LoadAsset<SpriteAtlas>(XSortEffectAtlasPath);
            }

            return XSortEffectAtlas;
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

        private static readonly Dictionary<string, XSortSkinRuntimeData> SkinByName = new();
        private static readonly Dictionary<string, int> ColorPriority = new();
        private static readonly Dictionary<string, int> ColorPriorityIn = new();
        private static readonly Dictionary<string, int> DefaultColorOrder = new();
        private static List<XSortSkinRuntimeData> allSkins;
        private static bool priorityLoaded;

        public static IReadOnlyList<XSortSkinRuntimeData> GetAvailableSkins(int levelNumber)
        {
            EnsureSkins();

            var openLevelCfgs = ConfigHelper.Tables?.TbGameXSortOpenLevelCfg?.DataList;
            if (openLevelCfgs == null)
            {
                return Array.Empty<XSortSkinRuntimeData>();
            }

            List<XSortSkinRuntimeData> result = new();
            foreach (GameXSortOpenLevelCfg cfg in openLevelCfgs)
            {
                if (cfg.Level == DisabledOpenLevel)
                {
                    continue;
                }

                if (cfg.Level > levelNumber)
                {
                    continue;
                }

                if (SkinByName.TryGetValue(cfg.Name, out XSortSkinRuntimeData skin))
                {
                    result.Add(skin);
                }
            }

            return result;
        }

        public static IReadOnlyList<XSortSkinRuntimeData> GetSkinsUnlockedAtLevel(int levelNumber)
        {
            EnsureSkins();

            var openLevelCfgs = ConfigHelper.Tables?.TbGameXSortOpenLevelCfg?.DataList;
            if (openLevelCfgs == null)
            {
                return Array.Empty<XSortSkinRuntimeData>();
            }

            List<XSortSkinRuntimeData> result = new();
            foreach (GameXSortOpenLevelCfg cfg in openLevelCfgs)
            {
                if (cfg.Level == DisabledOpenLevel || cfg.Level != levelNumber)
                {
                    continue;
                }

                if (SkinByName.TryGetValue(cfg.Name, out XSortSkinRuntimeData skin))
                {
                    result.Add(skin);
                }
            }

            return result;
        }

        public static XSortSkinRuntimeData GetSkin(string skinName)
        {
            if (string.IsNullOrEmpty(skinName))
            {
                return null;
            }

            EnsureSkins();
            return SkinByName.TryGetValue(skinName, out XSortSkinRuntimeData skin) ? skin : null;
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
        public static List<XSortSkinRuntimeData> BuildCoinLevelSkins(int otherGroupCount, int levelNumber, int coinXSortCount = -1)
        {
            EnsureSkins();

            List<XSortSkinRuntimeData> availableSkins = ExcludeBlueSkinsForIceTheme(
                GetAvailableSkins(levelNumber)
                    .Where(skin => skin != null && !CoinRewardExcludedColors.Contains(skin.Color))
                    .ToList()
            );
            if (availableSkins.Count == 0 || otherGroupCount <= 0)
            {
                Debug.Log(
                    $"[XSortSkin] coinLevel={levelNumber}, coinGroups={coinXSortCount / 3}, " +
                    $"otherGroups={otherGroupCount}, generated=0, colors=none"
                );
                return new List<XSortSkinRuntimeData>();
            }

            List<string> colors = availableSkins
                .Select(skin => skin.Color)
                .Distinct()
                .OrderBy(_ => UnityEngine.Random.value)
                .ToList();

            HashSet<string> usedSkins = new();
            HashSet<string> usedColors = new();
            List<XSortSkinRuntimeData> result = new();
            for (int i = 0; i < otherGroupCount; i++)
            {
                XSortSkinRuntimeData skin = PickCoinLevelSkin(availableSkins, usedSkins, colors, usedColors);
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
                $"[XSortSkin] coinLevel={levelNumber}, coinGroups={coinXSortCount / 3}, " +
                $"otherGroups={otherGroupCount}, generated={result.Count}, colors={colorsText}"
            );

            return result;
        }

        static XSortSkinRuntimeData PickCoinLevelSkin(
            List<XSortSkinRuntimeData> availableSkins,
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

                XSortSkinRuntimeData unusedColorSkin = PickFromCandidates(
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
            SplitColorChallengeSkins(color, levelNumber, out List<XSortSkinRuntimeData> inRange, out List<XSortSkinRuntimeData> outRange, out _);
            return CountNames(inRange, outRange);
        }

        /// <summary>
        /// 颜色挑战关：只从 colorchallenge 指定颜色里随机取 targetCount 个花色。
        /// 先取已释出且 NotAllowed!=1 的棋子。已释出花色不够时，再用更晚关卡或不再释出的同色花色补。
        /// 同色花色总数仍不够时返回空表。
        /// </summary>
        public static List<XSortSkinRuntimeData> BuildColorChallengeSkins(
            string color,
            int levelNumber,
            int targetCount)
        {
            List<XSortSkinRuntimeData> result = new();
            SplitColorChallengeSkins(color, levelNumber, out List<XSortSkinRuntimeData> inRange, out List<XSortSkinRuntimeData> outRange, out HashSet<string> blockedNames);
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
                $"[XSortSkin] colorChallenge level={levelNumber}, color={want}, " +
                $"groupCount={targetCount}, generated={result.Count}, " +
                $"inRange={inRange.Count}, outRange={outRange.Count}, blocked={blockedNames.Count}, " +
                $"skins={namesText}"
            );

            return result;
        }

        static int CountNames(List<XSortSkinRuntimeData> first, List<XSortSkinRuntimeData> second)
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
            List<XSortSkinRuntimeData> source,
            HashSet<string> usedNames,
            List<XSortSkinRuntimeData> result,
            int targetCount)
        {
            List<XSortSkinRuntimeData> picked = source
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
            out List<XSortSkinRuntimeData> inRange,
            out List<XSortSkinRuntimeData> outRange,
            out HashSet<string> blockedNames)
        {
            EnsureSkins();
            inRange = new List<XSortSkinRuntimeData>();
            outRange = new List<XSortSkinRuntimeData>();
            blockedNames = new HashSet<string>();
            string want = string.IsNullOrEmpty(color) ? "" : color.Trim();
            if (want.Length == 0)
            {
                return;
            }

            var openLevelCfgs = ConfigHelper.Tables?.TbGameXSortOpenLevelCfg?.DataList;
            if (openLevelCfgs == null)
            {
                return;
            }

            foreach (GameXSortOpenLevelCfg cfg in openLevelCfgs)
            {
                if (!SkinByName.TryGetValue(cfg.Name, out XSortSkinRuntimeData skin))
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

        public static List<XSortSkinRuntimeData> BuildLevelSkins(
            int cfgValue,
            int levelNumber,
            int targetCount,
            int totalXSortCount = -1,
            bool isFirstEnter = false)
        {
            EnsureSkins();
            EnsureColorPriority();

            List<XSortSkinRuntimeData> availableSkins = ExcludeBlueSkinsForIceTheme(GetAvailableSkins(levelNumber).ToList());
            if (availableSkins.Count == 0 || targetCount <= 0)
            {
                Debug.Log($"[XSortSkin] level={levelNumber}, cfg={cfgValue}, no available XSortsNew skin. available={availableSkins.Count}, groupCount={targetCount}, XSortTotal={totalXSortCount}");
                return new List<XSortSkinRuntimeData>();
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
                Debug.Log($"[XSortSkin] level={levelNumber}, cfg={cfgValue}, no selected XSort type. availableTypes={string.Join(",", allTypes)}, groupCount={targetCount}, XSortTotal={totalXSortCount}");
                return new List<XSortSkinRuntimeData>();
            }

            List<int> colorWeights = GetColorWeights(cfgValue, targetCount);
            int colorCount = Mathf.Clamp(GetColorPickCount(cfgValue), 1, Mathf.Min(allColors.Count, colorWeights.Count));
            List<string> selectedColors = PickColorsByPriority(allColors, colorCount);
            List<ColorAllocation> allocations = BuildAllocations(selectedColors, colorWeights, targetCount);
            List<string> typeSequence = BuildTypeSequence(selectedTypes, targetCount);

            HashSet<string> usedSkins = new();
            List<XSortSkinRuntimeData> result = new();
            List<string> fallbackDetails = new();
            List<XSortSkinRuntimeData> mandatorySkins = ApplyFirstEnterMandatorySkins(
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
                    XSortSkinRuntimeData skin = PickSkin(
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
                XSortSkinRuntimeData skin = PickAnySkin(availableSkins, usedSkins);
                if (skin == null)
                {
                    break;
                }

                fallbackDetails.Add($"分配数量不足: groupIndex={result.Count}, skin={skin.Name}, actualColor={skin.Color}, actualType={skin.Type}");
                usedSkins.Add(skin.Name);
                result.Add(skin);
            }

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
            string firstEnterMandatoryText = mandatorySkins.Count > 0
                ? string.Join(",", mandatorySkins.Select(skin => skin.Name))
                : "none";
            List<XSortSkinRuntimeData> newUnlockedSkins = GetSkinsUnlockedAtLevel(levelNumber).ToList();
            string newUnlockedSkinsText = newUnlockedSkins.Count > 0
                ? string.Join("\n", newUnlockedSkins.Select(skin => $"  {skin.Name}"))
                : "none";

            // [XSortSkin] 日志字段说明：
            // level            当前关卡号（1-based）
            // cfg              动态配置第一段花色数（连胜/连败会变化）
            // winCnt / failCnt 当前连胜、连败次数
            // groupCount       本局计划生成的棋子组数（1组=3颗）
            // generatedGroups  实际生成的棋子组数
            // XSortTotal        关卡棋盘总棋子数
            // colors           本局最终棋盘出现的颜色，按 colorAllocate 主色槽位顺序
            // types            本局最终棋盘出现的棋子类型
            // missingTypes     计划选中但未出现在结果中的类型
            // fallback         兜底选棋记录（资源不足时跨颜色/类型替换），none 表示无
            // firstEnterMandatory 首次进关时强制保底进局的新解锁棋子名（skin.Name）
            // newUnlockedSkins 配置表定义的本关新开放棋子列表（CSVGameXSortOpenLevelCfg.level==当前关）
            // XSortbaseId       本局随机/配置确定的棋子底板 id（LoadedLevelTheme）
            Debug.Log(
                $"[XSortSkin] level={levelNumber}, cfg={cfgValue}, winCnt={winCnt}, failCnt={failCnt}, " +
                $"groupCount={targetCount}, generatedGroups={result.Count}, XSortTotal={totalXSortCount}, " +
                $"colors={string.Join(",", resultColors)}, types={string.Join(",", resultTypes)}, " +
                $"missingTypes={string.Join(",", missingTypes)}, fallback={fallbackText}, " +
                $"firstEnterMandatory={firstEnterMandatoryText}\n " +
                $"XSortbaseId={LevelDirector.LoadedLevelTheme}," +
                $"newUnlockedSkins={newUnlockedSkinsText}"
            );

            AssertNoBlueWhenIceHasOtherColors(availableSkins, result);
            LogMissingTextureResources(levelNumber, result);

            return result;
        }

        /// <summary>
        /// 汇总日志之后：校验本局已选皮肤是否能加载到纹理，缺失则单独打 Error。
        /// </summary>
        private static void LogMissingTextureResources(int levelNumber, List<XSortSkinRuntimeData> skins)
        {
            if (skins == null || skins.Count == 0)
            {
                return;
            }

            List<string> missingDetails = new();
            foreach (XSortSkinRuntimeData skin in skins)
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
                $"[XSortSkin] 缺少纹理资源 level={levelNumber}, count={missingDetails.Count}, " +
                $"details={string.Join(" | ", missingDetails)}"
            );
        }

        private static void EnsureSkins()
        {
            // Allow rebuild when first call happened before ConfigHelper.EnsureInitialized (Test Level).
            if (allSkins != null && (allSkins.Count > 0 || ConfigHelper.Tables?.TbGameXSortOpenLevelCfg == null))
            {
                return;
            }

            allSkins = new List<XSortSkinRuntimeData>();
            SkinByName.Clear();
            DefaultColorOrder.Clear();

            var openLevelCfgs = ConfigHelper.Tables?.TbGameXSortOpenLevelCfg?.DataList;
            if (openLevelCfgs == null)
            {
                return;
            }

            foreach (GameXSortOpenLevelCfg cfg in openLevelCfgs)
            {
                if (!TryParseSkinName(cfg.Name, out string color, out string type, out int number))
                {
                    continue;
                }

                if (SkinByName.ContainsKey(cfg.Name))
                {
                    continue;
                }

                XSortSkinRuntimeData skin = new(cfg.Name, color, type, number);
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

        private static List<XSortSkinRuntimeData> ApplyFirstEnterMandatorySkins(
            bool isFirstEnter,
            int levelNumber,
            List<string> selectedColors,
            List<string> selectedTypes,
            List<ColorAllocation> allocations,
            HashSet<string> usedSkins,
            List<XSortSkinRuntimeData> result)
        {
            List<XSortSkinRuntimeData> mandatorySkins = new();
            if (!isFirstEnter)
            {
                return mandatorySkins;
            }

            HashSet<string> selectedColorSet = new(selectedColors);
            HashSet<string> selectedTypeSet = new(selectedTypes);
            List<XSortSkinRuntimeData> candidateSkins = GetSkinsUnlockedAtLevel(levelNumber)
                .Where(skin => selectedColorSet.Contains(skin.Color) && selectedTypeSet.Contains(skin.Type))
                .ToList();
            if (candidateSkins.Count == 0)
            {
                return mandatorySkins;
            }

            int mandatoryCount = Mathf.Min(candidateSkins.Count, selectedColors.Count);
            List<XSortSkinRuntimeData> pickedSkins = candidateSkins
                .OrderBy(_ => UnityEngine.Random.value)
                .Take(mandatoryCount)
                .ToList();

            foreach (XSortSkinRuntimeData skin in pickedSkins)
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

        private static List<string> BuildLogColorOrder(List<XSortSkinRuntimeData> result, List<string> selectedColors)
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

        private static XSortSkinRuntimeData PickSkin(
            List<XSortSkinRuntimeData> availableSkins,
            HashSet<string> usedSkins,
            string color,
            string preferredType,
            List<string> selectedTypes,
            out string fallbackDetail)
        {
            fallbackDetail = "";
            XSortSkinRuntimeData skin = PickFromCandidates(availableSkins, usedSkins, item => item.Color == color && item.Type == preferredType);
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

        private static XSortSkinRuntimeData PickAnySkin(List<XSortSkinRuntimeData> availableSkins, HashSet<string> usedSkins)
        {
            return PickFromCandidates(availableSkins, usedSkins, _ => true)
                ?? GetRandomItem(availableSkins);
        }

        private static XSortSkinRuntimeData PickFromCandidates(
            List<XSortSkinRuntimeData> availableSkins,
            HashSet<string> usedSkins,
            Func<XSortSkinRuntimeData, bool> predicate)
        {
            List<XSortSkinRuntimeData> candidates = availableSkins
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
