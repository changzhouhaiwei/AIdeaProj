using UnityEngine;
using FishFramework;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FishXSortPlay
{
    /// <summary>
    /// 关卡平台统一解析入口。编辑器、运行时、动态难度、打包都从这里派生"当前关卡平台"，
    /// 避免各处各自复制一份平台判断。平台来源是 <see cref="GameSettings.levelPlatform"/>。
    /// </summary>
    public static class LevelPlatformResolver
    {
        public const string LevelSystemFolder = "Assets/FishXSortSoEasy/Data/Level System";

        public const string AndroidLevelsFolder = LevelSystemFolder + "/LevelsAndroid";
        public const string IOSLevelsFolder = LevelSystemFolder + "/LevelsIOS";

        public const string AndroidDatabasePath = LevelSystemFolder + "/Level Database Android.asset";
        public const string IOSDatabasePath = LevelSystemFolder + "/Level Database IOS.asset";

        /// <summary>运行时 Catalog：仅 XSorts/effects，levels 为空，避免 GameScene 硬引用拉全库。</summary>
        public const string AndroidRuntimeDatabasePath = LevelSystemFolder + "/Level Database Android Runtime.asset";
        public const string IOSRuntimeDatabasePath = LevelSystemFolder + "/Level Database IOS Runtime.asset";

        /// <summary>读取当前配置的关卡平台。取不到配置时回退 Android，保证旧行为不被切到空的 iOS。</summary>
        public static LevelPlatform GetCurrentPlatform()
        {
            GameSettings settings = LoadSettings();
            return settings != null ? settings.levelPlatform : LevelPlatform.Android;
        }

        public static string GetRuntimeDatabasePath(LevelPlatform platform)
        {
            return platform == LevelPlatform.IOS ? IOSRuntimeDatabasePath : AndroidRuntimeDatabasePath;
        }

        public static string GetRuntimeDatabasePath()
        {
            return GetRuntimeDatabasePath(GetCurrentPlatform());
        }

        public static int GetConfiguredMaxLevelNum(LevelPlatform platform)
        {
            GameSettings settings = LoadSettings();
            if (settings == null)
            {
                return platform == LevelPlatform.IOS ? 100 : 1000;
            }

            return platform == LevelPlatform.IOS ? settings.iosMaxLevelNum : settings.androidMaxLevelNum;
        }

        public static int GetConfiguredMaxLevelNum()
        {
            return GetConfiguredMaxLevelNum(GetCurrentPlatform());
        }

        private static GameSettings LoadSettings()
        {
            GameSettings settings = null;

            try
            {
                if (GameModule.Setting != null)
                {
                    settings = GameModule.Setting.GetGameSettings();
                }
            }
            catch
            {
                settings = null;
            }

            if (settings == null)
            {
                settings = Resources.Load<GameSettings>(nameof(GameSettings));
            }

            return settings;
        }

        public static string GetDisplayName(LevelPlatform platform)
        {
            return platform == LevelPlatform.IOS ? "iOS" : "Android";
        }

        public static string GetDisplayName()
        {
            return GetDisplayName(GetCurrentPlatform());
        }

        public static string GetLevelsFolder(LevelPlatform platform)
        {
            return platform == LevelPlatform.IOS ? IOSLevelsFolder : AndroidLevelsFolder;
        }

        public static string GetLevelsFolder()
        {
            return GetLevelsFolder(GetCurrentPlatform());
        }

        public static string GetLevelAssetPrefix(LevelPlatform platform)
        {
            return platform == LevelPlatform.IOS ? "IOS_Level_" : "Android_Level_";
        }

        public static string GetLevelAssetPrefix()
        {
            return GetLevelAssetPrefix(GetCurrentPlatform());
        }

        public static string GetDatabasePath(LevelPlatform platform)
        {
            return platform == LevelPlatform.IOS ? IOSDatabasePath : AndroidDatabasePath;
        }

        public static string GetDatabasePath()
        {
            return GetDatabasePath(GetCurrentPlatform());
        }

#if UNITY_EDITOR
        public static LevelCatalog LoadDatabaseInEditor(LevelPlatform platform)
        {
            string path = GetDatabasePath(platform);
            return AssetDatabase.LoadAssetAtPath<LevelCatalog>(path);
        }

        public static LevelCatalog LoadDatabaseInEditor()
        {
            return LoadDatabaseInEditor(GetCurrentPlatform());
        }
#endif
    }
}
