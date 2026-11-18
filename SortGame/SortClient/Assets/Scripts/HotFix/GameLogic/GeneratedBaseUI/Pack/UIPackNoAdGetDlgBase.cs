using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;
using Spine.Unity;


public class UIPackNoAdGetDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UIAdsPack/UIPackNoAdGetDlg.prefab";
    
	protected TButton okBtn;
	protected SkeletonGraphic spine;

    protected override void OnBindCompsAndEvents() 
    {
		okBtn = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
		spine = viewBehaviour.GetComponentByIndexs<SkeletonGraphic>(1, 0);

		BindEvent(okBtn);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(okBtn);

		okBtn = null;
		spine = null;
	}
}