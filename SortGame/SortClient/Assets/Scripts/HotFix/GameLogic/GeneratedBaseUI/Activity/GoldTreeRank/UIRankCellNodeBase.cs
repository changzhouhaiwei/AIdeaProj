using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class UIRankCellNodeBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UIActivity/GoldTreeRank/UIRankCellNode.prefab";
    
	protected TextMeshProUGUI rankLb;
	protected RectTransform awardSp;
	protected TextMeshProUGUI nameTxt;
	protected TextMeshProUGUI scoreLb;
	protected Image headIcon;
	protected TButton emptyButton;
	protected Image cellBg1;
	protected Image cellBg2;
	protected RectTransform effFromNode;

    protected override void OnBindCompsAndEvents() 
    {
		rankLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
		awardSp = viewBehaviour.GetComponentByIndexs<RectTransform>(1, 0);
		nameTxt = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		scoreLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		headIcon = viewBehaviour.GetComponentByIndexs<Image>(4, 0);
		emptyButton = viewBehaviour.GetComponentByIndexs<TButton>(5, 0);
		cellBg1 = viewBehaviour.GetComponentByIndexs<Image>(6, 0);
		cellBg2 = viewBehaviour.GetComponentByIndexs<Image>(7, 0);
		effFromNode = viewBehaviour.GetComponentByIndexs<RectTransform>(8, 0);

		BindEvent(emptyButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(emptyButton);

		rankLb = null;
		awardSp = null;
		nameTxt = null;
		scoreLb = null;
		headIcon = null;
		emptyButton = null;
		cellBg1 = null;
		cellBg2 = null;
		effFromNode = null;
	}
}