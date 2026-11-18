using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class GiftPackCellBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Shop/GiftPack/GiftPackCell.prefab";
    
	protected TextMeshProUGUI name;
	protected Image timeObj;
	protected TextMeshProUGUI time;
	protected RectTransform reward;
	protected TButton btnReward;
	protected TextMeshProUGUI price;
	protected Image bG;
	protected TButton btnRight;
	protected TButton btnLeft;
	protected RectTransform switchObj;

    protected override void OnBindCompsAndEvents() 
    {
		name = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
		timeObj = viewBehaviour.GetComponentByIndexs<Image>(1, 0);
		time = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		reward = viewBehaviour.GetComponentByIndexs<RectTransform>(3, 0);
		btnReward = viewBehaviour.GetComponentByIndexs<TButton>(4, 0);
		price = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(5, 0);
		bG = viewBehaviour.GetComponentByIndexs<Image>(6, 0);
		btnRight = viewBehaviour.GetComponentByIndexs<TButton>(7, 0);
		btnLeft = viewBehaviour.GetComponentByIndexs<TButton>(8, 0);
		switchObj = viewBehaviour.GetComponentByIndexs<RectTransform>(9, 0);

		BindEvent(btnReward);
		BindEvent(btnRight);
		BindEvent(btnLeft);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(btnReward);
		UnbindEvent(btnRight);
		UnbindEvent(btnLeft);

		name = null;
		timeObj = null;
		time = null;
		reward = null;
		btnReward = null;
		price = null;
		bG = null;
		btnRight = null;
		btnLeft = null;
		switchObj = null;
	}
}