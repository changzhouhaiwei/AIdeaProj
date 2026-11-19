using UnityEngine;
using UnityEngine.UI;

namespace XSortGameCore
{
    [RequireComponent(typeof(Canvas)), RequireComponent(typeof(GraphicRaycaster))]
    public abstract class UIPage : MonoBehaviour
    {
        protected bool isPageDisplayed;
        public bool IsPageDisplayed { get => isPageDisplayed; set => isPageDisplayed = value; }

        protected Canvas canvas;
        public Canvas Canvas => canvas;

        protected GraphicRaycaster graphicRaycaster;
        public GraphicRaycaster GraphicRaycaster => graphicRaycaster;

        private string defaultName;

        public void CacheComponents()
        {
            defaultName = name;

            canvas = GetComponent<Canvas>();
            graphicRaycaster = GetComponent<GraphicRaycaster>();
        }

        public abstract void Init();

        public void EnableCanvas()
        {
            isPageDisplayed = true;

            canvas.enabled = true;

#if UNITY_EDITOR
            name = string.Format("{0} (Active)", defaultName);
#endif
        }

        public void DisableCanvas()
        {
            isPageDisplayed = false;

            canvas.enabled = false;

#if UNITY_EDITOR
            name = defaultName;
#endif
        }

        public abstract void PlayShowAnimation();
        public abstract void PlayHideAnimation();

        public virtual void Unload()
        {
            isPageDisplayed = false;

            canvas.enabled = false;
        }

        /// <summary>
        /// Esc按键按下时的回调
        /// 子类可以重写此方法来处理Esc按键的行为
        /// </summary>
        /// <returns>返回true表示已处理Esc按键，返回false表示未处理（会继续传递给其他UI）</returns>
        protected virtual bool OnEscButtonPressed()
        {
            // 默认返回false，表示未处理，让其他UI处理
            return true;
        }

        /// <summary>
        /// 内部方法：处理Esc按键
        /// </summary>
        /// <returns>返回true表示已处理，false表示未处理</returns>
        internal bool HandleEscButton()
        {
            // 如果页面未显示，不处理
            if (!isPageDisplayed)
            {
                return false;
            }

            // 调用子类的OnEscButtonPressed方法
            return OnEscButtonPressed();
        }
    }
}