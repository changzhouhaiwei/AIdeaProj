using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace FishFramework
{
    /// <summary>
    /// 一键重命名 UI 预制体，并同步 Base / 逻辑脚本 / 调用处。
    /// </summary>
    public class UIRenameWindow : EditorWindow
    {
        private const string kMenuPath = "FishFramework/UI/Rename Prefab";

        private GameObject m_PrefabAsset;
        private string m_OldName = "";
        private string m_NewName = "";
        private string m_PrefabPath = "";
        private string m_BaseScriptPath = "";
        private string m_LogicScriptPath = "";
        private string m_LogicClassName = "";

        private bool m_RenameRootGameObject = true;
        private bool m_RenameLogicClass = true;
        private bool m_UpdateCallSites = true;

        private Vector2 m_PreviewScroll;
        private readonly List<string> m_PreviewLines = new List<string>();
        private string m_StatusMessage = "";
        private MessageType m_StatusType = MessageType.None;

        [MenuItem(kMenuPath)]
        public static void Open()
        {
            var window = GetWindow<UIRenameWindow>();
            window.titleContent = new GUIContent("UI Rename");
            window.minSize = new Vector2(460, 420);
            window.TryLoadFromSelection();
            window.Show();
        }

        public static void OpenWithPrefab(GameObject prefabAsset)
        {
            Open();
            var window = GetWindow<UIRenameWindow>();
            window.SetPrefab(prefabAsset);
        }

        private void OnEnable()
        {
            TryLoadFromSelection();
        }

        private void OnSelectionChange()
        {
            if (m_PrefabAsset == null)
            {
                TryLoadFromSelection();
                Repaint();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("UI 预制体重命名", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "会同步：预制体文件名、*Base 脚本、同名逻辑脚本、代码调用处（OpenPanel<> 等）。\n" +
                "逻辑类若曾手动改名，可取消「重命名逻辑类」，仅更新继承的 Base。",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            m_PrefabAsset = (GameObject)EditorGUILayout.ObjectField("Prefab", m_PrefabAsset, typeof(GameObject), false);
            if (EditorGUI.EndChangeCheck())
            {
                SetPrefab(m_PrefabAsset);
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("当前名称", m_OldName);
                EditorGUILayout.TextField("Prefab Path", m_PrefabPath);
                EditorGUILayout.TextField("Base 脚本", m_BaseScriptPath);
                EditorGUILayout.TextField("逻辑脚本", string.IsNullOrEmpty(m_LogicScriptPath) ? "(未找到)" : m_LogicScriptPath);
                EditorGUILayout.TextField("逻辑类名", string.IsNullOrEmpty(m_LogicClassName) ? "(未找到)" : m_LogicClassName);
            }

            EditorGUILayout.Space(4);
            m_NewName = EditorGUILayout.TextField("新名称", m_NewName);
            m_RenameRootGameObject = EditorGUILayout.ToggleLeft("同步修改预制体根节点名", m_RenameRootGameObject);
            m_RenameLogicClass = EditorGUILayout.ToggleLeft(
                $"重命名逻辑类 {(string.IsNullOrEmpty(m_LogicClassName) ? "" : $"({m_LogicClassName} → {m_NewName})")}",
                m_RenameLogicClass);
            m_UpdateCallSites = EditorGUILayout.ToggleLeft("同步修改调用代码 / 字符串引用", m_UpdateCallSites);

            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("刷新预览", GUILayout.Height(28)))
                {
                    BuildPreview();
                }

                using (new EditorGUI.DisabledScope(!CanRename()))
                {
                    var oldColor = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.55f, 0.85f, 0.55f);
                    if (GUILayout.Button("执行重命名", GUILayout.Height(28)))
                    {
                        ExecuteRename();
                    }

                    GUI.backgroundColor = oldColor;
                }
            }

            if (m_StatusType != MessageType.None && !string.IsNullOrEmpty(m_StatusMessage))
            {
                EditorGUILayout.HelpBox(m_StatusMessage, m_StatusType);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("变更预览", EditorStyles.boldLabel);
            m_PreviewScroll = EditorGUILayout.BeginScrollView(m_PreviewScroll, GUILayout.ExpandHeight(true));
            if (m_PreviewLines.Count == 0)
            {
                EditorGUILayout.LabelField("点击「刷新预览」查看将要修改的内容。", EditorStyles.miniLabel);
            }
            else
            {
                foreach (var line in m_PreviewLines)
                {
                    EditorGUILayout.LabelField(line, EditorStyles.wordWrappedMiniLabel);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void TryLoadFromSelection()
        {
            if (Selection.activeObject is GameObject go)
            {
                string path = AssetDatabase.GetAssetPath(go);
                if (!string.IsNullOrEmpty(path) && path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    SetPrefab(go);
                    return;
                }

                // Hierarchy / Prefab Stage
                var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
                if (stage != null && !string.IsNullOrEmpty(stage.assetPath))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(stage.assetPath);
                    if (prefab != null)
                    {
                        SetPrefab(prefab);
                    }
                }
            }
        }

        private void SetPrefab(GameObject prefabAsset)
        {
            m_PrefabAsset = prefabAsset;
            m_StatusMessage = "";
            m_StatusType = MessageType.None;
            m_PreviewLines.Clear();

            if (prefabAsset == null)
            {
                m_OldName = "";
                m_NewName = "";
                m_PrefabPath = "";
                m_BaseScriptPath = "";
                m_LogicScriptPath = "";
                m_LogicClassName = "";
                return;
            }

            m_PrefabPath = AssetDatabase.GetAssetPath(prefabAsset);
            m_OldName = Path.GetFileNameWithoutExtension(m_PrefabPath);
            m_NewName = m_OldName;

            ResolveRelatedScripts();
            m_RenameLogicClass = !string.IsNullOrEmpty(m_LogicClassName) && m_LogicClassName == m_OldName;
        }

        private void ResolveRelatedScripts()
        {
            m_BaseScriptPath = "";
            m_LogicScriptPath = "";
            m_LogicClassName = "";

            if (string.IsNullOrEmpty(m_PrefabPath) || string.IsNullOrEmpty(m_OldName))
            {
                return;
            }

            string fullPrefabPath = Path.GetFullPath(Path.Combine(Application.dataPath, Path.GetRelativePath("Assets", m_PrefabPath)));
            string fullRootDir = Path.GetFullPath(Path.Combine(Application.dataPath, EditorSetting.Instance.uiPrefabRootDir));

            string expectedBase = "";
            string expectedLogic = "";
            if (fullPrefabPath.StartsWith(fullRootDir, StringComparison.OrdinalIgnoreCase))
            {
                string subPath = UIEditorUtility.RemapGeneratedScriptSubPath(Path.GetRelativePath(fullRootDir, fullPrefabPath));
                string subDir = Path.GetDirectoryName(subPath) ?? "";
                expectedBase = Path.GetFullPath(Path.Combine(
                    Application.dataPath,
                    EditorSetting.Instance.generatedBaseUIRootDir,
                    subDir,
                    m_OldName + "Base.cs"));
                expectedLogic = Path.GetFullPath(Path.Combine(
                    Application.dataPath,
                    EditorSetting.Instance.generatedTempUIRootDir,
                    subDir,
                    m_OldName + ".cs"));
            }

            if (!string.IsNullOrEmpty(expectedBase) && File.Exists(expectedBase))
            {
                m_BaseScriptPath = ToAssetPath(expectedBase);
            }
            else
            {
                m_BaseScriptPath = FindScriptAssetByClassName(m_OldName + "Base");
            }

            // 优先找 class OldName，其次找继承 OldNameBase 的逻辑类
            if (!string.IsNullOrEmpty(expectedLogic) && File.Exists(expectedLogic))
            {
                m_LogicScriptPath = ToAssetPath(expectedLogic);
                m_LogicClassName = m_OldName;
            }
            else
            {
                string byName = FindScriptAssetByClassName(m_OldName);
                if (!string.IsNullOrEmpty(byName))
                {
                    m_LogicScriptPath = byName;
                    m_LogicClassName = m_OldName;
                }
                else
                {
                    var inherit = FindScriptInheritingBase(m_OldName + "Base");
                    if (inherit.HasValue)
                    {
                        m_LogicScriptPath = inherit.Value.path;
                        m_LogicClassName = inherit.Value.className;
                    }
                }
            }
        }

        private bool CanRename()
        {
            if (Application.isPlaying) return false;
            if (m_PrefabAsset == null) return false;
            if (string.IsNullOrEmpty(m_OldName) || string.IsNullOrEmpty(m_NewName)) return false;
            if (m_OldName == m_NewName) return false;
            if (!IsValidIdentifier(m_NewName)) return false;
            if (string.IsNullOrEmpty(m_BaseScriptPath)) return false;
            return true;
        }

        private void BuildPreview()
        {
            m_PreviewLines.Clear();
            m_StatusMessage = "";
            m_StatusType = MessageType.None;

            string error = ValidateBeforeRename();
            if (!string.IsNullOrEmpty(error))
            {
                m_StatusMessage = error;
                m_StatusType = MessageType.Error;
                return;
            }

            string newPrefabPath = ReplaceFileName(m_PrefabPath, m_NewName + ".prefab");
            m_PreviewLines.Add($"[Prefab] {m_PrefabPath} → {newPrefabPath}");
            if (m_RenameRootGameObject)
            {
                m_PreviewLines.Add($"[RootGO] {m_OldName} → {m_NewName}");
            }

            string newBasePath = ReplaceFileName(m_BaseScriptPath, m_NewName + "Base.cs");
            m_PreviewLines.Add($"[Base] {m_BaseScriptPath} → {newBasePath}");
            m_PreviewLines.Add($"[Base] class {m_OldName}Base → {m_NewName}Base");
            m_PreviewLines.Add($"[Base] PrefabPath 更新为 {newPrefabPath}");

            if (!string.IsNullOrEmpty(m_LogicScriptPath))
            {
                if (m_RenameLogicClass && !string.IsNullOrEmpty(m_LogicClassName))
                {
                    string newLogicPath = ReplaceFileName(m_LogicScriptPath, m_NewName + ".cs");
                    m_PreviewLines.Add($"[Logic] {m_LogicScriptPath} → {newLogicPath}");
                    m_PreviewLines.Add($"[Logic] class {m_LogicClassName} → {m_NewName}");
                }
                else
                {
                    m_PreviewLines.Add($"[Logic] 保留类名 {m_LogicClassName}，仅更新 : {m_OldName}Base → : {m_NewName}Base");
                }
            }
            else
            {
                m_PreviewLines.Add("[Logic] 未找到逻辑脚本，跳过");
            }

            if (m_UpdateCallSites)
            {
                var hits = CollectCallSiteHits();
                m_PreviewLines.Add($"[Refs] 预计修改 {hits.Count} 个脚本文件中的引用");
                foreach (var hit in hits.Take(40))
                {
                    m_PreviewLines.Add($"  - {hit}");
                }

                if (hits.Count > 40)
                {
                    m_PreviewLines.Add($"  ... 另有 {hits.Count - 40} 个文件");
                }
            }

            m_StatusMessage = "预览已生成，确认无误后执行。";
            m_StatusType = MessageType.Info;
        }

        private void ExecuteRename()
        {
            string error = ValidateBeforeRename();
            if (!string.IsNullOrEmpty(error))
            {
                m_StatusMessage = error;
                m_StatusType = MessageType.Error;
                EditorUtility.DisplayDialog("重命名失败", error, "确定");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "确认重命名",
                    $"将把 UI「{m_OldName}」重命名为「{m_NewName}」。\n此操作会修改预制体与脚本，建议先提交或备份。",
                    "执行",
                    "取消"))
            {
                return;
            }

            string oldName = m_OldName;
            string newName = m_NewName;
            string oldBase = oldName + "Base";
            string newBase = newName + "Base";
            string oldLogicClass = m_LogicClassName;
            string oldPrefabPath = m_PrefabPath;
            string oldBasePath = m_BaseScriptPath;
            string oldLogicPath = m_LogicScriptPath;
            bool renameRoot = m_RenameRootGameObject;
            bool renameLogic = m_RenameLogicClass;
            bool updateRefs = m_UpdateCallSites;
            string newPrefabPath = ReplaceFileName(oldPrefabPath, newName + ".prefab");

            try
            {
                AssetDatabase.StartAssetEditing();

                // 1) 重命名预制体
                string renameErr = AssetDatabase.RenameAsset(oldPrefabPath, newName);
                if (!string.IsNullOrEmpty(renameErr))
                {
                    throw new Exception("重命名预制体失败: " + renameErr);
                }

                // 2) Base 内容 + 文件名
                string baseFull = ToFullPath(oldBasePath);
                string baseText = File.ReadAllText(baseFull, Encoding.UTF8);
                baseText = Regex.Replace(baseText, $@"\b{Regex.Escape(oldBase)}\b", newBase);
                baseText = baseText.Replace(oldPrefabPath, newPrefabPath);
                baseText = Regex.Replace(
                    baseText,
                    $@"(PrefabPath\s*=>\s*"")([^""]*/){Regex.Escape(oldName)}(\.prefab"")",
                    $"$1$2{newName}$3");
                File.WriteAllText(baseFull, baseText, Encoding.UTF8);

                string newBasePath = ReplaceFileName(oldBasePath, newBase + ".cs");
                if (!string.Equals(oldBasePath, newBasePath, StringComparison.OrdinalIgnoreCase))
                {
                    renameErr = AssetDatabase.RenameAsset(oldBasePath, newBase);
                    if (!string.IsNullOrEmpty(renameErr))
                    {
                        throw new Exception("重命名 Base 脚本失败: " + renameErr);
                    }
                }

                // 3) Logic 内容 + 文件名
                if (!string.IsNullOrEmpty(oldLogicPath) && File.Exists(ToFullPath(oldLogicPath)))
                {
                    string logicFull = ToFullPath(oldLogicPath);
                    string logicText = File.ReadAllText(logicFull, Encoding.UTF8);
                    logicText = Regex.Replace(logicText, $@"\b{Regex.Escape(oldBase)}\b", newBase);

                    if (renameLogic && !string.IsNullOrEmpty(oldLogicClass))
                    {
                        logicText = Regex.Replace(logicText, $@"\b{Regex.Escape(oldLogicClass)}\b", newName);
                        logicText = Regex.Replace(
                            logicText,
                            $@"(PoolObjPath\s*=>\s*""[^""]*/){Regex.Escape(oldLogicClass)}("")",
                            $"$1{newName}$2");
                    }

                    File.WriteAllText(logicFull, logicText, Encoding.UTF8);

                    if (renameLogic && !string.IsNullOrEmpty(oldLogicClass))
                    {
                        string newLogicPath = ReplaceFileName(oldLogicPath, newName + ".cs");
                        if (!string.Equals(oldLogicPath, newLogicPath, StringComparison.OrdinalIgnoreCase))
                        {
                            renameErr = AssetDatabase.RenameAsset(oldLogicPath, newName);
                            if (!string.IsNullOrEmpty(renameErr))
                            {
                                throw new Exception("重命名逻辑脚本失败: " + renameErr);
                            }
                        }
                    }
                }

                // 4) 全工程调用处（排除已处理的 Base / Logic 新路径）
                if (updateRefs)
                {
                    var skip = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { newBasePath };
                    if (!string.IsNullOrEmpty(oldLogicPath))
                    {
                        skip.Add(renameLogic
                            ? ReplaceFileName(oldLogicPath, newName + ".cs")
                            : oldLogicPath);
                    }

                    UpdateCallSites(
                        oldName,
                        newName,
                        oldBase,
                        newBase,
                        oldLogicClass,
                        renameLogic ? newName : oldLogicClass,
                        skip);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                m_StatusMessage = e.Message;
                m_StatusType = MessageType.Error;
                EditorUtility.DisplayDialog("重命名失败", e.Message, "确定");
                return;
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            // 根节点在 AssetEditing 外处理，避免 Prefab 保存冲突
            if (renameRoot)
            {
                try
                {
                    RenamePrefabRoot(newPrefabPath, newName);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
                catch (Exception e)
                {
                    Debug.LogWarning("预制体已重命名，但根节点改名失败: " + e.Message);
                }
            }

            // 重新加载状态
            var newPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ReplaceFileName(oldPrefabPath, newName + ".prefab"));
            SetPrefab(newPrefab);
            BuildPreview();

            m_StatusMessage = $"已成功重命名为 {newName}。请等待编译完成后检查引用。";
            m_StatusType = MessageType.Info;
            EditorUtility.DisplayDialog("完成", m_StatusMessage, "确定");
        }

        private string ValidateBeforeRename()
        {
            if (Application.isPlaying) return "请在非运行时操作。";
            if (m_PrefabAsset == null) return "请先指定预制体。";
            if (string.IsNullOrEmpty(m_PrefabPath)) return "无法解析预制体路径。";
            if (!m_PrefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)) return "目标不是预制体。";

            string fullPrefabPath = Path.GetFullPath(Path.Combine(Application.dataPath, Path.GetRelativePath("Assets", m_PrefabPath)));
            string fullRootDir = Path.GetFullPath(Path.Combine(Application.dataPath, EditorSetting.Instance.uiPrefabRootDir));
            if (!fullPrefabPath.StartsWith(fullRootDir, StringComparison.OrdinalIgnoreCase))
            {
                return "预制体不在可导出根目录中: " + EditorSetting.Instance.uiPrefabRootDir;
            }

            if (m_PrefabAsset.GetComponent<UIViewBehaviour>() == null)
            {
                return "预制体根节点缺少 UIPanelBehaviour / UIWidgetBehaviour。";
            }

            if (string.IsNullOrWhiteSpace(m_NewName)) return "新名称不能为空。";
            if (m_NewName == m_OldName) return "新名称与当前名称相同。";
            if (!IsValidIdentifier(m_NewName)) return "新名称不是合法的 C# 标识符（需字母/数字/下划线，且不能以数字开头）。";

            if (string.IsNullOrEmpty(m_BaseScriptPath) || !File.Exists(ToFullPath(m_BaseScriptPath)))
            {
                return $"未找到 {m_OldName}Base 脚本，请先 ExportBase。";
            }

            string newPrefabPath = ReplaceFileName(m_PrefabPath, m_NewName + ".prefab");
            if (File.Exists(ToFullPath(newPrefabPath)))
            {
                return "目标预制体已存在: " + newPrefabPath;
            }

            string newBasePath = ReplaceFileName(m_BaseScriptPath, m_NewName + "Base.cs");
            if (!string.Equals(m_BaseScriptPath, newBasePath, StringComparison.OrdinalIgnoreCase)
                && File.Exists(ToFullPath(newBasePath)))
            {
                return "目标 Base 脚本已存在: " + newBasePath;
            }

            if (m_RenameLogicClass && !string.IsNullOrEmpty(m_LogicScriptPath))
            {
                string newLogicPath = ReplaceFileName(m_LogicScriptPath, m_NewName + ".cs");
                if (!string.Equals(m_LogicScriptPath, newLogicPath, StringComparison.OrdinalIgnoreCase)
                    && File.Exists(ToFullPath(newLogicPath)))
                {
                    return "目标逻辑脚本已存在: " + newLogicPath;
                }
            }

            return null;
        }

        private List<string> CollectCallSiteHits()
        {
            var result = new List<string>();
            string[] guids = AssetDatabase.FindAssets("t:Script", new[] { "Assets/Scripts" });
            string oldBase = m_OldName + "Base";
            string logicClass = m_RenameLogicClass ? m_LogicClassName : null;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(path, m_BaseScriptPath, StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(path, m_LogicScriptPath, StringComparison.OrdinalIgnoreCase)) continue;

                string text = File.ReadAllText(ToFullPath(path), Encoding.UTF8);
                bool hit = Regex.IsMatch(text, $@"\b{Regex.Escape(oldBase)}\b");
                if (!hit)
                {
                    hit = Regex.IsMatch(text, $@"\b{Regex.Escape(m_OldName)}\b");
                }

                if (!hit && !string.IsNullOrEmpty(logicClass) && logicClass != m_OldName)
                {
                    hit = Regex.IsMatch(text, $@"\b{Regex.Escape(logicClass)}\b");
                }

                if (hit)
                {
                    result.Add(path);
                }
            }

            return result;
        }

        private void UpdateCallSites(
            string oldName,
            string newName,
            string oldBase,
            string newBase,
            string oldLogicClass,
            string newLogicClass,
            HashSet<string> skipPaths)
        {
            string[] guids = AssetDatabase.FindAssets("t:Script", new[] { "Assets/Scripts" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) continue;
                if (skipPaths != null && skipPaths.Contains(path)) continue;

                string full = ToFullPath(path);
                if (!File.Exists(full)) continue;

                string text = File.ReadAllText(full, Encoding.UTF8);
                string original = text;

                // 先替换更长的 Base，避免 OldName 误伤 OldNameBase
                text = Regex.Replace(text, $@"\b{Regex.Escape(oldBase)}\b", newBase);

                if (!string.IsNullOrEmpty(oldLogicClass)
                    && !string.IsNullOrEmpty(newLogicClass)
                    && oldLogicClass != newLogicClass)
                {
                    text = Regex.Replace(text, $@"\b{Regex.Escape(oldLogicClass)}\b", newLogicClass);
                    text = Regex.Replace(
                        text,
                        $@"(PoolObjPath\s*=>\s*""[^""]*/){Regex.Escape(oldLogicClass)}("")",
                        $"$1{newLogicClass}$2");
                }
                else if (oldLogicClass != oldName)
                {
                    // 逻辑类未改名时，不要把无关的 OldName 标识符全替换；
                    // 但仍替换明确的预制体路径片段与面板名字符串。
                }

                // 同名逻辑类场景：OldName 即逻辑类
                if (string.IsNullOrEmpty(oldLogicClass) || oldLogicClass == oldName)
                {
                    text = Regex.Replace(text, $@"\b{Regex.Escape(oldName)}\b", newName);
                }
                else
                {
                    // 仅替换路径/字符串中的预制体名
                    text = Regex.Replace(
                        text,
                        $@"(Assets/[^""]*/){Regex.Escape(oldName)}(\.prefab)",
                        $"$1{newName}$2");
                    text = Regex.Replace(
                        text,
                        $@"(/){Regex.Escape(oldName)}("")",
                        $"$1{newName}$2");
                }

                if (!string.Equals(original, text, StringComparison.Ordinal))
                {
                    File.WriteAllText(full, text, Encoding.UTF8);
                }
            }
        }

        private static string FindScriptAssetByClassName(string className)
        {
            string pattern = $@"\bclass\s+{Regex.Escape(className)}\b";
            string[] guids = AssetDatabase.FindAssets("t:Script", new[]
            {
                "Assets/Scripts/HotFix/GameLogic",
                "Assets/Scripts"
            });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) continue;
                string text = File.ReadAllText(ToFullPath(path), Encoding.UTF8);
                if (Regex.IsMatch(text, pattern))
                {
                    return path;
                }
            }

            return "";
        }

        private static (string path, string className)? FindScriptInheritingBase(string baseClassName)
        {
            string pattern = $@"\bclass\s+(\w+)\s*:\s*{Regex.Escape(baseClassName)}\b";
            string[] guids = AssetDatabase.FindAssets("t:Script", new[]
            {
                "Assets/Features",
                "Assets/Scripts/HotFix/GameLogic"
            });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) continue;
                string text = File.ReadAllText(ToFullPath(path), Encoding.UTF8);
                var match = Regex.Match(text, pattern);
                if (match.Success)
                {
                    return (path, match.Groups[1].Value);
                }
            }

            return null;
        }

        private static void RenamePrefabRoot(string prefabAssetPath, string newRootName)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(prefabAssetPath);
            try
            {
                if (contents != null && contents.name != newRootName)
                {
                    contents.name = newRootName;
                    PrefabUtility.SaveAsPrefabAsset(contents, prefabAssetPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static bool IsValidIdentifier(string name)
        {
            return Regex.IsMatch(name ?? "", @"^[A-Za-z_][A-Za-z0-9_]*$");
        }

        private static string ReplaceFileName(string assetPath, string newFileName)
        {
            if (string.IsNullOrEmpty(assetPath)) return assetPath;
            string dir = Path.GetDirectoryName(assetPath)?.Replace('\\', '/') ?? "";
            return string.IsNullOrEmpty(dir) ? newFileName : $"{dir}/{newFileName}";
        }

        private static string ToFullPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return assetPath;
            if (assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
                || assetPath.Equals("Assets", StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetFullPath(Path.Combine(Application.dataPath, Path.GetRelativePath("Assets", assetPath)));
            }

            return Path.GetFullPath(assetPath);
        }

        private static string ToAssetPath(string fullPath)
        {
            string normalized = Path.GetFullPath(fullPath).Replace('\\', '/');
            string dataPath = Application.dataPath.Replace('\\', '/');
            if (normalized.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
            {
                return "Assets" + normalized.Substring(dataPath.Length);
            }

            return normalized;
        }
    }
}
