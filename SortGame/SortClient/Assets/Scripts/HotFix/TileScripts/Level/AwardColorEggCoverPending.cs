using System;
using UnityEngine;

namespace FishTilePlay
{
    public static class AwardColorEggCoverPending
    {
        const string SaveKey = "AwardColorEggCoverPending";
        const int PhaseNone = 0;
        const int PhaseChoosing = 1;
        const int PhaseGranted = 2;
        const long ClaimWindowSeconds = 6L * 3600L;

        [Serializable]
        sealed class SaveData
        {
            public int levelIndex = -1;
            public int phase;
            public int selected = -1;
            public int item0;
            public int count0;
            public int lucky0;
            public int item1;
            public int count1;
            public int lucky1;
            public int item2;
            public int count2;
            public int lucky2;
            public long openedUnix;
            public int stagePassed;
            public int rewardGroup;
        }

        static SaveData data;
        static bool sessionStagePassed;
        static bool sessionMainStageSent;
        static int mainStageSentLevel = -1;
        static bool sessionWinRecorded;
        static Action homeContinue;

        /// <summary>
        /// 仅当「当前关」开奖时已经成功把进度推走时才跳过；残留标记 / 未真正加关 都不能挡住过关。
        /// </summary>
        public static bool ShouldSkipIncreaseLevelNum
        {
            get
            {
                Load();
                if (data.stagePassed == 0 || data.levelIndex != LevelDirector.LoadedLevelIndex)
                    return false;
                if (!GPUserDataManager.HasInit())
                    return false;
                // 有限：存档已离开本关；无尽：开奖会话里已调过 InceaseLevelNum
                if (GPUserDataManager.Instance.GetBInfinitMode())
                    return sessionStagePassed;
                return GPUserDataManager.Instance.GetLevelNum() != data.levelIndex;
            }
        }

        /// <summary>
        /// 彩蛋条件达成时已经打过 main_stage status=1。清存档不能把这个标记清掉，否则结算还会再打一次。
        /// 进关时 ResetMainStageSent 清掉，避免挡住同一关的下一次胜利。
        /// </summary>
        public static bool ShouldSkipMainStage =>
            mainStageSentLevel >= 0 && mainStageSentLevel == LevelDirector.LoadedLevelIndex;

        public static void ResetMainStageSent()
        {
            mainStageSentLevel = -1;
            sessionMainStageSent = false;
        }

        /// <summary>彩蛋收集完成：过关并上报胜利。抽奖、快速结算、UIComplete 不再打 main_stage 1。</summary>
        public static void CommitPassOnConditionMet(int levelIndex)
        {
            EnsureRolled(levelIndex);
            CommitStagePass(levelIndex);
            if (sessionMainStageSent || !GameData.BFromStartScene)
            {
                Debug.Log($"[彩蛋胜利] 条件达成但跳过上报 level={levelIndex} alreadySent={sessionMainStageSent} fromStart={GameData.BFromStartScene}");
                return;
            }
            GameBAData.ExcuteMainStage(1);
            sessionMainStageSent = true;
            mainStageSentLevel = levelIndex;
            Debug.Log($"[彩蛋胜利] 条件达成，上报 main_stage status=1 level={levelIndex}");
        }

        /// <summary>GM/F3 过关：清掉当前关彩蛋待领，避免会话标记残留挡下一关。</summary>
        public static void MarkCompletedIfCurrentLevel()
        {
            Load();
            if (data.levelIndex >= 0 && data.levelIndex == LevelDirector.LoadedLevelIndex)
                MarkCompleted();
        }

        public static int RewardGroup
        {
            get
            {
                Load();
                return data.rewardGroup;
            }
        }

        public static AwardColorEggCoverReward[] EnsureRolled(int levelIndex)
        {
            Load();
            if (data.levelIndex == levelIndex && data.phase != PhaseNone && data.count0 > 0)
                return ReadRewards();

            AwardColorEggCoverReward[] rolled = AwardColorEggCoverConfig.RollRewards(out int groupId);
            WriteRewards(levelIndex, rolled, PhaseChoosing, -1, groupId);
            return rolled;
        }

        public static string FormatRewardId(int selectedIndex, bool pickAll)
        {
            Load();
            AwardColorEggCoverReward[] rewards = ReadRewards();
            if (pickAll || selectedIndex < 0)
                return FormatAll(rewards);
            if (selectedIndex >= 0 && selectedIndex < rewards.Length)
                return FormatOne(rewards[selectedIndex]);
            return "";
        }

        public static string FormatAllRewardIds()
        {
            Load();
            return FormatAll(ReadRewards());
        }

        public static void CommitPassOnLotteryOpen(int levelIndex)
        {
            Load();
            if (data.levelIndex != levelIndex || data.phase == PhaseNone)
                EnsureRolled(levelIndex);

            if (data.openedUnix <= 0)
                data.openedUnix = NowUnix();

            CommitStagePass(levelIndex);
            Persist();
            Debug.Log($"[彩蛋胜利] 抽奖打开，不上报胜利 level={levelIndex} stagePassed={data.stagePassed} skipMainStage={ShouldSkipMainStage}");
        }

        static void CommitStagePass(int levelIndex)
        {
            Load();
            if (!sessionStagePassed && data.stagePassed == 0 && GPUserDataManager.HasInit())
            {
                // 有限：levelNum==本关；无尽：LoadedLevelIndex≠levelNum，仍要推 infinitLevelNum
                bool shouldAdvance = GPUserDataManager.Instance.GetBInfinitMode()
                    || GPUserDataManager.Instance.GetLevelNum() == levelIndex;
                if (shouldAdvance)
                {
                    GPUserDataManager.Instance.InceaseLevelNum();
                    RecoverLevelData.ClearCurrentLevelData();
                }

                data.stagePassed = 1;
                sessionStagePassed = true;
                Persist();
            }
            else if (data.stagePassed != 0)
            {
                sessionStagePassed = true;
            }
        }

        public static void MarkGranted(int selectedIndex, bool pickAll)
        {
            Load();
            data.selected = selectedIndex;
            data.phase = PhaseGranted;
            Persist();
            Grant(pickAll ? -1 : selectedIndex);
        }

        public static void MarkCompleted()
        {
            ClearSave();
        }

        public static void MarkSessionWinRecorded()
        {
            sessionWinRecorded = true;
        }

        /// <summary>
        /// 主城补领：有待弹返回 true（关闭后调 onContinue）；无需弹返回 false。
        /// </summary>
        public static bool TryShowPendingOnHome(Action onContinue = null)
        {
            Load();
            if (data.phase == PhaseNone || data.levelIndex < 0)
                return false;

            if (IsExpired())
            {
                ClearSave();
                return false;
            }

            if (data.phase == PhaseGranted)
            {
                AdvanceOrRecordWinIfNeeded();
                ClearSave();
                return false;
            }

            homeContinue = onContinue;
            UIAwardColorEggCoverLottery.Show(ReadRewards(), 1, OnHomeClaimed);
            return true;
        }

        static void OnHomeClaimed()
        {
            AdvanceOrRecordWinIfNeeded();
            ClearSave();
            Action cont = homeContinue;
            homeContinue = null;
            cont?.Invoke();
        }

        static void AdvanceOrRecordWinIfNeeded()
        {
            if (!GPUserDataManager.HasInit())
                return;

            if (GPUserDataManager.Instance.GetLevelNum() == data.levelIndex)
            {
                GPUserDataManager.Instance.InceaseLevelNum();
                data.stagePassed = 1;
                sessionStagePassed = true;
                RecoverLevelData.ClearCurrentLevelData();
            }

            if (!sessionWinRecorded)
            {
                GPUserDataManager.Instance.IncrementWinCount();
                GameData.Activity?.RecordDailyVictoryWin();
                sessionWinRecorded = true;
            }
        }

        static bool IsExpired()
        {
            if (data.openedUnix <= 0)
                return false;
            long now = NowUnix();
            return now > 0 && now - data.openedUnix > ClaimWindowSeconds;
        }

        static long NowUnix()
        {
            long server = GameData.GetServerTime();
            if (server > 0)
                return server;
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        static void Grant(int selectedIndex)
        {
            if (!GPUserDataManager.HasInit())
                return;

            AwardColorEggCoverReward[] rewards = ReadRewards();
            if (selectedIndex < 0)
            {
                for (int i = 0; i < rewards.Length; i++)
                    GrantOne(rewards[i]);
                return;
            }

            if (selectedIndex >= 0 && selectedIndex < rewards.Length)
                GrantOne(rewards[selectedIndex]);
        }

        static void GrantOne(AwardColorEggCoverReward reward)
        {
            if (reward.Count <= 0)
                return;
            GPUserDataManager.Instance.AddCommonItemData(reward.ItemId, reward.Count);
        }

        static AwardColorEggCoverReward[] ReadRewards()
        {
            return new[]
            {
                new AwardColorEggCoverReward(data.item0, data.count0, data.lucky0 != 0),
                new AwardColorEggCoverReward(data.item1, data.count1, data.lucky1 != 0),
                new AwardColorEggCoverReward(data.item2, data.count2, data.lucky2 != 0),
            };
        }

        static string FormatOne(AwardColorEggCoverReward reward)
        {
            return reward.ItemId + "_" + reward.Count;
        }

        static string FormatAll(AwardColorEggCoverReward[] rewards)
        {
            if (rewards == null || rewards.Length == 0)
                return "";
            string result = FormatOne(rewards[0]);
            for (int i = 1; i < rewards.Length; i++)
                result += "," + FormatOne(rewards[i]);
            return result;
        }

        static void WriteRewards(
            int levelIndex,
            AwardColorEggCoverReward[] rewards,
            int phase,
            int selected,
            int groupId)
        {
            sessionStagePassed = false;
            sessionMainStageSent = false;
            sessionWinRecorded = false;
            data = new SaveData
            {
                levelIndex = levelIndex,
                phase = phase,
                selected = selected,
                item0 = rewards[0].ItemId,
                count0 = rewards[0].Count,
                lucky0 = rewards[0].Lucky ? 1 : 0,
                item1 = rewards[1].ItemId,
                count1 = rewards[1].Count,
                lucky1 = rewards[1].Lucky ? 1 : 0,
                item2 = rewards[2].ItemId,
                count2 = rewards[2].Count,
                lucky2 = rewards[2].Lucky ? 1 : 0,
                openedUnix = NowUnix(),
                stagePassed = 0,
                rewardGroup = groupId,
            };
            Persist();
        }

        static void Load()
        {
            if (data != null)
                return;
            string json = PlayerPrefs.GetString(SaveKey, "");
            data = string.IsNullOrEmpty(json)
                ? new SaveData()
                : JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
            if (data.stagePassed != 0)
                sessionStagePassed = true;
        }

        static void Persist()
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        }

        static void ClearSave()
        {
            data = new SaveData();
            sessionStagePassed = false;
            sessionMainStageSent = false;
            sessionWinRecorded = false;
            PlayerPrefs.DeleteKey(SaveKey);
        }
    }
}
