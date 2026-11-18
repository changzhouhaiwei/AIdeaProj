using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FishFramework
{
    internal class UIBlocker : MonoSingleton<UIBlocker>, IPointerClickHandler, IBeginDragHandler, IDragHandler
    {
        private RectTransform m_RectTrransform;
        private RawImage m_RawImage;
        private Button m_Btn;
        private Action m_OnClickCallback;
        private bool m_IsDragging = false;
        private const float DRAG_THRESHOLD = 10f; // 拖动阈值（像素）

        private void Awake()
        {
            m_RectTrransform = gameObject.AddComponent<RectTransform>();
            m_RawImage = gameObject.AddComponent<RawImage>();
            m_Btn = gameObject.AddComponent<Button>();

            m_RawImage.color = new Color(0, 0, 0, 0);
            m_Btn.transition = Selectable.Transition.None;
            // 禁用 Button 的 onClick，改用 IPointerClickHandler
            m_Btn.onClick.RemoveAllListeners();

            gameObject.SetActive(false);
        }

        internal void Bind(RectTransform parent, Texture texture, Color color, bool passThrough, Action onClick = null)
        {
            if (transform.parent == parent)
            {
                return;
            }

            gameObject.SetActive(true);

            m_RectTrransform.SetParent(parent);
            m_RectTrransform.SetAsFirstSibling();

            m_RectTrransform.localPosition = Vector3.zero;
            m_RectTrransform.localRotation = Quaternion.Euler(Vector3.zero);
            m_RectTrransform.localScale = Vector3.one;

            m_RectTrransform.anchorMin = Vector2.zero;
            m_RectTrransform.anchorMax = Vector2.one;
            m_RectTrransform.sizeDelta = Vector2.zero;

            m_RawImage.texture = texture;
            m_RawImage.color = color;
            m_RawImage.raycastTarget = !passThrough;

            // 保存回调，在 OnPointerClick 中调用
            m_OnClickCallback = (!passThrough && onClick != null) ? onClick : null;
            m_IsDragging = false;
        }

        internal void Unbind()
        {
            if (transform.parent == GameModule.UI.UILayerRoot)
            {
                return;
            }

            m_OnClickCallback = null;
            m_IsDragging = false;
            m_RectTrransform.SetParent(GameModule.UI.UILayerRoot);
            gameObject.SetActive(false);
        }

        // 检测拖动开始
        public void OnBeginDrag(PointerEventData eventData)
        {
            // 检查移动距离是否超过阈值
            if (eventData.delta.magnitude > DRAG_THRESHOLD)
            {
                m_IsDragging = true;
            }
        }

        // 检测拖动中
        public void OnDrag(PointerEventData eventData)
        {
            // 如果移动距离超过阈值，标记为拖动
            if (!m_IsDragging && eventData.delta.magnitude > DRAG_THRESHOLD)
            {
                m_IsDragging = true;
            }
        }

        // 实现 IPointerClickHandler，确保只在点击完成（按下并抬起）时触发
        public void OnPointerClick(PointerEventData eventData)
        {
            // 检查是否是拖动操作：通过比较按下位置和抬起位置的距离
            float dragDistance = Vector2.Distance(eventData.pressPosition, eventData.position);
            
            // 如果拖动距离超过阈值，或者是拖动操作，则不触发点击
            if (m_IsDragging || dragDistance > DRAG_THRESHOLD)
            {
                m_IsDragging = false; // 重置拖动标志
                return;
            }

            if (m_OnClickCallback != null && m_RawImage.raycastTarget)
            {
                m_OnClickCallback();
            }
            
            m_IsDragging = false; // 重置拖动标志
        }
    }
}