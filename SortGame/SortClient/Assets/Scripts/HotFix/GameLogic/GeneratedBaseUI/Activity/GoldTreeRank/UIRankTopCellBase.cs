using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class UIRankTopCellBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UIActivity/GoldTreeRank/UIRankTopCell.prefab";
    	protected TextMeshProUGUI nameLb;	protected TextMeshProUGUI scoreLb;	protected Image awardIcon;	protected Image headIcon;	protected TButton emptyButton;	protected RectTransform effFromNode;
    protected override void OnBindCompsAndEvents() 
    {		nameLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);		scoreLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);		awardIcon = viewBehaviour.GetComponentByIndexs<Image>(2, 0);		headIcon = viewBehaviour.GetComponentByIndexs<Image>(3, 0);		emptyButton = viewBehaviour.GetComponentByIndexs<TButton>(4, 0);		effFromNode = viewBehaviour.GetComponentByIndexs<RectTransform>(5, 0);		BindEvent(emptyButton);	}

    protected override void OnUnbindCompsAndEvents() 
    {		UnbindEvent(emptyButton);		nameLb = null;		scoreLb = null;		awardIcon = null;		headIcon = null;		emptyButton = null;		effFromNode = null;	}
}