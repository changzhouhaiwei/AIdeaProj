using UnityEngine;

namespace FishFramework
{
    public static partial class Utility
    {
        public static Vector3 WorldPointToLocalPoint(Vector3 worldPos, Camera fromeCamera = null)
        {
            Canvas canvas = GameModule.UI.UICanvas;

            if (fromeCamera == null)
            {
                fromeCamera = GameModule.UI.UICamera;
            }

            //转换成屏幕坐标
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(fromeCamera, worldPos);

            //转换成局部坐标
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.GetComponent<RectTransform>(), screenPos,
                GameModule.UI.UICamera, out Vector2 localpoint);

            return localpoint;
        }


        // 判断点是否在圆的方法
        public static bool IsPointInCircle(Vector2 pos, Vector2 centerPos, float radius)
        {
            return Mathf.Pow(pos.x - centerPos.x, 2) + Mathf.Pow(pos.y - centerPos.y, 2) <= Mathf.Pow(radius, 2);
        }
    }
}