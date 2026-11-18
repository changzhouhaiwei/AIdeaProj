using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class UserHeadNodeBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UICommon/UserHeadNode.prefab";
    
	protected Image bSel;
	protected Image icon;
	protected TButton emptyButton;
	protected Image frame;

    protected override void OnBindCompsAndEvents() 
    {
		bSel = viewBehaviour.GetComponentByIndexs<Image>(0, 0);
		icon = viewBehaviour.GetComponentByIndexs<Image>(1, 0);
		emptyButton = viewBehaviour.GetComponentByIndexs<TButton>(2, 0);
		frame = viewBehaviour.GetComponentByIndexs<Image>(3, 0);

		BindEvent(emptyButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(emptyButton);

		bSel = null;
		icon = null;
		emptyButton = null;
		frame = null;
	}
}
