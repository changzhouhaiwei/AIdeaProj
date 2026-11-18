using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class CardCallUIObjBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Activity/CardCall/CardCallUIObj.prefab";
    
	protected UIWidgetBehaviour cardCallCell;
	protected TextMeshProUGUI time;
	protected LoopListView2 loopListView;

    protected override void OnBindCompsAndEvents() 
    {
		cardCallCell = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(0, 0);
		time = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		loopListView = viewBehaviour.GetComponentByIndexs<LoopListView2>(2, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		cardCallCell = null;
		time = null;
		loopListView = null;
	}
}