using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class ItemBagDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/ItemBag/ItemBagDlg.prefab";
    
	protected TToggle toggle2;
	protected TToggle toggle1;
	protected TToggle toggle3;
	protected TToggle toggle4;
	protected LoopGridView loopGridView;
	protected TextMeshProUGUI capacityText;
	protected TButton closeButton;

    protected override void OnBindCompsAndEvents() 
    {
		toggle2 = viewBehaviour.GetComponentByIndexs<TToggle>(0, 0);
		toggle1 = viewBehaviour.GetComponentByIndexs<TToggle>(1, 0);
		toggle3 = viewBehaviour.GetComponentByIndexs<TToggle>(2, 0);
		toggle4 = viewBehaviour.GetComponentByIndexs<TToggle>(3, 0);
		loopGridView = viewBehaviour.GetComponentByIndexs<LoopGridView>(4, 0);
		capacityText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(5, 0);
		closeButton = viewBehaviour.GetComponentByIndexs<TButton>(6, 0);

		BindEvent(toggle2);
		BindEvent(toggle1);
		BindEvent(toggle3);
		BindEvent(toggle4);
		BindEvent(closeButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(toggle2);
		UnbindEvent(toggle1);
		UnbindEvent(toggle3);
		UnbindEvent(toggle4);
		UnbindEvent(closeButton);

		toggle2 = null;
		toggle1 = null;
		toggle3 = null;
		toggle4 = null;
		loopGridView = null;
		capacityText = null;
		closeButton = null;
	}
}