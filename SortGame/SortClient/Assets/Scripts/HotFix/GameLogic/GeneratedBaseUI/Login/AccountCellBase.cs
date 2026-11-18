using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;

public class AccountCellBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UILogin/AccountCell.prefab";
    
	protected TButton emptyButton;
	protected TextMeshProUGUI nameText;

    protected override void OnBindCompsAndEvents() 
    {
		emptyButton = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
		nameText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);

		BindEvent(emptyButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(emptyButton);

		emptyButton = null;
		nameText = null;
	}
}