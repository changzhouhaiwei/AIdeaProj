using UnityEngine;

namespace FishFramework
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        private RectTransform rectTrans;
        private Rect LastSafeArea = new Rect(0, 0, 0, 0);

        private void Awake()
        {
            rectTrans = GetComponent<RectTransform>();
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        private void Refresh()
        {
            Rect safeArea = GetSafeArea();

            if (safeArea != LastSafeArea)
                ApplySafeArea(safeArea);
        }

        private Rect GetSafeArea()
        {
            Rect safeArea = Screen.safeArea;
            safeArea.height += safeArea.y;
            safeArea.y = 0f;
            return safeArea;
        }

        private void ApplySafeArea(Rect r)
        {
            LastSafeArea = r;

            // Convert safe area rectangle from absolute pixels to normalised anchor coordinates
            Vector2 anchorMin = r.position;
            Vector2 anchorMax = r.position + r.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;
            rectTrans.anchorMin = anchorMin;
            rectTrans.anchorMax = anchorMax;

            // Debug.LogFormat("New safe area applied to {0}: x={1}, y={2}, w={3}, h={4} on full extents w={5}, h={6}", name, r.x, r.y, r.width,
            //     r.height, Screen.width, Screen.height);
        }
    }
}