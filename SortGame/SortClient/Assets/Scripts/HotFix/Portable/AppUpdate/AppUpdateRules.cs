using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Portable.AppUpdate
{
    public enum AppUpdateKind
    {
        None = 0,
        Soft = 1,
        Force = 2,
    }

    /// <summary>
    /// 各游戏只填这份选项。Remote Config、关卡号、商店保底链接都由游戏自己传入。
    /// </summary>
    public sealed class AppUpdateHostOptions
    {
        /// <summary>配置里没写 min_show_level 时用这个关卡门槛。0 表示不限制。</summary>
        public int MinShowLevel;

        public string DefaultStoreUrl;
        public string DefaultFallbackUrl;
        public string DefaultTitle = "Important content update — please update the app";
        public string DefaultMessage = "Your current version is too old. This update adds brand-new clearing elements and level mechanics, so older versions cannot enter for now. Tap the button below to update to the latest version in the app store and continue playing right away~";
        public string DefaultConfirmText = "Update Now";
        public string DefaultSkipText = "Maybe Later";
        public string TitleTerm;
        public string MessageTerm;
        public string ConfirmTerm;
        public string SkipTerm;
        public Func<string, string> Translate;
        public bool AllowInEditor;
        public string SoftDismissPrefsKey = "app_update_soft_dismissed_version";
    }

    public sealed class AppUpdateConfig
    {
        public bool Enabled;
        public string StoreUrl;
        public string FallbackUrl;
        public string LatestVersion;
        public string MinRequiredVersion;
        public List<string> ForceVersions = new List<string>();
        public string Title;
        public string Message;
        public bool HasMinShowLevel;
        public int MinShowLevel;
    }

    public sealed class AppUpdatePrompt
    {
        public bool ForceUpdate;
        public string Title;
        public string Message;
        public string StoreUrl;
        public string FallbackUrl;
        public string LatestVersion;
        public string MinRequiredVersion;
        public string ConfirmText;
        public string SkipText;
    }

    public struct AppUpdateDecision
    {
        public AppUpdateKind Kind;
        public bool ShowDialog;
        public bool BlockProgress;
        public string DismissKey;
        public string Reason;
    }

    /// <summary>
    /// Remote Config 键 AppUpdateConfig 的纯逻辑。不读 SDK，不弹 UI。
    /// 强更：当前版本低于 min_required_version，或命中 force_update_versions。
    /// 软更：高于等于最低版本，但低于 latest_version。关掉后记住 latest_version，同一目标版本不再弹。
    /// </summary>
    public static class AppUpdateRules
    {
        public static AppUpdateConfig Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            var config = new AppUpdateConfig
            {
                Enabled = ReadBool(json, "enabled"),
                StoreUrl = ReadString(json, "store_url"),
                FallbackUrl = ReadString(json, "fallback_url"),
                LatestVersion = ReadString(json, "latest_version"),
                MinRequiredVersion = ReadString(json, "min_required_version"),
                Title = ReadString(json, "title"),
                Message = ReadString(json, "message"),
                ForceVersions = ReadStringArray(json, "force_update_versions"),
            };

            if (TryReadInt(json, "min_show_level", out int minShowLevel))
            {
                config.HasMinShowLevel = true;
                config.MinShowLevel = minShowLevel;
            }

            return config;
        }

        public static int CompareVersion(string left, string right)
        {
            var a = VersionParts(left);
            var b = VersionParts(right);
            int count = Math.Max(a.Count, b.Count);
            for (int i = 0; i < count; i++)
            {
                int av = i < a.Count ? a[i] : 0;
                int bv = i < b.Count ? b[i] : 0;
                if (av != bv)
                {
                    return av < bv ? -1 : 1;
                }
            }

            return 0;
        }

        public static AppUpdateDecision Evaluate(
            AppUpdateConfig config,
            string currentVersion,
            int currentLevel,
            AppUpdateHostOptions options,
            string dismissedLatest,
            bool isEditor)
        {
            if (options == null)
            {
                options = new AppUpdateHostOptions();
            }

            if (config == null)
            {
                return Hidden(AppUpdateKind.None, "配置为空");
            }

            if (!config.Enabled)
            {
                return Hidden(AppUpdateKind.None, "enabled=false");
            }

            if (string.IsNullOrWhiteSpace(currentVersion))
            {
                return Hidden(AppUpdateKind.None, "当前版本为空");
            }

            if (isEditor && !options.AllowInEditor)
            {
                return Hidden(AppUpdateKind.None, "编辑器默认不弹");
            }

            string version = currentVersion.Trim();
            AppUpdateKind kind = DecideKind(config, version);
            if (kind == AppUpdateKind.None)
            {
                return Hidden(kind, "不需要更新 current=" + version
                    + " latest=" + config.LatestVersion
                    + " min=" + config.MinRequiredVersion
                    + " force=" + JoinVersions(config.ForceVersions));
            }

            int gate = config.HasMinShowLevel ? config.MinShowLevel : options.MinShowLevel;
            if (currentLevel < gate)
            {
                return Hidden(kind, "关卡未到 level=" + currentLevel + " gate=" + gate + " " + DescribeKind(config, version, kind));
            }

            string dismissKey = (config.LatestVersion ?? string.Empty).Trim();
            if (kind == AppUpdateKind.Soft
                && !string.IsNullOrEmpty(dismissKey)
                && string.Equals(dismissedLatest, dismissKey, StringComparison.Ordinal))
            {
                return Hidden(kind, "软更已关闭 latest=" + dismissKey);
            }

            return new AppUpdateDecision
            {
                Kind = kind,
                ShowDialog = true,
                BlockProgress = kind == AppUpdateKind.Force,
                DismissKey = dismissKey,
                Reason = DescribeKind(config, version, kind),
            };
        }

        static AppUpdateDecision Hidden(AppUpdateKind kind, string reason)
        {
            return new AppUpdateDecision
            {
                Kind = kind,
                Reason = reason,
            };
        }

        static string DescribeKind(AppUpdateConfig config, string currentVersion, AppUpdateKind kind)
        {
            if (kind == AppUpdateKind.Force && config.ForceVersions != null)
            {
                for (int i = 0; i < config.ForceVersions.Count; i++)
                {
                    string listed = config.ForceVersions[i];
                    if (!string.IsNullOrWhiteSpace(listed) && CompareVersion(currentVersion, listed.Trim()) == 0)
                    {
                        return "强更 命中名单 " + listed.Trim();
                    }
                }
            }

            if (kind == AppUpdateKind.Force)
            {
                return "强更 低于min_required current=" + currentVersion + " min=" + config.MinRequiredVersion;
            }

            if (kind == AppUpdateKind.Soft)
            {
                return "软更 低于latest current=" + currentVersion + " latest=" + config.LatestVersion;
            }

            return "不需要更新";
        }

        static string JoinVersions(List<string> versions)
        {
            if (versions == null || versions.Count == 0)
            {
                return "[]";
            }

            return "[" + string.Join(",", versions.ToArray()) + "]";
        }

        public static AppUpdatePrompt BuildPrompt(AppUpdateConfig config, AppUpdateKind kind, AppUpdateHostOptions options)
        {
            if (options == null)
            {
                options = new AppUpdateHostOptions();
            }

            return new AppUpdatePrompt
            {
                ForceUpdate = kind == AppUpdateKind.Force,
                Title = Localized(options, options.TitleTerm, config != null ? config.Title : null, options.DefaultTitle, options.DefaultTitle),
                Message = Localized(options, options.MessageTerm, config != null ? config.Message : null, options.DefaultMessage, options.DefaultMessage),
                ConfirmText = Localized(options, options.ConfirmTerm, null, options.DefaultConfirmText, options.DefaultConfirmText),
                SkipText = Localized(options, options.SkipTerm, null, options.DefaultSkipText, options.DefaultSkipText),
                StoreUrl = FirstNonEmpty(config != null ? config.StoreUrl : null, options.DefaultStoreUrl),
                FallbackUrl = FirstNonEmpty(config != null ? config.FallbackUrl : null, options.DefaultFallbackUrl),
                LatestVersion = config != null ? config.LatestVersion : null,
                MinRequiredVersion = config != null ? config.MinRequiredVersion : null,
            };
        }

        static string Localized(AppUpdateHostOptions options, string term, string configValue, string fallback, string lastResort)
        {
            if (options.Translate != null && !string.IsNullOrWhiteSpace(term))
            {
                string translated = options.Translate(term);
                if (!string.IsNullOrWhiteSpace(translated)
                    && !string.Equals(translated, term, StringComparison.Ordinal))
                {
                    return translated.Trim();
                }
            }

            return FirstNonEmpty(configValue, fallback, lastResort);
        }

        public static AppUpdateKind Classify(AppUpdateConfig config, string currentVersion)
        {
            if (config == null || !config.Enabled || string.IsNullOrWhiteSpace(currentVersion))
            {
                return AppUpdateKind.None;
            }

            return DecideKind(config, currentVersion.Trim());
        }

        static AppUpdateKind DecideKind(AppUpdateConfig config, string currentVersion)
        {
            if (config.ForceVersions != null)
            {
                for (int i = 0; i < config.ForceVersions.Count; i++)
                {
                    string listed = config.ForceVersions[i];
                    if (!string.IsNullOrWhiteSpace(listed) && CompareVersion(currentVersion, listed.Trim()) == 0)
                    {
                        return AppUpdateKind.Force;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(config.MinRequiredVersion)
                && CompareVersion(currentVersion, config.MinRequiredVersion.Trim()) < 0)
            {
                return AppUpdateKind.Force;
            }

            if (!string.IsNullOrWhiteSpace(config.LatestVersion)
                && CompareVersion(currentVersion, config.LatestVersion.Trim()) < 0)
            {
                return AppUpdateKind.Soft;
            }

            return AppUpdateKind.None;
        }

        static List<int> VersionParts(string version)
        {
            var parts = new List<int>();
            if (string.IsNullOrWhiteSpace(version))
            {
                return parts;
            }

            string text = version.Trim();
            int i = 0;
            while (i < text.Length)
            {
                if (!char.IsDigit(text[i]))
                {
                    break;
                }

                int start = i;
                while (i < text.Length && char.IsDigit(text[i]))
                {
                    i++;
                }

                if (int.TryParse(text.Substring(start, i - start), out int value))
                {
                    parts.Add(value);
                }

                if (i < text.Length && text[i] == '.')
                {
                    i++;
                    continue;
                }

                break;
            }

            return parts;
        }

        static string FirstNonEmpty(params string[] values)
        {
            if (values == null)
            {
                return string.Empty;
            }

            for (int i = 0; i < values.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(values[i]))
                {
                    return values[i].Trim();
                }
            }

            return string.Empty;
        }

        static bool ReadBool(string json, string key)
        {
            var match = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(true|false)", RegexOptions.IgnoreCase);
            return match.Success && string.Equals(match.Groups[1].Value, "true", StringComparison.OrdinalIgnoreCase);
        }

        static bool TryReadInt(string json, string key, out int value)
        {
            var match = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(-?\\d+)");
            if (!match.Success)
            {
                value = 0;
                return false;
            }

            return int.TryParse(match.Groups[1].Value, out value);
        }

        static string ReadString(string json, string key)
        {
            var match = Regex.Match(
                json,
                "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"((?:\\\\.|[^\"\\\\])*)\"");
            return match.Success ? Unescape(match.Groups[1].Value) : string.Empty;
        }

        static List<string> ReadStringArray(string json, string key)
        {
            var values = new List<string>();
            var match = Regex.Match(
                json,
                "\"" + Regex.Escape(key) + "\"\\s*:\\s*\\[(.*?)\\]",
                RegexOptions.Singleline);
            if (!match.Success)
            {
                return values;
            }

            var items = Regex.Matches(match.Groups[1].Value, "\"((?:\\\\.|[^\"\\\\])*)\"");
            for (int i = 0; i < items.Count; i++)
            {
                values.Add(Unescape(items[i].Groups[1].Value));
            }

            return values;
        }

        static string Unescape(string value)
        {
            if (string.IsNullOrEmpty(value) || value.IndexOf('\\') < 0)
            {
                return value ?? string.Empty;
            }

            return value
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\")
                .Replace("\\/", "/")
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t");
        }
    }
}
