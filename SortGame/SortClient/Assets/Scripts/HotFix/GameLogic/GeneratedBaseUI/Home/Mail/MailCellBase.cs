using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class MailCellBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Mail/MailCell.prefab";
    
	protected Image icon2;
	protected Image bg2;
	protected TextMeshProUGUI contentLb;
	protected Image bg1;
	protected TButton tButton;
	protected TextMeshProUGUI timeLb;
	protected TextMeshProUGUI titleLb;
	protected Image icon1;

    protected override void OnBindCompsAndEvents() 
    {
		icon2 = viewBehaviour.GetComponentByIndexs<Image>(0, 0);
		bg2 = viewBehaviour.GetComponentByIndexs<Image>(1, 0);
		contentLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		bg1 = viewBehaviour.GetComponentByIndexs<Image>(3, 0);
		tButton = viewBehaviour.GetComponentByIndexs<TButton>(4, 0);
		timeLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(5, 0);
		titleLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(6, 0);
		icon1 = viewBehaviour.GetComponentByIndexs<Image>(7, 0);

		BindEvent(tButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(tButton);

		icon2 = null;
		bg2 = null;
		contentLb = null;
		bg1 = null;
		tButton = null;
		timeLb = null;
		titleLb = null;
		icon1 = null;
	}
}