using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace FishFramework
{
    public enum UIPanelShowState
    {
        Initing,
        Refreshing,
        Idle,
        Hidden, /* Destroyed */
    }

    public enum UIPanelAnimState
    {
        Opening,
        Idle,
        Closing,
        Closed
    }

    public abstract class UIPanel : UIView
    {
        public string panelId => viewId;
        public UIPanelBehaviour panelBehaviour => (UIPanelBehaviour)viewBehaviour;
        public Canvas canvas { private set; get; }
        public GraphicRaycaster graphicRaycaster { private set; get; }
        private CanvasGroup canvasGroup { set; get; }
        public UIPanelShowState showState { protected set; get; }
        public UIPanelAnimState animState { protected set; get; }

        public Action closeExtraAct;

        public void Create(string panel_Id, RectTransform layerRect)
        {
            base.Create(panel_Id, layerRect);
            PlayOpenAnim(null);
            PlayOpenSound();
        }

        internal void Close(Action onFinish = null)
        {
            PlayCloseAnim(() =>
            {
                base.Destroy();
                onFinish?.Invoke();
            });
        }

        internal new void Destroy()
        {
            base.Destroy();
        }

        public void SetSortingOrder(int sortingOrder)
        {
            canvas.sortingOrder = sortingOrder;
        }

        internal void SetSiblingIndex(int siblingIndex)
        {
            rectTransform.SetSiblingIndex(siblingIndex);
        }

        public void SetVisible(bool visible)
        {
            if (showState != UIPanelShowState.Hidden && visible)
            {
                return;
            }

            if (showState == UIPanelShowState.Hidden && !visible)
            {
                return;
            }

            if (canvasGroup == null)
            {
                canvasGroup = gameObject.GetOrAddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = visible ? 1 : 0;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;

            showState = visible ? UIPanelShowState.Idle : UIPanelShowState.Hidden;

            OnVisibleChanged(visible);
        }

        internal void SetBackground()
        {
            switch (panelBehaviour.bgClickEventType)
            {
                case UIPanelBgClickEventType.PassThrough:
                    UIBlocker.Instance.Bind(rectTransform, panelBehaviour.bgTexture, panelBehaviour.bgColor, true,
                        null);
                    break;

                case UIPanelBgClickEventType.DontRespone:
                    UIBlocker.Instance.Bind(rectTransform, panelBehaviour.bgTexture, panelBehaviour.bgColor, false,
                        null);
                    break;

                case UIPanelBgClickEventType.CloseSelf:
                    UIBlocker.Instance.Bind(rectTransform, panelBehaviour.bgTexture, panelBehaviour.bgColor, false,
                        () =>
                        {
                            if (showState != UIPanelShowState.Idle)
                            {
                                return;
                            }

                            OnBackgroundClicked(panelBehaviour.bgClickEventType);
                        });
                    break;

                case UIPanelBgClickEventType.DestorySelf:
                    UIBlocker.Instance.Bind(rectTransform, panelBehaviour.bgTexture, panelBehaviour.bgColor, false,
                        () =>
                        {
                            if (showState != UIPanelShowState.Idle)
                            {
                                return;
                            }

                            OnBackgroundClicked(panelBehaviour.bgClickEventType);
                        });
                    break;

                case UIPanelBgClickEventType.Custom:
                    UIBlocker.Instance.Bind(rectTransform, panelBehaviour.bgTexture, panelBehaviour.bgColor, false,
                        () =>
                        {
                            if (showState != UIPanelShowState.Idle)
                            {
                                return;
                            }

                            OnBackgroundClicked(panelBehaviour.bgClickEventType);
                        });
                    break;
            }
        }

        internal void SetFocus(bool got)
        {
            OnFocusChanged(got);
        }

        internal void DoEscPress()
        {
        }

        #region 操作自身接口

        protected void CloseSelf(Action onFinish = null, bool willExcuteExtraAct = true)
        {
            GameModule.UI.ClosePanel(panelId, onFinish);

            if (willExcuteExtraAct)
            {
                closeExtraAct?.Invoke();
            }
        }

        protected void DestroySelf()
        {
            GameModule.UI.DestroyPanel(panelId);
        }

        protected void SetSelfVisible(bool visible)
        {
            GameModule.UI.SetPanelVisible(panelId, visible);
        }

        #endregion 操作自身接口

        #region 打开关闭动画接口

        protected virtual void PlayOpenAnim(Action onFinish = null)
        {
            if (panelBehaviour.PanelType == UIPanelType.Popup || panelBehaviour.PanelType == UIPanelType.Tips)
            {
                if (panelBehaviour.AnimNodeRt != null)
                {
                    panelBehaviour.AnimNodeRt.localScale = new Vector3(0, 0, 1);
                    Sequence sequence = DOTween.Sequence();
                    sequence.Append(panelBehaviour.AnimNodeRt.DOScale(new Vector3(1.05f, 1.05f, 1), 0.18f));
                    sequence.Append(panelBehaviour.AnimNodeRt.DOScale(new Vector3(1f, 1f, 1), 0.06f));
                    sequence.AppendCallback(() =>
                    {
                        animState = UIPanelAnimState.Idle;
                        onFinish?.Invoke();
                    });
                    sequence.SetUpdate(true);
                    sequence.SetLink(gameObject);
                    sequence.OnComplete(() => sequence.Kill());
                }
            }
        }

        protected virtual void PlayOpenSound()
        {
        }

        protected virtual void PlayCloseAnim(Action onFinish = null)
        {
            // if (panelBehaviour.ExistValidAnimator() && panelBehaviour.openAnimPlayMode == UIPanelOpenAnimPlayMode.AutoPlay)
            // {
            animState = UIPanelAnimState.Closed;
            onFinish?.Invoke();
            // }
        }

        #endregion 打开关闭动画接口

        protected override void OnInternalCreating()
        {
            base.OnInternalCreating();

            rectTransform.localPosition = Vector3.zero;
            rectTransform.localRotation = Quaternion.Euler(Vector3.zero);
            rectTransform.localScale = Vector3.one;

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;

            canvas = panelBehaviour.gameObject.GetOrAddComponent<Canvas>();
            graphicRaycaster = gameObject.GetOrAddComponent<GraphicRaycaster>();
            canvas.overrideSorting = true;
            canvas.sortingLayerName = "UI";
        }

        protected override void OnInternalCreated()
        {
            showState = UIPanelShowState.Idle;
            animState = UIPanelAnimState.Idle;
        }

        protected override void OnInternalDestroying()
        {
            UIBlocker.Instance.Unbind();

            if (panelBehaviour != null && panelBehaviour.AnimNodeRt != null)
                panelBehaviour.AnimNodeRt.DOKill();

            //组件引用解除即可, 实例会随gameObject销毁
            canvasGroup = null;
            graphicRaycaster = null;
            canvas = null;

            base.OnInternalDestroying();
        }

        protected override void OnInternalDestroyed()
        {
            //showState = UIPanelShowState.Destroyed;
        }

        #region 子类生命周期

        protected virtual void OnVisibleChanged(bool visible)
        {
        }

        protected virtual void OnFocusChanged(bool got)
        {
        }

        protected virtual void OnBackgroundClicked(UIPanelBgClickEventType bgClickEventType)
        {
            switch (bgClickEventType)
            {
                case UIPanelBgClickEventType.CloseSelf:
                    {
                        CloseSelf(null);
                        break;
                    }
                case UIPanelBgClickEventType.DestorySelf:
                    {
                        DestroySelf();
                        break;
                    }
            }
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
        /// 是否参与 Esc/返回键路由（可见且未移出屏幕的 Idle 面板）
        /// </summary>
        internal bool IsEligibleForEscRouting()
        {
            if (showState != UIPanelShowState.Idle)
            {
                return false;
            }

            if (Layer == PanelLayer.Block || Layer == PanelLayer.BlackBorder)
            {
                return false;
            }

            if (rectTransform != null && rectTransform.anchoredPosition.x <= -3000f)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 内部方法：处理Esc按键
        /// </summary>
        /// <returns>返回true表示已处理，false表示未处理</returns>
        internal bool HandleEscButton()
        {
            if (!IsEligibleForEscRouting())
            {
                return false;
            }

            return OnEscButtonPressed();
        }

        #endregion 子类生命周期

        /// <summary>
        /// 粒子层级设置方法，比当前canvas的层级增加1
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="order"></param>
        /// <param name="orderName"></param>
        public void SetParticleSortOrder(GameObject obj, int order = 1, string orderName = "UI")
        {
            ParticleSystemRenderer[] particleSystemRenderers = obj.GetComponentsInChildren<ParticleSystemRenderer>(true);
            foreach (ParticleSystemRenderer pr in particleSystemRenderers)
            {
                pr.sortingOrder = canvas.sortingOrder + order;
                pr.sortingLayerName = orderName;
            }
        }
    }
}