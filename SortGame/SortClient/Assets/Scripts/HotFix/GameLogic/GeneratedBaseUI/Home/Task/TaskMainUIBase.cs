using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class TaskMainUIBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Task/TaskMainUI.prefab";
    
	protected TToggle toggle1;
	protected RedDotUI redDotUI_1;
	protected TToggle toggle2;
	protected RedDotUI redDotUI_2;
	protected TToggle toggle3;
	protected RedDotUI redDotUI_3;
	protected TToggle toggle4;
	protected RedDotUI redDotUI_4;
	protected UIWidgetBehaviour dailyTaskUIObj;
	protected UIWidgetBehaviour achievementTaskUIObj;
	protected UIWidgetBehaviour battlePassTaskUIObj;
	protected UIWidgetBehaviour postTaskUIObj;
	protected TButton closeButton;

    protected override void OnBindCompsAndEvents() 
    {
		toggle1 = viewBehaviour.GetComponentByIndexs<TToggle>(0, 0);
		redDotUI_1 = viewBehaviour.GetComponentByIndexs<RedDotUI>(1, 0);
		toggle2 = viewBehaviour.GetComponentByIndexs<TToggle>(2, 0);
		redDotUI_2 = viewBehaviour.GetComponentByIndexs<RedDotUI>(3, 0);
		toggle3 = viewBehaviour.GetComponentByIndexs<TToggle>(4, 0);
		redDotUI_3 = viewBehaviour.GetComponentByIndexs<RedDotUI>(5, 0);
		toggle4 = viewBehaviour.GetComponentByIndexs<TToggle>(6, 0);
		redDotUI_4 = viewBehaviour.GetComponentByIndexs<RedDotUI>(7, 0);
		dailyTaskUIObj = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(8, 0);
		achievementTaskUIObj = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(9, 0);
		battlePassTaskUIObj = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(10, 0);
		postTaskUIObj = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(11, 0);
		closeButton = viewBehaviour.GetComponentByIndexs<TButton>(12, 0);

		BindEvent(toggle1);
		BindEvent(toggle2);
		BindEvent(toggle3);
		BindEvent(toggle4);
		BindEvent(closeButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(toggle1);
		UnbindEvent(toggle2);
		UnbindEvent(toggle3);
		UnbindEvent(toggle4);
		UnbindEvent(closeButton);

		toggle1 = null;
		redDotUI_1 = null;
		toggle2 = null;
		redDotUI_2 = null;
		toggle3 = null;
		redDotUI_3 = null;
		toggle4 = null;
		redDotUI_4 = null;
		dailyTaskUIObj = null;
		achievementTaskUIObj = null;
		battlePassTaskUIObj = null;
		postTaskUIObj = null;
		closeButton = null;
	}
}