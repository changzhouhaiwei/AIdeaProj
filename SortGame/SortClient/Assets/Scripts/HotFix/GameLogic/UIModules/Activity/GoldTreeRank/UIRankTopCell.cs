

using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class UIRankTopCell : UIRankTopCellBase, IRankCell
{
    Action<IRankCell> mBtnFunc;
    private RankData.OneUserData mUserData;
    protected override void OnOpen()
    {
        base.OnOpen();
    }

    //刷新数据
    public void UpdateData(RankData.OneUserData data)
    {
        mUserData = data;
        nameLb.text = data.playerName;
        scoreLb.text = data.score.ToString();
        headIcon.sprite = LoadAtlasSprite("UserInfo", "Avatar_" + data.pictureUrl);
    }


    protected override void OnClicked(Button button)
    {
        base.OnClicked(button);
        if (button == emptyButton)
        {
            mBtnFunc?.Invoke(this);
        }
    }

    public void SetBtnClickCb(Action<IRankCell> cellFunc)
    {
        mBtnFunc = cellFunc;
    }

    public GameObject GetAwardIconObj()
    {
        return awardIcon.gameObject;
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
        scoreLb.text = (mUserData.score - addNum).ToString();

        for (int i = 0; i < 6; i++)
        {
            int idd = i + 1;
            Transform t = animRt.Find("Collect_" + idd);
            t.SetActiveEx(idd == addNum);
        }

        Delay(1.5f, () =>
        {
            animRt.SetActiveEx(false);
            scoreLb.text = (mUserData.score).ToString();
            action?.Invoke();
        });

    }
    public void RefreshBgByMe()
    {

    }

    public void SetDefaultBg()
    {
        
    }
}
