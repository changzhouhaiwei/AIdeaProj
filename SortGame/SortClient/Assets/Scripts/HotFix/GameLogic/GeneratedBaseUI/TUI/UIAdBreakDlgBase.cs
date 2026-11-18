using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class UIAdBreakDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UIMain/UIAdBreakDlg.prefab";
    
	protected Image bg;
	protected TButton emptyButton;
	protected TextMeshProUGUI text;

    protected override void OnBindCompsAndEvents() 
    {
		bg = viewBehaviour.GetComponentByIndexs<Image>(0, 0);
		emptyButton = viewBehaviour.GetComponentByIndexs<TButton>(1, 0);
		text = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);

		BindEvent(emptyButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(emptyButton);

		bg = null;
		emptyButton = null;
		text = null;
	}
}