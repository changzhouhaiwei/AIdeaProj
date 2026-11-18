using UnityEngine.Events;
using FishFramework;

public class MainBehaviour : MonoSingleton<MainBehaviour>
{
    private event UnityAction UpdateEvent;
    private event UnityAction FixedUpdateEvent;
    private event UnityAction LateUpdateEvent;
    private event UnityAction<bool> OnApplicationPauseEvent;
    private event UnityAction<bool> OnApplicationFocusEvent;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        UpdateEvent?.Invoke();
    }

    private void FixedUpdate()
    {
        FixedUpdateEvent?.Invoke();
    }

    private void LateUpdate()
    {
        LateUpdateEvent?.Invoke();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        OnApplicationPauseEvent?.Invoke(pauseStatus);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        OnApplicationFocusEvent?.Invoke(hasFocus);
    }

    public void AddLateUpdateListener(UnityAction fun)
    {
        LateUpdateEvent += fun;
    }

    public void RemoveLateUpdateListener(UnityAction fun)
    {
        LateUpdateEvent -= fun;
    }

    public void AddFixedUpdateListener(UnityAction fun)
    {
        FixedUpdateEvent += fun;
    }

    public void RemoveFixedUpdateListener(UnityAction fun)
    {
        FixedUpdateEvent -= fun;
    }

    public void AddUpdateListener(UnityAction fun)
    {
        UpdateEvent += fun;
    }

    public void RemoveUpdateListener(UnityAction fun)
    {
        UpdateEvent -= fun;
    }

    public void AddOnApplicationPauseListener(UnityAction<bool> fun)
    {
        OnApplicationPauseEvent += fun;
    }

    public void RemoveOnApplicationPauseListener(UnityAction<bool> fun)
    {
        OnApplicationPauseEvent -= fun;
    }

    public void AddOnApplicationFocusListener(UnityAction<bool> fun)
    {
        OnApplicationFocusEvent += fun;
    }

    public void RemoveOnApplicationFocusListener(UnityAction<bool> fun)
    {
        OnApplicationFocusEvent -= fun;
    }

    public void Release()
    {
        UpdateEvent = null;
        FixedUpdateEvent = null;
        LateUpdateEvent = null;
        OnApplicationPauseEvent = null;
        OnApplicationFocusEvent = null;
    }
}