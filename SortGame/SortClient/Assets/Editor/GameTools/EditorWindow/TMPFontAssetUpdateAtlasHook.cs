using System;
using System.Reflection;
using TMPro;
using TMPro.EditorUtilities;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 点击 TMP Font Asset Inspector 的 Update Atlas Texture（或右键同名菜单）时，
/// 对 Arial-Unicode-Bold-RSDF 执行「游戏工具/Arial字体设为动态」，再打开 Font Asset Creator。
/// </summary>
[CustomEditor(typeof(TMP_FontAsset))]
public class TMPFontAssetUpdateAtlasHookEditor : TMP_FontAssetEditor
{
    public override void OnInspectorGUI()
    {
        var font = target as TMP_FontAsset;
        Event e = Event.current;
        bool watchClick = TMPFontAssetUpdateAtlasHook.IsArialFont(font)
                          && e.type == EventType.MouseUp
                          && e.button == 0;
        Vector2 mouse = e.mousePosition;

        base.OnInspectorGUI();

        if (watchClick && TMPFontAssetUpdateAtlasHook.IsUpdateAtlasTextureButton(mouse))
        {
            TMPFontAssetUpdateAtlasHook.ScheduleSetArialDynamic(font);
        }
    }
}

[InitializeOnLoad]
public static class TMPFontAssetUpdateAtlasHook
{
    const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    static readonly FieldInfo SelectedFontField =
        typeof(TMPro_FontAssetCreatorWindow).GetField("m_SelectedFontAsset", InstanceFlags);

    static bool s_CreatorWasOpen;
    static int s_LastHandledFontId;
    static double s_LastHandledTime;

    static TMPFontAssetUpdateAtlasHook()
    {
        s_CreatorWasOpen = EditorWindow.HasOpenInstances<TMPro_FontAssetCreatorWindow>();
        EditorApplication.update += OnEditorUpdate;
    }

    public static bool IsArialFont(TMP_FontAsset font)
    {
        if (font == null)
        {
            return false;
        }

        string path = AssetDatabase.GetAssetPath(font);
        return string.Equals(path, TMPFontAtlasCompressor.ArialFontAssetPath, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 对齐 TMP_FontAssetEditor：Inspector 正文第一行 GetControlRect(false, 24)，按钮在右侧 130x18。
    /// </summary>
    public static bool IsUpdateAtlasTextureButton(Vector2 mousePosition)
    {
        if (mousePosition.y < 0f || mousePosition.y > 28f)
        {
            return false;
        }

        float viewWidth = EditorGUIUtility.currentViewWidth;
        const float margin = 18f;
        Rect row = new Rect(margin, 0f, Mathf.Max(0f, viewWidth - margin * 2f), 24f);
        Rect button = new Rect(row.xMax - 132f, row.y + 2f, 130f, 18f);
        button.xMin -= 8f;
        button.xMax += 8f;
        button.yMin -= 4f;
        button.yMax += 4f;
        return button.Contains(mousePosition);
    }

    public static void ScheduleSetArialDynamic(TMP_FontAsset font)
    {
        if (!IsArialFont(font))
        {
            return;
        }

        int id = font.GetInstanceID();
        double now = EditorApplication.timeSinceStartup;
        if (id == s_LastHandledFontId && now - s_LastHandledTime < 0.75)
        {
            return;
        }

        s_LastHandledFontId = id;
        s_LastHandledTime = now;

        EditorApplication.delayCall += () =>
        {
            if (font == null)
            {
                return;
            }

            if (!TMPFontAtlasCompressor.BindArialSourceFontAndSetDynamic())
            {
                return;
            }

            TMPro_FontAssetCreatorWindow.ShowFontAtlasCreatorWindow(font);
            Debug.Log("[TMPFontAssetUpdateAtlasHook] 已执行「Arial字体设为动态」，并刷新 Font Asset Creator。");
        };
    }

    static void OnEditorUpdate()
    {
        bool isOpen = EditorWindow.HasOpenInstances<TMPro_FontAssetCreatorWindow>();
        bool justOpened = isOpen && !s_CreatorWasOpen;
        s_CreatorWasOpen = isOpen;

        if (!justOpened)
        {
            return;
        }

        var creator = FindCreatorWindow();
        var font = SelectedFontField != null && creator != null
            ? SelectedFontField.GetValue(creator) as TMP_FontAsset
            : null;
        if (font == null)
        {
            font = Selection.activeObject as TMP_FontAsset;
        }

        ScheduleSetArialDynamic(font);
    }

    static TMPro_FontAssetCreatorWindow FindCreatorWindow()
    {
        var windows = Resources.FindObjectsOfTypeAll<TMPro_FontAssetCreatorWindow>();
        return windows != null && windows.Length > 0 ? windows[0] : null;
    }
}
