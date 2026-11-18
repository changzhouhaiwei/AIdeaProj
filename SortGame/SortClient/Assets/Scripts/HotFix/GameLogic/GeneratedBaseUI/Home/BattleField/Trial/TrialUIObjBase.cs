using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class TrialUIObjBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/BattleField/Trial/TrialUIObj.prefab";
    
	protected LoopListView2 scrollView;
	protected TButton rankBtn;
	protected TButton clearBtn;
	protected TButton blessBtn;
	protected TButton formationBtn;
	protected TButton closeButton;

    protected override void OnBindCompsAndEvents() 
    {
		scrollView = viewBehaviour.GetComponentByIndexs<LoopListView2>(0, 0);
		rankBtn = viewBehaviour.GetComponentByIndexs<TButton>(1, 0);
		clearBtn = viewBehaviour.GetComponentByIndexs<TButton>(2, 0);
		blessBtn = viewBehaviour.GetComponentByIndexs<TButton>(3, 0);
		formationBtn = viewBehaviour.GetComponentByIndexs<TButton>(4, 0);
		closeButton = viewBehaviour.GetComponentByIndexs<TButton>(5, 0);

		BindEvent(rankBtn);
		BindEvent(clearBtn);
		BindEvent(blessBtn);
		BindEvent(formationBtn);
		BindEvent(closeButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(rankBtn);
		UnbindEvent(clearBtn);
		UnbindEvent(blessBtn);
		UnbindEvent(formationBtn);
		UnbindEvent(closeButton);

		scrollView = null;
		rankBtn = null;
		clearBtn = null;
		blessBtn = null;
		formationBtn = null;
		closeButton = null;
	}
}