using System;
using FishFramework;
using I2.Loc;
using Spine.Unity;
using TileGameCore;
using TMPro;
using UnityEngine;

namespace FishTilePlay
{
    public enum SettlementIntroType
    {
        TotalWin = 0,
        Combo = 1,
        WinStreak = 2,
        Beat = 3,
    }

    /// <summary>
    /// 结算开场 Result01_Eff：按优先级互斥显示四面板，约 2s 后隐藏并回调。
    /// lv1–4 不展示、不等待。
    /// </summary>
    public class UICompleteResultIntro : MonoBehaviour
    {
        // 过关后 GetLevelNum 已 +1，前四关为 1..4
        public const int MinShowLevel = 5;
        public const int MinComboRecord = 12;
        private const float IntroDuration = 2f;

        private const string LocBeat = "UI_completed_beat";
        private const string LocTotalWin = "UI_completed_totalwin";
        private const string LocWinStreak = "UI_completed_daywinstreak";
        private const string LocCombo = "UI_completed_combo";

        [SerializeField] GameObject totalwinPanel;
        [SerializeField] GameObject comboPanel;
        [SerializeField] GameObject winstreakPanel;
        [SerializeField] GameObject beatPanel;

        [SerializeField] TextMeshProUGUI totalwinText;
        [SerializeField] TextMeshProUGUI comboText;
        [SerializeField] TextMeshProUGUI winstreakText;
        [SerializeField] TextMeshProUGUI beatText;

        [SerializeField] SkeletonGraphic totalwinSpine;
        [SerializeField] SkeletonGraphic comboSpine;
        [SerializeField] SkeletonGraphic winstreakSpine;
        [SerializeField] SkeletonGraphic beatSpine;

        private int mPlayGeneration;

        public static bool ShouldShow(int levelIndex)
        {
            return levelIndex >= MinShowLevel;
        }

        public static SettlementIntroType Resolve(
            int levelIndex,
            int winStreak,
            int comboPeakSum,
            int historyMaxCombo)
        {
            if (levelIndex > 0 && levelIndex % 50 == 0)
                return SettlementIntroType.TotalWin;

            if (comboPeakSum >= MinComboRecord && comboPeakSum > historyMaxCombo)
                return SettlementIntroType.Combo;

            if (winStreak > 0 && winStreak % 5 == 0)
                return SettlementIntroType.WinStreak;

            return SettlementIntroType.Beat;
        }

        public void Clear()
        {
            mPlayGeneration++;
            HideAll();
        }

        public void Play(Action onFinished)
        {
            int generation = ++mPlayGeneration;

            // 过关后关卡号已 +1，总胜利按展示关卡号（50/100/150…）计算
            int completedLevelNum = GPUserDataManager.Instance.GetShowLevelNum();
            int streak = GPUserDataManager.Instance.UserData.continusWinCnt;
            int comboSum = DockActor.GetComboPeakSum();
            int history = GPUserDataManager.Instance.GetMaxComboCnt();

            // 过关后 GetLevelNum 已 +1：1..4 为前四关结算，不展示 Result01_Eff
            if (!ShouldShow(GPUserDataManager.Instance.GetLevelNum()))
            {
                HideAll();
                onFinished?.Invoke();
                return;
            }

            SettlementIntroType type = Resolve(completedLevelNum, streak, comboSum, history);
            // 历史最高 combo 仅在实际弹出 comboPanel 时刷新
            if (type == SettlementIntroType.Combo)
                GPUserDataManager.Instance.TryUpdateMaxCombo(comboSum);
            gameObject.SetActive(true);
            ShowOnly(type);
            // 必须在面板 OnEnable（I2 Localize）之后再写入，否则 {0} 会被未格式化原文覆盖
            ApplyLocalizedText(type, completedLevelNum, streak, comboSum);

            Utility.DGDelayedCall(IntroDuration, () =>
            {
                if (generation != mPlayGeneration)
                    return;

                HideAll();
                onFinished?.Invoke();
            });
        }

        void ApplyLocalizedText(SettlementIntroType type, int level, int streak, int comboSum)
        {
            switch (type)
            {
                case SettlementIntroType.TotalWin:
                    SetFormattedText(totalwinText, LocTotalWin, level.ToString());
                    SetCenterText(totalwinPanel, level.ToString());
                    break;
                case SettlementIntroType.Combo:
                    SetFormattedText(comboText, LocCombo, comboSum.ToString());
                    SetCenterText(comboPanel, comboSum.ToString());
                    break;
                case SettlementIntroType.WinStreak:
                    SetFormattedText(winstreakText, LocWinStreak, streak.ToString());
                    SetCenterText(winstreakPanel, streak.ToString());
                    break;
                case SettlementIntroType.Beat:
                    string passRate = GetPassRateArg(level);
                    SetFormattedText(beatText, LocBeat, passRate);
                    SetCenterText(beatPanel, GetPassRateDisplay(level));
                    break;
            }
        }

        static void SetCenterText(GameObject panel, string numberStr)
        {
            if (panel == null)
                return;

            var labels = panel.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                var label = labels[i];
                if (label == null || label.gameObject.name != "CenterText")
                    continue;

                var localize = label.GetComponent<Localize>();
                if (localize != null)
                    localize.enabled = false;

                label.text = numberStr;
                return;
            }
        }

        static string GetPassRateDisplay(int displayedLevelIndex)
        {
            // 与 UIComplete.ShowPassRateDesc 一致：配置 id = Loaded/Displayed + 1
            int configId = displayedLevelIndex + 1;
            if (GameDirector.levelController != null)
            {
                configId = GameDirector.levelController.GetLoadedLevelIndex() + 1;
            }

            var cfg = LevelDynamicConfigProvider.GetLevelConfig(configId);
            string rateStr = cfg != null ? cfg.PassRate : string.Empty;
            if (string.IsNullOrEmpty(rateStr))
                return "0%";

            return rateStr.EndsWith("%") ? rateStr : rateStr + "%";
        }

        static string GetPassRateArg(int displayedLevelIndex)
        {
            string rateStr = GetPassRateDisplay(displayedLevelIndex);
            // 文案多为 "{0}%"，配置多为 "55%"，去掉末尾 % 避免重复
            if (rateStr.EndsWith("%"))
                return rateStr.Substring(0, rateStr.Length - 1);

            return rateStr;
        }

        static void SetFormattedText(TextMeshProUGUI label, string locKey, string arg0)
        {
            if (label == null)
                return;

            string format = I2LocalizeMgr.GetString(locKey);
            if (string.IsNullOrEmpty(format))
                format = "{0}";

            label.text = string.Format(format, arg0);
        }

        void ShowOnly(SettlementIntroType type)
        {
            SetPanelActive(totalwinPanel, type == SettlementIntroType.TotalWin);
            SetPanelActive(comboPanel, type == SettlementIntroType.Combo);
            SetPanelActive(winstreakPanel, type == SettlementIntroType.WinStreak);
            SetPanelActive(beatPanel, type == SettlementIntroType.Beat);

            PlayPanelSpineLoop(totalwinPanel, totalwinSpine, type == SettlementIntroType.TotalWin);
            PlayPanelSpineLoop(comboPanel, comboSpine, type == SettlementIntroType.Combo);
            PlayPanelSpineLoop(winstreakPanel, winstreakSpine, type == SettlementIntroType.WinStreak);
            PlayPanelSpineLoop(beatPanel, beatSpine, type == SettlementIntroType.Beat);
        }

        void HideAll()
        {
            StopPanelSpine(totalwinSpine, totalwinPanel);
            StopPanelSpine(comboSpine, comboPanel);
            StopPanelSpine(winstreakSpine, winstreakPanel);
            StopPanelSpine(beatSpine, beatPanel);

            SetPanelActive(totalwinPanel, false);
            SetPanelActive(comboPanel, false);
            SetPanelActive(winstreakPanel, false);
            SetPanelActive(beatPanel, false);
            gameObject.SetActive(false);
        }

        static void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
                panel.SetActive(active);
        }

        static void PlayPanelSpineLoop(GameObject panel, SkeletonGraphic spine, bool active)
        {
            if (!active)
                return;

            spine = ResolvePanelSpine(panel, spine);
            if (spine == null)
                return;

            if (!spine.IsValid)
                spine.Initialize(false);
            if (spine.AnimationState == null)
                return;

            string animName = spine.startingAnimation;
            if (string.IsNullOrEmpty(animName))
                return;

            spine.AnimationState.ClearTrack(0);
            spine.AnimationState.SetAnimation(0, animName, true);
            spine.Update(0f);
        }

        static void StopPanelSpine(SkeletonGraphic spine, GameObject panel)
        {
            spine = ResolvePanelSpine(panel, spine);
            if (spine == null || spine.AnimationState == null)
                return;

            spine.AnimationState.ClearTrack(0);
        }

        static SkeletonGraphic ResolvePanelSpine(GameObject panel, SkeletonGraphic spine)
        {
            if (spine != null)
                return spine;
            if (panel == null)
                return null;
            return panel.GetComponentInChildren<SkeletonGraphic>(true);
        }
    }
}
