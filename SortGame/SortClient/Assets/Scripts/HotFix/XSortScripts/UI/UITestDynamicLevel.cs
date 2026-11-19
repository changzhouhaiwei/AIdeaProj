using System;
using System.Collections;
using System.Collections.Generic;
using FishXSortPlay;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using FishFramework;
using XSortGameCore;

public class UITestDynamicLevel : MonoBehaviour
{
    private const string LevelBtnName = "btn6 (4)";
    private const string LastBtnName = "btnLast";
    private const string HideBtnName = "hideBtn";
    private const float LastBtnSpacing = 27.5f;

    [SerializeField] private TextMeshProUGUI winLb;
    [SerializeField] private TextMeshProUGUI failLb;

    static UITestDynamicLevel instance;

    TextMeshProUGUI hideBtnLabel;
    bool collapsed;
    readonly Dictionary<Transform, bool> collapsedChildActive = new();

    void Awake()
    {
        instance = this;
        RefreshVisibility();
    }

    void Start()
    {
        EnsureLastButton();
        WireHideBtn();
        RefreshWinFail();
        if (GameData.BFromStartScene)
        {
            GPUserDataManager.Instance.OnDataChanged += RefreshWinFail;
        }
    }

    public static void RefreshActive()
    {
        var target = instance != null ? instance : UnityEngine.Object.FindObjectOfType<UITestDynamicLevel>(true);
        target?.RefreshVisibility();
    }

    public void RefreshVisibility()
    {
        instance = this;
        bool show = GameData.BFromStartScene
            && GameModule.Setting != null
            && GameModule.Setting.GmLevelPassGlobalSwitch;
        if (gameObject.activeSelf != show)
        {
            gameObject.SetActive(show);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
        GameData.BFromStartScene = false;
        if (GameData.BFromStartScene)
        {
            GPUserDataManager.Instance.OnDataChanged -= RefreshWinFail;
        }
    }

    public void RefreshWinFail(String dataChangeStr = "")
    {
        winLb.text = "WIN:" + GPUserDataManager.Instance.UserData.continusWinCnt;
        failLb.text = "Fail:" + GPUserDataManager.Instance.UserData.continusFailCnt;
    }

    public void OnDefaultClick()
    {
        var levelIndex = GameDirector.levelController.GetLoadedLevelIndex();
        var cfg = LevelDynamicConfigProvider.GetLevelConfig(levelIndex + 1);
        var dd = cfg.DefaultCfg;
        GameDirector.LoadLevel(levelIndex, null, dd);
    }

    public void HandleWinButtonTap(int winCnt)
    {
        if (GameDirector.levelController == null)
        {
            return;
        }

        var levelIndex = GameDirector.levelController.GetLoadedLevelIndex();
        var cfg = LevelDynamicConfigProvider.GetLevelConfig(levelIndex + 1);
        if (cfg == null)
        {
            Debug.LogWarning($"Level {levelIndex + 1}, config not found for Win{winCnt}");
            return;
        }

        string winValue = winCnt switch
        {
            1 => cfg.Win1,
            2 => cfg.Win2,
            3 => cfg.Win3,
            4 => cfg.Win4,
            _ => ""
        };

        if (string.IsNullOrEmpty(winValue))
        {
            Debug.LogWarning($"Level {levelIndex + 1}, Win{winCnt} value is empty");
            return;
        }

        Debug.Log($"Level {levelIndex + 1}, Win{winCnt} value: {winValue}");
        GameDirector.LoadLevel(levelIndex, null, winValue, forceOpen: true);
    }

    public void HandleFailButtonTap(int failCnt)
    {
        if (GameDirector.levelController == null)
        {
            return;
        }

        var levelIndex = GameDirector.levelController.GetLoadedLevelIndex();
        var cfg = LevelDynamicConfigProvider.GetLevelConfig(levelIndex + 1);
        if (cfg == null)
        {
            Debug.LogWarning($"Level {levelIndex + 1}, config not found for Fail{failCnt}");
            return;
        }

        string failValue = failCnt switch
        {
            1 => cfg.Fail1,
            2 => cfg.Fail2,
            3 => cfg.Fail3,
            4 => cfg.Fail4,
            _ => ""
        };

        if (string.IsNullOrEmpty(failValue))
        {
            Debug.LogWarning($"Level {levelIndex + 1}, Fail{failCnt} value is empty");
            return;
        }

        Debug.Log($"Level {levelIndex + 1}, Fail{failCnt} value: {failValue}");
        GameDirector.LoadLevel(levelIndex, null, failValue, forceOpen: true);
    }

    public void HandleChooseButtonTap()
    {
        if (GameDirector.levelController == null)
        {
            return;
        }

        var nextLevelNum = GameDirector.levelController.GetLoadedLevelIndex() + 1;
        if (nextLevelNum < 0)
        {
            return;
        }

        GPUserDataManager.Instance.SetLevelNum(nextLevelNum);
        GameDirector.LoadLevel(nextLevelNum);
    }

    public void OnBtnLastClick()
    {
        if (GameDirector.levelController == null)
        {
            return;
        }

        var prevLevelNum = GameDirector.levelController.GetLoadedLevelIndex() - 1;
        if (prevLevelNum < 0)
        {
            return;
        }

        GPUserDataManager.Instance.SetLevelNum(prevLevelNum);
        GameDirector.LoadLevel(prevLevelNum);
    }

    void WireHideBtn()
    {
        var hideBtnTf = transform.Find(HideBtnName);
        if (hideBtnTf == null)
        {
            return;
        }

        var button = hideBtnTf.GetComponent<Button>();
        if (button != null)
        {
            button.onClick = new Button.ButtonClickedEvent();
        }

        if (button is TButton tButton)
        {
            tButton.clickDoTween = false;
        }

        var drag = hideBtnTf.GetComponent<HideBtnLongPressDrag>();
        if (drag == null)
        {
            drag = hideBtnTf.gameObject.AddComponent<HideBtnLongPressDrag>();
        }
        drag.Init((RectTransform)transform, OnHideBtnClick, () => collapsed);

        hideBtnLabel = hideBtnTf.GetComponentInChildren<TextMeshProUGUI>(true);
        ApplyHideBtnLabel();
    }

    void OnHideBtnClick()
    {
        collapsed = !collapsed;
        if (collapsed)
        {
            collapsedChildActive.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.name == HideBtnName)
                {
                    continue;
                }

                collapsedChildActive[child] = child.gameObject.activeSelf;
                child.gameObject.SetActive(false);
            }
        }
        else
        {
            foreach (var pair in collapsedChildActive)
            {
                if (pair.Key != null)
                {
                    pair.Key.gameObject.SetActive(pair.Value);
                }
            }
            collapsedChildActive.Clear();
        }

        ApplyHideBtnLabel();
    }

    void ApplyHideBtnLabel()
    {
        if (hideBtnLabel != null)
        {
            hideBtnLabel.text = collapsed ? "show" : "hide";
        }
    }

    void EnsureLastButton()
    {
        if (transform.Find(LastBtnName) != null)
        {
            return;
        }

        var levelBtnTf = transform.Find(LevelBtnName);
        if (levelBtnTf == null)
        {
            return;
        }

        var lastGo = Instantiate(levelBtnTf.gameObject, transform);
        lastGo.name = LastBtnName;

        var levelRect = levelBtnTf.GetComponent<RectTransform>();
        var lastRect = lastGo.GetComponent<RectTransform>();
        if (levelRect != null && lastRect != null)
        {
            lastRect.anchoredPosition = levelRect.anchoredPosition
                + new Vector2(levelRect.sizeDelta.x + LastBtnSpacing, 0f);
        }

        var label = lastGo.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            label.text = "last";
        }

        var button = lastGo.GetComponent<Button>();
        if (button != null)
        {
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(OnBtnLastClick);
        }
    }

}

class HideBtnLongPressDrag : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
    IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    Action onClick;
    Func<bool> dragAllowed;
    RectTransform rect;
    Vector2 grabOffset;
    bool dragging;

    public void Init(RectTransform dragTarget, Action click, Func<bool> allowDrag)
    {
        onClick = click;
        dragAllowed = allowDrag;
        rect = dragTarget;
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        eventData.useDragThreshold = true;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        dragging = false;
        CacheGrabOffset(eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (dragAllowed == null || !dragAllowed())
        {
            eventData.pointerDrag = null;
            eventData.dragging = false;
            return;
        }

        dragging = true;
        eventData.eligibleForClick = false;
        CacheGrabOffset(eventData);
        if (rect != null)
        {
            rect.SetAsLastSibling();
        }

        GetComponent<TButton>()?.CancelPressTween(true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging)
        {
            return;
        }

        ApplyDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        dragging = false;
        eventData.eligibleForClick = false;
        eventData.pointerDrag = null;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!dragging)
        {
            onClick?.Invoke();
        }

        dragging = false;
    }

    void CacheGrabOffset(PointerEventData eventData)
    {
        if (rect == null || rect.parent is not RectTransform parent)
        {
            return;
        }

        var cam = eventData.pressEventCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, cam, out var local))
        {
            grabOffset = (Vector2)rect.localPosition - local;
        }
    }

    void ApplyDrag(PointerEventData eventData)
    {
        if (rect == null || rect.parent is not RectTransform parent)
        {
            return;
        }

        var cam = eventData.pressEventCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, cam, out var local))
        {
            rect.localPosition = local + grabOffset;
        }
    }
}
