using FishFramework;

public class UIGoldTreeFirstTipsBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UIActivity/GoldTree/UIGoldTreeFirstTips.prefab";

	protected TButton backBtn;

    protected override void OnBindCompsAndEvents() 
    {
		backBtn = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);

		BindEvent(backBtn);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(backBtn);

		backBtn = null;
	}
}
