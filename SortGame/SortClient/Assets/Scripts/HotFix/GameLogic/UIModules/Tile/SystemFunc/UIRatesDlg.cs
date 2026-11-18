
using System.Collections.Generic;
using Core.Tools.MethodExtension;
using FishTilePlay;
using TileGameCore;
using UnityEngine;
using UnityEngine.UI;
using FishFramework;

public class UIRatesDlg : UIRatesDlgBase, IUIOpen<bool>
{
    public const string RateUsStateKey = "RateUsPromptState";
    public const int RateUsStateInitial = 0;
    public const int RateUsStateLater = 1;
    public const int RateUsStateCompleted = 2;

    private List<GameObject> starObjList = new();
    private List<GameObject> rateTextList = new();

    private int mStar = 0;
    private System.Action closeAction;

    protected override void OnOpen()
    {
        closeExtraAct = null;
        closeAction = null;
        OnOpenCore();
    }

    public void OnOpen(bool reopenSettingsAfterClose)
    {
        closeExtraAct = reopenSettingsAfterClose
            ? () =>
            {
                UISettings.MarkOpeningFromRateDialogReopen();
                UISettings.RefreshRateUsButtonVisibilityIfExists();
                UIController.ShowPage<UISettings>();
            }
            : null;
        closeAction = null;
        OnOpenCore();
    }

    public void SetCloseAction(System.Action action)
    {
        closeAction = action;
    }

    void OnOpenCore()
    {
        base.OnOpen();
        GameBAData.ExcuteCommentEvent(0);
        ShowRateStar();
    }

    static void PersistRateUsState(int state)
    {
        PlayerPrefs.SetInt(RateUsStateKey, state);
        PlayerPrefs.Save();
    }

    static void MarkRateUsCompleted()
    {
        PersistRateUsState(RateUsStateCompleted);
    }

    static void SyncSettingsRateUsButtonFromPrefs()
    {
        UISettings.RefreshRateUsButtonVisibilityIfExists();
    }

    protected override void OnDestroyed()
    {
        closeAction = null;
        base.OnDestroyed();
    }

    protected override void OnCreating()
    {
        base.OnCreating();
    }

    void ShowRateStar()
    {
        node1.SetActiveEx(true);
        node2.SetActiveEx(false);

        starObjList.Clear();
        starObjList.Add(star1.gameObject);
        starObjList.Add(star2.gameObject);
        starObjList.Add(star3.gameObject);
        starObjList.Add(star4.gameObject);
        starObjList.Add(star5.gameObject);

        rateTextList.Clear();
        rateTextList.Add(rateLb1.gameObject);
        rateTextList.Add(rateLb2.gameObject);
        rateTextList.Add(rateLb3.gameObject);
        rateTextList.Add(rateLb4.gameObject);
        rateTextList.Add(rateLb5.gameObject);

        SetStar(0);
    }

    void ShowRateText()
    {
        node1.SetActiveEx(false);
        node2.SetActiveEx(true);
        RefreshSubmitBtnState();
    }

    void RefreshSubmitBtnState()
    {
        submitBtn.interactable = inputField.text.Length > 0;
    }

    protected override void OnValueChanged(InputField inputField, string value)
    {
        if (inputField == this.inputField)
        {
            RefreshSubmitBtnState();
        }
    }

    protected override void OnClicked(Button button)
    {
        base.OnClicked(button);
        CheckBtnClick(button);

        if (button == submitBtn)
        {
            var text = inputField.text;
            if (text.Length > 0)
            {
                GameBAData.ExcuteCommentEvent(2, mStar);
                GPSDKMgr.Instance.UserFeedbackRate(text, mStar);
                MarkRateUsCompleted();
                SyncSettingsRateUsButtonFromPrefs();
                CloseRateDlg();
            }
            else
            {

            }
            //将文本推给SDK
        }
        else if (button == closeBtn || button == noBtn)
        {
            if (button == noBtn)
            {
                PersistRateUsState(RateUsStateLater);
            }

            HandleCloseButtonClicked();
        }
        else if (button == yesBtn)
        {
            if (mStar < 5)
            {
                MarkRateUsCompleted();
                SyncSettingsRateUsButtonFromPrefs();
                ShowRateText();
            }
            else
            {
                GameBAData.ExcuteCommentEvent(2, mStar);
                //提交评分
                GPSDKMgr.Instance.RateInGoogleStore();
                MarkRateUsCompleted();
                SyncSettingsRateUsButtonFromPrefs();
                CloseRateDlg();//且提交SDK
            }
        }
    }

    protected override bool OnEscButtonPressed()
    {
        HandleCloseButtonClicked();
        return true;
    }

    protected override void OnBackgroundClicked(UIPanelBgClickEventType bgClickEventType)
    {
        if (bgClickEventType == UIPanelBgClickEventType.CloseSelf)
        {
            HandleCloseButtonClicked();
            return;
        }

        base.OnBackgroundClicked(bgClickEventType);
    }

    void HandleCloseButtonClicked()
    {
        GameBAData.ExcuteCommentEvent(-1);
        CloseRateDlg();
    }

    void CloseRateDlg()
    {
        var action = closeAction;
        closeAction = null;
        CloseSelf(action);
    }

    void CheckBtnClick(Button btn)
    {
        var name = btn.name;
        if (name.Contains("starBtn"))
        {
            var idx = name.Substring(7, 1).ToInt();
            SetStar(idx);
            GameBAData.ExcuteCommentEvent(1, idx);
        }
    }

    void SetStar(int star)
    {
        mStar = star;
        for (int i = 0; i < 5; i++)
        {
            starObjList[i].SetActive(i < star);
            rateTextList[i].SetActive(i == star - 1);
        }

        yesBtn.interactable = star > 0;
    }
}
