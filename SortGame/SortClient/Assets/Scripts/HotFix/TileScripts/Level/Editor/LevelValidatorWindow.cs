
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using FishFramework;
using UnityEditor;
using UnityEngine;
using FishTilePlay;

namespace TileGameCore
{
    public class LevelValidatorWindow : EditorWindow
    {
        private LevelCatalog levelDatabase;
        private Vector2 scrollPosition;
        private List<LevelValidationResult> validationResults = new List<LevelValidationResult>();
        private bool showOnlyErrors = false;
        private bool autoFixEnabled = false;
        private int rangeStart = 1;
        private int rangeEnd = 1000;
        private bool exportConfigAudit = true;
        
        [System.Serializable]
        public class LevelValidationResult
        {
            public int levelIndex;
            public string levelName;
            public int tileCount;
            public int setsAmount;
            public bool isValid;
            public string errorMessage;
            public int tilesNeeded;
            public LevelModel levelData;
            
            public LevelValidationResult(int index, string name, LevelModel data)
            {
                levelIndex = index;
                levelName = name;
                levelData = data;
                ValidateLevel();
            }
            
            public void ValidateLevel()
            {
                if (levelData == null)
                {
                    isValid = false;
                    errorMessage = "关卡数据为空";
                    return;
                }
                
                tileCount = levelData.GetAmountOfFilledCells();
                setsAmount = levelData.SetsAmount;
                
                if (tileCount % 3 != 0)
                {
                    isValid = false;
                    if (tileCount % 3 == 1)
                    {
                        tilesNeeded = 2;
                        errorMessage = $"瓦片数量不正确，需要再添加2个瓦片才能组成完整集合";
                    }
                    else
                    {
                        tilesNeeded = 1;
                        errorMessage = $"瓦片数量不正确，需要再添加1个瓦片才能组成完整集合";
                    }
                }
                else
                {
                    isValid = true;
                    errorMessage = "瓦片数量正确";
                    tilesNeeded = 0;
                }
            }
        }
        
        [MenuItem("Tools/关卡验证器/瓦片数量检测")]
        public static void ShowWindow()
        {
            LevelValidatorWindow window = GetWindow<LevelValidatorWindow>("关卡瓦片数量检测器");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }
        
        private void OnEnable()
        {
            LoadLevelDatabase();
        }
        
        private void LoadLevelDatabase()
        {
            levelDatabase = LevelPlatformResolver.LoadDatabaseInEditor();
            if (levelDatabase == null)
            {
                Debug.LogError(string.Format(
                    "未找到当前平台关卡数据库 (LevelCatalog)：{0}",
                    LevelPlatformResolver.GetDatabasePath()));
            }
        }
        
        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            
            // 标题
            EditorGUILayout.LabelField("关卡瓦片数量检测器", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // 关卡数据库选择
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("关卡数据库:", GUILayout.Width(80));
            levelDatabase = (LevelCatalog)EditorGUILayout.ObjectField(levelDatabase, typeof(LevelCatalog), false);
            if (GUILayout.Button("重新加载", GUILayout.Width(80)))
            {
                LoadLevelDatabase();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // 控制选项
            EditorGUILayout.BeginHorizontal();
            showOnlyErrors = EditorGUILayout.Toggle("仅显示错误", showOnlyErrors, GUILayout.Width(100));
            autoFixEnabled = EditorGUILayout.Toggle("自动修复模式", autoFixEnabled, GUILayout.Width(120));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("关卡号:", GUILayout.Width(70));
            rangeStart = EditorGUILayout.IntField(rangeStart, GUILayout.Width(80));
            EditorGUILayout.LabelField("-", GUILayout.Width(12));
            rangeEnd = EditorGUILayout.IntField(rangeEnd, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            exportConfigAudit = EditorGUILayout.ToggleLeft("是否生成配置查验表", exportConfigAudit);
            
            EditorGUILayout.Space();
            
            // 检测按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("开始检测", GUILayout.Height(30)))
            {
                ValidateAllLevels();
            }
            
            if (validationResults.Count > 0 && GUILayout.Button("生成修复报告", GUILayout.Height(30), GUILayout.Width(120)))
            {
                GenerateFixReport();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // 结果显示
            if (validationResults.Count > 0)
            {
                ShowValidationResults();
            }
            else
            {
                EditorGUILayout.HelpBox("设置检测区间后，点击'开始检测'按钮来验证区间内的关卡数据", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void ValidateAllLevels()
        {
            if (levelDatabase == null)
            {
                EditorUtility.DisplayDialog("错误", "请先选择关卡数据库", "确定");
                return;
            }
            
            int lastNumber = levelDatabase.AmountOfLevels;
            if (lastNumber <= 0)
            {
                EditorUtility.DisplayDialog("错误", "关卡数据库中没有关卡", "确定");
                return;
            }
            
            int startNumber = Mathf.Clamp(Mathf.Min(rangeStart, rangeEnd), 1, lastNumber);
            int endNumber = Mathf.Clamp(Mathf.Max(rangeStart, rangeEnd), startNumber, lastNumber);
            int start = startNumber - 1;
            int end = endNumber - 1;
            
            validationResults.Clear();
            
            int span = end - start + 1;
            EditorUtility.DisplayProgressBar("检测关卡", "正在验证关卡数据...", 0);
            
            try
            {
                for (int i = start; i <= end; i++)
                {
                    int done = i - start;
                    EditorUtility.DisplayProgressBar("检测关卡", $"正在检测关卡 [{i + 1}] ({done + 1}/{span})", (float)done / span);
                    
                    LevelModel levelData = LevelForValidation(i);
                    string levelName = levelData != null ? levelData.name : $"关卡 {i + 1}";
                    
                    LevelValidationResult result = new LevelValidationResult(i + 1, levelName, levelData);
                    validationResults.Add(result);
                }

                if (exportConfigAudit)
                {
                    EditorUtility.DisplayProgressBar("检测关卡", "正在生成配置查验表...", 1f);
                    try
                    {
                        string assetPath = AssetDatabase.GetAssetPath(levelDatabase);
                        LevelPlatform platform = assetPath == LevelPlatformResolver.GetDatabasePath(LevelPlatform.IOS)
                            ? LevelPlatform.IOS
                            : LevelPlatform.Android;
                        string reportPath = LevelConfigAudit.Export(platform, start, end, ResolvedLevels(start, end));
                        Debug.Log("配置查验表已生成：" + reportPath);
                        EditorUtility.RevealInFinder(reportPath);
                    }
                    catch (System.Exception exception)
                    {
                        Debug.LogError(exception);
                        EditorUtility.DisplayDialog("配置查验表生成失败", exception.Message, "确定");
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            
            // 统计结果
            int errorCount = validationResults.Count(r => !r.isValid);
            int totalCount = validationResults.Count;
            
            Debug.Log($"关卡检测完成！区间 [{start + 1}-{end + 1}] 共 {totalCount} 个关卡，其中 {errorCount} 个关卡存在问题");
        }

        private bool UsesCurrentPlayOrder()
        {
            string path = AssetDatabase.GetAssetPath(levelDatabase);
            return path == LevelPlatformResolver.GetDatabasePath();
        }

        private LevelModel LevelForValidation(int index)
        {
            LevelModel referenced = levelDatabase.Levels[index];
            if (!UsesCurrentPlayOrder())
            {
                return referenced;
            }

            return LevelListSlot.Resolve(referenced, index);
        }

        private LevelModel[] ResolvedLevels(int start, int end)
        {
            LevelModel[] source = levelDatabase.Levels;
            LevelModel[] resolved = new LevelModel[source.Length];
            for (int i = start; i <= end && i < source.Length; i++)
            {
                resolved[i] = LevelForValidation(i);
            }

            return resolved;
        }
        
        private void ShowValidationResults()
        {
            var resultsToShow = showOnlyErrors ? validationResults.Where(r => !r.isValid) : validationResults;
            var resultsList = resultsToShow.ToList();
            
            // 统计信息
            int totalCount = validationResults.Count;
            int errorCount = validationResults.Count(r => !r.isValid);
            int validCount = totalCount - errorCount;
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"总计: {totalCount} 个关卡", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"正确: {validCount}", GUILayout.Width(80));
            EditorGUILayout.LabelField($"错误: {errorCount}", GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
            
            string problemLevels = errorCount == 0
                ? "无"
                : string.Join("；", validationResults.Where(r => !r.isValid).Select(r => $"L{r.levelIndex}"));
            EditorGUILayout.LabelField("问题关卡:", EditorStyles.boldLabel);
            float problemHeight = Mathf.Max(
                EditorGUIUtility.singleLineHeight,
                EditorStyles.textField.CalcHeight(new GUIContent(problemLevels), Mathf.Max(100f, EditorGUIUtility.currentViewWidth - 40f)));
            EditorGUILayout.SelectableLabel(problemLevels, EditorStyles.textField, GUILayout.Height(problemHeight));
            
            EditorGUILayout.Space();
            
            // 结果列表
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            foreach (var result in resultsList)
            {
                ShowLevelResult(result);
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void ShowLevelResult(LevelValidationResult result)
        {
            EditorGUILayout.BeginVertical("box");
            
            // 关卡信息
            EditorGUILayout.BeginHorizontal();
            
            // 状态图标
            GUIStyle statusStyle = new GUIStyle(EditorStyles.label);
            statusStyle.normal.textColor = result.isValid ? Color.green : Color.red;
            EditorGUILayout.LabelField(result.isValid ? "✓" : "✗", statusStyle, GUILayout.Width(20));
            
            // 关卡名称和索引
            EditorGUILayout.LabelField($"[{result.levelIndex}] {result.levelName}", EditorStyles.boldLabel);
            
            // 瓦片数量信息
            EditorGUILayout.LabelField($"瓦片数: {result.tileCount}", GUILayout.Width(80));
            EditorGUILayout.LabelField($"集合数: {result.setsAmount}", GUILayout.Width(80));
            
            EditorGUILayout.EndHorizontal();
            
            // 错误信息
            if (!result.isValid)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("错误:", GUILayout.Width(40));
                EditorGUILayout.LabelField(result.errorMessage, EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndHorizontal();
                
                // 修复按钮
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("查看关卡", GUILayout.Width(80)))
                {
                    Selection.activeObject = result.levelData;
                    EditorGUIUtility.PingObject(result.levelData);
                }
                
                if (GUILayout.Button("打开编辑器", GUILayout.Width(100)))
                {
                    OpenLevelEditor(result.levelData);
                }
                
                if (autoFixEnabled && GUILayout.Button("自动修复", GUILayout.Width(80)))
                {
                    AutoFixLevel(result);
                }
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
        
        private void OpenLevelEditor(LevelModel levelData)
        {
            // 打开关卡编辑器窗口
            var editorWindow = EditorWindow.GetWindow<LevelEditorWindow>();
            if (editorWindow != null)
            {
                // 这里需要根据实际的LevelEditorWindow实现来设置选中的关卡
                // 可能需要通过反射或公共方法来设置当前编辑的关卡
                Debug.Log($"尝试打开关卡编辑器: {levelData.name}");
            }
        }
        
        private void AutoFixLevel(LevelValidationResult result)
        {
            if (result.levelData == null || result.tilesNeeded <= 0)
                return;
            
            // 自动修复逻辑：在第一个图层的第一个空位置添加瓦片
            if (TryAddTilesToLevel(result.levelData, result.tilesNeeded))
            {
                EditorUtility.SetDirty(result.levelData);
                AssetDatabase.SaveAssets();
                
                // 重新验证
                result.ValidateLevel();
                Debug.Log($"已自动修复关卡 {result.levelName}，添加了 {result.tilesNeeded} 个瓦片");
            }
            else
            {
                Debug.LogWarning($"无法自动修复关卡 {result.levelName}：没有找到合适的空位置");
            }
        }
        
        private bool TryAddTilesToLevel(LevelModel levelData, int tilesToAdd)
        {
            // 由于LevelData的layers字段是私有的，我们需要通过反射来访问
            // 或者创建一个公共方法来添加瓦片
            // 这里提供一个简化的实现，建议手动修复
            
            Debug.Log($"建议手动修复关卡 {levelData.name}：");
            Debug.Log($"当前瓦片数: {levelData.GetAmountOfFilledCells()}");
            Debug.Log($"需要添加: {tilesToAdd} 个瓦片");
            Debug.Log($"修复后瓦片数: {levelData.GetAmountOfFilledCells() + tilesToAdd}");
            Debug.Log($"修复后集合数: {(levelData.GetAmountOfFilledCells() + tilesToAdd) / 3}");
            
            return false; // 需要手动修复
        }
        
        private void GenerateFixReport()
        {
            var errorResults = validationResults.Where(r => !r.isValid).ToList();
            
            if (errorResults.Count == 0)
            {
                Debug.Log("所有关卡瓦片数量都正确！");
                return;
            }
            
            Debug.Log("=== 关卡瓦片数量修复报告 ===");
            Debug.Log($"发现 {errorResults.Count} 个关卡存在问题：");
            Debug.Log("");
            
            foreach (var result in errorResults)
            {
                Debug.Log($"关卡 [{result.levelIndex}] {result.levelName}:");
                Debug.Log($"  当前瓦片数: {result.tileCount}");
                Debug.Log($"  需要添加: {result.tilesNeeded} 个瓦片");
                Debug.Log($"  修复后瓦片数: {result.tileCount + result.tilesNeeded}");
                Debug.Log($"  修复后集合数: {(result.tileCount + result.tilesNeeded) / 3}");
                Debug.Log($"  错误信息: {result.errorMessage}");
                Debug.Log("");
            }
            
            Debug.Log("=== 修复建议 ===");
            Debug.Log("1. 打开关卡编辑器");
            Debug.Log("2. 选择有问题的关卡");
            Debug.Log("3. 在空位置添加相应数量的瓦片");
            Debug.Log("4. 确保瓦片总数是3的倍数");
        }
    }
}

#endif
