#pragma warning disable 649


#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using System.IO;
using UnityEngine;
using UnityEditor;
using XSortGameCore.List;
using XSortGameCore;
using FishFramework;

namespace FishXSortPlay
{
    public class LevelsHandler
    {

        //strings
        private const string ASSET_SUFFIX = ".asset";
        private const string DATA_ID_PROPERTY_NAME = "dataId";
        private const string LEVELS_PROPERTY_NAME = "levels";
        private const string REMOVE_LEVEL = "Are you sure you want to remove ";
        private const string BRACKET = "\"";
        private const string QUESTION_MARK = "?";
        private const string REMOVING_LEVEL_TITLE = "Removing level";
        private const string YES = "Yes";
        private const string CANCEL = "Cancel";
        private const string PATH_SEPARATOR = "/";
        private const string DEFAULT_LEVEL_LIST_HEADER = "Levels amount: ";
        private const string MULTIPLE_CHOICE_LABEL = "Multiple choice";
        private const string GLOBAL_VALIDATION_LABEL = "Global Validation";
        private const string EXPORT_LAYER_XSort_COUNTS_LABEL = "导出关卡每层棋子数";
        private const string PAGE_SIZE_LABEL = "每页显示";
        private const string PAGE_SIZE_UNIT_LABEL = "条";
        private const string PAGE_SIZE_PREFS_KEY = "LevelEditor.LevelListPageSize";
        private const int DEFAULT_PAGE_SIZE = 40;
        private const string LOAD_START_LABEL = "开始关卡号";
        private const string LOAD_END_LABEL = "结束关卡号";
        private const string LOAD_CONFIRM_LABEL = "确认";
        private const string LOAD_START_PREFS_KEY = "LevelEditor.LoadStartLevel";
        private const string LOAD_END_PREFS_KEY = "LevelEditor.LoadEndLevel";
        private const int DEFAULT_LOAD_START = 1;
        private const int DEFAULT_LOAD_END = 1000;
        private const string REMOVE_ELEMENT_CALLBACK = "Remove element";
        private const string ON_ENABLE_OVERRIDEN_ERROR = "LevelEditorBase.Instance == null. OnEnable() overriden without base.OnEnable() call";
        private const string SET_POSITION_LABEL = "Set position";
        private const string COPY_LABEL = "Copy";
        private const string COPY_NAME_LABEL = "Copy Name";
        private const string PASTE_LABEL = "Paste";
        private const string COPY_LEVEL_TO_LABEL = "Copy Level to";
        private const string INSERT_BELOW_LABEL = "Insert blank below";
        private const string INDEX_CHANGE_WINDOW = "Index change window";
        private const string COPY_LEVEL_TO_WINDOW = "Copy Level to";
        private readonly Vector2 INDEX_CHANGE_WINDOW_SIZE = new Vector2(300, 64);
        private readonly Vector2 COPY_LEVEL_TO_WINDOW_SIZE = new Vector2(380, 160);

        // Survives between menu opens; cleared only when a new Copy replaces it.
        private static readonly List<UnityEngine.Object> copiedLevelSources = new List<UnityEngine.Object>();

        #region delegates

        public delegate void AddElementCallbackDelegate();
        public delegate void RemoveElementCallbackDelegate();
        public delegate void DisplayContextMenuCallbackDelegate(GenericMenu genericMenu);
        public delegate void OnClearSelectionCallbackDelegate();
        public delegate void OnRenameAllCallbackDelegate();

        public AddElementCallbackDelegate addElementCallback;
        public RemoveElementCallbackDelegate removeElementCallback;
        public DisplayContextMenuCallbackDelegate displayContextMenuCallback;
        public OnClearSelectionCallbackDelegate onClearSelectionCallback;
        public OnRenameAllCallbackDelegate onRenameAllCallback;
        public System.Action onLoadRangeApplied;
        #endregion

        private List<string> levelLabels;
        private SerializedObject levelsDatabaseSerializedObject;
        private SerializedProperty levelsSerializedProperty;
        private CustomList customList;
        private int loadStartIndex;
        private int loadEndIndex;
        private int draftStartIndex;
        private int draftEndIndex;
        private bool loadRangeLogged;
        private int[] orderSnapshot;
        private bool persistOrderScheduled;
        private int pendingPersistFrom = -1;
        private int pendingPersistTo = -1;
        private int openedLevelInstanceId;
        private static bool s_loggedPlayOrderDrift;

        public int SelectedLevelIndex => customList.SelectedIndex;
        public int SelectedLevelCount => customList != null ? customList.SelectedCount : 0;
        public SerializedProperty SelectedLevelProperty { get => levelsSerializedProperty.GetArrayElementAtIndex(SelectedLevelIndex); set => levelsSerializedProperty.GetArrayElementAtIndex(SelectedLevelIndex).objectReferenceValue = value.objectReferenceValue; }
        public bool IgnoreDragEvents { get => customList.IgnoreDragEvents; set => customList.IgnoreDragEvents = value; }
        public CustomList CustomList => customList;

        public LevelsHandler(SerializedObject levelsDatabaseSerializedObject, SerializedProperty levelsSerializedProperty)
        {
            this.levelsDatabaseSerializedObject = levelsDatabaseSerializedObject;
            this.levelsSerializedProperty = levelsSerializedProperty;
            this.levelLabels = new List<string>();
            loadStartIndex = EditorPrefs.GetInt(LOAD_START_PREFS_KEY, DEFAULT_LOAD_START);
            loadEndIndex = EditorPrefs.GetInt(LOAD_END_PREFS_KEY, DEFAULT_LOAD_END);
            draftStartIndex = loadStartIndex;
            draftEndIndex = loadEndIndex;

            SetLevelLabels();
            SetCustomList();
            ApplyViewRangeToList();
            CaptureOrderSnapshot();
            ScheduleLayoutFileRename();
        }

        #region Reordable list

        private void SetCustomList()
        {
            //customList = new CustomList(levelsDatabaseSerializedObject, levelsSerializedProperty);
            customList = new CustomList(levelsDatabaseSerializedObject, levelsSerializedProperty,  GetLabel);

            customList.getHeaderLabelCallback += GetHeaderCallback;
            customList.selectionChangedCallback += SelectionChangedCallback;
            customList.listReorderedCallback += ListReorderedCallback;
            customList.addElementCallback += AddElementCallback;
            customList.removeElementCallback += RemoveElementCallback;
            customList.displayContextMenuCallback += DisplayContextMenuCallback;
            customList.PageSize = EditorPrefs.GetInt(PAGE_SIZE_PREFS_KEY, DEFAULT_PAGE_SIZE);
        }

        private string GetLabel(SerializedProperty elementProperty, int elementIndex)
        {
            EnsureLevelLabel(elementIndex);
            if (elementIndex < 0 || elementIndex >= levelLabels.Count)
            {
                return string.Empty;
            }

            return levelLabels[elementIndex];
        }

        public void GetLoadRange(out int start, out int end)
        {
            int size = levelsSerializedProperty != null ? levelsSerializedProperty.arraySize : 0;
            if (size <= 0)
            {
                start = 0;
                end = -1;
                return;
            }

            int startLevel = Mathf.Max(1, loadStartIndex);
            int endLevel = Mathf.Max(startLevel, loadEndIndex);
            start = Mathf.Clamp(startLevel - 1, 0, size - 1);
            end = Mathf.Clamp(endLevel - 1, start, size - 1);
        }

        public void GetLoadLevelNumbers(out int startLevel, out int endLevel)
        {
            startLevel = Mathf.Max(1, loadStartIndex);
            endLevel = Mathf.Max(startLevel, loadEndIndex);
        }

        public bool IsIndexInLoadRange(int index)
        {
            GetLoadRange(out int start, out int end);
            return index >= start && index <= end;
        }

        private void ApplyViewRangeToList()
        {
            if (customList == null)
            {
                return;
            }

            GetLoadRange(out int start, out int end);
            customList.SetViewRange(start, end);
        }

        private void EnsureLevelLabel(int index)
        {
            if (index < 0 || index >= levelLabels.Count)
            {
                return;
            }

            if (levelLabels[index] != null)
            {
                return;
            }

            if (!IsIndexInLoadRange(index) || LevelEditorBase.Instance == null)
            {
                levelLabels[index] = "#";
                return;
            }

            levelLabels[index] = LevelEditorBase.Instance.GetLevelLabel(ListedLevel(index), index);
        }

        private void DisplayContextMenuCallback()
        {
            GenericMenu genericMenu = new GenericMenu();
            int selectedCount = SelectedLevelCount;
            bool singleSelected = selectedCount == 1
                && SelectedLevelIndex >= 0
                && SelectedLevelIndex < levelsSerializedProperty.arraySize;

            if (selectedCount > 0)
            {
                string copyNameLabel = selectedCount > 1 ? $"{COPY_NAME_LABEL} ({selectedCount})" : COPY_NAME_LABEL;
                genericMenu.AddItem(new GUIContent(copyNameLabel), false, CopySelectedLevelNames);
            }
            else
            {
                genericMenu.AddDisabledItem(new GUIContent(COPY_NAME_LABEL));
            }

            if (singleSelected)
            {
                genericMenu.AddItem(new GUIContent(SET_POSITION_LABEL), false, OpenSetIndexModalWindow);
            }
            else
            {
                genericMenu.AddDisabledItem(new GUIContent(SET_POSITION_LABEL));
            }

            if (selectedCount > 0)
            {
                string removeLabel = selectedCount > 1
                    ? $"{REMOVE_ELEMENT_CALLBACK} ({selectedCount})"
                    : REMOVE_ELEMENT_CALLBACK;
                genericMenu.AddItem(new GUIContent(removeLabel), false, RemoveElementCallback);
            }
            else
            {
                genericMenu.AddDisabledItem(new GUIContent(REMOVE_ELEMENT_CALLBACK));
            }

            genericMenu.AddSeparator(string.Empty);

            if (selectedCount > 0 && HasAnySelectedLevelObject())
            {
                string copyLabel = selectedCount > 1 ? $"{COPY_LABEL} ({selectedCount})" : COPY_LABEL;
                genericMenu.AddItem(new GUIContent(copyLabel), false, CopySelectedLevels);
                string copyToLabel = selectedCount > 1 ? $"{COPY_LEVEL_TO_LABEL} ({selectedCount})" : COPY_LEVEL_TO_LABEL;
                genericMenu.AddItem(new GUIContent(copyToLabel), false, OpenCopyLevelToModalWindow);
            }
            else
            {
                genericMenu.AddDisabledItem(new GUIContent(COPY_LABEL));
                genericMenu.AddDisabledItem(new GUIContent(COPY_LEVEL_TO_LABEL));
            }

            if (HasCopiedLevels()
                && SelectedLevelIndex >= 0
                && SelectedLevelIndex < levelsSerializedProperty.arraySize)
            {
                string pasteLabel = copiedLevelSources.Count > 1
                    ? $"{PASTE_LABEL} ({copiedLevelSources.Count})"
                    : PASTE_LABEL;
                genericMenu.AddItem(new GUIContent(pasteLabel), false, PasteCopiedLevels);
            }
            else
            {
                genericMenu.AddDisabledItem(new GUIContent(PASTE_LABEL));
            }

            if (singleSelected)
            {
                genericMenu.AddItem(new GUIContent(INSERT_BELOW_LABEL), false, InsertBlankLevelBelow);
            }
            else
            {
                genericMenu.AddDisabledItem(new GUIContent(INSERT_BELOW_LABEL));
            }

            displayContextMenuCallback?.Invoke(genericMenu);
            genericMenu.ShowAsContext();
        }

        private void CopySelectedLevelNames()
        {
            if (customList == null)
            {
                return;
            }

            var builder = new StringBuilder();
            foreach (int index in customList.GetSortedSelectedIndices())
            {
                string name = AssetNameAt(index);
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(name);
            }

            EditorGUIUtility.systemCopyBuffer = builder.ToString();
        }

        private string AssetNameAt(int index)
        {
            if (levelsSerializedProperty == null || index < 0 || index >= levelsSerializedProperty.arraySize)
            {
                return null;
            }

            int instanceId = levelsSerializedProperty.GetArrayElementAtIndex(index).objectReferenceInstanceIDValue;
            string guid;
            long localId;
            if (instanceId == 0 || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(instanceId, out guid, out localId))
            {
                return null;
            }

            return Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid));
        }

        private bool HasAnySelectedLevelObject()
        {
            foreach (int index in customList.GetSortedSelectedIndices())
            {
                if (index >= 0
                    && index < levelsSerializedProperty.arraySize
                    && levelsSerializedProperty.GetArrayElementAtIndex(index).objectReferenceValue != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasCopiedLevels()
        {
            if (copiedLevelSources.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < copiedLevelSources.Count; i++)
            {
                if (copiedLevelSources[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void CopySelectedLevels()
        {
            copiedLevelSources.Clear();
            foreach (int index in customList.GetSortedSelectedIndices())
            {
                if (index < 0 || index >= levelsSerializedProperty.arraySize)
                {
                    continue;
                }

                UnityEngine.Object source = levelsSerializedProperty.GetArrayElementAtIndex(index).objectReferenceValue;
                if (source != null)
                {
                    copiedLevelSources.Add(source);
                }
            }
        }

        private void PasteCopiedLevels()
        {
            if (!HasCopiedLevels() || LevelEditorBase.Instance == null)
            {
                return;
            }

            // Strip destroyed refs
            copiedLevelSources.RemoveAll(item => item == null);
            if (copiedLevelSources.Count == 0)
            {
                return;
            }

            if (copiedLevelSources.Count == 1)
            {
                PasteSingleOverwrite(copiedLevelSources[0]);
                return;
            }

            PasteAsNewLevels(copiedLevelSources);
        }

        private void PasteSingleOverwrite(UnityEngine.Object source)
        {
            if (SelectedLevelIndex < 0 || SelectedLevelIndex >= levelsSerializedProperty.arraySize)
            {
                return;
            }

            UnityEngine.Object targetLevel = levelsSerializedProperty.GetArrayElementAtIndex(SelectedLevelIndex).objectReferenceValue;
            if (targetLevel == null || source == targetLevel)
            {
                return;
            }

            string preservedName = targetLevel.name;
            Undo.RecordObject(targetLevel, "Paste Level Data");
            EditorUtility.CopySerialized(source, targetLevel);
            targetLevel.name = preservedName;
            EditorUtility.SetDirty(targetLevel);
            AssetDatabase.SaveAssets();

            levelsDatabaseSerializedObject.Update();
            levelsDatabaseSerializedObject.ApplyModifiedProperties();
            SetLevelLabels();
            OpenLevel(SelectedLevelIndex);
        }

        private void PasteAsNewLevels(List<UnityEngine.Object> sources)
        {
            int insertIndex = SelectedLevelIndex >= 0
                ? SelectedLevelIndex + 1
                : levelsSerializedProperty.arraySize;
            insertIndex = Mathf.Clamp(insertIndex, 0, levelsSerializedProperty.arraySize);

            int expectedCount = sources.Count;
            string undoName = expectedCount > 1 ? $"Paste Levels ({expectedCount})" : "Paste Levels";
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(undoName);
            RegisterDatabaseUndo(undoName);

            int firstNewIndex = insertIndex;
            int createdCount = 0;
            for (int i = 0; i < sources.Count; i++)
            {
                UnityEngine.Object source = sources[i];
                if (source == null)
                {
                    continue;
                }

                levelsSerializedProperty.arraySize++;
                int newLevelIndex = levelsSerializedProperty.arraySize - 1;
                UnityEngine.Object level = ScriptableObject.CreateInstance(LevelEditorBase.Instance.GetLevelType());
                LevelModel sourceLevel = source as LevelModel;
                string layoutName = sourceLevel != null ? sourceLevel.GetLayoutFileName() : "0r_0c";
                AssetDatabase.CreateAsset(level, AllocateAssetPath(LevelEditorBase.Instance.LEVELS_FOLDER_PATH, layoutName));
                Undo.RegisterCreatedObjectUndo(level, undoName);
                EditorUtility.CopySerialized(source, level);
                level.name = System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(level));
                SyncDataIdToAssetName(level);
                levelsSerializedProperty.GetArrayElementAtIndex(newLevelIndex).objectReferenceValue = level;

                if (insertIndex < newLevelIndex)
                {
                    levelsSerializedProperty.MoveArrayElement(newLevelIndex, insertIndex);
                    newLevelIndex = insertIndex;
                }

                SyncDataIdToAssetName(level);
                insertIndex = newLevelIndex + 1;
                createdCount++;
            }

            levelsDatabaseSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            levelsDatabaseSerializedObject.Update();
            AssetDatabase.SaveAssets();
            SetLevelLabels();
            Undo.CollapseUndoOperations(undoGroup);

            if (firstNewIndex >= 0 && firstNewIndex < levelsSerializedProperty.arraySize && createdCount > 0)
            {
                customList.ApplySelectionRange(firstNewIndex, firstNewIndex + createdCount - 1);
                OpenLevel(firstNewIndex);
            }
        }

        private void RegisterDatabaseUndo(string name)
        {
            UnityEngine.Object db = levelsDatabaseSerializedObject != null
                ? levelsDatabaseSerializedObject.targetObject
                : null;
            if (db != null)
            {
                levelsDatabaseSerializedObject.Update();
                Undo.RegisterCompleteObjectUndo(db, name);
            }
        }

        private void RemoveElementCallback()
        {
            RemoveSelectedLevels();
            CaptureOrderSnapshot();
            LevelChunkLoader.ClearPlayOrderCache();
            removeElementCallback?.Invoke();
        }

        private void AddElementCallback()
        {
            AddLevel();
            CaptureOrderSnapshot();
            LevelChunkLoader.ClearPlayOrderCache();
            addElementCallback?.Invoke();
        }

        private void ListReorderedCallback()
        {
            int from = customList != null ? customList.LastReorderFrom : -1;
            int to = customList != null ? customList.LastReorderTo : -1;

            // Single-item reorder: permute labels/snapshot in place — no full-array scan.
            // Asset rename + play-order rewrite run on delayCall.
            if (from >= 0 && to >= 0 && from != to)
            {
                MoveListEntry(levelLabels, from, to);
                MoveOrderSnapshotEntry(from, to);
                SchedulePersistOrder(from, to);
                return;
            }

            int min = 0;
            int max = -1;
            FindReorderedSpan(out min, out max);
            RetargetLabels(min, max);
            CaptureOrderSnapshot();
            SchedulePersistOrder(-1, -1);
        }

        static void MoveListEntry(List<string> list, int from, int to)
        {
            if (list == null || from < 0 || to < 0 || from >= list.Count || to >= list.Count || from == to)
            {
                return;
            }

            string item = list[from];
            list.RemoveAt(from);
            list.Insert(to, item);
        }

        void MoveOrderSnapshotEntry(int from, int to)
        {
            if (orderSnapshot == null || from < 0 || to < 0
                || from >= orderSnapshot.Length || to >= orderSnapshot.Length || from == to)
            {
                return;
            }

            int id = orderSnapshot[from];
            if (from < to)
            {
                for (int i = from; i < to; i++)
                {
                    orderSnapshot[i] = orderSnapshot[i + 1];
                }
            }
            else
            {
                for (int i = from; i > to; i--)
                {
                    orderSnapshot[i] = orderSnapshot[i - 1];
                }
            }

            orderSnapshot[to] = id;
        }

        private void SelectionChangedCallback()
        {
            OpenLevel(SelectedLevelIndex, reopenSame: false);
        }

        private string GetHeaderCallback()
        {
            GetLoadRange(out int start, out int end);
            int visible = end >= start ? end - start + 1 : 0;
            int total = levelsSerializedProperty.arraySize;
            int selected = SelectedLevelCount;
            if (selected > 1)
            {
                return $"{DEFAULT_LEVEL_LIST_HEADER}{visible}/{total}  |  selected: {selected}";
            }

            return $"{DEFAULT_LEVEL_LIST_HEADER}{visible}/{total}";
        }

        public void ClearSelection()
        {
            customList.ClearMultiSelection();
            openedLevelInstanceId = 0;
            onClearSelectionCallback?.Invoke();
        }

        public void UpdateCurrentLevelLabel(string label)
        {
            if (SelectedLevelIndex >= 0 && SelectedLevelIndex < levelLabels.Count)
            {
                levelLabels[SelectedLevelIndex] = label;
            }
        }

        public void DisplayReordableList()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
            DrawLoadRangeFields();
            DrawLevelSearch();
            customList.Display();
            DrawPageSizeField();
            EditorGUILayout.EndVertical();
        }

        private string levelSearch = "";
        private string appliedLevelSearch;
        private int appliedSearchStart = int.MinValue;
        private int appliedSearchEnd = int.MinValue;

        private void DrawLevelSearch()
        {
            EditorGUI.BeginChangeCheck();
            string next = EditorGUILayout.TextField("搜索", levelSearch);
            if (EditorGUI.EndChangeCheck())
            {
                levelSearch = next;
                appliedLevelSearch = null;
            }

            ApplyLevelSearch();
        }

        private void ApplyLevelSearch()
        {
            if (customList == null || levelsSerializedProperty == null)
            {
                return;
            }

            GetLoadRange(out int start, out int end);
            string query = levelSearch == null ? "" : levelSearch.Trim();
            if (string.Equals(query, appliedLevelSearch, System.StringComparison.Ordinal)
                && start == appliedSearchStart
                && end == appliedSearchEnd)
            {
                return;
            }

            appliedLevelSearch = query;
            appliedSearchStart = start;
            appliedSearchEnd = end;
            if (query.Length == 0)
            {
                customList.SetDisplayedIndices(null);
                return;
            }

            var matches = new List<int>();
            int size = levelsSerializedProperty.arraySize;
            int last = Mathf.Min(end, size - 1);
            for (int i = start; i <= last; i++)
            {
                int instanceId = levelsSerializedProperty.GetArrayElementAtIndex(i).objectReferenceInstanceIDValue;
                string guid;
                long localId;
                if (instanceId == 0
                    || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(instanceId, out guid, out localId))
                {
                    continue;
                }

                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(path);
                if (!string.IsNullOrEmpty(name) && name.IndexOf(query, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    matches.Add(i);
                }
            }

            customList.SetDisplayedIndices(matches.ToArray());
        }

        private void DrawLoadRangeFields()
        {
            int total = levelsSerializedProperty != null ? levelsSerializedProperty.arraySize : 0;

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            float backupLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 0f;
            EditorGUILayout.LabelField(LOAD_START_LABEL, GUILayout.Width(68f));
            draftStartIndex = EditorGUILayout.IntField(GUIContent.none, draftStartIndex, GUILayout.Width(48f));
            EditorGUILayout.LabelField(LOAD_END_LABEL, GUILayout.Width(68f));
            draftEndIndex = EditorGUILayout.IntField(GUIContent.none, draftEndIndex, GUILayout.Width(48f));
            EditorGUIUtility.labelWidth = backupLabelWidth;
            if (GUILayout.Button(LOAD_CONFIRM_LABEL, GUILayout.Width(44f)))
            {
                ConfirmLoadRange();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GetLoadLevelNumbers(out int currentStart, out int currentEnd);
            EditorGUILayout.LabelField($"当前区间 {currentStart}-{currentEnd}  总共 {total}");
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (!loadRangeLogged)
            {
                LogLoadRange(total);
            }
        }

        private void ConfirmLoadRange()
        {
            loadStartIndex = Mathf.Max(1, draftStartIndex);
            loadEndIndex = Mathf.Max(loadStartIndex, draftEndIndex);
            draftStartIndex = loadStartIndex;
            draftEndIndex = loadEndIndex;
            EditorPrefs.SetInt(LOAD_START_PREFS_KEY, loadStartIndex);
            EditorPrefs.SetInt(LOAD_END_PREFS_KEY, loadEndIndex);
            ApplyLoadRange();
        }

        private void ApplyLoadRange()
        {
            SetLevelLabels();
            ApplyViewRangeToList();
            if (SelectedLevelIndex >= 0 && !IsIndexInLoadRange(SelectedLevelIndex))
            {
                ClearSelection();
            }

            int total = levelsSerializedProperty != null ? levelsSerializedProperty.arraySize : 0;
            LogLoadRange(total);
            GUI.changed = true;
            onLoadRangeApplied?.Invoke();
        }

        private void LogLoadRange(int total)
        {
            GetLoadLevelNumbers(out int startLevel, out int endLevel);
            Debug.Log($"[LevelEditor] load range {startLevel}-{endLevel}, total={total}");
            loadRangeLogged = true;
        }

        private void DrawPageSizeField()
        {
            if (customList == null)
            {
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(PAGE_SIZE_LABEL, GUILayout.Width(56f));
            EditorGUI.BeginChangeCheck();
            int pageSize = EditorGUILayout.DelayedIntField(customList.PageSize, GUILayout.Width(48f));
            if (EditorGUI.EndChangeCheck())
            {
                customList.PageSize = pageSize;
                EditorPrefs.SetInt(PAGE_SIZE_PREFS_KEY, customList.PageSize);
                GUI.changed = true;
            }

            EditorGUILayout.LabelField(PAGE_SIZE_UNIT_LABEL, GUILayout.Width(20f));
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        public void OpenLevel(int index)
        {
            OpenLevel(index, reopenSame: true);
        }

        private void OpenLevel(int index, bool reopenSame)
        {
            if (LevelEditorBase.Instance == null)
            {
                Debug.LogError(ON_ENABLE_OVERRIDEN_ERROR);
                return;
            }

            if (index < 0 || index >= levelsSerializedProperty.arraySize)
            {
                return;
            }

            // Prefer instance id from the array slot — avoids ListedLevel/play-order work when skipping reopen.
            int slotInstanceId = GetElementInstanceId(levelsSerializedProperty, index);
            if (!reopenSame && slotInstanceId != 0 && slotInstanceId == openedLevelInstanceId)
            {
                return;
            }

            UnityEngine.Object levelObject = ListedLevel(index);
            int instanceId = levelObject != null ? levelObject.GetInstanceID() : slotInstanceId;
            if (!reopenSame && instanceId != 0 && instanceId == openedLevelInstanceId)
            {
                return;
            }

            openedLevelInstanceId = instanceId;
            if (levelObject == null)
            {
                Debug.LogError($"Level Editor: level at index {index} is Missing. Restore .meta GUIDs or reassign in Level Database.");
            }
            else
            {
                SyncDataIdToAssetName(levelObject);
            }

            LevelEditorBase.Instance.OpenLevel(levelObject, index);
        }

        public void ReopenLevel()
        {
            OpenLevel(SelectedLevelIndex, reopenSame: true);
        }

        public string SelectedAssetName()
        {
            LevelModel level = SelectedLevelModel();
            return level != null ? level.name : "";
        }

        public bool RenameSelectedLevelToLayout()
        {
            LevelModel level = SelectedLevelModel();
            return level != null && MoveSelectedLevelAsset(level, level.GetLayoutFileName(), allowSimilar: true);
        }

        public bool RenameSelectedLevel(string rawName)
        {
            LevelModel level = SelectedLevelModel();
            if (level == null)
            {
                return false;
            }

            string name = rawName == null ? "" : rawName.Trim();
            if (name.EndsWith(".asset", System.StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(0, name.Length - ".asset".Length).Trim();
            }

            if (name.Length == 0 || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                EditorUtility.DisplayDialog("自定义命名", "这个名字不能当文件名。", "确定");
                return false;
            }

            return MoveSelectedLevelAsset(level, name, allowSimilar: false);
        }

        LevelModel SelectedLevelModel()
        {
            if (levelsSerializedProperty == null || SelectedLevelIndex < 0 || SelectedLevelIndex >= levelsSerializedProperty.arraySize)
            {
                return null;
            }

            return levelsSerializedProperty.GetArrayElementAtIndex(SelectedLevelIndex).objectReferenceValue as LevelModel;
        }

        bool MoveSelectedLevelAsset(LevelModel level, string desired, bool allowSimilar)
        {
            if (string.IsNullOrEmpty(desired))
            {
                return false;
            }

            EditorUtility.SetDirty(level);
            AssetDatabase.SaveAssets();
            string assetPath = AssetDatabase.GetAssetPath(level).Replace('\\', '/');
            string current = Path.GetFileNameWithoutExtension(assetPath);
            if (allowSimilar ? IsLayoutFileName(current, desired) : string.Equals(current, desired, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string folder = Path.GetDirectoryName(assetPath);
            if (string.IsNullOrEmpty(folder))
            {
                return false;
            }

            folder = folder.Replace('\\', '/');
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string finalName = desired;
            string destAsset = folder + "/" + finalName + ASSET_SUFFIX;
            if (File.Exists(Path.Combine(projectRoot, destAsset))
                && !string.Equals(assetPath, destAsset, System.StringComparison.OrdinalIgnoreCase))
            {
                if (!allowSimilar)
                {
                    EditorUtility.DisplayDialog("自定义命名", "这个名字已经有了。", "确定");
                    return false;
                }

                finalName = NextSimilarName(folder, desired, assetPath, projectRoot);
                if (string.IsNullOrEmpty(finalName))
                {
                    EditorUtility.DisplayDialog("刷新命名", "这个布局名已经被占满了。", "确定");
                    return false;
                }

                destAsset = folder + "/" + finalName + ASSET_SUFFIX;
            }

            string fullSrc = Path.Combine(projectRoot, assetPath);
            string fullDest = Path.Combine(projectRoot, destAsset);
            LevelAssetNameSyncPostprocessor.BeginSuppress();
            try
            {
                PatchDataIdOnDisk(fullSrc, finalName);
                if (!string.Equals(assetPath, destAsset, System.StringComparison.OrdinalIgnoreCase))
                {
                    File.Move(fullSrc, fullDest);
                    string srcMeta = fullSrc + ".meta";
                    if (File.Exists(srcMeta))
                    {
                        File.Move(srcMeta, fullDest + ".meta");
                    }
                }
            }
            finally
            {
                LevelAssetNameSyncPostprocessor.EndSuppress();
            }

            AssetDatabase.Refresh();
            if (levelsDatabaseSerializedObject != null)
            {
                levelsDatabaseSerializedObject.Update();
            }

            LevelPlayOrderSync.TryRewrite(LevelPlatformResolver.GetCurrentPlatform());
            SetLevelLabels();
            appliedLevelSearch = null;
            ApplyLevelSearch();
            ReopenLevel();
            return true;
        }

        // 进关名单是文件名。GUID 若被换到别的 .meta 上，列表仍打开名单里的那一关。
        UnityEngine.Object ListedLevel(int index)
        {
            LevelModel referenced = levelsSerializedProperty.GetArrayElementAtIndex(index).objectReferenceValue as LevelModel;
            LevelModel listed = LevelListSlot.Resolve(referenced, index);
            if (listed != null && listed != referenced && !s_loggedPlayOrderDrift)
            {
                s_loggedPlayOrderDrift = true;
                Debug.LogError(
                    "[LevelEditor] 关卡 GUID 指向的文件和进关名单不一致，左侧列表改按名单打开。不要保存 Level Database。");
            }

            return listed != null ? listed : referenced;
        }


        private void InsertBlankLevelBelow()
        {
            if (SelectedLevelIndex < 0 || SelectedLevelIndex >= levelsSerializedProperty.arraySize)
            {
                return;
            }

            AddLevelAt(SelectedLevelIndex + 1);
        }

        public void AddLevel()
        {
            AddLevelAt(levelsSerializedProperty.arraySize);
        }

        private void AddLevelAt(int insertIndex)
        {
            if (LevelEditorBase.Instance == null)
            {
                Debug.LogError(ON_ENABLE_OVERRIDEN_ERROR);
                return;
            }

            insertIndex = Mathf.Clamp(insertIndex, 0, levelsSerializedProperty.arraySize);

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Add Level");
            RegisterDatabaseUndo("Add Level");

            levelsSerializedProperty.arraySize++;
            int newLevelIndex = levelsSerializedProperty.arraySize - 1;
            UnityEngine.Object level = ScriptableObject.CreateInstance(LevelEditorBase.Instance.GetLevelType());

            AssetDatabase.CreateAsset(level, AllocateAssetPath(LevelEditorBase.Instance.LEVELS_FOLDER_PATH, "0r_0c"));
            Undo.RegisterCreatedObjectUndo(level, "Add Level");
            LevelEditorBase.Instance.ClearLevel(level);
            SyncDataIdToAssetName(level);
            levelsSerializedProperty.GetArrayElementAtIndex(newLevelIndex).objectReferenceValue = level;

            if (insertIndex < newLevelIndex)
            {
                levelsSerializedProperty.MoveArrayElement(newLevelIndex, insertIndex);
                newLevelIndex = insertIndex;
            }

            // Keep dataId aligned with file name after possible reorder.
            SyncDataIdToAssetName(level);

            levelsDatabaseSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            levelsDatabaseSerializedObject.Update();
            AssetDatabase.SaveAssets();
            SetLevelLabels();
            Undo.CollapseUndoOperations(undoGroup);

            customList.SetSingleSelection(newLevelIndex, invokeCallback: false);
            OpenLevel(newLevelIndex);
            CaptureOrderSnapshot();
        }



        private static string AllocateAssetPath(string folder, string baseName)
        {
            string projectPath = LevelEditorBase.GetProjectPath();
            string path = folder + PATH_SEPARATOR + baseName + ASSET_SUFFIX;
            int suffix = 1;
            while (File.Exists(projectPath + path))
            {
                path = folder + PATH_SEPARATOR + baseName + "_" + suffix + ASSET_SUFFIX;
                suffix++;
            }

            return path;
        }

        private const string LAYOUT_RENAME_PREFS_KEY = "LevelEditor.LayoutFileNames.v3";
        private const string RENAME_ALL_LABEL = "全部重命名";
        private static bool layoutRenameScheduled;
        private bool forceLayoutRename;

        void ScheduleLayoutFileRename()
        {
            if (layoutRenameScheduled || EditorPrefs.GetBool(LAYOUT_RENAME_PREFS_KEY, false))
            {
                return;
            }

            layoutRenameScheduled = true;
            EditorApplication.delayCall += RenameAssetsToLayoutFileNames;
        }

        public void DrawRenameAllButton()
        {
            if (!GUILayout.Button(RENAME_ALL_LABEL, EditorCustomStyles.button))
            {
                return;
            }

            if (!EditorUtility.DisplayDialog(RENAME_ALL_LABEL, "按当前布局名规则重命名全部关卡？已经是这个名字的会跳过。", "重命名", "取消"))
            {
                return;
            }

            forceLayoutRename = true;
            try
            {
                RenameAssetsToLayoutFileNames();
            }
            finally
            {
                forceLayoutRename = false;
            }
        }

        void RenameAssetsToLayoutFileNames()
        {
            layoutRenameScheduled = false;
            if (levelsSerializedProperty == null || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (!forceLayoutRename && EditorPrefs.GetBool(LAYOUT_RENAME_PREFS_KEY, false))
            {
                return;
            }

            int count = levelsSerializedProperty.arraySize;
            var jobs = new List<PendingLayoutRename>();
            try
            {
                for (int i = 0; i < count; i++)
                {
                    LevelModel level = levelsSerializedProperty.GetArrayElementAtIndex(i).objectReferenceValue as LevelModel;
                    if (level == null)
                    {
                        continue;
                    }

                    string path = AssetDatabase.GetAssetPath(level);
                    string current = Path.GetFileNameWithoutExtension(path);
                    string desired = level.GetLayoutFileName();
                    if (IsLayoutFileName(current, desired))
                    {
                        continue;
                    }

                    jobs.Add(new PendingLayoutRename
                    {
                        Path = path,
                        FinalName = desired,
                    });
                }

                if (jobs.Count == 0)
                {
                    EditorPrefs.SetBool(LAYOUT_RENAME_PREFS_KEY, true);
                    return;
                }

                // Move .asset + .meta on disk, then one Refresh. No per-file RenameAsset import.
                LevelAssetNameSyncPostprocessor.BeginSuppress();
                bool failed = false;
                int moved = 0;
                try
                {
                    string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                    for (int i = 0; i < jobs.Count; i++)
                    {
                        string assetPath = jobs[i].Path.Replace('\\', '/');
                        string finalName = jobs[i].FinalName;
                        string folder = Path.GetDirectoryName(assetPath);
                        if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(finalName))
                        {
                            failed = true;
                            continue;
                        }

                        folder = folder.Replace('\\', '/');
                        string destAsset = folder + "/" + finalName + ASSET_SUFFIX;
                        if (string.Equals(assetPath, destAsset, System.StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        string fullSrc = Path.Combine(projectRoot, assetPath);
                        string fullDest = Path.Combine(projectRoot, destAsset);
                        if (File.Exists(fullDest))
                        {
                            finalName = NextSimilarName(folder, finalName, assetPath, projectRoot);
                            if (string.IsNullOrEmpty(finalName))
                            {
                                Debug.LogError($"Layout rename skipped, target already exists: {destAsset}");
                                failed = true;
                                continue;
                            }

                            destAsset = folder + "/" + finalName + ASSET_SUFFIX;
                            fullDest = Path.Combine(projectRoot, destAsset);
                            if (string.Equals(assetPath, destAsset, System.StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }
                        }

                        PatchDataIdOnDisk(fullSrc, finalName);
                        File.Move(fullSrc, fullDest);
                        string srcMeta = fullSrc + ".meta";
                        if (File.Exists(srcMeta))
                        {
                            File.Move(srcMeta, fullDest + ".meta");
                        }

                        moved++;
                    }
                }
                finally
                {
                    LevelAssetNameSyncPostprocessor.EndSuppress();
                }

                if (moved > 0)
                {
                    AssetDatabase.Refresh();
                }

                LevelPlayOrderSync.TryRewrite(LevelPlatformResolver.GetCurrentPlatform());
                SetLevelLabels();
                appliedLevelSearch = null;
                ApplyLevelSearch();
                if (!failed)
                {
                    EditorPrefs.SetBool(LAYOUT_RENAME_PREFS_KEY, true);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        struct PendingLayoutRename
        {
            public string Path;
            public string FinalName;
        }

        static bool IsLayoutFileName(string current, string desired)
        {
            if (string.Equals(current, desired, System.StringComparison.Ordinal))
            {
                return true;
            }

            string tail = "_" + desired;
            if (current.Length <= tail.Length || !current.EndsWith(tail, System.StringComparison.Ordinal))
            {
                return false;
            }

            string head = current.Substring(0, current.Length - tail.Length);
            if (head == "similar")
            {
                return true;
            }

            const string stem = "similar";
            if (!head.StartsWith(stem, System.StringComparison.Ordinal) || head.Length == stem.Length)
            {
                return false;
            }

            for (int i = stem.Length; i < head.Length; i++)
            {
                if (head[i] < '0' || head[i] > '9')
                {
                    return false;
                }
            }

            return true;
        }

        static string NextSimilarName(string folder, string desired, string assetPath, string projectRoot)
        {
            for (int k = 1; k < 100; k++)
            {
                string name = k == 1 ? "similar_" + desired : "similar" + k + "_" + desired;
                string destRel = folder + "/" + name + ASSET_SUFFIX;
                if (string.Equals(destRel, assetPath, System.StringComparison.OrdinalIgnoreCase)
                    || !File.Exists(Path.Combine(projectRoot, destRel)))
                {
                    return name;
                }
            }

            return null;
        }

        static void PatchDataIdOnDisk(string fullAssetPath, string dataId)
        {
            if (!File.Exists(fullAssetPath))
            {
                return;
            }

            string text = File.ReadAllText(fullAssetPath);
            string updated = ReplaceYamlValue(text, "\n  m_Name: ", dataId, last: false);
            updated = ReplaceYamlValue(updated, "\n  dataId: ", dataId, last: true);
            if (!string.Equals(updated, text, System.StringComparison.Ordinal))
            {
                File.WriteAllText(fullAssetPath, updated);
            }
        }

        static string ReplaceYamlValue(string text, string key, string value, bool last)
        {
            int keyAt = last ? text.LastIndexOf(key, System.StringComparison.Ordinal) : text.IndexOf(key, System.StringComparison.Ordinal);
            if (keyAt < 0)
            {
                return text;
            }

            int valueStart = keyAt + key.Length;
            int lineEnd = text.IndexOf('\n', valueStart);
            if (lineEnd < 0)
            {
                lineEnd = text.Length;
            }

            string current = text.Substring(valueStart, lineEnd - valueStart).Trim();
            if (string.Equals(current, value, System.StringComparison.Ordinal))
            {
                return text;
            }

            return text.Substring(0, valueStart) + value + text.Substring(lineEnd);
        }

        private void OpenCopyLevelToModalWindow()
        {
            List<UnityEngine.Object> sources = new List<UnityEngine.Object>();
            List<int> oneBasedIndices = new List<int>();

            foreach (int index in customList.GetSortedSelectedIndices())
            {
                if (index < 0 || index >= levelsSerializedProperty.arraySize)
                {
                    continue;
                }

                UnityEngine.Object source = levelsSerializedProperty.GetArrayElementAtIndex(index).objectReferenceValue;
                if (source == null)
                {
                    continue;
                }

                sources.Add(source);
                oneBasedIndices.Add(index + 1);
            }

            if (sources.Count == 0)
            {
                return;
            }

            CopyLevelToModalWindow window = ScriptableObject.CreateInstance<CopyLevelToModalWindow>();
            window.SetData(sources, oneBasedIndices, this);
            window.minSize = COPY_LEVEL_TO_WINDOW_SIZE;
            window.maxSize = COPY_LEVEL_TO_WINDOW_SIZE;
            window.titleContent = new GUIContent(COPY_LEVEL_TO_WINDOW);
            window.ShowModal();
        }

        /// <param name="insertAfterOneBased">1-based list position to insert after; ignored when appendToEnd.</param>
        public void CopyLevelsToPlatform(
            List<UnityEngine.Object> sources,
            LevelPlatform targetPlatform,
            int insertAfterOneBased,
            bool appendToEnd)
        {
            if (sources == null || sources.Count == 0)
            {
                return;
            }

            List<UnityEngine.Object> validSources = new List<UnityEngine.Object>(sources.Count);
            for (int i = 0; i < sources.Count; i++)
            {
                if (sources[i] != null)
                {
                    validSources.Add(sources[i]);
                }
            }

            if (validSources.Count == 0)
            {
                return;
            }

            LevelCatalog targetDatabase = LevelPlatformResolver.LoadDatabaseInEditor(targetPlatform);
            if (targetDatabase == null)
            {
                EditorUtility.DisplayDialog(
                    COPY_LEVEL_TO_WINDOW,
                    $"Level Database not found for {LevelPlatformResolver.GetDisplayName(targetPlatform)}.",
                    "OK");
                return;
            }

            LevelPlatform currentPlatform = LevelPlatformResolver.GetCurrentPlatform();
            bool isCurrentPlatform = targetPlatform == currentPlatform
                && levelsDatabaseSerializedObject != null
                && levelsDatabaseSerializedObject.targetObject == targetDatabase;

            SerializedObject targetSerializedObject;
            SerializedProperty targetLevelsProperty;
            if (isCurrentPlatform)
            {
                targetSerializedObject = levelsDatabaseSerializedObject;
                targetLevelsProperty = levelsSerializedProperty;
                targetSerializedObject.Update();
            }
            else
            {
                targetSerializedObject = new SerializedObject(targetDatabase);
                targetLevelsProperty = targetSerializedObject.FindProperty(LEVELS_PROPERTY_NAME);
                if (targetLevelsProperty == null || !targetLevelsProperty.isArray)
                {
                    EditorUtility.DisplayDialog(COPY_LEVEL_TO_WINDOW, "Target Level Database.levels property not found.", "OK");
                    return;
                }
            }

            int insertIndex = appendToEnd
                ? targetLevelsProperty.arraySize
                : Mathf.Clamp(insertAfterOneBased, 0, targetLevelsProperty.arraySize);

            string folder = LevelPlatformResolver.GetLevelsFolder(targetPlatform);
            LevelEditorBase.CreateFolderIfNotExist(folder);

            string undoName = validSources.Count > 1
                ? $"Copy Levels to ({validSources.Count})"
                : "Copy Level to";
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(undoName);

            if (isCurrentPlatform)
            {
                RegisterDatabaseUndo(undoName);
            }
            else
            {
                Undo.RegisterCompleteObjectUndo(targetDatabase, undoName);
            }

            System.Type levelType = LevelEditorBase.Instance != null
                ? LevelEditorBase.Instance.GetLevelType()
                : typeof(LevelModel);

            int firstNewIndex = insertIndex;
            int createdCount = 0;

            for (int i = 0; i < validSources.Count; i++)
            {
                UnityEngine.Object source = validSources[i];
                targetLevelsProperty.arraySize++;
                int newLevelIndex = targetLevelsProperty.arraySize - 1;

                LevelModel sourceLevel = source as LevelModel;
                string layoutName = sourceLevel != null ? sourceLevel.GetLayoutFileName() : "0r_0c";
                string assetPath = AllocateAssetPath(folder, layoutName);
                UnityEngine.Object level = ScriptableObject.CreateInstance(levelType);
                AssetDatabase.CreateAsset(level, assetPath);
                Undo.RegisterCreatedObjectUndo(level, undoName);
                EditorUtility.CopySerialized(source, level);
                level.name = Path.GetFileNameWithoutExtension(assetPath);
                SyncDataIdToAssetName(level);
                targetLevelsProperty.GetArrayElementAtIndex(newLevelIndex).objectReferenceValue = level;

                if (insertIndex < newLevelIndex)
                {
                    targetLevelsProperty.MoveArrayElement(newLevelIndex, insertIndex);
                    newLevelIndex = insertIndex;
                }

                SyncDataIdToAssetName(level);
                insertIndex = newLevelIndex + 1;
                createdCount++;
            }

            targetSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            targetSerializedObject.Update();
            AssetDatabase.SaveAssets();
            Undo.CollapseUndoOperations(undoGroup);

            if (createdCount <= 0)
            {
                return;
            }

            if (isCurrentPlatform)
            {
                SetLevelLabels();
                customList.ApplySelectionRange(firstNewIndex, firstNewIndex + createdCount - 1);
                OpenLevel(firstNewIndex);
            }
            else
            {
                EditorUtility.DisplayDialog(
                    COPY_LEVEL_TO_WINDOW,
                    $"Copied {createdCount} level(s) to {LevelPlatformResolver.GetDisplayName(targetPlatform)}.",
                    "OK");
            }
        }

        public void DeleteLevel(int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= levelsSerializedProperty.arraySize)
            {
                return;
            }

            customList.SetSingleSelection(levelIndex, invokeCallback: false);
            RemoveSelectedLevels();
        }

        public void RemoveSelectedLevels()
        {
            List<int> indices = customList.GetSortedSelectedIndices();
            if (indices.Count == 0)
            {
                return;
            }

            StringBuilder stringBuilder = LevelEditorBase.Instance.stringBuilder;
            stringBuilder.Clear();
            if (indices.Count == 1)
            {
                int levelIndex = indices[0];
                stringBuilder.Append(REMOVE_LEVEL);
                stringBuilder.Append(BRACKET);
                stringBuilder.Append(levelIndex >= 0 && levelIndex < levelLabels.Count ? levelLabels[levelIndex] : "#");
                stringBuilder.Append(BRACKET);
                stringBuilder.Append(QUESTION_MARK);
            }
            else
            {
                stringBuilder.Append($"Are you sure you want to remove {indices.Count} selected levels?");
            }

            if (!EditorUtility.DisplayDialog(REMOVING_LEVEL_TITLE, stringBuilder.ToString(), YES, CANCEL))
            {
                return;
            }

            int undoGroup = Undo.GetCurrentGroup();
            string undoName = indices.Count > 1 ? $"Remove Levels ({indices.Count})" : "Remove Level";
            Undo.SetCurrentGroupName(undoName);
            RegisterDatabaseUndo(undoName);

            for (int i = indices.Count - 1; i >= 0; i--)
            {
                HandleDeleteLevel(indices[i], clearSelection: false);
            }

            customList.ClearMultiSelection();
            levelsDatabaseSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            levelsDatabaseSerializedObject.Update();
            AssetDatabase.SaveAssets();
            SetLevelLabels();
            Undo.CollapseUndoOperations(undoGroup);
            onClearSelectionCallback?.Invoke();
        }

        private void HandleDeleteLevel(int levelIndex, bool clearSelection = true)
        {
            if (levelIndex < 0 || levelIndex >= levelsSerializedProperty.arraySize)
            {
                return;
            }

            UnityEngine.Object tempObject = levelsSerializedProperty.GetArrayElementAtIndex(levelIndex).objectReferenceValue;

            if (clearSelection && customList != null)
            {
                customList.ClearMultiSelection();
            }

            if (levelIndex < levelLabels.Count)
            {
                levelLabels.RemoveAt(levelIndex);
            }

            levelsSerializedProperty.GetArrayElementAtIndex(levelIndex).objectReferenceValue = null;
            levelsSerializedProperty.DeleteArrayElementAtIndex(levelIndex);

            if (tempObject != null)
            {
                Undo.DestroyObjectImmediate(tempObject);
            }
        }

        /// <summary>
        /// Rebuild list UI after Unity Undo/Redo restored the database / level assets.
        /// </summary>
        public void RefreshAfterUndoRedo()
        {
            if (levelsDatabaseSerializedObject == null || levelsSerializedProperty == null)
            {
                return;
            }

            levelsDatabaseSerializedObject.Update();
            SetLevelLabels();

            int size = levelsSerializedProperty.arraySize;
            int selected = customList != null ? customList.SelectedIndex : -1;
            if (selected < 0 || selected >= size)
            {
                ClearSelection();
                return;
            }

            // Clamp multi-selection; keep the active index if the asset is still valid.
            customList.SetSingleSelection(selected, invokeCallback: false);

            UnityEngine.Object levelObject = levelsSerializedProperty.GetArrayElementAtIndex(selected).objectReferenceValue;
            if (levelObject == null)
            {
                ClearSelection();
                return;
            }

            OpenLevel(selected);
        }

        public void SetLevelLabels()
        {
            levelLabels.Clear();

            int size = levelsSerializedProperty != null ? levelsSerializedProperty.arraySize : 0;
            for (int i = 0; i < size; i++)
            {
                levelLabels.Add(null);
            }

            if (LevelEditorBase.Instance == null)
            {
                Debug.LogError(ON_ENABLE_OVERRIDEN_ERROR);
            }
        }

        static int GetElementInstanceId(SerializedProperty levels, int index)
        {
            // 不碰 objectReferenceValue，否则会把列表里每个关卡都加载进内存。
            return levels.GetArrayElementAtIndex(index).objectReferenceInstanceIDValue;
        }

        void CaptureOrderSnapshot()
        {
            if (levelsSerializedProperty == null)
            {
                orderSnapshot = null;
                return;
            }

            int count = levelsSerializedProperty.arraySize;
            orderSnapshot = new int[count];
            for (int i = 0; i < count; i++)
            {
                orderSnapshot[i] = GetElementInstanceId(levelsSerializedProperty, i);
            }
        }

        void FindReorderedSpan(out int min, out int max)
        {
            min = 0;
            max = -1;
            if (levelsSerializedProperty == null)
            {
                return;
            }

            int count = levelsSerializedProperty.arraySize;
            if (orderSnapshot == null || orderSnapshot.Length != count)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                int id = GetElementInstanceId(levelsSerializedProperty, i);
                if (id == orderSnapshot[i])
                {
                    continue;
                }

                if (max < 0)
                {
                    min = i;
                }

                max = i;
            }
        }

        void RetargetLabels(int start, int end)
        {
            if (levelLabels == null || orderSnapshot == null || levelsSerializedProperty == null || end < start)
            {
                return;
            }

            Dictionary<int, string> idToLabel = new Dictionary<int, string>();
            int spanEnd = Mathf.Min(end, orderSnapshot.Length - 1);
            for (int i = start; i <= spanEnd; i++)
            {
                if (i >= 0 && i < levelLabels.Count)
                {
                    idToLabel[orderSnapshot[i]] = levelLabels[i];
                }
            }

            int labelEnd = Mathf.Min(end, levelLabels.Count - 1);
            for (int i = start; i <= labelEnd; i++)
            {
                int id = GetElementInstanceId(levelsSerializedProperty, i);
                string previous;
                if (!idToLabel.TryGetValue(id, out previous) || string.IsNullOrEmpty(previous))
                {
                    levelLabels[i] = null;
                    continue;
                }

                // Keep the original label string; asset names are not rewritten on reorder.
                levelLabels[i] = previous;
            }
        }

        void SchedulePersistOrder(int from, int to)
        {
            if (from >= 0 && to >= 0 && from != to)
            {
                if (persistOrderScheduled && pendingPersistFrom >= 0)
                {
                    pendingPersistFrom = -1;
                    pendingPersistTo = -1;
                }
                else if (!persistOrderScheduled)
                {
                    pendingPersistFrom = from;
                    pendingPersistTo = to;
                }
            }
            else
            {
                pendingPersistFrom = -1;
                pendingPersistTo = -1;
            }

            if (persistOrderScheduled)
            {
                return;
            }

            persistOrderScheduled = true;
            EditorApplication.delayCall += PersistOrderAfterReorder;
        }

        void PersistOrderAfterReorder()
        {
            persistOrderScheduled = false;
            if (levelsDatabaseSerializedObject == null)
            {
                return;
            }

            int from = pendingPersistFrom;
            int to = pendingPersistTo;
            pendingPersistFrom = -1;
            pendingPersistTo = -1;

            LevelPlatform platform = LevelPlatformResolver.GetCurrentPlatform();
            if (from >= 0 && to >= 0 && from != to
                && LevelPlayOrderSync.TryRewriteByMovingSlot(platform, from, to))
            {
                onRenameAllCallback?.Invoke();
                return;
            }

            levelsDatabaseSerializedObject.Update();
            LevelPlayOrderSync.TryRewrite(platform);
            onRenameAllCallback?.Invoke();
        }

        private static void SetDataIdValue(UnityEngine.Object levelObject, string dataId)
        {
            if (levelObject == null)
            {
                return;
            }

            SerializedObject serializedLevel = new SerializedObject(levelObject);
            SerializedProperty dataIdProperty = serializedLevel.FindProperty(DATA_ID_PROPERTY_NAME);
            if (dataIdProperty == null)
            {
                return;
            }

            if (string.Equals(dataIdProperty.stringValue, dataId, System.StringComparison.Ordinal))
            {
                return;
            }

            dataIdProperty.stringValue = dataId;
            serializedLevel.ApplyModifiedProperties();
            EditorUtility.SetDirty(levelObject);
        }

        /// <summary>Force dataId to match the LevelModel asset file name.</summary>
        public static void SyncDataIdToAssetName(UnityEngine.Object levelObject)
        {
            if (levelObject == null)
            {
                return;
            }

            SetDataIdValue(levelObject, levelObject.name);
        }

        #region draw buttons

        public void DrawMultipleChoiceButton()
        {
            bool enabled = customList != null && customList.CheckboxSelectionMode;
            bool newEnabled = GUILayout.Toggle(enabled, MULTIPLE_CHOICE_LABEL, EditorCustomStyles.button);
            if (newEnabled == enabled || customList == null)
            {
                return;
            }

            customList.CheckboxSelectionMode = newEnabled;
            if (!newEnabled)
            {
                int active = customList.SelectedIndex;
                if (active >= 0 && active < levelsSerializedProperty.arraySize)
                {
                    customList.SetSingleSelection(active, invokeCallback: false);
                }
                else
                {
                    customList.ClearMultiSelection();
                    onClearSelectionCallback?.Invoke();
                }
            }
        }

        public void DrawGlobalValidationButton()
        {
            if (GUILayout.Button(GLOBAL_VALIDATION_LABEL, EditorCustomStyles.button))
            {
                GetLoadRange(out int start, out int end);
                GetLoadLevelNumbers(out int startLevel, out int endLevel);
                Debug.Log($"Global validation log begins, range {startLevel}-{endLevel}");

                List<int> failedLevelNumbers = new List<int>();
                List<int> XSortsExceedLevelNumbers = new List<int>();
                List<int> XSortsExceedXLevelNumbers = new List<int>();
                List<int> XSortsExceedYLevelNumbers = new List<int>();
                List<int> XSortsUnderfillLevelNumbers = new List<int>();
                for (int i = start; i <= end; i++)
                {
                    bool passed = LevelEditorBase.Instance.LogErrorsForGlobalValidation(
                        ListedLevel(i),
                        i,
                        out bool XSortsExceedField,
                        out bool XSortsExceedFieldX,
                        out bool XSortsExceedFieldY,
                        out bool XSortsUnderfillField);
                    int levelNumber = i + 1;
                    if (!passed)
                    {
                        failedLevelNumbers.Add(levelNumber);
                    }

                    if (XSortsExceedField)
                    {
                        XSortsExceedLevelNumbers.Add(levelNumber);
                    }

                    if (XSortsExceedFieldX)
                    {
                        XSortsExceedXLevelNumbers.Add(levelNumber);
                    }

                    if (XSortsExceedFieldY)
                    {
                        XSortsExceedYLevelNumbers.Add(levelNumber);
                    }

                    if (XSortsUnderfillField)
                    {
                        XSortsUnderfillLevelNumbers.Add(levelNumber);
                    }
                }

                if (XSortsExceedXLevelNumbers.Count > 0)
                {
                    Debug.LogError(
                        "Levels exceeding LevelXSortsField on X（原因：棋子包围盒宽度超出可用区域）: "
                        + string.Join(", ", XSortsExceedXLevelNumbers));
                }
                else
                {
                    Debug.Log("No levels exceeding LevelXSortsField on X.");
                }

                if (XSortsExceedYLevelNumbers.Count > 0)
                {
                    Debug.LogError(
                        "Levels exceeding LevelXSortsField on Y（原因：棋子包围盒高度超出可用区域）: "
                        + string.Join(", ", XSortsExceedYLevelNumbers));
                }
                else
                {
                    Debug.Log("No levels exceeding LevelXSortsField on Y.");
                }

                if (XSortsExceedLevelNumbers.Count > 0)
                {
                    Debug.LogError(
                        "Levels exceeding LevelXSortsField（X 或 Y，原因：棋子包围盒超出 levelXSortsFieldOffset 可用区域）: "
                        + string.Join(", ", XSortsExceedLevelNumbers));
                }
                else
                {
                    Debug.Log("No levels exceeding LevelXSortsField.");
                }

                if (XSortsUnderfillLevelNumbers.Count > 0)
                {
                    Debug.LogError(
                        "Levels underfilling LevelXSortsField（原因：棋子包围盒未铺满可用区域，限制轴覆盖率 < 98%）: "
                        + string.Join(", ", XSortsUnderfillLevelNumbers));
                }
                else
                {
                    Debug.Log("No levels underfilling LevelXSortsField.");
                }

                if (failedLevelNumbers.Count > 0)
                {
                    Debug.LogError(
                        "Global validation failed levels（原因：存在任意校验错误，详见上方每条 Level #N [原因]）: "
                        + string.Join(", ", failedLevelNumbers));
                }
                else
                {
                    Debug.Log("Global validation passed: all levels in load range OK.");
                }

                Debug.Log("Global validation log ends");

                SetLevelLabels();
            }
        }

        public void DrawExportLayerXSortCountsButton()
        {
            if (GUILayout.Button(EXPORT_LAYER_XSort_COUNTS_LABEL, EditorCustomStyles.button))
            {
                GetLoadRange(out int start, out int end);
                int total = levelsSerializedProperty.arraySize;
                LevelModel[] referenced = new LevelModel[total];
                for (int i = start; i <= end && i < total; i++)
                {
                    referenced[i] = levelsSerializedProperty.GetArrayElementAtIndex(i).objectReferenceValue as LevelModel;
                }

                LevelLayerXSortCountExporter.ExportRange(referenced, start, end);
            }
        }

        #endregion

        #region Set index modal window

        private void OpenSetIndexModalWindow()
        {
            SetIndexModalWindow window = ScriptableObject.CreateInstance<SetIndexModalWindow>();
            window.SetData(SelectedLevelIndex, levelsSerializedProperty.arraySize, this);
            window.minSize = INDEX_CHANGE_WINDOW_SIZE;
            window.maxSize = INDEX_CHANGE_WINDOW_SIZE;
            window.titleContent = new GUIContent(INDEX_CHANGE_WINDOW);

            window.ShowModal();
        }

        private void ModalWindowProcessChange(int originalIndex, int newIndex)
        {
            levelsSerializedProperty.MoveArrayElement(originalIndex, newIndex);
            levelsDatabaseSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(levelsDatabaseSerializedObject.targetObject);
            customList.SelectedIndex = newIndex;
            MoveListEntry(levelLabels, originalIndex, newIndex);
            MoveOrderSnapshotEntry(originalIndex, newIndex);
            SchedulePersistOrder(originalIndex, newIndex);
        }

        private class SetIndexModalWindow : EditorWindow
        {
            private const string INT_FIELD_LABEL = "target element new #";
            private const string CANCEL_BUTTON_LABEL = "Cancel";
            private const string CHANGE_BUTTON_LABEL = "Change";
            private const string DEFAULT_LABEL = "target element #";
            public int elementOrinialIndex;
            public int arraySize;
            public LevelsHandler levelsHandler;
            private string label;
            private int newPositionNumber;

            public void SetData(int elementOrinialIndex,int arraySize, LevelsHandler levelsHandler)
            {
                this.elementOrinialIndex = elementOrinialIndex;
                this.levelsHandler = levelsHandler;
                this.arraySize = arraySize;
                label = DEFAULT_LABEL + (elementOrinialIndex + 1);
                newPositionNumber = elementOrinialIndex + 1;
            }

            void OnGUI()
            {
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(label);
                newPositionNumber = EditorGUILayout.IntField(INT_FIELD_LABEL, newPositionNumber);
                newPositionNumber = Mathf.Clamp(newPositionNumber, 1, arraySize);

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button(CANCEL_BUTTON_LABEL))
                {
                    this.Close();
                }

                if (GUILayout.Button(CHANGE_BUTTON_LABEL))
                {
                    levelsHandler.ModalWindowProcessChange(elementOrinialIndex, newPositionNumber - 1);
                    this.Close();
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }
        }

        private class CopyLevelToModalWindow : EditorWindow
        {
            private const string SELECTED_LABEL = "selected:";
            private const string TARGET_PLATFORM_LABEL = "target platform:";
            private const string POSITION_LABEL = "position:";
            private const string AFTER_BUTTON_LABEL = "After";
            private const string FINALLY_BUTTON_LABEL = "Finally";
            private const string ERROR_POSITION = "position must be a positive integer.";

            private LevelsHandler levelsHandler;
            private List<UnityEngine.Object> sources;
            private string selectedDisplay;
            private int maxSelectedOneBased;
            private LevelPlatform targetPlatform;
            private string positionText;
            private string errorLabel;

            public void SetData(List<UnityEngine.Object> sources, List<int> oneBasedIndices, LevelsHandler levelsHandler)
            {
                this.levelsHandler = levelsHandler;
                this.sources = sources != null ? new List<UnityEngine.Object>(sources) : new List<UnityEngine.Object>();
                targetPlatform = LevelPlatformResolver.GetCurrentPlatform();
                positionText = string.Empty;
                errorLabel = string.Empty;

                StringBuilder builder = new StringBuilder();
                maxSelectedOneBased = 0;
                if (oneBasedIndices != null)
                {
                    for (int i = 0; i < oneBasedIndices.Count; i++)
                    {
                        int value = oneBasedIndices[i];
                        if (value > maxSelectedOneBased)
                        {
                            maxSelectedOneBased = value;
                        }

                        if (i > 0)
                        {
                            builder.Append(',');
                        }

                        builder.Append(value);
                    }
                }

                selectedDisplay = builder.ToString();
            }

            void OnGUI()
            {
                EditorGUILayout.BeginVertical();

                EditorGUILayout.LabelField(SELECTED_LABEL, selectedDisplay);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(TARGET_PLATFORM_LABEL, GUILayout.Width(110));
                int platformIndex = targetPlatform == LevelPlatform.IOS ? 1 : 0;
                platformIndex = GUILayout.Toolbar(platformIndex, new[] { "Android", "iOS" });
                targetPlatform = platformIndex == 1 ? LevelPlatform.IOS : LevelPlatform.Android;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(POSITION_LABEL, GUILayout.Width(110));
                positionText = EditorGUILayout.TextField(positionText);
                EditorGUILayout.EndHorizontal();

                if (!string.IsNullOrEmpty(errorLabel))
                {
                    EditorGUILayout.HelpBox(errorLabel, MessageType.Error);
                }

                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button(AFTER_BUTTON_LABEL))
                {
                    if (TryResolveAfterPosition(out int insertAfterOneBased))
                    {
                        levelsHandler.CopyLevelsToPlatform(sources, targetPlatform, insertAfterOneBased, appendToEnd: false);
                        Close();
                    }
                }

                if (GUILayout.Button(FINALLY_BUTTON_LABEL))
                {
                    levelsHandler.CopyLevelsToPlatform(sources, targetPlatform, 0, appendToEnd: true);
                    Close();
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            private bool TryResolveAfterPosition(out int insertAfterOneBased)
            {
                errorLabel = string.Empty;
                if (string.IsNullOrWhiteSpace(positionText))
                {
                    insertAfterOneBased = maxSelectedOneBased;
                    return true;
                }

                if (!int.TryParse(positionText.Trim(), out insertAfterOneBased) || insertAfterOneBased < 0)
                {
                    errorLabel = ERROR_POSITION;
                    insertAfterOneBased = 0;
                    return false;
                }

                return true;
            }
        }

        #endregion
    }
}


#endif