using System;

namespace FishFramework
{
    public abstract partial class UIView
    {
        #region 网络消息通知

        /// <summary>
        /// 注册UI事件。
        /// </summary>
        private GameEventMgr _eventMgr;

        protected GameEventMgr EventMgr
        {
            get
            {
                if (_eventMgr == null)
                {
                    _eventMgr = MemoryPool.Acquire<GameEventMgr>();
                }

                return _eventMgr;
            }
        }

        public void AddUIEvent(int eventType, Action handler)
        {
            EventMgr.AddEvent(eventType, handler);
        }

        protected void AddUIEvent<T>(int eventType, Action<T> handler)
        {
            EventMgr.AddEvent(eventType, handler);
        }

        protected void AddUIEvent<T, U>(int eventType, Action<T, U> handler)
        {
            EventMgr.AddEvent(eventType, handler);
        }

        protected void AddUIEvent<T, U, V>(int eventType, Action<T, U, V> handler)
        {
            EventMgr.AddEvent(eventType, handler);
        }

        protected void AddUIEvent<T, U, V, W>(int eventType, Action<T, U, V, W> handler)
        {
            EventMgr.AddEvent(eventType, handler);
        }

        protected void RemoveAllUIEvent()
        {
            if (_eventMgr != null)
            {
                MemoryPool.Release(_eventMgr);
            }
        }

        #endregion
    }
}