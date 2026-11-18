using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using xasset;
using Object = UnityEngine.Object;

public class MessageBox : IEnumerator
{
    private readonly Asset mAsset;

    private static readonly List<MessageBox> _showed = new List<MessageBox>();
    private static readonly List<MessageBox> _hidden = new List<MessageBox>();
    private TextMeshProUGUI _content;
    private TextMeshProUGUI _textOk;

    public bool ok;

    private MessageBox(string content, Action<bool> completed, string ok)
    {
        mAsset = Asset.Load("Assets/CABRes/Prefabs/HotFangshenhe/MessageBox.prefab", typeof(GameObject));
        gameObject = Object.Instantiate(mAsset.asset as GameObject);
        _content = GetComponent<TextMeshProUGUI>("nineBg/descBg/descText");
        _textOk = GetComponent<TextMeshProUGUI>("nineBg/sureButton/sureText");

        var ok1 = GetComponent<Button>("nineBg/sureButton");
        ok1.onClick.AddListener(OnClickOk);
        this.completed += completed;
        Init(content, ok);
    }

    public bool Visible { get; private set; } = true;

    private GameObject gameObject { get; set; }

    public Action<bool> completed { get; set; }

    public void SetContent(string value)
    {
        _content.text = value;
    }

    public static void Dispose()
    {
        foreach (var item in _hidden)
        {
            item.Destroy();
        }

        _hidden.Clear();

        foreach (var item in _showed)
        {
            item.Destroy();
        }

        _showed.Clear();
    }

    public static void CloseAll()
    {
        foreach (var messageBox in _showed)
        {
            messageBox.Hide();
            _hidden.Add(messageBox);
        }

        _showed.Clear();
    }

    public static MessageBox Show(string content, Action<bool> completed = null, string ok = "")
    {
        if (string.IsNullOrEmpty(ok))
        {
            ok = I2LocalizeMgr.GetString("HotFangshenhe_9");
        }

        if (_hidden.Count > 0)
        {
            var mb = _hidden[0];
            mb.completed += completed;
            mb.Init(content, ok);
            mb.gameObject.SetActive(true);
            _hidden.RemoveAt(0);
            return mb;
        }

        return new MessageBox(content, completed, ok);
    }

    private void Destroy()
    {
        _textOk = null;
        _content = null;
        Object.Destroy(gameObject);
        gameObject = null;
        mAsset.ReleaseRef();
    }

    private void Init(string content, string okTxt)
    {
        _content.text = content;
        _textOk.text = okTxt;
        _showed.Add(this);
        Visible = true;
    }

    private T GetComponent<T>(string path) where T : Component
    {
        var trans = gameObject.transform.Find(path);
        return trans.GetComponent<T>();
    }

    private void OnClickNo()
    {
        HandleEvent(false);
    }

    private void OnClickOk()
    {
        HandleEvent(true);
    }

    public void HandleEvent(bool isOk)
    {
        ok = isOk;
        Close();
        if (completed == null) return;
        var saved = completed;
        completed(isOk);
        completed -= saved;
    }

    public void Close()
    {
        Hide();
        _hidden.Add(this);
        _showed.Remove(this);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
        Visible = false;
    }

    #region IEnumerator implementation

    public bool MoveNext()
    {
        return Visible;
    }

    public void Reset()
    {
    }

    public object Current => null;

    #endregion
}