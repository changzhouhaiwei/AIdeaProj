using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class UIDailyRewardGetBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UIActivity/DailyReward/UIDailyRewardGet.prefab";
    	protected TButton okBtn;	protected Image cellBg1;	protected Image cellBg2;	protected Image cellBg3;	protected Image cellBg4;	protected RectTransform line1;	protected RectTransform line2;	protected RectTransform cell1;	protected RectTransform cell2;	protected RectTransform cell3;	protected RectTransform cell4;
    protected override void OnBindCompsAndEvents() 
    {		okBtn = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);		cellBg1 = viewBehaviour.GetComponentByIndexs<Image>(1, 0);		cellBg2 = viewBehaviour.GetComponentByIndexs<Image>(2, 0);		cellBg3 = viewBehaviour.GetComponentByIndexs<Image>(3, 0);		cellBg4 = viewBehaviour.GetComponentByIndexs<Image>(4, 0);		line1 = viewBehaviour.GetComponentByIndexs<RectTransform>(5, 0);		line2 = viewBehaviour.GetComponentByIndexs<RectTransform>(6, 0);		cell1 = viewBehaviour.GetComponentByIndexs<RectTransform>(7, 0);		cell2 = viewBehaviour.GetComponentByIndexs<RectTransform>(8, 0);		cell3 = viewBehaviour.GetComponentByIndexs<RectTransform>(9, 0);		cell4 = viewBehaviour.GetComponentByIndexs<RectTransform>(10, 0);		BindEvent(okBtn);	}

    protected override void OnUnbindCompsAndEvents() 
    {		UnbindEvent(okBtn);		okBtn = null;		cellBg1 = null;		cellBg2 = null;		cellBg3 = null;		cellBg4 = null;		line1 = null;		line2 = null;		cell1 = null;		cell2 = null;		cell3 = null;		cell4 = null;	}
}