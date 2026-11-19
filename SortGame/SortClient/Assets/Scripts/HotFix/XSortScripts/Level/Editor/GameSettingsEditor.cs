#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using FishFramework;
using FishXSortPlay;

namespace XSortGameCore
{
    [CustomEditor(typeof(GameSettings))]
    public class GameSettingsEditor : OdinEditor
    {
        private LevelPlatform previousPlatform;

        protected override void OnEnable()
        {
            base.OnEnable();
            previousPlatform = ((GameSettings)target).levelPlatform;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var settings = (GameSettings)target;
            if (settings.levelPlatform == previousPlatform)
            {
                return;
            }

            previousPlatform = settings.levelPlatform;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            LevelEditorWindow.ApplyLevelPlatformRefresh();
            Debug.LogFormat(
                "[GameSettings] 关卡平台已切换为 {0}，GameScene 与 LevelCatalog 已刷新。",
                LevelPlatformResolver.GetDisplayName(settings.levelPlatform));
        }

        [MenuItem("Tools/Level Editor/GameSetting", false, 1)]
        static void OpenGameSettings()
        {
            var settings = GameUtility.LoadGameSettings();
            if (settings == null)
            {
                Debug.LogError("[GameSettings] 无法加载 GameSettings 资源。");
                return;
            }

            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }
    }
}
#endif
