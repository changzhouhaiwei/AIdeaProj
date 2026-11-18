using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class GrowthFundCellBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Activity/GrowthFund/GrowthFundCell.prefab";
    
	protected Image reach;
	protected TextMeshProUGUI lv;
	protected RectTransform sldier;
	protected Image itemHead1;
	protected Image itemHead2;
	protected Image itemHead3;
	protected TButton btnReward1;
	protected TButton btnReward2;
	protected TButton btnReward3;

    protected override void OnBindCompsAndEvents() 
    {
		reach = viewBehaviour.GetComponentByIndexs<Image>(0, 0);
		lv = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		sldier = viewBehaviour.GetComponentByIndexs<RectTransform>(2, 0);
		itemHead1 = viewBehaviour.GetComponentByIndexs<Image>(3, 0);
		itemHead2 = viewBehaviour.GetComponentByIndexs<Image>(4, 0);
		itemHead3 = viewBehaviour.GetComponentByIndexs<Image>(5, 0);
		btnReward1 = viewBehaviour.GetComponentByIndexs<TButton>(6, 0);
		btnReward2 = viewBehaviour.GetComponentByIndexs<TButton>(7, 0);
		btnReward3 = viewBehaviour.GetComponentByIndexs<TButton>(8, 0);

		BindEvent(btnReward1);
		BindEvent(btnReward2);
		BindEvent(btnReward3);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(btnReward1);
		UnbindEvent(btnReward2);
		UnbindEvent(btnReward3);

		reach = null;
		lv = null;
		sldier = null;
		itemHead1 = null;
		itemHead2 = null;
		itemHead3 = null;
		btnReward1 = null;
		btnReward2 = null;
		btnReward3 = null;
	}
}