using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class UIRatesDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UISystemFunc/UIRatesDlg.prefab";
    
	protected TButton closeBtn;
	protected TButton yesBtn;
	protected TButton noBtn;
	protected RectTransform node1;
	protected RectTransform node2;
	protected TextMeshProUGUI rateLb1;
	protected TextMeshProUGUI rateLb2;
	protected TextMeshProUGUI rateLb3;
	protected TextMeshProUGUI rateLb4;
	protected TextMeshProUGUI rateLb5;
	protected Image star1;
	protected Image star2;
	protected Image star3;
	protected Image star4;
	protected Image star5;
	protected InputField inputField;
	protected TButton submitBtn;
	protected Button starBtn1;
	protected Button starBtn2;
	protected Button starBtn3;
	protected Button starBtn4;
	protected Button starBtn5;

    protected override void OnBindCompsAndEvents() 
    {
		closeBtn = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
		yesBtn = viewBehaviour.GetComponentByIndexs<TButton>(1, 0);
		noBtn = viewBehaviour.GetComponentByIndexs<TButton>(2, 0);
		node1 = viewBehaviour.GetComponentByIndexs<RectTransform>(3, 0);
		node2 = viewBehaviour.GetComponentByIndexs<RectTransform>(4, 0);
		rateLb1 = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(5, 0);
		rateLb2 = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(6, 0);
		rateLb3 = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(7, 0);
		rateLb4 = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(8, 0);
		rateLb5 = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(9, 0);
		star1 = viewBehaviour.GetComponentByIndexs<Image>(10, 0);
		star2 = viewBehaviour.GetComponentByIndexs<Image>(11, 0);
		star3 = viewBehaviour.GetComponentByIndexs<Image>(12, 0);
		star4 = viewBehaviour.GetComponentByIndexs<Image>(13, 0);
		star5 = viewBehaviour.GetComponentByIndexs<Image>(14, 0);
		inputField = viewBehaviour.GetComponentByIndexs<InputField>(15, 0);
		submitBtn = viewBehaviour.GetComponentByIndexs<TButton>(16, 0);
		starBtn1 = viewBehaviour.GetComponentByIndexs<Button>(17, 0);
		starBtn2 = viewBehaviour.GetComponentByIndexs<Button>(18, 0);
		starBtn3 = viewBehaviour.GetComponentByIndexs<Button>(19, 0);
		starBtn4 = viewBehaviour.GetComponentByIndexs<Button>(20, 0);
		starBtn5 = viewBehaviour.GetComponentByIndexs<Button>(21, 0);

		BindEvent(closeBtn);
		BindEvent(yesBtn);
		BindEvent(noBtn);
		BindEvent(inputField);
		BindEvent(submitBtn);
		BindEvent(starBtn1);
		BindEvent(starBtn2);
		BindEvent(starBtn3);
		BindEvent(starBtn4);
		BindEvent(starBtn5);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(closeBtn);
		UnbindEvent(yesBtn);
		UnbindEvent(noBtn);
		UnbindEvent(inputField);
		UnbindEvent(submitBtn);
		UnbindEvent(starBtn1);
		UnbindEvent(starBtn2);
		UnbindEvent(starBtn3);
		UnbindEvent(starBtn4);
		UnbindEvent(starBtn5);

		closeBtn = null;
		yesBtn = null;
		noBtn = null;
		node1 = null;
		node2 = null;
		rateLb1 = null;
		rateLb2 = null;
		rateLb3 = null;
		rateLb4 = null;
		rateLb5 = null;
		star1 = null;
		star2 = null;
		star3 = null;
		star4 = null;
		star5 = null;
		inputField = null;
		submitBtn = null;
		starBtn1 = null;
		starBtn2 = null;
		starBtn3 = null;
		starBtn4 = null;
		starBtn5 = null;
	}
}