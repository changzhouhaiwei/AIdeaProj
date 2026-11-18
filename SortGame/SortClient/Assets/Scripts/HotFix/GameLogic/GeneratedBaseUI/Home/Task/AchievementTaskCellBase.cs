using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class AchievementTaskCellBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Task/AchievementTaskCell.prefab";
    
	protected TextMeshProUGUI descText;
	protected RectTransform awardBg;
	protected TButton awardButton;
	protected TButton jumpButton;
	protected Image alreadyImage;

    protected override void OnBindCompsAndEvents() 
    {
		descText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
		awardBg = viewBehaviour.GetComponentByIndexs<RectTransform>(1, 0);
		awardButton = viewBehaviour.GetComponentByIndexs<TButton>(2, 0);
		jumpButton = viewBehaviour.GetComponentByIndexs<TButton>(3, 0);
		alreadyImage = viewBehaviour.GetComponentByIndexs<Image>(4, 0);

		BindEvent(awardButton);
		BindEvent(jumpButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(awardButton);
		UnbindEvent(jumpButton);

		descText = null;
		awardBg = null;
		awardButton = null;
		jumpButton = null;
		alreadyImage = null;
	}
}