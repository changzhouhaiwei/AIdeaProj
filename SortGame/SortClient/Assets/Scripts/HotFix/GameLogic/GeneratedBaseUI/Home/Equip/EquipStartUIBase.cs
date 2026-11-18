using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class EquipStartUIBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Equip/EquipStartUI.prefab";
    
	protected UIWidgetBehaviour fromEquipHeadObj;
	protected Image arrowImage;
	protected UIWidgetBehaviour toEquipHeadObj;
	protected UIWidgetBehaviour equipUpStarDesc_4;
	protected UIWidgetBehaviour equipUpStarDesc_3;
	protected UIWidgetBehaviour equipUpStarDesc_5;
	protected UIWidgetBehaviour equipUpStarDesc_2;
	protected UIWidgetBehaviour equipUpStarDesc_1;
	protected TButton starButton;
	protected Image subTitleBg;
	protected TextMeshProUGUI subTitleText;
	protected LoopGridView loopGridView;
	protected Image maxImage;

    protected override void OnBindCompsAndEvents() 
    {
		fromEquipHeadObj = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(0, 0);
		arrowImage = viewBehaviour.GetComponentByIndexs<Image>(1, 0);
		toEquipHeadObj = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(2, 0);
		equipUpStarDesc_4 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(3, 0);
		equipUpStarDesc_3 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(4, 0);
		equipUpStarDesc_5 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(5, 0);
		equipUpStarDesc_2 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(6, 0);
		equipUpStarDesc_1 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(7, 0);
		starButton = viewBehaviour.GetComponentByIndexs<TButton>(8, 0);
		subTitleBg = viewBehaviour.GetComponentByIndexs<Image>(9, 0);
		subTitleText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(10, 0);
		loopGridView = viewBehaviour.GetComponentByIndexs<LoopGridView>(11, 0);
		maxImage = viewBehaviour.GetComponentByIndexs<Image>(12, 0);

		BindEvent(starButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(starButton);

		fromEquipHeadObj = null;
		arrowImage = null;
		toEquipHeadObj = null;
		equipUpStarDesc_4 = null;
		equipUpStarDesc_3 = null;
		equipUpStarDesc_5 = null;
		equipUpStarDesc_2 = null;
		equipUpStarDesc_1 = null;
		starButton = null;
		subTitleBg = null;
		subTitleText = null;
		loopGridView = null;
		maxImage = null;
	}
}