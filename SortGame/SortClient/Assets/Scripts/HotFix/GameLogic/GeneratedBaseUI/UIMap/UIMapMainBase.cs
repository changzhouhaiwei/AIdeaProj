using UnityEngine.UI;
using FishFramework;

public class UIMapMainBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UIMap/UIMapMain.prefab";

    protected TButton closeBtn;
    protected ScrollRect scrollView;
    protected TButton downBtn;

    protected override void OnBindCompsAndEvents()
    {
        closeBtn = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
        scrollView = viewBehaviour.GetComponentByIndexs<ScrollRect>(1, 0);
        downBtn = viewBehaviour.GetComponentByIndexs<TButton>(2, 0);
        BindEvent(closeBtn);
        BindEvent(scrollView);
        BindEvent(downBtn);
    }

    protected override void OnUnbindCompsAndEvents()
    {
        UnbindEvent(closeBtn);
        UnbindEvent(scrollView);
        UnbindEvent(downBtn);
        closeBtn = null;
        scrollView = null;
        downBtn = null;
    }
}
