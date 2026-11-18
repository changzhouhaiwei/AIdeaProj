using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class AnnouncementButtonBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Announcement/AnnouncementButton.prefab";
    
	protected TButton btnClick;
	protected TextMeshProUGUI notText;
	protected TextMeshProUGUI secletText;
	protected RectTransform btnNotSeclet;
	protected RectTransform btnSeclet;

    protected override void OnBindCompsAndEvents() 
    {
		btnClick = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
		notText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		secletText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		btnNotSeclet = viewBehaviour.GetComponentByIndexs<RectTransform>(3, 0);
		btnSeclet = viewBehaviour.GetComponentByIndexs<RectTransform>(4, 0);

		BindEvent(btnClick);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(btnClick);

		btnClick = null;
		notText = null;
		secletText = null;
		btnNotSeclet = null;
		btnSeclet = null;
	}
}