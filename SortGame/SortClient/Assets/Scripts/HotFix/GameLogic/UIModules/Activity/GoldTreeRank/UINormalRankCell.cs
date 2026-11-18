
using System;
using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class UINormalRankCell : UIRankCellNodeBase, IRankCell
{
    static readonly Color TextColorSelf = new Color32(0x0D, 0x74, 0x4C, 0xFF);
    static readonly Color TextColorOther = new Color32(0x9A, 0x3B, 0x38, 0xFF);
    static readonly Color TextColorRank2Name = new Color32(0x29, 0x54, 0x8E, 0xFF);

    Action<IRankCell> mBtnFunc;
    RankData.OneUserData mUserData;
    GameObject mNo1Bg;
    GameObject mNo2Bg;
    GameObject mNo3Bg;
    Image mNo1BgImage;
    Image mNo2BgImage;
    Image mNo3BgImage;
    Sprite mNo1DefaultSprite;
    Sprite mNo2DefaultSprite;
    Sprite mNo3DefaultSprite;
    Sprite mSelfTop3Sprite;
    bool mTop3BgCached;
    Transform mJpNo1;
    Transform mJpNo2;
    Transform mJpNo3;
    Vector3 mJpDefaultScale = Vector3.one;
    bool mJpDefaultCached;
    Tween mJpPopTween;
    int mAddTreeAnimSeq;

    protected override void OnOpen()
    {
        base.OnOpen();
        EnsureTop3BgNodes();
    }

    protected override void OnDestroyed()
    {
        mJpPopTween?.Kill();
        mJpPopTween = null;
        base.OnDestroyed();
    }

    //刷新数据
    public void UpdateData(RankData.OneUserData data)
    {
        nameTxt.text = data.playerName;
        scoreLb.text = data.score.ToString();
        rankLb.text = data.rank.ToString();
        headIcon.sprite = LoadAtlasSprite("UserInfo", "Avatar_" + data.pictureUrl);

        mUserData = data;
        awardSp.SetActiveEx(data.rank < 7);
        if (data.rank >= 1 && data.rank <= 3)
        {
            var giftSprite = LoadAtlasSprite("Rankboard", $"Rank_No{data.rank}_gift");
            if (giftSprite != null)
                awardSp.GetComponent<Image>().sprite = giftSprite;
        }
    }

    public string GetPlayerId()
        => mUserData?.playerId;

    // 升榜动画结束后：同一机器人只同步名次/宝箱，不重刷头像、昵称、分数。
    public void TrySyncRankAfterAnim(RankData.OneUserData data)
    {
        if (data == null || mUserData == null)
            return;

        if (!mUserData.playerId.Equals(data.playerId))
            return;

        if (mUserData.rank != data.rank)
            UpdateRankOnly((int)data.rank);
    }

    // 同一玩家仅同步变化字段；换玩家时整行重刷。返回 true 表示未整行重刷。
    public bool TryRefreshRankData(RankData.OneUserData data)
    {
        if (data == null)
            return true;

        if (mUserData != null && mUserData.playerId.Equals(data.playerId))
        {
            if (mUserData.rank != data.rank)
                UpdateRankOnly((int)data.rank);

            if (mUserData.score != data.score)
            {
                mUserData.score = data.score;
                scoreLb.text = data.score.ToString();
            }

            return true;
        }

        UpdateData(data);
        return false;
    }

    // 仅刷新名次与奖励图标（被超越行下移时用，避免整行重刷闪烁）。
    public void UpdateRankOnly(int rank)
    {
        if (mUserData != null)
            mUserData.rank = rank;
        rankLb.text = rank.ToString();

        awardSp.SetActiveEx(rank < 7);
        if (rank >= 1 && rank <= 3)
        {
            var giftSprite = LoadAtlasSprite("Rankboard", $"Rank_No{rank}_gift");
            if (giftSprite != null)
                awardSp.GetComponent<Image>().sprite = giftSprite;
        }

        bool isSelf = mUserData != null
            && mUserData.playerId.Equals(GPUserDataManager.Instance.GetUID());
        RefreshRankBg(rank, isSelf);
    }

    public void RefreshBgByMe()
    {
        if (mUserData == null)
            return;

        bool isSelf = mUserData.playerId.Equals(GPUserDataManager.Instance.GetUID());
        RefreshRankBg((int)mUserData.rank, isSelf);
    }

    public void SetDefaultBg()
    {
        int rank = mUserData != null ? (int)mUserData.rank : 0;
        RefreshRankBg(rank, false);
    }

    // 玩家自己的格子（MyRankyCellNode）强制显示 cellBg2；前三名显示 no.1~no.3。
    public void SetSelfBg()
    {
        int rank = mUserData != null ? (int)mUserData.rank : 0;
        RefreshRankBg(rank, true);
    }

    void EnsureTop3BgNodes()
    {
        if (mTop3BgCached)
            return;

        mTop3BgCached = true;
        var root = viewBehaviour != null ? viewBehaviour.transform : null;
        if (root == null)
            return;

        var imageNode = root.Find("Image") ?? root;
        mNo1Bg = imageNode.Find("no.1")?.gameObject;
        mNo2Bg = imageNode.Find("no.2")?.gameObject;
        mNo3Bg = imageNode.Find("no.3")?.gameObject;

        mNo1BgImage = mNo1Bg != null ? mNo1Bg.GetComponent<Image>() : null;
        mNo2BgImage = mNo2Bg != null ? mNo2Bg.GetComponent<Image>() : null;
        mNo3BgImage = mNo3Bg != null ? mNo3Bg.GetComponent<Image>() : null;

        if (mNo1BgImage != null)
            mNo1DefaultSprite = mNo1BgImage.sprite;
        if (mNo2BgImage != null)
            mNo2DefaultSprite = mNo2BgImage.sprite;
        if (mNo3BgImage != null)
            mNo3DefaultSprite = mNo3BgImage.sprite;

        CacheJpNode(mNo1Bg, ref mJpNo1);
        CacheJpNode(mNo2Bg, ref mJpNo2);
        CacheJpNode(mNo3Bg, ref mJpNo3);
    }

    void CacheJpNode(GameObject noNode, ref Transform jp)
    {
        jp = noNode != null ? noNode.transform.Find("jp") : null;
        if (jp == null || mJpDefaultCached)
            return;

        var scale = jp.localScale;
        if (scale.sqrMagnitude > 0.0001f)
        {
            mJpDefaultScale = scale;
            mJpDefaultCached = true;
        }
    }

    Transform GetActiveTop3Jp()
    {
        if (mNo1Bg != null && mNo1Bg.activeSelf)
            return mJpNo1;
        if (mNo2Bg != null && mNo2Bg.activeSelf)
            return mJpNo2;
        if (mNo3Bg != null && mNo3Bg.activeSelf)
            return mJpNo3;
        return null;
    }

    public void PlayTop3JpPop()
    {
        EnsureTop3BgNodes();
        var jp = GetActiveTop3Jp();
        if (jp == null)
            return;

        mJpPopTween?.Kill();
        jp.localScale = Vector3.zero;
        mJpPopTween = jp.DOScale(mJpDefaultScale, 0.1f)
            .SetEase(Ease.OutQuad)
            .SetLink(jp.gameObject, LinkBehaviour.KillOnDestroy);
    }

    Sprite GetSelfTop3Sprite()
    {
        if (mSelfTop3Sprite != null)
            return mSelfTop3Sprite;

        if (cellBg2 != null && cellBg2.sprite != null)
            mSelfTop3Sprite = cellBg2.sprite;
        else
            mSelfTop3Sprite = LoadAtlasSprite("Rankboard", "Rank_zj_dk");

        return mSelfTop3Sprite;
    }

    void RefreshTop3BgSprites(int rank, bool isSelf)
    {
        RefreshTop3NodeSprite(mNo1BgImage, mNo1DefaultSprite, rank, 1, isSelf);
        RefreshTop3NodeSprite(mNo2BgImage, mNo2DefaultSprite, rank, 2, isSelf);
        RefreshTop3NodeSprite(mNo3BgImage, mNo3DefaultSprite, rank, 3, isSelf);
    }

    void RefreshTop3NodeSprite(Image image, Sprite defaultSprite, int rank, int nodeRank, bool isSelf)
    {
        if (image == null)
            return;

        if (rank == nodeRank && isSelf)
        {
            var selfSprite = GetSelfTop3Sprite();
            if (selfSprite != null)
            {
                image.sprite = selfSprite;
                if (cellBg2 != null)
                    image.type = cellBg2.type;
            }
            return;
        }

        if (defaultSprite != null)
        {
            image.sprite = defaultSprite;
            image.type = Image.Type.Simple;
        }
    }

    GameObject GetTop3Bg(int rank)
    {
        switch (rank)
        {
            case 1: return mNo1Bg;
            case 2: return mNo2Bg;
            case 3: return mNo3Bg;
            default: return null;
        }
    }

    void RefreshRankBg(int rank, bool isSelf)
    {
        EnsureTop3BgNodes();

        mNo1Bg?.SetActiveEx(false);
        mNo2Bg?.SetActiveEx(false);
        mNo3Bg?.SetActiveEx(false);

        if (rank >= 1 && rank <= 3)
        {
            cellBg1?.SetActiveEx(false);
            cellBg2?.SetActiveEx(false);
            RefreshTop3BgSprites(rank, isSelf);
            GetTop3Bg(rank)?.SetActiveEx(true);
        }
        else
        {
            RefreshTop3BgSprites(0, false);
            if (isSelf)
            {
                cellBg1?.SetActiveEx(false);
                cellBg2?.SetActiveEx(true);
            }
            else
            {
                cellBg1?.SetActiveEx(true);
                cellBg2?.SetActiveEx(false);
            }
        }

        ApplyTextColors(rank, isSelf);
    }

    void ApplyTextColors(int rank, bool isSelf)
    {
        var color = isSelf ? TextColorSelf : TextColorOther;
        var rank2Color = rank == 2 ? TextColorRank2Name : color;
        rankLb.color = color;
        nameTxt.color = rank2Color;
        scoreLb.color = rank2Color;
    }


    public void SetBtnClickCb(Action<IRankCell> cellFunc)
    {
        mBtnFunc = cellFunc;
    }

    protected override void OnClicked(Button button)
    {
        if (button == emptyButton)
        {
            mBtnFunc?.Invoke(this);
        }
    }

    public GameObject GetAwardIconObj()
    {
        return awardSp.gameObject;
    }

    public cfg.Game.GoldTreeRankAwardCfg GetRankAwardData()
    {
        return GameData.GoldTreeActData.GetAwardCfgByRank((int)mUserData.rank);
    }

    public long GetRank()
    {
        return mUserData.rank;
    }

    public RectTransform GetRectTransform()
    {
        return GetOwnerRectTransform();
    }

    public void PlayAddTreeAnim(RectTransform animRt, int addNum, Action action)
    {
        animRt.SetParent(effFromNode);
        animRt.InitLocalTransform();
        animRt.SetActiveEx(true);

        RankTreeCollectAnimHelper.PlayGoldNumAnim(animRt, addNum);

        addNum = math.min(6, addNum);

        for (int i = 0; i < 6; i++)
        {
            int idd = i + 1;
            Transform t = animRt.Find("Collect_" + idd);
            t.SetActiveEx(idd == addNum);
        }

        int seq = ++mAddTreeAnimSeq;
        Delay(2.0f, () =>
        {
            if (seq != mAddTreeAnimSeq)
                return;

            animRt.SetActiveEx(false);
            action?.Invoke();
        });
    }

    public void CancelAddTreeAnim()
    {
        mAddTreeAnimSeq++;
    }

    public void CancelTop3JpPop()
    {
        mJpPopTween?.Kill();
        mJpPopTween = null;
        if (!mTop3BgCached)
            return;

        if (mJpNo1 != null)
            mJpNo1.localScale = mJpDefaultScale;
        if (mJpNo2 != null)
            mJpNo2.localScale = mJpDefaultScale;
        if (mJpNo3 != null)
            mJpNo3.localScale = mJpDefaultScale;
    }
}