#pragma warning disable 649
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using TileGameCore;
using UnityEditor.SceneManagement;
using FishFramework;

namespace FishTilePlay
{
    public class LevelEditorWindow : LevelEditorBase
    {
        private const string EditorPreviewRootName = "[Level editor]";

        /// <summary>
        /// HideAndDontSave preview roots survive scene loads and are Hierarchy-invisible.
        /// Sweep orphans on domain reload so leaked blank tiles do not stick in StartScene / Game view.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void CleanupOrphanEditorPreviewsOnLoad()
        {
            EditorApplication.delayCall += DestroyOrphanEditorPreviews;
        }

        private enum EditorToolMode
        {
            Draw,
            Select
        }

        //used variables
        private const string RETURN_LABEL = "← Back";
        private LevelGenService selectedLevelGenFunc;
        private LevelsHandler levelsHandler;
        private CellTypesHandler gridHandler;
        private TabHandler tabHandler;
        private string levelLabel;
        private GameSettings gameSettings;

        //level drawing
        private Rect drawRect;
        private int currentOffset;
        private float xSize;
        private float ySize;
        private float elementSize;
        private int selectedLayerWidth;
        private int selectedLayerHeight;
        private Event currentEvent;
        private Vector2 elementUnderMouseIndex;
        private Vector2Int elementPosition;
        private float buttonRectX;
        private float buttonRectY;
        private Rect buttonRect;
        private const int BOTTOM_PADDING = 8;
        private int selectedLayerIndex;
        private string[] layersOptions;
        private Color defaultColor;
        private int maxLayerHeight;
        private int maxLayerWidth;
        private float halfElementSize;
        private bool tempBoolValue;
        private float elementOffset;
        private float defaultLabelWidth;

        //Database 
        private const string LEVELS_PROPERTY_NAME = "levels";
        private const string TILES_PROPERTY_NAME = "tiles";
        private const string TILE_EFFECTS_PROPERTY = "tileEffects";
        private const string BACKGROUND_DATA_PROPERTY = "backgroundData";
        private const string SELECTED_COLOR_PROPERTY = "selectedColor";
        private const string UNSELECTED_COLOR_PROPERTY = "unselectedColor";
        private const string TINT_COLOR_PROPERTY = "tintColor";
        private const string BACKGROUND_COLOR_PROPERTY = "backgroundColor";
        private const string GRID_COLOR_PROPERTY = "gridColor";
        private SerializedProperty levelsSerializedProperty;
        private SerializedProperty tilesProperty;
        private SerializedProperty backgroundDataProperty;
        private SerializedProperty tileEffectsSerializedProperty;
        private SerializedProperty selectedColorSerializedProperty;
        private SerializedProperty unselectedColorSerializedProperty;
        private SerializedProperty tintColorSerializedProperty;
        private SerializedProperty backgroundColorSerializedProperty;
        private SerializedProperty gridColorSerializedProperty;

        //Draw levels Tab
        private const string LEVELS_TAB_NAME = "Levels";
        private const string SETTINGS_TAB_NAME = "Settings";
        private const int SIDEBAR_WIDTH = 320;
        private const int BUTTONS_WIDTH = 200;
        private const string TEST_LEVEL = "Test level";
        private const string GAME_SCENE_PATH = "Assets/CABRes/Scenes/GameScene.unity";
        private const string GAME_SCENE_NAME = "GameScene";
        private const string UI_MAIN_CANVAS_NAME = "UI Main Canvas";
        private const string UI_GAME_OBJECT_NAME = "UI Game";
        private const string POWER_UP_PANEL_NAME = "Power Up Panel";

        //PlayerPrefs
        private const string PREFS_LEVEL = "editor_level_index";
        private const string PREFS_WIDTH = "editor_sidebar_width";
        private const string PREFS_BUTTONS_WIDTH = "editor_buttons_width";

        //edit layers tab
        private static readonly string[] TOOL_MODE_LABELS = { "Draw", "Select" };
        private static readonly string[] BOX_COVER_SIZE_LABELS = { "2x2", "2x3", "3x3" };
        private static readonly string[] BOX_COVER_TYPE_LABELS = { "Coke" };
        private static readonly Color SELECTED_CELL_HIGHLIGHT_COLOR = new Color(1f, 0.92f, 0.016f, 1f);
        private static readonly Color BOX_COVER_2X2_COLOR = new Color(0.42f, 0.82f, 1f, 1f);
        private static readonly Color BOX_COVER_2X3_COLOR = new Color(0.42f, 0.82f, 1f, 1f);
        private static readonly Color BOX_COVER_3X3_COLOR = new Color(0.42f, 0.82f, 1f, 1f);
        private static readonly Color INVALID_BOX_COVER_COLOR = new Color(1f, 0.2f, 0.2f, 1f);
        private const float SELECTED_CELL_OUTLINE_THICKNESS = 2f;

        private const int INFO_HEIGH = 64; //found out using Debug.Log(infoRect) on worst case scenario
        private const float LEVEL_EDIT_SETTINGS_SCROLL_HEIGHT = 140f;
        private const string LEVEL_PASSED_VALIDATION = "Level passed validation.";
        private const string WARNING_LABEL = "Warning";
        private const string OK_LABEL = "Ok";
        private const string CANCEL_LABEL = "Cancel";
        private const string CURRENT_LAYER_REMOVE_WARNING = "Are you sure you want to remove the current layer?";

        //edit variables tab
        private Rect infoRect;
        private const string PREFAB_PROPERTY_NAME = "prefab";

        private bool validationNeededAfterDrag;
        private bool isDraggingCells;
        private int currentSideBarWidth;
        private int currentButtonsWidth;
        private EditorSceneController editorSceneController;
        private bool lastActiveLevelOpened;
        private bool isRebuildingPreview;
        private bool gameUiHiddenForEditor;
        private bool rehideGameUiAfterSceneSave;
        private bool isEnteringPlayFromTestLevel;
        private Dictionary<int, bool> cachedGameUiChildActiveStates;
        private Transform cachedMainCanvasForHide;
        private Transform cachedUiGameForHide;
        private Transform cachedPowerUpPanelForHide;
        private CellTypesHandler.ExtraProp tempExtraProp;
        private int menuIndex1;
        private int menuIndex2;
        private int menuIndex3;
        private bool preparedForMouseDrag;
        private bool[,] dragArray;
        private float layerButtonsHeight;
        private Rect separatorRect;
        private bool separatorIsDragged;
        private TintMap tintMap;
        private readonly List<TintLayerDrawCache> tintLayerDrawCache = new List<TintLayerDrawCache>(8);
        private bool buttonSeparatorIsDragged;
        private bool drawEditVariablesTabDisplayed;
        private GUIContent editColorsContent;
        private int previousSelectedLayerIndex;
        private int cachedCurrentLayerTileCount = -1;

        // Layer side list (context menu + drag reorder)
        private const float LAYER_DRAG_START_DISTANCE = 5f;
        private const float LAYER_DRAG_HANDLE_HIT_WIDTH = 20f;
        private static string copiedLayerJson;
        private bool isDraggingLayerList;
        private bool layerListDragArmed;
        private int layerDragStartIndex = -1;
        private int layerDragCurrentIndex = -1;
        private float layerDragElementY;
        private float layerDragOffsetY;
        private float layerListRowHeight = 28f;
        private Vector2 layerListMouseDownPos;
        private int layerListMouseDownIndex = -1;
        private Rect layerListContentRect;
        private GUIStyle layerDragHandleStyle;
        
        // Delta X and Y for layer positioning
        private float deltaX = 0f;
        private float deltaY = 0f;
        private bool showDeltaSettings = false;
        private bool easyHardFoldoutExpanded;

        // Cell selection tool — toolbar pins Draw/Select; holding Ctrl temporarily forces Select.
        private EditorToolMode editorToolMode = EditorToolMode.Draw;
        private bool ctrlHeldForSelect;
        private readonly HashSet<Vector2Int> selectedCells = new HashSet<Vector2Int>();
        private float selectionOffsetX;
        private float selectionOffsetY;
        private Rect cachedBoardRect;
        private bool isPointerOverBoard;
        private Vector2 levelEditSettingsScroll;
        private bool boxCoverMode;
        private BoxCoverSize selectedBoxCoverSize = BoxCoverSize.Size2x2;
        private bool boxCoverStrokePending;
        private readonly List<BoxCoverCandidateView> boxCoverCandidates = new List<BoxCoverCandidateView>(32);

        public struct BoxCoverCandidateView
        {
            public BoxCoverSize size;
            public Vector2Int anchor;

            public BoxCoverCandidateView(BoxCoverSize size, Vector2Int anchor)
            {
                this.size = size;
                this.anchor = anchor;
            }
        }

        protected override WindowConfiguration SetUpWindowConfiguration(WindowConfiguration.Builder builder)
        {
            return builder.SetWindowMinSize(new Vector2(700, 500)).Build();
        }

        protected override Type GetLevelsDatabaseType()
        {
            return typeof(LevelCatalog);
        }

        // ===== 关卡平台化：编辑器按 GameSettings.levelPlatform 定位数据库、目录与命名 =====

        protected override string LEVELS_DATABASE_FOLDER_PATH => LevelPlatformResolver.LevelSystemFolder;

        protected override string LEVELS_FOLDER_NAME => System.IO.Path.GetFileName(LevelPlatformResolver.GetLevelsFolder());

        public override string LevelAssetPrefix => LevelPlatformResolver.GetLevelAssetPrefix();

        protected override string GetLevelsDatabaseCreatePath()
        {
            return LevelPlatformResolver.GetDatabasePath();
        }

        protected override UnityEngine.Object LoadLevelsDatabase()
        {
            LevelCatalog db = LevelPlatformResolver.LoadDatabaseInEditor();
            if (db == null)
            {
                Debug.LogError(string.Format(
                    "[LevelEditor] 当前关卡平台 {0} 的数据库不存在：{1}。请先创建该平台数据库，不要静默使用另一平台的数据。",
                    LevelPlatformResolver.GetDisplayName(), LevelPlatformResolver.GetDatabasePath()));
            }
            return db;
        }

        public override Type GetLevelType()
        {
            return typeof(LevelModel);
        }

        protected override void ReadLevelDatabaseFields()
        {
            levelsSerializedProperty = levelsDatabaseSerializedObject.FindProperty(LEVELS_PROPERTY_NAME);
            tilesProperty = levelsDatabaseSerializedObject.FindProperty(TILES_PROPERTY_NAME);
            backgroundDataProperty = levelsDatabaseSerializedObject.FindProperty(BACKGROUND_DATA_PROPERTY);
            tileEffectsSerializedProperty = levelsDatabaseSerializedObject.FindProperty(TILE_EFFECTS_PROPERTY);

            selectedColorSerializedProperty = levelsDatabaseSerializedObject.FindProperty(SELECTED_COLOR_PROPERTY);
            unselectedColorSerializedProperty = levelsDatabaseSerializedObject.FindProperty(UNSELECTED_COLOR_PROPERTY);
            tintColorSerializedProperty = levelsDatabaseSerializedObject.FindProperty(TINT_COLOR_PROPERTY);
            backgroundColorSerializedProperty = levelsDatabaseSerializedObject.FindProperty(BACKGROUND_COLOR_PROPERTY);
            gridColorSerializedProperty = levelsDatabaseSerializedObject.FindProperty(GRID_COLOR_PROPERTY);

        }

        protected override void InitializeVariables()
        {
            gameSettings = GameUtility.LoadGameSettings();
            Serializer.Init();
            levelsHandler = new LevelsHandler(levelsDatabaseSerializedObject, levelsSerializedProperty);
            BindLevelsHandlerCallbacks();

            gridHandler = new CellTypesHandler();
            TileEffectType[] tileEffectTypes = (TileEffectType[])Enum.GetValues(typeof(TileEffectType));

            for (int i = 0; i < tileEffectTypes.Length; i++)
            {
                gridHandler.AddExtraProp(new CellTypesHandler.ExtraProp(i, tileEffectTypes[i].ToString()));
            }

            tabHandler = new TabHandler();
            tabHandler.AddTab(new TabHandler.Tab(LEVELS_TAB_NAME, DrawLevelsTab));
            tabHandler.AddTab(new TabHandler.Tab(SETTINGS_TAB_NAME, DrawSettingsTab));
            defaultColor = GUI.color;
            validationNeededAfterDrag = false;
            currentSideBarWidth = PlayerPrefs.GetInt(PREFS_WIDTH, SIDEBAR_WIDTH);
            currentButtonsWidth = PlayerPrefs.GetInt(PREFS_BUTTONS_WIDTH, BUTTONS_WIDTH);
            EnsureEditorSceneController(preferGameScene: false);

            wantsMouseMove = true;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorSceneManager.sceneOpened -= OnEditorSceneOpened;
            EditorSceneManager.sceneOpened += OnEditorSceneOpened;
            EditorSceneManager.sceneSaving -= OnEditorSceneSaving;
            EditorSceneManager.sceneSaving += OnEditorSceneSaving;
            EditorSceneManager.sceneSaved -= OnEditorSceneSaved;
            EditorSceneManager.sceneSaved += OnEditorSceneSaved;
            Undo.undoRedoPerformed -= OnLevelListUndoRedoPerformed;
            Undo.undoRedoPerformed += OnLevelListUndoRedoPerformed;
            HideGameUiForLevelEditor();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            // HideAndDontSave preview is Hierarchy-invisible and survives scene loads; always destroy before Play.
            if (change == PlayModeStateChange.ExitingEditMode)
            {
                RestoreEditorDockPreview();
                DestroyEditorPreview(includingOrphans: true);

                // Editor hide uses SetActive; runtime ShowPage only toggles Canvas.enabled.
                EnsureGameUiActiveForPlayMode();

                if (isEnteringPlayFromTestLevel)
                {
                    isEnteringPlayFromTestLevel = false;
                    return;
                }

                if (levelsHandler.SelectedLevelIndex == -1)
                {
                    ClearSelection();
                }
                else
                {
                    SetAsCurrentLevel(fromTestLevelButton: false);
                }

                return;
            }

            if (change == PlayModeStateChange.EnteredEditMode)
            {
                isEnteringPlayFromTestLevel = false;
                HideGameUiForLevelEditor();
            }
        }


        private void OpenLastActiveLevel()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (!lastActiveLevelOpened)
                {
                    Styles();

                    if ((levelsSerializedProperty.arraySize > 0) && PlayerPrefs.HasKey(PREFS_LEVEL))
                    {
                        int levelIndex = Mathf.Clamp(PlayerPrefs.GetInt(PREFS_LEVEL, 0), 0, levelsSerializedProperty.arraySize - 1);
                        if (!levelsHandler.IsIndexInLoadRange(levelIndex))
                        {
                            levelsHandler.ClearSelection();
                        }
                        else
                        {
                            UnityEngine.Object levelObject = levelsSerializedProperty.GetArrayElementAtIndex(levelIndex).objectReferenceValue;
                            if (levelObject == null)
                            {
                                // Missing reference (often broken GUID after .meta regeneration).
                                Debug.LogWarning($"Level Editor: saved level index {levelIndex} is missing. Clear selection.");
                                PlayerPrefs.DeleteKey(PREFS_LEVEL);
                                PlayerPrefs.Save();
                                levelsHandler.ClearSelection();
                            }
                            else
                            {
                                levelsHandler.CustomList.SelectedIndex = levelIndex;
                                levelsHandler.OpenLevel(levelIndex);
                            }
                        }
                    }

                    lastActiveLevelOpened = true;
                }
            }
        }

        protected override void Styles()
        {
            if (levelsDatabase != null)
            {
                levelsHandler = new LevelsHandler(levelsDatabaseSerializedObject, levelsSerializedProperty);
                BindLevelsHandlerCallbacks();
            }

            if (tabHandler != null)
            {
                tabHandler.SetDefaultToolbarStyle();
                gridHandler.SetDefaultLabelStyle();
                editColorsContent = new GUIContent(EditorCustomStyles.GetIcon("icon_menu"), "Edit used colors");
            }
        }

        public override void OpenLevel(UnityEngine.Object levelObject, int index)
        {
            PlayerPrefs.SetInt(PREFS_LEVEL, index);
            PlayerPrefs.Save();

            if (levelObject == null)
            {
                Debug.LogError($"Level Editor: cannot open level at index {index} — reference is missing (check Level Database / .meta GUIDs).");
                selectedLevelGenFunc = null;
                levelLabel = "# | <Missing Level>";
                ClearSelectedCells();
                return;
            }

            selectedLevelGenFunc = new LevelGenService(levelObject);
            levelLabel = selectedLevelGenFunc.GetLevelLabel(levelsHandler.SelectedLevelIndex, stringBuilder);
            selectedLevelGenFunc.ValidateLevel();
            UpdateLayersOptions();

            previousSelectedLayerIndex = int.MinValue;
            cachedCurrentLayerTileCount = -1;
            InitCellsForGameView();
            
            // 切换关卡时重置delta值
            deltaX = 0f;
            deltaY = 0f;
            showDeltaSettings = false;
            ClearSelectedCells();
        }

        public override string GetLevelLabel(UnityEngine.Object levelObject, int index)
        {
            return new LevelGenService(levelObject).GetLevelLabel(index, stringBuilder);
        }

        public override void ClearLevel(UnityEngine.Object levelObject)
        {
            new LevelGenService(levelObject).Clear();
        }

        public override bool LogErrorsForGlobalValidation(
            UnityEngine.Object levelObject,
            int index,
            out bool tilesExceedField,
            out bool tilesExceedFieldX,
            out bool tilesExceedFieldY,
            out bool tilesUnderfillField)
        {
            LevelGenService level = new LevelGenService(levelObject);
            level.ValidateLevel();
            level.ValidateTilesFieldBounds();
            tilesExceedField = level.TilesFieldExceeded;
            tilesExceedFieldX = level.TilesFieldExceededX;
            tilesExceedFieldY = level.TilesFieldExceededY;
            tilesUnderfillField = level.TilesFieldUnderfilled;

            if (!level.IsLevelCorrect)
            {
                int levelNumber = index + 1;
                Debug.LogError($"Level #{levelNumber} validation errors ({level.errorLabels.Count}):");

                for (int i = 0; i < level.errorLabels.Count; i++)
                {
                    string error = level.errorLabels[i];
                    string cause = GetValidationErrorCause(error);
                    Debug.LogError($"Level #{levelNumber} [{cause}]: {error}");
                }

                return false;
            }

            return true;
        }

        private static string GetValidationErrorCause(string error)
        {
            if (string.IsNullOrEmpty(error))
            {
                return "Unknown";
            }

            if (error.StartsWith("Tiles exceed LevelTilesField on X", StringComparison.Ordinal))
            {
                return "X轴超出 LevelTilesField：棋子包围盒宽度大于可用区域";
            }

            if (error.StartsWith("Tiles exceed LevelTilesField on Y", StringComparison.Ordinal))
            {
                return "Y轴超出 LevelTilesField：棋子包围盒高度大于可用区域";
            }

            if (error.StartsWith("Tiles exceed LevelTilesField", StringComparison.Ordinal))
            {
                return "超出 LevelTilesField：棋子包围盒大于可用区域";
            }

            if (error.StartsWith("Tiles underfill LevelTilesField", StringComparison.Ordinal))
            {
                return "未铺满 LevelTilesField：棋子包围盒偏小未贴边";
            }

            if (error.StartsWith("LevelFitter not found", StringComparison.Ordinal)
                || error.StartsWith("LevelTilesField size is invalid", StringComparison.Ordinal))
            {
                return "LevelTilesField 无法检测：缺少 LevelFitter 或场尺寸无效";
            }

            if (error.StartsWith("Incorrect tiles amount", StringComparison.Ordinal))
            {
                return "瓦片数量错误：不是 3 的倍数";
            }

            if (error.IndexOf("is empty", StringComparison.Ordinal) >= 0)
            {
                return "空层：某层没有任何已填格子";
            }

            if (error.IndexOf("incorrect height", StringComparison.Ordinal) >= 0
                || error.IndexOf("incorrect width", StringComparison.Ordinal) >= 0)
            {
                return "层尺寸错误：宽/高与底层级联规则不一致";
            }

            if (error.StartsWith("Level requires at least 2 layers", StringComparison.Ordinal))
            {
                return "层数不足：至少需要 2 层";
            }

            if (error.StartsWith("dataId", StringComparison.Ordinal))
            {
                return "dataId 冲突：与其它关卡重复";
            }

            if (error.StartsWith("Level reference is missing", StringComparison.Ordinal))
            {
                return "关卡引用缺失：LevelModel 为空或加载失败";
            }

            return "其它校验问题";
        }

        protected override void DrawContent()
        {
            tabHandler.DisplayTab();
        }

        #region Levels Tab
        protected override void ProcessEditorHotkeys()
        {
            SyncSelectModeFromCtrl();
            HandleLevelListUndoHotkeys();
        }

        private void HandleLevelListUndoHotkeys()
        {
            Event evt = Event.current;
            if (evt == null || evt.type != EventType.KeyDown)
            {
                return;
            }

            if (EditorGUIUtility.editingTextField)
            {
                return;
            }

            bool modifier = evt.control || evt.command;
            if (!modifier)
            {
                return;
            }

            if (evt.keyCode == KeyCode.Z && !evt.shift)
            {
                // Commit in-progress paint so Undo can target the finished stroke.
                EndBoardCellInteraction();
                Undo.PerformUndo();
                RefreshLevelListAfterUndoRedo();
                evt.Use();
                return;
            }

            if (evt.keyCode == KeyCode.Y || (evt.keyCode == KeyCode.Z && evt.shift))
            {
                EndBoardCellInteraction();
                Undo.PerformRedo();
                RefreshLevelListAfterUndoRedo();
                evt.Use();
            }
        }

        private void OnLevelListUndoRedoPerformed()
        {
            RefreshLevelListAfterUndoRedo();
        }

        private void RefreshLevelListAfterUndoRedo()
        {
            if (levelsHandler == null)
            {
                return;
            }

            if (levelsDatabaseSerializedObject != null)
            {
                levelsDatabaseSerializedObject.Update();
            }

            levelsHandler.RefreshAfterUndoRedo();
            Repaint();
        }

        private void DrawSettingsTab()
        {
            contentScrollViewVector = EditorGUILayout.BeginScrollView(contentScrollViewVector);
            DisplayPropertiesTab();
            EditorGUILayout.EndScrollView();
        }

        private void DrawLevelsTab()
        {
            // Clear board→list drag lock before the sidebar list handles this event.
            // Use rawType so cleanup still runs if another control already called Use().
            EventType rawType = Event.current.rawType;
            if (rawType == EventType.MouseUp || rawType == EventType.MouseLeaveWindow)
            {
                EndBoardCellInteraction();
            }

            EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));

            DrawPlatformToolbar();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
            DisplaySidebar();
            HandleChangingSideBar();
            DisplaySelectedLevel();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
        }

        private void DrawPlatformToolbar()
        {
            if (gameSettings == null)
            {
                gameSettings = GameUtility.LoadGameSettings();
            }
            LevelPlatform currentPlatform = gameSettings != null ? gameSettings.levelPlatform : LevelPlatformResolver.GetCurrentPlatform();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(string.Format(
                "当前编辑平台：{0}  |  数据库：{1}  |  目录：{2}",
                LevelPlatformResolver.GetDisplayName(currentPlatform),
                System.IO.Path.GetFileName(LevelPlatformResolver.GetDatabasePath(currentPlatform)),
                System.IO.Path.GetFileName(LevelPlatformResolver.GetLevelsFolder(currentPlatform))), EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("平台设置", GUILayout.Width(70));
            DrawPlatformButton(LevelPlatform.Android, currentPlatform);
            DrawPlatformButton(LevelPlatform.IOS, currentPlatform);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("切换平台会保存 GameSettings.levelPlatform，并重新加载对应平台的关卡数据库。新建/删除/排序只影响当前平台数据库。", MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        private void DrawPlatformButton(LevelPlatform platform, LevelPlatform currentPlatform)
        {
            EditorGUI.BeginDisabledGroup(platform == currentPlatform || gameSettings == null);
            if (GUILayout.Button(LevelPlatformResolver.GetDisplayName(platform), GUILayout.Width(90)))
            {
                SwitchLevelPlatform(platform);
            }
            EditorGUI.EndDisabledGroup();
        }

        private static LevelEditorWindow FindOpenLevelEditorWindow()
        {
            var windows = Resources.FindObjectsOfTypeAll<LevelEditorWindow>();
            for (int i = 0; i < windows.Length; i++)
            {
                if (windows[i] != null)
                {
                    return windows[i];
                }
            }

            return null;
        }

        /// <summary>Refresh sidebar labels after Project-window rename synced dataId to asset name.</summary>
        public static void RefreshOpenLevelListLabels()
        {
            LevelEditorWindow window = FindOpenLevelEditorWindow();
            if (window == null || window.levelsHandler == null)
            {
                return;
            }

            window.levelsHandler.SetLevelLabels();
            window.Repaint();
        }

        public static void ApplyLevelPlatformRefresh()
        {
            try
            {
                BuildUtility.ApplyGameSceneLevelDatabaseForBuild();
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
                return;
            }

            var window = FindOpenLevelEditorWindow();
            if (window == null)
            {
                return;
            }

            window.BindEditorInstance();

            if (window.selectedLevelGenFunc != null)
            {
                window.selectedLevelGenFunc.ApplyChanges();
            }

            window.gameSettings = GameUtility.LoadGameSettings();
            window.ClearSelection();
            window.lastActiveLevelOpened = false;
            window.ReloadLevelsDatabase();

            LevelPlatform platform = window.gameSettings != null
                ? window.gameSettings.levelPlatform
                : LevelPlatformResolver.GetCurrentPlatform();
            window.titleContent = new GUIContent("Level Editor - " + LevelPlatformResolver.GetDisplayName(platform));
            window.Repaint();
        }

        private void SwitchLevelPlatform(LevelPlatform platform)
        {
            if (gameSettings == null)
            {
                Debug.LogError("[LevelEditor] 无法加载 GameSettings，关卡平台切换失败。");
                return;
            }

            if (gameSettings.levelPlatform == platform)
            {
                return;
            }

            gameSettings.levelPlatform = platform;
            EditorUtility.SetDirty(gameSettings);
            AssetDatabase.SaveAssets();
            ApplyLevelPlatformRefresh();
            Debug.LogFormat("[LevelEditor] 已切换关卡编辑平台：{0}", LevelPlatformResolver.GetDisplayName(platform));
        }

        private void EndBoardCellInteraction()
        {
            if (validationNeededAfterDrag && selectedLevelGenFunc != null)
            {
                // One apply at stroke end — register Undo so Ctrl+Z reverts this paint stroke.
                Undo.SetCurrentGroupName("Paint Cells");
                selectedLevelGenFunc.ApplyChanges(registerUndo: true);
                validationNeededAfterDrag = false;
                RefreshCurrentLevelLabelOnly();
            }

            if (boxCoverStrokePending && selectedLevelGenFunc != null)
            {
                Undo.SetCurrentGroupName("Edit Box Cover Candidates");
                selectedLevelGenFunc.ApplyChanges(registerUndo: true);
                boxCoverStrokePending = false;
                selectedLevelGenFunc.ValidateLevel(checkDataIdUniqueness: false);
                RefreshBoxCoverPreview();
                RefreshCurrentLevelLabelOnly();
            }

            if (isDraggingCells || (levelsHandler != null && levelsHandler.IgnoreDragEvents && !separatorIsDragged && !buttonSeparatorIsDragged))
            {
                isDraggingCells = false;
                if (levelsHandler != null && !separatorIsDragged && !buttonSeparatorIsDragged)
                {
                    levelsHandler.IgnoreDragEvents = false;
                }
            }
        }

        private void HandleChangingSideBar()
        {
            separatorRect = EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(0), GUILayout.ExpandHeight(true));
            EditorGUILayout.EndHorizontal();
            separatorRect.xMin -= GUI.skin.box.margin.right;
            separatorRect.xMax += GUI.skin.box.margin.left;
            EditorGUIUtility.AddCursorRect(separatorRect, MouseCursor.ResizeHorizontal);


            if (separatorRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.MouseDown)
                {
                    separatorIsDragged = true;
                    levelsHandler.IgnoreDragEvents = true;
                    Event.current.Use();
                }
            }

            if (separatorIsDragged)
            {
                if (Event.current.type == EventType.MouseUp)
                {
                    separatorIsDragged = false;
                    levelsHandler.IgnoreDragEvents = false;
                    PlayerPrefs.SetInt(PREFS_WIDTH, currentSideBarWidth);
                    PlayerPrefs.Save();
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.MouseDrag)
                {
                    currentSideBarWidth = Mathf.RoundToInt(Event.current.delta.x) + currentSideBarWidth;
                    Event.current.Use();
                }
            }
        }

        private void DisplaySidebar()
        {
            OpenLastActiveLevel();

            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(currentSideBarWidth), GUILayout.ExpandHeight(true));
            levelsHandler.DisplayReordableList();
            levelsHandler.DrawMultipleChoiceButton();
            levelsHandler.DrawGlobalValidationButton();
            levelsHandler.DrawExportLayerTileCountsButton();
            levelsHandler.DrawRenameAllButton();

            EditorGUILayout.EndVertical();
        }
        private void DisplaySelectedLevel()
        {
            if (drawEditVariablesTabDisplayed)
            {
                DrawEditVariablesTab();
                return;
            }


            if (levelsHandler.SelectedLevelIndex == -1)
            {
                EditorGUILayout.Space();
                return;
            }

            if (selectedLevelGenFunc == null)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.HelpBox(
                    "Selected level reference is Missing.\nRestore Level *.asset.meta GUIDs from git (with Unity closed), or reassign the level in Level Database.",
                    MessageType.Error);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            DrawEditLayersTab();
            EditorGUILayout.EndVertical();
        }



        private void ClearSelectedCells()
        {
            selectedCells.Clear();
            selectionOffsetX = 0;
            selectionOffsetY = 0;
        }

        private static bool IsEventCtrlDown(Event evt)
        {
            return evt != null && (evt.control || evt.command);
        }

        private bool IsSelectModeActive()
        {
            return !boxCoverMode && (ctrlHeldForSelect || editorToolMode == EditorToolMode.Select);
        }

        private void SetEditorToolMode(EditorToolMode mode)
        {
            if (editorToolMode == mode)
            {
                return;
            }

            editorToolMode = mode;
            if (mode == EditorToolMode.Draw)
            {
                // Manual switch to Draw also exits select / clears pending offset selection.
                ClearSelectedCells();
            }
        }

        private void SetBoxCoverMode(bool enabled)
        {
            if (boxCoverMode == enabled)
            {
                return;
            }

            EndBoardCellInteraction();
            boxCoverMode = enabled;
            if (enabled)
            {
                ClearSelectedCells();
            }

            RefreshBoxCoverPreview();
            Repaint();
        }

        /// <summary>
        /// Clears selection and pins toolbar back to Draw.
        /// </summary>
        private void ExitSelectToDraw()
        {
            ClearSelectedCells();
            editorToolMode = EditorToolMode.Draw;
        }

        /// <summary>
        /// Holding Ctrl temporarily shows/uses Select. Releasing Ctrl with a selection keeps Select
        /// until Apply or empty-cell click.
        /// </summary>
        private void SyncSelectModeFromCtrl()
        {
            currentEvent = Event.current;
            if (currentEvent == null)
            {
                return;
            }

            // Only steal text focus on board interaction, never on Repaint.
            if (isPointerOverBoard
                && (currentEvent.type == EventType.MouseMove || currentEvent.type == EventType.MouseDown))
            {
                ReleaseTextFieldFocus();
            }

            bool ctrlDown = IsEventCtrlDown(currentEvent);
            if (ctrlHeldForSelect == ctrlDown)
            {
                return;
            }

            bool wasHeld = ctrlHeldForSelect;
            ctrlHeldForSelect = ctrlDown;

            if (ctrlDown)
            {
                ReleaseTextFieldFocus();
            }
            else if (wasHeld && selectedCells.Count > 0)
            {
                // Stay in Select after Ctrl release when tiles are selected.
                editorToolMode = EditorToolMode.Select;
            }

            Repaint();
        }

        private void ReleaseTextFieldFocus()
        {
            // Do not clear GUIUtility.hotControl — that breaks GUI click tracking / rapid cell clicks.
            if (!EditorGUIUtility.editingTextField && GUIUtility.keyboardControl == 0)
            {
                return;
            }

            EditorGUIUtility.editingTextField = false;
            GUIUtility.keyboardControl = 0;
        }

        private void UpdateLayersOptions()
        {
            selectedLayerIndex = -1;

            if (selectedLevelGenFunc.layersProperty.arraySize > 0)
            {
                selectedLayerIndex = 0;
            }

            ClearSelectedCells();
            RebuildLayersOptionsLabels();
        }

        private void RebuildLayersOptionsLabels()
        {
            layersOptions = new string[selectedLevelGenFunc.layersProperty.arraySize];

            for (int i = 0; i < layersOptions.Length; i++)
            {
                if (i == 0)
                {
                    layersOptions[i] = selectedLevelGenFunc.ResolveLayerLabel(i) + " [top]";
                }
                else if (i == layersOptions.Length - 1)
                {
                    layersOptions[i] = selectedLevelGenFunc.ResolveLayerLabel(i) + " [bottom]";
                }
                else
                {
                    layersOptions[i] = selectedLevelGenFunc.ResolveLayerLabel(i);
                }
            }
        }

        private void RefreshAfterLayerReorder(int newSelectedLayerIndex)
        {
            ClearSelectedCells();
            RebuildLayersOptionsLabels();
            selectedLayerIndex = newSelectedLayerIndex;
            selectedLevelGenFunc.ValidateLevel();
            InitCellsForGameView();
            previousSelectedLayerIndex = int.MinValue;
        }

        private void RemoveCurrentLayerWithConfirm()
        {
            if (selectedLevelGenFunc == null || selectedLayerIndex < 0)
            {
                return;
            }

            if (!EditorUtility.DisplayDialog(WARNING_LABEL, CURRENT_LAYER_REMOVE_WARNING, OK_LABEL, CANCEL_LABEL))
            {
                return;
            }

            ClearSelectedCells();
            selectedLevelGenFunc.RemoveLayerAt(selectedLayerIndex);
            UpdateLayersOptions();
            selectedLevelGenFunc.ValidateLevel();
            InitCellsForGameView();
            previousSelectedLayerIndex = int.MinValue;
        }

        private void MoveSelectedLayerUp()
        {
            if (selectedLevelGenFunc == null || selectedLayerIndex <= 0)
            {
                return;
            }

            int newIndex = selectedLevelGenFunc.MoveLayerTowardTop(selectedLayerIndex);
            RefreshAfterLayerReorder(newIndex);
        }

        private void MoveSelectedLayerDown()
        {
            if (selectedLevelGenFunc == null
                || selectedLayerIndex < 0
                || selectedLayerIndex >= selectedLevelGenFunc.layersProperty.arraySize - 1)
            {
                return;
            }

            int newIndex = selectedLevelGenFunc.MoveLayerTowardBottom(selectedLayerIndex);
            RefreshAfterLayerReorder(newIndex);
        }

        private void CopySelectedLayer()
        {
            if (selectedLevelGenFunc == null || selectedLayerIndex < 0)
            {
                return;
            }

            if (selectedLevelGenFunc.TryCopyLayer(selectedLayerIndex, out string json))
            {
                copiedLayerJson = json;
            }
        }

        private void PasteOntoSelectedLayer()
        {
            if (selectedLevelGenFunc == null || selectedLayerIndex < 0 || string.IsNullOrEmpty(copiedLayerJson))
            {
                return;
            }

            if (!CanPasteCopiedLayerBelowSelected(out string errorMessage))
            {
                EditorUtility.DisplayDialog("Paste Layer", errorMessage, "OK");
                return;
            }

            if (!selectedLevelGenFunc.TryInsertCopiedLayerBelow(selectedLayerIndex, copiedLayerJson, out int newIndex))
            {
                return;
            }

            ClearSelectedCells();
            RebuildLayersOptionsLabels();
            selectedLayerIndex = newIndex;
            selectedLevelGenFunc.ValidateLevel();
            InitCellsForGameView();
            previousSelectedLayerIndex = int.MinValue;
            GUI.changed = true;
        }

        private bool CanPasteCopiedLayerBelowSelected(out string errorMessage)
        {
            errorMessage = null;
            if (!HasCopiedLayer() || selectedLevelGenFunc == null || selectedLayerIndex < 0)
            {
                errorMessage = "No copied layer or no layer selected.";
                return false;
            }

            return true;
        }

        private static bool HasCopiedLayer()
        {
            return !string.IsNullOrEmpty(copiedLayerJson);
        }

        private void ClearSelectedLayer()
        {
            if (selectedLevelGenFunc == null || selectedLayerIndex < 0)
            {
                return;
            }

            selectedLevelGenFunc.ClearLayer(selectedLayerIndex);
            ClearSelectedCells();
            cachedCurrentLayerTileCount = 0;
            selectedLevelGenFunc.ValidateLevel(checkDataIdUniqueness: false);
            InitCellsForGameView();
            previousSelectedLayerIndex = int.MinValue;
            RefreshCurrentLevelLabelOnly();
            GUI.changed = true;
        }

        private void ClearSelectedLayerEffects()
        {
            if (selectedLevelGenFunc == null || selectedLayerIndex < 0)
            {
                return;
            }

            selectedLevelGenFunc.ClearLayerEffects(selectedLayerIndex);
            Undo.SetCurrentGroupName("Clear Layer Effects And Box Covers");
            selectedLevelGenFunc.ApplyChanges(registerUndo: true);
            selectedLevelGenFunc.ValidateLevel(checkDataIdUniqueness: false);
            InitCellsForGameView();
            previousSelectedLayerIndex = int.MinValue;
            RefreshCurrentLevelLabelOnly();
            GUI.changed = true;
        }

        private void ClearSelectedLayerBoxCovers()
        {
            if (selectedLevelGenFunc == null || selectedLayerIndex < 0)
            {
                return;
            }

            if (!selectedLevelGenFunc.ClearLayerBoxCovers(selectedLayerIndex))
            {
                return;
            }

            Undo.SetCurrentGroupName("Clear Box Covers");
            selectedLevelGenFunc.ApplyChanges(registerUndo: true);
            selectedLevelGenFunc.ValidateLevel(checkDataIdUniqueness: false);
            RefreshBoxCoverPreview();
            RefreshCurrentLevelLabelOnly();
            GUI.changed = true;
            Repaint();
        }

        private void ShowLayerContextMenu()
        {
            GenericMenu menu = new GenericMenu();

            if (selectedLayerIndex >= 0 && selectedLevelGenFunc != null && selectedLevelGenFunc.layersProperty.arraySize > 0)
            {
                menu.AddItem(new GUIContent("Remove current layer"), false, RemoveCurrentLayerWithConfirm);
                menu.AddItem(new GUIContent("Clear tiles"), false, ClearSelectedLayer);
                menu.AddItem(new GUIContent("Clear effects & Box Covers"), false, ClearSelectedLayerEffects);
                menu.AddItem(new GUIContent("Clear Box Covers"), false, ClearSelectedLayerBoxCovers);
                menu.AddItem(new GUIContent("Copy layer"), false, CopySelectedLayer);
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Remove current layer"));
                menu.AddDisabledItem(new GUIContent("Clear tiles"));
                menu.AddDisabledItem(new GUIContent("Clear effects & Box Covers"));
                menu.AddDisabledItem(new GUIContent("Clear Box Covers"));
                menu.AddDisabledItem(new GUIContent("Copy layer"));
            }

            if (CanPasteCopiedLayerBelowSelected(out _))
            {
                menu.AddItem(new GUIContent("Paste layer"), false, PasteOntoSelectedLayer);
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Paste layer"));
            }

            if (selectedLayerIndex > 0)
            {
                menu.AddItem(new GUIContent("Move layer up"), false, MoveSelectedLayerUp);
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Move layer up"));
            }

            if (selectedLevelGenFunc != null
                && selectedLayerIndex >= 0
                && selectedLayerIndex < selectedLevelGenFunc.layersProperty.arraySize - 1)
            {
                menu.AddItem(new GUIContent("Move layer down"), false, MoveSelectedLayerDown);
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Move layer down"));
            }

            menu.ShowAsContext();
        }

        private void DrawLayersSideList()
        {
            if (layersOptions == null || layersOptions.Length == 0)
            {
                GUILayout.Box("No layers", GUILayout.Width(currentButtonsWidth), GUILayout.ExpandHeight(true));
                return;
            }

            EnsureLayerDragHandleStyle();

            int count = layersOptions.Length;
            Rect listRect = EditorGUILayout.BeginVertical(GUILayout.Width(currentButtonsWidth), GUILayout.ExpandHeight(true));
            Event evt = Event.current;

            // Cache full list area (SelectionGrid used ExpandHeight — rows share this height).
            if (listRect.height > 1f && listRect.width > 1f)
            {
                layerListContentRect = listRect;
                layerListRowHeight = Mathf.Max(EditorGUIUtility.singleLineHeight + 8f, listRect.height / count);
            }

            HandleLayerListDragInput(evt, count);

            if (isDraggingLayerList)
            {
                float drawY = layerListContentRect.y;
                for (int slot = 0; slot < count; slot++)
                {
                    Rect rowRect = new Rect(layerListContentRect.x, drawY, layerListContentRect.width, layerListRowHeight);
                    if (slot == layerDragCurrentIndex)
                    {
                        if (evt.type == EventType.Repaint)
                        {
                            EditorGUI.DrawRect(rowRect, new Color(0.24f, 0.48f, 0.9f, 0.35f));
                        }

                        drawY += layerListRowHeight;
                        continue;
                    }

                    int sourceIndex = GetLayerDragSourceIndexForSlot(slot);
                    DrawLayerListRow(rowRect, sourceIndex, false);
                    drawY += layerListRowHeight;
                }

                float floatY = Mathf.Clamp(
                    layerDragElementY,
                    layerListContentRect.yMin,
                    layerListContentRect.yMax - layerListRowHeight);
                Rect floatRect = new Rect(layerListContentRect.x, floatY, layerListContentRect.width, layerListRowHeight);
                DrawLayerListRow(floatRect, layerDragStartIndex, true);

                GUILayoutUtility.GetRect(currentButtonsWidth, layerListContentRect.height, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    // Match original SelectionGrid: each row expands to fill remaining height.
                    Rect rowRect = GUILayoutUtility.GetRect(
                        GUIContent.none,
                        GUI.skin.button,
                        GUILayout.ExpandWidth(true),
                        GUILayout.ExpandHeight(true));

                    Rect handleHitRect = GetLayerDragHandleHitRect(rowRect);

                    if (evt.type == EventType.MouseDown && rowRect.Contains(evt.mousePosition))
                    {
                        if (evt.button == 0)
                        {
                            if (selectedLayerIndex != i)
                            {
                                selectedLayerIndex = i;
                                GUI.changed = true;
                            }

                            // Only arm drag when pressing the handle.
                            if (handleHitRect.Contains(evt.mousePosition))
                            {
                                layerListMouseDownPos = evt.mousePosition;
                                layerListMouseDownIndex = i;
                                layerListDragArmed = true;
                                EditorGUIUtility.AddCursorRect(handleHitRect, MouseCursor.Pan);
                            }
                            else
                            {
                                layerListMouseDownIndex = -1;
                                layerListDragArmed = false;
                            }

                            evt.Use();
                            Repaint();
                        }
                        else if (evt.button == 1)
                        {
                            selectedLayerIndex = i;
                            ShowLayerContextMenu();
                            layerListMouseDownIndex = -1;
                            layerListDragArmed = false;
                            evt.Use();
                            GUI.changed = true;
                            Repaint();
                        }
                    }

                    EditorGUIUtility.AddCursorRect(handleHitRect, MouseCursor.Pan);
                    DrawLayerListRow(rowRect, i, i == selectedLayerIndex);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void EnsureLayerDragHandleStyle()
        {
            if (layerDragHandleStyle != null)
            {
                return;
            }

            layerDragHandleStyle = GUI.skin.FindStyle("RL DragHandle");
            if (layerDragHandleStyle == null)
            {
                layerDragHandleStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 10,
                    normal = { textColor = new Color(0.75f, 0.75f, 0.75f, 1f) }
                };
            }
        }

        private static Rect GetLayerDragHandleHitRect(Rect rowRect)
        {
            return new Rect(rowRect.x, rowRect.y, LAYER_DRAG_HANDLE_HIT_WIDTH, rowRect.height);
        }

        private static Rect GetLayerDragHandleDrawRect(Rect rowRect)
        {
            const float handleWidth = 10f;
            const float handleHeight = 6f;
            return new Rect(
                rowRect.x + 5f,
                rowRect.y + (rowRect.height - handleHeight) * 0.5f,
                handleWidth,
                handleHeight);
        }

        private void DrawLayerListRow(Rect rowRect, int layerIndex, bool selected)
        {
            if (layerIndex < 0 || layersOptions == null || layerIndex >= layersOptions.Length)
            {
                return;
            }

            if (Event.current.type == EventType.Repaint)
            {
                GUIContent label = new GUIContent("   " + layersOptions[layerIndex]);
                GUI.skin.button.Draw(rowRect, label, false, false, selected, false);

                Rect handleRect = GetLayerDragHandleDrawRect(rowRect);
                if (layerDragHandleStyle.name == "RL DragHandle")
                {
                    layerDragHandleStyle.Draw(handleRect, false, false, false, false);
                }
                else
                {
                    GUI.Label(GetLayerDragHandleHitRect(rowRect), "≡", layerDragHandleStyle);
                }
            }
        }

        private void HandleLayerListDragInput(Event evt, int count)
        {
            if (buttonSeparatorIsDragged || count <= 0 || layerListContentRect.height < 1f)
            {
                return;
            }

            float rowHeight = layerListRowHeight;

            if (!isDraggingLayerList)
            {
                if (evt.type == EventType.MouseDrag
                    && layerListDragArmed
                    && layerListMouseDownIndex >= 0
                    && (layerListMouseDownPos - evt.mousePosition).sqrMagnitude >= LAYER_DRAG_START_DISTANCE * LAYER_DRAG_START_DISTANCE)
                {
                    isDraggingLayerList = true;
                    layerDragStartIndex = layerListMouseDownIndex;
                    layerDragCurrentIndex = layerDragStartIndex;
                    float startRowY = layerListContentRect.y + layerDragStartIndex * rowHeight;
                    layerDragOffsetY = evt.mousePosition.y - startRowY;
                    layerDragElementY = Mathf.Clamp(
                        evt.mousePosition.y - layerDragOffsetY,
                        layerListContentRect.yMin,
                        layerListContentRect.yMax - rowHeight);
                    selectedLayerIndex = layerDragStartIndex;
                    evt.Use();
                    Repaint();
                }
                else if (evt.rawType == EventType.MouseUp)
                {
                    layerListMouseDownIndex = -1;
                    layerListDragArmed = false;
                }

                return;
            }

            if (evt.type == EventType.MouseDrag)
            {
                layerDragElementY = Mathf.Clamp(
                    evt.mousePosition.y - layerDragOffsetY,
                    layerListContentRect.yMin,
                    layerListContentRect.yMax - rowHeight);

                float localY = evt.mousePosition.y - layerListContentRect.y;
                int hover = Mathf.FloorToInt(localY / rowHeight);
                layerDragCurrentIndex = Mathf.Clamp(hover, 0, count - 1);
                evt.Use();
                Repaint();
            }
            else if (evt.rawType == EventType.MouseUp)
            {
                int from = layerDragStartIndex;
                int to = layerDragCurrentIndex;
                isDraggingLayerList = false;
                layerListDragArmed = false;
                layerListMouseDownIndex = -1;
                layerDragStartIndex = -1;
                layerDragCurrentIndex = -1;

                if (evt.type != EventType.Used)
                {
                    evt.Use();
                }

                if (from >= 0 && to >= 0 && from != to && selectedLevelGenFunc != null)
                {
                    int newIndex = selectedLevelGenFunc.MoveLayerTo(from, to);
                    RefreshAfterLayerReorder(newIndex);
                    GUI.changed = true;
                }

                Repaint();
            }
        }

        private int GetLayerDragSourceIndexForSlot(int slot)
        {
            if (slot == layerDragCurrentIndex)
            {
                return -1;
            }

            if (layerDragCurrentIndex < layerDragStartIndex)
            {
                if (slot < layerDragCurrentIndex)
                {
                    return slot;
                }

                if (slot <= layerDragStartIndex)
                {
                    return slot - 1;
                }

                return slot;
            }

            if (layerDragCurrentIndex > layerDragStartIndex)
            {
                if (slot < layerDragStartIndex)
                {
                    return slot;
                }

                if (slot < layerDragCurrentIndex)
                {
                    return slot + 1;
                }

                return slot;
            }

            return slot == layerDragStartIndex ? -1 : slot;
        }

        private void EnsureGameSceneForPreview()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path == GAME_SCENE_PATH)
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(GAME_SCENE_PATH);
        }

        private void EnsureEditorSceneController(bool preferGameScene = true)
        {
            // Open GameScene first: switching scenes destroys HideAndDontSave preview roots.
            if (preferGameScene)
            {
                EnsureGameSceneForPreview();
            }

            if (editorSceneController != null && editorSceneController.IsValid)
            {
                return;
            }

            if (editorSceneController != null)
            {
                editorSceneController.Destroy();
                editorSceneController = null;
            }

            CreateEditorSceneController();
        }

        private void CreateEditorSceneController()
        {
            GameObject gameObject = new GameObject("[Level editor]");
            // Same as tileexplorerlike: hidden in Hierarchy, still rendered in Scene view.
            gameObject.hideFlags = HideFlags.HideAndDontSave;

            editorSceneController = new EditorSceneController(gameObject);

            if (tilesProperty.arraySize == 0)
            {
                Debug.LogError("Level editor needs at least one object tiles array to work properly");
            }

            if (tilesProperty.GetArrayElementAtIndex(0).FindPropertyRelative(PREFAB_PROPERTY_NAME).objectReferenceValue != null)
            {
                editorSceneController.InitMaterials(tilesProperty.GetArrayElementAtIndex(0).FindPropertyRelative(PREFAB_PROPERTY_NAME).objectReferenceValue as GameObject);
            }
            else
            {
                Debug.LogError("Null prefab at index 0.");
            }
        }

        private void OnEditorSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            // Test Level entry must keep UI active — do not re-hide after OpenScene.
            if (EditorApplication.isPlayingOrWillChangePlaymode || isRebuildingPreview || isEnteringPlayFromTestLevel)
            {
                return;
            }

            // Only rebuild when landing on GameScene; other scenes would immediately re-open GameScene via Ensure.
            if (!IsGameScene(scene))
            {
                // HideAndDontSave does NOT unload with the scene — destroy preview or blank tiles linger in StartScene.
                DestroyEditorPreview(includingOrphans: true);
                DiscardGameUiVisibilityCache();
                return;
            }

            HideGameUiForLevelEditor();

            if (levelsHandler == null || levelsHandler.SelectedLevelIndex < 0 || selectedLevelGenFunc == null)
            {
                return;
            }

            // Must Destroy before rebuild; nulling alone orphans previous HideAndDontSave roots.
            DestroyEditorPreview(includingOrphans: true);
            InitCellsForGameView();
            Repaint();
        }

        private void InitCellsForGameView()
        {
            if (selectedLevelGenFunc == null)
            {
                return;
            }

            bool wasRebuildingPreview = isRebuildingPreview;
            isRebuildingPreview = true;
            try
            {
                EnsureEditorSceneController(preferGameScene: true);
                if (!gameUiHiddenForEditor)
                {
                    HideGameUiForLevelEditor();
                }

                bool value;
                editorSceneController.Clear();

                int totalLayerCount = selectedLevelGenFunc.layersProperty.arraySize;
                ApplyRuntimeLayoutToEditorPreview(totalLayerCount);

                int maxPrefabs = Mathf.Max(1, tilesProperty.arraySize - 1);
                GameObject[] prefabs = new GameObject[Mathf.Clamp(selectedLevelGenFunc.elementsPerLevelProperty.intValue, 1, maxPrefabs)];

                for (int i = 0; i < prefabs.Length; i++)
                {
                    prefabs[i] = tilesProperty.GetArrayElementAtIndex(i).FindPropertyRelative(PREFAB_PROPERTY_NAME).objectReferenceValue as GameObject;
                }

                editorSceneController.prefabs = prefabs;
                editorSceneController.setsAmount = selectedLevelGenFunc.SetsAmount;

                int layerWidth;
                int layerHeight;

                for (int i = 0; i < totalLayerCount; i++)
                {
                    layerWidth = selectedLevelGenFunc.GetLayerWidth(i);
                    layerHeight = selectedLevelGenFunc.GetLayerHeight(i);

                    // 设置该层级的delta偏移量
                    float deltaX = selectedLevelGenFunc.GetLayerDeltaX(i);
                    float deltaY = selectedLevelGenFunc.GetLayerDeltaY(i);
                    editorSceneController.SetLayerDeltaOffset(i, deltaX, deltaY);
                    editorSceneController.EnsureLayer(i);

                    for (int y = 0; y < layerHeight; y++)
                    {
                        for (int x = 0; x < layerWidth; x++)
                        {
                            value = selectedLevelGenFunc.GetCellValue(i, x, y);
                            if (!value)
                            {
                                continue;
                            }

                            editorSceneController.InitCells(i, x, y, true, totalLayerCount);
                            editorSceneController.SetCellDeltaOffset(i, x, y,
                                selectedLevelGenFunc.GetCellDeltaX(i, x, y),
                                selectedLevelGenFunc.GetCellDeltaY(i, x, y));
                        }
                    }
                }

                RefreshBoxCoverPreview();
                AlignEditorPreviewDockIfNeeded();

                // Rebuild recreates all layer roots as active; re-apply selected-layer visibility.
                if (selectedLayerIndex >= 0 && selectedLayerIndex < totalLayerCount)
                {
                    editorSceneController.UpdateSelectedLayerIndex(selectedLayerIndex);
                    editorSceneController.UpdateBoxCoverSelection(selectedLayerIndex, selectedBoxCoverSize);
                    previousSelectedLayerIndex = selectedLayerIndex;
                }
                else
                {
                    previousSelectedLayerIndex = int.MinValue;
                }
            }
            finally
            {
                isRebuildingPreview = wasRebuildingPreview;
            }
        }

        /// <summary>
        /// Editor preview placement: LevelTilesFieldCenter + TileSize(showLayerScale) + showLayerDelta.
        /// </summary>
        private void ApplyRuntimeLayoutToEditorPreview(int totalLayerCount)
        {
            if (editorSceneController == null)
            {
                return;
            }

            if (totalLayerCount >= 1)
            {
                int evenLayerIndex = totalLayerCount - 1;
                editorSceneController.EvenLayerSize = new Vector2Int(
                    selectedLevelGenFunc.GetLayerWidth(evenLayerIndex),
                    selectedLevelGenFunc.GetLayerHeight(evenLayerIndex));
            }
            else
            {
                editorSceneController.EvenLayerSize = new Vector2Int(
                    selectedLevelGenFunc.bottomLayerWidthProperty.intValue,
                    selectedLevelGenFunc.bottomLayerHeightProperty.intValue);
            }

            if (totalLayerCount >= 2)
            {
                int oddLayerIndex = totalLayerCount - 2;
                editorSceneController.OddLayerSize = new Vector2Int(
                    selectedLevelGenFunc.GetLayerWidth(oddLayerIndex),
                    selectedLevelGenFunc.GetLayerHeight(oddLayerIndex));
            }
            else
            {
                int offset = selectedLevelGenFunc.GetOffset();
                editorSceneController.OddLayerSize = new Vector2Int(
                    editorSceneController.EvenLayerSize.x + offset,
                    editorSceneController.EvenLayerSize.y + offset);
            }

            editorSceneController.ShowLayerDelta = new Vector2(
                selectedLevelGenFunc.showLayerDeltaXProperty.floatValue,
                selectedLevelGenFunc.showLayerDeltaYProperty.floatValue);

            LevelFitter levelScaler = UnityEngine.Object.FindObjectOfType<LevelFitter>();
            // Fallback: design camera size with zero tiles-field offset (center Y shift -1.5).
            Vector2 tilesFieldSize = new Vector2(7.2f, 15.6f);
            Vector3 fieldCenter = new Vector3(0f, -1.5f, 0f);

            if (levelScaler != null)
            {
                levelScaler.InitForEditorPreview();
                tilesFieldSize = levelScaler.LevelTilesFieldSize;
                fieldCenter = levelScaler.LevelTilesFieldCenter;
            }

            TileGameModel gameData = FindTileGameDataInScene();
            float maxTileSize = gameData != null ? gameData.MaxTileSize : 1.5f;
            Vector2 dataTileSize = gameData != null ? gameData.TileSize : Vector2.one;
            Vector2Int biggerLayerSize = selectedLevelGenFunc.GetMinShowSize();
            float showLayerScale = selectedLevelGenFunc.showLayerScaleProperty.floatValue;

            editorSceneController.TileSize = LevelFitter.CalculateTileSize(
                tilesFieldSize,
                biggerLayerSize,
                showLayerScale,
                maxTileSize,
                dataTileSize);

            editorSceneController.TilesFieldSize = tilesFieldSize;
            editorSceneController.SetFieldCenter(fieldCenter);
        }

        private void RefreshBoxCoverPreview()
        {
            if (selectedLevelGenFunc == null
                || editorSceneController == null
                || !editorSceneController.IsValid)
            {
                return;
            }

            editorSceneController.RefreshBoxCovers(
                selectedLevelGenFunc.LevelData,
                selectedLayerIndex,
                selectedBoxCoverSize);
        }

        void AlignEditorPreviewDockIfNeeded()
        {
            DockActor dock = UnityEngine.Object.FindObjectOfType<DockActor>();
            if (dock == null)
                return;

            LevelModel previewLevel = selectedLevelGenFunc != null ? selectedLevelGenFunc.LevelData : null;
            SpecialPlayDock.SyncPreview(previewLevel);
            dock.ApplyTheme();

            bool iqLevel = levelsHandler != null
                && LevelDirector.IsIQChallengeLevelIndex(levelsHandler.SelectedLevelIndex);
            bool shouldAdapt = previewLevel != null
                && (previewLevel.HasBoxCovers || iqLevel)
                && editorSceneController != null
                && editorSceneController.IsValid;
            if (!shouldAdapt)
            {
                dock.RestoreAuthoredPosition();
                return;
            }

            UIGame gameUI = UnityEngine.Object.FindObjectOfType<UIGame>(true);
            editorSceneController.ApplyBoxCoverPlayfield(dock, gameUI);
        }

        void RefreshEditorSpecialDockPreview()
        {
            AlignEditorPreviewDockIfNeeded();
        }

        static void RestoreEditorDockPreview()
        {
            SpecialPlayDock.PreviewNoLevelIdPlate = false;
            DockActor dock = UnityEngine.Object.FindObjectOfType<DockActor>();
            dock?.ApplyTheme();
            dock?.RestoreAuthoredPosition();
        }

        /// <summary>
        /// Cheap update for showLayerScale / showLayerDelta: keep existing tiles, only refresh size/center/positions.
        /// </summary>
        private void RefreshEditorPreviewLayout()
        {
            if (selectedLevelGenFunc == null)
            {
                return;
            }

            if (editorSceneController == null || !editorSceneController.IsValid)
            {
                InitCellsForGameView();
                return;
            }

            int totalLayerCount = selectedLevelGenFunc.layersProperty.arraySize;
            ApplyRuntimeLayoutToEditorPreview(totalLayerCount);
            editorSceneController.RefreshLayout(totalLayerCount);
            AlignEditorPreviewDockIfNeeded();
        }

        /// <summary>
        /// One-click fit: write showLayerScale / showLayerDelta so filled tiles sit centered in LevelTilesField.
        /// </summary>
        private void ApplyCenterCorrection()
        {
            if (selectedLevelGenFunc == null)
            {
                return;
            }

            TryApplyCenterCorrection(selectedLevelGenFunc, refreshPreview: true, showDialogs: true);
            GUI.changed = true;
        }

        /// <returns>True if scale/delta were written; false if skipped (no layers / no filled tiles / null).</returns>
        private bool TryApplyCenterCorrection(LevelGenService level, bool refreshPreview, bool showDialogs)
        {
            if (level == null || level.NullLevel || level.layersProperty == null)
            {
                return false;
            }

            int totalLayerCount = level.layersProperty.arraySize;
            if (totalLayerCount <= 0)
            {
                if (showDialogs)
                {
                    EditorUtility.DisplayDialog("Center Correction", "No layers to correct.", "OK");
                }

                return false;
            }

            ResolvePreviewLayerSizes(level, totalLayerCount, out Vector2Int evenLayerSize, out Vector2Int oddLayerSize);

            LevelFitter levelScaler = UnityEngine.Object.FindObjectOfType<LevelFitter>();
            // Fallback: design camera size with zero tiles-field offset.
            Vector2 tilesFieldSize = new Vector2(7.2f, 15.6f);
            if (levelScaler != null)
            {
                levelScaler.InitForEditorPreview();
                tilesFieldSize = levelScaler.LevelTilesFieldSize;
            }

            TileGameModel gameData = FindTileGameDataInScene();
            float maxTileSize = gameData != null ? gameData.MaxTileSize : 1.5f;
            Vector2 dataTileSize = gameData != null ? gameData.TileSize : Vector2.one;
            Vector2Int biggerLayerSize = level.GetMinShowSize();

            Vector2 tileSizeAtScale1 = LevelFitter.CalculateTileSize(
                tilesFieldSize,
                biggerLayerSize,
                1f,
                maxTileSize,
                dataTileSize);

            if (!TryComputeFilledTilesAabb(
                    level,
                    totalLayerCount,
                    tileSizeAtScale1,
                    evenLayerSize,
                    oddLayerSize,
                    Vector2.zero,
                    out Vector2 aabbMin,
                    out Vector2 aabbMax))
            {
                if (showDialogs)
                {
                    EditorUtility.DisplayDialog("Center Correction", "No filled tiles to correct.", "OK");
                }

                return false;
            }

            float aabbWidth = Mathf.Max(0.0001f, aabbMax.x - aabbMin.x);
            float aabbHeight = Mathf.Max(0.0001f, aabbMax.y - aabbMin.y);
            float scale = Mathf.Min(tilesFieldSize.x / aabbWidth, tilesFieldSize.y / aabbHeight);

            Vector2 tileSize = LevelFitter.CalculateTileSize(
                tilesFieldSize,
                biggerLayerSize,
                scale,
                maxTileSize,
                dataTileSize);

            Vector2 centerAtScale1 = (aabbMin + aabbMax) * 0.5f;
            float safeTileX = Mathf.Max(0.0001f, tileSizeAtScale1.x);
            float safeTileY = Mathf.Max(0.0001f, tileSizeAtScale1.y);
            Vector2 center = new Vector2(
                centerAtScale1.x * (tileSize.x / safeTileX),
                centerAtScale1.y * (tileSize.y / safeTileY));

            float deltaX = -center.x / Mathf.Max(0.0001f, tileSize.x);
            float deltaY = -center.y / Mathf.Max(0.0001f, tileSize.y);

            level.showLayerScaleProperty.floatValue = scale;
            level.showLayerDeltaXProperty.floatValue = deltaX;
            level.showLayerDeltaYProperty.floatValue = deltaY;
            level.ApplyChanges();

            if (refreshPreview)
            {
                RefreshEditorPreviewLayout();
            }

            return true;
        }

        /// <summary>
        /// Batch Center prep: reset showLayerScale/Delta only.
        /// Does NOT clear per-layer Position Offset (Layer.deltaX/Y) — those are authored layout.
        /// showLayerDelta centers the whole board; layer deltas stay as relative offsets.
        /// </summary>
        private static void ResetLevelLayoutOffsetsForBatch(LevelGenService level)
        {
            if (level == null || level.NullLevel || level.layersProperty == null)
            {
                return;
            }

            if (level.showLayerScaleProperty != null)
            {
                level.showLayerScaleProperty.floatValue = 1f;
            }

            if (level.showLayerDeltaXProperty != null)
            {
                level.showLayerDeltaXProperty.floatValue = 0f;
            }

            if (level.showLayerDeltaYProperty != null)
            {
                level.showLayerDeltaYProperty.floatValue = 0f;
            }

            level.ApplyChanges();
        }

        private void ApplyBatchCenterCorrection()
        {
            if (levelsSerializedProperty == null || levelsHandler == null)
            {
                return;
            }

            levelsHandler.GetLoadRange(out int start, out int end);
            levelsHandler.GetLoadLevelNumbers(out int startLevel, out int endLevel);
            bool confirmed = EditorUtility.DisplayDialog(
                "Batch Center",
                $"是否确认对加载区间 {startLevel}-{endLevel} 内的关卡做 Center Correction?\n\n仅重算 showLayerScale / showLayerDelta（整体居中），不会清空各层 Position Offset（Layer Delta X/Y）。",
                "Yes",
                "No");
            if (!confirmed)
            {
                return;
            }

            int total = end >= start ? end - start + 1 : 0;
            int applied = 0;
            int skipped = 0;

            try
            {
                for (int i = start; i <= end; i++)
                {
                    int progressIndex = i - start;
                    EditorUtility.DisplayProgressBar(
                        "Batch Center",
                        $"Center Correction {progressIndex + 1}/{total} (#{i})",
                        total > 0 ? (float)progressIndex / total : 1f);

                    UnityEngine.Object levelObject = levelsSerializedProperty.GetArrayElementAtIndex(i).objectReferenceValue;
                    if (levelObject == null)
                    {
                        skipped++;
                        continue;
                    }

                    LevelGenService level = new LevelGenService(levelObject);
                    ResetLevelLayoutOffsetsForBatch(level);
                    if (TryApplyCenterCorrection(level, refreshPreview: false, showDialogs: false))
                    {
                        applied++;
                    }
                    else
                    {
                        skipped++;
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();

            if (selectedLevelGenFunc != null)
            {
                // Reload selected level properties from disk and refresh Scene preview.
                if (levelsHandler != null && levelsHandler.SelectedLevelIndex >= 0)
                {
                    levelsHandler.OpenLevel(levelsHandler.SelectedLevelIndex);
                }
                else
                {
                    RefreshEditorPreviewLayout();
                }
            }

            Debug.Log($"Batch Center finished: applied={applied}, skipped={skipped}, total={total}, range={startLevel}-{endLevel}.");
            GUI.changed = true;
        }

        private static void ResolvePreviewLayerSizes(
            LevelGenService level,
            int totalLayerCount,
            out Vector2Int evenLayerSize,
            out Vector2Int oddLayerSize)
        {
            if (totalLayerCount >= 1)
            {
                int evenLayerIndex = totalLayerCount - 1;
                evenLayerSize = new Vector2Int(
                    level.GetLayerWidth(evenLayerIndex),
                    level.GetLayerHeight(evenLayerIndex));
            }
            else
            {
                evenLayerSize = new Vector2Int(
                    level.bottomLayerWidthProperty.intValue,
                    level.bottomLayerHeightProperty.intValue);
            }

            if (totalLayerCount >= 2)
            {
                int oddLayerIndex = totalLayerCount - 2;
                oddLayerSize = new Vector2Int(
                    level.GetLayerWidth(oddLayerIndex),
                    level.GetLayerHeight(oddLayerIndex));
            }
            else
            {
                int offset = level.GetOffset();
                oddLayerSize = new Vector2Int(evenLayerSize.x + offset, evenLayerSize.y + offset);
            }
        }

        private static bool TryComputeFilledTilesAabb(
            LevelGenService level,
            int totalLayerCount,
            Vector2 tileSize,
            Vector2Int evenLayerSize,
            Vector2Int oddLayerSize,
            Vector2 showLayerDelta,
            out Vector2 aabbMin,
            out Vector2 aabbMax)
        {
            aabbMin = Vector2.zero;
            aabbMax = Vector2.zero;
            bool hasFilled = false;
            Vector2 halfSize = tileSize * 0.5f;

            for (int layerIndex = 0; layerIndex < totalLayerCount; layerIndex++)
            {
                int layerWidth = level.GetLayerWidth(layerIndex);
                int layerHeight = level.GetLayerHeight(layerIndex);
                float layerDeltaX = level.GetLayerDeltaX(layerIndex);
                float layerDeltaY = level.GetLayerDeltaY(layerIndex);

                for (int y = 0; y < layerHeight; y++)
                {
                    for (int x = 0; x < layerWidth; x++)
                    {
                        if (!level.GetCellValue(layerIndex, x, y))
                        {
                            continue;
                        }

                        Vector2 layerAndCellDelta = new Vector2(
                            layerDeltaX + level.GetCellDeltaX(layerIndex, x, y),
                            layerDeltaY + level.GetCellDeltaY(layerIndex, x, y));

                        Vector3 local = LevelFitter.GetPreviewLocalPosition(
                            new ElementPosition(x, y, layerIndex),
                            totalLayerCount,
                            tileSize,
                            evenLayerSize,
                            oddLayerSize,
                            layerAndCellDelta,
                            showLayerDelta);

                        Vector2 cellMin = new Vector2(local.x - halfSize.x, local.y - halfSize.y);
                        Vector2 cellMax = new Vector2(local.x + halfSize.x, local.y + halfSize.y);

                        if (!hasFilled)
                        {
                            aabbMin = cellMin;
                            aabbMax = cellMax;
                            hasFilled = true;
                        }
                        else
                        {
                            aabbMin = Vector2.Min(aabbMin, cellMin);
                            aabbMax = Vector2.Max(aabbMax, cellMax);
                        }
                    }
                }
            }

            return hasFilled;
        }

        private static TileGameModel FindTileGameDataInScene()
        {
            GameDirector gameController = UnityEngine.Object.FindObjectOfType<GameDirector>();
            if (gameController == null)
            {
                return null;
            }

            SerializedObject serializedObject = new SerializedObject(gameController);
            SerializedProperty dataProperty = serializedObject.FindProperty("data");
            return dataProperty != null ? dataProperty.objectReferenceValue as TileGameModel : null;
        }

        private void SetAsCurrentLevel(bool fromTestLevelButton = true)
        {
            if (fromTestLevelButton)
            {
                isEnteringPlayFromTestLevel = true;
            }

            // Unhide Level Editor preview hide before Play serializes the scene.
            EnsureGameUiActiveForPlayMode();

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                if (fromTestLevelButton)
                {
                    isEnteringPlayFromTestLevel = false;
                    HideGameUiForLevelEditor();
                }

                return;
            }

            EditorSceneManager.OpenScene(GAME_SCENE_PATH);
            EnsureGameUiActiveForPlayMode();

            GlobalSave globalSave = SaveController.GetGlobalSave();
            LevelArchive gameSave = globalSave.GetSaveObject<LevelArchive>("level");
            gameSave.RealLevelIndex = levelsHandler.SelectedLevelIndex;
            gameSave.DisplayLevelIndex = levelsHandler.SelectedLevelIndex;
            gameSave.MaxReachedLevelIndex = levelsHandler.SelectedLevelIndex;

            SaveController.SaveCustom(globalSave);
            GameDirector.AutoRunLevelInEditor = true;

            RemoveSceneRepresentation();

            if (fromTestLevelButton)
            {
                EditorApplication.isPlaying = true;
            }
            else if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.isPlaying = true;
            }
        }

        private void RemoveSceneRepresentation()
        {
            DestroyEditorPreview(includingOrphans: false);
        }

        /// <summary>
        /// Destroys the tracked preview controller and optionally any orphaned HideAndDontSave roots
        /// left behind when the controller was nulled without Destroy (invisible in Hierarchy, blank white tiles).
        /// </summary>
        private void DestroyEditorPreview(bool includingOrphans)
        {
            if (editorSceneController != null)
            {
                editorSceneController.Destroy();
                editorSceneController = null;
            }

            previousSelectedLayerIndex = int.MinValue;

            if (includingOrphans)
            {
                DestroyOrphanEditorPreviews();
            }
        }

        private static void DestroyOrphanEditorPreviews()
        {
            HashSet<GameObject> liveRoots = null;
            LevelEditorWindow[] windows = Resources.FindObjectsOfTypeAll<LevelEditorWindow>();
            for (int w = 0; w < windows.Length; w++)
            {
                LevelEditorWindow window = windows[w];
                if (window == null || window.editorSceneController == null || !window.editorSceneController.IsValid)
                {
                    continue;
                }

                liveRoots ??= new HashSet<GameObject>();
                liveRoots.Add(window.editorSceneController.Root);
            }

            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < allObjects.Length; i++)
            {
                GameObject go = allObjects[i];
                if (go == null || go.name != EditorPreviewRootName)
                {
                    continue;
                }

                // Skip assets / prefabs; only runtime editor instances.
                if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(go)))
                {
                    continue;
                }

                if (liveRoots != null && liveRoots.Contains(go))
                {
                    continue;
                }

                DestroyImmediate(go);
            }
        }

        /// <summary>
        /// While Level Editor is open in GameScene, hide game UI clutter and keep only Power Up Panel.
        /// Hides UI Main Canvas siblings (UI Complete / menus / etc.) and all UI Game children except Power Up Panel.
        /// </summary>
        private void HideGameUiForLevelEditor()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (!IsGameSceneActive())
            {
                return;
            }

            if (cachedGameUiChildActiveStates == null)
            {
                cachedGameUiChildActiveStates = new Dictionary<int, bool>();
            }

            if (cachedMainCanvasForHide == null)
            {
                cachedMainCanvasForHide = FindUIMainCanvas();
            }

            if (cachedUiGameForHide == null)
            {
                cachedUiGameForHide = FindUIGameRoot(cachedMainCanvasForHide);
                if (cachedUiGameForHide != null && cachedMainCanvasForHide == null)
                {
                    cachedMainCanvasForHide = cachedUiGameForHide.parent;
                }
            }

            Transform mainCanvas = cachedMainCanvasForHide;
            Transform uiGameRoot = cachedUiGameForHide;

            if (mainCanvas != null)
            {
                for (int i = 0; i < mainCanvas.childCount; i++)
                {
                    GameObject childGo = mainCanvas.GetChild(i).gameObject;
                    if (uiGameRoot != null && childGo.transform == uiGameRoot)
                    {
                        continue;
                    }

                    CacheAndHide(childGo);
                }
            }

            if (uiGameRoot != null)
            {
                // UI Game must stay active so Power Up Panel can render on its Canvas.
                if (!uiGameRoot.gameObject.activeSelf)
                {
                    uiGameRoot.gameObject.SetActive(true);
                }

                if (cachedPowerUpPanelForHide == null)
                {
                    cachedPowerUpPanelForHide = FindPowerUpPanel(uiGameRoot);
                }

                for (int i = 0; i < uiGameRoot.childCount; i++)
                {
                    Transform child = uiGameRoot.GetChild(i);
                    if (cachedPowerUpPanelForHide != null && child == cachedPowerUpPanelForHide)
                    {
                        if (!child.gameObject.activeSelf)
                        {
                            child.gameObject.SetActive(true);
                        }
                        continue;
                    }

                    CacheAndHide(child.gameObject);
                }
            }

            gameUiHiddenForEditor = mainCanvas != null || uiGameRoot != null;
        }

        private void CacheAndHide(GameObject go)
        {
            int id = go.GetInstanceID();
            if (!cachedGameUiChildActiveStates.ContainsKey(id))
            {
                cachedGameUiChildActiveStates[id] = go.activeSelf;
            }

            if (go.activeSelf)
            {
                go.SetActive(false);
            }
        }

        private void RestoreGameUiForLevelEditor()
        {
            if (!gameUiHiddenForEditor && cachedGameUiChildActiveStates == null)
            {
                return;
            }

            if (cachedGameUiChildActiveStates != null && cachedGameUiChildActiveStates.Count > 0)
            {
                Transform mainCanvas = cachedMainCanvasForHide != null ? cachedMainCanvasForHide : FindUIMainCanvas();
                Transform uiGameRoot = cachedUiGameForHide != null ? cachedUiGameForHide : FindUIGameRoot(mainCanvas);

                if (mainCanvas != null)
                {
                    RestoreChildrenActiveState(mainCanvas);
                }

                if (uiGameRoot != null)
                {
                    RestoreChildrenActiveState(uiGameRoot);
                }
            }

            gameUiHiddenForEditor = false;
            cachedGameUiChildActiveStates = null;
            cachedMainCanvasForHide = null;
            cachedUiGameForHide = null;
            cachedPowerUpPanelForHide = null;
        }

        private void RestoreChildrenActiveState(Transform parent)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                GameObject childGo = parent.GetChild(i).gameObject;
                if (cachedGameUiChildActiveStates.TryGetValue(childGo.GetInstanceID(), out bool wasActive))
                {
                    if (childGo.activeSelf != wasActive)
                    {
                        childGo.SetActive(wasActive);
                    }
                }
            }
        }

        private void DiscardGameUiVisibilityCache()
        {
            gameUiHiddenForEditor = false;
            rehideGameUiAfterSceneSave = false;
            cachedGameUiChildActiveStates = null;
            cachedMainCanvasForHide = null;
            cachedUiGameForHide = null;
            cachedPowerUpPanelForHide = null;
        }

        /// <summary>
        /// Before Test Level / Play: restore hide cache, then force-activate UI Main Canvas / UI Game children.
        /// </summary>
        private void EnsureGameUiActiveForPlayMode()
        {
            RestoreGameUiForLevelEditor();
            ForceActivateGameUiRootsForPlay();
        }

        private static void ForceActivateGameUiRootsForPlay()
        {
            Transform mainCanvas = FindUIMainCanvas();
            if (mainCanvas == null)
            {
                return;
            }

            if (!mainCanvas.gameObject.activeSelf)
            {
                mainCanvas.gameObject.SetActive(true);
            }

            for (int i = 0; i < mainCanvas.childCount; i++)
            {
                GameObject childGo = mainCanvas.GetChild(i).gameObject;
                if (!childGo.activeSelf)
                {
                    childGo.SetActive(true);
                }
            }

            Transform uiGameRoot = FindUIGameRoot(mainCanvas);
            if (uiGameRoot == null)
            {
                return;
            }

            if (!uiGameRoot.gameObject.activeSelf)
            {
                uiGameRoot.gameObject.SetActive(true);
            }

            for (int i = 0; i < uiGameRoot.childCount; i++)
            {
                GameObject childGo = uiGameRoot.GetChild(i).gameObject;
                if (!childGo.activeSelf)
                {
                    childGo.SetActive(true);
                }
            }
        }

        private void OnEditorSceneSaving(UnityEngine.SceneManagement.Scene scene, string path)
        {
            if (!gameUiHiddenForEditor)
            {
                return;
            }

            if (!IsGameScene(scene) && path != GAME_SCENE_PATH)
            {
                return;
            }

            // Avoid persisting temporary hide state into GameScene.
            rehideGameUiAfterSceneSave = true;
            RestoreGameUiForLevelEditor();
        }

        private void OnEditorSceneSaved(UnityEngine.SceneManagement.Scene scene)
        {
            if (!rehideGameUiAfterSceneSave)
            {
                return;
            }

            rehideGameUiAfterSceneSave = false;
            // Never re-hide while entering Test Level / Play.
            if (!EditorApplication.isPlayingOrWillChangePlaymode && !isEnteringPlayFromTestLevel && IsGameScene(scene))
            {
                HideGameUiForLevelEditor();
            }
        }

        private static bool IsGameSceneActive()
        {
            return IsGameScene(EditorSceneManager.GetActiveScene());
        }

        private static bool IsGameScene(UnityEngine.SceneManagement.Scene scene)
        {
            return scene.IsValid() && (scene.path == GAME_SCENE_PATH || scene.name == GAME_SCENE_NAME);
        }

        private static Transform FindUIMainCanvas()
        {
            Transform byName = FindSceneObjectByName(UI_MAIN_CANVAS_NAME);
            if (byName != null)
            {
                return byName;
            }

            Transform uiGame = FindUIGameRoot(null);
            return uiGame != null ? uiGame.parent : null;
        }

        private static Transform FindUIGameRoot(Transform mainCanvas)
        {
            if (mainCanvas != null)
            {
                for (int i = 0; i < mainCanvas.childCount; i++)
                {
                    Transform child = mainCanvas.GetChild(i);
                    if (child.name == UI_GAME_OBJECT_NAME || child.GetComponent<UIGame>() != null)
                    {
                        return child;
                    }
                }
            }

            UIGame[] uiGames = Resources.FindObjectsOfTypeAll<UIGame>();
            for (int i = 0; i < uiGames.Length; i++)
            {
                UIGame uiGame = uiGames[i];
                if (uiGame == null || uiGame.gameObject == null)
                {
                    continue;
                }

                GameObject go = uiGame.gameObject;
                if (!go.scene.IsValid() || !go.scene.isLoaded)
                {
                    continue;
                }

                return go.transform;
            }

            return FindSceneObjectByName(UI_GAME_OBJECT_NAME);
        }

        private static Transform FindPowerUpPanel(Transform uiGameRoot)
        {
            if (uiGameRoot == null)
            {
                return null;
            }

            for (int i = 0; i < uiGameRoot.childCount; i++)
            {
                Transform child = uiGameRoot.GetChild(i);
                if (child.name == POWER_UP_PANEL_NAME || child.GetComponent<PUUIDirector>() != null)
                {
                    return child;
                }
            }

            return null;
        }

        private static Transform FindSceneObjectByName(string objectName)
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform found = FindChildRecursive(roots[i].transform, objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Transform FindChildRecursive(Transform parent, string objectName)
        {
            if (parent.name == objectName)
            {
                return parent;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindChildRecursive(parent.GetChild(i), objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        #endregion

        #region Edit Layers Tab
        private void DrawEditLayersTab()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(TEST_LEVEL, EditorCustomStyles.button))
            {
                SetAsCurrentLevel();
            }

            if (GUILayout.Button("刷新命名", EditorCustomStyles.button, GUILayout.Width(72)))
            {
                ApplyOpenLevelFileName();
            }

            if (GUILayout.Button("自定义命名", EditorCustomStyles.button, GUILayout.Width(84)))
            {
                LevelCustomNamePopup.Open(this, levelsHandler != null ? levelsHandler.SelectedAssetName() : "");
            }

            GUILayout.Space(20);

            EditorGUILayout.LabelField(levelLabel, EditorCustomStyles.labelMedium);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(editColorsContent, GUILayout.Width(20), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
            {
                drawEditVariablesTabDisplayed = true;
            }

            EditorGUILayout.EndHorizontal();

            // Fixed settings viewport; remaining space goes to the board below.
            levelEditSettingsScroll = EditorGUILayout.BeginScrollView(
                levelEditSettingsScroll,
                GUILayout.Height(LEVEL_EDIT_SETTINGS_SCROLL_HEIGHT));

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Add Layer", EditorCustomStyles.buttonBlue))
            {
                // With only 1 layer, former UI offered larger/smaller — default to larger.
                // After that, keep alternating sizes via AddLayer() (SetCorrectLevelSize).
                if (selectedLevelGenFunc.layersProperty.arraySize == 1)
                {
                    selectedLevelGenFunc.AddLayer(1);
                }
                else
                {
                    selectedLevelGenFunc.AddLayer();
                }

                UpdateLayersOptions();
                selectedLayerIndex = 0;
                selectedLevelGenFunc.ValidateLevel();
                InitCellsForGameView();
                previousSelectedLayerIndex = int.MinValue;
            }

            if (GUILayout.Button("Center Correction", EditorCustomStyles.buttonBlue))
            {
                ApplyCenterCorrection();
            }

            if (GUILayout.Button("Batch Center", EditorCustomStyles.buttonBlue))
            {
                ApplyBatchCenterCorrection();
            }

            // Remove / Move layer actions remain available via the layer list context menu (right-click).
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Right-column fields (Bottom Layer Height / Same Color List Str) start here; Offset X/Y aligns to this.
            const float pairedFieldWidth = 260f;
            const float pairedFieldRightOffset = 100f;
            float offsetControlsAlignX = pairedFieldWidth + pairedFieldRightOffset;

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(selectedLevelGenFunc.bottomLayerWidthProperty, GUILayout.Width(pairedFieldWidth));
            bool bottomLayerSizeChanged = EditorGUI.EndChangeCheck();
            GUILayout.Space(pairedFieldRightOffset);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(selectedLevelGenFunc.bottomLayerHeightProperty, GUILayout.Width(pairedFieldWidth));
            bottomLayerSizeChanged |= EditorGUI.EndChangeCheck();
            EditorGUILayout.EndHorizontal();

            if (bottomLayerSizeChanged)
            {
                selectedLevelGenFunc.HandleSizePropertyChange();
                UpdateLayersOptions();
                selectedLevelGenFunc.ValidateLevel();
                InitCellsForGameView();
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(selectedLevelGenFunc.elementsPerLevelProperty, GUILayout.Width(pairedFieldWidth));
            bool elementsPerLevelChanged = EditorGUI.EndChangeCheck();
            GUILayout.Space(pairedFieldRightOffset);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(
                selectedLevelGenFunc.sameColorListStrProperty,
                new GUIContent("Same Color (Deprecated)", "已废弃。运行时忽略关卡上的 sameColorListStr。同色只认难度配置第三段指向的同色表。"),
                GUILayout.Width(pairedFieldWidth));
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            if (elementsPerLevelChanged)
            {
                // Prefab pool size only affects next full rebuild; avoid Instantiating the whole board.
                if (editorSceneController != null && editorSceneController.IsValid)
                {
                    editorSceneController.setsAmount = selectedLevelGenFunc.SetsAmount;
                }
            }

            easyHardFoldoutExpanded = EditorGUILayout.Foldout(easyHardFoldoutExpanded, "Easy / Hard", true);
            if (easyHardFoldoutExpanded)
            {
                EditorGUI.indentLevel++;
                // Placeholder content: single input for now; more options can be added later.
                EditorGUILayout.PropertyField(selectedLevelGenFunc.easyHardValueProperty, new GUIContent("Value"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(selectedLevelGenFunc.useInRandomizerProperty);

            bool showLayoutChanged = false;
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(selectedLevelGenFunc.showLayerScaleProperty, GUILayout.Width(260));
            showLayoutChanged |= EditorGUI.EndChangeCheck();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(selectedLevelGenFunc.showLayerDeltaXProperty, GUILayout.Width(260));
            EditorGUILayout.PropertyField(selectedLevelGenFunc.showLayerDeltaYProperty, GUILayout.Width(260));
            showLayoutChanged |= EditorGUI.EndChangeCheck();
            EditorGUILayout.EndHorizontal();

            if (showLayoutChanged)
            {
                // Preview-only refresh; ApplyChanges happens once via GUI.changed at end of tab.
                RefreshEditorPreviewLayout();
            }

            selectedLevelGenFunc.DisplayProperties();

            EditorGUILayout.EndScrollView();

            // Tool / layer-offset controls stay outside the settings scroll (fixed below it).
            // 当层级发生变化时，从层级数据中读取delta值
            if (previousSelectedLayerIndex != selectedLayerIndex)
            {
                ClearSelectedCells();
                cachedCurrentLayerTileCount = -1;
                deltaX = selectedLevelGenFunc.GetLayerDeltaX(selectedLayerIndex);
                deltaY = selectedLevelGenFunc.GetLayerDeltaY(selectedLayerIndex);
                showDeltaSettings = true;
                RefreshBoxCoverPreview();
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Tool:", GUILayout.Width(40));
            // While Ctrl is held, show Select as selected even if the toolbar pin is still Draw.
            EditorToolMode displayedToolMode = ctrlHeldForSelect ? EditorToolMode.Select : editorToolMode;
            EditorGUI.BeginDisabledGroup(boxCoverMode);
            EditorToolMode newToolMode = (EditorToolMode)GUILayout.Toolbar((int)displayedToolMode, TOOL_MODE_LABELS, GUILayout.Width(160));
            EditorGUI.EndDisabledGroup();
            if (newToolMode != displayedToolMode)
            {
                SetEditorToolMode(newToolMode);
            }
            EditorGUILayout.EndHorizontal();

            DrawBoxCoverTools();

            bool selectMode = IsSelectModeActive();
            float rowH = EditorGUIUtility.singleLineHeight * 1.5f;
            GUIStyle offsetTitleStyle = new GUIStyle(EditorCustomStyles.labelMediumBold)
            {
                alignment = TextAnchor.MiddleLeft,
                fixedHeight = rowH
            };
            GUIStyle offsetLabelStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fixedHeight = rowH
            };
            int offsetFieldFontSize = Mathf.RoundToInt(EditorStyles.numberField.fontSize > 0
                ? EditorStyles.numberField.fontSize * 1.5f
                : 18f);
            GUIStyle offsetFieldStyle = new GUIStyle(EditorStyles.numberField)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = offsetFieldFontSize,
                fixedHeight = rowH
            };

            if (selectMode)
            {
                // Select: always show Tiles Offset on one row (vertically centered).
                const float tilesOffsetTitleWidth = 140f;
                EditorGUILayout.BeginHorizontal(GUILayout.Height(rowH));
                GUILayout.Label($"Tiles Offset ({selectedCells.Count})", offsetTitleStyle, GUILayout.Width(tilesOffsetTitleWidth), GUILayout.Height(rowH));
                GUILayout.Space(Mathf.Max(0f, offsetControlsAlignX - tilesOffsetTitleWidth));
                GUILayout.Label("Offset X:", offsetLabelStyle, GUILayout.Width(60), GUILayout.Height(rowH));
                selectionOffsetX = RoundOffsetThreeDecimal(EditorGUILayout.FloatField(selectionOffsetX, offsetFieldStyle, GUILayout.Width(60), GUILayout.Height(rowH)));
                GUILayout.Label("Offset Y:", offsetLabelStyle, GUILayout.Width(60), GUILayout.Height(rowH));
                selectionOffsetY = RoundOffsetThreeDecimal(EditorGUILayout.FloatField(selectionOffsetY, offsetFieldStyle, GUILayout.Width(60), GUILayout.Height(rowH)));
                EditorGUI.BeginDisabledGroup(selectedCells.Count == 0);
                if (GUILayout.Button("Apply", EditorCustomStyles.button, GUILayout.Width(96), GUILayout.Height(rowH)))
                {
                    ApplyOffsetToSelectedCells(selectionOffsetX, selectionOffsetY);
                }
                EditorGUI.EndDisabledGroup();
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            else if (selectedLayerIndex >= 0 && selectedLayerIndex < selectedLevelGenFunc.layersProperty.arraySize)
            {
                // Draw: always show Layer Position Offset on one row (vertically centered).
                const float positionOffsetTitleWidth = 180f;
                EditorGUILayout.BeginHorizontal(GUILayout.Height(rowH));
                GUILayout.Label($"Layer {selectedLayerIndex + 1} Position Offset", offsetTitleStyle, GUILayout.Width(positionOffsetTitleWidth), GUILayout.Height(rowH));
                GUILayout.Space(Mathf.Max(0f, offsetControlsAlignX - positionOffsetTitleWidth));
                GUILayout.Label("Delta X:", offsetLabelStyle, GUILayout.Width(55), GUILayout.Height(rowH));
                deltaX = EditorGUILayout.FloatField(deltaX, offsetFieldStyle, GUILayout.Width(60), GUILayout.Height(rowH));
                GUILayout.Label("Delta Y:", offsetLabelStyle, GUILayout.Width(55), GUILayout.Height(rowH));
                deltaY = EditorGUILayout.FloatField(deltaY, offsetFieldStyle, GUILayout.Width(60), GUILayout.Height(rowH));
                if (GUILayout.Button("Apply", EditorCustomStyles.button, GUILayout.Width(96), GUILayout.Height(rowH)))
                {
                    ApplyDeltaToLayer(selectedLayerIndex, deltaX, deltaY);
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(16f);
            EditorGUILayout.BeginHorizontal();

            if (selectedLayerIndex != -1)
            {
                DrawLayer();
            }
            else
            {
                GUILayout.FlexibleSpace();
            }

            HandleChangingButtonWidth();
            DrawLayersSideList();

            EditorGUILayout.EndHorizontal();

            // After board draw so isPointerOverBoard is current; do not steal ScrollView wheel.
            HandleScrollEvent();

            if (GUI.changed)
            {
                // Board paint applies on MouseUp via EndBoardCellInteraction.
                // Never rebuild all level labels here — with 1000+ levels that hitch every cell click.
                if (!isDraggingCells
                    && !validationNeededAfterDrag
                    && !boxCoverStrokePending
                    && selectedLevelGenFunc != null)
                {
                    selectedLevelGenFunc.ApplyChanges();
                    RefreshCurrentLevelLabelOnly();
                }
            }

            DrawTipsAndWarnings();

            EditorGUILayout.EndVertical();
        }

        private void DrawBoxCoverTools()
        {
            bool hasLayer = selectedLevelGenFunc != null
                && selectedLayerIndex >= 0
                && selectedLayerIndex < selectedLevelGenFunc.layersProperty.arraySize;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            bool newBoxCoverMode = GUILayout.Toggle(
                boxCoverMode,
                "Box Cover Mode",
                GUI.skin.button,
                GUILayout.Width(130f));
            if (newBoxCoverMode != boxCoverMode)
            {
                SetBoxCoverMode(newBoxCoverMode);
            }

            GUILayout.Space(8f);
            EditorGUILayout.LabelField("Type:", GUILayout.Width(36f));
            EditorGUI.BeginDisabledGroup(!hasLayer);
            BoxCoverType currentType = hasLayer
                ? selectedLevelGenFunc.GetBoxCoverType()
                : BoxCoverType.Coke;
            int newTypeIndex = GUILayout.Toolbar(
                (int)currentType,
                BOX_COVER_TYPE_LABELS,
                GUILayout.Width(72f));
            EditorGUI.EndDisabledGroup();
            BoxCoverType newType = (BoxCoverType)Mathf.Clamp(
                newTypeIndex,
                0,
                BOX_COVER_TYPE_LABELS.Length - 1);
            if (hasLayer && newType != currentType && selectedLevelGenFunc.SetBoxCoverType(newType))
            {
                CommitBoxCoverChange("Set Box Cover Type");
                RefreshBoxCoverPreview();
            }

            GUILayout.Space(8f);
            EditorGUILayout.LabelField("Size:", GUILayout.Width(34f));
            EditorGUI.BeginDisabledGroup(!hasLayer);
            int newSizeIndex = GUILayout.Toolbar(
                (int)selectedBoxCoverSize,
                BOX_COVER_SIZE_LABELS,
                GUILayout.Width(180f));
            EditorGUI.EndDisabledGroup();
            BoxCoverSize newSize = (BoxCoverSize)Mathf.Clamp(
                newSizeIndex,
                0,
                BOX_COVER_SIZE_LABELS.Length - 1);
            if (newSize != selectedBoxCoverSize)
            {
                EndBoardCellInteraction();
                selectedBoxCoverSize = newSize;
                if (editorSceneController != null && editorSceneController.IsValid)
                {
                    editorSceneController.UpdateBoxCoverSelection(
                        selectedLayerIndex,
                        selectedBoxCoverSize);
                }
                Repaint();
            }

            GUILayout.FlexibleSpace();
            if (boxCoverMode && selectedLayerIndex == 0)
            {
                GUIStyle warningStyle = new GUIStyle(EditorStyles.boldLabel);
                warningStyle.normal.textColor = INVALID_BOX_COVER_COLOR;
                GUILayout.Label("Top layer is not allowed", warningStyle);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginDisabledGroup(!hasLayer);
            EditorGUILayout.LabelField("Count:", GUILayout.Width(42f));
            int currentCount = hasLayer
                ? selectedLevelGenFunc.GetBoxCoverCount(selectedLayerIndex, selectedBoxCoverSize)
                : 0;
            EditorGUI.BeginChangeCheck();
            int newCount = EditorGUILayout.IntField(currentCount, GUILayout.Width(56f));
            bool countChanged = EditorGUI.EndChangeCheck();
            if (countChanged
                && selectedLevelGenFunc.SetBoxCoverCount(
                    selectedLayerIndex,
                    selectedBoxCoverSize,
                    Mathf.Max(0, newCount)))
            {
                CommitBoxCoverChange("Set Box Cover Count");
            }

            int candidateCount = hasLayer
                ? selectedLevelGenFunc.GetBoxCoverCandidateCount(
                    selectedLayerIndex,
                    selectedBoxCoverSize)
                : 0;
            EditorGUILayout.LabelField($"Candidates: {candidateCount}", GUILayout.Width(92f));
            EditorGUI.EndDisabledGroup();

            GUILayout.FlexibleSpace();
            EditorGUI.BeginDisabledGroup(!hasLayer);
            if (GUILayout.Button(
                new GUIContent(
                    "Clear Effects",
                    "Clears tile effects and Box Covers on this layer."),
                GUILayout.Width(90f)))
            {
                ClearSelectedLayerEffects();
            }

            if (GUILayout.Button("Clear Covers", GUILayout.Width(90f)))
            {
                ClearSelectedLayerBoxCovers();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void CommitBoxCoverChange(string undoName)
        {
            if (selectedLevelGenFunc == null)
            {
                return;
            }

            Undo.SetCurrentGroupName(undoName);
            selectedLevelGenFunc.ApplyChanges(registerUndo: true);
            selectedLevelGenFunc.ValidateLevel(checkDataIdUniqueness: false);
            RefreshBoxCoverPreview();
            RefreshCurrentLevelLabelOnly();
            GUI.changed = true;
        }

        private void RefreshCurrentLevelLabelOnly()
        {
            if (selectedLevelGenFunc == null || levelsHandler == null)
            {
                return;
            }

            levelLabel = selectedLevelGenFunc.GetLevelLabel(levelsHandler.SelectedLevelIndex, stringBuilder);
            levelsHandler.UpdateCurrentLevelLabel(levelLabel);
        }

        private void ApplyOpenLevelFileName()
        {
            ApplyNamedLevelFile(null);
        }

        internal void ApplyCustomLevelName(string name)
        {
            ApplyNamedLevelFile(name);
        }

        private void ApplyNamedLevelFile(string customName)
        {
            if (selectedLevelGenFunc == null || levelsHandler == null || levelsHandler.SelectedLevelIndex < 0)
            {
                return;
            }

            selectedLevelGenFunc.ApplyChanges();
            bool renamed = customName == null
                ? levelsHandler.RenameSelectedLevelToLayout()
                : levelsHandler.RenameSelectedLevel(customName);
            if (renamed)
            {
                RefreshCurrentLevelLabelOnly();
            }
        }

        private class LevelCustomNamePopup : EditorWindow
        {
            private const string NameControl = "level-custom-name";
            private LevelEditorWindow owner;
            private string nameText = "";
            private bool focused;

            public static void Open(LevelEditorWindow owner, string currentName)
            {
                LevelCustomNamePopup window = CreateInstance<LevelCustomNamePopup>();
                window.owner = owner;
                window.nameText = currentName ?? "";
                window.titleContent = new GUIContent("自定义命名");
                Rect host = owner != null ? owner.position : new Rect(200, 200, 0, 0);
                window.position = new Rect(host.x + 80f, host.y + 80f, 360f, 96f);
                window.minSize = new Vector2(320f, 96f);
                window.maxSize = new Vector2(520f, 96f);
                EditorApplication.delayCall += window.ShowModalUtility;
            }

            private void OnGUI()
            {
                EditorGUILayout.Space();
                GUI.SetNextControlName(NameControl);
                nameText = EditorGUILayout.TextField("文件名", nameText);
                if (!focused)
                {
                    EditorGUI.FocusTextInControl(NameControl);
                    focused = true;
                }

                Event current = Event.current;
                if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape)
                {
                    Close();
                    current.Use();
                    return;
                }

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                bool confirm = GUILayout.Button("确定", GUILayout.Width(72))
                    || (current.type == EventType.KeyDown && (current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter));
                if (confirm)
                {
                    if (owner != null)
                    {
                        owner.ApplyCustomLevelName(nameText);
                    }

                    Close();
                    current.Use();
                }

                if (GUILayout.Button("取消", GUILayout.Width(72)))
                {
                    Close();
                }

                EditorGUILayout.EndHorizontal();
            }
        }



        private void HandleScrollEvent()
        {
            currentEvent = Event.current;

            if (currentEvent.type != EventType.ScrollWheel || currentEvent.delta.y == 0)
            {
                return;
            }

            // Only switch layers when the pointer is over the board, so the settings ScrollView can scroll.
            if (!isPointerOverBoard || layersOptions == null || layersOptions.Length == 0)
            {
                return;
            }

            if (currentEvent.delta.y < 0) // scroll up
            {
                if (selectedLayerIndex > 0)
                {
                    selectedLayerIndex--;
                }
            }
            else if (selectedLayerIndex < layersOptions.Length - 1)
            {
                selectedLayerIndex++;
            }

            currentEvent.Use();
            Repaint();
        }

        private void DrawLayer()
        {
            drawRect = EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            drawRect.xMin += 4;
            currentOffset = selectedLevelGenFunc.GetOffset();
            maxLayerHeight = Mathf.Max(selectedLevelGenFunc.bottomLayerHeightProperty.intValue, selectedLevelGenFunc.bottomLayerHeightProperty.intValue + currentOffset);
            maxLayerWidth = Mathf.Max(selectedLevelGenFunc.bottomLayerWidthProperty.intValue, selectedLevelGenFunc.bottomLayerWidthProperty.intValue + currentOffset);
            xSize = Mathf.Floor(drawRect.width / maxLayerWidth);
            ySize = Mathf.Floor(drawRect.height / maxLayerHeight);
            elementSize = Mathf.Min(xSize, ySize);
            selectedLayerWidth = selectedLevelGenFunc.GetLayerWidth(selectedLayerIndex);
            selectedLayerHeight = selectedLevelGenFunc.GetLayerHeight(selectedLayerIndex);
            halfElementSize = elementSize / 2f;

            if (selectedLayerHeight == maxLayerHeight)
            {
                elementOffset = 0;
            }
            else
            {
                elementOffset = halfElementSize;
            }


            currentEvent = Event.current;
            // Layout pass often returns a zero/invalid rect; only cache size on Repaint.
            if (currentEvent.type == EventType.Repaint)
            {
                Rect boardRect = new Rect(drawRect.position.x, drawRect.position.y, elementSize * maxLayerWidth, elementSize * maxLayerHeight);
                if (boardRect.width > 1f && boardRect.height > 1f)
                {
                    cachedBoardRect = boardRect;
                }
            }

            // Inside scroll-view GUI space — keep hover flag fresh for the next KeyDown hotkey pass.
            if (cachedBoardRect.width > 1f)
            {
                isPointerOverBoard = cachedBoardRect.Contains(currentEvent.mousePosition);
            }
            else
            {
                isPointerOverBoard = false;
            }

            if (boxCoverMode
                && isPointerOverBoard
                && currentEvent.type == EventType.MouseMove)
            {
                Repaint();
            }

            if (!buttonSeparatorIsDragged && !isDraggingLayerList)
            {
                if (currentEvent.type == EventType.MouseDown
                    && (currentEvent.button == 0 || currentEvent.button == 1)
                    && TryGetBoardCellAtMouse(currentEvent.mousePosition, out int clickColumn, out int clickRow))
                {
                    ReleaseTextFieldFocus();
                    bool boxCoverChanged = false;
                    if (boxCoverMode)
                    {
                        boxCoverChanged = currentEvent.button == 0
                            ? selectedLevelGenFunc.TryAddBoxCoverCandidate(
                                selectedLayerIndex,
                                selectedBoxCoverSize,
                                new Vector2Int(clickColumn, clickRow))
                            : selectedLevelGenFunc.TryRemoveBoxCoverCandidateAtCell(
                                selectedLayerIndex,
                                selectedBoxCoverSize,
                                new Vector2Int(clickColumn, clickRow));
                        boxCoverStrokePending |= boxCoverChanged;
                    }
                    else if (currentEvent.button == 0)
                    {
                        HandleBoardCellLeftClick(clickColumn, clickRow);
                    }
                    else
                    {
                        HandleBoardCellRightClick(clickColumn, clickRow);
                    }

                    if (currentEvent.button == 0)
                    {
                        // Seed drag mask so a follow-up MouseDrag does not toggle the same cell twice.
                        dragArray = new bool[selectedLayerWidth, selectedLayerHeight];
                        int visualY = selectedLayerHeight - clickRow - 1;
                        if (visualY >= 0 && visualY < selectedLayerHeight)
                        {
                            dragArray[clickColumn, visualY] = true;
                        }

                        // Do not lock list drag here — only lock once an actual board MouseDrag starts.
                        isDraggingCells = true;
                    }

                    currentEvent.Use();
                    GUI.changed |= !boxCoverMode || boxCoverChanged;
                    Repaint();
                }
                else if ((currentEvent.type == EventType.MouseDrag) && (currentEvent.button == 0) && isDraggingCells)
                {
                    if (!levelsHandler.IgnoreDragEvents)
                    {
                        levelsHandler.IgnoreDragEvents = true;
                    }

                    if (dragArray == null)
                    {
                        dragArray = new bool[selectedLayerWidth, selectedLayerHeight];
                    }

                    // 应用delta偏移到鼠标位置计算（基于单元格宽高的乘积）
                    float adjustedOffsetX = elementOffset + (deltaX * elementSize);
                    float adjustedOffsetY = elementOffset - (deltaY * elementSize); // deltaY正值向上移动
                    elementUnderMouseIndex = (currentEvent.mousePosition - drawRect.position - new Vector2(adjustedOffsetX, adjustedOffsetY)) / (elementSize);

                    elementPosition = new Vector2Int(Mathf.FloorToInt(elementUnderMouseIndex.x), Mathf.FloorToInt(elementUnderMouseIndex.y));

                    if ((elementPosition.x >= 0) && (elementPosition.x < selectedLayerWidth) && (elementPosition.y >= 0) && (elementPosition.y < selectedLayerHeight) && (!dragArray[elementPosition.x, elementPosition.y]))
                    {
                        int dataY = selectedLayerHeight - elementPosition.y - 1;
                        bool interacted = false;
                        if (boxCoverMode)
                        {
                            interacted = selectedLevelGenFunc.TryAddBoxCoverCandidate(
                                selectedLayerIndex,
                                selectedBoxCoverSize,
                                new Vector2Int(elementPosition.x, dataY));
                            boxCoverStrokePending |= interacted;
                        }
                        else if (IsSelectModeActive())
                        {
                            if (selectedLevelGenFunc.GetCellValue(selectedLayerIndex, elementPosition.x, dataY))
                            {
                                selectedCells.Add(new Vector2Int(elementPosition.x, dataY));
                                SyncSelectionOffsetFields();
                                interacted = true;
                            }
                        }
                        else if (selectedCells.Count == 0)
                        {
                            tempBoolValue = selectedLevelGenFunc.ToggleCellValue(selectedLayerIndex, elementPosition.x, dataY);
                            if (editorSceneController != null && editorSceneController.IsValid)
                            {
                                editorSceneController.UpdateCells(selectedLayerIndex, elementPosition.x, dataY, tempBoolValue);
                            }
                            selectedLevelGenFunc.AdjustElementCounter(tempBoolValue ? 1 : -1);
                            if (cachedCurrentLayerTileCount >= 0)
                            {
                                cachedCurrentLayerTileCount = Mathf.Max(0, cachedCurrentLayerTileCount + (tempBoolValue ? 1 : -1));
                            }
                            validationNeededAfterDrag = true;
                            interacted = true;
                        }

                        dragArray[elementPosition.x, elementPosition.y] = true;
                        if (interacted)
                        {
                            currentEvent.Use();
                            GUI.changed = true;
                        }
                    }
                }
                else if (currentEvent.type == EventType.MouseUp || currentEvent.rawType == EventType.MouseUp)
                {
                    EndBoardCellInteraction();
                }
            }
            else if (currentEvent.type == EventType.MouseUp || currentEvent.rawType == EventType.MouseUp)
            {
                EndBoardCellInteraction();
            }

            // Heavy board painting only on Repaint — skip Layout/MouseMove scans.
            if (currentEvent.type == EventType.Repaint)
            {
                DrawLayerVisuals();
            }

            GUI.color = defaultColor;
            EditorGUILayout.EndVertical();

            GUILayout.Space(BOTTOM_PADDING);
        }

        private bool TryGetBoardCellAtMouse(Vector2 mousePosition, out int columnIndex, out int rowIndex)
        {
            // Same grid mapping as drag painting (layer delta only; per-cell offset ignored for hit-test).
            float adjustedOffsetX = elementOffset + (deltaX * elementSize);
            float adjustedOffsetY = elementOffset - (deltaY * elementSize);
            Vector2 local = (mousePosition - drawRect.position - new Vector2(adjustedOffsetX, adjustedOffsetY)) / elementSize;
            int visualX = Mathf.FloorToInt(local.x);
            int visualY = Mathf.FloorToInt(local.y);
            if (visualX < 0 || visualX >= selectedLayerWidth || visualY < 0 || visualY >= selectedLayerHeight)
            {
                columnIndex = -1;
                rowIndex = -1;
                return false;
            }

            columnIndex = visualX;
            rowIndex = selectedLayerHeight - visualY - 1;
            return true;
        }

        private void HandleBoardCellLeftClick(int columnIndex, int rowIndex)
        {
            bool cellHasTile = selectedLevelGenFunc.GetCellValue(selectedLayerIndex, columnIndex, rowIndex);

            if (IsSelectModeActive())
            {
                if (cellHasTile)
                {
                    Vector2Int cell = new Vector2Int(columnIndex, rowIndex);
                    if (!selectedCells.Add(cell))
                    {
                        selectedCells.Remove(cell);
                    }

                    SyncSelectionOffsetFields();
                }
                else
                {
                    // Empty cell exits Select back to Draw.
                    ExitSelectToDraw();
                }

                return;
            }

            tempBoolValue = selectedLevelGenFunc.ToggleCellValue(selectedLayerIndex, columnIndex, rowIndex);
            if (editorSceneController != null && editorSceneController.IsValid)
            {
                editorSceneController.UpdateCells(selectedLayerIndex, columnIndex, rowIndex, tempBoolValue);
            }
            selectedLevelGenFunc.AdjustElementCounter(tempBoolValue ? 1 : -1);
            if (cachedCurrentLayerTileCount >= 0)
            {
                cachedCurrentLayerTileCount = Mathf.Max(0, cachedCurrentLayerTileCount + (tempBoolValue ? 1 : -1));
            }
            // Defer label refresh until MouseUp — avoids hitch between rapid clicks.
            validationNeededAfterDrag = true;
        }

        private void HandleBoardCellRightClick(int columnIndex, int rowIndex)
        {
            if (!selectedLevelGenFunc.GetCellValue(selectedLayerIndex, columnIndex, rowIndex))
            {
                return;
            }

            tempExtraProp = gridHandler.GetExtraProp(selectedLevelGenFunc.GetCellEffect(selectedLayerIndex, columnIndex, rowIndex));
            GenericMenu menu = new GenericMenu();

            menuIndex1 = selectedLayerIndex;
            menuIndex2 = columnIndex;
            menuIndex3 = rowIndex;

            foreach (CellTypesHandler.ExtraProp el in gridHandler.extraProps)
            {
                menu.AddItem(new GUIContent(el.label), el.value == tempExtraProp.value, delegate
                {
                    selectedLevelGenFunc.SetCellEffect(menuIndex1, menuIndex2, menuIndex3, el.value);
                    Undo.SetCurrentGroupName("Set Cell Effect");
                    selectedLevelGenFunc.ApplyChanges(registerUndo: true);
                    // Skip FindAssets uniqueness scan — paint/effect edits do not change dataId.
                    selectedLevelGenFunc.ValidateLevel(checkDataIdUniqueness: false);
                    RefreshCurrentLevelLabelOnly();
                    InitCellsForGameView();
                    Repaint();
                });
            }

            menu.ShowAsContext();
        }

        private void DrawLayerVisuals()
        {
            GUI.color = backgroundColorSerializedProperty.colorValue;
            GUI.DrawTexture(new Rect(drawRect.position.x, drawRect.position.y, elementSize * maxLayerWidth, elementSize * maxLayerHeight), Texture2D.whiteTexture);

            float adjustedOffsetX = elementOffset + (deltaX * elementSize);
            float adjustedOffsetY = elementOffset - (deltaY * elementSize);
            bool hasSelection = selectedCells.Count > 0;

            for (int rowIndex = selectedLayerHeight - 1; rowIndex >= 0; rowIndex--)
            {
                for (int columnIndex = 0; columnIndex < selectedLayerWidth; columnIndex++)
                {
                    float cellDeltaX = selectedLevelGenFunc.GetCellDeltaX(selectedLayerIndex, columnIndex, rowIndex);
                    float cellDeltaY = selectedLevelGenFunc.GetCellDeltaY(selectedLayerIndex, columnIndex, rowIndex);
                    buttonRectX = drawRect.position.x + columnIndex * elementSize + adjustedOffsetX + cellDeltaX * elementSize;
                    buttonRectY = drawRect.position.y + (selectedLayerHeight - rowIndex - 1) * elementSize + adjustedOffsetY - cellDeltaY * elementSize;
                    buttonRect = new Rect(buttonRectX, buttonRectY, elementSize, elementSize);

                    GUI.color = selectedLevelGenFunc.GetCellValue(selectedLayerIndex, columnIndex, rowIndex)
                        ? selectedColorSerializedProperty.colorValue
                        : unselectedColorSerializedProperty.colorValue;
                    GUI.DrawTexture(buttonRect, Texture2D.whiteTexture);

                    tempExtraProp = gridHandler.GetExtraProp(selectedLevelGenFunc.GetCellEffect(selectedLayerIndex, columnIndex, rowIndex));
                    if (tempExtraProp.value != 0)
                    {
                        GUI.color = defaultColor;
                        GUI.Label(buttonRect, tempExtraProp.label, gridHandler.GetLabelStyle(GUI.color));
                    }

                    if (hasSelection && selectedCells.Contains(new Vector2Int(columnIndex, rowIndex)))
                    {
                        DrawSelectedCellOutline(buttonRect);
                    }
                }
            }

            HandleTint();

            GUI.color = gridColorSerializedProperty.colorValue;
            float gridOffsetX = elementOffset + (deltaX * elementSize);
            float gridOffsetY = elementOffset - (deltaY * elementSize);

            for (int i = 0; i <= selectedLayerWidth; i++)
            {
                GUI.DrawTexture(new Rect(drawRect.position.x + gridOffsetX + elementSize * i, drawRect.position.y + gridOffsetY, 1, elementSize * selectedLayerHeight), Texture2D.whiteTexture);
            }

            for (int i = 0; i <= selectedLayerHeight; i++)
            {
                GUI.DrawTexture(new Rect(drawRect.position.x + gridOffsetX, drawRect.position.y + gridOffsetY + elementSize * i, elementSize * selectedLayerWidth, 1), Texture2D.whiteTexture);
            }

            Color previousGuiColor = GUI.color;
            GUI.color = Color.white;
            DrawBoxCoverCandidates();
            GUI.color = previousGuiColor;
        }

        private void DrawBoxCoverCandidates()
        {
            if (selectedLevelGenFunc == null || selectedLayerIndex < 0)
            {
                return;
            }

            boxCoverCandidates.Clear();
            selectedLevelGenFunc.CollectBoxCoverCandidates(selectedLayerIndex, boxCoverCandidates);
            Rect gridRect = GetCurrentLayerGridRect();

            for (int i = 0; i < boxCoverCandidates.Count; i++)
            {
                BoxCoverCandidateView candidate = boxCoverCandidates[i];
                Rect candidateRect = GetBoxCoverCandidateRect(candidate.anchor, candidate.size);
                Rect visibleRect = IntersectRect(candidateRect, gridRect);
                if (visibleRect.width <= 0f || visibleRect.height <= 0f)
                {
                    continue;
                }

                bool valid = IsBoxCoverCandidateValidForDrawing(i);
                bool selectedSize = boxCoverMode && candidate.size == selectedBoxCoverSize;
                Color color = valid ? GetBoxCoverColor(candidate.size) : INVALID_BOX_COVER_COLOR;
                Color fillColor = color;
                fillColor.a = selectedSize ? 0.34f : 0.16f;
                EditorGUI.DrawRect(visibleRect, fillColor);
                DrawRectOutline(
                    visibleRect,
                    color,
                    selectedSize ? 3f : 1.5f);

                GUIStyle labelStyle = new GUIStyle(EditorStyles.whiteBoldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    clipping = TextClipping.Clip,
                };
                GUI.Label(
                    visibleRect,
                    BoxCoverSizeUtility.GetDisplayName(candidate.size),
                    labelStyle);
            }

            if (!boxCoverMode
                || !TryGetBoardCellAtMouse(
                    currentEvent.mousePosition,
                    out int hoverColumn,
                    out int hoverRow))
            {
                return;
            }

            Vector2Int hoverAnchor = new Vector2Int(hoverColumn, hoverRow);
            bool canAdd = selectedLevelGenFunc.CanAddBoxCoverCandidate(
                selectedLayerIndex,
                selectedBoxCoverSize,
                hoverAnchor);
            Rect hoverRect = IntersectRect(
                GetBoxCoverCandidateRect(hoverAnchor, selectedBoxCoverSize),
                gridRect);
            if (hoverRect.width <= 0f || hoverRect.height <= 0f)
            {
                return;
            }

            Color hoverColor = canAdd
                ? GetBoxCoverColor(selectedBoxCoverSize)
                : INVALID_BOX_COVER_COLOR;
            Color hoverFill = hoverColor;
            hoverFill.a = 0.24f;
            EditorGUI.DrawRect(hoverRect, hoverFill);
            DrawRectOutline(hoverRect, hoverColor, 3f);

            GUIStyle hoverLabelStyle = new GUIStyle(EditorStyles.whiteBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Max(11, EditorStyles.whiteBoldLabel.fontSize),
                clipping = TextClipping.Clip,
            };
            GUI.Label(
                hoverRect,
                $"{(canAdd ? "+" : "x")} "
                + BoxCoverSizeUtility.GetDisplayName(selectedBoxCoverSize),
                hoverLabelStyle);
        }

        private Rect GetCurrentLayerGridRect()
        {
            float adjustedOffsetX = elementOffset + deltaX * elementSize;
            float adjustedOffsetY = elementOffset - deltaY * elementSize;
            return new Rect(
                drawRect.position.x + adjustedOffsetX,
                drawRect.position.y + adjustedOffsetY,
                selectedLayerWidth * elementSize,
                selectedLayerHeight * elementSize);
        }

        private Rect GetBoxCoverCandidateRect(Vector2Int anchor, BoxCoverSize size)
        {
            Vector2Int cellSize = BoxCoverSizeUtility.GetCellSize(size);
            Rect gridRect = GetCurrentLayerGridRect();
            return new Rect(
                gridRect.x + anchor.x * elementSize,
                gridRect.y + (selectedLayerHeight - anchor.y - cellSize.y) * elementSize,
                cellSize.x * elementSize,
                cellSize.y * elementSize);
        }

        private bool IsBoxCoverCandidateValidForDrawing(int candidateIndex)
        {
            BoxCoverCandidateView candidate = boxCoverCandidates[candidateIndex];
            Vector2Int candidateSize = BoxCoverSizeUtility.GetCellSize(candidate.size);
            if (selectedLayerIndex == 0
                || candidate.anchor.x < 0
                || candidate.anchor.y < 0
                || candidate.anchor.x + candidateSize.x > selectedLayerWidth
                || candidate.anchor.y + candidateSize.y > selectedLayerHeight)
            {
                return false;
            }

            for (int i = 0; i < boxCoverCandidates.Count; i++)
            {
                if (i == candidateIndex)
                {
                    continue;
                }

                BoxCoverCandidateView other = boxCoverCandidates[i];
                if (candidate.size == other.size)
                {
                    if (candidate.anchor == other.anchor)
                    {
                        return false;
                    }

                    continue;
                }

                if (BoxCoverRegionsOverlap(
                    candidate.anchor,
                    candidateSize,
                    other.anchor,
                    BoxCoverSizeUtility.GetCellSize(other.size)))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool BoxCoverRegionsOverlap(
            Vector2Int leftAnchor,
            Vector2Int leftSize,
            Vector2Int rightAnchor,
            Vector2Int rightSize)
        {
            return leftAnchor.x < rightAnchor.x + rightSize.x
                && leftAnchor.x + leftSize.x > rightAnchor.x
                && leftAnchor.y < rightAnchor.y + rightSize.y
                && leftAnchor.y + leftSize.y > rightAnchor.y;
        }

        private static Color GetBoxCoverColor(BoxCoverSize size)
        {
            return size switch
            {
                BoxCoverSize.Size2x3 => BOX_COVER_2X3_COLOR,
                BoxCoverSize.Size3x3 => BOX_COVER_3X3_COLOR,
                _ => BOX_COVER_2X2_COLOR,
            };
        }

        private static Rect IntersectRect(Rect left, Rect right)
        {
            float xMin = Mathf.Max(left.xMin, right.xMin);
            float yMin = Mathf.Max(left.yMin, right.yMin);
            float xMax = Mathf.Min(left.xMax, right.xMax);
            float yMax = Mathf.Min(left.yMax, right.yMax);
            return xMax > xMin && yMax > yMin
                ? Rect.MinMaxRect(xMin, yMin, xMax, yMax)
                : Rect.zero;
        }

        private static void DrawRectOutline(Rect rect, Color color, float thickness)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private void DrawSelectedCellOutline(Rect cellRect)
        {
            Color previousColor = GUI.color;
            GUI.color = SELECTED_CELL_HIGHLIGHT_COLOR;
            float t = SELECTED_CELL_OUTLINE_THICKNESS;
            GUI.DrawTexture(new Rect(cellRect.x, cellRect.y, cellRect.width, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cellRect.x, cellRect.yMax - t, cellRect.width, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cellRect.x, cellRect.y, t, cellRect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cellRect.xMax - t, cellRect.y, t, cellRect.height), Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private void HandleTint()
        {
            if (previousSelectedLayerIndex != selectedLayerIndex)
            {
                RebuildTintDrawCache();
                previousSelectedLayerIndex = selectedLayerIndex;
                // Preview root is destroyed when leaving GameScene; the grid still draws.
                if (editorSceneController != null && editorSceneController.IsValid)
                {
                    editorSceneController.UpdateSelectedLayerIndex(selectedLayerIndex);
                    editorSceneController.UpdateBoxCoverSelection(
                        selectedLayerIndex,
                        selectedBoxCoverSize);
                }
            }

            GUI.color = tintColorSerializedProperty.colorValue;

            // Draw from cache — avoid GetCellValue/GetCellDelta* on every Repaint.
            // Recompute starts from current drawRect so window resize stays correct.
            for (int i = 0; i < tintLayerDrawCache.Count; i++)
            {
                TintLayerDrawCache layerCache = tintLayerDrawCache[i];
                float layerElementOffset = layerCache.layerHeight == maxLayerHeight ? 0f : halfElementSize;
                float layerStartX = drawRect.position.x + layerElementOffset + (layerCache.layerDeltaX * elementSize);
                float layerStartY = drawRect.position.y + layerElementOffset - (layerCache.layerDeltaY * elementSize);

                for (int c = 0; c < layerCache.cells.Count; c++)
                {
                    TintDrawCell cell = layerCache.cells[c];
                    float cellX = layerStartX + cell.x * elementSize + cell.cellDeltaX * elementSize;
                    float cellY = layerStartY + (layerCache.layerHeight - cell.y - 1) * elementSize - cell.cellDeltaY * elementSize;
                    GUI.DrawTexture(new Rect(cellX, cellY, elementSize, elementSize), Texture2D.whiteTexture);
                }
            }
        }

        private void RebuildTintDrawCache()
        {
            tintMap = new TintMap(maxLayerWidth, maxLayerHeight);
            tintLayerDrawCache.Clear();

            for (int layerIndex = selectedLevelGenFunc.layersProperty.arraySize - 1; layerIndex > selectedLayerIndex; layerIndex--)
            {
                int layerWidth = selectedLevelGenFunc.GetLayerWidth(layerIndex);
                int layerHeight = selectedLevelGenFunc.GetLayerHeight(layerIndex);
                var layerCache = new TintLayerDrawCache
                {
                    layerHeight = layerHeight,
                    layerDeltaX = selectedLevelGenFunc.GetLayerDeltaX(layerIndex),
                    layerDeltaY = selectedLevelGenFunc.GetLayerDeltaY(layerIndex),
                    cells = new List<TintDrawCell>(64)
                };

                for (int x = 0; x < layerWidth; x++)
                {
                    for (int y = 0; y < layerHeight; y++)
                    {
                        if (!selectedLevelGenFunc.GetCellValue(layerIndex, x, y))
                        {
                            continue;
                        }

                        if (layerHeight == maxLayerHeight)
                        {
                            tintMap.MarkPositionInOuterLayer(x, y);
                        }
                        else
                        {
                            tintMap.MarkPositionInInnerLayer(x, y);
                        }

                        layerCache.cells.Add(new TintDrawCell
                        {
                            x = x,
                            y = y,
                            cellDeltaX = selectedLevelGenFunc.GetCellDeltaX(layerIndex, x, y),
                            cellDeltaY = selectedLevelGenFunc.GetCellDeltaY(layerIndex, x, y)
                        });
                    }
                }

                if (layerCache.cells.Count > 0)
                {
                    tintLayerDrawCache.Add(layerCache);
                }
            }

            tintMap.FillMapList();
        }

        private struct TintDrawCell
        {
            public int x;
            public int y;
            public float cellDeltaX;
            public float cellDeltaY;
        }

        private class TintLayerDrawCache
        {
            public int layerHeight;
            public float layerDeltaX;
            public float layerDeltaY;
            public List<TintDrawCell> cells;
        }

        private void HandleChangingButtonWidth()
        {
            separatorRect = EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(0), GUILayout.ExpandHeight(true));
            EditorGUILayout.EndHorizontal();
            separatorRect.xMin -= GUI.skin.box.margin.right;
            separatorRect.xMax += GUI.skin.box.margin.left;


            EditorGUIUtility.AddCursorRect(separatorRect, MouseCursor.ResizeHorizontal);


            if (separatorRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.MouseDown)
                {
                    buttonSeparatorIsDragged = true;
                    Event.current.Use();
                }
            }

            if (buttonSeparatorIsDragged)
            {
                if (Event.current.type == EventType.MouseUp)
                {
                    buttonSeparatorIsDragged = false;
                    PlayerPrefs.SetInt(PREFS_BUTTONS_WIDTH, currentButtonsWidth);
                    PlayerPrefs.Save();
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.MouseDrag)
                {
                    currentButtonsWidth = currentButtonsWidth - Mathf.RoundToInt(Event.current.delta.x);
                    Event.current.Use();
                }
            }
        }

        private void DrawTipsAndWarnings()
        {
            infoRect = EditorGUILayout.BeginVertical(GUILayout.MinHeight(INFO_HEIGH));

            if (cachedCurrentLayerTileCount < 0 && selectedLayerIndex >= 0)
            {
                cachedCurrentLayerTileCount = selectedLevelGenFunc.GetLayerTileCount(selectedLayerIndex);
            }

            int currentLayerTileCount = selectedLayerIndex >= 0 ? cachedCurrentLayerTileCount : 0;

            if (selectedLevelGenFunc.IsLevelCorrect)
            {
                EditorGUILayout.HelpBox($"{LEVEL_PASSED_VALIDATION} Current layer tiles: {currentLayerTileCount}", MessageType.Info);
            }
            else
            {
                string errorMessage = selectedLevelGenFunc.errorLabels.Count > 0
                    ? selectedLevelGenFunc.errorLabels[0]
                    : "Level has validation errors.";
                if (errorMessage.StartsWith("Incorrect tiles amount"))
                {
                    errorMessage += $" Current layer tiles: {currentLayerTileCount}";
                }
                EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
            }

            EditorGUILayout.HelpBox($"Tiles amount: {selectedLevelGenFunc.elementCounter}. Sets amount: {selectedLevelGenFunc.SetsAmount}. {selectedLevelGenFunc.effectsLabel}\nDifficulty score: {selectedLevelGenFunc.DifficultyScore} (sets / unique items)", MessageType.Info);
            EditorGUILayout.EndVertical();

            //Debug.Log(infoRect.height);
        }

        #endregion

        private void DrawEditVariablesTab()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(RETURN_LABEL, EditorCustomStyles.button))
            {
                drawEditVariablesTabDisplayed = false;
            }

            EditorGUILayout.LabelField("Colors used during level editing", EditorCustomStyles.labelMediumBold);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(selectedColorSerializedProperty);
            EditorGUILayout.PropertyField(unselectedColorSerializedProperty);
            EditorGUILayout.PropertyField(tintColorSerializedProperty);
            EditorGUILayout.PropertyField(backgroundColorSerializedProperty);
            EditorGUILayout.PropertyField(gridColorSerializedProperty);
            EditorGUILayout.EndVertical();
        }

        private void OnDestroy()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorSceneManager.sceneOpened -= OnEditorSceneOpened;
            EditorSceneManager.sceneSaving -= OnEditorSceneSaving;
            EditorSceneManager.sceneSaved -= OnEditorSceneSaved;
            Undo.undoRedoPerformed -= OnLevelListUndoRedoPerformed;
            RestoreEditorDockPreview();
            DestroyEditorPreview(includingOrphans: true);
            RestoreGameUiForLevelEditor();
        }

        public override void OnBeforeAssemblyReload()
        {
            Undo.undoRedoPerformed -= OnLevelListUndoRedoPerformed;
            // ExitingEditMode already restored the dock and UI. Play entry with
            // Enter Play Mode Options (scene reload off) still domain-reloads, and
            // UIGame can already be destroyed by the time this runs.
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                RestoreEditorDockPreview();
                RestoreGameUiForLevelEditor();
            }

            DestroyEditorPreview(includingOrphans: true);
            ClearSelection();
            lastActiveLevelOpened = false;
        }

        public override bool WindowClosedInPlaymode()
        {
            //不关编辑器观察下。
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (editorSceneController != null)
                {
                    ClearSelection();
                    lastActiveLevelOpened = false;
                }

                return true;
                // return false;
            }
            else
            {
                return false;
            }
        }


        private void ClearSelection()
        {
            levelsHandler.ClearSelection();
            selectedLevelGenFunc = null;
            RemoveSceneRepresentation();
        }

        private void BindLevelsHandlerCallbacks()
        {
            if (levelsHandler == null)
            {
                return;
            }

            levelsHandler.onClearSelectionCallback = ClearPreviewAfterListCleared;
            levelsHandler.onLoadRangeApplied = RefreshOpenLevelAfterRangeApplied;
        }

        private void ClearPreviewAfterListCleared()
        {
            selectedLevelGenFunc = null;
            RemoveSceneRepresentation();
        }

        private void RefreshOpenLevelAfterRangeApplied()
        {
            if (levelsHandler == null || levelsHandler.SelectedLevelIndex < 0)
            {
                return;
            }

            levelsHandler.OpenLevel(levelsHandler.SelectedLevelIndex);
        }

        private void ApplyDeltaToLayer(int layerIndex, float deltaX, float deltaY)
        {
            if (selectedLevelGenFunc == null || layerIndex < 0 || layerIndex >= selectedLevelGenFunc.layersProperty.arraySize)
                return;

            // 保存delta值到层级数据中
            selectedLevelGenFunc.SetLayerDeltaX(layerIndex, deltaX);
            selectedLevelGenFunc.SetLayerDeltaY(layerIndex, deltaY);
            
            Debug.Log($"Applied Delta X: {deltaX} (×{elementSize}), Delta Y: {deltaY} (×{elementSize}) to Layer {layerIndex + 1}");
            
            // 刷新UI
            InitCellsForGameView();
            Repaint();
        }

        /// <summary>
        /// Writes visual offset only to selected cells' CellModel.deltaX/Y.
        /// Does not modify Layer.deltaX/Y.
        /// </summary>
        private void ApplyOffsetToSelectedCells(float offsetX, float offsetY)
        {
            if (selectedLevelGenFunc == null || selectedLayerIndex < 0 || selectedCells.Count == 0)
            {
                return;
            }

            offsetX = RoundOffsetThreeDecimal(offsetX);
            offsetY = RoundOffsetThreeDecimal(offsetY);
            selectionOffsetX = offsetX;
            selectionOffsetY = offsetY;

            foreach (Vector2Int cell in selectedCells)
            {
                selectedLevelGenFunc.SetCellDelta(selectedLayerIndex, cell.x, cell.y, offsetX, offsetY);
                if (editorSceneController != null && editorSceneController.IsValid)
                {
                    editorSceneController.SetCellDeltaOffset(selectedLayerIndex, cell.x, cell.y, offsetX, offsetY);
                }
            }

            selectedLevelGenFunc.ApplyChanges();
            ExitSelectToDraw();
            Repaint();
        }

        private static float RoundOffsetThreeDecimal(float value)
        {
            return Mathf.Clamp(Mathf.Round(value * 1000f) / 1000f, -1f, 1f);
        }

        private void SyncSelectionOffsetFields()
        {
            if (selectedCells.Count == 0 || selectedLevelGenFunc == null || selectedLayerIndex < 0)
            {
                selectionOffsetX = 0f;
                selectionOffsetY = 0f;
                return;
            }

            bool first = true;
            float commonX = 0f;
            float commonY = 0f;
            bool sameX = true;
            bool sameY = true;

            foreach (Vector2Int cell in selectedCells)
            {
                float cellX = selectedLevelGenFunc.GetCellDeltaX(selectedLayerIndex, cell.x, cell.y);
                float cellY = selectedLevelGenFunc.GetCellDeltaY(selectedLayerIndex, cell.x, cell.y);
                if (first)
                {
                    commonX = cellX;
                    commonY = cellY;
                    first = false;
                }
                else
                {
                    if (!Mathf.Approximately(commonX, cellX))
                    {
                        sameX = false;
                    }
                    if (!Mathf.Approximately(commonY, cellY))
                    {
                        sameY = false;
                    }
                }
            }

            selectionOffsetX = sameX ? RoundOffsetThreeDecimal(commonX) : 0f;
            selectionOffsetY = sameY ? RoundOffsetThreeDecimal(commonY) : 0f;
        }


        private class TintMap
        {
            public int maxLayerWidth;
            public int maxLayerHeight;
            public bool[,] map;
            public List<Vector2Int> mapList;
            public List<Vector2Int> mapListWithOffsets; // 包含偏移的列表

            public TintMap(int maxLayerWidth, int maxLayerHeight)
            {
                this.maxLayerWidth = maxLayerWidth;
                this.maxLayerHeight = maxLayerHeight;
                this.map = new bool[maxLayerWidth * 2, maxLayerHeight * 2];
                mapList = new List<Vector2Int>();
                mapListWithOffsets = new List<Vector2Int>();
            }

            public void MarkPositionInOuterLayer(int x, int y) // if firstLayerSize = 5 this function is for every layer with size 6
            {
                int topLeftX = x * 2;
                int topLeftY = (maxLayerHeight - y - 1) * 2;

                try
                {
                    map[topLeftX, topLeftY] = true;
                    map[topLeftX + 1, topLeftY] = true;
                    map[topLeftX, topLeftY + 1] = true;
                    map[topLeftX + 1, topLeftY + 1] = true;
                }
                catch (IndexOutOfRangeException e)
                {
                    Debug.LogError("Incorrect layers size.");
                    Debug.LogError(e);
                }
            }

            public void MarkPositionInInnerLayer(int x, int y) // if firstLayerSize = 5 this function is for every layer with size 5
            {
                int topLeftX = x * 2 + 1;
                int topLeftY = (maxLayerHeight - y - 2) * 2 + 1;

                try
                {
                    map[topLeftX, topLeftY] = true;
                    map[topLeftX + 1, topLeftY] = true;
                    map[topLeftX, topLeftY + 1] = true;
                    map[topLeftX + 1, topLeftY + 1] = true;
                }
                catch (IndexOutOfRangeException e)
                {
                    Debug.LogError("Incorrect layers size");
                    Debug.LogError(e);
                }
            }

            public void FillMapList()
            {
                for (int i = 0; i < maxLayerWidth * 2; i++)
                {
                    for (int j = 0; j < maxLayerHeight * 2; j++)
                    {
                        if (map[i, j])
                        {
                            mapList.Add(new Vector2Int(i, j));
                        }
                    }
                }
            }
        }

        protected class LevelGenService : LevelGenFuncBase
        {
            private const string LAYERS_PROPERTY_NAME = "layers";
            private const string BOTTOM_LAYER_WIDTH_PROPERTY_NAME = "bottomLayerWidth";
            private const string BOTTOM_LAYER_HEIGHT_PROPERTY_NAME = "bottomLayerHeight";

            private const string SHOW_LAYER_SCALE = "showLayerScale";

            // private const string SHOW_LAYER_HEIGHT_PROPERTY_NAME = "showLayerHeight";
            // private const string SHOW_LAYER_WIDTH_PROPERTY_NAME = "showLayerWidth";

            private const string SHOW_LAYERDELTA_X_PROPERTY_NAME = "showLayerDeltaX";
            private const string SHOW_LAYERDELTA_Y_PROPERTY_NAME = "showLayerDeltaY";

            private const string USER_IN_RANDOMIZER_PROPERTY_NAME = "useInRandomizer";
            private const string ELEMENTS_PER_LEVEL_PROPERTY_NAME = "elementsPerLevel";
            private const string COINS_REWARD_PROPERTY_NAME = "coinsReward";
            private const string SAME_COLOR_LIST_STR_PROPERTY_NAME = "sameColorListStr";
            private const string EASY_HARD_VALUE_NAME = "easyHardValue";

            private const string EDITOR_NOTE_PROPERTY_NAME = "editorNote";
            private const string DATA_ID_PROPERTY_NAME = "dataId";


            private const string ROWS_PROPERTY_NAME = "rows";
            private const string CELLS_PROPERTY_NAME = "cells";
            private const string CELL_IS_FILLED_PROPERTY_NAME = "IsFilled";
            private const string EFFECT_PROPERTY_NAME = "Effect";
            private const string GOLDEN_TREE_IS_X2_PROPERTY_NAME = "GoldenTreeIsX2";
            private const string CELL_DELTA_X_PROPERTY_NAME = "deltaX";
            private const string CELL_DELTA_Y_PROPERTY_NAME = "deltaY";
            private const string DELTA_X_PROPERTY_NAME = "deltaX";
            private const string DELTA_Y_PROPERTY_NAME = "deltaY";
            private const string BOX_COVERS_PROPERTY_NAME = "boxCovers";
            private const string BOX_COVER_TYPE_PROPERTY_NAME = "boxCoverType";
            private const string BOX_COVER_SIZE_PROPERTY_NAME = "size";
            private const string BOX_COVER_COUNT_PROPERTY_NAME = "count";
            private const string BOX_COVER_ANCHORS_PROPERTY_NAME = "candidateAnchors";


            public SerializedProperty layersProperty;
            public SerializedProperty bottomLayerWidthProperty;
            public SerializedProperty bottomLayerHeightProperty;

            // public SerializedProperty showLayerWidthProperty;
            // public SerializedProperty showLayerHeightProperty;

            public SerializedProperty showLayerScaleProperty;

            public SerializedProperty showLayerDeltaXProperty;
            public SerializedProperty showLayerDeltaYProperty;

            public SerializedProperty useInRandomizerProperty;
            public SerializedProperty elementsPerLevelProperty;
            public SerializedProperty coinsRewardProperty;
            public SerializedProperty sameColorListStrProperty;
            public SerializedProperty easyHardValueProperty;
            public SerializedProperty editorNoteProperty;
            public SerializedProperty dataIdProperty;
            public SerializedProperty boxCoverTypeProperty;

            private SerializedProperty tempProperty;

            public int elementCounter;
            public bool elementCounterReady;
            public int SetsAmount => (elementCounter - (elementCounter % 3)) / 3;
            public LevelModel LevelData => levelObject as LevelModel;
            public string effectsLabel;
            private int[] effectsCounter;


            public float DifficultyScore => Mathf.Round(Mathf.Clamp(SetsAmount / (float)elementsPerLevelProperty.intValue, 1, float.MaxValue) * 10.0f) * 0.1f;

            public LevelGenService(UnityEngine.Object levelObject) : base(levelObject)
            {
            }

            protected override void ReadFields()
            {
                layersProperty = serializedLevelObject.FindProperty(LAYERS_PROPERTY_NAME);
                bottomLayerWidthProperty = serializedLevelObject.FindProperty(BOTTOM_LAYER_WIDTH_PROPERTY_NAME);
                bottomLayerHeightProperty = serializedLevelObject.FindProperty(BOTTOM_LAYER_HEIGHT_PROPERTY_NAME);

                showLayerScaleProperty =  serializedLevelObject.FindProperty(SHOW_LAYER_SCALE);
                showLayerDeltaXProperty = serializedLevelObject.FindProperty(SHOW_LAYERDELTA_X_PROPERTY_NAME);
                showLayerDeltaYProperty = serializedLevelObject.FindProperty(SHOW_LAYERDELTA_Y_PROPERTY_NAME);

                useInRandomizerProperty = serializedLevelObject.FindProperty(USER_IN_RANDOMIZER_PROPERTY_NAME);
                elementsPerLevelProperty = serializedLevelObject.FindProperty(ELEMENTS_PER_LEVEL_PROPERTY_NAME);
                coinsRewardProperty = serializedLevelObject.FindProperty(COINS_REWARD_PROPERTY_NAME);
                sameColorListStrProperty = serializedLevelObject.FindProperty(SAME_COLOR_LIST_STR_PROPERTY_NAME);
                easyHardValueProperty = serializedLevelObject.FindProperty(EASY_HARD_VALUE_NAME);
                editorNoteProperty = serializedLevelObject.FindProperty(EDITOR_NOTE_PROPERTY_NAME);
                dataIdProperty = serializedLevelObject.FindProperty(DATA_ID_PROPERTY_NAME);
                boxCoverTypeProperty = serializedLevelObject.FindProperty(BOX_COVER_TYPE_PROPERTY_NAME);

                effectsCounter = new int[Enum.GetValues(typeof(TileEffectType)).Length];
            }

            public override void Clear()
            {
                layersProperty.arraySize = 0;
                bottomLayerHeightProperty.intValue = 10;
                bottomLayerWidthProperty.intValue = 10;
                // showLayerHeightProperty.intValue = 0;
                // showLayerWidthProperty.intValue = 0;
                showLayerScaleProperty.floatValue = 1.0f;
                useInRandomizerProperty.boolValue = true;
                elementsPerLevelProperty.intValue = 8;
                coinsRewardProperty.intValue = 20;
                sameColorListStrProperty.stringValue = "";
                easyHardValueProperty.intValue = 0;
                if (dataIdProperty != null)
                {
                    dataIdProperty.stringValue = BuildDefaultDataId(levelObject != null ? levelObject.name : string.Empty);
                }
                if (boxCoverTypeProperty != null)
                {
                    boxCoverTypeProperty.enumValueIndex = (int)BoxCoverType.Coke;
                }
                ApplyChanges();
            }

            public static string ExtractLevelNumberFromName(string assetName)
            {
                if (string.IsNullOrEmpty(assetName))
                {
                    return "0";
                }

                int start = -1;
                int end = -1;
                for (int i = 0; i < assetName.Length; i++)
                {
                    if (char.IsDigit(assetName[i]))
                    {
                        if (start < 0)
                        {
                            start = i;
                        }

                        end = i;
                    }
                    else if (start >= 0)
                    {
                        break;
                    }
                }

                if (start < 0)
                {
                    return "0";
                }

                string digits = assetName.Substring(start, end - start + 1);
                if (int.TryParse(digits, out int number))
                {
                    return number.ToString();
                }

                return digits;
            }

            public static string BuildDefaultDataId(string assetName)
            {
                if (!string.IsNullOrEmpty(assetName))
                {
                    return assetName;
                }

                return BuildDefaultDataIdFromNumber("0");
            }

            public static string BuildDefaultDataIdFromNumber(string number)
            {
                string prefix = LevelEditorBase.Instance != null
                    ? LevelEditorBase.Instance.LevelAssetPrefix
                    : LevelPlatformResolver.GetLevelAssetPrefix();
                if (int.TryParse(number, out int parsed))
                {
                    return prefix + parsed;
                }

                return prefix + number;
            }

            public static string ResolveDefaultDataId(string assetName, int listIndex)
            {
                if (!string.IsNullOrEmpty(assetName))
                {
                    return assetName;
                }

                if (listIndex >= 0)
                {
                    return BuildDefaultDataIdFromNumber((listIndex + 1).ToString());
                }

                return BuildDefaultDataIdFromNumber("0");
            }

            private string GetDisplayDataId(int listIndex)
            {
                // 左侧 # 是列表序号，不显示文件名里的 1000000。
                if (listIndex >= 0)
                {
                    return BuildDefaultDataIdFromNumber((listIndex + 1).ToString());
                }

                if (levelObject != null && !string.IsNullOrEmpty(levelObject.name))
                {
                    return levelObject.name;
                }

                if (dataIdProperty != null && !string.IsNullOrWhiteSpace(dataIdProperty.stringValue))
                {
                    return dataIdProperty.stringValue;
                }

                return ResolveDefaultDataId(string.Empty, listIndex);
            }

            private void EnsureDefaultDataId(int listIndex)
            {
                if (NullLevel || dataIdProperty == null)
                {
                    return;
                }

                if (!string.IsNullOrWhiteSpace(dataIdProperty.stringValue))
                {
                    return;
                }

                dataIdProperty.stringValue = ResolveDefaultDataId(levelObject.name, listIndex);
                ApplyChanges();
            }

            private void ValidateDataIdUniqueness()
            {
                if (NullLevel || dataIdProperty == null)
                {
                    return;
                }

                string currentId = dataIdProperty.stringValue;
                if (string.IsNullOrWhiteSpace(currentId))
                {
                    return;
                }

                string[] guids = AssetDatabase.FindAssets("t:LevelModel");
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    LevelModel other = AssetDatabase.LoadAssetAtPath<LevelModel>(path);
                    if (other == null || other == levelObject)
                    {
                        continue;
                    }

                    if (string.Equals(other.DataId, currentId, StringComparison.Ordinal))
                    {
                        errorLabels.Add($"dataId \"{currentId}\" is already used by \"{other.name}\".");
                        break;
                    }
                }
            }

            public BoxCoverType GetBoxCoverType()
            {
                if (boxCoverTypeProperty == null)
                {
                    return BoxCoverType.Coke;
                }

                return (BoxCoverType)Mathf.Clamp(
                    boxCoverTypeProperty.enumValueIndex,
                    0,
                    BOX_COVER_TYPE_LABELS.Length - 1);
            }

            public bool SetBoxCoverType(BoxCoverType type)
            {
                if (boxCoverTypeProperty == null)
                {
                    return false;
                }

                int enumIndex = (int)type;
                if (boxCoverTypeProperty.enumValueIndex == enumIndex)
                {
                    return false;
                }

                boxCoverTypeProperty.enumValueIndex = enumIndex;
                return true;
            }

            private SerializedProperty GetLayerBoxCoversProperty(int layerIndex)
            {
                if (layerIndex < 0 || layerIndex >= layersProperty.arraySize)
                {
                    return null;
                }

                return layersProperty
                    .GetArrayElementAtIndex(layerIndex)
                    .FindPropertyRelative(BOX_COVERS_PROPERTY_NAME);
            }

            private SerializedProperty FindBoxCoverConfigurationProperty(
                int layerIndex,
                BoxCoverSize size)
            {
                SerializedProperty boxCoversProperty = GetLayerBoxCoversProperty(layerIndex);
                if (boxCoversProperty == null)
                {
                    return null;
                }

                for (int i = 0; i < boxCoversProperty.arraySize; i++)
                {
                    SerializedProperty configuration =
                        boxCoversProperty.GetArrayElementAtIndex(i);
                    SerializedProperty sizeProperty =
                        configuration.FindPropertyRelative(BOX_COVER_SIZE_PROPERTY_NAME);
                    if (sizeProperty != null && sizeProperty.enumValueIndex == (int)size)
                    {
                        return configuration;
                    }
                }

                return null;
            }

            private SerializedProperty EnsureBoxCoverConfigurationProperty(
                int layerIndex,
                BoxCoverSize size)
            {
                SerializedProperty configuration =
                    FindBoxCoverConfigurationProperty(layerIndex, size);
                if (configuration != null)
                {
                    return configuration;
                }

                SerializedProperty boxCoversProperty = GetLayerBoxCoversProperty(layerIndex);
                if (boxCoversProperty == null)
                {
                    return null;
                }

                int index = boxCoversProperty.arraySize;
                boxCoversProperty.InsertArrayElementAtIndex(index);
                configuration = boxCoversProperty.GetArrayElementAtIndex(index);

                SerializedProperty sizeProperty =
                    configuration.FindPropertyRelative(BOX_COVER_SIZE_PROPERTY_NAME);
                SerializedProperty countProperty =
                    configuration.FindPropertyRelative(BOX_COVER_COUNT_PROPERTY_NAME);
                SerializedProperty anchorsProperty =
                    configuration.FindPropertyRelative(BOX_COVER_ANCHORS_PROPERTY_NAME);
                if (sizeProperty != null)
                    sizeProperty.enumValueIndex = (int)size;
                if (countProperty != null)
                    countProperty.intValue = 0;
                if (anchorsProperty != null)
                    anchorsProperty.arraySize = 0;
                return configuration;
            }

            public int GetBoxCoverCount(int layerIndex, BoxCoverSize size)
            {
                SerializedProperty configuration =
                    FindBoxCoverConfigurationProperty(layerIndex, size);
                SerializedProperty countProperty =
                    configuration?.FindPropertyRelative(BOX_COVER_COUNT_PROPERTY_NAME);
                return countProperty != null ? Mathf.Max(0, countProperty.intValue) : 0;
            }

            public int GetBoxCoverCandidateCount(int layerIndex, BoxCoverSize size)
            {
                SerializedProperty configuration =
                    FindBoxCoverConfigurationProperty(layerIndex, size);
                SerializedProperty anchorsProperty =
                    configuration?.FindPropertyRelative(BOX_COVER_ANCHORS_PROPERTY_NAME);
                return anchorsProperty?.arraySize ?? 0;
            }

            public bool SetBoxCoverCount(int layerIndex, BoxCoverSize size, int count)
            {
                int clampedCount = Mathf.Max(0, count);
                if (layerIndex <= 0 && clampedCount > 0)
                {
                    Debug.LogWarning("[BoxCover] Top layer does not allow box covers.");
                    return false;
                }

                SerializedProperty configuration =
                    FindBoxCoverConfigurationProperty(layerIndex, size);
                if (configuration == null)
                {
                    if (clampedCount == 0)
                    {
                        return false;
                    }

                    configuration = EnsureBoxCoverConfigurationProperty(layerIndex, size);
                }
                SerializedProperty countProperty =
                    configuration?.FindPropertyRelative(BOX_COVER_COUNT_PROPERTY_NAME);
                if (countProperty == null || countProperty.intValue == clampedCount)
                {
                    return false;
                }

                countProperty.intValue = clampedCount;
                return true;
            }

            public void CollectBoxCoverCandidates(
                int layerIndex,
                List<BoxCoverCandidateView> output)
            {
                if (output == null)
                {
                    return;
                }

                SerializedProperty boxCoversProperty = GetLayerBoxCoversProperty(layerIndex);
                if (boxCoversProperty == null)
                {
                    return;
                }

                for (int i = 0; i < boxCoversProperty.arraySize; i++)
                {
                    SerializedProperty configuration =
                        boxCoversProperty.GetArrayElementAtIndex(i);
                    SerializedProperty sizeProperty =
                        configuration.FindPropertyRelative(BOX_COVER_SIZE_PROPERTY_NAME);
                    SerializedProperty anchorsProperty =
                        configuration.FindPropertyRelative(BOX_COVER_ANCHORS_PROPERTY_NAME);
                    if (sizeProperty == null || anchorsProperty == null)
                    {
                        continue;
                    }

                    BoxCoverSize size = (BoxCoverSize)sizeProperty.enumValueIndex;
                    for (int j = 0; j < anchorsProperty.arraySize; j++)
                    {
                        output.Add(new BoxCoverCandidateView(
                            size,
                            anchorsProperty.GetArrayElementAtIndex(j).vector2IntValue));
                    }
                }
            }

            public bool CanAddBoxCoverCandidate(
                int layerIndex,
                BoxCoverSize size,
                Vector2Int anchor)
            {
                if (layerIndex <= 0 || layerIndex >= layersProperty.arraySize)
                {
                    return false;
                }

                Vector2Int cellSize = BoxCoverSizeUtility.GetCellSize(size);
                if (anchor.x < 0
                    || anchor.y < 0
                    || anchor.x + cellSize.x > GetLayerWidth(layerIndex)
                    || anchor.y + cellSize.y > GetLayerHeight(layerIndex))
                {
                    return false;
                }

                List<BoxCoverCandidateView> candidates = new List<BoxCoverCandidateView>();
                CollectBoxCoverCandidates(layerIndex, candidates);
                for (int i = 0; i < candidates.Count; i++)
                {
                    BoxCoverCandidateView existing = candidates[i];
                    if (existing.size == size)
                    {
                        // Same-size anchors are an alternative random pool. Only exact duplicates
                        // are invalid; the resolver chooses a non-overlapping subset at runtime.
                        if (existing.anchor == anchor)
                        {
                            return false;
                        }
                        continue;
                    }

                    if (BoxCoverRegionsOverlap(
                        anchor,
                        cellSize,
                        existing.anchor,
                        BoxCoverSizeUtility.GetCellSize(existing.size)))
                    {
                        return false;
                    }
                }

                return true;
            }

            public bool TryAddBoxCoverCandidate(
                int layerIndex,
                BoxCoverSize size,
                Vector2Int anchor)
            {
                if (!CanAddBoxCoverCandidate(layerIndex, size, anchor))
                {
                    return false;
                }

                SerializedProperty configuration =
                    EnsureBoxCoverConfigurationProperty(layerIndex, size);
                SerializedProperty anchorsProperty =
                    configuration?.FindPropertyRelative(BOX_COVER_ANCHORS_PROPERTY_NAME);
                if (anchorsProperty == null)
                {
                    return false;
                }

                int index = anchorsProperty.arraySize;
                anchorsProperty.InsertArrayElementAtIndex(index);
                anchorsProperty.GetArrayElementAtIndex(index).vector2IntValue = anchor;

                SerializedProperty countProperty =
                    configuration.FindPropertyRelative(BOX_COVER_COUNT_PROPERTY_NAME);
                if (countProperty != null && countProperty.intValue <= 0)
                {
                    countProperty.intValue = 1;
                }
                return true;
            }

            public bool TryRemoveBoxCoverCandidateAtCell(
                int layerIndex,
                BoxCoverSize size,
                Vector2Int cell)
            {
                SerializedProperty configuration =
                    FindBoxCoverConfigurationProperty(layerIndex, size);
                SerializedProperty anchorsProperty =
                    configuration?.FindPropertyRelative(BOX_COVER_ANCHORS_PROPERTY_NAME);
                if (anchorsProperty == null)
                {
                    return false;
                }

                Vector2Int cellSize = BoxCoverSizeUtility.GetCellSize(size);
                for (int i = anchorsProperty.arraySize - 1; i >= 0; i--)
                {
                    Vector2Int anchor =
                        anchorsProperty.GetArrayElementAtIndex(i).vector2IntValue;
                    if (cell.x < anchor.x
                        || cell.x >= anchor.x + cellSize.x
                        || cell.y < anchor.y
                        || cell.y >= anchor.y + cellSize.y)
                    {
                        continue;
                    }

                    anchorsProperty.DeleteArrayElementAtIndex(i);
                    SerializedProperty countProperty =
                        configuration.FindPropertyRelative(BOX_COVER_COUNT_PROPERTY_NAME);
                    if (countProperty != null
                        && countProperty.intValue > anchorsProperty.arraySize)
                    {
                        countProperty.intValue = anchorsProperty.arraySize;
                    }
                    return true;
                }

                return false;
            }

            public bool ClearLayerBoxCovers(int layerIndex)
            {
                SerializedProperty boxCoversProperty = GetLayerBoxCoversProperty(layerIndex);
                if (boxCoversProperty == null)
                {
                    return false;
                }

                bool changed = false;
                for (int i = 0; i < boxCoversProperty.arraySize; i++)
                {
                    SerializedProperty configuration =
                        boxCoversProperty.GetArrayElementAtIndex(i);
                    SerializedProperty countProperty =
                        configuration.FindPropertyRelative(BOX_COVER_COUNT_PROPERTY_NAME);
                    SerializedProperty anchorsProperty =
                        configuration.FindPropertyRelative(BOX_COVER_ANCHORS_PROPERTY_NAME);
                    if (countProperty != null && countProperty.intValue != 0)
                    {
                        countProperty.intValue = 0;
                        changed = true;
                    }
                    if (anchorsProperty != null && anchorsProperty.arraySize > 0)
                    {
                        anchorsProperty.arraySize = 0;
                        changed = true;
                    }
                }

                return changed;
            }

            private int GetConfiguredBoxCoverCount()
            {
                int total = 0;
                for (int layerIndex = 0;
                     layerIndex < layersProperty.arraySize;
                     layerIndex++)
                {
                    SerializedProperty configurations =
                        GetLayerBoxCoversProperty(layerIndex);
                    if (configurations == null)
                    {
                        continue;
                    }

                    for (int i = 0; i < configurations.arraySize; i++)
                    {
                        SerializedProperty countProperty = configurations
                            .GetArrayElementAtIndex(i)
                            .FindPropertyRelative(BOX_COVER_COUNT_PROPERTY_NAME);
                        if (countProperty != null)
                        {
                            total += Mathf.Max(0, countProperty.intValue);
                        }
                    }
                }

                return total;
            }

            public bool GetCellValue(int layer, int xIndex, int yIndex)
            {
                return layersProperty.GetArrayElementAtIndex(layer).FindPropertyRelative(ROWS_PROPERTY_NAME).GetArrayElementAtIndex(yIndex).FindPropertyRelative(CELLS_PROPERTY_NAME).GetArrayElementAtIndex(xIndex).FindPropertyRelative(CELL_IS_FILLED_PROPERTY_NAME).boolValue;
            }

            public int GetLayerTileCount(int layerIndex)
            {
                int count = 0;
                int layerWidth = GetLayerWidth(layerIndex);
                int layerHeight = GetLayerHeight(layerIndex);

                for (int x = 0; x < layerWidth; x++)
                {
                    for (int y = 0; y < layerHeight; y++)
                    {
                        if (GetCellValue(layerIndex, x, y))
                        {
                            count++;
                        }
                    }
                }

                return count;
            }

            /// <summary>
            /// Fast path after painting cells: keep sets/tile-count UI in sync without full-grid scan.
            /// </summary>
            public void AdjustElementCounter(int delta)
            {
                elementCounter = Mathf.Max(0, elementCounter + delta);
                elementCounterReady = true;
                errorLabels.RemoveAll(label => label != null && label.StartsWith("Incorrect tiles amount", StringComparison.Ordinal));
                if (elementCounter % 3 == 1)
                {
                    errorLabels.Insert(0, "Incorrect tiles amount.Need 2 more to create a full set.");
                }
                else if (elementCounter % 3 == 2)
                {
                    errorLabels.Insert(0, "Incorrect tiles amount.Need 1 more to create a full set.");
                }
            }

            /// <summary>
            /// Mirrors LevelDirector.GetMinShowSize for editor preview TileSize calculation.
            /// </summary>
            public Vector2Int GetMinShowSize()
            {
                int maxWidth = 0;
                int maxHeight = 0;

                for (int layerIndex = 0; layerIndex < layersProperty.arraySize; layerIndex++)
                {
                    int layerWidth = 0;
                    int layerHeight = GetLayerHeight(layerIndex);
                    int cellsPerRow = GetLayerWidth(layerIndex);

                    for (int rowIndex = 0; rowIndex < layerHeight; rowIndex++)
                    {
                        int firstFilledIndex = -1;
                        int lastFilledIndex = -1;

                        for (int cellIndex = 0; cellIndex < cellsPerRow; cellIndex++)
                        {
                            if (GetCellValue(layerIndex, cellIndex, rowIndex))
                            {
                                if (firstFilledIndex == -1)
                                {
                                    firstFilledIndex = cellIndex;
                                }

                                lastFilledIndex = cellIndex;
                            }
                        }

                        if (firstFilledIndex != -1 && lastFilledIndex != -1)
                        {
                            int rowWidth = lastFilledIndex - firstFilledIndex + 1;
                            if (rowWidth > layerWidth)
                            {
                                layerWidth = rowWidth;
                            }
                        }
                    }

                    if (layerWidth > maxWidth)
                    {
                        maxWidth = layerWidth;
                    }

                    maxHeight = Mathf.Max(maxHeight, layerHeight);
                }

                return new Vector2Int(Mathf.Max(1, maxWidth), Mathf.Max(1, maxHeight));
            }

            public void SetCellValue(int layer, int xIndex, int yIndex, bool newValue)
            {
                GetCellProperty(layer, xIndex, yIndex).FindPropertyRelative(CELL_IS_FILLED_PROPERTY_NAME).boolValue = newValue;
                if (!newValue)
                {
                    SetCellDelta(layer, xIndex, yIndex, 0f, 0f);
                }
            }

            public bool ToggleCellValue(int layer, int xIndex, int yIndex)
            {
                tempProperty = GetCellProperty(layer, xIndex, yIndex).FindPropertyRelative(CELL_IS_FILLED_PROPERTY_NAME);
                tempProperty.boolValue = (!tempProperty.boolValue);
                if (!tempProperty.boolValue)
                {
                    SetCellDelta(layer, xIndex, yIndex, 0f, 0f);
                }
                return tempProperty.boolValue;
            }

            public int GetCellEffect(int layer, int xIndex, int yIndex)
            {
                return layersProperty.GetArrayElementAtIndex(layer).FindPropertyRelative(ROWS_PROPERTY_NAME).GetArrayElementAtIndex(yIndex).FindPropertyRelative(CELLS_PROPERTY_NAME).GetArrayElementAtIndex(xIndex).FindPropertyRelative(EFFECT_PROPERTY_NAME).enumValueIndex;
            }

            public void SetCellEffect(int layer, int xIndex, int yIndex, int newValue)
            {
                SerializedProperty cellProperty = GetCellProperty(layer, xIndex, yIndex);
                cellProperty.FindPropertyRelative(EFFECT_PROPERTY_NAME).enumValueIndex = newValue;
                if (newValue == 0)
                {
                    SerializedProperty goldenTreeIsX2Property = cellProperty.FindPropertyRelative(GOLDEN_TREE_IS_X2_PROPERTY_NAME);
                    if (goldenTreeIsX2Property != null)
                    {
                        goldenTreeIsX2Property.boolValue = false;
                    }
                }
            }

            private SerializedProperty GetCellProperty(int layer, int xIndex, int yIndex)
            {
                return layersProperty.GetArrayElementAtIndex(layer).FindPropertyRelative(ROWS_PROPERTY_NAME).GetArrayElementAtIndex(yIndex).FindPropertyRelative(CELLS_PROPERTY_NAME).GetArrayElementAtIndex(xIndex);
            }

            public float GetCellDeltaX(int layer, int xIndex, int yIndex)
            {
                var deltaProperty = GetCellProperty(layer, xIndex, yIndex).FindPropertyRelative(CELL_DELTA_X_PROPERTY_NAME);
                return deltaProperty != null ? deltaProperty.floatValue : 0f;
            }

            public float GetCellDeltaY(int layer, int xIndex, int yIndex)
            {
                var deltaProperty = GetCellProperty(layer, xIndex, yIndex).FindPropertyRelative(CELL_DELTA_Y_PROPERTY_NAME);
                return deltaProperty != null ? deltaProperty.floatValue : 0f;
            }

            public void SetCellDelta(int layer, int xIndex, int yIndex, float deltaX, float deltaY)
            {
                SerializedProperty cellProperty = GetCellProperty(layer, xIndex, yIndex);
                var deltaXProperty = cellProperty.FindPropertyRelative(CELL_DELTA_X_PROPERTY_NAME);
                var deltaYProperty = cellProperty.FindPropertyRelative(CELL_DELTA_Y_PROPERTY_NAME);
                if (deltaXProperty != null)
                {
                    deltaXProperty.floatValue = deltaX;
                }
                if (deltaYProperty != null)
                {
                    deltaYProperty.floatValue = deltaY;
                }
            }

            public float GetLayerDeltaX(int layerIndex)
            {
                if (layerIndex < 0 || layerIndex >= layersProperty.arraySize)
                    return 0f;
                
                var layerProperty = layersProperty.GetArrayElementAtIndex(layerIndex);
                var deltaXProperty = layerProperty.FindPropertyRelative(DELTA_X_PROPERTY_NAME);
                return deltaXProperty != null ? deltaXProperty.floatValue : 0f;
            }

            public float GetLayerDeltaY(int layerIndex)
            {
                if (layerIndex < 0 || layerIndex >= layersProperty.arraySize)
                    return 0f;
                
                var layerProperty = layersProperty.GetArrayElementAtIndex(layerIndex);
                var deltaYProperty = layerProperty.FindPropertyRelative(DELTA_Y_PROPERTY_NAME);
                return deltaYProperty != null ? deltaYProperty.floatValue : 0f;
            }

            public void SetLayerDeltaX(int layerIndex, float deltaX)
            {
                if (layerIndex < 0 || layerIndex >= layersProperty.arraySize)
                    return;
                
                var layerProperty = layersProperty.GetArrayElementAtIndex(layerIndex);
                var deltaXProperty = layerProperty.FindPropertyRelative(DELTA_X_PROPERTY_NAME);
                if (deltaXProperty != null)
                {
                    deltaXProperty.floatValue = deltaX;
                }
            }

            public void SetLayerDeltaY(int layerIndex, float deltaY)
            {
                if (layerIndex < 0 || layerIndex >= layersProperty.arraySize)
                    return;
                
                var layerProperty = layersProperty.GetArrayElementAtIndex(layerIndex);
                var deltaYProperty = layerProperty.FindPropertyRelative(DELTA_Y_PROPERTY_NAME);
                if (deltaYProperty != null)
                {
                    deltaYProperty.floatValue = deltaY;
                }
            }

            public void HandleSizePropertyChange()
            {
                if (bottomLayerHeightProperty.intValue < 2)
                {
                    bottomLayerHeightProperty.intValue = 2;
                }

                if (bottomLayerWidthProperty.intValue < 2)
                {
                    bottomLayerWidthProperty.intValue = 2;
                }

                
                for (int layerIndex = 0; layerIndex < layersProperty.arraySize; layerIndex++)
                {
                    SetCorrectLevelSize(layerIndex);
                }
            }

            private void SetCorrectLevelSize(int layerIndex)
            {
                int correctWidth;
                int correctHeight;

                if ((layersProperty.arraySize + 1) % 2 == 0)
                {
                    correctWidth = bottomLayerWidthProperty.intValue;
                    correctHeight = bottomLayerHeightProperty.intValue;
                }
                else
                {
                    int offset = GetOffset();
                    correctWidth = bottomLayerWidthProperty.intValue + offset;
                    correctHeight = bottomLayerHeightProperty.intValue + offset;
                }

                for (int i = 0; i < correctHeight; i++)
                {
                    layersProperty.GetArrayElementAtIndex(layerIndex).FindPropertyRelative(ROWS_PROPERTY_NAME).arraySize = correctHeight;
                    layersProperty.GetArrayElementAtIndex(layerIndex).FindPropertyRelative(ROWS_PROPERTY_NAME).GetArrayElementAtIndex(i).FindPropertyRelative(CELLS_PROPERTY_NAME).arraySize = correctWidth;
                }
            }

            public int GetOffset()
            {
                if (layersProperty.arraySize >= 2)
                {
                    return GetLayerHeight(layersProperty.arraySize - 2) - GetLayerHeight(layersProperty.arraySize - 1);
                }
                else
                {
                    return 1;
                }
            }

            public void ClearEffects()
            {
                int layerWidth;
                int layerHeight;

                for (int i = 0; i < layersProperty.arraySize; i++)
                {
                    layerWidth = GetLayerWidth(i);
                    layerHeight = GetLayerHeight(i);

                    for (int y = 0; y < layerHeight; y++)
                    {
                        for (int x = 0; x < layerWidth; x++)
                        {
                            SetCellEffect(i, x, y, 0);
                        }
                    }

                    ClearLayerBoxCovers(i);
                }
            }

            public void ClearLayer(int layerIndex)
            {
                if (layerIndex < 0 || layerIndex >= layersProperty.arraySize)
                {
                    return;
                }

                int layerWidth = GetLayerWidth(layerIndex);
                int layerHeight = GetLayerHeight(layerIndex);

                for (int y = 0; y < layerHeight; y++)
                {
                    for (int x = 0; x < layerWidth; x++)
                    {
                        SetCellValue(layerIndex, x, y, false);
                        SetCellEffect(layerIndex, x, y, 0);
                    }
                }

                ClearLayerBoxCovers(layerIndex);
            }

            public void ClearLayerEffects(int layerIndex)
            {
                if (layerIndex < 0 || layerIndex >= layersProperty.arraySize)
                {
                    return;
                }

                int layerWidth = GetLayerWidth(layerIndex);
                int layerHeight = GetLayerHeight(layerIndex);

                for (int y = 0; y < layerHeight; y++)
                {
                    for (int x = 0; x < layerWidth; x++)
                    {
                        SetCellEffect(layerIndex, x, y, 0);
                    }
                }

                ClearLayerBoxCovers(layerIndex);
            }

            public override string GetLevelLabel(int index, StringBuilder stringBuilder)
            {
                stringBuilder.Clear();
                stringBuilder.Append(NUMBER);

                if (NullLevel)
                {
                    stringBuilder.Append(SEPARATOR);
                    stringBuilder.Append(NULL_FILE);
                    return stringBuilder.ToString();
                }

                EnsureDefaultDataId(index);

                // 行号在列表最左。名称用当前文件名（含 similar 前缀），再接 sets 等。
                stringBuilder.Append(SEPARATOR);
                stringBuilder.Append(levelObject != null ? levelObject.name : GetLayoutSizePrefix());
                stringBuilder.Append(" sets:");
                // Prefer cached counter for the open level; cold labels fall back to LevelModel scan.
                if (elementCounterReady)
                {
                    stringBuilder.Append(SetsAmount);
                }
                else
                {
                    LevelModel levelData = levelObject as LevelModel;
                    stringBuilder.Append(levelData != null ? levelData.SetsAmount : SetsAmount);
                }

                if (HasUnknownTiles())
                {
                    stringBuilder.Append(" [unknown]");
                }

                int boxCoverCount = GetConfiguredBoxCoverCount();
                if (boxCoverCount > 0)
                {
                    stringBuilder.Append(" boxcover");
                    stringBuilder.Append(boxCoverCount);
                }

                if (!IsLevelCorrect)
                {
                    stringBuilder.Append(SEPARATOR);
                    stringBuilder.Append("[Incorrect]");
                }

                return stringBuilder.ToString();
            }

            private string GetLayoutSizePrefix()
            {
                LevelModel levelData = levelObject as LevelModel;
                return levelData != null ? levelData.GetLayoutSizePrefix() : "0r_0c_0";
            }

            private bool HasUnknownTiles()
            {
                int unknownIndex = (int)TileEffectType.Unknown;
                if (elementCounterReady && effectsCounter != null
                    && unknownIndex >= 0 && unknownIndex < effectsCounter.Length)
                {
                    return effectsCounter[unknownIndex] > 0;
                }

                LevelModel levelData = levelObject as LevelModel;
                return levelData != null && levelData.ContainsEffect(TileEffectType.Unknown);
            }

            public override void ValidateLevel() // validation from old editor
            {
                ValidateLevel(checkDataIdUniqueness: true);
            }

            /// <param name="checkDataIdUniqueness">
            /// AssetDatabase.FindAssets over all LevelModel — keep for open/global validate, skip on cell paint.
            /// </param>
            public void ValidateLevel(bool checkDataIdUniqueness)
            {
                errorLabels.Clear();

                if (NullLevel || layersProperty == null || effectsCounter == null)
                {
                    errorLabels.Add("Level reference is missing or failed to load.");
                    return;
                }

                //check layers size

                for (int i = 0; i < effectsCounter.Length; i++)
                {
                    effectsCounter[i] = 0;
                }

                if(layersProperty.arraySize < 2)
                {
                    errorLabels.Add("Level requires at least 2 layers.");
                }

                bool haveOffset = false;
                int offset = GetOffset();

                for (int i = layersProperty.arraySize - 1; i >= 0; i--)
                {
                    if (haveOffset)
                    {
                        if (GetLayerHeight(i) != bottomLayerHeightProperty.intValue + offset)
                        {
                            errorLabels.Add(ResolveLayerLabel(i) + " have incorrect height. Correct height: " + (bottomLayerHeightProperty.intValue + offset));
                        }

                        if (GetLayerWidth(i) != bottomLayerWidthProperty.intValue + offset)
                        {
                            errorLabels.Add(ResolveLayerLabel(i) + " have incorrect width. Correct width: " + (bottomLayerWidthProperty.intValue + offset));
                        }
                    }
                    else
                    {
                        if (GetLayerHeight(i) != bottomLayerHeightProperty.intValue)
                        {
                            errorLabels.Add(ResolveLayerLabel(i) + " have incorrect height. Correct height: " + (bottomLayerHeightProperty.intValue));
                        }

                        if (GetLayerWidth(i) != bottomLayerWidthProperty.intValue)
                        {
                            errorLabels.Add(ResolveLayerLabel(i) + " have incorrect width. Correct width: " + (bottomLayerWidthProperty.intValue));
                        }
                    }

                    haveOffset = (!haveOffset);
                }

                //check for element
                elementCounter = 0;
                elementCounterReady = true;
                int layerWidth;
                int layerHeight;
                int effectValue;
                bool haveEffects = false;
                bool layerIsEmpty;

                for (int i = 0; i < layersProperty.arraySize; i++)
                {
                    layerWidth = GetLayerWidth(i);
                    layerHeight = GetLayerHeight(i);
                    layerIsEmpty = true;

                    for (int x = 0; x < layerWidth; x++)
                    {
                        for (int y = 0; y < layerHeight; y++)
                        {
                            if (GetCellValue(i, x, y))
                            {
                                elementCounter++;
                                layerIsEmpty = false;
                            }

                            effectValue = GetCellEffect(i, x, y);

                            if (effectValue > 0)
                            {
                                effectsCounter[effectValue]++;
                                haveEffects = true;
                            }
                        }
                    }

                    if (layerIsEmpty)
                    {
                        errorLabels.Add($"Layer {i + 1} is empty.");
                    }
                }

                if (elementCounter % 3 != 0)
                {
                    if (elementCounter % 3 == 1)
                    {
                        errorLabels.Add("Incorrect tiles amount.Need 2 more to create a full set.");
                    }
                    else
                    {
                        errorLabels.Add("Incorrect tiles amount.Need 1 more to create a full set.");
                    }
                }

                if (haveEffects)
                {
                    effectsLabel = "Effects: ";
                    bool firstElement = true;

                    for (int i = 0; i < effectsCounter.Length; i++)
                    {
                        if (effectsCounter[i] > 0)
                        {
                            if (firstElement)
                            {
                                effectsLabel += Enum.GetName(typeof(TileEffectType), i) + " x" + effectsCounter[i];
                                firstElement = false;
                            }
                            else
                            {
                                effectsLabel += ", " + Enum.GetName(typeof(TileEffectType), i) + " x" + effectsCounter[i];
                            }
                        }
                    }

                    effectsLabel += ".";
                }
                else
                {
                    effectsLabel = string.Empty;
                }

                LevelModel boxCoverLevel = levelObject as LevelModel;
                if (boxCoverLevel != null)
                {
                    List<string> boxCoverErrors = BoxCoverLayoutResolver.Validate(boxCoverLevel);
                    for (int i = 0; i < boxCoverErrors.Count; i++)
                    {
                        errorLabels.Add(boxCoverErrors[i]);
                    }
                }

                EnsureDefaultDataId(-1);
                if (checkDataIdUniqueness)
                {
                    ValidateDataIdUniqueness();
                }
            }

            /// <summary>
            /// Global-validation only: check filled tiles AABB against LevelTilesField (levelTilesFieldOffset).
            /// </summary>
            public bool TilesFieldExceeded { get; private set; }
            public bool TilesFieldExceededX { get; private set; }
            public bool TilesFieldExceededY { get; private set; }
            public bool TilesFieldUnderfilled { get; private set; }

            public void ValidateTilesFieldBounds()
            {
                TilesFieldExceeded = false;
                TilesFieldExceededX = false;
                TilesFieldExceededY = false;
                TilesFieldUnderfilled = false;

                if (NullLevel || layersProperty == null)
                {
                    return;
                }

                int totalLayerCount = layersProperty.arraySize;
                if (totalLayerCount <= 0)
                {
                    return;
                }

                LevelFitter levelScaler = UnityEngine.Object.FindObjectOfType<LevelFitter>();
                if (levelScaler == null)
                {
                    errorLabels.Add("LevelFitter not found; cannot validate LevelTilesField bounds.");
                    return;
                }

                levelScaler.InitForEditorPreview();
                Vector2 tilesFieldSize = levelScaler.LevelTilesFieldSize;
                if (tilesFieldSize.x <= 0.0001f || tilesFieldSize.y <= 0.0001f)
                {
                    errorLabels.Add("LevelTilesField size is invalid.");
                    return;
                }

                ResolvePreviewLayerSizes(this, totalLayerCount, out Vector2Int evenLayerSize, out Vector2Int oddLayerSize);

                TileGameModel gameData = FindTileGameDataInScene();
                float maxTileSize = gameData != null ? gameData.MaxTileSize : 1.5f;
                Vector2 dataTileSize = gameData != null ? gameData.TileSize : Vector2.one;
                Vector2Int biggerLayerSize = GetMinShowSize();
                float showLayerScale = showLayerScaleProperty != null ? showLayerScaleProperty.floatValue : 1f;
                Vector2 showLayerDelta = new Vector2(
                    showLayerDeltaXProperty != null ? showLayerDeltaXProperty.floatValue : 0f,
                    showLayerDeltaYProperty != null ? showLayerDeltaYProperty.floatValue : 0f);

                Vector2 tileSize = LevelFitter.CalculateTileSize(
                    tilesFieldSize,
                    biggerLayerSize,
                    showLayerScale,
                    maxTileSize,
                    dataTileSize);

                if (!TryComputeFilledTilesAabb(
                        this,
                        totalLayerCount,
                        tileSize,
                        evenLayerSize,
                        oddLayerSize,
                        showLayerDelta,
                        out Vector2 aabbMin,
                        out Vector2 aabbMax))
                {
                    return;
                }

                Vector2 halfField = tilesFieldSize * 0.5f;
                float overflowLeft = Mathf.Max(0f, -halfField.x - aabbMin.x);
                float overflowRight = Mathf.Max(0f, aabbMax.x - halfField.x);
                float overflowBottom = Mathf.Max(0f, -halfField.y - aabbMin.y);
                float overflowTop = Mathf.Max(0f, aabbMax.y - halfField.y);

                const float epsilon = 0.03f;
                bool exceedX = overflowLeft > epsilon || overflowRight > epsilon;
                bool exceedY = overflowBottom > epsilon || overflowTop > epsilon;
                if (exceedX || exceedY)
                {
                    TilesFieldExceeded = true;
                    TilesFieldExceededX = exceedX;
                    TilesFieldExceededY = exceedY;
                    if (exceedX)
                    {
                        errorLabels.Add(
                            $"Tiles exceed LevelTilesField on X (left +{overflowLeft:F2}, right +{overflowRight:F2}).");
                    }

                    if (exceedY)
                    {
                        errorLabels.Add(
                            $"Tiles exceed LevelTilesField on Y (bottom +{overflowBottom:F2}, top +{overflowTop:F2}).");
                    }

                    return;
                }

                // Limiting-axis coverage: after Center Correction this should be ~1.
                // Aspect mismatch leaves slack on the non-limiting axis — that alone is not underfill.
                float aabbWidth = Mathf.Max(0.0001f, aabbMax.x - aabbMin.x);
                float aabbHeight = Mathf.Max(0.0001f, aabbMax.y - aabbMin.y);
                float coverX = aabbWidth / tilesFieldSize.x;
                float coverY = aabbHeight / tilesFieldSize.y;
                float cover = Mathf.Max(coverX, coverY);
                const float underfillTolerance = 0.02f; // 2%
                if (cover < 1f - underfillTolerance)
                {
                    TilesFieldUnderfilled = true;
                    errorLabels.Add(
                        $"Tiles underfill LevelTilesField (cover {cover:P1}, aabb {aabbWidth:F2}x{aabbHeight:F2} vs field {tilesFieldSize.x:F2}x{tilesFieldSize.y:F2}).");
                }
            }

            public int GetLayerHeight(int layerIndex)
            {
                return layersProperty.GetArrayElementAtIndex(layerIndex).FindPropertyRelative(ROWS_PROPERTY_NAME).arraySize;
            }

            public int GetLayerWidth(int layerIndex)
            {
                if (layersProperty.GetArrayElementAtIndex(layerIndex).FindPropertyRelative(ROWS_PROPERTY_NAME).arraySize > 0)
                {
                    return layersProperty.GetArrayElementAtIndex(layerIndex).FindPropertyRelative(ROWS_PROPERTY_NAME).GetArrayElementAtIndex(0).FindPropertyRelative(CELLS_PROPERTY_NAME).arraySize;
                }
                else
                {
                    return 0;
                }
            }

            public string ResolveLayerLabel(int layerIndex)
            {
                return String.Format("Layer # {0} ({1}x{2})", layerIndex + 1, GetLayerWidth(layerIndex), GetLayerHeight(layerIndex));
            }

            public void AddLayer()
            {
                layersProperty.InsertArrayElementAtIndex(0);

                SetCorrectLevelSize(0);
                int layerHeight = GetLayerHeight(0);
                int layerWidth = GetLayerWidth(0);

                for (int i = 0; i < layerWidth; i++)
                {
                    for (int j = 0; j < layerHeight; j++)
                    {
                        SetCellValue(0, i, j, false);
                    }
                }

                ClearLayerBoxCovers(0);
            }

            public void AddLayer(int offset)
            {
                layersProperty.InsertArrayElementAtIndex(0);

                int correctWidth = bottomLayerWidthProperty.intValue + offset;
                int correctHeight = bottomLayerHeightProperty.intValue + offset;

                for (int i = 0; i < correctHeight; i++)
                {
                    layersProperty.GetArrayElementAtIndex(0).FindPropertyRelative(ROWS_PROPERTY_NAME).arraySize = correctHeight;
                    layersProperty.GetArrayElementAtIndex(0).FindPropertyRelative(ROWS_PROPERTY_NAME).GetArrayElementAtIndex(i).FindPropertyRelative(CELLS_PROPERTY_NAME).arraySize = correctWidth;
                }

                for (int i = 0; i < correctWidth; i++)
                {
                    for (int j = 0; j < correctHeight; j++)
                    {
                        SetCellValue(0, i, j, false);
                    }
                }

                ClearLayerBoxCovers(0);
            }

            public int InsertBlankLayerBelow(int layerIndex)
            {
                if (layerIndex < 0 || layerIndex >= layersProperty.arraySize)
                {
                    return layerIndex;
                }

                int insertIndex = layerIndex + 1;
                int refWidth = GetLayerWidth(layerIndex);
                int refHeight = GetLayerHeight(layerIndex);
                int offset = Mathf.Max(1, GetOffset());
                int bottomW = bottomLayerWidthProperty.intValue;
                int bottomH = bottomLayerHeightProperty.intValue;

                int correctWidth;
                int correctHeight;
                if (refWidth == bottomW && refHeight == bottomH)
                {
                    correctWidth = bottomW + offset;
                    correctHeight = bottomH + offset;
                }
                else
                {
                    correctWidth = bottomW;
                    correctHeight = bottomH;
                }

                layersProperty.InsertArrayElementAtIndex(insertIndex);

                SerializedProperty rowsProperty = layersProperty.GetArrayElementAtIndex(insertIndex).FindPropertyRelative(ROWS_PROPERTY_NAME);
                rowsProperty.arraySize = correctHeight;
                for (int i = 0; i < correctHeight; i++)
                {
                    rowsProperty.GetArrayElementAtIndex(i).FindPropertyRelative(CELLS_PROPERTY_NAME).arraySize = correctWidth;
                }

                SetLayerDeltaX(insertIndex, 0f);
                SetLayerDeltaY(insertIndex, 0f);

                for (int x = 0; x < correctWidth; x++)
                {
                    for (int y = 0; y < correctHeight; y++)
                    {
                        SetCellValue(insertIndex, x, y, false);
                        SetCellEffect(insertIndex, x, y, 0);
                        SetCellDelta(insertIndex, x, y, 0f, 0f);
                    }
                }

                ClearLayerBoxCovers(insertIndex);
                SyncBottomLayerSize();
                return insertIndex;
            }

            public void RemoveLayerAt(int layerIndex)
            {
                if (layerIndex < 0 || layerIndex >= layersProperty.arraySize)
                {
                    return;
                }

                bool removingBottomLayer = layerIndex == layersProperty.arraySize - 1;
                layersProperty.DeleteArrayElementAtIndex(layerIndex);

                if (removingBottomLayer && layersProperty.arraySize > 0)
                {
                    SyncBottomLayerSize();
                }
            }

            public int MoveLayerTowardTop(int layerIndex)
            {
                if (layerIndex <= 0 || layerIndex >= layersProperty.arraySize)
                {
                    return layerIndex;
                }

                layersProperty.MoveArrayElement(layerIndex, layerIndex - 1);
                SyncBottomLayerSize();
                return layerIndex - 1;
            }

            public int MoveLayerTowardBottom(int layerIndex)
            {
                if (layerIndex < 0 || layerIndex >= layersProperty.arraySize - 1)
                {
                    return layerIndex;
                }

                layersProperty.MoveArrayElement(layerIndex, layerIndex + 1);
                SyncBottomLayerSize();
                return layerIndex + 1;
            }

            public int MoveLayerTo(int fromIndex, int toIndex)
            {
                if (fromIndex < 0 || fromIndex >= layersProperty.arraySize
                    || toIndex < 0 || toIndex >= layersProperty.arraySize
                    || fromIndex == toIndex)
                {
                    return fromIndex;
                }

                layersProperty.MoveArrayElement(fromIndex, toIndex);
                SyncBottomLayerSize();
                return toIndex;
            }

            public bool TryCopyLayer(int layerIndex, out string json)
            {
                json = null;
                if (!(levelObject is LevelModel levelData))
                {
                    return false;
                }

                Layer layer = levelData.GetLayer(layerIndex);
                if (layer == null)
                {
                    return false;
                }

                json = JsonUtility.ToJson(layer);
                return !string.IsNullOrEmpty(json);
            }

            /// <summary>
            /// Inserts a deserialized copy of a layer as a new layer directly below <paramref name="layerIndex"/>.
            /// </summary>
            public bool TryInsertCopiedLayerBelow(int layerIndex, string json, out int newIndex)
            {
                newIndex = -1;
                if (string.IsNullOrEmpty(json) || !(levelObject is LevelModel levelData))
                {
                    return false;
                }

                if (layerIndex < 0 || layerIndex >= layersProperty.arraySize)
                {
                    return false;
                }

                Layer pasted = JsonUtility.FromJson<Layer>(json);
                if (pasted == null)
                {
                    return false;
                }

                FieldInfo layersField = typeof(LevelModel).GetField("layers", BindingFlags.Instance | BindingFlags.NonPublic);
                if (layersField == null)
                {
                    return false;
                }

                Layer[] layers = layersField.GetValue(levelData) as Layer[];
                if (layers == null || layerIndex >= layers.Length)
                {
                    return false;
                }

                newIndex = layerIndex + 1;
                Layer[] expanded = new Layer[layers.Length + 1];
                for (int i = 0; i < newIndex; i++)
                {
                    expanded[i] = layers[i];
                }

                expanded[newIndex] = pasted;
                for (int i = newIndex; i < layers.Length; i++)
                {
                    expanded[i + 1] = layers[i];
                }

                Undo.RecordObject(levelData, "Paste Layer Below");
                layersField.SetValue(levelData, expanded);
                EditorUtility.SetDirty(levelData);
                serializedLevelObject.Update();
                SyncBottomLayerSize();
                ApplyChanges();
                return true;
            }

            private void SyncBottomLayerSize()
            {
                if (layersProperty.arraySize <= 0)
                {
                    return;
                }

                int bottomIndex = layersProperty.arraySize - 1;
                bottomLayerWidthProperty.intValue = GetLayerWidth(bottomIndex);
                bottomLayerHeightProperty.intValue = GetLayerHeight(bottomIndex);
            }

            public void TrimLevel()
            {
                if(layersProperty.arraySize == 0)
                {
                    Debug.LogError("Attempt to trim level with no layers");
                    return;
                }


                int layer0Width = GetLayerWidth(0);
                int layer0Height = GetLayerHeight(0);

                int layer1Width = 0;
                int layer1Height = 0;

                if(layersProperty.arraySize > 1)
                {
                    layer1Width = GetLayerWidth(1);
                    layer1Height = GetLayerHeight(1);
                }


                int maxLayerWidth = Mathf.Max(layer0Width, layer1Width);
                int maxLayerHeight = Mathf.Max(layer0Height, layer1Height);

                TintMap tintMap = new TintMap(maxLayerWidth, maxLayerHeight);

                int layerWidth;
                int layerHeight;

                for (int layerIndex = layersProperty.arraySize - 1; layerIndex >= 0; layerIndex--)
                {
                    layerWidth = GetLayerWidth(layerIndex);
                    layerHeight = GetLayerHeight(layerIndex);

                    for (int x = 0; x < layerWidth; x++)
                    {
                        for (int y = 0; y < layerHeight; y++)
                        {
                            if (GetCellValue(layerIndex, x, y))
                            {
                                if (layerHeight == maxLayerHeight)
                                {
                                    tintMap.MarkPositionInOuterLayer(x, y);
                                }
                                else
                                {
                                    tintMap.MarkPositionInInnerLayer(x, y);
                                }
                            }
                        }
                    }
                }

                int emptyMapRowsTop = 0;
                int emptyMapColumsLeft = 0;
                int emptyMapRowsBottom = 0;
                int emptyMapColumsRight = 0;

                bool isEmpty = true;

                for (int x = 0; x < (tintMap.maxLayerWidth * 2) && isEmpty; x++)
                {
                    for (int y = 0; y < (tintMap.maxLayerHeight * 2) && isEmpty; y++)
                    {
                        if (tintMap.map[x, y])
                        {
                            isEmpty = false;
                        }
                    }

                    if (isEmpty)
                    {
                        emptyMapColumsLeft++;
                    }
                }


                isEmpty = true;

                for (int y = 0; y < (tintMap.maxLayerHeight * 2) && isEmpty; y++)
                {
                    for (int x = 0; x < (tintMap.maxLayerWidth * 2) && isEmpty; x++)
                    {
                        if (tintMap.map[x, y])
                        {
                            isEmpty = false;
                        }
                    }

                    if (isEmpty)
                    {
                        emptyMapRowsTop++;
                    }
                }

                isEmpty = true;

                for (int x = (tintMap.maxLayerWidth * 2) - 1; (x >= 0) && isEmpty; x--)
                {
                    for (int y = 0; y < (tintMap.maxLayerHeight * 2) && isEmpty; y++)
                    {
                        if (tintMap.map[x, y])
                        {
                            isEmpty = false;
                        }
                    }


                    if (isEmpty)
                    {
                        emptyMapColumsRight++;
                    }
                }

                isEmpty = true;

                for (int y = (tintMap.maxLayerHeight * 2) - 1; (y >= 0) && isEmpty; y--)
                {
                    for (int x = 0; x < (tintMap.maxLayerWidth * 2) && isEmpty; x++)
                    {
                        if (tintMap.map[x, y])
                        {
                            isEmpty = false;
                        }
                    }


                    if (isEmpty)
                    {
                        emptyMapRowsBottom++;
                    }
                }

                int widthOffset;
                int heightOffset;
                int xOffset;
                int yOffset;

                bool needToChangeHeight = (emptyMapRowsTop > 1) || (emptyMapRowsBottom > 1);
                bool needToChangedWidth = (emptyMapColumsRight > 1) || (emptyMapColumsLeft > 1);

                if ((!needToChangedWidth) && (!needToChangeHeight))
                {
                    Debug.LogWarning("Nothing to trim");
                    return;
                }

                xOffset = emptyMapColumsLeft / 2;
                yOffset = emptyMapRowsBottom / 2;
                widthOffset = xOffset + emptyMapColumsRight / 2;
                heightOffset = yOffset + emptyMapRowsTop / 2;

                if((widthOffset >= maxLayerWidth) || (heightOffset >= maxLayerHeight))
                {
                    Debug.LogError("Attempt to trim empty level");
                    return;
                }

                bool cellValue;
                int effectValue;
                float cellDeltaX;
                float cellDeltaY;

                for (int layerIndex = 0; layerIndex < layersProperty.arraySize; layerIndex++)
                {
                    layerWidth = GetLayerWidth(layerIndex);
                    layerHeight = GetLayerHeight(layerIndex);

                    for (int y = 0; y + yOffset < layerHeight; y++)
                    {
                        for (int x = 0; x + xOffset < layerWidth; x++)
                        {
                            cellValue = GetCellValue(layerIndex, x + xOffset, y + yOffset);
                            effectValue = GetCellEffect(layerIndex, x + xOffset, y + yOffset);
                            cellDeltaX = GetCellDeltaX(layerIndex, x + xOffset, y + yOffset);
                            cellDeltaY = GetCellDeltaY(layerIndex, x + xOffset, y + yOffset);
                            SetCellValue(layerIndex, x + xOffset, y + yOffset, false);
                            SetCellEffect(layerIndex, x + xOffset, y + yOffset, 0);
                            SetCellDelta(layerIndex, x + xOffset, y + yOffset, 0f, 0f);
                            SetCellValue(layerIndex, x, y, cellValue);
                            SetCellEffect(layerIndex, x, y, effectValue);
                            SetCellDelta(layerIndex, x, y, cellDeltaX, cellDeltaY);
                        }

                        if (needToChangedWidth)
                        {
                            layersProperty.GetArrayElementAtIndex(layerIndex).FindPropertyRelative(ROWS_PROPERTY_NAME).GetArrayElementAtIndex(y).FindPropertyRelative(CELLS_PROPERTY_NAME).arraySize = layerWidth - widthOffset;
                        }
                    }

                    if (needToChangeHeight)
                    {
                        layersProperty.GetArrayElementAtIndex(layerIndex).FindPropertyRelative(ROWS_PROPERTY_NAME).arraySize = layerHeight - heightOffset;
                    }
                }

                bottomLayerWidthProperty.intValue = GetLayerWidth(layersProperty.arraySize - 1);
                bottomLayerHeightProperty.intValue = GetLayerHeight(layersProperty.arraySize - 1);
            }
        }
    }
}

// -----------------
// 2d grid level editor
// -----------------

// Changelog
// v 1.2
// • Reordered some methods
// v 1.1
// • Added global validation
// • Added validation example
// • Fixed mouse click bug
// v 1 basic version works

#endif