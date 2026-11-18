using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class ItemDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/ItemBag/ItemDlg.prefab";
    
	protected Image itemBg;
	protected UIWidgetBehaviour itemHeadObj;
	protected TextMeshProUGUI nameText;
	protected TextMeshProUGUI numText;
	protected TextMeshProUGUI typeText;
	protected TextMeshProUGUI descText;
	protected TButton sureButton;
	protected RectTransform layout2;

    protected override void OnBindCompsAndEvents() 
    {
		itemBg = viewBehaviour.GetComponentByIndexs<Image>(0, 0);
		itemHeadObj = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(1, 0);
		nameText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		numText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		typeText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(4, 0);
		descText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(5, 0);
		sureButton = viewBehaviour.GetComponentByIndexs<TButton>(6, 0);
		layout2 = viewBehaviour.GetComponentByIndexs<RectTransform>(7, 0);

		BindEvent(sureButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(sureButton);

		itemBg = null;
		itemHeadObj = null;
		nameText = null;
		numText = null;
		typeText = null;
		descText = null;
		sureButton = null;
		layout2 = null;
	}
}