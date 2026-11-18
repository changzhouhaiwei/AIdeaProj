
using System;
using UnityEngine;

public interface IRankCell
{
    public void UpdateData(RankData.OneUserData data);
    public void SetBtnClickCb(Action<IRankCell> cellFunc);
    public GameObject GetAwardIconObj();
    public cfg.Game.GoldTreeRankAwardCfg GetRankAwardData();
    public long GetRank();
    public RectTransform GetRectTransform();
    public void PlayAddTreeAnim(RectTransform animRt, int addNum, Action action);
    public void RefreshBgByMe();
    public void SetDefaultBg();
}