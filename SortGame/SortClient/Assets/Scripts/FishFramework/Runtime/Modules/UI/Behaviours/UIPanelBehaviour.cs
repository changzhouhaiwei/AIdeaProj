using System;
using System.Collections;
using UnityEngine;

namespace FishFramework
{
    public enum UIPanelType
    {
        /// <summary>
        /// 全屏界面、一级功能界面等：有背景（透明、阻挡下方事件、点击背景不响应）
        /// </summary>
        FullScreen,

        /// <summary>
        /// 弹出型功能界面、确认框等：有背景（黑色半透、阻挡下方事件、点击背景默认关闭自身）
        /// </summary>
        Popup,

        /// <summary>
        /// 弹出型功能界面、确认框等：有背景（黑色半透、阻挡下方事件、点击背景默认关闭自身）
        /// 与Pop的区别主要没有堆栈效果，适合突然弹出来的礼包
        /// </summary>
        Tips,

        /// <summary>
        /// 浮动功能气泡（如聊天气泡）、Toast等：无背景、不抢夺焦点
        /// </summary>
        Float,

        /// <summary>
        /// 网络转圈等待、引导界面等：有背景（黑色半透、阻挡下方事件、点击背景不响应）
        /// </summary>
        System,

        /// <summary>
        /// 支持灵活设置背景、焦点、返回键按下类型，但最好通过增加类型的方式解决
        /// </summary>
        Custom
    }

    public enum UIPanelBgShowType
    {
        Alpha,
        HalfAlphaBlack,
        CustomColor, /* CustomTexture, BlurryScreenshot */
    }

    public enum UIPanelBgClickEventType
    {
        PassThrough,
        DontRespone,
        CloseSelf,
        DestorySelf,
        Custom,
    }

    public enum UIPanelOpenAnimPlayMode
    {
        AutoPlay,
        ControlBySelf
    }

    public enum UIPanelCloseAnimPlayMode
    {
        AutoPlay,
        ControlBySelf
    }

    public class UIPanelBehaviour : UIViewBehaviour
    {
#pragma warning disable 414
        [SerializeField] private UIPanelType m_PanelType;
#pragma warning restore 414

        [SerializeField] private bool m_HasBg;

        [SerializeField] private UIPanelBgShowType m_BgShowType;

        [SerializeField] private Color m_CustomBgColor;

        [SerializeField] private UIPanelBgClickEventType m_BgClickEventType;

        //层级相关
        [SerializeField] private int m_Thickness;

        [SerializeField] private UIPanelOpenAnimPlayMode m_OpenAnimPlayMode;

        [SerializeField] private UIPanelCloseAnimPlayMode m_CloseAnimPlayMode;

        [SerializeField] private RectTransform m_AnimNode;

        public UIPanelType PanelType => m_PanelType;

        public bool hasBg => m_HasBg;

        public RectTransform AnimNodeRt => m_AnimNode;

        private static readonly Color sm_BgColor_Alpha = new(0, 0, 0, 0);
        private static readonly Color sm_BgColor_HalfAlphaBlack = new(0, 0, 0, 0.7f);

        public Texture bgTexture => null;

        private const int DefaultPanelThickness = 10;

        public Color bgColor
        {
            get
            {
                Debug.Assert(hasBg);
                return m_BgShowType switch
                {
                    UIPanelBgShowType.Alpha => sm_BgColor_Alpha,
                    UIPanelBgShowType.HalfAlphaBlack => sm_BgColor_HalfAlphaBlack,
                    UIPanelBgShowType.CustomColor => m_CustomBgColor,
                    _ => Color.white,
                };
            }
        }

        public UIPanelBgClickEventType bgClickEventType => m_BgClickEventType;

        public int thickness => m_Thickness;

        public UIPanelOpenAnimPlayMode openAnimPlayMode => m_OpenAnimPlayMode;

        public UIPanelCloseAnimPlayMode closeAnimPlayMode => m_CloseAnimPlayMode;

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();

            //默认显示为 FullScreen
            m_PanelType = UIPanelType.FullScreen;

            //默认显示为 FullScreen 的子项
            m_HasBg = true;
            m_BgShowType = UIPanelBgShowType.Alpha;
            m_CustomBgColor = Color.white;
            m_BgClickEventType = UIPanelBgClickEventType.DontRespone;

            m_Thickness = DefaultPanelThickness;
            m_OpenAnimPlayMode = UIPanelOpenAnimPlayMode.AutoPlay;
            m_CloseAnimPlayMode = UIPanelCloseAnimPlayMode.AutoPlay;
        }
#endif
        public bool ExistValidAnimator()
        {
            return m_AnimNode != null;
        }
    }
}