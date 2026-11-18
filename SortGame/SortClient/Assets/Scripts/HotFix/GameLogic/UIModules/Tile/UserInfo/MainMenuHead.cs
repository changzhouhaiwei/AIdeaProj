using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using FishFramework;

public class MainMenuHead : GPViewBase
{
    [SerializeField] Image headIcon;
    Image mFrameIcon;
    Transform mFrameRoot;
    Coroutine mRemainCoroutine;
    private bool _subscribedUserHeadChange;
    private bool _subscribedUserFrameChange;
    private bool _subscribedDailyVictoryWinRecorded;

    void Start()
    {
        if (!GameData.BFromStartScene) return;

        FindFrameNodes();
        _subscribedUserHeadChange = true;
        _subscribedUserFrameChange = true;
        _subscribedDailyVictoryWinRecorded = true;
        RefreshHeadDisplay();
        StartRemainRefresh();
        GameEvent.AddEventListener((int)EventNameDef.UserHeadChange, CbUserHeadChange);
        GameEvent.AddEventListener((int)EventNameDef.UserFrameChange, CbUserFrameChange);
        GameEvent.AddEventListener((int)EventNameDef.DailyVictoryWinRecorded, CbDailyVictoryWinRecorded);
    }

    protected override void OnDestroy()
    {
        StopRemainRefresh();
        if (_subscribedUserHeadChange)
            GameEvent.RemoveEventListener((int)EventNameDef.UserHeadChange, CbUserHeadChange);
        if (_subscribedUserFrameChange)
            GameEvent.RemoveEventListener((int)EventNameDef.UserFrameChange, CbUserFrameChange);
        if (_subscribedDailyVictoryWinRecorded)
            GameEvent.RemoveEventListener((int)EventNameDef.DailyVictoryWinRecorded, CbDailyVictoryWinRecorded);
        base.OnDestroy();
    }

    void CbUserHeadChange()
    {
        RefreshHeadDisplay();
    }

    void CbUserFrameChange()
    {
        RefreshHeadDisplay();
    }

    void CbDailyVictoryWinRecorded()
    {
        if (mFrameRoot == null)
        {
            FindFrameNodes();
        }

        RefreshFrameDecor();
    }

    public void RefreshDisplay()
    {
        if (!GameData.BFromStartScene)
        {
            return;
        }

        FindFrameNodes();
        RefreshHeadDisplay();
    }

    void RefreshHeadDisplay()
    {
        int headId = GPUserDataManager.Instance.UserData.userhead;
        headIcon.sprite = LoadAtlasSprite("UserInfo", "Avatar_" + headId);

        if (mFrameIcon == null || mFrameRoot == null)
        {
            FindFrameNodes();
        }

        int frameId = GPUserDataManager.Instance.UserData.userframe;
        ApplyFrameImage(frameId);
        RefreshFrameDecor();
    }

    void ApplyFrameImage(int frameId)
    {
        if (mFrameIcon == null)
        {
            return;
        }

        if (frameId <= 0)
        {
            mFrameIcon.gameObject.SetActiveEx(false);
            return;
        }

        var cfg = ConfigHelper.Tables.TbAvatarFrameCfg.GetOrDefault(frameId);
        if (cfg == null)
        {
            mFrameIcon.gameObject.SetActiveEx(false);
            return;
        }

        mFrameIcon.sprite = LoadAtlasSprite("UserInfo", cfg.Pic);
        mFrameIcon.gameObject.SetActiveEx(true);
    }

    void RefreshFrameDecor()
    {
        if (mFrameRoot == null)
        {
            return;
        }

        int frameId = GPUserDataManager.Instance.UserData.userframe;
        AvatarFrameUiHelper.RefreshTotalWinCount(mFrameRoot, frameId);
    }

    void FindFrameNodes()
    {
        var frameRoot = transform.Find("UserFrameNode");
        if (frameRoot == null)
        {
            return;
        }

        mFrameRoot = frameRoot;

        var iconTran = frameRoot.Find("icon");
        if (iconTran != null)
        {
            iconTran.gameObject.SetActiveEx(false);
        }

        var frameTran = frameRoot.Find("frame");
        mFrameIcon = frameTran != null ? frameTran.GetComponent<Image>() : null;
    }

    void StartRemainRefresh()
    {
        StopRemainRefresh();
        mRemainCoroutine = StartCoroutine(RemainRefreshLoop());
    }

    void StopRemainRefresh()
    {
        if (mRemainCoroutine != null)
        {
            StopCoroutine(mRemainCoroutine);
            mRemainCoroutine = null;
        }
    }

    IEnumerator RemainRefreshLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(60f);
            GPUserDataManager.Instance.TryCleanupExpiredAvatarFrames();
            RefreshFrameDecor();
        }
    }
}
