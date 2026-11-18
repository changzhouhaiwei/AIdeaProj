using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif
using TileGameCore;
using DG.Tweening;
using FishFramework;
using System;
using Spine.Unity;

namespace FishTilePlay
{
    public class UIGame : UIPage
    {
        [SerializeField] RectTransform safeAreaRectTransform;
        [SerializeField] CoinPanelView coinsPanel;
        // [SerializeField] UILevelQuitPopUp quitPopUp;
        [SerializeField] UILevelNumberText levelNumberText;
        [SerializeField] TextMeshProUGUI debugLevelNumberText;

        [SerializeField] PUUIDirector powerUpsUIController;
        public PUUIDirector PowerUpsUIController => powerUpsUIController;

        // [SerializeField] UILevelQuitPopUp exitPopUp;
        [SerializeField] Button exitButton;
        [SerializeField] Button settingButton;
        [SerializeField] RectTransform boxCoverTopBoundary;
        [SerializeField] Button gmLevelPassBtn;
        
        [SerializeField] UIFadeAnimation exitButtonFadeAnimation;

        [SerializeField] GameObject devOverlay;
        [SerializeField] TextMeshProUGUI textLevelNumCntLb;
        [SerializeField] UITipsDlg tintDlg;

        [SerializeField] GameObject adEffectNode;
        [SerializeField] Image adEffectBar;
        [SerializeField] SkeletonGraphic spine12;
        [SerializeField] TileHint tileHintNode;

        [SerializeField] GameObject guideHintObj;
        [SerializeField] UIGoldTreeGame goldTreeGameUI;
        [SerializeField] GameObject coinCollect;
        [SerializeField] Transform coinCollectIcon;
        [SerializeField] TextMeshProUGUI coinCollectNumLb;

        private BoxCoverHUD boxCoverHud;
        private RectTransform boxCoverFlyLayer;
        bool goldTreeSpecialDockRaised;

        private Vector3 initAdEffPos;
        void Awake()
        {
            initAdEffPos = adEffectNode.GetComponent<RectTransform>().anchoredPosition;
            HideCoinCollect();
            EnsureBoxCoverHud();
        }

        public override void Init()
        {
            EnsureGoldTreeGameUI();
            HideCoinCollect();
            EnsureBoxCoverHud();
            coinsPanel.SetAutoGainAnim(true);
            coinsPanel.SetupPanel();
            exitButton.onClick.AddListener(ShowExitPopUp);
            gmLevelPassBtn.onClick.AddListener(OnGmLevelPassBtnClick);
            NotchSaveArea.RegisterRectTransform(safeAreaRectTransform);
            DevPanelEnabler.RegisterPanel(devOverlay);
            EnsureGuideDismissOverlay();
        }

        Action mPendingGoldTreeFirstTipsAfterDo;
        Action mPendingLevelEnterAfterDo;

        public void ShowLevelTintDlg(Action afterDoFunc = null)
        {
            if (GameData.BFromStartScene
                && GameData.GoldTreeActData != null
                && GameData.GoldTreeActData.CheckShowFirstTips())
            {
                // 首次提示等棋子生成布局动画完成后再弹（见 OnLevelLayoutFinished）
                GameData.GoldTreeActData.MarkFirstTipsShown();
                mPendingGoldTreeFirstTipsAfterDo = afterDoFunc;
                mPendingLevelEnterAfterDo = null;
                return;
            }

            mPendingGoldTreeFirstTipsAfterDo = null;
            // 布局动画结束后再弹 TipsDlg，避免生成动画期间误触。
            mPendingLevelEnterAfterDo = afterDoFunc;
            tintDlg.SetActiveEx(false);
        }

        /// <summary>棋子生成布局动画结束后调用。</summary>
        public void ShowGoldTreeFirstTipsAfterLayout()
        {
            OnLevelLayoutFinished();
        }

        void OnLevelLayoutFinished()
        {
            if (mPendingGoldTreeFirstTipsAfterDo != null)
            {
                var afterDoFunc = mPendingGoldTreeFirstTipsAfterDo;
                mPendingGoldTreeFirstTipsAfterDo = null;

                Utility.DGDelayedCall(0.1f, () =>
                {
                    var firstTips = GameModule.UI.OpenPanel<UIGoldTreeFirstTips>();
                    firstTips.ClickAction = afterDoFunc;
                });
                return;
            }

            if (mPendingLevelEnterAfterDo == null)
            {
                return;
            }

            var pendingAfterDo = mPendingLevelEnterAfterDo;
            mPendingLevelEnterAfterDo = null;

            int levelIndex = GameDirector.levelController.GetLoadedLevelIndex();
            if (GameData.BFromStartScene && tintDlg != null && !LevelDirector.UsesColorChallenge(levelIndex))
            {
                if (!TipsDlgDailyCap.Allow(levelIndex))
                {
                    pendingAfterDo?.Invoke();
                    return;
                }

                Utility.DGDelayedCall(0.01f, () =>
                {
                    tintDlg.SetActiveEx(true);
                    tintDlg.PlayShowTint(pendingAfterDo);
                });
                return;
            }

            pendingAfterDo?.Invoke();
        }

        public TileHint GetTileHintNode()
        {
            return tileHintNode;
        }

        void EnsureBoxCoverHud()
        {
            EnsureGoalHud(ResolveActiveGoalHudStyle());
        }

        static GoalHudStyle ResolveActiveGoalHudStyle()
        {
            if (IQChallengeManager.HasActiveLevel
                && !BoxCoverManager.HasActiveLevel
                && !AwardColorEggCoverManager.HasActiveLevel)
                return GoalHudStyle.IQChallenge;
            return GoalHudStyle.Collection;
        }

        void EnsureGoalHud(GoalHudStyle style)
        {
            string path = BoxCoverHUD.PrefabPathFor(style);
            if (boxCoverHud != null && boxCoverHud.SourcePrefabPath == path)
                return;

            if (boxCoverHud != null)
            {
                Destroy(boxCoverHud.gameObject);
                boxCoverHud = null;
            }

            // 收集条要对齐世界坐标里的码头，挂整屏页面，不跟安全区缩进。
            Transform hudParent = transform;
            TMP_FontAsset font = debugLevelNumberText != null
                ? debugLevelNumberText.font
                : (textLevelNumCntLb != null ? textLevelNumCntLb.font : null);
            boxCoverHud = BoxCoverHUD.Create(hudParent, font, path);
        }

        public Color GetIqConvertTint(Color fallback)
        {
            if (boxCoverHud == null || boxCoverHud.SourcePrefabPath != BoxCoverHUD.IQPrefabPath)
                return fallback;

            return boxCoverHud.ReadNamedImageColor("ConvertTint", fallback);
        }

        public void ShowBoxCoverHud(int collected, int total, bool animate)
        {
            ShowGoalHud(GoalHudStyle.Collection, collected, total, animate);
        }

        public void SetBoxCoverHudCollectible(Sprite sprite)
        {
            EnsureBoxCoverHud();
            boxCoverHud?.SetCollectibleOverride(sprite);
        }

        public void ShowGoalHud(GoalHudStyle style, int collected, int total, bool animate)
        {
            EnsureGoalHud(style);
            boxCoverHud?.Show(collected, total, animate, style);
            SyncGoldTreeRowToBoxCoverHud();
        }

        public void LayoutBoxCoverHud()
        {
            EnsureGoalHud(ResolveActiveGoalHudStyle());
            boxCoverHud?.LayoutToCurrentDock();
            SyncGoldTreeRowToBoxCoverHud();
        }

        public float GetRequiredBoxCoverTopBoundaryWorldY(Camera gameCamera)
        {
            if (boxCoverTopBoundary == null)
                throw new MissingReferenceException(
                    "UIGame.boxCoverTopBoundary must be wired on the UI Game Play prefab.");
            if (safeAreaRectTransform == null)
                throw new MissingReferenceException(
                    "UIGame.safeAreaRectTransform must be wired on the UI Game Play prefab.");
            if (!boxCoverTopBoundary.IsChildOf(safeAreaRectTransform))
                throw new InvalidOperationException(
                    "UIGame.boxCoverTopBoundary must be a child of Safe Area.");
            if (gameCamera == null)
                throw new MissingReferenceException(
                    "Gameplay camera is required for the BoxCover top boundary.");

            // 只使用 SafeArea 内的静态布局坐标，不读取页面进场动画造成的世界位移。
            Canvas.ForceUpdateCanvases();
            Rect safeLayoutRect = safeAreaRectTransform.rect;
            Rect safeScreenRect = Screen.safeArea;
            if (safeLayoutRect.height <= 0.001f || safeScreenRect.height <= 0.001f)
                throw new InvalidOperationException("Safe Area is invalid for BoxCover layout.");

            Bounds boundaryBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                safeAreaRectTransform,
                boxCoverTopBoundary);
            float normalizedY = Mathf.InverseLerp(
                safeLayoutRect.yMin,
                safeLayoutRect.yMax,
                boundaryBounds.min.y);
            if (normalizedY < 0f || normalizedY > 1f)
                throw new InvalidOperationException(
                    $"BoxCover top boundary is outside Safe Area: normalizedY={normalizedY:F3}.");

            float screenY = Mathf.Lerp(safeScreenRect.yMin, safeScreenRect.yMax, normalizedY);
            float depth = Mathf.Abs(gameCamera.transform.position.z);
            if (depth < 0.01f)
                depth = 10f;
            return gameCamera.ScreenToWorldPoint(
                new Vector3(Screen.width * 0.5f, screenY, depth)).y;
        }

        public float GetRequiredBoxCoverBoardBottomWorldY(Camera gameCamera)
        {
            if (powerUpsUIController == null)
                throw new MissingReferenceException(
                    "UIGame.powerUpsUIController must be wired on the UI Game Play prefab.");
            return powerUpsUIController.GetRequiredBoxCoverBoardBottomWorldY(gameCamera);
        }

        public void SyncBoxCoverHudToDockBreathing()
        {
            boxCoverHud?.SyncToDockBreathingMotion();
        }

        public void RestoreBoxCoverHudFromDockBreathing()
        {
            boxCoverHud?.RestoreDockBreathingLayout();
        }

        public void HideBoxCoverHud()
        {
            boxCoverHud?.Hide();
            RestoreGoldTreeRowWithoutHud();
        }

        public void FadeBoxCoverHud(float duration)
        {
            EnsureBoxCoverHud();
            if (boxCoverHud == null)
            {
                RestoreGoldTreeRowWithoutHud();
                return;
            }

            boxCoverHud.FadeOut(duration, RestoreGoldTreeRowWithoutHud);
        }

        public void PlayBoxCoverHudAppear(float duration, float delay = 0f)
        {
            EnsureBoxCoverHud();
            boxCoverHud?.PlayAppearFade(duration, delay);
        }

        public void CancelBoxCoverHudAppear()
        {
            boxCoverHud?.CancelAppear();
        }

        public Vector3 GetBoxCoverHudTargetWorldPosition(Vector3 sourceWorldPosition)
        {
            EnsureBoxCoverHud();
            return boxCoverHud != null
                ? boxCoverHud.GetTargetWorldPosition(sourceWorldPosition)
                : sourceWorldPosition + Vector3.up * 3f;
        }

        public RectTransform CreateBoxCoverFlyItem(SpriteRenderer source)
        {
            if (source == null || source.sprite == null)
                return null;

            RectTransform layer = EnsureBoxCoverFlyLayer();
            if (layer == null
                || !TryGetSpriteFlyLayout(source, out Vector2 local, out Vector2 size, out float zAngle))
                return null;

            GameObject item = new GameObject(
                "BoxCoverFlyItem",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            item.layer = layer.gameObject.layer;
            RectTransform rect = (RectTransform)item.transform;
            rect.SetParent(layer, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = local;
            rect.sizeDelta = size;
            rect.localRotation = Quaternion.Euler(0f, 0f, zAngle);
            rect.localScale = Vector3.one;

            Image image = item.GetComponent<Image>();
            image.sprite = source.sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = source.color;
            return rect;
        }

        bool TryGetSpriteFlyLayout(
            SpriteRenderer source,
            out Vector2 local,
            out Vector2 size,
            out float zAngle)
        {
            local = default;
            size = new Vector2(48f, 48f);
            zAngle = 0f;

            RectTransform layer = EnsureBoxCoverFlyLayer();
            Camera gameCamera = Camera.main;
            if (source == null || source.sprite == null || layer == null || gameCamera == null)
                return false;

            Bounds spriteBounds = source.sprite.bounds;
            Vector3 center = spriteBounds.center;
            Vector3 extents = spriteBounds.extents;
            Vector3[] worldCorners =
            {
                source.transform.TransformPoint(center + new Vector3(-extents.x, -extents.y, 0f)),
                source.transform.TransformPoint(center + new Vector3(extents.x, -extents.y, 0f)),
                source.transform.TransformPoint(center + new Vector3(extents.x, extents.y, 0f)),
                source.transform.TransformPoint(center + new Vector3(-extents.x, extents.y, 0f)),
            };

            Vector2[] uiCorners = new Vector2[4];
            Camera uiCamera = GetBoxCoverUiCamera();
            for (int i = 0; i < 4; i++)
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(gameCamera, worldCorners[i]);
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screen, uiCamera, out uiCorners[i]))
                    return false;
            }

            local = (uiCorners[0] + uiCorners[1] + uiCorners[2] + uiCorners[3]) * 0.25f;
            Vector2 right = uiCorners[1] - uiCorners[0];
            Vector2 up = uiCorners[3] - uiCorners[0];
            size = new Vector2(
                Mathf.Max(8f, right.magnitude),
                Mathf.Max(8f, up.magnitude));
            zAngle = Mathf.Atan2(right.y, right.x) * Mathf.Rad2Deg;
            return true;
        }

        public bool TryWorldToFlyLocal(Vector3 worldPosition, out Vector2 local)
        {
            local = default;
            RectTransform layer = EnsureBoxCoverFlyLayer();
            Camera gameCamera = Camera.main;
            if (layer == null || gameCamera == null)
                return false;

            Vector2 screen = RectTransformUtility.WorldToScreenPoint(gameCamera, worldPosition);
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                layer,
                screen,
                GetBoxCoverUiCamera(),
                out local);
        }

        public bool TryGetBoxCoverFlyTarget(out Vector2 local, out Vector2 size)
        {
            return TryGetBoxCoverFlyTarget(out local, out size, out _);
        }

        public bool TryGetBoxCoverFlyTarget(out Vector2 local, out Vector2 size, out float zAngle)
        {
            local = default;
            size = new Vector2(34f, 34f);
            zAngle = 0f;
            EnsureBoxCoverHud();
            RectTransform target = boxCoverHud != null ? boxCoverHud.GetCollectibleRect() : null;
            RectTransform layer = EnsureBoxCoverFlyLayer();
            if (target == null || layer == null)
                return false;

            size = target.rect.size;
            Vector3[] worldCorners = new Vector3[4];
            target.GetWorldCorners(worldCorners);
            Camera uiCamera = GetBoxCoverUiCamera();
            Vector2[] uiCorners = new Vector2[4];
            for (int i = 0; i < 4; i++)
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(uiCamera, worldCorners[i]);
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        layer,
                        screen,
                        uiCamera,
                        out uiCorners[i]))
                    return false;
            }

            local = (uiCorners[0] + uiCorners[1] + uiCorners[2] + uiCorners[3]) * 0.25f;
            Vector2 right = uiCorners[3] - uiCorners[0];
            Vector2 up = uiCorners[1] - uiCorners[0];
            size = new Vector2(
                Mathf.Max(8f, right.magnitude),
                Mathf.Max(8f, up.magnitude));
            zAngle = Mathf.Atan2(right.y, right.x) * Mathf.Rad2Deg;
            return true;
        }

        public BoxCoverFlySettings GetBoxCoverFlySettings()
        {
            EnsureBoxCoverHud();
            return boxCoverHud != null ? boxCoverHud.FlySettings : BoxCoverFlySettings.Default;
        }

        RectTransform EnsureBoxCoverFlyLayer()
        {
            if (boxCoverFlyLayer == null)
            {
                GameObject layerObject = new GameObject("BoxCoverFlyLayer", typeof(RectTransform), typeof(Canvas));
                layerObject.layer = gameObject.layer;
                boxCoverFlyLayer = (RectTransform)layerObject.transform;
                boxCoverFlyLayer.SetParent(transform, false);
                boxCoverFlyLayer.anchorMin = Vector2.zero;
                boxCoverFlyLayer.anchorMax = Vector2.one;
                boxCoverFlyLayer.offsetMin = Vector2.zero;
                boxCoverFlyLayer.offsetMax = Vector2.zero;
                boxCoverFlyLayer.pivot = new Vector2(0.5f, 0.5f);
            }

            boxCoverFlyLayer.SetAsLastSibling();
            Canvas host = GetComponent<Canvas>();
            Canvas flyCanvas = boxCoverFlyLayer.GetComponent<Canvas>();
            flyCanvas.overrideSorting = true;
            flyCanvas.sortingLayerName = host != null ? host.sortingLayerName : "UI";
            flyCanvas.sortingOrder = (host != null ? host.sortingOrder : 0) + 1;
            return boxCoverFlyLayer;
        }

        Camera GetBoxCoverUiCamera()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                return canvas.worldCamera;
            return null;
        }

        private void OnEnable()
        {
            StopAdShowAnim();
            // exitPopUp.OnConfirmExitEvent += ExitPopUpConfirmExitButton;
            // exitPopUp.OnCancelExitEvent += ExitPopCloseButton;
        }

        private void OnDisable()
        {
            StopAdShowAnim();
            boxCoverHud?.Hide();
            RestoreGoldTreeRowWithoutHud();
            // exitPopUp.OnConfirmExitEvent -= ExitPopUpConfirmExitButton;
            // exitPopUp.OnCancelExitEvent += ExitPopCloseButton;
        }

        #region Show/Hide

        public override void PlayShowAnimation()
        {

            ShowOrHideAdEffect(false);
            gameObject.SetActive(false);
            gameObject.SetActive(true);
            coinsPanel.Activate();
            HideCoinCollect();
            tintDlg.SetActiveEx(false);
            
            UIController.OnPageOpened(this);

            if (!GameData.BFromStartScene)
            {
                textLevelNumCntLb.gameObject.SetActive(true);
            }
            else
            {
                textLevelNumCntLb.gameObject.SetActive(false);
            }

            RefreshLevelCellCntLb();
            RefreshUnlcokUI();
            RefreshGoldTreeUI();
            RefreshGmDebugUI();
            BoxCoverManager.RefreshHud();
            IQChallengeManager.RefreshHud();
        }

        public void RefreshGmDebugUI()
        {
            // SettingModule is null when GameScene opens via Level Editor Test Level (no GameApp boot).
            bool showGmPass = GameModule.Setting != null && GameModule.Setting.GmLevelPassGlobalSwitch;
            gmLevelPassBtn.SetActiveEx(showGmPass);
            UITestDynamicLevel.RefreshActive();
        }

        public void RefreshGoldTreeUI()
        {
            EnsureGoldTreeGameUI();
            if (goldTreeGameUI == null)
            {
                Debug.LogError("[GoldTree] goldTreeNode / UIGoldTreeGame missing on UI Game prefab. Run Tile2/GoldTree/Wire Game goldTreeNode Only.");
                return;
            }
            goldTreeGameUI.HideAndReset();

            if (!GameData.BFromStartScene)
            {
                levelNumberText.SetActiveEx(true);
                return;
            }

            if (GameData.GoldTreeActData != null && GameData.GoldTreeActData.GetBActOpen())
            {
                levelNumberText.SetActiveEx(false);
            }
            else
            {
                levelNumberText.SetActiveEx(true);
            }
        }

        void EnsureGoldTreeGameUI()
        {
            if (goldTreeGameUI != null)
                return;

            var searchRoot = safeAreaRectTransform != null ? safeAreaRectTransform : transform;
            var node = FindChildDeep(searchRoot, "goldTreeNode");
            if (node != null)
            {
                goldTreeGameUI = node.GetComponent<UIGoldTreeGame>();
                if (goldTreeGameUI == null)
                    Debug.LogError("[GoldTree] goldTreeNode exists but UIGoldTreeGame component is missing.");
            }
        }

        static Transform FindChildDeep(Transform root, string nodeName)
        {
            if (root == null)
                return null;
            if (root.name == nodeName)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindChildDeep(root.GetChild(i), nodeName);
                if (found != null)
                    return found;
            }

            return null;
        }

        public void LayoutGoldTreeForSpecialDock(bool raised)
        {
            goldTreeSpecialDockRaised = raised;
            EnsureGoldTreeGameUI();
            goldTreeGameUI?.SetRaisedForSpecialDock(raised);
            if (raised)
                SyncGoldTreeRowToBoxCoverHud();
        }

        void RestoreGoldTreeRowWithoutHud()
        {
            EnsureGoldTreeGameUI();
            goldTreeGameUI?.SetRaisedForSpecialDock(goldTreeSpecialDockRaised);
        }

        void SyncGoldTreeRowToBoxCoverHud()
        {
            if (boxCoverHud == null || !boxCoverHud.gameObject.activeInHierarchy)
                return;

            Canvas.ForceUpdateCanvases();
            EnsureGoldTreeGameUI();
            if (goldTreeGameUI == null || !boxCoverHud.TryGetRowCenterWorldY(out float worldY))
                return;

            goldTreeGameUI.AlignCenterToWorldY(worldY);
        }

        public void ShowOrHideGoldTree(bool bShow)
        {
            EnsureGoldTreeGameUI();
            if (goldTreeGameUI == null)
            {
                Debug.LogError("[GoldTree] ShowOrHideGoldTree failed: goldTreeGameUI is null.");
                return;
            }

            goldTreeGameUI.HideAndReset();
            if (bShow)
                goldTreeGameUI.ShowDefaultPer();
        }

        public void ShowGoldTreeForCollect(int flyCount)
        {
            EnsureGoldTreeGameUI();
            if (goldTreeGameUI == null)
                return;
            goldTreeGameUI.ShowForCollect(flyCount);
        }

        private void RefreshUnlcokUI()
        {
            if (LevelDirector.LoadedLevelIndex == 0)
            {
                exitButton.SetActiveEx(false);
#if UNITY_IOS
                settingButton.SetActiveEx(true);
#else
                settingButton.SetActiveEx(false);
#endif
            }
            else
            {
                exitButton.SetActiveEx(true);
                settingButton.SetActiveEx(true);
            }
        }

        private void RefreshLevelCellCntLb()
        {
            textLevelNumCntLb.text = LevelDirector.Level.GetLevelCurCntStr();
        }

        public override void PlayHideAnimation()
        {
            HideCoinCollect();
            coinsPanel.Hide();
            StopAdShowAnim();
            levelNumberText.SetActiveEx(false);
            UIController.OnPageClosed(this);
        }

        public void PlayCoinCollectThen(Action onDone)
        {
            int count = LevelDirector.CoinRewardTileTotalCount;

            if (coinCollect == null || coinsPanel == null)
            {
                GrantCoinReward(count);
                onDone?.Invoke();
                return;
            }

            UIQuickVictory.HideAll();
            if (coinCollectNumLb != null)
            {
                coinCollectNumLb.text = "x" + count;
            }

            coinCollect.SetActiveEx(true);
            var rewardsClip = AudioController.AudioClips?.coinlevel_rewards;
            if (rewardsClip != null)
                AudioController.PlaySound(rewardsClip);
            PlayCoinCollectAnim();

            bool finished = false;
            void Finish()
            {
                if (finished)
                    return;
                finished = true;
                coinsPanel.SetAutoGainAnim(true);
                onDone?.Invoke();
            }

            float animLen = GetCoinCollectAnimLength();
            Utility.DGDelayedCall(animLen, () =>
            {
                Transform flyFrom = coinCollectIcon != null ? coinCollectIcon : coinCollect.transform;
                HideCoinCollect();
                coinsPanel.SetAutoGainAnim(false);
                AudioController.PlaySound(AudioController.AudioClips.coinAdd);
                coinsPanel.PlayCoinGainBatched(count, GrantCoinReward);
                coinsPanel.PlayCoinFly(Finish, flyFrom);
            });
            // 飞金币回调丢失时仍能进结算，避免金币关通关后卡死
            Utility.DGDelayedCall(animLen + 6f, Finish);
        }

        const float CoinCollectFallbackDuration = 1.4f;

        void PlayCoinCollectAnim()
        {
            var anim = coinCollect.GetComponent<Animator>();
            if (anim == null)
            {
                return;
            }

            anim.Rebind();
            anim.Play("CoinCollect", 0, 0f);
        }

        float GetCoinCollectAnimLength()
        {
            var anim = coinCollect.GetComponent<Animator>();
            var clips = anim != null ? anim.runtimeAnimatorController?.animationClips : null;
            if (clips != null)
            {
                for (int i = 0; i < clips.Length; i++)
                {
                    if (clips[i] != null && clips[i].name == "CoinCollect")
                    {
                        return Mathf.Max(clips[i].length, 0.1f);
                    }
                }
            }

            return CoinCollectFallbackDuration;
        }

        void HideCoinCollect()
        {
            coinCollect.SetActiveEx(false);
        }

        static void GrantCoinReward(int amount)
        {
            if (amount <= 0 || !GameData.BFromStartScene)
            {
                return;
            }

            GPUserDataManager.Instance.AddCoin(amount);
        }

        public void UpdateLevelNumber(int levelNumber)
        {
            levelNumberText.UpdateLevelNumber(levelNumber);
        }

        public void UpdateDebugLevel(int levelNumber)
        {
            if (debugLevelNumberText == null)
            {
                return;
            }

            if (!GameData.BFromStartScene)
            {
                debugLevelNumberText.text = levelNumber > 0 ? ("real " + levelNumber) : "";
                return;
            }

            if (GameModule.Setting != null && GameModule.Setting.GmMode)
            {
                debugLevelNumberText.text = "real " + levelNumber;
            }
            else
            {
                debugLevelNumberText.text = "";
            }
        }

        #endregion

        public void ShowExitPopUp()
        {
            // 检查是否有棋子正在移动或消除动画未结束，如果有则阻止返回主菜单
            if (LevelDirector.IsLevelLoaded && LevelDirector.Dock != null)
            {
                // 检查棋子是否还没到达目的地
                if (LevelDirector.Dock.HasActiveSlotMotion())
                {
                    // 棋子正在移动，无法返回主菜单
                    return;
                }

                // 检查消除动画是否结束
                if (LevelDirector.Dock.IsClearingAnimation())
                {
                    // 消除动画未结束，无法返回主菜单
                    return;
                }

                // 终止所有动画，包括协程、DOTween动画和延迟调用
                LevelDirector.Dock.StopAllAnimations();
            }

            //保存关卡数据
            RecoverLevelData.SaveCurrentLevelData();
            GameBAData.ExcuteMainStage(4);
            ExitPopUpConfirmExitButton();
            GPSDKMgr.Instance.HideADBanner();
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);

            GameDirector.RecordLastPlayTime(true);
            StopAdShowAnim();
        }

        protected override bool OnEscButtonPressed()
        {
            ShowExitPopUp();
            return true;
        }

        public void ExitPopCloseButton()
        {
            // exitPopUp.Hide();
        }

        public void ExitPopUpConfirmExitButton()
        {
            UIController.HidePage<UIGame>();

            GameDirector.ReturnToMenu();

            // exitPopUp.Hide();
        }

        public void ShowUISettings()
        {
            UIController.ShowPage<UISettings>();
            TileGameCore.AudioController.PlaySound(TileGameCore.AudioController.AudioClips.buttonSound);
        }

        #region Development
#if UNITY_EDITOR
        UITestDynamicLevel mDynamicLevelHotkeys;

        void Update()
        {
            // 仅对局页显示时生效，避免 Canvas 关闭后 Update 仍跑导致主界面误触
            if (!IsPageDisplayed)
            {
                return;
            }

            // 不依赖 GameSettings.gmMode；GM 面板关闭时快捷键仍可用
            // 项目启用了新 Input System，必须用 Keyboard.current
            var keyboard = Keyboard.current;
            if (keyboard == null || GameDirector.levelController == null)
            {
                return;
            }

            if (IsTypingInInputField())
            {
                return;
            }

            if (mDynamicLevelHotkeys == null)
            {
                mDynamicLevelHotkeys = GetComponentInChildren<UITestDynamicLevel>(true);
            }

            // 下一关：↓
            if (keyboard.downArrowKey.wasPressedThisFrame)
            {
                if (mDynamicLevelHotkeys != null)
                {
                    mDynamicLevelHotkeys.HandleChooseButtonTap();
                }
                else
                {
                    GotoNextLevelDev();
                }
                return;
            }

            // 上一关：↑
            if (keyboard.upArrowKey.wasPressedThisFrame)
            {
                if (mDynamicLevelHotkeys != null)
                {
                    mDynamicLevelHotkeys.OnBtnLastClick();
                }
                else
                {
                    GotoPrevLevelDev();
                }
                return;
            }

            bool shiftHeld = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            if (!shiftHeld)
            {
                return;
            }

            // 关卡难度切换为 win4：Shift + 4
            if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame)
            {
                if (mDynamicLevelHotkeys != null)
                {
                    mDynamicLevelHotkeys.HandleWinButtonTap(4);
                }
                else
                {
                    ApplyWin4DifficultyHotkey();
                }
            }
        }

        static void ApplyWin4DifficultyHotkey()
        {
            if (GameDirector.levelController == null)
            {
                return;
            }

            var levelIndex = GameDirector.levelController.GetLoadedLevelIndex();
            var cfg = LevelDynamicConfigProvider.GetLevelConfig(levelIndex + 1);
            if (cfg == null || string.IsNullOrEmpty(cfg.Win4))
            {
                Debug.LogWarning($"[GM Hotkey] Level {levelIndex + 1} Win4 config missing");
                return;
            }

            GameDirector.LoadLevel(levelIndex, null, cfg.Win4, forceOpen: true);
        }

        static bool IsTypingInInputField()
        {
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            var selected = eventSystem.currentSelectedGameObject;
            if (selected == null)
            {
                return false;
            }

            return selected.GetComponent<InputField>() != null
                || selected.GetComponent<TMP_InputField>() != null;
        }
#endif

        public void RestartLevelDev()
        {
            GameDirector.ReplayLevel();
            UIController.DisablePage<UIGame>();
            UIController.ShowPage<UIGame>();
        }

        public void OnDevLevelFieldChanged(string newLevel)
        {
            int level = -1;

            if (int.TryParse(newLevel, out level))
            {
                LevelArchive levelSave = SaveController.GetSaveObject<LevelArchive>("level");
                levelSave.DisplayLevelIndex = Mathf.Clamp((level - 1), 0, int.MaxValue);
                levelSave.RealLevelIndex = levelSave.DisplayLevelIndex;

                GameDirector.ReplayLevel();

                UIController.DisablePage<UIGame>();
                UIController.ShowPage<UIGame>();
            }
        }

        public void GotoPrevLevelDev()
        {
            LevelArchive levelSave = SaveController.GetSaveObject<LevelArchive>("level");
            levelSave.DisplayLevelIndex = Mathf.Clamp(levelSave.DisplayLevelIndex - 1, 0, int.MaxValue);
            levelSave.RealLevelIndex = levelSave.DisplayLevelIndex;

            GameDirector.ReplayLevel();
            UIController.DisablePage<UIGame>();
            UIController.ShowPage<UIGame>();
        }

        public void GotoNextLevelDev()
        {
            LevelArchive levelSave = SaveController.GetSaveObject<LevelArchive>("level");
            levelSave.DisplayLevelIndex = levelSave.DisplayLevelIndex + 1;
            levelSave.RealLevelIndex = levelSave.DisplayLevelIndex;

            GameDirector.ReplayLevel();

            UIController.DisablePage<UIGame>();
            UIController.ShowPage<UIGame>();
        }

        public void OnGmLevelPassBtnClick()
        {
            if (GameModule.Setting == null || !GameModule.Setting.GmLevelPassGlobalSwitch)
            {
                return;
            }

            GameDirector.levelController.OnMatchCompleted(true);
        }

        public void FinishLevelDev()
        {
            StartCoroutine(CompleteCoroutine());
        }

        private IEnumerator CompleteCoroutine()
        {
            while (GameDirector.IsGameActive)
            {
                PUDirector.UsePowerUp(PUType.Hint);
                yield return new WaitForSeconds(0.2f);
            }

        }

        private DG.Tweening.Tween m_adEffTween1;
        private DG.Tweening.Tween m_adEffTween2;
        private DG.Tweening.Tween m_adEffTween3;
        private DG.Tweening.Tween m_adEffTween4;

        public void ShowOrHideAdEffect(bool b, bool bForce = true)
        {
            if (!GameData.BFromStartScene)
            {
                return;
            }

            StopAdShowAnim();
            string adKey = "SideBuyItem" + 1;
            if (b)
            {
                adEffectNode.SetActive(true);
                GameBAData.ExcuteAdImpress(adKey);
                PlayShowAdEffectAnim();
            }
            else
            {
                if (bForce == true)
                {
                    adEffectNode.SetActive(false);
                }
                else
                {
                    PlayHideAdEffectAnim();
                }
            }
        }

        private void StopAdShowAnim()
        {
            m_adEffTween1?.Kill();
            m_adEffTween1 = null;
            m_adEffTween2?.Kill();
            m_adEffTween2 = null;
            m_adEffTween3?.Kill();
            m_adEffTween3 = null;
            m_adEffTween4?.Kill();
            m_adEffTween4 = null;
        }

        //显示侧边道具
        public void PlayShowAdEffectAnim()
        {
            adEffectBar.fillAmount = 0f;
            StopAdShowAnim();
            GameBAData.ExcuteStageEvent(13);

            var rt = adEffectNode.GetComponent<RectTransform>();
            rt.anchoredPosition = initAdEffPos + new Vector3(-400, 0, 0);
            m_adEffTween1 = rt.DOAnchorPos3D(initAdEffPos, 1f)
                .OnComplete(() =>
                {
                    spine12.AnimationState.SetAnimation(0, "open", false);
                    m_adEffTween4 = adEffectBar.DOFillAmount(1f, 10f).SetUpdate(true)
                        .OnComplete(() => ShowOrHideAdEffect(false, false));
                });

            m_adEffTween3 = DOVirtual.DelayedCall(1.8f, () =>
            {
                spine12.AnimationState.SetAnimation(0, "idle", true);
            });
        }

        public void PlayHideAdEffectAnim()
        {
            StopAdShowAnim();
            var rt = adEffectNode.GetComponent<RectTransform>();
            m_adEffTween2 = DOVirtual.DelayedCall(0.1f, () =>
            {
                m_adEffTween1 = rt.DOAnchorPos3D(initAdEffPos + new Vector3(-400, 0, 0), 1f)
                    .OnComplete(() =>
                    {
                        adEffectNode.SetActive(false);
                        adEffectBar.fillAmount = 0f;
                    });
            });
        }

        //初次提示：关闭遮罩挂在页面根节点，避免随气泡 DOScale 导致点不到
        DG.Tweening.Tween guideCancelEnableTween;
        GameObject guideDismissOverlay;
        UnityEngine.UI.Button guideDismissButton;

        void EnsureGuideDismissOverlay()
        {
            if (guideDismissOverlay != null)
            {
                return;
            }

            // 关掉气泡内旧 cover 的射线，避免挡在错误层级
            if (guideHintObj != null)
            {
                foreach (var graphic in guideHintObj.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                {
                    if (graphic.gameObject.name == "guideCoverBtn")
                    {
                        graphic.raycastTarget = false;
                    }
                }
            }

            guideDismissOverlay = new GameObject("GuideDismissOverlay");
            guideDismissOverlay.layer = gameObject.layer;
            var rt = guideDismissOverlay.AddComponent<RectTransform>();
            rt.SetParent(transform, false);
            rt.SetAsLastSibling();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localPosition = Vector3.zero;

            guideDismissOverlay.AddComponent<CanvasRenderer>();
            var image = guideDismissOverlay.AddComponent<UnityEngine.UI.RawImage>();
            image.color = new Color(0f, 0f, 0f, 0.02f);
            image.raycastTarget = true;

            var canvasRenderer = guideDismissOverlay.GetComponent<CanvasRenderer>();
            canvasRenderer.cullTransparentMesh = false;

            guideDismissButton = guideDismissOverlay.AddComponent<UnityEngine.UI.Button>();
            guideDismissButton.transition = UnityEngine.UI.Selectable.Transition.None;
            guideDismissButton.targetGraphic = image;
            guideDismissButton.onClick.AddListener(GuideButtonCallback);

            guideDismissOverlay.SetActive(false);
        }

        public void ShowOrHideGuide(bool b)
        {
            EnsureGuideDismissOverlay();

            //隐藏UI等
            if (!b)
            {
                safeAreaRectTransform.SetLocalPosX(0);
                powerUpsUIController.SetLocalPosX(0);
            }

            //显示guide 气泡
            guideHintObj.SetActive(b);
            if (b)
            {
                var tr = guideHintObj.transform;
                tr.DOKill();
                tr.localScale = Vector3.zero;
                tr.DOScale(1f, 0.6f).SetEase(Ease.OutBack).SetUpdate(true);

                // 全屏关闭层：独立于气泡缩放，保证任意位置可点
                guideDismissOverlay.transform.SetAsLastSibling();
                guideDismissOverlay.SetActive(true);
            }
            else
            {
                guideCancelEnableTween?.Kill();
                guideCancelEnableTween = null;
                guideHintObj.transform.DOKill();
                guideHintObj.transform.localScale = Vector3.one;
                if (guideDismissOverlay != null)
                {
                    guideDismissOverlay.SetActive(false);
                }
            }

            //显示最后两个slot的轮廓
            var dock = LevelDirector.Dock;
            if (dock != null)
            {
                dock.ShowOrHideGuide(b);
            }

            if (dock != null)
            {
                if (b)
                {
                    dock.transform.SetLocalPosX(-1000);
                }
                else
                {
                    dock.transform.SetLocalPosX(0);
                }
            }

            //captureNode 显示出来 GetCaptureToSprite
            var cap = GameObject.FindObjectOfType<CaptureToSprite>(true);
            if (cap != null)
            {
                cap.gameObject.SetActive(b);
                if (b)
                {
                    cap.CaptureNow(() =>
                    {
                        safeAreaRectTransform.SetLocalPosX(-1000);
                        powerUpsUIController.SetLocalPosX(-1000);
                        if (dock != null)
                        {
                            dock.transform.SetLocalPosX(0);
                        }
                    });
                }
            }
        }


        bool canCancelGuide = false;
        public bool CheckShowGuide()
        {
            if (GameDirector.GetHasShowGuide())
            {
                return false;
            }

            if (LevelDirector.Dock.GetHasThreeSameTypeOccupiedSlot())
            {
                return false;
            }

            //如果dockbehaviour操作时即将到5个禁止触摸,
            //并且等tile飞到上面时,显示guide
            ShowOrHideGuide(true);
            canCancelGuide = false;
            guideCancelEnableTween?.Kill();
            // 与原先一致：出现约1秒后才可点关闭，防止动画中误触
            guideCancelEnableTween = DOVirtual.DelayedCall(1f, () => { canCancelGuide = true; }).SetUpdate(true);
            GameDirector.SetHasShowGuide();
            return true;
        }

        public void GuideButtonCallback()
        {
            if (!canCancelGuide)
            {
                return;
            }

            canCancelGuide = false;
            ShowOrHideGuide(false);
            RaycastController.Enable();
        }

        public void HandleAdButtonTap()
        {
            string adKey = "SideBuyItem" + 1;
            GameBAData.ExcuteAdClick(adKey);
            if (!AdsPlayStrategy.CheckRewardAdsPlay(adKey))
            {
                return;
            }
#if UNITY_EDITOR

#else
            // 广告加载成功，开始播放广告
            UIController.ShowPage<UIAdsLoading>();
            GPSDKMgr.Instance.PlayRewardedAd(adKey, 4);
            PlayHideAdEffectAnim();    
#endif
        }

        #endregion
    }
}
