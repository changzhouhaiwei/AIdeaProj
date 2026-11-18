using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class BattleFieldUIBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/BattleField/BattleFieldUI.prefab";
    
	protected UIWidgetBehaviour dailyCopyBtn;
	protected UIWidgetBehaviour dailyCopyBtn1;
	protected UIWidgetBehaviour dailyCopyBtn2;
	protected UIWidgetBehaviour dailyCopyBtn3;
	protected RectTransform content;

    protected override void OnBindCompsAndEvents() 
    {
		dailyCopyBtn = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(0, 0);
		dailyCopyBtn1 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(1, 0);
		dailyCopyBtn2 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(2, 0);
		dailyCopyBtn3 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(3, 0);
		content = viewBehaviour.GetComponentByIndexs<RectTransform>(4, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		dailyCopyBtn = null;
		dailyCopyBtn1 = null;
		dailyCopyBtn2 = null;
		dailyCopyBtn3 = null;
		content = null;
	}
}