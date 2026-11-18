using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class ChoseEquipCellBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Equip/ChoseEquipCell.prefab";
    
	protected RectTransform equipHeadBg;
	protected TextMeshProUGUI nameText;
	protected TextMeshProUGUI numText;
	protected TButton equipButton;
	protected TButton dischargeButton;

    protected override void OnBindCompsAndEvents() 
    {
		equipHeadBg = viewBehaviour.GetComponentByIndexs<RectTransform>(0, 0);
		nameText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		numText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		equipButton = viewBehaviour.GetComponentByIndexs<TButton>(3, 0);
		dischargeButton = viewBehaviour.GetComponentByIndexs<TButton>(4, 0);

		BindEvent(equipButton);
		BindEvent(dischargeButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(equipButton);
		UnbindEvent(dischargeButton);

		equipHeadBg = null;
		nameText = null;
		numText = null;
		equipButton = null;
		dischargeButton = null;
	}
}