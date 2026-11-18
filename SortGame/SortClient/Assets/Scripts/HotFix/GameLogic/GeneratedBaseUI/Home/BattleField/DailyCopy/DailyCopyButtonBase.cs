using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class DailyCopyButtonBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/BattleField/DailyCopy/DailyCopyButton.prefab";
    
	protected Image btnNotSeclet;
	protected Image btnSeclet;
	protected TButton btnClick;

    protected override void OnBindCompsAndEvents() 
    {
		btnNotSeclet = viewBehaviour.GetComponentByIndexs<Image>(0, 0);
		btnSeclet = viewBehaviour.GetComponentByIndexs<Image>(1, 0);
		btnClick = viewBehaviour.GetComponentByIndexs<TButton>(2, 0);

		BindEvent(btnClick);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(btnClick);

		btnNotSeclet = null;
		btnSeclet = null;
		btnClick = null;
	}
}