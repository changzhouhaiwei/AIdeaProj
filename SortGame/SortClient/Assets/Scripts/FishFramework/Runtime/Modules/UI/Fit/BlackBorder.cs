using UnityEngine;
using UnityEngine.UI;

namespace FishFramework
{
    public class BlackBorder : MonoBehaviour
    {
        private void Awake()
        {
            GameObject lGo = new GameObject("lBlackImage")
            {
                layer = LayerMask.NameToLayer("UI")
            };
            RectTransform lRectTransform = lGo.AddComponent<RectTransform>();
            lRectTransform.SetParent(transform);
            lRectTransform.sizeDelta = new Vector2(1024, 2048);
            lRectTransform.anchoredPosition = new Vector2(-lRectTransform.rect.width / 2 - GameModule.UI.DesignScreenWidth_F / 2, 0f);

            Image lImage = lGo.AddComponent<Image>();
            lImage.color = new Color(0, 0, 0, 1);
            lImage.raycastTarget = false;

            GameObject rRo = new GameObject("rBlackImage")
            {
                layer = LayerMask.NameToLayer("UI")
            };
            RectTransform rRectTransform = rRo.AddComponent<RectTransform>();
            rRectTransform.SetParent(transform);
            rRectTransform.sizeDelta = new Vector2(1024, 2048);
            rRectTransform.anchoredPosition = new Vector2(rRectTransform.rect.width / 2 + GameModule.UI.DesignScreenWidth_F / 2, 0f);

            Image rImage = rRo.AddComponent<Image>();
            rImage.color = new Color(0, 0, 0, 1);
            rImage.raycastTarget = false;
        }
    }
}