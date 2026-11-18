using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class UseBoxItemCellBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/ItemBag/UseBoxItemCell.prefab";
    
	protected RectTransform itemBg;
	protected TextMeshProUGUI nameText;
	protected TextMeshProUGUI numText;
	protected TButton subtractButton;
	protected TButton plusButton;
	protected TextMeshProUGUI useText;

    protected override void OnBindCompsAndEvents() 
    {
		itemBg = viewBehaviour.GetComponentByIndexs<RectTransform>(0, 0);
		nameText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		numText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		subtractButton = viewBehaviour.GetComponentByIndexs<TButton>(3, 0);
		plusButton = viewBehaviour.GetComponentByIndexs<TButton>(4, 0);
		useText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(5, 0);

		BindEvent(subtractButton);
		BindEvent(plusButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(subtractButton);
		UnbindEvent(plusButton);

		itemBg = null;
		nameText = null;
		numText = null;
		subtractButton = null;
		plusButton = null;
		useText = null;
	}
}