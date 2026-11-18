using UnityEngine;

namespace FishTilePlay
{
    /// <summary>
    /// Result02Panel 上 Animator 帧事件中继：Unity 事件只打到 Animator 同物体。
    /// </summary>
    public class UICompleteResult02AnimEvents : MonoBehaviour
    {
        public void PlayResultTitleIn()
        {
            var page = GetComponentInParent<UIComplete>(true);
            if (page == null || !page.IsPageDisplayed)
            {
                return;
            }

            page.PlayResultTitleIn();
        }

        public void PlayProgressFill()
        {
            var page = GetComponentInParent<UIComplete>(true);
            if (page == null || !page.IsPageDisplayed)
            {
                return;
            }

            page.PlayProgressFill();
        }
    }
}
