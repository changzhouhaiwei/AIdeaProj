using FishFramework;
using TileGameCore;

namespace FishTilePlay
{
    /// <summary>
    /// 裂变棋快速结算页：动效、粒子和横幅都复用可乐箱结算。
    /// </summary>
    public class UIIQChallengeQuickVictory : UIBoxCoverQuickVictory
    {
        public new const string PrefabPath = "Assets/CABRes/Prefabs/IQChallenge/UIIQChallengeQuickVictory.prefab";

        public static new void Show()
        {
            UIIQChallengeQuickVictory existing = UIController.GetPage<UIIQChallengeQuickVictory>();
            bool alreadyDisplayed = existing != null && existing.IsPageDisplayed;
            UIIQChallengeQuickVictory page = ShowFromPrefab<UIIQChallengeQuickVictory>(PrefabPath);
            if (alreadyDisplayed && page != null)
                page.PlayShowAnimation();
        }
    }
}
