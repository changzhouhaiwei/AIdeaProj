#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FishXSortPlay
{
    public sealed class LevelDifficultyPreRunWindow : EditorWindow
    {
        const string GameScenePath = "Assets/CABRes/Scenes/GameScene.unity";
        const string LevelFromPref = "LevelPreRun.Window.LevelFrom";
        const string LevelToPref = "LevelPreRun.Window.LevelTo";
        const string RunsPref = "LevelPreRun.Window.Runs";
        const string SeedPref = "LevelPreRun.Window.Seed";

        int _levelFrom = 1;
        int _levelTo = 10;
        int _runs = 200;
        long _seed;
        Vector2 _scroll;

        [MenuItem("Tools/关卡验证器/上线前难度预跑")]
        public static void ShowWindow()
        {
            var window = GetWindow<LevelDifficultyPreRunWindow>("关卡难度预跑");
            window.minSize = new Vector2(420, 400);
            window.Show();
        }

        void OnEnable()
        {
            int legacy = EditorPrefs.GetInt("LevelPreRun.Window.LevelId", 0);
            _levelFrom = EditorPrefs.GetInt(LevelFromPref, legacy > 0 ? legacy : 1);
            _levelTo = EditorPrefs.GetInt(LevelToPref, legacy > 0 ? legacy : 10);
            _runs = EditorPrefs.GetInt(RunsPref, 200);
            if (!long.TryParse(EditorPrefs.GetString(SeedPref, ""), out _seed))
                _seed = EditorPrefs.GetInt(SeedPref, 0);
        }

        void OnInspectorUpdate()
        {
            Repaint();
        }

        void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("CSVGameLevelCfg 预跑", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "按关卡区间跑 defaultCfg / win1–win4。选子用 P1→P5，看不见由遮挡面积决定。不打开游戏 UI、不播任何动效。\n整批跑完后会把 runs.csv / summary.csv 发给 chengbo@sofish.com、xieyasi@sofish.com、wanghaiwei@sofish.com。\n单局卡住（超时/报错）会把 error.log 和已跑完的 csv 一并发出，然后跳过该局继续。",
                MessageType.Info);

            int levelFrom = EditorGUILayout.IntField("起始关卡", _levelFrom);
            int levelTo = EditorGUILayout.IntField("结束关卡", _levelTo);
            int runs = EditorGUILayout.IntField("每参数局数", _runs);
            if (levelFrom != _levelFrom || levelTo != _levelTo || runs != _runs)
            {
                _levelFrom = Mathf.Max(1, levelFrom);
                _levelTo = Mathf.Max(1, levelTo);
                _runs = Mathf.Clamp(runs, 1, 5000);
                EditorPrefs.SetInt(LevelFromPref, _levelFrom);
                EditorPrefs.SetInt(LevelToPref, _levelTo);
                EditorPrefs.SetInt(RunsPref, _runs);
            }

            if (_levelTo < _levelFrom)
                EditorGUILayout.HelpBox("结束关卡小于起始关卡时，会自动对调后开跑。", MessageType.None);

            EditorGUILayout.Space(6);
            using (new EditorGUI.DisabledScope(LevelDifficultyPreRun.IsBusy))
            {
                if (GUILayout.Button(EditorApplication.isPlaying ? "开始预跑" : "进入 Play 并预跑", GUILayout.Height(28)))
                    StartPreRun();
            }

            bool canResume = LevelDifficultyPreRun.TryGetResume(out LevelDifficultyPreRun.ResumeInfo resume)
                && !resume.Completed;
            using (new EditorGUI.DisabledScope(LevelDifficultyPreRun.IsBusy || !canResume))
            {
                string continueLabel = EditorApplication.isPlaying ? "继续上次" : "进入 Play 并继续上次";
                if (GUILayout.Button(continueLabel, GUILayout.Height(28)))
                    StartContinue();
            }

            if (canResume)
            {
                string progress = resume.Expected > 0
                    ? $"{resume.Done}/{resume.Expected}"
                    : resume.Done.ToString();
                EditorGUILayout.HelpBox(
                    $"可继续：关卡 {resume.LevelFrom}-{resume.LevelTo}，每档 {resume.Runs} 局，已落盘 {progress}。中断的那一局会重跑。",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("没有未完成的本地预跑。新开的任务会边跑边写 runs.csv，中途停下后可点继续。", MessageType.None);
            }

            using (new EditorGUI.DisabledScope(!LevelDifficultyPreRun.IsBusy))
            {
                if (GUILayout.Button("停止", GUILayout.Height(22)))
                    LevelDifficultyPreRun.Instance?.StopRun();
            }

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("按种子还原", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "把 runs.csv 里的种子贴进来。同样不播 UI/动效，按当时关卡、档位、配色瞬间进关。",
                MessageType.Info);

            long seed = EditorGUILayout.LongField("种子号", _seed);
            if (seed != _seed)
            {
                _seed = seed;
                EditorPrefs.SetString(SeedPref, _seed.ToString());
            }

            if (LevelDifficultySeed.TryDecode(_seed, out int decodedLevel, out int slotIndex, out int runIndex))
            {
                EditorGUILayout.LabelField(
                    $"将还原：关卡 {decodedLevel}  {LevelDifficultySeed.SlotName(slotIndex)}  第 {runIndex + 1} 局");
            }
            else if (_seed != 0)
            {
                EditorGUILayout.HelpBox("种子格式不对，请填预跑 CSV 里的种子整数。", MessageType.Warning);
            }

            using (new EditorGUI.DisabledScope(LevelDifficultyPreRun.IsBusy || _seed == 0))
            {
                if (GUILayout.Button(EditorApplication.isPlaying ? "按种子还原" : "进入 Play 并还原", GUILayout.Height(28)))
                    StartReplay();
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("状态", LevelDifficultyPreRun.Status ?? "Idle");

            if (!string.IsNullOrEmpty(LevelDifficultyPreRun.LastOutputDir))
            {
                EditorGUILayout.LabelField("输出", LevelDifficultyPreRun.LastOutputDir);
                if (GUILayout.Button("打开输出目录"))
                    EditorUtility.RevealInFinder(LevelDifficultyPreRun.LastOutputDir);
            }

            if (!string.IsNullOrEmpty(LevelDifficultyPreRun.LastMailStatus))
                EditorGUILayout.HelpBox(LevelDifficultyPreRun.LastMailStatus, MessageType.None);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (!string.IsNullOrEmpty(LevelDifficultyPreRun.LastSummary))
                EditorGUILayout.TextArea(LevelDifficultyPreRun.LastSummary, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        void StartPreRun()
        {
            LevelDifficultyPreRun.QueueStart(_levelFrom, _levelTo, _runs);
            EnterPlayOrRunPending();
        }

        void StartContinue()
        {
            if (!LevelDifficultyPreRun.TryGetResume(out LevelDifficultyPreRun.ResumeInfo info) || info.Completed)
            {
                EditorUtility.DisplayDialog("继续预跑", "没有未完成的本地预跑。", "OK");
                return;
            }

            LevelDifficultyPreRun.QueueContinue();
            EnterPlayOrRunPending();
        }

        void StartReplay()
        {
            if (!LevelDifficultySeed.TryDecode(_seed, out _, out _, out _))
            {
                EditorUtility.DisplayDialog("按种子还原", "种子格式不对，请填预跑 CSV 里的种子整数。", "OK");
                return;
            }

            LevelDifficultyPreRun.QueueReplay(_seed);
            EnterPlayOrRunPending();
        }

        void EnterPlayOrRunPending()
        {
            if (EditorApplication.isPlaying)
            {
                if (LevelDifficultyPreRun.Instance == null)
                {
                    EditorUtility.DisplayDialog("关卡预跑",
                        "当前 Play 场景没有 GameDirector。请停掉 Play，再点「进入 Play 并预跑」。",
                        "OK");
                    return;
                }

                LevelDifficultyPreRun.TryStartPending();
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(GameScenePath);
            GameDirector.AutoRunLevelInEditor = false;
            EditorApplication.isPlaying = true;
        }
    }
}
#endif
