using FishFramework;
using FishXSortPlay;
using XSortGameCore;
using UnityEngine;

namespace RopeSort
{
    public static class RopeGameController
    {
        public static int DisplayedLevelIndex { get; private set; }

        public static void JumpTo(int index)
        {
            index = Mathf.Clamp(index, 0, RopeLevelCatalog.Last);
            if (GPUserDataManager.HasInit())
            {
                GPUserDataManager.Instance.SetLevelNum(index);
                GPUserDataManager.Instance.SetInfinitLevelNum(0);
                GPUserDataManager.Instance.SaveData();
                PlayerPrefs.Save();
            }

            UIController.DisablePage<UIComplete>();
            UIController.DisablePage<UIGameOver>();
            UIController.DisablePage<UISettings>();
            GameDirector.LoadLevel(index);
        }

        public static void Enter(int index)
        {
            DisplayedLevelIndex = Mathf.Clamp(index, 0, RopeLevelCatalog.Last);
            if (GameData.BFromStartScene && GPUserDataManager.HasInit())
                DisplayedLevelIndex = Mathf.Min(GPUserDataManager.Instance.GetLevelNum(), RopeLevelCatalog.Last);

            RopeGamePage page = UIController.GetPage<RopeGamePage>();
            if (page == null)
            {
                Debug.LogError("[RopeSort] UI Rope Game 不在主画布上。");
                return;
            }

            page.Prepare(DisplayedLevelIndex);
            UIController.ShowPage<RopeGamePage>();
        }

        public static void NotifyWin()
        {
            if (GameData.BFromStartScene && GPUserDataManager.HasInit())
            {
                if (GPUserDataManager.Instance.GetLevelNum() < RopeLevelCatalog.Last)
                    GPUserDataManager.Instance.InceaseLevelNum();
                GPUserDataManager.Instance.IncrementWinCount();
                GPUserDataManager.Instance.SaveData();
            }
            else if (DisplayedLevelIndex < RopeLevelCatalog.Last)
            {
                DisplayedLevelIndex++;
            }

            RopeGamePage page = UIController.GetPage<RopeGamePage>();
            if (page != null)
                page.ReleaseForSettlement();

            UIController.HidePage<RopeGamePage>(() =>
            {
                AdsPlayStrategy.TryPlayOnWin();
                Utility.DGDelayedCall(1f / 33f, () =>
                {
                    UIController.ShowSettlementPage<UIComplete>();
                });
            });
        }

        public static void NotifyFail()
        {
            UIController.HidePage<RopeGamePage>(() => UIController.ShowPage<UIGameOver>());
        }
    }
}
