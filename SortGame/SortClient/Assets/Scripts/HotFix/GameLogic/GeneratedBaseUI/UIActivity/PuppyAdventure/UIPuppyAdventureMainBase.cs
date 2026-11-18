using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class UIPuppyAdventureMainBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UIActivity/PuppyAdventure/UIPuppyAdventureMain.prefab";

	protected TextMeshProUGUI progressTex;
	protected TextMeshProUGUI playersTex;
	protected TextMeshProUGUI remainDesTex;
	protected TextMeshProUGUI timerTex;
	protected TextMeshProUGUI failTex;
	protected TButton infoBtn;
	protected TButton advanceBtn;
	protected TButton failBtn;
	protected TButton tapBtn;
	protected TButton closeBtn;
	protected RectTransform playerAvatar;
	protected RectTransform failRoot;
	protected RectTransform successRoot;
	protected RectTransform bgMid;
	protected RectTransform bgNear;
	protected RectTransform trailNodes;
	protected TextMeshProUGUI coinText;
    protected override void OnBindCompsAndEvents() 
    {
		progressTex = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
		playersTex = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		remainDesTex = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		timerTex = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		failTex = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(4, 0);
		infoBtn = viewBehaviour.GetComponentByIndexs<TButton>(5, 0);
		advanceBtn = viewBehaviour.GetComponentByIndexs<TButton>(6, 0);
		failBtn = viewBehaviour.GetComponentByIndexs<TButton>(7, 0);
		tapBtn = viewBehaviour.GetComponentByIndexs<TButton>(8, 0);
		failRoot = viewBehaviour.GetComponentByIndexs<RectTransform>(9, 0);
		successRoot = viewBehaviour.GetComponentByIndexs<RectTransform>(10, 0);
		trailNodes = viewBehaviour.GetComponentByIndexs<RectTransform>(11, 0);
		coinText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(13, 0);
		closeBtn = viewBehaviour.GetComponentByIndexs<TButton>(12, 0);
		playerAvatar = null;
		bgMid = null;
		bgNear = null;

		BindEvent(infoBtn);
		BindEvent(advanceBtn);
		BindEvent(failBtn);
		BindEvent(tapBtn);
		BindEvent(closeBtn);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(infoBtn);
		UnbindEvent(advanceBtn);
		UnbindEvent(failBtn);
		UnbindEvent(tapBtn);
		UnbindEvent(closeBtn);

		progressTex = null;
		playersTex = null;
		remainDesTex = null;
		timerTex = null;
		failTex = null;
		infoBtn = null;
		advanceBtn = null;
		failBtn = null;
		tapBtn = null;
		closeBtn = null;
		playerAvatar = null;
		failRoot = null;
		successRoot = null;
		bgMid = null;
		bgNear = null;
		trailNodes = null;
		coinText = null;
	}
}
