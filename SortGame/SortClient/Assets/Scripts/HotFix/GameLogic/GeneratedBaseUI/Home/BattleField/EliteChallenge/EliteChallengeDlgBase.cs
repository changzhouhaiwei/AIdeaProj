using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class EliteChallengeDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/BattleField/EliteChallenge/EliteChallengeDlg.prefab";
    
	protected TextMeshProUGUI titleText;
	protected TextMeshProUGUI starText1;
	protected Image star1;
	protected TextMeshProUGUI starText2;
	protected TextMeshProUGUI starText3;
	protected Image star2;
	protected Image star3;
	protected TextMeshProUGUI text;
	protected TextMeshProUGUI number;
	protected Button numberBtn;
	protected TButton awardButton;
	protected RectTransform content;

    protected override void OnBindCompsAndEvents() 
    {
		titleText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
		starText1 = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		star1 = viewBehaviour.GetComponentByIndexs<Image>(2, 0);
		starText2 = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		starText3 = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(4, 0);
		star2 = viewBehaviour.GetComponentByIndexs<Image>(5, 0);
		star3 = viewBehaviour.GetComponentByIndexs<Image>(6, 0);
		text = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(7, 0);
		number = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(8, 0);
		numberBtn = viewBehaviour.GetComponentByIndexs<Button>(9, 0);
		awardButton = viewBehaviour.GetComponentByIndexs<TButton>(10, 0);
		content = viewBehaviour.GetComponentByIndexs<RectTransform>(11, 0);

		BindEvent(numberBtn);
		BindEvent(awardButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(numberBtn);
		UnbindEvent(awardButton);

		titleText = null;
		starText1 = null;
		star1 = null;
		starText2 = null;
		starText3 = null;
		star2 = null;
		star3 = null;
		text = null;
		number = null;
		numberBtn = null;
		awardButton = null;
		content = null;
	}
}