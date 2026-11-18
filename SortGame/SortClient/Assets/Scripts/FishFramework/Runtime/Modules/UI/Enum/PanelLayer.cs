using UnityEngine;

namespace FishFramework
{
    //不要修改值 否则已存在的界面会错误
    //只能新增 不允许修改
    /// <summary>
    /// 层级类型
    /// </summary>
    public enum PanelLayer
    {
        /// <summary>
        /// 屏蔽层
        /// </summary>
        [Header("屏蔽层")] Block = 0,

        /// <summary>
        /// 黑边
        /// </summary>
        [Header("黑边")] BlackBorder = 1,

        /// <summary>
        /// 最高层  
        /// </summary>
        [Header("最高层")] Top = 2,

        /// <summary>
        /// 新手引导
        /// </summary>
        [Header("新手引导")] Guide = 3,

        /// <summary>
        /// 提示层
        /// 一般 提示飘字 确认弹窗  跑马灯之类的
        /// </summary>
        [Header("提示层")] Tips = 4,

        /// <summary>
        /// GM命令
        /// </summary>
        [Header("GM")] GM = 5,

        /// <summary>
        /// 普通面板层
        /// 全屏界面 所有Panel打开关闭受回退功能影响
        /// </summary>
        [Header("面板层")] Panel = 6,

        /// <summary>
        /// 只是用来记录数量，不可用
        /// </summary>
        [Header("")] Count = 7,
    }
}