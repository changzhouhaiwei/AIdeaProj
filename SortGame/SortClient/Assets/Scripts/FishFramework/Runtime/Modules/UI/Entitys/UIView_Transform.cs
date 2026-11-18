using UnityEngine;

namespace FishFramework
{
    public abstract partial class UIView
    {
        public void SetAnchoredPos(float x, float y)
        {
            rectTransform.anchoredPosition = new Vector2(x, y);
        }

        public RectTransform GetOwnerRectTransform()
        {
            return rectTransform;
        }
    }
}