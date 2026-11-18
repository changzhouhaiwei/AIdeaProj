using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class WarOrderExtraCellBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/WarOrder/WarOrderExtraCell.prefab";
    
	protected TextMeshProUGUI textLb;
	protected Image expBar;
	protected TButton rewardBtn;
	protected Image redDot;

    protected override void OnBindCompsAndEvents() 
    {
		textLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
		expBar = viewBehaviour.GetComponentByIndexs<Image>(1, 0);
		rewardBtn = viewBehaviour.GetComponentByIndexs<TButton>(2, 0);
		redDot = viewBehaviour.GetComponentByIndexs<Image>(3, 0);

		BindEvent(rewardBtn);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(rewardBtn);

		textLb = null;
		expBar = null;
		rewardBtn = null;
		redDot = null;
	}
}