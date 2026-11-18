using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class LotteryRecruitRotaryUIBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/LotteryRecruit/LotteryRecruitRotaryUI.prefab";
    
	protected RectTransform rotaryCell_1;
	protected RectTransform image_1;
	protected TextMeshProUGUI selectText_1;
	protected TButton ruleButton_1;
	protected TButton rotaryButton_1;
	protected TextMeshProUGUI normalText_1;
	protected RectTransform rotaryCell_2;
	protected RectTransform image_2;
	protected TextMeshProUGUI selectText_2;
	protected TButton ruleButton_2;
	protected TButton rotaryButton_2;
	protected TextMeshProUGUI normalText_2;
	protected RectTransform rotaryCell_3;
	protected RectTransform image_3;
	protected TextMeshProUGUI selectText_3;
	protected TButton ruleButton_3;
	protected TButton rotaryButton_3;
	protected TextMeshProUGUI normalText_3;

    protected override void OnBindCompsAndEvents() 
    {
		rotaryCell_1 = viewBehaviour.GetComponentByIndexs<RectTransform>(0, 0);
		image_1 = viewBehaviour.GetComponentByIndexs<RectTransform>(1, 0);
		selectText_1 = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		ruleButton_1 = viewBehaviour.GetComponentByIndexs<TButton>(3, 0);
		rotaryButton_1 = viewBehaviour.GetComponentByIndexs<TButton>(4, 0);
		normalText_1 = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(5, 0);
		rotaryCell_2 = viewBehaviour.GetComponentByIndexs<RectTransform>(6, 0);
		image_2 = viewBehaviour.GetComponentByIndexs<RectTransform>(7, 0);
		selectText_2 = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(8, 0);
		ruleButton_2 = viewBehaviour.GetComponentByIndexs<TButton>(9, 0);
		rotaryButton_2 = viewBehaviour.GetComponentByIndexs<TButton>(10, 0);
		normalText_2 = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(11, 0);
		rotaryCell_3 = viewBehaviour.GetComponentByIndexs<RectTransform>(12, 0);
		image_3 = viewBehaviour.GetComponentByIndexs<RectTransform>(13, 0);
		selectText_3 = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(14, 0);
		ruleButton_3 = viewBehaviour.GetComponentByIndexs<TButton>(15, 0);
		rotaryButton_3 = viewBehaviour.GetComponentByIndexs<TButton>(16, 0);
		normalText_3 = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(17, 0);

		BindEvent(ruleButton_1);
		BindEvent(rotaryButton_1);
		BindEvent(ruleButton_2);
		BindEvent(rotaryButton_2);
		BindEvent(ruleButton_3);
		BindEvent(rotaryButton_3);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(ruleButton_1);
		UnbindEvent(rotaryButton_1);
		UnbindEvent(ruleButton_2);
		UnbindEvent(rotaryButton_2);
		UnbindEvent(ruleButton_3);
		UnbindEvent(rotaryButton_3);

		rotaryCell_1 = null;
		image_1 = null;
		selectText_1 = null;
		ruleButton_1 = null;
		rotaryButton_1 = null;
		normalText_1 = null;
		rotaryCell_2 = null;
		image_2 = null;
		selectText_2 = null;
		ruleButton_2 = null;
		rotaryButton_2 = null;
		normalText_2 = null;
		rotaryCell_3 = null;
		image_3 = null;
		selectText_3 = null;
		ruleButton_3 = null;
		rotaryButton_3 = null;
		normalText_3 = null;
	}
}