using System;
using System.Collections.Generic;

namespace FishFramework
{
    public abstract partial class UIView
    {
        #region 定时器

        private HashSet<Timer> timerSet = new HashSet<Timer>();

        protected Timer Delay(float duration, Action onComplete, bool useRealTime = false)
        {
            Timer timer = Timer.DelayAction(duration, onComplete, null, useRealTime);
            timerSet.Add(timer);
            return timer;
        }

        protected Timer Interval(float interval, Action<float> onUpdate, Action onComplete = null, bool useRealTime = false, bool executeOnStart = true)
        {
            Timer timer = Timer.LoopAction(interval, onComplete, onUpdate, useRealTime, executeOnStart);
            timerSet.Add(timer);
            return timer;
        }

        protected void Unschedule(Timer schedule)
        {
            Timer.Cancel(schedule);
            timerSet.Remove(schedule);
        }

        private void UnscheduleAll()
        {
            foreach (Timer timer in timerSet)
            {
                Timer.Cancel(timer);
            }

            timerSet.Clear();
            timerSet = null;
        }

        #endregion
    }
}