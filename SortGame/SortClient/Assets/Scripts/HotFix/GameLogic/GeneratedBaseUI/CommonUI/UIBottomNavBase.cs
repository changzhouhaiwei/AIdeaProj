using UnityEngine;
using UnityEngine.UI;
using FishFramework;

public class UIBottomNavBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UICommon/UIBottomNav.prefab";

    protected TButton mainmenuBtn;
    protected TButton shopBtn;
    protected TButton mapBtn;

    protected override void OnBindCompsAndEvents()
    {
        mainmenuBtn = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
        shopBtn = viewBehaviour.GetComponentByIndexs<TButton>(1, 0);
        mapBtn = viewBehaviour.GetComponentByIndexs<TButton>(2, 0);

        BindEvent(mainmenuBtn);
        BindEvent(shopBtn);
        BindEvent(mapBtn);
    }

    protected override void OnUnbindCompsAndEvents()
    {
        UnbindEvent(mainmenuBtn);
        UnbindEvent(shopBtn);
        UnbindEvent(mapBtn);

        mainmenuBtn = null;
        shopBtn = null;
        mapBtn = null;
    }
}
