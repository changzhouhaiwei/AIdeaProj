using Spine.Unity;
using TMPro;
using UnityEngine;
using FishFramework;

public class WorldBossUIObjBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/BattleField/WorldBoss/WorldBossUIObj.prefab";

    protected TextMeshProUGUI time;
    protected TButton rankBtn;
    protected TButton rewardBtn;
    protected TButton exchangeBtn;
    protected TextMeshProUGUI allDamage;
    protected TextMeshProUGUI allRank;
    protected RectTransform roleList;
    protected TextMeshProUGUI challengeNum;
    protected TButton formationBtn;
    protected TButton challengeBtn;
    protected SkeletonGraphic boss;
    protected TButton closeButton;

    protected override void OnBindCompsAndEvents()
    {
        time = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
        rankBtn = viewBehaviour.GetComponentByIndexs<TButton>(1, 0);
        rewardBtn = viewBehaviour.GetComponentByIndexs<TButton>(2, 0);
        exchangeBtn = viewBehaviour.GetComponentByIndexs<TButton>(3, 0);
        allDamage = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(4, 0);
        allRank = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(5, 0);
        roleList = viewBehaviour.GetComponentByIndexs<RectTransform>(6, 0);
        challengeNum = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(7, 0);
        formationBtn = viewBehaviour.GetComponentByIndexs<TButton>(8, 0);
        challengeBtn = viewBehaviour.GetComponentByIndexs<TButton>(9, 0);
        boss = viewBehaviour.GetComponentByIndexs<SkeletonGraphic>(10, 0);
        closeButton = viewBehaviour.GetComponentByIndexs<TButton>(11, 0);

        BindEvent(rankBtn);
        BindEvent(rewardBtn);
        BindEvent(exchangeBtn);
        BindEvent(formationBtn);
        BindEvent(challengeBtn);
        BindEvent(closeButton);
    }

    protected override void OnUnbindCompsAndEvents()
    {
        UnbindEvent(rankBtn);
        UnbindEvent(rewardBtn);
        UnbindEvent(exchangeBtn);
        UnbindEvent(formationBtn);
        UnbindEvent(challengeBtn);
        UnbindEvent(closeButton);

        time = null;
        rankBtn = null;
        rewardBtn = null;
        exchangeBtn = null;
        allDamage = null;
        allRank = null;
        roleList = null;
        challengeNum = null;
        formationBtn = null;
        challengeBtn = null;
        boss = null;
        closeButton = null;
    }
}