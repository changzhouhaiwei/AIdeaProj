using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class CircleDailyAwardRuleUIBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UIActivity/CircleAward/CircleDailyAwardRuleUI.prefab";
    
	protected TButton closeBtn;
	protected TextMeshProUGUI descLb;
	protected UIWidgetBehaviour cellTemplate;

    protected override void OnBindCompsAndEvents() 
    {
		closeBtn = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
		descLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		cellTemplate = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(2, 0);

		BindEvent(closeBtn);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(closeBtn);

		closeBtn = null;
		descLb = null;
		cellTemplate = null;
	}
}
