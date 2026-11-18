using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class UIStoryInfoDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/TUIStory/UIStoryInfoDlg.prefab";
    
	protected TButton okBtn;
	protected TextMeshProUGUI descLb;
	protected TextMeshProUGUI titleText;
	protected Image bg;
	protected TextMeshProUGUI name2Text;
	protected TButton backButton;

    protected override void OnBindCompsAndEvents() 
    {
		okBtn = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
		descLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		titleText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		bg = viewBehaviour.GetComponentByIndexs<Image>(3, 0);
		name2Text = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(4, 0);
		backButton = viewBehaviour.GetComponentByIndexs<TButton>(5, 0);

		BindEvent(okBtn);
		BindEvent(backButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(okBtn);
		UnbindEvent(backButton);

		okBtn = null;
		descLb = null;
		titleText = null;
		bg = null;
		name2Text = null;
		backButton = null;
	}
}