using Sirenix.OdinInspector;
using UnityEngine;

public class RedDotUI : MonoBehaviour
{
    [SerializeField] private string redDotKey;

    [ShowInInspector]
    [ReadOnly]
    [LabelText("显隐")]
    public bool Show { get; private set; }

    [ShowInInspector]
    [ReadOnly]
    [LabelText("数量")]
    public int Count { get; private set; }

    private void Awake()
    {
        if (string.IsNullOrEmpty(redDotKey) == false)
        {
            RedDotMgr.Instance.AddListener(redDotKey, OnRedDotChangeHandler);
        }
    }

    public void RegisterRedDotNode(string key)
    {
        redDotKey = key;
        RedDotMgr.Instance.AddListener(key, OnRedDotChangeHandler);
        OnRedDotChangeHandler(RedDotMgr.Instance.GetValue(key));
    }

    public void SetCount(int count)
    {
        OnRedDotChangeHandler(count);
    }

    private void OnDestroy()
    {
        if (string.IsNullOrEmpty(redDotKey) == false)
        {
            RedDotMgr.Instance.RemoveListener(redDotKey, OnRedDotChangeHandler);
        }
    }

    private void OnRedDotChangeHandler(int count)
    {
        Show = count >= 1;
        Count = count;
        Refresh();
    }

    private void Refresh()
    {
        gameObject.SetActive(Show);
    }
}