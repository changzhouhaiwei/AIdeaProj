#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FishTilePlay
{
    /// <summary>
    /// MCP / 编辑器连打 IQ 关：按策划 5.2–5.4 记录每轮 ClickCost，并实机点裂变棋过关。
    /// </summary>
    public sealed class IQChallengePlayAudit : MonoBehaviour
    {
        const string PendingKey = "IQChallengePlayAudit.Pending";
        const string FromKey = "IQChallengePlayAudit.From";
        const string ToKey = "IQChallengePlayAudit.To";
        const string WantPlayKey = "IQChallengePlayAudit.WantPlay";
        const string GameScenePath = "Assets/CABRes/Scenes/GameScene.unity";

        public static IQChallengePlayAudit Instance { get; private set; }
        public static bool IsBusy { get; private set; }
        public static string Status { get; private set; } = "Idle";
        public static string LastReport { get; private set; } = string.Empty;

        readonly PlayBotPlanner _planner = new PlayBotPlanner();
        Coroutine _loop;

        public static bool HasPending
        {
            get { return SessionState.GetBool(PendingKey, false); }
        }

        public static void Bind(GameDirector host)
        {
            if (host == null)
                return;
            if (host.GetComponent<IQChallengePlayAudit>() == null)
                host.gameObject.AddComponent<IQChallengePlayAudit>();
        }

        public static void QueueStart(int levelFrom, int levelTo)
        {
            SessionState.SetBool(PendingKey, true);
            SessionState.SetInt(FromKey, levelFrom);
            SessionState.SetInt(ToKey, levelTo);
            Status = "Queued " + levelFrom + "-" + levelTo;
        }

        public static void QueueAndEnterPlay(int levelFrom, int levelTo)
        {
            QueueStart(levelFrom, levelTo);
            SessionState.SetBool(WantPlayKey, true);
            EditorApplication.delayCall += EnterPlayIfNeeded;
        }

        public static void TryStartPending()
        {
            if (!HasPending)
                return;

            int from = SessionState.GetInt(FromKey, 101);
            int to = SessionState.GetInt(ToKey, 120);
            SessionState.SetBool(PendingKey, false);

            if (Instance == null)
            {
                Status = "No GameDirector audit host";
                return;
            }

            Instance.StartRun(from, to);
        }

        static void EnterPlayIfNeeded()
        {
            if (!SessionState.GetBool(WantPlayKey, false))
                return;
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            SessionState.SetBool(WantPlayKey, false);
            if (EditorSceneManager.GetActiveScene().path != GameScenePath)
                EditorSceneManager.OpenScene(GameScenePath);
            GameDirector.AutoRunLevelInEditor = false;
            EditorApplication.isPlaying = true;
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

        public void StartRun(int levelFrom, int levelTo)
        {
            if (IsBusy)
            {
                Debug.LogWarning("[IQMcpPlay] already running");
                return;
            }

            if (_loop != null)
                StopCoroutine(_loop);
            _loop = StartCoroutine(RunLevels(levelFrom, levelTo));
        }

        public void StopRun()
        {
            if (_loop != null)
            {
                StopCoroutine(_loop);
                _loop = null;
            }

            IsBusy = false;
            LevelDifficultySim.EndBatch();
        }

        IEnumerator RunLevels(int levelFrom, int levelTo)
        {
            IsBusy = true;
            LastReport = string.Empty;
            var report = new StringBuilder();
            int inBand = 0;
            int total = 0;
            int wins = 0;
            int fails = 0;

            LevelDifficultySim.BeginBatch();
            Debug.Log("[IQMcpPlay] start " + levelFrom + "-" + levelTo);

            for (int display = levelFrom; display <= levelTo; display++)
            {
                Status = "Lv " + display;
                yield return PlayOneLevel(display, report, (ib, t, w, f) =>
                {
                    inBand += ib;
                    total += t;
                    wins += w;
                    fails += f;
                });
            }

            LevelDifficultySim.EndBatch();
            LastReport = "inBand=" + inBand + "/" + total +
                         " wins=" + wins + " fails=" + fails + "\n" + report;
            Status = "Done inBand=" + inBand + "/" + total + " wins=" + wins + " fails=" + fails;
            Debug.Log("[IQMcpPlay] " + LastReport);
            IsBusy = false;
            _loop = null;
        }

        delegate void Accrue(int inBand, int total, int wins, int fails);

        IEnumerator PlayOneLevel(int displayLevel, StringBuilder report, Accrue accrue)
        {
            int index = displayLevel - 1;
            _planner.Reset();
            LevelDifficultySim.BeginGame();
            RecoverLevelData.ClearCurrentLevelData();

            bool loaded = false;
            GameDirector.LoadLevel(index, () => { loaded = true; }, "", 0, true);

            float wait = 0f;
            while (!loaded && wait < 20f)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!loaded || !GameDirector.IsGameActive || LevelDirector.LevelGenService == null)
            {
                report.AppendLine("Lv" + displayLevel + " LOAD_FAIL");
                accrue(0, 0, 0, 1);
                yield break;
            }

            if (!IQChallengeManager.HasActiveLevel)
            {
                report.AppendLine("Lv" + displayLevel + " NOT_IQ");
                accrue(0, 0, 0, 1);
                yield break;
            }

            int localInBand = 0;
            int localTotal = 0;
            RecordRound(displayLevel, "init", 0, report, ref localInBand, ref localTotal);

            int lastCleared = IQChallengeManager.ClearedCount;
            int clicks = 0;
            int safety = 480;
            int idle = 0;

            while (safety-- > 0 && IQChallengeManager.ClearedCount < 4 && !LevelDifficultySim.HasFinished)
            {
                if (!GameDirector.IsGameActive)
                    break;

                DockActor dock = LevelDirector.Dock;
                if (dock != null && dock.IsFilled && !dock.GetHasThreeSameTypeOccupiedSlot())
                {
                    TileActor dump = _planner.PickNext();
                    if (dump != null && PlayBotClickCost.CanPlayerClick(dump))
                    {
                        PlayBotPlanner.SimClick(dump);
                        clicks++;
                        yield return null;
                        continue;
                    }

                    LevelDifficultySim.NotifyFinished(false);
                    break;
                }

                int cleared = IQChallengeManager.ClearedCount;
                if (cleared > lastCleared)
                {
                    lastCleared = cleared;
                    if (cleared < 4)
                        RecordRound(displayLevel, "r" + cleared, cleared, report, ref localInBand, ref localTotal);
                }

                if (IQChallengeManager.IsReadyToWin || IQChallengeManager.IsVictorySequenceStarted)
                    break;

                TileActor tile = PickIqTile();
                if (tile == null)
                {
                    idle++;
                    if (idle >= 8)
                    {
                        LevelDifficultySim.NotifyFinished(false);
                        break;
                    }

                    yield return null;
                    continue;
                }

                idle = 0;
                PlayBotPlanner.SimClick(tile);
                clicks++;
                if (clicks % 16 == 0)
                    yield return null;
            }

            if (IQChallengeManager.ClearedCount > lastCleared && IQChallengeManager.ClearedCount < 4)
                RecordRound(
                    displayLevel,
                    "r" + IQChallengeManager.ClearedCount,
                    IQChallengeManager.ClearedCount,
                    report,
                    ref localInBand,
                    ref localTotal);

            bool won = IQChallengeManager.ClearedCount >= 4
                       || IQChallengeManager.IsReadyToWin
                       || (LevelDifficultySim.HasFinished && LevelDifficultySim.LastWon);
            if (won)
            {
                report.AppendLine("Lv" + displayLevel + " WIN clicks=" + clicks);
                accrue(localInBand, localTotal, 1, 0);
            }
            else
            {
                report.AppendLine(
                    "Lv" + displayLevel + " FAIL cleared=" + IQChallengeManager.ClearedCount +
                    " clicks=" + clicks);
                accrue(localInBand, localTotal, 0, 1);
            }

            yield return null;
        }

        static void RecordRound(
            int displayLevel,
            string tag,
            int cleared,
            StringBuilder report,
            ref int inBand,
            ref int total)
        {
            IQChallengeSelector.GetCostBand(cleared, out int minCost, out int maxCost);
            int cost = IQChallengeSelector.LastClickCost;
            bool ok = IQChallengeSelector.LastWasInBand;
            total++;
            if (ok)
                inBand++;

            string line = "Lv" + displayLevel + " " + tag +
                          " cost=" + cost +
                          " extra=" + IQChallengeSelector.LastExtra +
                          " vis=" + IQChallengeSelector.LastVisible +
                          " band=[" + minCost + "," + maxCost + "]" +
                          " in=" + ok +
                          " mixed=" + IQChallengeSelector.LastUsedMixedColorFallback +
                          " " + IQChallengeSelector.LastColorDump;
            report.AppendLine(line);
            Debug.Log("[IQMcpPlay] " + line);
        }

        TileActor PickIqTile()
        {
            List<TileActor> act = LevelDirector.LevelGenService != null
                ? LevelDirector.LevelGenService.ActTiles
                : null;
            if (act == null)
                return null;

            var fission = new List<TileActor>();
            for (int i = 0; i < act.Count; i++)
            {
                TileActor tile = act[i];
                if (tile != null && tile.IsFission && !tile.IsSubmitted && tile.gameObject.activeInHierarchy)
                    fission.Add(tile);
            }

            for (int i = 0; i < fission.Count; i++)
            {
                if (PlayBotClickCost.CanPlayerClick(fission[i]))
                    return fission[i];
            }

            TileActor dockComplete = FindDockComplete(act);
            if (dockComplete != null)
                return dockComplete;

            return _planner.PickNext();
        }

        static TileActor FindDockComplete(List<TileActor> act)
        {
            DockActor dock = LevelDirector.Dock;
            if (dock == null || act == null)
                return null;

            List<TileActor> occupied = dock.GetOccupiedTileIndices();
            if (occupied == null || occupied.Count == 0)
                return null;

            var counts = new Dictionary<int, int>();
            for (int i = 0; i < occupied.Count; i++)
            {
                TileActor tile = occupied[i];
                if (tile == null || tile.TileModel == null)
                    continue;
                int id = tile.MatchTypeId;
                int n;
                counts.TryGetValue(id, out n);
                counts[id] = n + 1;
            }

            TileActor best = null;
            int bestNeed = 3;
            for (int i = 0; i < act.Count; i++)
            {
                TileActor tile = act[i];
                if (tile == null || tile.IsSubmitted || !PlayBotClickCost.CanPlayerClick(tile))
                    continue;

                int n;
                if (!counts.TryGetValue(tile.MatchTypeId, out n) || n <= 0 || n >= 3)
                    continue;

                int need = 3 - n;
                if (need < bestNeed)
                {
                    bestNeed = need;
                    best = tile;
                    if (need == 1)
                        return tile;
                }
            }

            return best;
        }

        [InitializeOnLoad]
        static class Bootstrap
        {
            static Bootstrap()
            {
                EditorApplication.delayCall += EnterPlayIfNeeded;
                EditorApplication.playModeStateChanged += OnPlayMode;
            }

            static void OnPlayMode(PlayModeStateChange change)
            {
                if (change == PlayModeStateChange.EnteredPlayMode && HasPending && Instance != null)
                    TryStartPending();
            }
        }
    }
}
#endif
