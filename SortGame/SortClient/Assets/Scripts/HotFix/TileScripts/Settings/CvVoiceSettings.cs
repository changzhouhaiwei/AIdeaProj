using UnityEngine;

namespace FishTilePlay
{
    /// <summary>设置页 soundPeople：关卡喝彩 CV / Perfect Collect 人声音效开关。</summary>
    public static class CvVoiceSettings
    {
        const string PrefsKey = "CvVoiceEnabled";

        public static bool IsEnabled
        {
            get => PlayerPrefs.GetInt(PrefsKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(PrefsKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
    }
}
