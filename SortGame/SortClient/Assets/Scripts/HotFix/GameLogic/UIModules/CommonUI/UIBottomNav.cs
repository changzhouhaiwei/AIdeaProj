using UnityEngine;
using FishFramework;
using FishTilePlay;
using TileGameCore;

public enum BottomNavTab
{
    Home = 0,
    Shop = 1,
    Map = 2,
}

/// <summary>
/// Static facade for UIBottomNav (UIBottomNavHost).
/// Host mounts under Panel layer, sorted above MainMenu/Shop/Map but below other panels.
/// </summary>
public static class UIBottomNav
{
    public static void Show(BottomNavTab tab = BottomNavTab.Home)
    {
        // Editor Test Level opens GameScene without GameModule bootstrap.
        if (GameModule.UI == null)
            return;

        if (tab == BottomNavTab.Home)
        {
            if (GameModule.UI.ExistPanel<UIShopMain>())
            {
                tab = BottomNavTab.Shop;
            }
            else if (GameModule.UI.ExistPanel<UIMapMain>())
            {
                tab = BottomNavTab.Map;
            }
        }

        var host = ResolveHost();
        if (host != null)
        {
            host.Show(tab);
            return;
        }

        Debug.LogError("[UIBottomNav] UIBottomNavHost not found under UIMainMenu.");
    }

    public static void Hide()
    {
        ResolveHost()?.Hide();
    }

    public static void SetTab(BottomNavTab tab)
    {
        ResolveHost()?.SetTab(tab);
    }

    public static void OpenShopFromNav(bool inGame = false)
    {
        CloseMapIfOpen();

        GPUserDataManager.Instance.BShopRed = false;
        GameModule.UI.OpenPanel<UIShopMain, bool>(inGame);
        if (inGame)
        {
            GPSDKMgr.Instance.HideADBanner();
        }
    }

    public static void OpenMapFromNav()
    {
        CloseShopIfOpen();

        if (GameModule.UI.ExistPanel<UIMapMain>())
        {
            Show(BottomNavTab.Map);
            return;
        }

        GameModule.UI.OpenPanel<UIMapMain>();
        Show(BottomNavTab.Map);
    }

    internal static void HandleHomeClicked()
    {
        CloseShopIfOpen();
        CloseMapIfOpen();
        UIController.ShowPage<UIMainMenu>();
        Show(BottomNavTab.Home);
    }

    internal static void HandleShopClicked(BottomNavTab currentTab)
    {
        if (currentTab == BottomNavTab.Shop && GameModule.UI.ExistPanel<UIShopMain>())
        {
            Show(BottomNavTab.Shop);
            return;
        }

        OpenShopFromNav();
    }

    internal static void HandleMapClicked()
    {
        OpenMapFromNav();
    }

    private static void CloseShopIfOpen()
    {
        if (GameModule.UI != null && GameModule.UI.ExistPanel<UIShopMain>())
        {
            GameModule.UI.GetPanel<UIShopMain>()?.CloseFromBottomNav();
        }
    }

    private static void CloseMapIfOpen()
    {
        if (GameModule.UI != null && GameModule.UI.ExistPanel<UIMapMain>())
        {
            GameModule.UI.GetPanel<UIMapMain>()?.CloseFromBottomNav();
        }
    }

    private static UIBottomNavHost ResolveHost()
    {
        if (UIBottomNavHost.Instance != null)
        {
            return UIBottomNavHost.Instance;
        }

        return Object.FindObjectOfType<UIBottomNavHost>(true);
    }
}
