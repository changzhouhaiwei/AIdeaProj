using System;
using System.Collections;
using UnityEngine;
using xasset;

namespace FishFramework
{
    [DisallowMultipleComponent]
    public class SettingModule : MonoBehaviour
    {
        public const string RES_VERSION = "RES_VERSION";
        const string UserLanguagePrefsKey = "UserSelectedLanguage";

        private GameSettings settings;

        private float gameSpeedBeforePause = 1f;
        private bool bgmPausedBeforeGamePause = false;
        private bool sfxPausedBeforeGamePause = false;

        /// <summary>
        /// 获取或设置游戏帧率。
        /// </summary>
        public int FrameRate
        {
            get => frameRate;
            set => Application.targetFrameRate = frameRate = value;
        }

        private int frameRate = 60;

        /// <summary>
        /// 获取或设置游戏速度。
        /// </summary>
        public float GameSpeed
        {
            get => gameSpeed;
            set => Time.timeScale = gameSpeed = value >= 0f ? value : 0f;
        }

        private float gameSpeed = 1f;

        /// <summary>
        /// 获取游戏是否暂停。
        /// </summary>
        public bool IsGamePaused => gameSpeed <= 0f;

        /// <summary>
        /// 获取是否正常游戏速度。
        /// </summary>
        public bool IsNormalGameSpeed => Math.Abs(gameSpeed - 1f) < 0.01f;

        /// <summary>
        /// 获取或设置是否允许后台运行。
        /// </summary>
        public bool RunInBackground
        {
            get => runInBackground;
            set => Application.runInBackground = runInBackground = value;
        }

        private bool runInBackground = true;

        /// <summary>
        /// 获取或设置是否禁止休眠。
        /// </summary>
        public bool NeverSleep
        {
            get => neverSleep;
            set
            {
                neverSleep = value;
                Screen.sleepTimeout = value ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
            }
        }

        private bool neverSleep = true;

        public bool GmMode { get; private set; }
        public bool GmLevelPassGlobalSwitch { get; private set; }

        /// <summary>
        /// 暂停游戏。
        /// </summary>
        public void PauseGame()
        {
            if (IsGamePaused)
            {
                return;
            }

            bgmPausedBeforeGamePause = GameModule.Audio != null && GameModule.Audio.BGM != null && GameModule.Audio.BGM.IsPaused;
            sfxPausedBeforeGamePause = GameModule.Audio != null && GameModule.Audio.SFX != null && GameModule.Audio.SFX.IsPaused;
            PauseAudio();
            gameSpeedBeforePause = GameSpeed;
            GameSpeed = 0f;
        }

        /// <summary>
        /// 恢复游戏。
        /// </summary>
        public void ResumeGame()
        {
            if (!IsGamePaused)
            {
                return;
            }

            GameSpeed = gameSpeedBeforePause;
            ResumeAudio();
        }

        private void PauseAudio()
        {
            if (GameModule.Audio == null)
            {
                return;
            }

            if (GameModule.Audio.BGM != null && !bgmPausedBeforeGamePause)
            {
                GameModule.Audio.BGM.IsPaused = true;
            }

            if (GameModule.Audio.SFX != null && !sfxPausedBeforeGamePause)
            {
                GameModule.Audio.SFX.IsPaused = true;
            }
        }

        private void ResumeAudio()
        {
            if (GameModule.Audio == null)
            {
                return;
            }

            if (GameModule.Audio.BGM != null && !bgmPausedBeforeGamePause)
            {
                GameModule.Audio.BGM.IsPaused = false;
            }

            if (GameModule.Audio.SFX != null && !sfxPausedBeforeGamePause)
            {
                GameModule.Audio.SFX.IsPaused = false;
            }
        }

        /// <summary>
        /// 重置为正常游戏速度。
        /// </summary>
        public void ResetNormalGameSpeed()
        {
            if (IsNormalGameSpeed)
            {
                return;
            }

            GameSpeed = 1f;
        }

        // 游戏设置初始化
        public IEnumerator Initialize()
        {
            settings = Resources.Load<GameSettings>(nameof(GameSettings));
            //资源下载地址

            Input.multiTouchEnabled = false;
            RunInBackground = true;
            NeverSleep = false;
            Debug.unityLogger.logEnabled = settings.logMode;
            SetFrameRate();

            GmMode = settings.gmMode;
            GmLevelPassGlobalSwitch = GmMode;
            ApplySavedLanguage();
            yield return StartCoroutine(InitVersion());
        }

        public void EnableGmLevelPassGlobalSwitch()
        {
            GmLevelPassGlobalSwitch = true;
        }


        public void SetFrameRate()
        {
            var systemMemory = SystemInfo.systemMemorySize;
            int dstFps = 60;
            bool setFrameRateAutomatically = false;

#if UNITY_ANDROID
            if (systemMemory < 3000)
            {
                setFrameRateAutomatically = false;
                dstFps = 30;
            }
            else if (systemMemory <= 4096)
            {
                setFrameRateAutomatically = false;
                dstFps = 60;
            }
            else
            {
                setFrameRateAutomatically = true;
                dstFps = 60;
            }
#endif

            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = -1;

            if (setFrameRateAutomatically)
            {
                uint numerator = Screen.currentResolution.refreshRateRatio.numerator;
                uint denominator = Screen.currentResolution.refreshRateRatio.denominator;

                if (numerator != 0 && denominator != 0 && numerator / denominator > 59f)
                {
                    Application.targetFrameRate = Mathf.RoundToInt(numerator / denominator);
                }
                else
                {
                    Application.targetFrameRate = dstFps;
                }
            }
            else
            {
                Application.targetFrameRate = dstFps;
            }
        }


        private IEnumerator InitVersion()
        {
            if (settings.appVersion > PlayerPrefs.GetInt("APP_VERSION"))
            {
                PlayerPrefs.SetInt("APP_VERSION", settings.appVersion);
                var clearAsync = Versions.ClearAsync();
                yield return clearAsync;
            }

            //资源版本号初始化
            var builtVersion = new System.Version(settings.major, settings.minor, settings.build);
            var localVerStr = PlayerPrefs.GetString(RES_VERSION);
            if (string.IsNullOrEmpty(localVerStr))
            {
                PlayerPrefs.SetString(RES_VERSION, builtVersion.ToString());
            }
            else
            {
                var localVersion = new System.Version(localVerStr);
                if (builtVersion > localVersion)
                {
                    PlayerPrefs.SetString(RES_VERSION, builtVersion.ToString());
                }
            }
        }

        public GameSettings GetGameSettings()
        {
            return settings;
        }

        public string GetServerLoginURL()
        {
            switch (settings.urlType)
            {
                case ServerURLType.INTRA_URL:
                    return "http://192.168.0.211:8090/login";
                case ServerURLType.OUTER_URL:
                    return "http://121.89.182.226:8184/login";

                default:
                    return "";
            }
        }

        private string GetServerAddress()
        {
            switch (settings.urlType)
            {
                case ServerURLType.INTRA_URL:
                    return "192.168.0.211";
                case ServerURLType.OUTER_URL:
                    return "121.89.182.226";

                default:
                    return "";
            }
        }

        public string GetServerVersionURL()
        {
            switch (settings.urlType)
            {
                case ServerURLType.INTRA_URL:
                    return "http://192.168.0.210/herohehe/update/version.dat";
                case ServerURLType.OUTER_URL:
                    return "http://121.89.182.226/herohehe/update/version.dat";

                default:
                    return "";
            }
        }

        public string GetNoticeURL()
        {
            return $"http://{GetServerAddress()}/upload/notice.json";
        }

        public string GetGMURL()
        {
            switch (settings.urlType)
            {
                case ServerURLType.INTRA_URL:
                    return "http://192.168.0.211:8085/command";
                case ServerURLType.OUTER_URL:
                    return "http://121.89.182.226:8183/command";

                default:
                    return "";
            }
        }

        public string GetRecRechargeFakeURL()
        {
            switch (settings.urlType)
            {
                case ServerURLType.INTRA_URL:
                    return "http://192.168.0.211:8085/recharge/fake";
                case ServerURLType.OUTER_URL:
                    return "http://121.89.182.226:8183/recharge/fake";

                default:
                    return "";
            }
        }

        static bool hasSet = false;
        static float oriWidth = 0f;
        static float oriHeight = 0f;

        //设置分辨率
        //普通机型720P ,低端机型640P
        public void SetDynamicResolution()
        {
            if (hasSet)
            {
                return;
            }
            else
            {
                oriWidth = Screen.width;
                oriHeight = Screen.height;
            }

            var under3GValue = 3072;
            var b =   SystemInfo.systemMemorySize <= under3GValue;

            if (b)//640p
            {
                if (Screen.width > 540f)
                {
                    float width1 = 540f;
                    float height1 = oriHeight * (540f / oriWidth);
                    Screen.SetResolution(Mathf.RoundToInt(width1), Mathf.RoundToInt(height1), true);
                }
            }
            else//720p
            {
                if (Screen.width > 720f)
                {
                    float width1 = 720f;
                    float height1 = oriHeight * (720f / oriWidth);
                    Screen.SetResolution(Mathf.RoundToInt(width1), Mathf.RoundToInt(height1), true);
                }
            }

            hasSet = true;
        }

        public string GetCurrentLanguage()
        {
            return LanguageToI2Name(settings.language);
        }

        /// <summary>用户切换语言时写入 GameSettings 并持久化，下次启动生效。</summary>
        public void SetLanguageByCode(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode))
                return;

            var lang = CodeToLanguage(languageCode);
            settings.language = lang;
            PlayerPrefs.SetInt(UserLanguagePrefsKey, (int)lang);
            PlayerPrefs.Save();
        }

        /// <summary>优先读取用户在设置中保存的语言，否则跟随系统语言。</summary>
        void ApplySavedLanguage()
        {
            if (PlayerPrefs.HasKey(UserLanguagePrefsKey))
            {
                var saved = (Languages)PlayerPrefs.GetInt(UserLanguagePrefsKey);
                if (Enum.IsDefined(typeof(Languages), saved))
                    settings.language = saved;
                return;
            }

            settings.language = GetSystemLanguage();
        }

        static Languages GetSystemLanguage()
        {
            return Application.systemLanguage switch
            {
                SystemLanguage.ChineseSimplified or SystemLanguage.Chinese => Languages.ChineseSimplified,
                SystemLanguage.ChineseTraditional => Languages.ChineseTraditional,
                SystemLanguage.English => Languages.English,
                SystemLanguage.Spanish => Languages.Spanish,
                SystemLanguage.Portuguese => Languages.Portuguese,
                SystemLanguage.Russian => Languages.Russian,
                SystemLanguage.German => Languages.German,
                SystemLanguage.French => Languages.French,
                SystemLanguage.Turkish => Languages.Turkish,
                SystemLanguage.Indonesian => Languages.Indonesian,
                SystemLanguage.Italian => Languages.Italian,
                SystemLanguage.Japanese => Languages.Japanese,
                SystemLanguage.Korean => Languages.Korean,
                SystemLanguage.Ukrainian => Languages.Ukrainian,
                _ => Languages.English
            };
        }

        static string LanguageToI2Name(Languages lang)
        {
            return lang switch
            {
                Languages.English => "English",
                Languages.Portuguese => "Portuguese (Brazil)",
                Languages.Spanish => "Spanish (Mexico)",
                Languages.French => "French",
                Languages.German => "German",
                Languages.Turkish => "Turkish",
                Languages.Russian => "Russian",
                Languages.Indonesian => "Indonesian",
                Languages.Italian => "Italian",
                Languages.Japanese => "Japanese",
                Languages.Korean => "Korean",
                Languages.Ukrainian => "Ukrainian",
                Languages.ChineseSimplified => "Simplified Chinese",
                Languages.ChineseTraditional => "Traditional Chinese",
                _ => "English"
            };
        }

        static Languages CodeToLanguage(string languageCode)
        {
            return languageCode switch
            {
                "en" => Languages.English,
                "es-MX" or "es" => Languages.Spanish,
                "pt-BR" or "pt" => Languages.Portuguese,
                "ru" => Languages.Russian,
                "de" => Languages.German,
                "fr" => Languages.French,
                "tr" => Languages.Turkish,
                "id" => Languages.Indonesian,
                "it" => Languages.Italian,
                "ja" => Languages.Japanese,
                "ko" => Languages.Korean,
                "uk" => Languages.Ukrainian,
                "zh-CN" or "zh-Hans" => Languages.ChineseSimplified,
                "zh-TW" or "zh-Hant" => Languages.ChineseTraditional,
                _ => Languages.English
            };
        }
    }
}