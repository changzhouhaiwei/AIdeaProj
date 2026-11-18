using System;
using TMPro;
using UnityEngine;

public class GMCell : MonoBehaviour
{
    [SerializeField] private TButton button;
    [SerializeField] private TextMeshProUGUI descText;

    private Action<int> callBack;
    private int index;

    private void Start()
    {
        button.onClick.AddListener(() => { callBack?.Invoke(index); });
    }

    public void AddListener(Action<int> action)
    {
        callBack = action;
    }

    public void UpdateUI(string desc, int idx)
    {
        index = idx;
        descText.text = desc;
    }
}