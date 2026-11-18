using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class RoleMainUIBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Role/RoleMainUI.prefab";
    
	protected SkeletonGraphic roleSpine;
	protected TextMeshProUGUI qualityText;
	protected TextMeshProUGUI nameText;
	protected Image starImage_1;
	protected Image starImage_2;
	protected Image starImage_3;
	protected Image starImage_4;
	protected Image starImage_5;
	protected TButton sxButton_TButton;
	protected Image sxButton_Image;
	protected TButton professionButton_TButton;
	protected Image professionButton_Image;
	protected TextMeshProUGUI fightPowerText;
	protected RectTransform equipBg;
	protected UIWidgetBehaviour equipHead_1;
	protected UIWidgetBehaviour equipHead_2;
	protected UIWidgetBehaviour equipHead_3;
	protected UIWidgetBehaviour equipHead_4;
	protected UIWidgetBehaviour equipEmptyHead_1;
	protected UIWidgetBehaviour equipEmptyHead_2;
	protected UIWidgetBehaviour equipEmptyHead_3;
	protected UIWidgetBehaviour equipEmptyHead_4;
	protected TButton rightButton;
	protected TButton leftButton;
	protected TextMeshProUGUI professionPosDesc;
	protected TButton rebirthButton;
	protected TButton rollbackButton;
	protected TButton backButton;
	protected TToggle toggle_1;
	protected TToggle toggle_2;
	protected TToggle toggle_3;
	protected UIWidgetBehaviour roleInfoUIObj;
	protected UIWidgetBehaviour roleTalentUIObj;
	protected UIWidgetBehaviour roleStarUIObj;

    protected override void OnBindCompsAndEvents() 
    {
		roleSpine = viewBehaviour.GetComponentByIndexs<SkeletonGraphic>(0, 0);
		qualityText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		nameText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		starImage_1 = viewBehaviour.GetComponentByIndexs<Image>(3, 0);
		starImage_2 = viewBehaviour.GetComponentByIndexs<Image>(4, 0);
		starImage_3 = viewBehaviour.GetComponentByIndexs<Image>(5, 0);
		starImage_4 = viewBehaviour.GetComponentByIndexs<Image>(6, 0);
		starImage_5 = viewBehaviour.GetComponentByIndexs<Image>(7, 0);
		sxButton_TButton = viewBehaviour.GetComponentByIndexs<TButton>(8, 0);
		sxButton_Image = viewBehaviour.GetComponentByIndexs<Image>(8, 1);
		professionButton_TButton = viewBehaviour.GetComponentByIndexs<TButton>(9, 0);
		professionButton_Image = viewBehaviour.GetComponentByIndexs<Image>(9, 1);
		fightPowerText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(10, 0);
		equipBg = viewBehaviour.GetComponentByIndexs<RectTransform>(11, 0);
		equipHead_1 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(12, 0);
		equipHead_2 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(13, 0);
		equipHead_3 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(14, 0);
		equipHead_4 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(15, 0);
		equipEmptyHead_1 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(16, 0);
		equipEmptyHead_2 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(17, 0);
		equipEmptyHead_3 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(18, 0);
		equipEmptyHead_4 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(19, 0);
		rightButton = viewBehaviour.GetComponentByIndexs<TButton>(20, 0);
		leftButton = viewBehaviour.GetComponentByIndexs<TButton>(21, 0);
		professionPosDesc = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(22, 0);
		rebirthButton = viewBehaviour.GetComponentByIndexs<TButton>(23, 0);
		rollbackButton = viewBehaviour.GetComponentByIndexs<TButton>(24, 0);
		backButton = viewBehaviour.GetComponentByIndexs<TButton>(25, 0);
		toggle_1 = viewBehaviour.GetComponentByIndexs<TToggle>(26, 0);
		toggle_2 = viewBehaviour.GetComponentByIndexs<TToggle>(27, 0);
		toggle_3 = viewBehaviour.GetComponentByIndexs<TToggle>(28, 0);
		roleInfoUIObj = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(29, 0);
		roleTalentUIObj = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(30, 0);
		roleStarUIObj = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(31, 0);

		BindEvent(sxButton_TButton);
		BindEvent(professionButton_TButton);
		BindEvent(rightButton);
		BindEvent(leftButton);
		BindEvent(rebirthButton);
		BindEvent(rollbackButton);
		BindEvent(backButton);
		BindEvent(toggle_1);
		BindEvent(toggle_2);
		BindEvent(toggle_3);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(sxButton_TButton);
		UnbindEvent(professionButton_TButton);
		UnbindEvent(rightButton);
		UnbindEvent(leftButton);
		UnbindEvent(rebirthButton);
		UnbindEvent(rollbackButton);
		UnbindEvent(backButton);
		UnbindEvent(toggle_1);
		UnbindEvent(toggle_2);
		UnbindEvent(toggle_3);

		roleSpine = null;
		qualityText = null;
		nameText = null;
		starImage_1 = null;
		starImage_2 = null;
		starImage_3 = null;
		starImage_4 = null;
		starImage_5 = null;
		sxButton_TButton = null;
		sxButton_Image = null;
		professionButton_TButton = null;
		professionButton_Image = null;
		fightPowerText = null;
		equipBg = null;
		equipHead_1 = null;
		equipHead_2 = null;
		equipHead_3 = null;
		equipHead_4 = null;
		equipEmptyHead_1 = null;
		equipEmptyHead_2 = null;
		equipEmptyHead_3 = null;
		equipEmptyHead_4 = null;
		rightButton = null;
		leftButton = null;
		professionPosDesc = null;
		rebirthButton = null;
		rollbackButton = null;
		backButton = null;
		toggle_1 = null;
		toggle_2 = null;
		toggle_3 = null;
		roleInfoUIObj = null;
		roleTalentUIObj = null;
		roleStarUIObj = null;
	}
}