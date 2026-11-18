using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class CricleDailyAwardUIBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UIActivity/CircleAward/CricleDailyAwardUI.prefab";
    
	protected TButton ruleBtn;
	protected TButton closeBtn;
	protected TButton watchBtn;

    protected override void OnBindCompsAndEvents() 
    {
		ruleBtn = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
		closeBtn = viewBehaviour.GetComponentByIndexs<TButton>(1, 0);
		watchBtn = viewBehaviour.GetComponentByIndexs<TButton>(2, 0);

		BindEvent(ruleBtn);
		BindEvent(closeBtn);
		BindEvent(watchBtn);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(ruleBtn);
		UnbindEvent(closeBtn);
		UnbindEvent(watchBtn);

		ruleBtn = null;
		closeBtn = null;
		watchBtn = null;
	}
}
