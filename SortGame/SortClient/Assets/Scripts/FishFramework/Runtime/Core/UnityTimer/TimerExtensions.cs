using System;
using FishFramework;
using UnityEngine;
using UpdateMode = FishFramework.Timer.UpdateMode;

/// <summary>
/// Contains extension methods related to <see cref="Timer"/>s.
/// </summary>
public static class TimerExtensions
{
    #region ComponentUseRealTime
    public static DelayTimer DelayAction(this Component component, float duration, Action onComplete, Action<float> onUpdate = null, bool useRealTime = false)
    {
        return Timer.DelayAction(duration, onComplete, onUpdate, useRealTime, component);
    }
    
    public static LoopTimer LoopAction(this Component component, float interval, Action onComplete, Action<float> onUpdate = null,
        bool useRealTime = false, bool executeOnStart = false)
    {
        return Timer.LoopAction(interval, onComplete, onUpdate, useRealTime, executeOnStart, component);
    }

    //Persistence
    public static DelayTimer PersistenceDelayAction(this Component component, float duration, Action onComplete, Action<float> onUpdate = null, bool useRealTime = false)
    {
        return Timer.PersistenceDelayAction(duration, onComplete, onUpdate, useRealTime, component);
    }
    
    public static LoopTimer PersistenceLoopAction(this Component component, float interval, Action onComplete, Action<float> onUpdate = null,
        bool useRealTime = false, bool executeOnStart = false)
    {
        return Timer.PersistenceLoopAction(interval, onComplete, onUpdate, useRealTime, executeOnStart, component);
    }
    #endregion
    
    #region GameObjectUseRealTime
    public static DelayTimer DelayAction(this GameObject gameObject, float duration, Action onComplete, Action<float> onUpdate = null, bool useRealTime = false)
    {
        return Timer.DelayAction(duration, onComplete, onUpdate, useRealTime, gameObject);
    }
    
    public static LoopTimer LoopAction(this GameObject component, float interval, Action onComplete, Action<float> onUpdate = null,
        bool useRealTime = false, bool executeOnStart = false)
    {
        return Timer.LoopAction(interval, onComplete, onUpdate, useRealTime, executeOnStart, component);
    }

    //Persistence
    public static DelayTimer PersistenceDelayAction(this GameObject component, float duration, Action onComplete, Action<float> onUpdate = null, bool useRealTime = false)
    {
        return Timer.PersistenceDelayAction(duration, onComplete, onUpdate, useRealTime, component);
    }
    
    public static LoopTimer PersistenceLoopAction(this GameObject component, float interval, Action onComplete, Action<float> onUpdate = null,
        bool useRealTime = false, bool executeOnStart = false)
    {
        return Timer.PersistenceLoopAction(interval, onComplete, onUpdate, useRealTime, executeOnStart, component);
    }
    #endregion
    
    #region Component
    public static DelayTimer DelayAction(this Component component, float duration, Action onComplete, Action<float> onUpdate, UpdateMode updateMode)
    {
        return Timer.DelayAction(duration, onComplete, onUpdate, updateMode, component);
    }
    
    public static DelayFrameTimer DelayFrameAction(this Component component, int frame, Action onComplete, Action<float> onUpdate = null)
    {
        return Timer.DelayFrameAction(frame, onComplete, onUpdate, component);
    }
    
    public static LoopTimer LoopAction(this Component component, float interval, Action onComplete, Action<float> onUpdate,
        UpdateMode updateMode, bool executeOnStart = false)
    {
        return Timer.LoopAction(interval, onComplete, onUpdate, updateMode, executeOnStart, component);
    }

    //Persistence
    public static DelayTimer PersistenceDelayAction(this Component component, float duration, Action onComplete, Action<float> onUpdate, UpdateMode updateMode)
    {
        return Timer.PersistenceDelayAction(duration, onComplete, onUpdate, updateMode, component);
    }
    
    public static DelayFrameTimer PersistenceDelayFrameAction(this Component component, int frame, Action onComplete, Action<float> onUpdate = null)
    {
        return Timer.PersistenceDelayFrameAction(frame, onComplete, onUpdate, component);
    }
    
    public static LoopTimer PersistenceLoopAction(this Component component, float interval, Action onComplete, Action<float> onUpdate,
        UpdateMode updateMode, bool executeOnStart = false)
    {
        return Timer.PersistenceLoopAction(interval, onComplete, onUpdate, updateMode, executeOnStart, component);
    }

    public static void CancelAllTimer(this Component component)
    {
        Timer.CancelAllRegisteredTimersByOwner(component);
    }
    #endregion
    
    #region GameObject
    public static DelayTimer DelayAction(this GameObject gameObject, float duration, Action onComplete, Action<float> onUpdate, UpdateMode updateMode)
    {
        return Timer.DelayAction(duration, onComplete, onUpdate, updateMode, gameObject);
    }
    
    public static DelayFrameTimer DelayFrameAction(this GameObject component, int frame, Action onComplete, Action<float> onUpdate = null)
    {
        return Timer.DelayFrameAction(frame, onComplete, onUpdate, component);
    }
    
    public static LoopTimer LoopAction(this GameObject component, float interval, Action onComplete, Action<float> onUpdate,
        UpdateMode updateMode, bool executeOnStart = false)
    {
        return Timer.LoopAction(interval, onComplete, onUpdate, updateMode, executeOnStart, component);
    }

    //Persistence
    public static DelayTimer PersistenceDelayAction(this GameObject component, float duration, Action onComplete, Action<float> onUpdate, UpdateMode updateMode)
    {
        return Timer.PersistenceDelayAction(duration, onComplete, onUpdate, updateMode, component);
    }
    
    public static DelayFrameTimer PersistenceDelayFrameAction(this GameObject component, int frame, Action onComplete, Action<float> onUpdate = null)
    {
        return Timer.PersistenceDelayFrameAction(frame, onComplete, onUpdate, component);
    }
    
    public static LoopTimer PersistenceLoopAction(this GameObject component, float interval, Action onComplete, Action<float> onUpdate,
        UpdateMode updateMode, bool executeOnStart = false)
    {
        return Timer.PersistenceLoopAction(interval, onComplete, onUpdate, updateMode, executeOnStart, component);
    }

    public static void CancelAllTimer(this GameObject component)
    {
        Timer.CancelAllRegisteredTimersByOwner(component);
    }
    #endregion
}
