using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using FishFramework;
using Tween = DG.Tweening.Tween;

/// <summary>
/// Shared bottom nav. Uses UIBottomNav itself (Canvas + GraphicRaycaster on this GO),
/// reparented under UIRoot Panel so it stays above MainMenu / Shop / Map,
/// but below any other panels opened on top of them.
/// </summary>
public class UIBottomNavHost : MonoBehaviour
{
    public static UIBottomNavHost Instance { get; private set; }

    /// <summary>
    /// Home-only: below the first Panel (startOrder 0), still above UIPage MainMenu
    /// because sorting layer "UI" renders above the Default-layer main canvas.
    /// </summary>
    private const int HomeNavSortingOrder = -1;

    [SerializeField] private Button mainmenuBtn;
    [SerializeField] private Button shopBtn;
    [SerializeField] private Button mapBtn;

    private BottomNavTab currentTab = BottomNavTab.Home;
    private Canvas navCanvas;
    private const float IconSelectedOffsetY = 60f;
    private const float TextSelectedOffsetY = 10f;
    private const float MoveDuration = 0.3f;
    private const float DeselectDuration = 0.15f;
    private const float TextMoveDelay = 0.05f;
    private const float TextMoveDuration = 0.35f;
    private const float TextStartScale = 0.9f;
    private const float TextPeakScale = 1.1f;
    private const float TextGrowDuration = 0.22f;
    private const float TextSettleDuration = 0.18f;
    private const float ClickCooldown = 0.35f;

    private NavButtonVisual mainmenuVisual;
    private NavButtonVisual shopVisual;
    private NavButtonVisual mapVisual;
    private float nextAllowedClickTime;

    private sealed class NavButtonVisual
    {
        public RectTransform Icon;
        public RectTransform Text;
        public GameObject SelectedIconImage;
        public Vector2 IconRestPosition;
        public Vector2 TextRestPosition;
        public Vector3 TextRestScale;
        public Tween IconMoveTween;
        public Tween TextMoveTween;
        public Tween TextScaleTween;

        public void KillTweens()
        {
            IconMoveTween?.Kill();
            TextMoveTween?.Kill();
            TextScaleTween?.Kill();
            IconMoveTween = null;
            TextMoveTween = null;
            TextScaleTween = null;
        }
    }

    private void Awake()
    {
        Instance = this;
        gameObject.name = "UIBottomNav";

        EnsureButtonRefs();
        CacheButtonVisuals();
        BindButtons();
        ApplyTabVisual(currentTab, false);
    }

    private void OnDestroy()
    {
        KillVisualTweens();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnDisable()
    {
        KillVisualTweens();
    }

    private void KillVisualTweens()
    {
        mainmenuVisual?.KillTweens();
        shopVisual?.KillTweens();
        mapVisual?.KillTweens();
    }

    private void EnsureButtonRefs()
    {
        if (mainmenuBtn == null)
        {
            mainmenuBtn = FindNavButton("MainmenuBtn");
        }

        if (shopBtn == null)
        {
            shopBtn = FindNavButton("ShopBtn");
        }

        if (mapBtn == null)
        {
            mapBtn = FindNavButton("MapBtn");
        }

        ConfigureNavButton(mainmenuBtn);
        ConfigureNavButton(shopBtn);
        ConfigureNavButton(mapBtn);
    }

    private void CacheButtonVisuals()
    {
        mainmenuVisual = CacheButtonVisual(mainmenuBtn);
        shopVisual = CacheButtonVisual(shopBtn);
        mapVisual = CacheButtonVisual(mapBtn);
    }

    private static NavButtonVisual CacheButtonVisual(Button button)
    {
        if (button == null)
        {
            return null;
        }

        RectTransform icon = null;
        for (int i = 0; i < button.transform.childCount; i++)
        {
            var child = button.transform.GetChild(i);
            if (child.name.EndsWith("Icon"))
            {
                icon = child as RectTransform;
                break;
            }
        }

        if (icon == null)
        {
            return null;
        }

        var text = icon.Find("Text") as RectTransform;
        return new NavButtonVisual
        {
            Icon = icon,
            Text = text,
            SelectedIconImage = icon.Find("Image")?.gameObject,
            IconRestPosition = icon.anchoredPosition,
            TextRestPosition = text != null ? text.anchoredPosition : Vector2.zero,
            TextRestScale = text != null ? text.localScale : Vector3.one,
        };
    }

    private Button FindNavButton(string buttonName)
    {
        var direct = transform.Find(buttonName);
        if (direct != null)
        {
            return direct.GetComponent<Button>();
        }

        var underGroup = transform.Find("BottomGroup/" + buttonName);
        return underGroup?.GetComponent<Button>();
    }

    private static void ConfigureNavButton(Button button)
    {
        if (button is TButton tButton)
        {
            tButton.clickDoTween = false;
        }
    }

    private void BindButtons()
    {
        if (mainmenuBtn != null)
        {
            mainmenuBtn.onClick.RemoveListener(OnHomeClicked);
            mainmenuBtn.onClick.AddListener(OnHomeClicked);
        }

        if (shopBtn != null)
        {
            shopBtn.onClick.RemoveListener(OnShopClicked);
            shopBtn.onClick.AddListener(OnShopClicked);
        }

        if (mapBtn != null)
        {
            mapBtn.onClick.RemoveListener(OnMapClicked);
            mapBtn.onClick.AddListener(OnMapClicked);
        }
    }

    public void Show(BottomNavTab tab)
    {
        MountToPanelLayer();
        gameObject.SetActive(true);

        EnsureCanvas();
        SetTab(tab);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void MountToPanelLayer()
    {
        if (GameModule.UI == null)
        {
            if (GameData.BFromStartScene)
                Debug.LogError("[UIBottomNav] GameModule.UI is null.");
            return;
        }

        var panelRect = GameModule.UI.GetLayerRect(PanelLayer.Panel);
        if (panelRect == null)
        {
            if (GameData.BFromStartScene)
                Debug.LogError("[UIBottomNav] Panel layer is null.");
            return;
        }

        if (transform.parent != panelRect)
        {
            transform.SetParent(panelRect, false);
        }

        // ScreenSpaceCamera canvases are filtered by the UI camera's culling
        // mask. The MainMenu prefab overrides the nav hierarchy to another
        // layer, so synchronize it with the destination Panel layer.
        int panelLayer = panelRect.gameObject.layer;
        foreach (var child in GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.layer = panelLayer;
        }

        var rt = transform as RectTransform;
        if (rt != null)
        {
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 75f);
            rt.sizeDelta = new Vector2(0f, 150f);
        }

        transform.SetAsLastSibling();
    }

    private void EnsureCanvas()
    {
        navCanvas = GetComponent<Canvas>();
        if (navCanvas == null)
        {
            navCanvas = gameObject.AddComponent<Canvas>();
        }

        var raycaster = GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            raycaster = gameObject.AddComponent<GraphicRaycaster>();
        }

        navCanvas.enabled = true;
        raycaster.enabled = true;

        // Keep this nested canvas in the same space as the UI root. The prefab
        // has its own render-mode overrides, so relying on its serialized value
        // can make the nav behave like a standalone Overlay canvas in Scene view.
        var parentCanvas = transform.parent != null
            ? transform.parent.GetComponentInParent<Canvas>()
            : null;
        var rootCanvas = parentCanvas != null ? parentCanvas.rootCanvas : null;
        if (rootCanvas != null)
        {
            navCanvas.renderMode = rootCanvas.renderMode;
            navCanvas.worldCamera = rootCanvas.worldCamera;
        }

        navCanvas.overrideSorting = true;
        navCanvas.sortingLayerName = "UI";
        navCanvas.sortingOrder = ComputeNavSortingOrder();
    }

    /// <summary>
    /// Sit just above the active tab page (Shop/Map), or just below the first Panel when on Home,
    /// so dialogs opened via OpenPanel always stack above the nav.
    /// </summary>
    private static int ComputeNavSortingOrder()
    {
        int order = HomeNavSortingOrder;
        if (GameModule.UI == null)
        {
            return order;
        }

        if (GameModule.UI.ExistPanel<UIShopMain>())
        {
            var shop = GameModule.UI.GetPanel<UIShopMain>();
            if (shop?.canvas != null)
            {
                order = Mathf.Max(order, shop.canvas.sortingOrder + shop.panelBehaviour.thickness);
            }
        }

        if (GameModule.UI.ExistPanel<UIMapMain>())
        {
            var map = GameModule.UI.GetPanel<UIMapMain>();
            if (map?.canvas != null)
            {
                order = Mathf.Max(order, map.canvas.sortingOrder + map.panelBehaviour.thickness);
            }
        }

        return order;
    }

    public void SetTab(BottomNavTab tab)
    {
        bool animate = tab != currentTab;
        currentTab = tab;
        if (animate)
        {
            ApplyTabVisual(tab, true);
        }
        else
        {
            // Home/Map 打开流程会重复 Show/SetTab。同 Tab 再次应用无动画状态
            // 会 Kill 前一次刚启动的 Tween，导致只有 Shop 看起来有动画。
            ApplySelectedNodeState(tab);
        }
        RefreshSortingOrder();
    }

    /// <summary>
    /// Recompute sorting after Shop/Map close so subsequent dialogs stay above the nav.
    /// </summary>
    public void RefreshSortingOrder()
    {
        if (navCanvas == null)
        {
            navCanvas = GetComponent<Canvas>();
        }

        if (navCanvas != null && gameObject.activeInHierarchy)
        {
            navCanvas.sortingOrder = ComputeNavSortingOrder();
        }
    }

    private void OnHomeClicked()
    {
        if (!TryBeginNavClick())
        {
            return;
        }

        UIBottomNav.HandleHomeClicked();
    }

    private void OnShopClicked()
    {
        if (!TryBeginNavClick())
        {
            return;
        }

        UIBottomNav.HandleShopClicked(currentTab);
    }

    private void OnMapClicked()
    {
        if (!TryBeginNavClick())
        {
            return;
        }

        UIBottomNav.HandleMapClicked();
    }

    private bool TryBeginNavClick()
    {
        if (Time.unscaledTime < nextAllowedClickTime)
        {
            return false;
        }

        nextAllowedClickTime = Time.unscaledTime + ClickCooldown;
        return true;
    }

    private void ApplyTabVisual(BottomNavTab tab, bool animate)
    {
        SetButtonSelected(mainmenuBtn, mainmenuVisual, tab == BottomNavTab.Home, animate);
        SetButtonSelected(shopBtn, shopVisual, tab == BottomNavTab.Shop, animate);
        SetButtonSelected(mapBtn, mapVisual, tab == BottomNavTab.Map, animate);
    }

    private void ApplySelectedNodeState(BottomNavTab tab)
    {
        SetSelectedNode(mainmenuBtn, tab == BottomNavTab.Home);
        SetSelectedNode(shopBtn, tab == BottomNavTab.Shop);
        SetSelectedNode(mapBtn, tab == BottomNavTab.Map);
        SetSelectedIconImage(mainmenuVisual, tab == BottomNavTab.Home);
        SetSelectedIconImage(shopVisual, tab == BottomNavTab.Shop);
        SetSelectedIconImage(mapVisual, tab == BottomNavTab.Map);
    }

    private static void SetButtonSelected(
        Button button,
        NavButtonVisual visual,
        bool selected,
        bool animate)
    {
        if (button == null)
        {
            return;
        }

        SetSelectedNode(button, selected);
        SetSelectedIconImage(visual, selected);
        ApplySelectionAnimation(visual, selected, animate);
    }

    private static void SetSelectedNode(Button button, bool selected)
    {
        if (button == null)
        {
            return;
        }

        var selectedNode = button.transform.Find("selected");
        if (selectedNode != null)
        {
            selectedNode.gameObject.SetActive(selected);
        }
    }

    private static void SetSelectedIconImage(NavButtonVisual visual, bool selected)
    {
        if (visual?.SelectedIconImage == null)
        {
            return;
        }

        visual.SelectedIconImage.SetActive(selected);
    }

    private static void ApplySelectionAnimation(NavButtonVisual visual, bool selected, bool animate)
    {
        if (visual?.Icon == null)
        {
            return;
        }

        visual.KillTweens();

        Vector2 iconTarget = visual.IconRestPosition;
        Vector2 textTarget = visual.TextRestPosition;
        if (selected)
        {
            iconTarget.y += IconSelectedOffsetY;
            textTarget.y += TextSelectedOffsetY;
        }

        if (!animate)
        {
            visual.Icon.anchoredPosition = iconTarget;
            if (visual.Text != null)
            {
                visual.Text.anchoredPosition = textTarget;
                visual.Text.localScale = visual.TextRestScale;
            }

            return;
        }

        float moveDuration = selected ? MoveDuration : DeselectDuration;
        visual.IconMoveTween = visual.Icon
            .DOAnchorPos(iconTarget, moveDuration)
            .SetEase(Ease.InOutCubic)
            .SetLink(visual.Icon.gameObject, LinkBehaviour.KillOnDestroy);

        if (visual.Text == null)
        {
            return;
        }

        if (selected)
        {
            var textMoveSequence = DOTween.Sequence();
            textMoveSequence.AppendInterval(TextMoveDelay);
            textMoveSequence.Append(visual.Text
                .DOAnchorPos(textTarget, TextMoveDuration)
                .SetEase(Ease.OutCubic));
            textMoveSequence.SetLink(visual.Text.gameObject, LinkBehaviour.KillOnDestroy);
            visual.TextMoveTween = textMoveSequence;

            visual.Text.localScale = visual.TextRestScale * TextStartScale;
            var scaleSequence = DOTween.Sequence();
            scaleSequence.Append(CreateSafeTextScaleTween(
                visual,
                TextStartScale,
                TextPeakScale,
                TextGrowDuration,
                Ease.OutCubic));
            scaleSequence.Append(CreateSafeTextScaleTween(
                visual,
                TextPeakScale,
                1f,
                TextSettleDuration,
                Ease.OutSine));
            scaleSequence.SetLink(visual.Text.gameObject, LinkBehaviour.KillOnDestroy);
            visual.TextScaleTween = scaleSequence;
        }
        else
        {
            visual.TextMoveTween = visual.Text
                .DOAnchorPos(textTarget, DeselectDuration)
                .SetEase(Ease.InOutCubic)
                .SetLink(visual.Text.gameObject, LinkBehaviour.KillOnDestroy);
            float currentScale = visual.TextRestScale.x != 0f
                ? visual.Text.localScale.x / visual.TextRestScale.x
                : 1f;
            visual.TextScaleTween = CreateSafeTextScaleTween(
                    visual,
                    currentScale,
                    1f,
                    DeselectDuration,
                    Ease.InOutCubic)
                .SetLink(visual.Text.gameObject, LinkBehaviour.KillOnDestroy);
        }
    }

    private static Tween CreateSafeTextScaleTween(
        NavButtonVisual visual,
        float startScale,
        float endScale,
        float duration,
        Ease ease)
    {
        float scale = startScale;
        return DOTween.To(
                () => scale,
                value =>
                {
                    scale = value;
                    if (visual?.Text != null)
                    {
                        visual.Text.localScale = visual.TextRestScale * value;
                    }
                },
                endScale,
                duration)
            .SetEase(ease);
    }
}
