using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class BattlePassTaskUIBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Task/BattlePassTaskUI.prefab";
    
	protected LoopListView2 loopListView;
	protected TButton awardButton;

    protected override void OnBindCompsAndEvents() 
    {
		loopListView = viewBehaviour.GetComponentByIndexs<LoopListView2>(0, 0);
		awardButton = viewBehaviour.GetComponentByIndexs<TButton>(1, 0);

		BindEvent(awardButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(awardButton);

		loopListView = null;
		awardButton = null;
	}
}