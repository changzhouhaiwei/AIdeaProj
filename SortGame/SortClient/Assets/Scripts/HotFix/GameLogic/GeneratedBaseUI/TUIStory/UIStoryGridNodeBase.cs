using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class UIStoryGridNodeBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/TUIStory/UIStoryGridNode.prefab";
    
	protected TButton bgButton;
	protected Image icon;
	protected Image lockSp;

    protected override void OnBindCompsAndEvents() 
    {
		bgButton = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
		icon = viewBehaviour.GetComponentByIndexs<Image>(1, 0);
		lockSp = viewBehaviour.GetComponentByIndexs<Image>(2, 0);

		BindEvent(bgButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(bgButton);

		bgButton = null;
		icon = null;
		lockSp = null;
	}
}