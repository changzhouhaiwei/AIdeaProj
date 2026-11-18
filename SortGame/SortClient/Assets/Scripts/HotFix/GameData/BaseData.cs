using System;
using System.Collections.Generic;
using FishFramework;

public abstract class BaseData
{
    private readonly List<int> listEventTypes;
    private readonly List<Delegate> listHandles;
    private readonly bool isInit = false;

    protected BaseData()
    {
        if (isInit)
        {
            return;
        }

        isInit = true;
        listEventTypes = new List<int>();
        listHandles = new List<Delegate>();
    }

    public void OnClear()
    {
        if (!isInit)
        {
            return;
        }

        for (int i = 0; i < listEventTypes.Count; ++i)
        {
            var eventType = listEventTypes[i];
            var handle = listHandles[i];
            GameEvent.RemoveEventListener(eventType, handle);
        }

        listEventTypes.Clear();
        listHandles.Clear();
    }

    private void AddEvent(int eventType, Delegate handler)
    {
        listEventTypes.Add(eventType);
        listHandles.Add(handler);
    }

    protected void AddDataEvent<T>(int eventType, Action<T> handler)
    {
        if (GameEvent.AddEventListener(eventType, handler))
        {
            AddEvent(eventType, handler);
        }
    }

    public abstract void OnInit();
    
    public abstract void OnReset();

    public abstract void OnRemove();
}