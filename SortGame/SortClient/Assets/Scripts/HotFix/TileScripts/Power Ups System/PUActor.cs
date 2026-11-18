using System;
using UnityEngine;

namespace FishTilePlay
{
    /// <summary>
    /// 道具行为基类 - 所有道具行为组件的抽象基类
    /// 定义了道具的基本接口和通用功能
    /// </summary>
    public abstract class PUActor : MonoBehaviour
    {
        protected PUConfig settings; // 道具设置
        public PUConfig Settings => settings;

        private bool isBusy; // 道具是否处于忙碌状态
        public bool IsBusy
        {
            get => isBusy;
            protected set
            {
                isBusy = value;
                isDirty = true; // 设置忙碌状态时标记为需要重新绘制

                bindBehaviour?.UpdateBtnStateShow();
            }
        }

        public PUUIActor bindBehaviour;

        protected bool isDirty = true; // 是否需要重新绘制UI
        public bool IsDirty => isDirty;

        /// <summary>
        /// 初始化道具设置
        /// </summary>
        /// <param name="settings">道具设置对象</param>
        public void InitialiseSettings(PUConfig settings)
        {
            this.settings = settings;
        }

        /// <summary>
        /// 初始化道具 - 子类必须实现
        /// </summary>
        public abstract void Init();

        /// <summary>
        /// 激活道具 - 子类必须实现，执行道具的具体功能
        /// </summary>
        /// <returns>激活是否成功</returns>
        public abstract bool Activate(Action beforeUseFunc = null);

        /// <summary>
        /// 获取浮动消息文本 - 道具使用时显示的消息
        /// </summary>
        /// <returns>浮动消息文本</returns>
        public virtual string GetFloatingMessage()
        {
            return settings.FloatingMessage;
        }

        /// <summary>
        /// 获取计时器 - 用于道具冷却时间显示
        /// </summary>
        /// <returns>计时器对象，如果没有则返回null</returns>
        public virtual PUTimer GetTimer()
        {
            return null;
        }

        /// <summary>
        /// 重置道具行为状态 - 子类可以重写以清理状态
        /// </summary>
        public virtual void ResetBehavior()
        {

        }

        /// <summary>
        /// 标记道具需要重新绘制UI
        /// </summary>
        public void SetDirty()
        {
            isDirty = true;
        }

        /// <summary>
        /// 标记道具已重新绘制完成
        /// </summary>
        public void OnRedrawn()
        {
            isDirty = false;
        }

        public virtual bool GetCanRelease()
        {
            return true;
        }
    }
}
