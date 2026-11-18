using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class WarOrderCellBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/WarOrder/WarOrderCell.prefab";
    
	protected TextMeshProUGUI lvLb;
	protected Image bar1U;
	protected Image bar2U;
	protected Image line2;
	protected Image line1;
	protected RectTransform itemHead1;
	protected Image itemHead2;
	protected Image itemHead3;
	protected TButton rAwardBtn;
	protected TButton lAwardBtn;
	protected Image bar1D;
	protected Image bar2D;

    protected override void OnBindCompsAndEvents() 
    {
		lvLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
		bar1U = viewBehaviour.GetComponentByIndexs<Image>(1, 0);
		bar2U = viewBehaviour.GetComponentByIndexs<Image>(2, 0);
		line2 = viewBehaviour.GetComponentByIndexs<Image>(3, 0);
		line1 = viewBehaviour.GetComponentByIndexs<Image>(4, 0);
		itemHead1 = viewBehaviour.GetComponentByIndexs<RectTransform>(5, 0);
		itemHead2 = viewBehaviour.GetComponentByIndexs<Image>(6, 0);
		itemHead3 = viewBehaviour.GetComponentByIndexs<Image>(7, 0);
		rAwardBtn = viewBehaviour.GetComponentByIndexs<TButton>(8, 0);
		lAwardBtn = viewBehaviour.GetComponentByIndexs<TButton>(9, 0);
		bar1D = viewBehaviour.GetComponentByIndexs<Image>(10, 0);
		bar2D = viewBehaviour.GetComponentByIndexs<Image>(11, 0);

		BindEvent(rAwardBtn);
		BindEvent(lAwardBtn);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(rAwardBtn);
		UnbindEvent(lAwardBtn);

		lvLb = null;
		bar1U = null;
		bar2U = null;
		line2 = null;
		line1 = null;
		itemHead1 = null;
		itemHead2 = null;
		itemHead3 = null;
		rAwardBtn = null;
		lAwardBtn = null;
		bar1D = null;
		bar2D = null;
	}
}