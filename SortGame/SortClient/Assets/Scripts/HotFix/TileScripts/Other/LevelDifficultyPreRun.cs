#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using cfg.Game;
using TileGameCore;
using UnityEditor;
using UnityEngine;

namespace FishTilePlay
{
    public sealed class LevelDifficultyPreRun : MonoBehaviour
    {
        const string PendingKey = "LevelPreRun.Pending";
        const string ModeKey = "LevelPreRun.Mode";
        const string LevelFromKey = "LevelPreRun.LevelFrom";
        const string LevelToKey = "LevelPreRun.LevelTo";
        const string RunsKey = "LevelPreRun.Runs";
        const string SeedKey = "LevelPreRun.Seed";
        const string ResumeDirKey = "LevelPreRun.ResumeDir";
        const string LastJobDirPref = "LevelPreRun.LastJobDir";
        const int ModeBatch = 0;
        const int ModeReplay = 1;
        const int ModeContinue = 2;
        const int StepsPerFrame = 16;
        const string JobFileName = "job.json";
        const string RunsFileName = "runs.csv";
        const string SummaryFileName = "summary.csv";
        const string ErrorLogName = "error.log";
        const float StuckSeconds = 180f;
        const int LogCap = 400;

        public static LevelDifficultyPreRun Instance { get; private set; }

        public static bool IsBusy { get; private set; }
        public static string Status { get; private set; } = "Idle";
        public static string LastOutputDir { get; private set; }
        public static string LastSummary { get; private set; }
        public static string LastMailStatus { get; private set; }

        readonly PlayBotPlanner _planner = new PlayBotPlanner();
        readonly object _logGate = new object();
        readonly List<string> _capturedLogs = new List<string>();
        Coroutine _loop;
        bool _abortGame;
        bool _gameInProgress;
        float _gameStartRealtime;
        string _stuckReason;

        public static bool HasPending => SessionState.GetBool(PendingKey, false);

        public static void Bind(GameDirector host)
        {
            if (host == null)
                return;
            if (host.GetComponent<LevelDifficultyPreRun>() == null)
                host.gameObject.AddComponent<LevelDifficultyPreRun>();
        }

        public static void QueueStart(int levelFrom, int levelTo, int runs)
        {
            SessionState.SetBool(PendingKey, true);
            SessionState.SetInt(ModeKey, ModeBatch);
            SessionState.SetInt(LevelFromKey, levelFrom);
            SessionState.SetInt(LevelToKey, levelTo);
            SessionState.SetInt(RunsKey, runs);
        }

        public static void QueueContinue()
        {
            if (!TryGetResume(out ResumeInfo info) || info.Completed)
                return;

            SessionState.SetBool(PendingKey, true);
            SessionState.SetInt(ModeKey, ModeContinue);
            SessionState.SetString(ResumeDirKey, info.Dir);
        }

        public static void QueueReplay(long seed)
        {
            SessionState.SetBool(PendingKey, true);
            SessionState.SetInt(ModeKey, ModeReplay);
            SessionState.SetString(SeedKey, seed.ToString());
        }

        public static void TryStartPending()
        {
            if (!HasPending)
                return;

            int mode = SessionState.GetInt(ModeKey, ModeBatch);
            SessionState.SetBool(PendingKey, false);

            if (Instance == null)
                return;

            if (mode == ModeReplay)
            {
                long.TryParse(SessionState.GetString(SeedKey, "0"), out long replaySeed);
                Instance.StartReplay(replaySeed);
            }
            else if (mode == ModeContinue)
                Instance.StartContinue(SessionState.GetString(ResumeDirKey, ""));
            else
                Instance.StartRun(
                    SessionState.GetInt(LevelFromKey, 1),
                    SessionState.GetInt(LevelToKey, 1),
                    SessionState.GetInt(RunsKey, 200));
        }

        public static void RequestStart(int levelFrom, int levelTo, int runs)
        {
            if (Instance != null)
            {
                Instance.StartRun(levelFrom, levelTo, runs);
                return;
            }

            QueueStart(levelFrom, levelTo, runs);
        }

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            StopRun();
        }

        public void StartRun(int levelFrom, int levelTo, int runs)
        {
            if (IsBusy)
            {
                Debug.LogWarning("[LevelPreRun] already running");
                return;
            }

            if (_loop != null)
                StopCoroutine(_loop);
            PlayBotStepLog.Console = false;
            PlayBotStepLog.File = false;
            _loop = StartCoroutine(RunBatch(levelFrom, levelTo, Mathf.Max(1, runs), null));
        }

        public void StartContinue(string resumeDir)
        {
            if (IsBusy)
            {
                Debug.LogWarning("[LevelPreRun] already running");
                return;
            }

            if (!TryReadResume(resumeDir, out ResumeInfo info) || info.Completed)
            {
                Status = "没有可继续的预跑";
                Debug.LogWarning("[LevelPreRun] " + Status);
                return;
            }

            if (_loop != null)
                StopCoroutine(_loop);
            PlayBotStepLog.Console = false;
            PlayBotStepLog.File = false;
            _loop = StartCoroutine(RunBatch(info.LevelFrom, info.LevelTo, Mathf.Max(1, info.Runs), info.Dir));
        }

        public void StartReplay(long seed)
        {
            if (IsBusy)
            {
                Debug.LogWarning("[LevelPreRun] already running");
                return;
            }

            if (_loop != null)
                StopCoroutine(_loop);
            PlayBotStepLog.Console = false;
            PlayBotStepLog.File = false;
            _loop = StartCoroutine(RunReplay(seed));
        }

        public void StopRun()
        {
            if (_loop != null)
            {
                StopCoroutine(_loop);
                _loop = null;
            }

            IsBusy = false;
            PlayBotStepLog.Console = true;
            PlayBotStepLog.File = true;
            EndLogCapture();
            LevelDifficultySim.EndBatch();
            LevelDifficultySeed.IsReplayLoad = false;
            LevelDifficultySeed.PendingInitState = 0;
            if (Status == null || !Status.StartsWith("Done", StringComparison.Ordinal))
                Status = "Stopped";
            if (!string.IsNullOrEmpty(LastOutputDir))
                FlushJobFromDisk(LastOutputDir, completed: false);
        }

        IEnumerator RunBatch(int levelFrom, int levelTo, int runs, string resumeDir)
        {
            if (levelTo < levelFrom)
            {
                int swap = levelFrom;
                levelFrom = levelTo;
                levelTo = swap;
            }

            IsBusy = true;
            LastSummary = "";
            LastMailStatus = "";

            if (PlayBotController.Instance != null)
                PlayBotController.Instance.StopBot(restoreTimeScale: true);

            bool resume = !string.IsNullOrEmpty(resumeDir);
            string outDir;
            JobState job;
            var allRuns = new List<RunRecord>();
            var doneSeeds = new HashSet<long>();

            if (resume)
            {
                outDir = Path.GetFullPath(resumeDir);
                job = LoadJob(outDir) ?? new JobState
                {
                    levelFrom = levelFrom,
                    levelTo = levelTo,
                    runs = runs
                };
                levelFrom = job.levelFrom;
                levelTo = job.levelTo;
                runs = Mathf.Max(1, job.runs);
                allRuns = LoadRuns(outDir);
                for (int i = 0; i < allRuns.Count; i++)
                    doneSeeds.Add(allRuns[i].Seed);
                Status = $"继续 已跳过 {doneSeeds.Count} 局  {outDir}";
                Debug.Log("[LevelPreRun] resume " + outDir + " skip=" + doneSeeds.Count);
            }
            else
            {
                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string folder = levelFrom == levelTo
                    ? $"level_{levelFrom}_{stamp}"
                    : $"levels_{levelFrom}-{levelTo}_{stamp}";
                outDir = Path.GetFullPath(Path.Combine(RootDir, folder));
                Directory.CreateDirectory(outDir);
                job = new JobState
                {
                    levelFrom = levelFrom,
                    levelTo = levelTo,
                    runs = runs,
                    expectedRuns = CountExpected(levelFrom, levelTo, runs)
                };
                SaveJob(outDir, job);
                EnsureRunsHeader(outDir);
            }

            LastOutputDir = outDir;
            EditorPrefs.SetString(LastJobDirPref, outDir);

            LevelDifficultySim.BeginBatch();
            BeginLogCapture();
            var summaries = new List<CfgSummary>();
            int ranLevels = 0;
            int newRuns = 0;

            try
            {
                for (int levelId = levelFrom; levelId <= levelTo; levelId++)
                {
                    GameLevelConfig cfg = LevelDynamicConfigProvider.GetLevelConfig(levelId);
                    if (cfg == null)
                    {
                        Debug.LogWarning($"[LevelPreRun] 关卡 {levelId} 没有 CSVGameLevelCfg，跳过");
                        continue;
                    }

                    var jobs = CollectJobs(cfg);
                    if (jobs.Count == 0)
                    {
                        Debug.LogWarning($"[LevelPreRun] 关卡 {levelId} 没有可跑的 defaultCfg/win1-4，跳过");
                        continue;
                    }

                    ranLevels++;
                    for (int j = 0; j < jobs.Count; j++)
                    {
                        CfgJob cfgJob = jobs[j];
                        var summary = new CfgSummary
                        {
                            LevelId = levelId,
                            Slot = cfgJob.Slot,
                            Cfg = cfgJob.Cfg
                        };
                        FillSummaryFromRuns(summary, allRuns);

                        for (int i = 0; i < runs; i++)
                        {
                            long seed = LevelDifficultySeed.Encode(levelId, cfgJob.SlotIndex, i);
                            if (doneSeeds.Contains(seed))
                                continue;

                            Status = $"Lv {levelId}/{levelTo}  {cfgJob.Slot}  {i + 1}/{runs}  已有{doneSeeds.Count}";
                            int before = allRuns.Count;
                            _stuckReason = null;
                            _abortGame = false;
                            yield return RunOneGame(levelId, cfgJob.Cfg, seed, summary, allRuns, i);

                            if (allRuns.Count > before)
                            {
                                RunRecord added = allRuns[allRuns.Count - 1];
                                AppendRunLine(outDir, added);
                                doneSeeds.Add(added.Seed);
                                newRuns++;
                            }
                            else if (!string.IsNullOrEmpty(_stuckReason))
                            {
                                Record(summary, allRuns, i, seed, false, RemainingTiles(), 0, new int[6]);
                                RunRecord added = allRuns[allRuns.Count - 1];
                                AppendRunLine(outDir, added);
                                doneSeeds.Add(added.Seed);
                                newRuns++;
                            }

                            if (!string.IsNullOrEmpty(_stuckReason))
                            {
                                var snap = new List<CfgSummary>(summaries) { summary };
                                WriteRows(Path.Combine(outDir, SummaryFileName), BuildSummaryRows(snap));
                                SaveJob(outDir, job);
                                yield return NotifyStuck(
                                    outDir, levelFrom, levelTo, levelId, cfgJob.Slot, seed, i + 1, runs);
                                _stuckReason = null;
                                _abortGame = false;
                            }
                        }

                        summaries.Add(summary);
                        WriteRows(Path.Combine(outDir, SummaryFileName), BuildSummaryRows(summaries));
                        SaveJob(outDir, job);
                        Debug.Log($"[LevelPreRun] Lv{levelId} {cfgJob.Slot}={cfgJob.Cfg}  win={summary.Wins} fail={summary.Fails}  pass={summary.PassRate:P2}");
                    }
                }
            }
            finally
            {
                LevelDifficultySim.EndBatch();
                EndLogCapture();
            }

            if (ranLevels == 0 && allRuns.Count == 0)
            {
                Status = $"关卡 {levelFrom}-{levelTo} 没有可跑配置";
                Debug.LogError("[LevelPreRun] " + Status);
                IsBusy = false;
                _loop = null;
                yield break;
            }

            WriteCsv(outDir, allRuns, summaries);
            LastSummary = FormatSummary(summaries);
            Debug.Log("[LevelPreRun] " + LastSummary);
            Debug.Log("[LevelPreRun] wrote " + outDir + " newRuns=" + newRuns);

            if (job.mailed)
            {
                LastMailStatus = "此前已发过邮件";
                Status = $"Done  {outDir}  {LastMailStatus}";
            }
            else
            {
                Status = $"Done  {outDir}  正在发送邮件…";
                yield return SendResultMail(outDir, LastSummary, levelFrom, levelTo);
                job.mailed = LastMailStatus != null && LastMailStatus.StartsWith("已发邮件", StringComparison.Ordinal);
            }

            job.completed = true;
            SaveJob(outDir, job);
            Status = $"Done  {outDir}  {LastMailStatus}";
            IsBusy = false;
            _loop = null;
        }

        static IEnumerator SendResultMail(string outDir, string summary, int levelFrom, int levelTo)
        {
            string result = null;
            string configPath = LevelDifficultyMail.ConfigPath;
            var thread = new Thread(() =>
            {
                try
                {
                    result = LevelDifficultyMail.SendLevelResults(
                        outDir, summary, levelFrom, levelTo, configPath);
                }
                catch (Exception e)
                {
                    result = "邮件发送失败：" + e.Message;
                }
            });
            thread.IsBackground = true;
            thread.Start();
            while (thread.IsAlive)
                yield return null;

            LastMailStatus = string.IsNullOrEmpty(result) ? "邮件状态未知" : result;
            if (LastMailStatus.StartsWith("已发邮件", StringComparison.Ordinal))
                Debug.Log("[LevelPreRun] " + LastMailStatus);
            else
                Debug.LogWarning("[LevelPreRun] " + LastMailStatus);
        }

        IEnumerator RunReplay(long seed)
        {
            if (PlayBotController.Instance != null)
                PlayBotController.Instance.StopBot(restoreTimeScale: true);

            if (!TryResolveSeed(seed, out int levelId, out int slotIndex, out string slotName, out string cfg, out int runIndex, out string error))
            {
                Status = error;
                Debug.LogError("[LevelPreRun] " + error);
                yield break;
            }

            IsBusy = true;
            Status = $"还原种子 {seed}  关卡{levelId} {slotName} 第{runIndex + 1}局";
            LevelDifficultySim.EndBatch();
            LevelDifficultySeed.IsReplayLoad = true;
            LevelDifficultySeed.SetPending(seed);

            bool loaded = false;
            GameDirector.LoadLevel(levelId - 1, () => loaded = true, cfg, forceOpen: true);

            float wait = 0f;
            while (!loaded && wait < 20f)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }

            LevelDifficultySeed.IsReplayLoad = false;
            LevelDifficultySeed.PendingInitState = 0;
            IsBusy = false;
            _loop = null;

            if (!loaded || !GameDirector.IsGameActive)
            {
                Status = $"种子 {seed} 还原失败";
                Debug.LogError("[LevelPreRun] " + Status);
                yield break;
            }

            Status = $"已还原 关卡{levelId} {slotName}[{cfg}] 第{runIndex + 1}局 种子{seed}（无 UI/动效）";
            Debug.Log("[LevelPreRun] " + Status);
        }

        IEnumerator RunOneGame(
            int levelId,
            string dynamicStr,
            long seed,
            CfgSummary summary,
            List<RunRecord> allRuns,
            int runIndex)
        {
            _gameInProgress = true;
            _gameStartRealtime = Time.realtimeSinceStartup;
            LevelDifficultySim.BeginGame();
            _planner.Reset();
            LevelDifficultySeed.SetPending(seed);

            bool loaded = false;
            try
            {
                GameDirector.LoadLevel(levelId - 1, () => loaded = true, dynamicStr, forceOpen: true);
            }
            catch (Exception e)
            {
                _stuckReason = "进关异常\n" + e;
                Debug.LogException(e);
            }

            if (!string.IsNullOrEmpty(_stuckReason) && !loaded)
            {
                Record(summary, allRuns, runIndex, seed, false, 0, 0, new int[6]);
                _gameInProgress = false;
                yield break;
            }

            float wait = 0f;
            while (!loaded && wait < 15f)
            {
                if (IsGameStuck("进关等待超时"))
                    break;
                wait += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!string.IsNullOrEmpty(_stuckReason))
            {
                Record(summary, allRuns, runIndex, seed, false, 0, 0, new int[6]);
                _gameInProgress = false;
                yield break;
            }

            if (!loaded || !GameDirector.IsGameActive)
            {
                Record(summary, allRuns, runIndex, seed, false, 0, 0, new int[6]);
                _gameInProgress = false;
                yield break;
            }

            int initial = LevelDirector.LevelGenService != null
                ? LevelDirector.LevelGenService.InitialTileCount
                : 0;
            var counts = new int[6];
            int stepsThisFrame = 0;
            int safety = Mathf.Max(64, initial * 8);

            while (!LevelDifficultySim.HasFinished && safety-- > 0)
            {
                if (IsGameStuck("单局超时无进展"))
                    break;
                if (!GameDirector.IsGameActive)
                    break;

                DockActor dock = LevelDirector.Dock;
                if (dock != null && dock.IsFilled && !dock.GetHasThreeSameTypeOccupiedSlot())
                {
                    LevelDifficultySim.NotifyFinished(false);
                    break;
                }

                TileActor tile;
                try
                {
                    tile = _planner.PickNext();
                }
                catch (Exception e)
                {
                    _stuckReason = "选子异常\n" + e;
                    Debug.LogException(e);
                    break;
                }

                if (tile == null)
                {
                    LevelDifficultySim.NotifyFinished(false);
                    break;
                }

                if (_planner.LastWasNewPlan)
                {
                    int cat = _planner.LastCategory;
                    if (cat >= 1 && cat <= 5)
                        counts[cat]++;
                    else
                        counts[0]++;
                }

                try
                {
                    PlayBotPlanner.SimClick(tile);
                }
                catch (Exception e)
                {
                    _stuckReason = "落子异常\n" + e;
                    Debug.LogException(e);
                    break;
                }

                if (++stepsThisFrame >= StepsPerFrame)
                {
                    stepsThisFrame = 0;
                    yield return null;
                }
            }

            if (!LevelDifficultySim.HasFinished)
                LevelDifficultySim.NotifyFinished(false);

            bool won = string.IsNullOrEmpty(_stuckReason) && LevelDifficultySim.LastWon;
            int remaining = RemainingTiles();
            Record(summary, allRuns, runIndex, seed, won, remaining, initial, counts);
            _gameInProgress = false;
            yield return null;
        }

        bool IsGameStuck(string reason)
        {
            if (_abortGame)
            {
                if (string.IsNullOrEmpty(_stuckReason))
                    _stuckReason = reason;
                return true;
            }

            if (_gameInProgress && Time.realtimeSinceStartup - _gameStartRealtime >= StuckSeconds)
            {
                _abortGame = true;
                _stuckReason = $"{reason}（超过 {StuckSeconds:0} 秒）";
                return true;
            }

            return false;
        }

        IEnumerator NotifyStuck(
            string outDir,
            int levelFrom,
            int levelTo,
            int levelId,
            string slot,
            long seed,
            int runNo,
            int runs)
        {
            string reason = _stuckReason ?? "未知卡住";
            Status = $"卡住  Lv{levelId} {slot} 第{runNo}/{runs}局  种子{seed}";
            Debug.LogError("[LevelPreRun] " + Status + "\n" + reason);
            WriteErrorLog(outDir, reason, Status);
            LastSummary = File.Exists(Path.Combine(outDir, SummaryFileName))
                ? LastSummary
                : Status;

            string summaryText = Status + "\n" + reason + "\n\n" + (LastSummary ?? "");
            Status = $"卡住，正在发送邮件…  {outDir}";
            yield return SendStuckMail(outDir, summaryText, levelFrom, levelTo, reason);
            Status = $"卡住已通知  Lv{levelId} {slot} 种子{seed}  {LastMailStatus}";
        }

        static IEnumerator SendStuckMail(
            string outDir, string summary, int levelFrom, int levelTo, string reason)
        {
            string result = null;
            string configPath = LevelDifficultyMail.ConfigPath;
            var thread = new Thread(() =>
            {
                try
                {
                    result = LevelDifficultyMail.SendStuckReport(
                        outDir, summary, levelFrom, levelTo, reason, configPath);
                }
                catch (Exception e)
                {
                    result = "邮件发送失败：" + e.Message;
                }
            });
            thread.IsBackground = true;
            thread.Start();
            while (thread.IsAlive)
                yield return null;

            LastMailStatus = string.IsNullOrEmpty(result) ? "邮件状态未知" : result;
            if (LastMailStatus.StartsWith("已发邮件", StringComparison.Ordinal))
                Debug.Log("[LevelPreRun] stuck mail " + LastMailStatus);
            else
                Debug.LogWarning("[LevelPreRun] stuck mail " + LastMailStatus);
        }

        void BeginLogCapture()
        {
            lock (_logGate)
                _capturedLogs.Clear();
            Application.logMessageReceivedThreaded -= OnCapturedLog;
            Application.logMessageReceivedThreaded += OnCapturedLog;
        }

        void EndLogCapture()
        {
            Application.logMessageReceivedThreaded -= OnCapturedLog;
        }

        void OnCapturedLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
                return;

            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{type}] {condition}";
            if (!string.IsNullOrEmpty(stackTrace))
                line += "\n" + stackTrace.TrimEnd();

            lock (_logGate)
            {
                _capturedLogs.Add(line);
                if (_capturedLogs.Count > LogCap)
                    _capturedLogs.RemoveAt(0);
            }
        }

        void WriteErrorLog(string outDir, string reason, string status)
        {
            Directory.CreateDirectory(outDir);
            var sb = new StringBuilder();
            sb.AppendLine("==== 关卡预跑卡住 " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ====");
            sb.AppendLine("状态：" + status);
            sb.AppendLine("原因：" + reason);
            sb.AppendLine("输出：" + outDir);
            sb.AppendLine();
            sb.AppendLine("---- Unity 报错 ----");
            lock (_logGate)
            {
                if (_capturedLogs.Count == 0)
                    sb.AppendLine("（本批尚未捕获到 Error/Exception）");
                else
                {
                    for (int i = 0; i < _capturedLogs.Count; i++)
                    {
                        sb.AppendLine(_capturedLogs[i]);
                        sb.AppendLine();
                    }
                }
            }

            File.AppendAllText(Path.Combine(outDir, ErrorLogName), sb.ToString(), new UTF8Encoding(true));
        }

        static int RemainingTiles()
        {
            LevelGenService gen = LevelDirector.LevelGenService;
            if (gen == null)
                return 0;
            return gen.GetRestTileCount();
        }

        static void Record(
            CfgSummary summary,
            List<RunRecord> allRuns,
            int runIndex,
            long seed,
            bool won,
            int remaining,
            int initial,
            int[] counts)
        {
            var record = new RunRecord
            {
                LevelId = summary.LevelId,
                Slot = summary.Slot,
                Cfg = summary.Cfg,
                RunIndex = runIndex + 1,
                Seed = seed,
                Won = won,
                Remaining = remaining,
                Initial = initial,
                P1 = counts[1],
                P2 = counts[2],
                P3 = counts[3],
                P4 = counts[4],
                P5 = counts[5],
                R = counts[0]
            };
            ApplyStats(summary, record);
            allRuns.Add(record);
        }

        static void ApplyStats(CfgSummary summary, RunRecord r)
        {
            if (r.Won)
                summary.Wins++;
            else
            {
                summary.Fails++;
                summary.FailRemainingSum += r.Remaining;
                summary.FailSeeds.Add(r.Seed);
                if (r.Initial > 0)
                    summary.FailProgresses.Add(1f - (float)r.Remaining / r.Initial);
            }

            summary.ChoiceCounts[1] += r.P1;
            summary.ChoiceCounts[2] += r.P2;
            summary.ChoiceCounts[3] += r.P3;
            summary.ChoiceCounts[4] += r.P4;
            summary.ChoiceCounts[5] += r.P5;
            summary.ChoiceCounts[0] += r.R;
            if (r.Initial > 0)
                summary.InitialTiles = r.Initial;
        }

        static void FillSummaryFromRuns(CfgSummary summary, List<RunRecord> runs)
        {
            for (int i = 0; i < runs.Count; i++)
            {
                RunRecord r = runs[i];
                if (r.LevelId == summary.LevelId && r.Slot == summary.Slot)
                    ApplyStats(summary, r);
            }
        }

        static List<CfgJob> CollectJobs(GameLevelConfig cfg)
        {
            var jobs = new List<CfgJob>();
            AddJob(jobs, "defaultCfg", cfg.DefaultCfg, 0);
            AddJob(jobs, "win1", cfg.Win1, 1);
            AddJob(jobs, "win2", cfg.Win2, 2);
            AddJob(jobs, "win3", cfg.Win3, 3);
            AddJob(jobs, "win4", cfg.Win4, 4);
            return jobs;
        }

        static void AddJob(List<CfgJob> jobs, string slot, string value, int slotIndex)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;
            jobs.Add(new CfgJob { Slot = slot, Cfg = value.Trim(), SlotIndex = slotIndex });
        }

        public static bool TryResolveSeed(long seed, out int levelId, out int slotIndex, out string slotName, out string cfg, out int runIndex, out string error)
        {
            levelId = 0;
            slotIndex = 0;
            slotName = "";
            cfg = "";
            runIndex = 0;
            error = "";

            if (!LevelDifficultySeed.TryDecode(seed, out levelId, out slotIndex, out runIndex))
            {
                error = "种子格式不对，请填预跑 CSV 里的种子整数";
                return false;
            }

            slotName = LevelDifficultySeed.SlotName(slotIndex);
            GameLevelConfig levelCfg = LevelDynamicConfigProvider.GetLevelConfig(levelId);
            if (levelCfg == null)
            {
                error = $"关卡 {levelId} 没有 CSVGameLevelCfg";
                return false;
            }

            cfg = CfgOfSlot(levelCfg, slotIndex);
            if (string.IsNullOrWhiteSpace(cfg))
            {
                error = $"关卡 {levelId} 的 {slotName} 为空，无法还原";
                return false;
            }

            cfg = cfg.Trim();
            return true;
        }

        static string CfgOfSlot(GameLevelConfig cfg, int slotIndex)
        {
            switch (slotIndex)
            {
                case 0: return cfg.DefaultCfg;
                case 1: return cfg.Win1;
                case 2: return cfg.Win2;
                case 3: return cfg.Win3;
                case 4: return cfg.Win4;
                default: return "";
            }
        }

        static void WriteCsv(string outDir, List<RunRecord> runs, List<CfgSummary> summaries)
        {
            WriteRows(Path.Combine(outDir, RunsFileName), BuildRunRows(runs));
            WriteRows(Path.Combine(outDir, SummaryFileName), BuildSummaryRows(summaries));
        }

        static string FormatRunRow(RunRecord r)
        {
            float ratio = r.Initial > 0 ? (float)r.Remaining / r.Initial : 0f;
            return string.Join(",",
                r.LevelId,
                r.Slot,
                Quote(r.Cfg),
                r.RunIndex,
                r.Seed,
                r.Won ? "胜利" : "失败",
                r.Remaining,
                r.Initial,
                ratio.ToString("0.####"),
                r.P1, r.P2, r.P3, r.P4, r.P5, r.R);
        }

        static List<string> BuildRunRows(List<RunRecord> runs)
        {
            var rows = new List<string> { RunsHeader };
            for (int i = 0; i < runs.Count; i++)
                rows.Add(FormatRunRow(runs[i]));
            return rows;
        }

        static List<string> BuildSummaryRows(List<CfgSummary> summaries)
        {
            var rows = new List<string>
            {
                "关卡ID,参数列,配置串,局数,胜利次数,失败次数,通过率,P1占比,P2占比,P3占比,P4占比,P5占比,R占比,失败平均剩余棋子,失败平均剩余比例,失败种子,deadlockRate,deadlockProgressAvg,deadlockProgressMedian,dangerFactor"
            };
            for (int i = 0; i < summaries.Count; i++)
            {
                CfgSummary s = summaries[i];
                int total = s.Wins + s.Fails;
                int choiceTotal = 0;
                for (int c = 0; c < 6; c++)
                    choiceTotal += s.ChoiceCounts[c];

                float avgRemain = s.Fails > 0 ? (float)s.FailRemainingSum / s.Fails : 0f;
                float avgRatio = s.InitialTiles > 0 && s.Fails > 0
                    ? avgRemain / s.InitialTiles
                    : 0f;
                rows.Add(string.Join(",",
                    s.LevelId,
                    s.Slot,
                    Quote(s.Cfg),
                    total,
                    s.Wins,
                    s.Fails,
                    s.PassRate.ToString("0.####"),
                    Pct(s.ChoiceCounts[1], choiceTotal),
                    Pct(s.ChoiceCounts[2], choiceTotal),
                    Pct(s.ChoiceCounts[3], choiceTotal),
                    Pct(s.ChoiceCounts[4], choiceTotal),
                    Pct(s.ChoiceCounts[5], choiceTotal),
                    Pct(s.ChoiceCounts[0], choiceTotal),
                    avgRemain.ToString("0.##"),
                    avgRatio.ToString("0.####"),
                    Quote(string.Join("|", s.FailSeeds)),
                    s.DeadlockRate.ToString("0.####"),
                    s.DeadlockProgressAvg.ToString("0.####"),
                    s.DeadlockProgressMedian.ToString("0.####"),
                    s.DangerFactor.ToString("0.####")));
            }

            return rows;
        }

        static string FormatSummary(List<CfgSummary> summaries)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < summaries.Count; i++)
            {
                CfgSummary s = summaries[i];
                int choiceTotal = 0;
                for (int c = 0; c < 6; c++)
                    choiceTotal += s.ChoiceCounts[c];
                float avgRemain = s.Fails > 0 ? (float)s.FailRemainingSum / s.Fails : 0f;
                float avgRatio = s.InitialTiles > 0 && s.Fails > 0
                    ? avgRemain / s.InitialTiles
                    : 0f;
                sb.AppendLine(
                    $"{s.LevelId} {s.Slot} [{s.Cfg}]  {s.Wins}胜/{s.Fails}负  通过率{s.PassRate:P2}  " +
                    $"P1-5 {Pct(s.ChoiceCounts[1], choiceTotal)}/{Pct(s.ChoiceCounts[2], choiceTotal)}/" +
                    $"{Pct(s.ChoiceCounts[3], choiceTotal)}/{Pct(s.ChoiceCounts[4], choiceTotal)}/" +
                    $"{Pct(s.ChoiceCounts[5], choiceTotal)}  R{Pct(s.ChoiceCounts[0], choiceTotal)}  " +
                    $"失败剩余{avgRatio:P2}  " +
                    $"deadlockRate {s.DeadlockRate:0.####}  " +
                    $"progressAvg {s.DeadlockProgressAvg:0.####}  " +
                    $"progressMedian {s.DeadlockProgressMedian:0.####}  " +
                    $"danger {s.DangerFactor:0.####}");
            }

            return sb.ToString();
        }

        static string Pct(int count, int total)
        {
            if (total <= 0)
                return "0";
            return ((float)count / total).ToString("0.####");
        }

        static float Median(List<float> values)
        {
            if (values == null || values.Count == 0)
                return 0f;
            var sorted = new List<float>(values);
            sorted.Sort();
            int mid = sorted.Count / 2;
            if ((sorted.Count & 1) == 1)
                return sorted[mid];
            return 0.5f * (sorted[mid - 1] + sorted[mid]);
        }

        static string Quote(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "\"\"";
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        static void WriteRows(string path, List<string> rows)
        {
            var sb = new StringBuilder();
            sb.Append('\uFEFF');
            for (int i = 0; i < rows.Count; i++)
            {
                sb.Append(rows[i]);
                sb.Append("\r\n");
            }

            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        }

        const string RunsHeader = "关卡ID,参数列,配置串,局号,种子,结果,剩余棋子,总棋子,剩余比例,P1,P2,P3,P4,P5,R";

        public struct ResumeInfo
        {
            public string Dir;
            public int LevelFrom;
            public int LevelTo;
            public int Runs;
            public int Done;
            public int Expected;
            public bool Completed;
        }

        static string RootDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "LevelPreRun"));

        public static bool TryGetResume(out ResumeInfo info)
        {
            info = default;
            string pref = EditorPrefs.GetString(LastJobDirPref, "");
            if (TryReadResume(pref, out info) && !info.Completed)
                return true;

            if (!Directory.Exists(RootDir))
                return false;

            ResumeInfo best = default;
            DateTime bestTime = DateTime.MinValue;
            string[] dirs = Directory.GetDirectories(RootDir);
            for (int i = 0; i < dirs.Length; i++)
            {
                if (!TryReadResume(dirs[i], out ResumeInfo cur) || cur.Completed)
                    continue;
                DateTime write = Directory.GetLastWriteTimeUtc(dirs[i]);
                if (write <= bestTime)
                    continue;
                best = cur;
                bestTime = write;
            }

            if (bestTime == DateTime.MinValue)
                return false;

            info = best;
            return true;
        }

        public static bool TryReadResume(string dir, out ResumeInfo info)
        {
            info = default;
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                return false;

            JobState job = LoadJob(dir);
            if (job == null)
                return false;

            List<RunRecord> runs = LoadRuns(dir);
            info = new ResumeInfo
            {
                Dir = Path.GetFullPath(dir),
                LevelFrom = job.levelFrom,
                LevelTo = job.levelTo,
                Runs = Mathf.Max(1, job.runs),
                Done = runs.Count,
                Expected = job.expectedRuns,
                Completed = job.completed
            };
            return true;
        }

        static int CountExpected(int levelFrom, int levelTo, int runs)
        {
            if (levelTo < levelFrom)
            {
                int swap = levelFrom;
                levelFrom = levelTo;
                levelTo = swap;
            }

            int total = 0;
            for (int levelId = levelFrom; levelId <= levelTo; levelId++)
            {
                GameLevelConfig cfg = LevelDynamicConfigProvider.GetLevelConfig(levelId);
                if (cfg == null)
                    continue;
                total += CollectJobs(cfg).Count * Mathf.Max(1, runs);
            }

            return total;
        }

        static void EnsureRunsHeader(string outDir)
        {
            string path = Path.Combine(outDir, RunsFileName);
            if (File.Exists(path) && new FileInfo(path).Length > 0)
                return;
            WriteRows(path, new List<string> { RunsHeader });
        }

        static void AppendRunLine(string outDir, RunRecord record)
        {
            EnsureRunsHeader(outDir);
            File.AppendAllText(
                Path.Combine(outDir, RunsFileName),
                FormatRunRow(record) + "\r\n",
                new UTF8Encoding(false));
        }

        static void SaveJob(string outDir, JobState job)
        {
            if (job == null || string.IsNullOrEmpty(outDir))
                return;
            Directory.CreateDirectory(outDir);
            File.WriteAllText(Path.Combine(outDir, JobFileName), JsonUtility.ToJson(job, true));
        }

        static JobState LoadJob(string outDir)
        {
            string path = Path.Combine(outDir, JobFileName);
            if (!File.Exists(path))
                return null;
            try
            {
                return JsonUtility.FromJson<JobState>(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[LevelPreRun] 读 job.json 失败: " + e.Message);
                return null;
            }
        }

        static void FlushJobFromDisk(string outDir, bool completed)
        {
            JobState job = LoadJob(outDir);
            if (job == null)
                return;
            List<RunRecord> runs = LoadRuns(outDir);
            var summaries = RebuildSummaries(runs);
            if (summaries.Count > 0)
                WriteRows(Path.Combine(outDir, SummaryFileName), BuildSummaryRows(summaries));
            job.completed = completed;
            SaveJob(outDir, job);
        }

        static List<CfgSummary> RebuildSummaries(List<RunRecord> runs)
        {
            var summaries = new List<CfgSummary>();
            var index = new Dictionary<string, CfgSummary>();
            for (int i = 0; i < runs.Count; i++)
            {
                RunRecord r = runs[i];
                string key = r.LevelId + "\t" + r.Slot;
                if (!index.TryGetValue(key, out CfgSummary summary))
                {
                    summary = new CfgSummary
                    {
                        LevelId = r.LevelId,
                        Slot = r.Slot,
                        Cfg = r.Cfg
                    };
                    index[key] = summary;
                    summaries.Add(summary);
                }

                ApplyStats(summary, r);
            }

            return summaries;
        }

        static List<RunRecord> LoadRuns(string outDir)
        {
            var runs = new List<RunRecord>();
            string path = Path.Combine(outDir, RunsFileName);
            if (!File.Exists(path))
                return runs;

            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length > 0 && line[0] == '\uFEFF')
                    line = line.Substring(1).Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("关卡ID", StringComparison.Ordinal))
                    continue;
                if (!TryParseRunRow(line, out RunRecord record))
                    continue;
                runs.Add(record);
            }

            return runs;
        }

        static bool TryParseRunRow(string line, out RunRecord record)
        {
            record = default;
            List<string> fields = SplitCsvLine(line);
            if (fields.Count < 15)
                return false;
            if (!int.TryParse(fields[0], out int levelId) ||
                !int.TryParse(fields[3], out int runIndex) ||
                !long.TryParse(fields[4], out long seed) ||
                !int.TryParse(fields[6], out int remaining) ||
                !int.TryParse(fields[7], out int initial) ||
                !int.TryParse(fields[9], out int p1) ||
                !int.TryParse(fields[10], out int p2) ||
                !int.TryParse(fields[11], out int p3) ||
                !int.TryParse(fields[12], out int p4) ||
                !int.TryParse(fields[13], out int p5) ||
                !int.TryParse(fields[14], out int r))
                return false;

            record = new RunRecord
            {
                LevelId = levelId,
                Slot = fields[1],
                Cfg = fields[2],
                RunIndex = runIndex,
                Seed = seed,
                Won = fields[5] == "胜利",
                Remaining = remaining,
                Initial = initial,
                P1 = p1,
                P2 = p2,
                P3 = p3,
                P4 = p4,
                P5 = p5,
                R = r
            };
            return true;
        }

        static List<string> SplitCsvLine(string line)
        {
            var fields = new List<string>();
            var sb = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (quoted)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            sb.Append('"');
                            i++;
                        }
                        else
                            quoted = false;
                    }
                    else
                        sb.Append(c);
                    continue;
                }

                if (c == '"')
                    quoted = true;
                else if (c == ',')
                {
                    fields.Add(sb.ToString());
                    sb.Length = 0;
                }
                else
                    sb.Append(c);
            }

            fields.Add(sb.ToString());
            return fields;
        }

        [Serializable]
        class JobState
        {
            public int levelFrom;
            public int levelTo;
            public int runs;
            public int expectedRuns;
            public bool completed;
            public bool mailed;
        }

        struct CfgJob
        {
            public string Slot;
            public string Cfg;
            public int SlotIndex;
        }

        sealed class CfgSummary
        {
            public int LevelId;
            public string Slot;
            public string Cfg;
            public int Wins;
            public int Fails;
            public int FailRemainingSum;
            public int InitialTiles;
            public readonly int[] ChoiceCounts = new int[6];
            public readonly List<long> FailSeeds = new List<long>();
            public readonly List<float> FailProgresses = new List<float>();
            public float PassRate => Wins + Fails <= 0 ? 0f : (float)Wins / (Wins + Fails);
            public float DeadlockRate => Wins + Fails <= 0 ? 0f : (float)Fails / (Wins + Fails);
            public float DeadlockProgressAvg
            {
                get
                {
                    if (FailProgresses.Count == 0)
                        return 0f;
                    float sum = 0f;
                    for (int i = 0; i < FailProgresses.Count; i++)
                        sum += FailProgresses[i];
                    return sum / FailProgresses.Count;
                }
            }
            public float DeadlockProgressMedian => Median(FailProgresses);
            public float DangerFactor
            {
                get
                {
                    float rate = DeadlockRate;
                    return rate + DeadlockProgressAvg * (1f - rate);
                }
            }
        }

        struct RunRecord
        {
            public int LevelId;
            public string Slot;
            public string Cfg;
            public int RunIndex;
            public long Seed;
            public bool Won;
            public int Remaining;
            public int Initial;
            public int P1, P2, P3, P4, P5, R;
        }
    }
}
#endif
