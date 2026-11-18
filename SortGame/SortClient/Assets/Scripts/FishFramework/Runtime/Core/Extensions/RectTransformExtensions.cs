using UnityEngine;

namespace FishFramework
{
    public static class RectTransformExtensions
    {
        //重置为全屏自适应UI
        public static void ResetToFullScreen(this RectTransform self)
        {
            self.anchorMin = Vector2.zero;
            self.anchorMax = Vector2.one;
            self.anchoredPosition3D = Vector3.zero;
            self.pivot = new Vector2(0.5f, 0.5f);
            self.offsetMax = Vector2.zero;
            self.offsetMin = Vector2.zero;
            self.sizeDelta = Vector2.zero;
            self.localEulerAngles = Vector3.zero;
            self.localScale = Vector3.one;
        }
    }
}