using TMPro;
using FishFramework;

public class UIPackNoAdPanelBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UIAdsPack/UIPackNoAdPanel.prefab";
    
	protected TButton closeBtn;
	protected TButton buyBtn;
	protected TextMeshProUGUI descLb;
	protected TextMeshProUGUI priceText;

    protected override void OnBindCompsAndEvents() 
    {
		closeBtn = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
		buyBtn = viewBehaviour.GetComponentByIndexs<TButton>(1, 0);
		descLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		priceText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);

		BindEvent(closeBtn);
		BindEvent(buyBtn);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(closeBtn);
		UnbindEvent(buyBtn);

		closeBtn = null;
		buyBtn = null;
		descLb = null;
		priceText = null;
	}
}