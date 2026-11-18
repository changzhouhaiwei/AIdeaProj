using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;

public class UserAssetsUIBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/UserAssetsUI.prefab";
    
	protected RectTransform contentBg;
	protected UIWidgetBehaviour assetNode_1;
	protected UIWidgetBehaviour assetNode_2;
	protected UIWidgetBehaviour assetNode_3;

    protected override void OnBindCompsAndEvents() 
    {
		contentBg = viewBehaviour.GetComponentByIndexs<RectTransform>(0, 0);
		assetNode_1 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(1, 0);
		assetNode_2 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(2, 0);
		assetNode_3 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(3, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		contentBg = null;
		assetNode_1 = null;
		assetNode_2 = null;
		assetNode_3 = null;
	}
}