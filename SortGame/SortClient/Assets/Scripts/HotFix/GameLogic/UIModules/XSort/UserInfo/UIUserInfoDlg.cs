using System.Collections;
using System.Collections.Generic;
using cfg.Game;
using UnityEngine;
using UnityEngine.UI;
using FishFramework;

public class UIUserInfoDlg : UIUserInfoDlgBase
{
    List<UserHead> mChoseHeadList = new();
    List<UserFrame> mChoseFrameList = new();
    UserHead myHead;
    UserFrame myHeadFrame;
    int myHeadId = 1;
    int myFrameId;
    string myName;
    Coroutine mFrameRemainCoroutine;

    protected override void OnClicked(Button button)
    {
        base.OnClicked(button);
        if (button == closeButton)
        {
            CloseSelf();
        }
        else if (button == okBtn)
        {
            CheckSetUserInfo();
        }
        else if (button == copyIdBtn)
        {
            CopyId();
            XSortGameCore.AudioController.PlaySound(XSortGameCore.AudioController.AudioClips.buttonSoundSpread);
        }
    }

    protected override void OnValueChanged(Toggle toggle, bool value)
    {
        base.OnValueChanged(toggle, value);
        if (!value)
        {
            return;
        }

        if (toggle == avatarToggle)
        {
            XSortGameCore.AudioController.PlaySound(XSortGameCore.AudioController.AudioClips.buttonSoundSpread);
            SwitchToAvatarTab();
        }
        else if (toggle == frameToggle)
        {
            if (!IsAvatarFrameChangeEnabled())
            {
                return;
            }

            XSortGameCore.AudioController.PlaySound(XSortGameCore.AudioController.AudioClips.buttonSoundSpread);
            SwitchToFrameTab();
        }
    }

    void CopyId()
    {
        GUIUtility.systemCopyBuffer = iDText.text;
    }

    public void CheckSetUserInfo()
    {
        string dstName = nameInput.text;
        if (dstName.Length < 3)
        {
            XSortGameCore.AudioController.PlaySound(XSortGameCore.AudioController.AudioClips.buttonSound);
            AlertDlg.ShowDlg(I2LocalizeMgr.GetString("UI_EnterNamePanel_Tips1"));
            return;
        }

        if (dstName.Length > 12)
        {
            XSortGameCore.AudioController.PlaySound(XSortGameCore.AudioController.AudioClips.buttonSound);
            AlertDlg.ShowDlg(I2LocalizeMgr.GetString("UI_EnterNamePanel_Tips2"));
            return;
        }

        GPUserDataManager.Instance.UserData.userhead = myHeadId;
        GameEvent.Send((int)EventNameDef.UserHeadChange);

        if (IsAvatarFrameChangeEnabled()
            && (myFrameId <= 0 || GPUserDataManager.Instance.IsAvatarFrameUnlocked(myFrameId)))
        {
            GPUserDataManager.Instance.UserData.userframe = myFrameId;
            GameEvent.Send((int)EventNameDef.UserFrameChange);
        }

        GPUserDataManager.Instance.UserData.username = dstName;
        // CloseSelf 内已播放 buttonSound (ui_button_1)
        CloseSelf();
    }

    protected override void OnOpen()
    {
        base.OnOpen();
        AddEvent();
        GPUserDataManager.Instance.TryCleanupExpiredAvatarFrames();
        InitHeads();
        if (IsAvatarFrameChangeEnabled())
        {
            InitAvatarFrames();
        }

        ApplyFrameTabAvailability();
        InitName();
        StartRemainRefresh();
    }

    protected override void OnDestroying()
    {
        base.OnDestroying();
        RemoveEvent();
        StopRemainRefresh();

        if (nameInput != null)
        {
            nameInput.onValueChanged.RemoveListener(OnNameInputChanged);
        }
    }

    static bool IsAvatarFrameChangeEnabled()
    {
        // 头像框换框入口不再依赖七日连胜硬开关
        return true;
    }

    void ApplyFrameTabAvailability()
    {
        if (avatarToggle != null)
        {
            avatarToggle.SetIsOnWithoutNotify(true);
        }

        if (frameToggle != null)
        {
            frameToggle.SetIsOnWithoutNotify(false);
        }

        SwitchToAvatarTab();
    }

    void SwitchToAvatarTab()
    {
        headNode.gameObject.SetActiveEx(true);
        frameNode.gameObject.SetActiveEx(false);
    }

    void SwitchToFrameTab()
    {
        if (!IsAvatarFrameChangeEnabled())
        {
            SwitchToAvatarTab();
            return;
        }

        headNode.gameObject.SetActiveEx(false);
        frameNode.gameObject.SetActiveEx(true);
        RefreshFrameItems();
    }

    void InitName()
    {
        UpdateName();
        nameInput.onValueChanged.AddListener(OnNameInputChanged);
        iDText.text = GPUserDataManager.GPUserData.uid;
    }

    void UpdateName()
    {
        myName = GPUserDataManager.Instance.UserData.username;
        nameInput.text = myName;
    }

    void OnNameInputChanged(string value)
    {
        okBtn.interactable = true;
        if (value.Length > 12)
        {
            nameInput.text = value.Substring(0, 12);
        }
        else if (value.Length == 0)
        {
            okBtn.interactable = false;
        }
    }

    void InitHeads()
    {
        myHeadId = GPUserDataManager.Instance.UserData.userhead;
        myFrameId = GPUserDataManager.Instance.UserData.userframe;

        RectTransform pTran = headNode;
        for (int i = 1; i <= 12; i++)
        {
            var oneTran = pTran.GetChild(i - 1);
            var oneHead = CreateWidget<UserHead>("userhead" + i, oneTran, oneTran.GetComponent<UIWidgetBehaviour>());
            mChoseHeadList.Add(oneHead);
            oneHead.UpdateIcon(i);
            oneHead.SetBSelected(myHeadId == i);
            oneHead.OnClickCb = OnHeadClick;
        }

        myHead = CreateWidget<UserHead>("myuserhead", myHeadNode.GetComponent<UIWidgetBehaviour>());
        var myHeadFrameBehaviour = FindMyHeadFrameBehaviour();
        if (myHeadFrameBehaviour != null)
        {
            myHeadFrame = CreateWidget<UserFrame>("myheadframe", myHeadFrameBehaviour);
        }

        RefreshMyHead();
    }

    UIWidgetBehaviour FindMyHeadFrameBehaviour()
    {
        for (int i = 0; i < myHeadNode.childCount; i++)
        {
            var child = myHeadNode.GetChild(i);
            var widgetBehaviour = child.GetComponent<UIWidgetBehaviour>();
            if (widgetBehaviour != null)
            {
                return widgetBehaviour;
            }
        }

        return null;
    }

    void InitAvatarFrames()
    {
        var frameCfgs = ConfigHelper.Tables.TbAvatarFrameCfg.DataList;
        RectTransform pTran = frameNode;

        for (int i = 0; i < pTran.childCount; i++)
        {
            var oneTran = pTran.GetChild(i);
            if (i >= frameCfgs.Count)
            {
                oneTran.gameObject.SetActiveEx(false);
                continue;
            }

            oneTran.gameObject.SetActiveEx(true);
            var cfg = frameCfgs[i];
            var oneFrame = CreateWidget<UserFrame>("userframe" + cfg.Id, oneTran, oneTran.GetComponent<UIWidgetBehaviour>());
            mChoseFrameList.Add(oneFrame);
            oneFrame.OnClickCb = OnFrameClick;
            RefreshFrameItem(oneFrame, cfg);
        }
    }

    void RefreshMyHead()
    {
        myHead.UpdateIcon(myHeadId);
        myHead.UpdateFrame(0);

        if (myHeadFrame != null)
        {
            myHeadFrame.UpdatePreview(myFrameId);
        }
    }

    void RefreshFrameItems()
    {
        GPUserDataManager.Instance.TryCleanupExpiredAvatarFrames();
        var frameCfgs = ConfigHelper.Tables.TbAvatarFrameCfg.DataList;
        for (int i = 0; i < mChoseFrameList.Count && i < frameCfgs.Count; i++)
        {
            RefreshFrameItem(mChoseFrameList[i], frameCfgs[i]);
        }

        if (myFrameId > 0 && !GPUserDataManager.Instance.IsAvatarFrameUnlocked(myFrameId))
        {
            myFrameId = GPUserData.DefaultAvatarFrameId;
            RefreshMyHead();
        }
    }

    void RefreshFrameItem(UserFrame frameItem, AvatarFrameCfg cfg)
    {
        bool unlocked = GPUserDataManager.Instance.IsAvatarFrameUnlocked(cfg.Id);
        string remainText = GPUserDataManager.Instance.GetAvatarFrameRemainText(cfg.Id);
        frameItem.UpdateFrame(cfg, unlocked, myFrameId == cfg.Id, remainText);
    }

    void OnHeadClick(UserHead chosenHead)
    {
        XSortGameCore.AudioController.PlaySound(XSortGameCore.AudioController.AudioClips.buttonSoundSpread);

        foreach (var item in mChoseHeadList)
        {
            item.SetBSelected(item == chosenHead);
            if (item == chosenHead)
            {
                myHeadId = item.GetHeadId();
            }
        }

        RefreshMyHead();
    }

    void OnFrameClick(UserFrame chosenFrame)
    {
        if (!chosenFrame.IsUnlocked())
        {
            AlertDlg.ShowDlg(I2LocalizeMgr.GetString("UI_DailyStreakFrametext"));
            return;
        }

        XSortGameCore.AudioController.PlaySound(XSortGameCore.AudioController.AudioClips.buttonSoundSpread);

        foreach (var item in mChoseFrameList)
        {
            item.SetBSelected(item == chosenFrame);
            if (item == chosenFrame)
            {
                myFrameId = item.GetFrameId();
            }
        }

        RefreshMyHead();
    }

    void StartRemainRefresh()
    {
        StopRemainRefresh();
        mFrameRemainCoroutine = viewBehaviour.StartCoroutine(RemainRefreshLoop());
    }

    void StopRemainRefresh()
    {
        if (mFrameRemainCoroutine != null)
        {
            viewBehaviour.StopCoroutine(mFrameRemainCoroutine);
            mFrameRemainCoroutine = null;
        }
    }

    IEnumerator RemainRefreshLoop()
    {
        while (true)
        {
            GPUserDataManager.Instance.TryCleanupExpiredAvatarFrames();
            if (myHeadFrame != null)
            {
                myHeadFrame.RefreshRemainDisplay(myFrameId);
            }

            if (frameNode.gameObject.activeSelf)
            {
                RefreshFrameItems();
            }

            yield return new WaitForSeconds(GetFrameRemainRefreshInterval());
        }
    }

    float GetFrameRemainRefreshInterval()
    {
        if (HasShortRemain(myFrameId))
        {
            return 1f;
        }

        foreach (var frameItem in mChoseFrameList)
        {
            if (HasShortRemain(frameItem.GetFrameId()))
            {
                return 1f;
            }
        }

        return 60f;
    }

    static bool HasShortRemain(int frameId)
    {
        var remain = GPUserDataManager.Instance.GetAvatarFrameRemain(frameId);
        return remain.HasValue && remain.Value.TotalHours < 1;
    }

    void AddEvent()
    {
        GameEvent.AddEventListener<string>((int)EventNameDef.SensitiveCheckSuc, CbSdkSuc);
        GameEvent.AddEventListener<string>((int)EventNameDef.SensitiveCheckException, CbSdkException);
        GameEvent.AddEventListener<string>((int)EventNameDef.SensitiveCheckError, CbSdkError);
    }

    void RemoveEvent()
    {
        GameEvent.RemoveEventListener<string>((int)EventNameDef.SensitiveCheckSuc, CbSdkSuc);
        GameEvent.RemoveEventListener<string>((int)EventNameDef.SensitiveCheckException, CbSdkException);
        GameEvent.RemoveEventListener<string>((int)EventNameDef.SensitiveCheckError, CbSdkError);
    }

    void CbSdkSuc(string text)
    {
        GPUserDataManager.Instance.UserData.username = text;
        UpdateName();
        CloseSelf();
    }

    void CbSdkException(string text)
    {
        GPUserDataManager.Instance.UserData.username = text;
        UpdateName();
    }

    void CbSdkError(string text)
    {
        AlertDlg.ShowDlg(I2LocalizeMgr.GetString("UINetworkDisconnectedDes1"));
    }

    protected override bool OnEscButtonPressed()
    {
        CloseSelf();
        return true;
    }
}
