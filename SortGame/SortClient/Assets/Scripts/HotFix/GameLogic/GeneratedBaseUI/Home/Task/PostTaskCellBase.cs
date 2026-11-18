using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class PostTaskCellBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Task/PostTaskCell.prefab";
    
	protected Image bgImage;
	protected Image xsImage;
	protected RectTransform descBg;
	protected TextMeshProUGUI descText;
	protected Image barBg;
	protected Image barImage;
	protected TextMeshProUGUI timeText2;
	protected Image timeImage;
	protected TextMeshProUGUI timeText1;
	protected RectTransform awardBg;
	protected TButton awardButton;
	protected TButton dispatchButton;
	protected RectTransform alreadyImage;
	protected RectTransform dispatchIngImage;

    protected override void OnBindCompsAndEvents() 
    {
		bgImage = viewBehaviour.GetComponentByIndexs<Image>(0, 0);
		xsImage = viewBehaviour.GetComponentByIndexs<Image>(1, 0);
		descBg = viewBehaviour.GetComponentByIndexs<RectTransform>(2, 0);
		descText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		barBg = viewBehaviour.GetComponentByIndexs<Image>(4, 0);
		barImage = viewBehaviour.GetComponentByIndexs<Image>(5, 0);
		timeText2 = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(6, 0);
		timeImage = viewBehaviour.GetComponentByIndexs<Image>(7, 0);
		timeText1 = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(8, 0);
		awardBg = viewBehaviour.GetComponentByIndexs<RectTransform>(9, 0);
		awardButton = viewBehaviour.GetComponentByIndexs<TButton>(10, 0);
		dispatchButton = viewBehaviour.GetComponentByIndexs<TButton>(11, 0);
		alreadyImage = viewBehaviour.GetComponentByIndexs<RectTransform>(12, 0);
		dispatchIngImage = viewBehaviour.GetComponentByIndexs<RectTransform>(13, 0);

		BindEvent(awardButton);
		BindEvent(dispatchButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(awardButton);
		UnbindEvent(dispatchButton);

		bgImage = null;
		xsImage = null;
		descBg = null;
		descText = null;
		barBg = null;
		barImage = null;
		timeText2 = null;
		timeImage = null;
		timeText1 = null;
		awardBg = null;
		awardButton = null;
		dispatchButton = null;
		alreadyImage = null;
		dispatchIngImage = null;
	}
}