using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class AnnouncementDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Announcement/AnnouncementDlg.prefab";
    
	protected TButton closeButton;
	protected LoopListView2 loopListView;
	protected TextMeshProUGUI titleText;
	protected TextMeshProUGUI conditonText;
	protected Image announcementImage;
	protected RectTransform image;
	protected RectTransform nullPage;
	protected RectTransform rT_RTTextContent;
	protected ScrollRect scrollView;

    protected override void OnBindCompsAndEvents() 
    {
		closeButton = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
		loopListView = viewBehaviour.GetComponentByIndexs<LoopListView2>(1, 0);
		titleText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		conditonText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		announcementImage = viewBehaviour.GetComponentByIndexs<Image>(4, 0);
		image = viewBehaviour.GetComponentByIndexs<RectTransform>(5, 0);
		nullPage = viewBehaviour.GetComponentByIndexs<RectTransform>(6, 0);
		rT_RTTextContent = viewBehaviour.GetComponentByIndexs<RectTransform>(7, 0);
		scrollView = viewBehaviour.GetComponentByIndexs<ScrollRect>(8, 0);

		BindEvent(closeButton);
		BindEvent(scrollView);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(closeButton);
		UnbindEvent(scrollView);

		closeButton = null;
		loopListView = null;
		titleText = null;
		conditonText = null;
		announcementImage = null;
		image = null;
		nullPage = null;
		rT_RTTextContent = null;
		scrollView = null;
	}
}