using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class DailyCopyCellBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/BattleField/DailyCopy/DailyCopyCell.prefab";
    
	protected RectTransform awardBg;
	protected TButton challengeBtn;
	protected TButton clearButton;
	protected Image unOpened;
	protected Image index;
	protected TextMeshProUGUI powerText;
	protected Image bg_gold;
	protected Image bg_exp;
	protected Image bg_up;

    protected override void OnBindCompsAndEvents() 
    {
		awardBg = viewBehaviour.GetComponentByIndexs<RectTransform>(0, 0);
		challengeBtn = viewBehaviour.GetComponentByIndexs<TButton>(1, 0);
		clearButton = viewBehaviour.GetComponentByIndexs<TButton>(2, 0);
		unOpened = viewBehaviour.GetComponentByIndexs<Image>(3, 0);
		index = viewBehaviour.GetComponentByIndexs<Image>(4, 0);
		powerText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(5, 0);
		bg_gold = viewBehaviour.GetComponentByIndexs<Image>(6, 0);
		bg_exp = viewBehaviour.GetComponentByIndexs<Image>(7, 0);
		bg_up = viewBehaviour.GetComponentByIndexs<Image>(8, 0);

		BindEvent(challengeBtn);
		BindEvent(clearButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(challengeBtn);
		UnbindEvent(clearButton);

		awardBg = null;
		challengeBtn = null;
		clearButton = null;
		unOpened = null;
		index = null;
		powerText = null;
		bg_gold = null;
		bg_exp = null;
		bg_up = null;
	}
}