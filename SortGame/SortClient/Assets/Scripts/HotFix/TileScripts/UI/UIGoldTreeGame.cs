using DG.Tweening;
using FishFramework;
using FishTilePlay;
using TMPro;
using TileGameCore;
using UnityEngine;

public class UIGoldTreeGame : MonoBehaviour
{
    const float ShownX = 135f;
    const float HiddenX = -74f;
    const float SlideDuration = 0.5f;
    const float HideDelay = 1.2f;
    const float SpecialDockAnchoredY = -255f;

    [SerializeField]
    TextMeshProUGUI goldTreeProcLb;
    int mMaxCnt = -1;
    int mCurCnt = -1;
    int mPendingFlyCnt;
    RectTransform mRt;
    RectTransform iconRect;
    float restAnchoredY;
    bool restAnchoredYCaptured;
    DG.Tweening.Tween mSlideTween;
    DG.Tweening.Tween mHideDelayTween;

    public void UpdateGoldTreePocLb()
    {
        if (goldTreeProcLb == null)
        {
            Debug.LogError("[GoldTree] goldTreeProcLb is not wired on UIGoldTreeGame.");
            return;
        }

        goldTreeProcLb.text = mCurCnt + "/" + mMaxCnt;
    }

    //初始化棋子
    public void InitGoldTreeProc(bool bFromSave)
    {
        SyncProcFromLevelData();
        UpdateGoldTreePocLb();
    }

    void Awake()
    {
        mRt = transform as RectTransform;
        if (goldTreeProcLb != null)
            goldTreeProcLb.raycastTarget = false;
        SnapHidden();
    }

    void OnEnable()
    {
        AddEvent();
        // LoadLevelAnimFinish 可能在 Start 订阅之前已发送；进局时若关卡已加载则立刻对齐进度文案。
        // GoldTreeActData is null when GameScene opens via Level Editor Test Level (no GameData.RegisterData).
        if (GameData.GoldTreeActData != null
            && GameData.GoldTreeActData.GetBActOpen()
            && GameData.BFromStartScene
            && LevelDirector.IsLevelLoaded)
        {
            InitGoldTreeProc(false);
        }
    }

    void OnDisable()
    {
        RemoveEvent();
        KillAnims();
    }

    public void OpenHelp()
    {
        if (AudioController.AudioClips != null)
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);

        if (GameModule.Setting.GmMode)
        {
            GameDirector.levelController.OnMatchCompleted(true);
            return;
        }
        UIGameFunc.OpenGoldTreeHelp();
    }

    public void AddEvent()
    {
        RemoveEvent();

        GameEvent.AddEventListener((int)EventNameDef.LoadLevelAnimFinish, CbLevelAnimFinish);
        GameEvent.AddEventListener((int)EventNameDef.LoadSaveLevelAnimFinish, CbSaveLevelAnimFinish);
        GameEvent.AddEventListener((int)EventNameDef.GoldTreeCollected, CbGoldTreeCollectAnimOver);
    }

    void RemoveEvent()
    {
        GameEvent.RemoveEventListener((int)EventNameDef.LoadLevelAnimFinish, CbLevelAnimFinish);
        GameEvent.RemoveEventListener((int)EventNameDef.LoadSaveLevelAnimFinish, CbSaveLevelAnimFinish);
        GameEvent.RemoveEventListener((int)EventNameDef.GoldTreeCollected, CbGoldTreeCollectAnimOver);
    }

    void CbLevelAnimFinish()
    {
        if (GameData.GoldTreeActData != null && GameData.GoldTreeActData.GetBActOpen())
            InitGoldTreeProc(false);
        HideAndReset();
    }

    void CbSaveLevelAnimFinish()
    {
        if (GameData.GoldTreeActData != null && GameData.GoldTreeActData.GetBActOpen())
            InitGoldTreeProc(true);
        HideAndReset();
    }

    void CbGoldTreeCollectAnimOver() //收集动作结束
    {
        if (GameData.GoldTreeActData == null || !GameData.GoldTreeActData.GetBActOpen())
            return;

        var goldTreeData = LevelDirector.GoldenTreeData;
        mMaxCnt = goldTreeData.MaxGoldenTreeCnt;
        if (mCurCnt < goldTreeData.CurrentGoldenTreeScore)
            mCurCnt++;

        if (goldTreeData.CurrentGoldenTreeCnt >= mMaxCnt && mMaxCnt > 0)
            LevelDirector.Dock.ShowColletAllGoldTreeEff();

        UpdateGoldTreePocLb();
        if (mPendingFlyCnt > 0)
            mPendingFlyCnt--;
        if (mPendingFlyCnt <= 0)
        {
            mPendingFlyCnt = 0;
            ScheduleHide();
        }
    }

    void SyncProcFromLevelData()
    {
        var goldTreeData = LevelDirector.GoldenTreeData;
        mMaxCnt = goldTreeData.MaxGoldenTreeCnt;
        mCurCnt = goldTreeData.CurrentGoldenTreeScore;
    }

    public void ShowDefaultPer()
    {
        if (gameObject.activeInHierarchy)
        {
            SyncProcFromLevelData();
            UpdateGoldTreePocLb();
        }
    }

    public void ShowForCollect(int flyCount)
    {
        if (flyCount < 1)
            flyCount = 1;
        mPendingFlyCnt += flyCount;
        KillHideDelay();
        SlideTo(ShownX);
        UpdateGoldTreePocLb();
    }

    public void HideAndReset()
    {
        mPendingFlyCnt = 0;
        SnapHidden();
    }

    /// <summary>
    /// 可乐箱 / 裂变棋 Dock 下移时，把节点抬到 <see cref="SpecialDockAnchoredY"/>。只改 Y。
    /// </summary>
    public void SetRaisedForSpecialDock(bool raised)
    {
        if (mRt == null)
            mRt = transform as RectTransform;
        if (mRt == null)
            return;

        if (!restAnchoredYCaptured)
        {
            restAnchoredY = mRt.anchoredPosition.y;
            restAnchoredYCaptured = true;
        }

        float y = raised ? SpecialDockAnchoredY : restAnchoredY;
        if (Mathf.Abs(mRt.anchoredPosition.y - y) < 0.01f)
            return;

        Vector2 pos = mRt.anchoredPosition;
        pos.y = y;
        mRt.anchoredPosition = pos;
    }

    /// <summary>只改 Y，让金币图标矩形中心和收集条中心在同一水平线上。</summary>
    public void AlignCenterToWorldY(float worldY)
    {
        if (mRt == null)
            mRt = transform as RectTransform;
        if (mRt == null)
            return;

        if (!restAnchoredYCaptured)
        {
            restAnchoredY = mRt.anchoredPosition.y;
            restAnchoredYCaptured = true;
        }

        RectTransform icon = GoldTreeIconRect();
        float delta = worldY - BoxCoverHUD.RectCenterWorldY(icon);
        if (Mathf.Abs(delta) < 0.01f)
            return;

        Vector3 pos = mRt.position;
        pos.y += delta;
        mRt.position = pos;
    }

    RectTransform GoldTreeIconRect()
    {
        if (iconRect != null)
            return iconRect;

        Transform icon = transform.Find("Image/goldTreeIcon");
        iconRect = icon as RectTransform;
        return iconRect != null ? iconRect : mRt;
    }

    public Vector3 GetShownIconWorldPos(Transform icon)
    {
        if (icon == null)
            return Vector3.zero;
        if (mRt == null)
            return icon.position;

        float dx = ShownX - mRt.anchoredPosition.x;
        if (Mathf.Abs(dx) < 0.5f)
            return icon.position;
        return icon.position + mRt.TransformVector(new Vector3(dx, 0f, 0f));
    }

    void ScheduleHide()
    {
        KillHideDelay();
        mHideDelayTween = DOVirtual.DelayedCall(HideDelay, () =>
        {
            mHideDelayTween = null;
            if (mPendingFlyCnt > 0)
                return;
            SlideTo(HiddenX);
        }).SetLink(gameObject);
    }

    void SlideTo(float x)
    {
        if (mRt == null)
            return;

        KillSlide();
        if (Mathf.Abs(mRt.anchoredPosition.x - x) < 0.5f)
        {
            mRt.anchoredPosition = new Vector2(x, mRt.anchoredPosition.y);
            return;
        }

        mSlideTween = mRt.DOAnchorPosX(x, SlideDuration)
            .SetEase(Ease.OutCubic)
            .SetLink(gameObject)
            .OnComplete(() => mSlideTween = null);
    }

    void SnapHidden()
    {
        KillAnims();
        if (mRt != null)
            mRt.anchoredPosition = new Vector2(HiddenX, mRt.anchoredPosition.y);
    }

    void KillAnims()
    {
        KillSlide();
        KillHideDelay();
    }

    void KillSlide()
    {
        mSlideTween?.Kill();
        mSlideTween = null;
    }

    void KillHideDelay()
    {
        mHideDelayTween?.Kill();
        mHideDelayTween = null;
    }
}
