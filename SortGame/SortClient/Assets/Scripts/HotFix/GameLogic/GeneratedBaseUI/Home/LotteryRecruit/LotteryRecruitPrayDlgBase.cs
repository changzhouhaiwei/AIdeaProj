using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class LotteryRecruitPrayDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/LotteryRecruit/LotteryRecruitPrayDlg.prefab";
    
	protected UIWidgetBehaviour roleHeadObj;
	protected TextMeshProUGUI prayText;
	protected LoopGridView loopGridView;
	protected TButton sureButton;
	protected TButton closeButton;
	protected TToggle toggle_5;
	protected TToggle toggle_6;
	protected TToggle toggle_3;
	protected TToggle toggle_0;
	protected TToggle toggle_1;
	protected TToggle toggle_4;
	protected TToggle toggle_2;

    protected override void OnBindCompsAndEvents() 
    {
		roleHeadObj = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(0, 0);
		prayText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		loopGridView = viewBehaviour.GetComponentByIndexs<LoopGridView>(2, 0);
		sureButton = viewBehaviour.GetComponentByIndexs<TButton>(3, 0);
		closeButton = viewBehaviour.GetComponentByIndexs<TButton>(4, 0);
		toggle_5 = viewBehaviour.GetComponentByIndexs<TToggle>(5, 0);
		toggle_6 = viewBehaviour.GetComponentByIndexs<TToggle>(6, 0);
		toggle_3 = viewBehaviour.GetComponentByIndexs<TToggle>(7, 0);
		toggle_0 = viewBehaviour.GetComponentByIndexs<TToggle>(8, 0);
		toggle_1 = viewBehaviour.GetComponentByIndexs<TToggle>(9, 0);
		toggle_4 = viewBehaviour.GetComponentByIndexs<TToggle>(10, 0);
		toggle_2 = viewBehaviour.GetComponentByIndexs<TToggle>(11, 0);

		BindEvent(sureButton);
		BindEvent(closeButton);
		BindEvent(toggle_5);
		BindEvent(toggle_6);
		BindEvent(toggle_3);
		BindEvent(toggle_0);
		BindEvent(toggle_1);
		BindEvent(toggle_4);
		BindEvent(toggle_2);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(sureButton);
		UnbindEvent(closeButton);
		UnbindEvent(toggle_5);
		UnbindEvent(toggle_6);
		UnbindEvent(toggle_3);
		UnbindEvent(toggle_0);
		UnbindEvent(toggle_1);
		UnbindEvent(toggle_4);
		UnbindEvent(toggle_2);

		roleHeadObj = null;
		prayText = null;
		loopGridView = null;
		sureButton = null;
		closeButton = null;
		toggle_5 = null;
		toggle_6 = null;
		toggle_3 = null;
		toggle_0 = null;
		toggle_1 = null;
		toggle_4 = null;
		toggle_2 = null;
	}
}