using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class BoxItemDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/ItemBag/BoxItemDlg.prefab";
    
	protected Image itemBg;
	protected UIWidgetBehaviour itemHeadObj;
	protected TextMeshProUGUI nameText;
	protected TextMeshProUGUI typeText;
	protected TextMeshProUGUI numText;
	protected TextMeshProUGUI descText;
	protected LoopGridView loopGridView;
	protected TButton useButton;
	protected TButton closeButton;

    protected override void OnBindCompsAndEvents() 
    {
		itemBg = viewBehaviour.GetComponentByIndexs<Image>(0, 0);
		itemHeadObj = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(1, 0);
		nameText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		typeText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		numText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(4, 0);
		descText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(5, 0);
		loopGridView = viewBehaviour.GetComponentByIndexs<LoopGridView>(6, 0);
		useButton = viewBehaviour.GetComponentByIndexs<TButton>(7, 0);
		closeButton = viewBehaviour.GetComponentByIndexs<TButton>(8, 0);

		BindEvent(useButton);
		BindEvent(closeButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(useButton);
		UnbindEvent(closeButton);

		itemBg = null;
		itemHeadObj = null;
		nameText = null;
		typeText = null;
		numText = null;
		descText = null;
		loopGridView = null;
		useButton = null;
		closeButton = null;
	}
}