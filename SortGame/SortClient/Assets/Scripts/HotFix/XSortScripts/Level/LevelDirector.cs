using System;
using System.Collections.Generic;
using XSortGameCore;
using UnityEngine;
using UnityEngine.UI;
using FishFramework;

namespace FishXSortPlay
{
    public class LevelDirector : MonoBehaviour
    {
        private static LevelDirector instance;

        [SerializeField] LevelCatalog database;
        [SerializeField] LevelSpawnAnimation levelSpawnAnimation;

        [Space]
        [SerializeField] LevelFitter levelScaler;
        [SerializeField] GameObject levelObject;
        [SerializeField] GameObject layersParentObject;
        [SerializeField] DockActor dock;
        //测试AB的六格测试 
        [SerializeField] DockAbService dockAbFunc;

        private static bool isLevelLoaded;
        public static bool IsLevelLoaded => isLevelLoaded;

        private static LevelModel level;
        public static LevelModel Level => level;

        private static LevelArchive levelSave;

        public static LevelCatalog Database => instance.database;

        public static int MaxReachedLevelIndex => levelSave.MaxReachedLevelIndex;
        public static int DisplayedLevelIndex => levelSave.DisplayLevelIndex;

        private static int loadedLevelIndex;
        public static int LoadedLevelIndex => loadedLevelIndex;

        public static int loadedLevelTheme;
        public static int LoadedLevelTheme => loadedLevelTheme;

        /// <summary>-1 用关卡主题底板，&gt;=0 为 XSortBaseStyle。</summary>
        public static int loadedXSortBaseStyle = XSortBaseStyleResolver.UseThemeBase;
        public static int LoadedXSortBaseStyle => loadedXSortBaseStyle;

        public static GameObject LevelObject => instance.levelObject;

        private static LevelGenService _levelGenFunc;
        public static LevelGenService LevelGenService => _levelGenFunc;

        private static Dictionary<XSortEffectType, XSortEffect> effectsLink;

        //奇偶尺寸
        public static Vector2Int EvenLayerSize => new(Level.GetLayer(Level.AmountOfLayers - 1).GetRow(0).AmountOfCells, Level.GetLayer(Level.AmountOfLayers - 1).AmountOfRows);
        public static Vector2Int OddLayerSize => new(Level.GetLayer(Level.AmountOfLayers - 2).GetRow(0).AmountOfCells, Level.GetLayer(Level.AmountOfLayers - 2).AmountOfRows);
        public static bool IsEvenLayerBigger => EvenLayerSize.x > OddLayerSize.x;

        public static int CurrentReward => GetCurrentLevelReward();
        public static DockActor Dock => instance.dock;
        public static DockAbService DockAb => instance.dockAbFunc;
        public static BackgroundActor Background { get; private set; }

        private static bool isCustomLevel;
        public static bool IsCustomLevel => isCustomLevel;

        public static bool IsRaycastEnabled { get; set; } = true;
        private static bool isBusy;
        public static bool IsBusy => isBusy;
        private static bool isReviving;

        public const string CoinRewardSkinName = "CoinReward";
        public static bool BInCoinRewardLevel = false;
        public static XSortModel CoinRewardXSortData { get; private set; }
        /// <summary>本关生成的金币棋子总数（通关结算 Coin Collect 展示用）。</summary>
        public static int CoinRewardXSortTotalCount { get; private set; }

        public static bool IsCoinRewardXSort(XSortModel XSortData)
        {
            return XSortData != null && CoinRewardXSortData != null && XSortData == CoinRewardXSortData;
        }

        public static bool IsCoinRewardSkin(string skinName)
        {
            return skinName == CoinRewardSkinName;
        }

        public static void SetCoinRewardXSortData(XSortModel XSortData)
        {
            CoinRewardXSortData = XSortData;
        }

        public static void SetCoinRewardXSortTotalCount(int count)
        {
            CoinRewardXSortTotalCount = Mathf.Max(0, count);
        }

        public static void ClearCoinRewardState()
        {
            BInCoinRewardLevel = false;
            CoinRewardXSortData = null;
            CoinRewardXSortTotalCount = 0;
        }

        static int s_colorChallengeMarkIndex = int.MinValue;
        static bool s_colorChallengeMarkActive;

        public static void MarkColorChallenge(int levelIndex, bool active)
        {
            s_colorChallengeMarkIndex = levelIndex;
            s_colorChallengeMarkActive = active;
        }

        /// <summary>
        /// 本关生成之后是否真的走了同色。提示界面用这个，避免池子不够退回普通关时仍显示颜色关。
        /// </summary>
        public static bool UsesColorChallenge(int levelIndex)
        {
            if (s_colorChallengeMarkIndex == levelIndex)
            {
                return s_colorChallengeMarkActive;
            }

            return IsColorChallengeLevel(levelIndex);
        }

        public static bool IsColorChallengeLevel(int levelIndex)
        {
            if (!ActivityData.IsColorChallengeEnabled())
            {
                return false;
            }

            var cfg = LevelDynamicConfigProvider.GetLevelConfig(levelIndex + 1);
            if (cfg == null || string.IsNullOrEmpty(cfg.Colorchallenge))
            {
                return false;
            }

            int need = LevelGenService.GetActivePieceCount(levelIndex + 1);
            if (need <= 0)
            {
                return true;
            }

            return XSortSkinCatalog.CountColorChallengeSkins(cfg.Colorchallenge, levelIndex + 1) >= need;
        }

        /// <summary>
        /// GameLevel.iqchallenge = 1 的关卡。关卡号与金币关同一套：levelIndex + 1。
        /// </summary>
        public static bool IsIQChallengeLevelIndex(int levelIndex)
        {
            if (levelIndex < 0)
                return false;

            if (ConfigHelper.Tables == null)
            {
#if UNITY_EDITOR
                ConfigHelper.Initialize();
#else
                return false;
#endif
            }

            var cfg = LevelDynamicConfigProvider.GetLevelConfig(levelIndex + 1);
            return cfg != null && cfg.Iqchallenge == 1;
        }

        public static bool IsCoinRewardLevelIndex(int levelIndex)
        {
            if (!ActivityData.IsCoinRewardLevelEnabled())
            {
                return false;
            }

            var cfg = LevelDynamicConfigProvider.GetLevelConfig(levelIndex + 1);
            return cfg != null && cfg.Coinlevel == 1;
        }

        public static bool ResolveCoinRewardLevel(int levelIndex)
        {
            BInCoinRewardLevel = IsCoinRewardLevelIndex(levelIndex);
            if (!BInCoinRewardLevel)
            {
                CoinRewardXSortData = null;
                CoinRewardXSortTotalCount = 0;
            }

            return BInCoinRewardLevel;
        }

        /// <summary>
        /// 金币关 75/25 拆分（按 3 对齐）。
        /// </summary>
        public static void GetCoinRewardSplit(int totalXSorts, out int coinXSortCount, out int otherGroupCount)
        {
            if (totalXSorts <= 0)
            {
                coinXSortCount = 0;
                otherGroupCount = 0;
                return;
            }

            int coinCount = Mathf.CeilToInt(totalXSorts * 0.75f);
            coinCount = Mathf.CeilToInt(coinCount / 3f) * 3;
            coinCount = Mathf.Min(coinCount, totalXSorts);
            coinCount = coinCount / 3 * 3;
            otherGroupCount = (totalXSorts - coinCount) / 3;
            coinXSortCount = coinCount;
            Debug.Assert(coinXSortCount % 3 == 0);
            Debug.Assert(coinXSortCount + otherGroupCount * 3 <= totalXSorts);
        }

        public static int WinState { get; set; }
        /// <summary>最近一次通关是否由 GM 直接完成；活动进度与正式胜场统计必须排除。</summary>
        public static bool LastCompletionWasGM { get; private set; }

        /// <summary>棋盘与 dock / 底部技能栏的最小间隙。</summary>
        private const float BoardUiClearanceGap = 0.3f;
        /// <summary>棋盘左右离屏幕边缘留出的世界单位。</summary>
        private const float BoardSideClearance = 0.15f;
        /// <summary>视觉上棋盘略偏上，在 dock 与技能按钮的正中再往下挪这么多。</summary>
        private const float BoardDownNudge = 0.25f;
        /// <summary>自动缩放下限；装不下时最多缩到该比例，避免棋子过小。</summary>
        private const float BoardFitMinScale = 0.95f;

        private void Awake()
        {
            instance = this;
        }

        public Vector3 GetShowDelta()
        {
            return new Vector3(level.ShowLayerDeltaX, level.ShowLayerDeltaY, 0);
        }

        public float GetShowScale()
        {
            return level.ShowLayerScale;
        }

        private void ResetLayersParentTransform()
        {
            if (layersParentObject == null)
            {
                return;
            }

            layersParentObject.transform.localScale = Vector3.one;
            layersParentObject.transform.position = levelScaler.LevelFieldCenter;
        }

        void AdaptBoxCoverPlayfieldIfNeeded(UIGame gameUI)
        {
            if (dock == null)
            {
                return;
            }

            if (level == null || layersParentObject == null)
            {
                dock.RestoreAuthoredPosition();
                FitBoardBetweenDockAndPowerUps();
                return;
            }

            if (AwardColorEggCoverManager.HasActiveLevel
                || level.HasBoxCovers
                || IQChallengeManager.HasActiveLevel)
            {
                BoxCoverLayout.ApplyPlayfield(
                    dock,
                    layersParentObject.transform,
                    levelScaler,
                    gameUI);
                if (IQChallengeManager.HasActiveLevel)
                    IQChallengeManager.RefreshHud();
                return;
            }

            dock.RestoreAuthoredPosition();
            FitBoardBetweenDockAndPowerUps();
        }

        /// <summary>
        /// 按 dock 底边与底部技能按钮顶边构成的可用带，缩放并居中棋盘（含 X 视觉居中）。
        /// </summary>
        private void FitBoardBetweenDockAndPowerUps()
        {
            if (dock == null || layersParentObject == null || _levelGenFunc == null || levelScaler == null)
            {
                return;
            }

            var XSorts = _levelGenFunc.ActXSorts;
            if (XSorts == null || XSorts.Count == 0)
            {
                return;
            }

            Transform parent = layersParentObject.transform;
            parent.localScale = Vector3.one;
            parent.position = levelScaler.LevelFieldCenter;

            // 统一用棋子坐标 + XSortSize，不依赖登场前后 Renderer/scale 状态
            if (!TryGetBoardLogicalWorldBounds(XSorts, out float boardMinX, out float boardMaxX, out float boardMinY, out float boardMaxY))
            {
                return;
            }

            float boardHeight = boardMaxY - boardMinY;
            if (boardHeight <= 0.001f)
            {
                return;
            }

            float dockBottom = dock.GetSlotsBottomWorldY();
            float skillTop = GetPowerUpsTopWorldY();
            float fieldBottom = levelScaler.LevelFieldCenter.y - levelScaler.LevelFieldSize.y * 0.5f;

            // 技能顶边测高了会导致可用带为负并直接放弃适配；回退到 LevelField 底边
            if (skillTop >= dockBottom - BoardUiClearanceGap * 2f)
            {
                skillTop = fieldBottom;
            }

            // 中心在 dock 底和技能顶之间，再往下挪一截，避免棋盘贴着 dock。
            float targetCenterY = (dockBottom + skillTop) * 0.5f - BoardDownNudge;
            float availableHeight = (dockBottom - skillTop) - BoardUiClearanceGap * 2f;
            float boardWidth = boardMaxX - boardMinX;
            float targetCenterX = levelScaler.LevelFieldCenter.x;
            float playWidth = 0f;
            Camera gameCamera = Camera.main;
            if (gameCamera != null
                && TryGetSafePlayWorldRect(gameCamera, out float safeMinX, out float safeMaxX, out _, out _))
            {
                playWidth = ApplySideClearance(safeMaxX - safeMinX);
                targetCenterX = (safeMinX + safeMaxX) * 0.5f;
            }
            else if (gameCamera != null)
            {
                playWidth = ApplySideClearance(gameCamera.orthographicSize * 2f * gameCamera.aspect);
            }

            if (availableHeight <= 0.001f)
            {
                return;
            }

            // 宽、高各自能放到的倍数，取更小的那个。
            // 上限 1：保留策划 ShowLayerScale 调好的尺寸，只在装不下时缩小，不放大填空。
            float heightScale = availableHeight / boardHeight;
            float widthScale = boardWidth > 0.001f && playWidth > 0.001f
                ? playWidth / boardWidth
                : float.PositiveInfinity;
            float scale = Mathf.Min(1f, heightScale, widthScale);
            // 竖向装不下时保留原来的下限，但不能因此重新超出左右。
            if (scale < BoardFitMinScale && heightScale <= widthScale)
                scale = Mathf.Min(1f, widthScale, BoardFitMinScale);

            float boardCenterX = (boardMinX + boardMaxX) * 0.5f;
            float boardCenterY = (boardMinY + boardMaxY) * 0.5f;

            // 先缩放再按父节点枢轴推算中心，避免二次测量与登场动画打架
            var parentPos = parent.position;
            parent.localScale = Vector3.one * scale;
            float scaledCenterX = parentPos.x + (boardCenterX - parentPos.x) * scale;
            float scaledCenterY = parentPos.y + (boardCenterY - parentPos.y) * scale;

            parentPos.x += targetCenterX - scaledCenterX;
            parentPos.y += targetCenterY - scaledCenterY;
            parent.position = parentPos;

#if UNITY_EDITOR
            Debug.Log(
                $"[BoardFit] dockBottom={dockBottom:F2}, skillTop={skillTop:F2}, playWidth={playWidth:F2}, " +
                $"safe={Screen.safeArea}, " +
                $"board=({boardMinX:F2},{boardMaxX:F2})x({boardMinY:F2},{boardMaxY:F2}), " +
                $"target=({targetCenterX:F2},{targetCenterY:F2}), scale={scale:F3}, parent={parent.position}");
#endif
        }

        static float ApplySideClearance(float width)
        {
            return Mathf.Max(0.05f, width - BoardSideClearance * 2f);
        }

        /// <summary>
        /// 把 Screen.safeArea 投到玩法相机。左右不再在这里额外内缩，避免把可用宽度提前吃掉。
        /// </summary>
        static bool TryGetSafePlayWorldRect(
            Camera camera,
            out float minX, out float maxX,
            out float minY, out float maxY)
        {
            minX = maxX = minY = maxY = 0f;
            if (camera == null || Screen.width < 1 || Screen.height < 1)
                return false;

            Rect safe = Screen.safeArea;
            if (safe.width < 1f || safe.height < 1f)
                return false;

            float depth = Mathf.Abs(camera.transform.position.z);
            if (depth < 0.01f)
                depth = 10f;

            Vector3 bottomLeft = camera.ScreenToWorldPoint(new Vector3(safe.xMin, safe.yMin, depth));
            Vector3 topRight = camera.ScreenToWorldPoint(new Vector3(safe.xMax, safe.yMax, depth));
            minX = Mathf.Min(bottomLeft.x, topRight.x);
            maxX = Mathf.Max(bottomLeft.x, topRight.x);
            minY = Mathf.Min(bottomLeft.y, topRight.y);
            maxY = Mathf.Max(bottomLeft.y, topRight.y);
            return maxX - minX > 0.05f && maxY - minY > 0.05f;
        }

        private static bool TryGetBoardLogicalWorldBounds(
            List<XSortActor> XSorts,
            out float minX, out float maxX,
            out float minY, out float maxY)
        {
            minX = float.MaxValue;
            maxX = float.MinValue;
            minY = float.MaxValue;
            maxY = float.MinValue;
            float halfXSortX = LevelFitter.XSortSize.x * 0.5f;
            float halfXSortY = LevelFitter.XSortSize.y * 0.5f;
            bool any = false;

            for (int i = 0; i < XSorts.Count; i++)
            {
                var XSort = XSorts[i];
                if (XSort == null || XSort.IsSubmitted)
                {
                    continue;
                }

                Vector3 p = XSort.transform.position;
                float XSortMinX = p.x - halfXSortX;
                float XSortMaxX = p.x + halfXSortX;
                float XSortMinY = p.y - halfXSortY;
                float XSortMaxY = p.y + halfXSortY;

                if (XSortMinX < minX) minX = XSortMinX;
                if (XSortMaxX > maxX) maxX = XSortMaxX;
                if (XSortMinY < minY) minY = XSortMinY;
                if (XSortMaxY > maxY) maxY = XSortMaxY;
                any = true;
            }

            return any && minX < maxX && minY < maxY;
        }

        private float GetPowerUpsTopWorldY()
        {
            Camera worldCam = Camera.main;
            UIGame gameUI = UIController.GetPage<UIGame>();
            if (worldCam != null && gameUI != null && gameUI.PowerUpsUIController != null
                && gameUI.PowerUpsUIController.TryGetButtonsTopWorldY(worldCam, out float topY))
            {
                return topY;
            }

            return levelScaler.LevelFieldCenter.y - levelScaler.LevelFieldSize.y * 0.5f;
        }

        public void CheckChangeXSortSkin()
        {

        }

        //获得展示的真正宽高
        public Vector2Int GetMinShowSize()
        {
            int maxWidth = 0;
            int maxHeight = 0;

            // 遍历所有层
            for (int layerIndex = 0; layerIndex < level.AmountOfLayers; layerIndex++)
            {
                var layer = level.GetLayer(layerIndex);

                // 计算当前层的最大宽度（包括间隔）
                int layerWidth = 0;
                for (int rowIndex = 0; rowIndex < layer.AmountOfRows; rowIndex++)
                {
                    var row = layer.GetRow(rowIndex);
                    int rowWidth = 0;

                    // 找到第一个和最后一个填充的单元格位置
                    int firstFilledIndex = -1;
                    int lastFilledIndex = -1;

                    for (int cellIndex = 0; cellIndex < row.AmountOfCells; cellIndex++)
                    {
                        var cell = row.GetCell(cellIndex);
                        if (cell.IsFilled)
                        {
                            if (firstFilledIndex == -1)
                            {
                                firstFilledIndex = cellIndex;
                            }
                            lastFilledIndex = cellIndex;
                        }
                    }

                    // 如果有填充的单元格，计算从第一个到最后一个填充单元格的距离（包括间隔）
                    if (firstFilledIndex != -1 && lastFilledIndex != -1)
                    {
                        rowWidth = lastFilledIndex - firstFilledIndex + 1;
                    }

                    // 更新当前层的最大宽度
                    if (rowWidth > layerWidth)
                    {
                        layerWidth = rowWidth;
                    }
                }

                // 更新全局最大宽度
                if (layerWidth > maxWidth)
                {
                    maxWidth = layerWidth;
                }

                // 更新全局最大高度（层数）
                maxHeight = Mathf.Max(maxHeight, layer.AmountOfRows);
            }

            return new Vector2Int(maxWidth, maxHeight);
        }

        public void Init()
        {
            LevelFitter levelScaler = GetComponent<LevelFitter>();
            levelScaler.Init();

            if (database == null)
            {
                Debug.LogError(string.Format(
                    "[LevelDirector] 当前关卡平台 {0} 的数据库为空，请通过关卡编辑器平台切换或 AB 打包前同步 GameScene 的 database 引用。",
                    LevelPlatformResolver.GetDisplayName()));
                return;
            }

            database.Init();
            dock.Init(this);

            levelSave = SaveController.GetSaveObject<LevelArchive>("level");

            RaycastController raycastController = gameObject.AddComponent<RaycastController>();
            raycastController.Init();

            // Initialise special effects
            effectsLink = new Dictionary<XSortEffectType, XSortEffect>();
            XSortEffect[] availableEffects = database.XSortEffects;
            for (int i = 0; i < availableEffects.Length; i++)
            {
                if (effectsLink.ContainsKey(availableEffects[i].EffectType))
                {
                    Debug.LogWarning(string.Format("XSort effect with type {0} has duplicates in the database!", availableEffects[i].EffectType));

                    continue;
                }

                effectsLink.Add(availableEffects[i].EffectType, availableEffects[i]);
            }

            LoadBackground();
        }

        private void OnDestroy()
        {

            dock.Unload();
            database.Unload();
            _levelGenFunc = null;
            effectsLink = null;
            loadedLevelIndex = -1;
            isLevelLoaded = false;
            isBusy = false;
        }



        private void RefreshLevel(SimpleCallback onLevelLoaded)
        {
            isLevelLoaded = true;
            if (LevelDifficultySim.IsRunning)
                RaycastController.Disable();
            else
                RaycastController.Enable();
            isBusy = false;
            isReviving = false;
            onLevelLoaded?.Invoke();
            _levelGenFunc.UpdateClickableStates(withAnimation: !LevelDifficultySim.SkipPresentation);
            IQChallengeManager.NotifyBoardReady();
        }

        public static void CompleteCustomLevel()
        {
            if (_levelGenFunc != null)
            {
                UnloadLevel();
            }
            isCustomLevel = false;
        }

        public int GetLoadedLevelIndex()
        {
            return loadedLevelIndex;
        }

        //是否显示Banner提取
        private void CheckShowBanner(int levelIndex)
        {
            // Editor 下无 Android 广告/归因 native 环境，走 AttrSdk、AdSdk 易触发 Unity 编辑器进程崩溃；真机保留原逻辑。
#if UNITY_EDITOR
            return;
#endif
            //设置媒介类型
            GPUserDataManager.Instance.CheckSetMediaSourceType();

            // 插屏加载改由 GameDirector.LoadLevel 进关前统一判定；此处只保证激励已发起过加载。
            GPSDKMgr.Instance.CheckLoadAllAd();

            //广告加载
            if (GPUserDataManager.Instance.GetBNoAd())
            {
                GPSDKMgr.Instance.HideADBanner();
                return;
            }

            //奖励渠道加载完直接就显示,非奖励渠道第五关开始显示.
            if (GPSDKMgr.Instance.GetBIsMediaSourceType1() || levelIndex >= 4)
            {
                GPSDKMgr.Instance.ShowADBanner();
                return;
            }
        }

        void SetTheme(RecoverLevelData recover = null)
        {
            loadedLevelTheme = XSortBaseThemeResolver.ResolveCurrentLevelXSortBaseId();
            if (recover != null && recover.XSortBaseStyleSet && RecoverLevelData.HasLayoutProgress())
            {
                loadedXSortBaseStyle = RestoreXSortBaseStyle(recover.XSortBaseStyleName);
                return;
            }

            loadedXSortBaseStyle = XSortBaseStyleResolver.ResolveForCurrentLevel();
        }

        static int RestoreXSortBaseStyle(string styleName)
        {
            if (string.IsNullOrEmpty(styleName))
            {
                return XSortBaseStyleResolver.UseThemeBase;
            }

            int index = XSortBaseStyleTable.IndexOf(styleName);
            return index >= 0 ? index : XSortBaseStyleResolver.ResolveForCurrentLevel();
        }

        /// <summary>
        /// 加载指定关卡
        /// </summary>
        /// <param name="levelIndex">要加载的关卡索引</param>
        /// <param name="onLevelLoaded">关卡加载完成后的回调函数</param>
        public void LoadLevel(int levelIndex, SimpleCallback onLevelLoaded = null, string dynamicStr = "", int baLoadState = 0)
        {
            RecoverLevelData.ResetLayoutProgress();
            CheckShowBanner(levelIndex);

            // 如果当前已有关卡加载，先卸载它
            if (_levelGenFunc != null)
            {
                UnloadLevel();
            }

            // 确定实际要加载的关卡索引（处理随机关卡逻辑）
            int realLevelIndex;

#if UNITY_EDITOR
            // Test Level / 预跑: same generation path as normal StartScene entry (exact index + new layout/skins).
            bool useNormalLevelGeneration = GameData.BFromStartScene || GameDirector.AutoRunLevelInEditor || LevelDifficultySim.SkipPresentation;
#else
            bool useNormalLevelGeneration = GameData.BFromStartScene;
#endif

            if (useNormalLevelGeneration)
            {
                realLevelIndex = levelIndex;
            }
            else
            {
                // 如果正在玩随机关卡且显示关卡索引匹配且真实关卡索引有效，则使用保存的真实关卡索引
                if (levelSave.IsPlayingRandomLevel && levelIndex == levelSave.DisplayLevelIndex && levelSave.RealLevelIndex != -1)
                {
                    realLevelIndex = levelSave.RealLevelIndex;
                }
                else
                {
                    // 从数据库中获取随机关卡索引
                    realLevelIndex = database.GetRandomLevelIndex(levelIndex, levelSave.LastPlayerLevelIndex, false);

                    // 更新保存的关卡索引信息
                    levelSave.LastPlayerLevelIndex = realLevelIndex;
                    levelSave.RealLevelIndex = realLevelIndex;

                    // 如果真实关卡索引与显示关卡索引不同，标记为正在玩随机关卡
                    if (realLevelIndex != levelIndex)
                    {
                        levelSave.IsPlayingRandomLevel = true;
                    }
                }
                // 保存游戏状态
                SaveController.Save();
            }


            // 更新关卡状态信息
            levelSave.DisplayLevelIndex = levelIndex;  // 设置显示的关卡索引
            loadedLevelIndex = levelIndex;             // 设置已加载的关卡索引
            isBusy = true;                             // 设置忙碌状态，防止重复操作
            WinState = 1;
            ClearCoinRewardState();
            ResolveCoinRewardLevel(loadedLevelIndex);
            SetTheme();
            if (!LevelDifficultySim.SkipPresentation)
                dock.ApplyTheme();

            // 从数据库获取关卡数据（分块缓存；未就绪时同步兜底）
            level = database.FetchLevelData(realLevelIndex);
            if (level == null)
            {
                LevelChunkLoader.EnsureChunkSync(realLevelIndex);
                level = database.FetchLevelData(realLevelIndex);
            }

            if (level == null)
            {
                Debug.LogError(string.Format(
                    "[LevelDirector] LoadLevel failed: LevelModel missing at index {0} (path {1})",
                    realLevelIndex,
                    LevelChunkLoader.GetLevelPath(realLevelIndex)));
                isBusy = false;
                return;
            }

            UIGame gameUI = null;
            if (!LevelDifficultySim.SkipPresentation)
            {
                gameUI = UIController.GetPage<UIGame>();
                gameUI.PowerUpsUIController.OnLevelStarted(levelIndex);

                int showIndex = useNormalLevelGeneration && !GameData.BFromStartScene
                    ? levelIndex
                    : GPUserDataManager.Instance.GetShowLevelNum();
                gameUI.UpdateLevelNumber(showIndex + 1);

#if UNITY_EDITOR
                gameUI.UpdateDebugLevel(levelIndex + 1);
#else
                gameUI.UpdateDebugLevel(-1);
#endif
            }

            // 激活关卡对象
            levelObject.SetActive(true);

            // 重新计算关卡缩放和位置
            levelScaler.Recalculate();
            ResetLayersParentTransform();

            // 初始化关卡表示对象
            _levelGenFunc = new LevelGenService(level, layersParentObject);


            bool isFirstEnter = false;
            if (useNormalLevelGeneration)
            {
                if (string.IsNullOrEmpty(dynamicStr))
                {
                    dynamicStr = _levelGenFunc.GetWinFailLevelCfg();
                }

                if (GameData.BFromStartScene && !LevelDifficultySim.SkipPresentation)
                {
                    isFirstEnter = GPUserDataManager.Instance.GetLevelPlayCount(loadedLevelIndex) == 0;
                    //进入关卡记录
                    GPUserDataManager.Instance.RecordLevelEntry(loadedLevelIndex);
                }
                else
                {
                    // Level Editor Test Level / 预跑: force first-enter so XSortsNew mandatory skins apply.
                    isFirstEnter = true;
                }
            }

            _levelGenFunc.ParseDynamicConfig(dynamicStr);

            LevelDifficultySeed.ApplyPendingInitState();

            // 准备要在关卡中放置的棋子类型数据
            XSortModel[] availableObjects = database.AvailableForLevel(
                level,
                _levelGenFunc.GetMaxPieceNumber(),
                isFirstEnter
            );

            void SpawnAndPresent()
            {
                // 生成关卡中的所有棋子对象
                _levelGenFunc.SpawnObjects(availableObjects);
                AwardColorEggCoverManager.Load(level, layersParentObject);
                BoxCoverManager.Load(level, layersParentObject);
                IQChallengeManager.Load(level);
                // 特殊玩法在 ApplyTheme 之后才 Load，需再刷一次备战栏底板
                if (!LevelDifficultySim.SkipPresentation)
                {
                    dock.ApplyTheme();
                }
                _levelGenFunc.RefreshStackOccludedEffectVisibility();

                if (LevelDifficultySim.SkipPresentation)
                {
                    FinishLoadForSim(onLevelLoaded);
                    return;
                }

                RaycastController.Disable();

                if (GameData.BFromStartScene)
                {
                    GameEvent.Send((int)EventNameDef.LoadLevelAnimFinish);
                }


                // 播放码头出现动画 
                dock.PlayAppearAnimation();
                // 加载背景（内部会 Init 相机，须在适配前完成）
                LoadBackground();
                Canvas.ForceUpdateCanvases();
                // 登场动画开始前只适配一次；动画结束后不再改位置，避免整盘抖一下
                AdaptBoxCoverPlayfieldIfNeeded(gameUI);

                // 播放关卡生成动画，动画完成后执行回调
                levelSpawnAnimation.Play(_levelGenFunc, () =>
                {
                    if (!LevelDifficultySeed.IsReplayLoad)
                        RecoverLevelData.SaveCurrentLevelData();

                    BoxCoverManager.NotifyLevelSpawnCompleted();
                    AwardColorEggCoverManager.NotifyLevelSpawnCompleted();
                    _levelGenFunc.UpdateClickableStates();
                    IQChallengeManager.NotifyBoardReady();

                    // Enables raycast via pending ShowLevelTintDlg callback (including Level Editor Test Level).
                    gameUI.OnLevelLayoutFinished();

                    if (GameData.BFromStartScene && !LevelDifficultySeed.IsReplayLoad)
                    {
                        GameBAData.ExcuteMainStage(baLoadState); //常规进入是0 //重新开始倒计时开始时8 //手动重新开始时9
                    }
                });

                GameDirector.MarkLevelGenerated(loadedLevelIndex);
                gameUI.ShowLevelTintDlg(() =>
                {
                    GameDirector.RecordLastPlayTime();
                    RefreshLevel(onLevelLoaded);
                });
            }

            // 颜色关先播独立界面，播完再生成棋子。预跑不播。
            if (!LevelDifficultySim.SkipPresentation && UsesColorChallenge(loadedLevelIndex))
            {
                int enterIndex = loadedLevelIndex;
                string color = LevelDynamicConfigProvider.GetLevelConfig(enterIndex + 1)?.Colorchallenge;
                UIColorChallenge.Play(color, () =>
                {
                    if (_levelGenFunc == null || loadedLevelIndex != enterIndex)
                        return;
                    SpawnAndPresent();
                });
                return;
            }

            SpawnAndPresent();
        }

        void FinishLoadForSim(SimpleCallback onLevelLoaded)
        {
            RaycastController.Disable();

            List<XSortActor> XSorts = _levelGenFunc.ActXSorts;
            for (int i = 0; i < XSorts.Count; i++)
            {
                XSortActor XSort = XSorts[i];
                if (XSort == null)
                    continue;
                XSort.transform.localScale = Vector3.one;
                XSort.SetSpawnTweenVisualsActive(true);
            }

            Physics.SyncTransforms();
            BoxCoverManager.NotifyLevelSpawnCompleted();
            AwardColorEggCoverManager.NotifyLevelSpawnCompleted();
            _levelGenFunc.UpdateClickableStates(false);
            GameDirector.MarkLevelGenerated(loadedLevelIndex);
            RefreshLevel(onLevelLoaded);
        }

        public void LoadLevelFromSave(int levelIndex, SimpleCallback onLevelLoaded = null, string dynamicStr = "", int baLoadState = 0, RecoverLevelData recoverLevelData = null)//从本地存档中加载关卡.
        {
            if (!GameData.BFromStartScene)
            {
                return;
            }

            //道具数量是固定的.
            //恢复dock
            //恢复棋盘
            CheckShowBanner(levelIndex);

            // 如果当前已有关卡加载，先卸载它
            if (_levelGenFunc != null)
            {
                UnloadLevel();
            }

            // 确定实际要加载的关卡索引（处理随机关卡逻辑,只处理正常逻辑.
            int realLevelIndex = 1;
            realLevelIndex = levelIndex;

            // 更新关卡状态信息
            levelSave.DisplayLevelIndex = levelIndex;  // 设置显示的关卡索引
            loadedLevelIndex = levelIndex;             // 设置已加载的关卡索引
            ClearCoinRewardState();
            ResolveCoinRewardLevel(loadedLevelIndex);
            SetTheme(recoverLevelData);
            dock.ApplyTheme();
            isBusy = true;                             // 设置忙碌状态，防止重复操作
            WinState = 1;

            // 从数据库获取关卡数据
            level = database.FetchLevelData(realLevelIndex);
            if (level == null)
            {
                LevelChunkLoader.EnsureChunkSync(realLevelIndex);
                level = database.FetchLevelData(realLevelIndex);
            }

            if (level == null)
            {
                Debug.LogError(string.Format(
                    "[LevelDirector] LoadLevelFromSave failed: LevelModel missing at index {0} (path {1})",
                    realLevelIndex,
                    LevelChunkLoader.GetLevelPath(realLevelIndex)));
                isBusy = false;
                return;
            }

            // 更新游戏UI状态
            UIGame gameUI = UIController.GetPage<UIGame>();
            gameUI.PowerUpsUIController.OnLevelStarted(levelIndex, true);  // 通知道具系统关卡开始


            int showIndex = GPUserDataManager.Instance.GetShowLevelNum();
            gameUI.UpdateLevelNumber(showIndex + 1);                // 更新UI显示的关卡号


#if UNITY_EDITOR
            gameUI.UpdateDebugLevel(levelIndex + 1);
#else
            gameUI.UpdateDebugLevel(-1);
#endif

            // 激活关卡对象
            levelObject.SetActive(true); //显示dock等.
                                         // 重新计算关卡缩放和位置
            levelScaler.Recalculate();
            ResetLayersParentTransform();
            // 初始化关卡表示对象
            _levelGenFunc = new LevelGenService(level, layersParentObject);
            string recoverDynamicStr = recoverLevelData != null && !string.IsNullOrEmpty(recoverLevelData.dynamicStr)
                ? recoverLevelData.dynamicStr
                : dynamicStr;
            if (string.IsNullOrEmpty(recoverDynamicStr))
            {
                recoverDynamicStr = _levelGenFunc.GetWinFailLevelCfg();
            }
            _levelGenFunc.ParseDynamicConfig(recoverDynamicStr);

            if (recoverLevelData != null && recoverLevelData.LevelIndex == levelIndex)
            {
                _levelGenFunc.SpawnObjectsFromSave(recoverLevelData);

                // 恢复dock棋子
                if (recoverLevelData.DockXSortIndices != null && recoverLevelData.DockXSortIndices.Count > 0)
                {
                    foreach (var XSortIdx in recoverLevelData.DockXSortIndices)
                    {
                        SpawnDockXSort(XSortIdx);
                    }
                }

                // 恢复 HighLight 置灰效果（无消耗，不二次扣费）
                if (recoverLevelData.highLightActive)
                {
                    PUHighLightBehavior.RestoreFromSave(true);
                    PUDirector.PowerUpsUIController?.RefreshStates();
                }

                NextDayFirstEnterEase.ApplyBoardEaseIfActive();
            }
            else
            {
                recoverLevelData = null;
                bool isFirstEnter = GPUserDataManager.Instance.GetLevelPlayCount(loadedLevelIndex) == 0;
                XSortModel[] availableObjects = database.AvailableForLevel(
                    level,
                    _levelGenFunc.GetMaxPieceNumber(),
                    isFirstEnter
                    );
                _levelGenFunc.SpawnObjects(availableObjects);
            }

            AwardColorEggCoverManager.Load(level, layersParentObject, recoverLevelData);
            BoxCoverManager.Load(level, layersParentObject, recoverLevelData);
            IQChallengeManager.Load(level, recoverLevelData);
            dock.ApplyTheme();
            _levelGenFunc.RefreshStackOccludedEffectVisibility();

            // 暂时禁用射线检测，防止在动画播放期间误触
            RaycastController.Disable();

            if (GameData.BFromStartScene)
            {
                if (recoverLevelData != null)
                    GameEvent.Send((int)EventNameDef.LoadSaveLevelAnimFinish);
                else
                    GameEvent.Send((int)EventNameDef.LoadLevelAnimFinish);
            }

            // 播放码头出现动画
            dock.PlayAppearAnimation();
            // 加载背景（内部会 Init 相机，须在适配前完成）
            LoadBackground();
            Canvas.ForceUpdateCanvases();
            // 登场动画开始前只适配一次；动画结束后不再改位置，避免整盘抖一下
            AdaptBoxCoverPlayfieldIfNeeded(gameUI);

            // 播放关卡生成动画，动画完成后执行回调
            levelSpawnAnimation.Play(_levelGenFunc, () =>
            {
                BoxCoverManager.NotifyLevelSpawnCompleted();
                AwardColorEggCoverManager.NotifyLevelSpawnCompleted();
                if (GameData.BFromStartScene)
                {
                    gameUI.OnLevelLayoutFinished();
                    GameBAData.ExcuteMainStage(10, recoverLevelData.costTime); //从存档开始游戏是10
                }
            }
            );

            gameUI.ShowLevelTintDlg(() =>
            {
                GameDirector.RecordLastPlayTime();
                RefreshLevel(onLevelLoaded);
                _levelGenFunc.UpdateClickableStates();
            });
        }


        private void LoadBackground(BackgroundModel backgroundData = null)
        {
            levelScaler.Init();

            if (Background != null)
            {
                if (!Background.IsTransitioning)
                {
                    Background.RefreshBg();
                }
                return;
            }

            if (backgroundData == null)
                backgroundData = database.GetLastAvailableBackgroundData();

            if (backgroundData != null)
            {
                if (backgroundData.BackgroundPrefab == null)
                {
                    Debug.LogError("[LevelDirector] BackgroundPrefab is null.");
                    return;
                }

                var instanceGo = Instantiate(backgroundData.BackgroundPrefab);
                Background = instanceGo != null ? instanceGo.GetComponent<BackgroundActor>() : null;
                if (Background == null)
                {
                    Debug.LogError(
                        "[LevelDirector] BackgroundActor missing on prefab (Missing Script / stale AB after rename from BackgroundBehavior).");
                    return;
                }

                if (!Background.IsTransitioning)
                {
                    Background.RefreshBg();
                }
            }
        }

        public static void EnsureBackgroundExists()
        {
            if (Background != null || instance == null)
            {
                return;
            }

            var backgroundData = instance.database.GetLastAvailableBackgroundData();
            if (backgroundData == null || backgroundData.BackgroundPrefab == null)
            {
                return;
            }

            var instanceGo = Instantiate(backgroundData.BackgroundPrefab);
            Background = instanceGo != null ? instanceGo.GetComponent<BackgroundActor>() : null;
            if (Background == null)
            {
                Debug.LogError(
                    "[LevelDirector] BackgroundActor missing on prefab (Missing Script / stale AB after rename from BackgroundBehavior).");
            }
        }

        public static bool NeedsBackgroundTransition(int levelIndex)
        {
            EnsureBackgroundExists();
            return Background != null && Background.NeedsTransitionTo(levelIndex);
        }

        public static void PlayBackgroundTransition(int levelIndex, Action onComplete = null)
        {
            EnsureBackgroundExists();
            Background?.PlayBgTransition(levelIndex, onComplete);
        }

        public static float GetLevelXSortsLoadDelay()
        {
            EnsureBackgroundExists();
            return Background != null ? Background.LevelXSortsLoadDelay : 0f;
        }

        public static void OnXSortSubmitted(XSortActor XSortBehavior, bool bFromHintItem = false)
        {
            if (!GameDirector.IsGameActive) return;

            XSortEffect effect = XSortBehavior.Effect;
            if (effect != null)
            {
                effect.OnXSortSubmitted();
            }

            List<XSortActor> activeXSorts = _levelGenFunc.ActXSorts;
            foreach (XSortActor XSorts in activeXSorts)
            {
                effect = XSorts.Effect;
                if (effect != null)
                {
                    effect.OnAnyXSortSubmitted(bFromHintItem);
                }
            }

        }

        public static void UnloadLevel()
        {
            RaycastController.ResetLongPress();
            XSortActor.ResetDockSubmitSortOrder();
            PUDirector.ResetBehaviors();
            isReviving = false;
            BoxCoverManager.Clear();
            AwardColorEggCoverManager.Clear();
            IQChallengeManager.Clear();

            if (_levelGenFunc != null)
            {
                _levelGenFunc.Clear();
                _levelGenFunc = null;
            }

            ClearCoinRewardState();
            instance.levelSpawnAnimation.Clear();
            instance.dock.RestoreAuthoredPosition();
            instance.dock.DisposeQuickly();
            instance.dock.HideSlots();
        }


        public void OnMatchCompleted(bool bGM = false)//关卡完成.
        {
            LastCompletionWasGM = bGM;
            GameEvent.Send((int)EventNameDef.SubmitOver);

            if (isCustomLevel) return;

            // 彩蛋开奖已提前 InceaseLevelNum；须在 TryConsumeVictory/ClearSave 之前读跳过标记，否则会连加两关
            bool skipIncreaseLevelNum = AwardColorEggCoverPending.ShouldSkipIncreaseLevelNum;

            bool isCompleted = bGM;
            if (!isCompleted && AwardColorEggCoverManager.HasActiveLevel)
            {
                if (!AwardColorEggCoverManager.AreAllCollected)
                {
                    AwardColorEggCoverManager.UpdateStates();
                    if (!AwardColorEggCoverManager.AreAllCollected)
                    {
                        CheckXSortScaleException();
                        return;
                    }
                }

                AwardColorEggCoverManager.EnsureVictorySequence();
                if (!AwardColorEggCoverManager.TryConsumeVictory())
                {
                    CheckXSortScaleException();
                    return;
                }

                isCompleted = true;
            }
            else if (!isCompleted && BoxCoverManager.HasActiveLevel)
            {
                if (!BoxCoverManager.AreAllCollected)
                {
                    CheckXSortScaleException();
                    return;
                }

                BoxCoverManager.EnsureVictorySequence();
                if (!BoxCoverManager.TryConsumeVictory())
                {
                    CheckXSortScaleException();
                    return;
                }

                isCompleted = true;
            }
            else if (!isCompleted && IQChallengeManager.HasActiveLevel)
            {
                if (!IQChallengeManager.IsReadyToWin)
                {
                    bool boardEmpty = LevelGenService == null
                        || LevelGenService.ActXSorts == null
                        || LevelGenService.ActXSorts.Count == 0;
                    bool dockEmpty = dock == null || dock.IsEmpty;
                    if (boardEmpty && dockEmpty)
                        IQChallengeManager.SettleIfBoardExhausted();
                    CheckXSortScaleException();
                    return;
                }

                IQChallengeManager.EnsureVictorySequence();
                if (!IQChallengeManager.TryConsumeVictory())
                {
                    CheckXSortScaleException();
                    return;
                }

                isCompleted = true;
            }
            else if (!isCompleted)
            {
                isCompleted = LevelGenService != null
                    && LevelGenService.GetRestXSortCount() == 0
                    && dock != null
                    && dock.IsEmpty;
            }

            if (isCompleted)
            {
                RaycastController.ResetLongPress();
                RaycastController.Disable();

                if (LevelDifficultySim.IsRunning)
                {
                    GameDirector.OnLevelCompleted();
                    return;
                }

                if (GameData.BFromStartScene)
                {
                    if (skipIncreaseLevelNum)
                        Debug.Log($"[彩蛋胜利] 结算不再加关 level={LoadedLevelIndex}");
                    else
                        GPUserDataManager.Instance.InceaseLevelNum();
                    // F3/GM 不走 TryConsumeVictory，需主动清当前关彩蛋 pending，否则 session 标记会挡后续关
                    if (bGM)
                        AwardColorEggCoverPending.MarkCompletedIfCurrentLevel();
                    int totalWin = GPUserDataManager.Instance.GetPassLevelCnt();
                    if (totalWin > 0 && totalWin % 5 == 0)
                    {
                        ResourceModule.ReleaseAllReleasableMemory();
                    }
                }
                else
                {
                    levelSave.IsPlayingRandomLevel = false;
                    levelSave.DisplayLevelIndex++;
                    SaveController.MarkAsSaveIsRequired();
                    if (levelSave.DisplayLevelIndex > levelSave.MaxReachedLevelIndex)
                    {
                        levelSave.MaxReachedLevelIndex = levelSave.DisplayLevelIndex;
                    }
                }
                GameDirector.OnLevelCompleted();
            }

            CheckXSortScaleException();
        }

        public void OnSlotsFilled()
        {
            if (!GameDirector.IsGameActive)
                return;

            // 飞入/三消未结束时先不锁操作：否则 LateUpdate 误判满槽后射线关掉且不会走到失败页
            if (dock.HasActiveSlotMotion() || dock.IsClearingAnimation())
            {
                dock.CancelPendingQuickFinishIfLoss();
                return;
            }

            if (AwardColorEggCoverManager.HandleDockFilled())
                return;

            if (BoxCoverManager.HandleDockFilled())
                return;

            if (IQChallengeManager.HandleDockFilled())
                return;

            RaycastController.Disable();
            GameDirector.OnLevelFailed();

            if (!LevelDifficultySim.IsRunning)
                AudioController.PlaySound(AudioController.AudioClips.levelFailed);
        }

        private void Update()
        {
        }

        public static bool SubmitIsAllowed()
        {
            // 待消不占有效容量；满物理槽时靠临时格继续收
            return instance.dock.CanAcceptMoreSubmits();
        }

        public static XSortActor SpawnDockXSort(RecoverLevelData.RLAXSortData rLAXSortData)
        {
            XSortModel XSortData = Database.GetXSort(rLAXSortData.XSortIndex);
            ElementPosition elementPosition = new ElementPosition(rLAXSortData.x, rLAXSortData.y, rLAXSortData.layer);

            XSortActor XSortBehavior = XSortData.Pool.GetPooledObject().GetComponent<XSortActor>();
            // 确保从内存池获取的棋子对象是激活的，避免关卡切换时棋子不显示的问题
            if (XSortBehavior != null && !XSortBehavior.gameObject.activeSelf)
            {
                XSortBehavior.gameObject.SetActive(true);
            }
            XSortBehavior.Init(XSortData, elementPosition);
            if (IsCoinRewardSkin(rLAXSortData.XSortSkinName) || IsCoinRewardXSort(XSortData))
            {
                XSortBehavior.ApplyCoinRewardSkin();
                if (CoinRewardXSortData == null)
                {
                    SetCoinRewardXSortData(XSortData);
                }
            }
            else
            {
                XSortBehavior.UpdateRuntimeSkin(rLAXSortData.XSortSkinName);
            }
            XSortBehavior.transform.localScale = Vector3.one;
            XSortBehavior.SetScale(Vector2.one * 0.84f);
            XSortBehavior.MarkAsSubmitted();
            if (rLAXSortData.isFission)
                XSortBehavior.SetFission(true);

            Dock.SubmitToSlot(XSortBehavior, true);

            return XSortBehavior;
        }

        public static void SubmitElement(XSortActor XSortBehavior)
        {
            if (LevelDifficultySim.IsRunning)
            {
                XSortBehavior.MarkAsSubmitted();
                _levelGenFunc.RemoveObject(XSortBehavior);
                bool simOk = instance.dock.SubmitToSlot(XSortBehavior, true);
                if (!simOk)
                {
                    XSortBehavior.ResetSubmitState();
                    _levelGenFunc.AddObject(XSortBehavior);
                    _levelGenFunc.UpdateClickableStates(false);
                    return;
                }

                _levelGenFunc.UpdateClickableStates(false);
                return;
            }

            XSortBehavior.MarkAsSubmitted();

            bool bSuc = instance.dock.SubmitToSlot(XSortBehavior, false);
            if (!bSuc)
            {
                XSortBehavior.ResetSubmitState();
                return;
            }

            var handPos = XSortBehavior.transform.position;
            int preCnt = _levelGenFunc.GetCalucuteFitXSortCnt(XSortBehavior);
            _levelGenFunc.RemoveObject(XSortBehavior);
            _levelGenFunc.UpdateClickableStates(true);
            int afterCnt = _levelGenFunc.GetCalucuteFitXSortCnt(XSortBehavior);

            if (afterCnt > preCnt)
            {
                LevelDirector.Dock.ShowFingerAnim(handPos);
            }

            //增加操作次数,数据统计
            if (GameData.BFromStartScene)
            {
                GameBAData.IncreateOperCnt();
            }
        }

        public static void RevertElement(XSortActor XSortBehavior)
        {
            XSortBehavior.ResetSubmitState();

            _levelGenFunc.AddObject(XSortBehavior);
            _levelGenFunc.UpdateClickableStates(true);
        }

        public static void RevertElements(List<XSortActor> XSortBehaviors)
        {
            foreach (XSortActor XSortBehavior in XSortBehaviors)
            {
                XSortBehavior.ResetSubmitState();

                _levelGenFunc.AddObject(XSortBehavior);
            }

            _levelGenFunc.UpdateClickableStates(true);
        }

        public static void SubmitElements(List<XSortActor> XSortBehaviors, bool bFromHintItem = false)
        {
            for (int i = 0; i < XSortBehaviors.Count; i++)
            {
                var XSortBehavior = XSortBehaviors[i];

                // 有效占用达上限时停止；仍允许凑三消/待消进临时格
                if (!instance.dock.CanAcceptMoreSubmits())
                {
                    XSortBehavior.ResetSubmitState();
                    continue;
                }

                XSortBehavior.MarkAsSubmitted(bFromHintItem);

                if (!instance.dock.SubmitToSlot(XSortBehavior, false))
                {
                    XSortBehavior.ResetSubmitState();
                    continue;
                }

                _levelGenFunc.RemoveObject(XSortBehavior);
            }

            _levelGenFunc.UpdateClickableStates(true);
        }

        public static XSortEffect GetXSortEffect(XSortEffectType XSortEffectType)
        {
            if (effectsLink.ContainsKey(XSortEffectType))
                return effectsLink[XSortEffectType];

            return null;
        }

        public static XSortEffect GetXSortEffect(CellModel cell)
        {
            var ret = GetXSortEffect(cell.Effect);
            cell.CheckResetEffect();
            return ret;
        }

        public static List<XSortActor> GetActiveXSorts(bool ignoreEffects, bool includeBoxCovered = false)
        {
            List<XSortActor> tempXSorts = new();
            List<XSortActor> activeXSorts = LevelGenService?.ActXSorts;
            if (activeXSorts == null)
            {
                return tempXSorts;
            }

            for (int i = 0; i < activeXSorts.Count; i++)
            {
                var XSort = activeXSorts[i];
                if (XSort == null || XSort.IsSubmitted)
                {
                    continue;
                }

                if (!includeBoxCovered
                    && (BoxCoverManager.IsXSortBlocked(XSort) || AwardColorEggCoverManager.IsXSortBlocked(XSort)))
                {
                    continue;
                }

                if (ignoreEffects)
                {
                    if (XSort.Effect == null)
                    {
                        tempXSorts.Add(XSort);
                    }
                }
                else
                {
                    tempXSorts.Add(XSort);
                }
            }

            return tempXSorts;
        }

        public static List<XSortActor> GetClickableXSorts()
        {
            List<XSortActor> tempXSorts = new List<XSortActor>();
            List<XSortActor> activeXSorts = LevelGenService?.ActXSorts;
            if (activeXSorts == null)
            {
                return tempXSorts;
            }

            for (int i = 0; i < activeXSorts.Count; i++)
            {
                var XSort = activeXSorts[i];
                if (XSort != null
                    && !XSort.IsSubmitted
                    && XSort.IsClickable
                    && !BoxCoverManager.IsXSortBlocked(XSort)
                    && !AwardColorEggCoverManager.IsXSortBlocked(XSort))
                {
                    tempXSorts.Add(XSort);
                }
            }

            return tempXSorts;
        }

        public static List<XSortActor> GetXSortsByType(XSortModel XSortData, int amount = int.MaxValue, bool includeBoxCovered = false)
        {
            List<XSortActor> tempXSorts = new();
            List<XSortActor> activeXSorts = LevelGenService?.ActXSorts;
            if (activeXSorts == null)
            {
                return tempXSorts;
            }

            for (int i = 0; i < activeXSorts.Count; i++)
            {
                XSortActor XSort = activeXSorts[i];
                if (XSort.IsSubmitted)
                    continue;
                if (XSort.IsFission)
                    continue;
                if (!includeBoxCovered
                    && (BoxCoverManager.IsXSortBlocked(XSort) || AwardColorEggCoverManager.IsXSortBlocked(XSort)))
                    continue;
                if (XSort.XSortModel == XSortData)
                {
                    tempXSorts.Add(XSort);

                    if (tempXSorts.Count >= amount)
                        break;
                }
            }

            return tempXSorts;
        }

        public static List<XSortActor> GetXSortsMatching(XSortActor sample, int amount = int.MaxValue, bool includeBoxCovered = false)
        {
            List<XSortActor> tempXSorts = new();
            if (sample == null)
                return tempXSorts;

            List<XSortActor> activeXSorts = LevelGenService?.ActXSorts;
            if (activeXSorts == null)
                return tempXSorts;

            for (int i = 0; i < activeXSorts.Count; i++)
            {
                XSortActor XSort = activeXSorts[i];
                if (XSort == null || XSort.IsSubmitted)
                    continue;
                if (!includeBoxCovered
                    && (BoxCoverManager.IsXSortBlocked(XSort) || AwardColorEggCoverManager.IsXSortBlocked(XSort)))
                    continue;
                if (!XSort.IsSameType(sample))
                    continue;

                tempXSorts.Add(XSort);
                if (tempXSorts.Count >= amount)
                    break;
            }

            return tempXSorts;
        }

        public static List<XSortActor> GetNeighbourXSorts(ElementPosition elementPosition)
        {
            List<XSortActor> neighbourXSorts = new List<XSortActor>();

            ElementPosition[] neighbourPositions = new ElementPosition[] { elementPosition.UpNeighbourPos, elementPosition.RightNeighbourPos, elementPosition.BottomNeighbourPos, elementPosition.LeftNeighbourPos };
            for (int i = 0; i < neighbourPositions.Length; i++)
            {
                if (_levelGenFunc.IsXSortExists(neighbourPositions[i]))
                {
                    neighbourXSorts.Add(_levelGenFunc.Layers[neighbourPositions[i]].XSort);
                }
            }

            return neighbourXSorts;
        }

        public static XSortActor GetXSort(ElementPosition elementPosition)
        {
            if (_levelGenFunc.IsXSortExists(elementPosition))
            {
                return _levelGenFunc.Layers[elementPosition].XSort;
            }

            return null;
        }

        public static bool HasNeighbourXSorts(ElementPosition elementPosition)
        {
            ElementPosition[] neighbourPositions = new ElementPosition[] { elementPosition.UpNeighbourPos, elementPosition.RightNeighbourPos, elementPosition.BottomNeighbourPos, elementPosition.LeftNeighbourPos };
            for (int i = 0; i < neighbourPositions.Length; i++)
            {
                if (_levelGenFunc.IsXSortExists(neighbourPositions[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static int GetCurrentLevelReward()
        {
            if (Level != null)
            {
                return (int)(level.CoinsReward);
            }

            return 5;
        }

        private static int GetUICompleteAdReward()//获得结算界面的广告奖励
        {
            return 50;
        }

        public static bool ReturnXSorts(int count, SimpleCallback callback, Action beforeUseFunc = null)
        {
            GameDirector.RecordLastPlayTime();

            // 若 Dock 正在移动或移除动画，禁止撤销，避免与三消合成竞态
            if (count == 1 && Dock != null && Dock.HasActiveSlotMotion())
            {
                callback?.Invoke();
                return false;
            }

            // 埋点须在移出 Dock 之前：ExcuteStageEvent 校验 temp+XSort_left 能被 3 整除
            if (count == 1)
            {
                if (!DockActor.HasUndoablePickedObject())
                {
                    callback?.Invoke();
                    return false;
                }
                beforeUseFunc?.Invoke();
            }
            else if (Dock != null && Dock.GetOccupiedSlotsCount() > 0)
            {
                beforeUseFunc?.Invoke();
            }

            // 撤销 / 复活：均按用户收集顺序从后往前回退，而非 Dock 最右侧槽位
            List<XSortActor> removedXSorts = DockActor.RemoveLastPickedObjects(count)
                .ConvertAll((slotable) => (XSortActor)slotable);

            if (removedXSorts.IsNullOrEmpty())
            {
                callback?.Invoke();
                return false;
            }

            RevertElements(removedXSorts);

            int revertedXSorts = 0;
            foreach (XSortActor XSort in removedXSorts)
            {
                if (XSort == null)
                {
                    revertedXSorts++;
                    if (revertedXSorts >= removedXSorts.Count)
                        callback?.Invoke();
                    continue;
                }

                Transform layerTf = _levelGenFunc != null
                    ? _levelGenFunc.GetXSortLayerTransform(XSort.ElementPosition)
                    : null;
                if (layerTf != null)
                {
                    if (!layerTf.gameObject.activeSelf)
                        layerTf.gameObject.SetActive(true);
                    XSort.transform.SetParent(layerTf, true);
                }

                if (!XSort.gameObject.activeSelf)
                    XSort.gameObject.SetActive(true);

                Vector3 returnPosition = LevelFitter.GetPosition(XSort.ElementPosition);

                Transform parentTransform = XSort.transform.parent;
                if (parentTransform != null)
                {
                    returnPosition = parentTransform.TransformPoint(returnPosition);
                }

                XSort.SubmitMove(returnPosition, Vector3.one * LevelFitter.XSortSize, () =>
                {
                    revertedXSorts++;
                    XSort.SetPosition(XSort.ElementPosition);
                    if (revertedXSorts >= removedXSorts.Count)
                        callback?.Invoke();
                }, unscaledTime: true);
            }

            return true;
        }

        public static bool IsLevelCompletable()
        {
            List<XSortActor> XSorts = LevelGenService.ActXSorts;
            foreach (XSortActor XSort in XSorts)
            {
                if (XSort.IsClickable)
                {
                    return true;
                }
            }

            return false;
        }

        public static void CheckXSortScaleException()
        {
            float exceptScaleRate = 0.1f;

            List<XSortActor> XSorts = LevelGenService?.ActXSorts;
            if (XSorts == null)
            {
                return;
            }
            bool bException = false;
            foreach (var item in XSorts)
            {
                if (item.transform.localScale.x <= exceptScaleRate)
                {
                    bException = true;
                    break;
                }
            }

            if (bException)
            {
                foreach (var item in XSorts)
                {
                    if (item.transform.localScale.x <= exceptScaleRate)
                    {
                        Debug.Log("exception XSort name == + " + item.name);

                        item.SetScale(LevelFitter.XSortSize);
                    }
                }
#if UNITY_EDITOR
                Debug.LogWarning("异常状态,有棋子不显示");
#else

#endif
            }
        }

        public static void SetBusyState(bool state)
        {
            isBusy = state;
        }

        public static void ClampMaxReachedLevel()
        {
            levelSave.MaxReachedLevelIndex = Mathf.Clamp(levelSave.MaxReachedLevelIndex, 0, Database.AmountOfLevels - 1);
        }

        public static void Revive(SimpleCallback onXSortsReturned = null)
        {
            if (isReviving)
                return;

            isReviving = true;
            SetBusyState(true);
            RaycastController.Disable();
            Dock?.StopAllAnimations();
            UIQuickVictory.HideAll();
            ReturnXSorts(5, () =>
            {
                onXSortsReturned?.Invoke();
                if (!ReviveShuffle())
                    FinishRevive(enableRaycast: true);
            });
        }

        static void FinishRevive(bool enableRaycast)
        {
            isReviving = false;
            SetBusyState(false);
            if (enableRaycast)
                RaycastController.Enable();
        }

        public static void ShowOrHideLevelNode(bool b)
        {
            LevelObject.SetActive(b);
        }


        //


        // AI生成
        //逻辑 dock中剩余位置足够，根据dock已存在的XSort，按照重复多的依次判定，满足条件的返回需要补充的棋子数组。
        //返回的数组只有一种颜色。并且棋子满足 IsXSortUnconcealed

        public static List<XSortActor> GetHintXSorts()
        {
            // 1) 统计 Dock 中各颜色数量（按数量降序）。
            Dictionary<int, int> dockCounts = new Dictionary<int, int>();
            Dictionary<int, XSortModel> idToXSortData = new Dictionary<int, XSortModel>();
            List<XSortActor> dockBehaviors = Dock.GetOccupiedXSortIndices();
            for (int i = 0; i < dockBehaviors.Count; i++)
            {
                var tb = dockBehaviors[i];
                if (tb == null || tb.XSortModel == null) continue;
                int id = tb.MatchTypeId;
                if (!dockCounts.ContainsKey(id))
                {
                    dockCounts[id] = 0;
                    idToXSortData[id] = tb.XSortModel;
                }
                dockCounts[id]++;
            }

            // 2) 统计棋盘可见可用的各颜色数量。
            Dictionary<int, int> boardCounts = new Dictionary<int, int>();
            List<XSortActor> oriActXSorts = LevelGenService.ActXSorts;
            var actXSorts = oriActXSorts.FindAll((a) => a.Effect == null);
            for (int i = 0; i < actXSorts.Count; i++)
            {
                XSortActor t = actXSorts[i];
                if (t == null || t.XSortModel == null || t.IsSubmitted) continue;
                if (!t.IsClickable) continue;
                if (t.Effect != null) continue;

                int id = t.MatchTypeId;
                if (!boardCounts.ContainsKey(id))
                {
                    boardCounts[id] = 0;
                    if (!idToXSortData.ContainsKey(id)) idToXSortData[id] = t.XSortModel;
                }
                boardCounts[id]++;
            }

            // 3) 构建候选颜色顺序：先 Dock（降序），再棋盘剩余（降序）。
            List<int> candidates = new List<int>();
            foreach (var kv in dockCounts)
            {
                candidates.Add(kv.Key);
            }
            candidates.Sort((a, b) => dockCounts[b].CompareTo(dockCounts[a]));
            // 追加棋盘上未在 Dock 出现的颜色。
            List<int> boardKeys = new List<int>(boardCounts.Keys);
            boardKeys.Sort((a, b) => boardCounts[b].CompareTo(boardCounts[a]));
            for (int i = 0; i < boardKeys.Count; i++)
            {
                int id = boardKeys[i];
                if (!candidates.Contains(id)) candidates.Add(id);
            }

            // 4) 依序尝试每个候选颜色，若能满足 dock 补齐三消则返回。
            int slotsAvailable = DockActor.GetSlotsAvailable();
            for (int c = 0; c < candidates.Count; c++)
            {
                int colorId = candidates[c];
                XSortModel colorXSortData = idToXSortData.ContainsKey(colorId) ? idToXSortData[colorId] : null;
                if (colorXSortData == null) continue;

                int inDock = dockCounts.ContainsKey(colorId) ? dockCounts[colorId] : 0;
                int required = Mathf.Max(0, 3 - inDock);
                if (required == 0) continue; // 已经可以三消，不用提示
                if (slotsAvailable < required) continue;

                // 从棋盘上收集该颜色的可见棋子。
                List<XSortActor> result = new List<XSortActor>();
                for (int i = 0; i < actXSorts.Count; i++)
                {
                    XSortActor XSort = actXSorts[i];
                    if (XSort == null || XSort.IsSubmitted || XSort.XSortModel == null) continue;
                    if (XSort.MatchTypeId != colorId) continue;
                    if (!XSort.IsClickable) continue;

                    result.Add(XSort);
                    if (result.Count >= required) break;
                }

                if (result.Count >= required)
                {
                    return result;
                }
                // 否则尝试下一个候选颜色。
            }

            return null;
        }

        //用ai生成
        private static void SetShuffleXSortSuitDock(List<XSortActor> activeXSorts)
        {
            var list = Dock.GetOccupiedXSortIndices();
            if (list.Count == 2)
            {
                var type1 = list[0].MatchTypeId;
                var type2 = list[1].MatchTypeId;
                // 选取当前最上层的若干个棋子（用于交换位置，把目标类型放到最上层）
                List<XSortActor> topLayerXSorts = new List<XSortActor>(activeXSorts);
                topLayerXSorts.Sort((a, b) => -b.ElementPosition.LayerId.CompareTo(a.ElementPosition.LayerId));

                // 交换两个棋子的格子与层级归属
                void SwapXSortsElementPosition(XSortActor a, XSortActor b)
                {
                    if (a == null || b == null || a == b)
                        return;

                    var layers = LevelGenService.Layers;

                    ElementPosition posA = a.ElementPosition;
                    ElementPosition posB = b.ElementPosition;

                    // 交换逻辑位置
                    a.SetPosition(posB);
                    b.SetPosition(posA);

                    // 更新世界位置
                    a.transform.localPosition = LevelFitter.GetPosition(posB);
                    b.transform.localPosition = LevelFitter.GetPosition(posA);

                    // 更新父节点（层级对象）
                    a.transform.SetParent(layers[posB.LayerId].LayerObject.transform);
                    b.transform.SetParent(layers[posA.LayerId].LayerObject.transform);

                    // 更新名称便于调试
                    a.transform.name = $"after_l:{posB.LayerId}_x:{posB.X}_y:{posB.Y}";
                    b.transform.name = $"after_l:{posA.LayerId}_x:{posA.X}_y:{posA.Y}";
                }

                // 在可打乱的棋子里寻找指定类型的候选
                XSortActor FindCandidateByType(int XSortType, HashSet<XSortActor> exclude)
                {
                    for (int i = 0; i < activeXSorts.Count; i++)
                    {
                        var t = activeXSorts[i];
                        if (exclude != null && exclude.Contains(t))
                            continue;
                        if (t != null && !t.IsSubmitted && t.XSortModel != null && t.MatchTypeId == XSortType)
                        {
                            return t;
                        }
                    }
                    return null;
                }

                // 推升指定类型的若干个棋子到更上层（通过与当前最上层棋子交换）
                int PromoteType(int XSortType, int requiredCount, HashSet<XSortActor> exclude)
                {
                    int promoted = 0;
                    for (int seek = 0; seek < requiredCount; seek++)
                    {
                        XSortActor candidate = FindCandidateByType(XSortType, exclude);
                        if (candidate == null)
                            break;

                        // 找一个当前最上层但不是 candidate 自己的棋子进行交换
                        XSortActor topXSortForSwap = null;
                        for (int i = 0; i < topLayerXSorts.Count; i++)
                        {
                            XSortActor topXSort = topLayerXSorts[i];
                            if (topXSort != null && topXSort != candidate && (exclude == null || !exclude.Contains(topXSort)))
                            {
                                topXSortForSwap = topXSort;
                                break;
                            }
                        }

                        if (topXSortForSwap == null)
                            break;

                        SwapXSortsElementPosition(candidate, topXSortForSwap);
                        exclude?.Add(candidate);
                        exclude?.Add(topXSortForSwap);
                        promoted++;
                    }
                    return promoted;
                }

                // 统计各类型可用数量（不含已使用与已提交）
                Dictionary<int, List<XSortActor>> typeToXSorts = new Dictionary<int, List<XSortActor>>();
                for (int i = 0; i < activeXSorts.Count; i++)
                {
                    var t = activeXSorts[i];
                    if (t == null || t.IsSubmitted || t.XSortModel == null) continue;
                    int idx = t.MatchTypeId;
                    if (!typeToXSorts.TryGetValue(idx, out var lst))
                    {
                        lst = new List<XSortActor>();
                        typeToXSorts[idx] = lst;
                    }
                    lst.Add(t);
                }
                // 目标：把与 dock 呼应的棋子顶到最上层（最多两个）
                HashSet<XSortActor> used = new HashSet<XSortActor>();

                if (type1 == type2)
                {
                    // Dock 两枚相同类型时：
                    // 1) 优先把第三枚同类型顶到最上层（若有）
                    PromoteType(type1, 1, used);

                    // 2) 再尝试在前面几层额外形成两组三个同色（不包含 dock 类型）
                    int formedTriples = 0;
                    foreach (var kv in typeToXSorts)
                    {
                        if (kv.Key == type1) continue;
                        // 过滤掉已使用的
                        int available = 0;
                        for (int i = 0; i < kv.Value.Count; i++)
                        {
                            if (!used.Contains(kv.Value[i])) available++;
                        }
                        if (available >= 3)
                        {
                            int promoted = PromoteType(kv.Key, 3, used);
                            if (promoted == 3)
                            {
                                formedTriples++;
                                if (formedTriples >= 2)
                                    break;
                            }
                        }
                    }
                }
                else
                {
                    // Dock 两枚不同类型时：
                    // 1) 分别把这两种类型各再顶两枚到前面几层（与上方同色，方便凑三消）
                    PromoteType(type1, 2, used);
                    PromoteType(type2, 2, used);

                    // 2) 再额外找一个第三颜色（非 dock 两色），在前面几层形成一组三色
                    int thirdType = -1;
                    foreach (var kv in typeToXSorts)
                    {
                        if (kv.Key == type1 || kv.Key == type2) continue;
                        int available = 0;
                        for (int i = 0; i < kv.Value.Count; i++)
                        {
                            if (!used.Contains(kv.Value[i])) available++;
                        }
                        if (available >= 3)
                        {
                            thirdType = kv.Key;
                            break;
                        }
                    }
                    if (thirdType != -1)
                    {
                        PromoteType(thirdType, 3, used);
                    }
                }
            }
        }

        public static bool ReviveShuffle(Action beforeUseFunc = null)
        {
            // 复活流程已占用 isBusy；其它来源占用时拒绝叠洗牌
            if (isBusy && !isReviving)
                return false;

            List<XSortActor> activeXSorts = LevelGenService.ActXSorts;
            if (activeXSorts == null)
                return false;
            if (activeXSorts.Count <= 1)
                return false;

            List<XSortActor> allowedToShuffleXSorts = new List<XSortActor>(activeXSorts);
            for (int i = allowedToShuffleXSorts.Count - 1; i >= 0; i--)
            {
                if (!allowedToShuffleXSorts[i].IsShuffleAllowed())
                    allowedToShuffleXSorts.RemoveAt(i);
            }

            if (allowedToShuffleXSorts.Count <= 1)
                return false;

            beforeUseFunc?.Invoke();
            isBusy = true;
            RaycastController.Disable();

            ElementPosition[] shuffleElements = new ElementPosition[allowedToShuffleXSorts.Count];
            for (int i = 0; i < shuffleElements.Length; i++)
                shuffleElements[i] = allowedToShuffleXSorts[i].ElementPosition;

            shuffleElements.RandomizeOrder();

            for (int i = 0; i < activeXSorts.Count; i++)
            {
                activeXSorts[i].transform.localScale = Vector3.zero;
                activeXSorts[i].SetSpawnTweenVisualsActive(false);
            }

            var layersRevive = LevelGenService.Layers;
            for (int i = 0; i < shuffleElements.Length; i++)
            {
                ElementPosition elementPosition = shuffleElements[i];
                Transform layerT = layersRevive[elementPosition.LayerId].LayerObject.transform;
                XSortActor tb = allowedToShuffleXSorts[i];

                tb.transform.SetParent(layerT);
                tb.transform.localPosition = LevelFitter.GetPosition(shuffleElements[i]);
                tb.transform.localScale = Vector3.zero;
                tb.SetPosition(shuffleElements[i]);
                tb.transform.name = $"after_l:{elementPosition.LayerId}_x:{elementPosition.X}_y:{elementPosition.Y}";
            }

            SetShuffleXSortSuitDock(allowedToShuffleXSorts);

            for (int i = 0; i < activeXSorts.Count; i++)
                activeXSorts[i].InvalidateSpawnTweenVisualCache();

            LevelGenService.RelinkXSorts(false);

            PUDirector.GetShuffleBoardScaleTweenParams(out float reviveScaleTime, out float reviveMinDelay, out float reviveMaxDelay, out DG.Tweening.Ease reviveScaleEasing);
            OptimisedXSortsScaleTweenClip optimisedShuffleTweenCase = new OptimisedXSortsScaleTweenClip(
                activeXSorts,
                reviveScaleTime,
                reviveMinDelay,
                reviveMaxDelay,
                reviveScaleEasing
            );

            optimisedShuffleTweenCase.OnComplete(() =>
            {
                FinishRevive(enableRaycast: true);
                LevelGenService.UpdateClickableStates();
                GameEvent.Send((int)EventNameDef.LoadSaveLevelAnimFinish);
            });

            optimisedShuffleTweenCase.StartTween();
            return true;
        }
    }
}