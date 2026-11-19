using FishTilePlay;
using TileGameCore;
using UnityEngine;

namespace RopeSort
{
    public static class RopeGameController
    {
        public static int DisplayedLevelIndex { get; private set; }

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
                {
                    GPUserDataManager.Instance.InceaseLevelNum();
                    GPUserDataManager.Instance.SaveData();
                }
            }
            else if (DisplayedLevelIndex < RopeLevelCatalog.Last)
            {
                DisplayedLevelIndex++;
            }

            RopeGamePage page = UIController.GetPage<RopeGamePage>();
            if (page != null)
                page.ShowWinSettlement();
        }

        public static void NotifyFail()
        {
            UIController.HidePage<RopeGamePage>(() => UIController.ShowPage<UIGameOver>());
        }
    }
}
