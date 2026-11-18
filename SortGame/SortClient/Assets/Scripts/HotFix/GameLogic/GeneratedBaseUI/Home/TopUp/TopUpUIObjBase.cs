using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class TopUpUIObjBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/TopUp/TopUpUIObj.prefab";
    
	protected TextMeshProUGUI time;
	protected LoopListView2 loopListView;
	protected TToggle toggle1;
	protected TToggle toggle2;

    protected override void OnBindCompsAndEvents() 
    {
		time = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
		loopListView = viewBehaviour.GetComponentByIndexs<LoopListView2>(1, 0);
		toggle1 = viewBehaviour.GetComponentByIndexs<TToggle>(2, 0);
		toggle2 = viewBehaviour.GetComponentByIndexs<TToggle>(3, 0);

		BindEvent(toggle1);
		BindEvent(toggle2);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(toggle1);
		UnbindEvent(toggle2);

		time = null;
		loopListView = null;
		toggle1 = null;
		toggle2 = null;
	}
}