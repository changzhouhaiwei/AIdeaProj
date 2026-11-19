using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

namespace FishXSortPlay
{
    public static class DevPanelEnabler
    {
        private const string SettingName = "IsDevPanelDisplayed";

        private static GameObject panel;

        public static bool IsDevPanelDisplayed()
        {
#if UNITY_EDITOR
            return IsDevPanelDisplayedPrefs;
#else
            return false;
#endif
        }

        public static void RegisterPanel(GameObject panel)
        {
            DevPanelEnabler.panel = panel;
            panel.SetActive(IsDevPanelDisplayed());
        }

#if UNITY_EDITOR
        private static bool IsDevPanelDisplayedPrefs
        {
            get { return EditorPrefs.GetBool(SettingName, false); }
        }
#endif
    }
}