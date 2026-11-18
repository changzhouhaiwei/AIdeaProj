using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TileGameCore
{
    public class UIController : MonoBehaviour
    {
        private static UIController uiController;

        [SerializeField] FloatingCloud currencyCloud;
        [SerializeField] NotchSaveArea notchSaveArea;
        
        private static List<UIPage> pages;
        private static Dictionary<Type, UIPage> pagesLink = new Dictionary<Type, UIPage>();
        
        // 维护显示顺序的栈，后显示的在上层
        private static List<UIPage> displayedPagesStack = new List<UIPage>();

        private static List<IPopupWindow> popupWindows;
        public static bool IsPopupOpened => !popupWindows.IsNullOrEmpty();

        private static bool isTablet;
        public static bool IsTablet => isTablet;

        private static Canvas mainCanvas;
        public static Canvas MainCanvas => mainCanvas;
        public static CanvasScaler CanvasScaler { get; private set; }

        private static Camera mainCamera;

        private static SimpleCallback localPageClosedCallback;

        public static event PageCallback PageOpened;
        public static event PageCallback PageClosed;

        public static event PopupWindowCallback PopupOpened;
        public static event PopupWindowCallback PopupClosed;

        public void Init()
        {
            uiController = this;

            mainCanvas = GetComponent<Canvas>();
            CanvasScaler = GetComponent<CanvasScaler>();

            isTablet = false;//UIUtils.IsWideScreen(Camera.main);
            mainCamera = Camera.main;

            if (mainCamera != null && mainCamera.aspect <= 720 / 1560f)
            {
                CanvasScaler.matchWidthOrHeight = 0 ;//isTablet ? 1 : 0;
            }
            else
            {
                CanvasScaler.matchWidthOrHeight = 1 ;//isTablet ? 1 : 0;
            }


            popupWindows = new List<IPopupWindow>();

            pages = new List<UIPage>();
            pagesLink = new Dictionary<Type, UIPage>();
            for (int i = 0; i < transform.childCount; i++)
            {
                UIPage uiPage = transform.GetChild(i).GetComponent<UIPage>();
                if(uiPage != null)
                {
                    uiPage.CacheComponents();

                    if(pagesLink.ContainsKey(uiPage.GetType()))
                    {
                        Debug.LogError($"[UI Controller] Page {uiPage.GetType()} is already added to the UIController. Please remove the duplicate object to resolve this issue.", uiPage);

                        continue;
                    }

                    pagesLink.Add(uiPage.GetType(), uiPage);

                    pages.Add(uiPage);
                }
            }

            // Initialize global overlay
            Overlay.Init(this);
        }

        public void InitPages()
        {
            // Refresh notch save area
            notchSaveArea.Init();

            // Initialize currency cloud
            currencyCloud.Init();

            for (int i = 0; i < pages.Count; i++)
            {
                pages[i].Init();
                pages[i].DisableCanvas();
            }
        }

        public static void ResetPages()
        {
            UIController controller = uiController;
            if (controller != null)
            {
                for (int i = 0; i < pages.Count; i++)
                {
                    if (pages[i].IsPageDisplayed)
                    {
                        pages[i].Unload();
                        // 从显示顺序栈中移除
                        displayedPagesStack.Remove(pages[i]);
                    }
                }
            }
        }

        public static void RegisterPage(UIPage page)
        {
            if (page == null || pagesLink == null || pages == null)
                return;

            Type pageType = page.GetType();
            if (pagesLink.ContainsKey(pageType))
                return;

            page.CacheComponents();
            page.Init();
            page.DisableCanvas();
            pagesLink.Add(pageType, page);
            pages.Add(page);
        }

        public static bool TryGetRegisteredPage<T>(out T page) where T : UIPage
        {
            page = GetPage<T>();
            return page != null;
        }

        public static void ShowPage<T>() where T : UIPage
        {
            Type pageType = typeof(T);
            if (pagesLink == null || !pagesLink.TryGetValue(pageType, out UIPage page) || page == null)
                return;

            if (!page.IsPageDisplayed)
            {
                page.IsPageDisplayed = true;
                page.PlayShowAnimation();
                page.EnableCanvas();
                page.GraphicRaycaster.enabled = true;
            }
            
            // 无论是否已经显示，都更新显示顺序栈（确保在最上层）
            displayedPagesStack.Remove(page);
            displayedPagesStack.Add(page);
        }

        /// <summary>
        /// 结算页展示：即使页面已处于显示状态，也重新播放 WellDone 与后续结算流程。
        /// </summary>
        public static void ShowSettlementPage<T>() where T : UIPage
        {
            Type pageType = typeof(T);
            if (pagesLink == null || !pagesLink.TryGetValue(pageType, out UIPage page) || page == null)
                return;

            if (!page.IsPageDisplayed)
            {
                ShowPage<T>();
                return;
            }

            page.PlayShowAnimation();
            page.EnableCanvas();
            page.GraphicRaycaster.enabled = true;
            displayedPagesStack.Remove(page);
            displayedPagesStack.Add(page);
        }

        public static void ShowPage(UIPage page)
        {
            if (!page.IsPageDisplayed)
            {
                page.PlayShowAnimation();
                page.EnableCanvas();
                page.GraphicRaycaster.enabled = true;
            }
            
            // 无论是否已经显示，都更新显示顺序栈（确保在最上层）
            displayedPagesStack.Remove(page);
            displayedPagesStack.Add(page);
        }

        public static void HidePage<T>(SimpleCallback onPageClosed = null)
        {
            Type pageType = typeof(T);
            if (pagesLink == null || !pagesLink.TryGetValue(pageType, out UIPage page) || page == null)
            {
                onPageClosed?.Invoke();
                return;
            }

            if (page.IsPageDisplayed)
            {
                localPageClosedCallback = onPageClosed;

                page.GraphicRaycaster.enabled = false;
                page.PlayHideAnimation();
            }
            else
            {
                onPageClosed?.Invoke();
            }
        }

        public static void DisablePage<T>()
        {
            Type pageType = typeof(T);
            if (pagesLink == null || !pagesLink.TryGetValue(pageType, out UIPage page) || page == null)
                return;

            if (page.IsPageDisplayed)
            {
                page.DisableCanvas();

                OnPageClosed(page);
            }
        }

        public static bool IsDisplayed<T>() where T : UIPage
        {
            Type type = typeof(T);
            if (pagesLink.ContainsKey(type))
            {
                return pagesLink[type].IsPageDisplayed;
            }

            return false;
        }

        public static void OnPageClosed(UIPage page)
        {
            page.DisableCanvas();
            
            // 从显示顺序栈中移除
            displayedPagesStack.Remove(page);

            PageClosed?.Invoke(page, page.GetType());

            if (localPageClosedCallback != null)
            {
                localPageClosedCallback.Invoke();
                localPageClosedCallback = null;
            }
        }

        public static void OnPageOpened(UIPage page)
        {
            PageOpened?.Invoke(page, page.GetType());
        }

        public static void OnPopupWindowOpened(IPopupWindow popupWindow)
        {
            if(!popupWindows.Contains(popupWindow))
            {
                popupWindows.Add(popupWindow);

                PopupOpened?.Invoke(popupWindow, true);
            }
        }

        public static void OnPopupWindowClosed(IPopupWindow popupWindow)
        {
            if (popupWindows.Contains(popupWindow))
            {
                popupWindows.Remove(popupWindow);

                PopupClosed?.Invoke(popupWindow, false);
            }
        }

        public static T GetPage<T>() where T : UIPage
        {
            UIPage page;

            if (pagesLink.TryGetValue(typeof(T), out page))
                return (T)page;

            return null;
        }

        /// <summary>
        /// 获取最上层的UIPage（根据显示顺序，后显示的在上层）
        /// </summary>
        /// <returns>最上层的页面，如果没有则返回null</returns>
        public static UIPage GetTopPage()
        {
            if (displayedPagesStack == null || displayedPagesStack.Count == 0)
            {
                return null;
            }

            // 清理栈中已关闭的页面
            displayedPagesStack.RemoveAll(page => page == null || !page.IsPageDisplayed);

            // 如果清理后栈为空，返回null
            if (displayedPagesStack.Count == 0)
            {
                return null;
            }

            // 返回栈顶的页面（最后显示的）
            return displayedPagesStack[displayedPagesStack.Count - 1];
        }

        /// <summary>
        /// 处理最上层UIPage的Esc按键
        /// </summary>
        /// <returns>返回true表示已处理，false表示未处理</returns>
        public static bool HandleTopPageEsc()
        {
            UIPage topPage = GetTopPage();
            if (topPage != null)
            {
                return topPage.HandleEscButton();
            }
            return false;
        }

        public static Vector3 FixUIElementToWorld(Transform target, Vector3 offset)
        {
            Vector3 targPos = target.transform.position + offset;
            Vector3 camForward = mainCamera.transform.forward;

            float distInFrontOfCamera = Vector3.Dot(targPos - (mainCamera.transform.position + camForward), camForward);
            if (distInFrontOfCamera < 0f)
            {
                targPos -= camForward * distInFrontOfCamera;
            }

            return RectTransformUtility.WorldToScreenPoint(mainCamera, targPos);
        }

        private void OnDestroy()
        {
            FloatingCloud.Clear();

            Overlay.Clear();
        }

        public delegate void PageCallback(UIPage page, Type pageType);
        public delegate void PopupWindowCallback(IPopupWindow popupWindow, bool state);
    }
}
