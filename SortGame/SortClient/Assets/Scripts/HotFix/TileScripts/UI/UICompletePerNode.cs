using System;
using System.Collections.Generic;
using System.Linq;
using Core.Tools.MethodExtension;
using Spine.Unity;
using Spine.Unity.Examples;
using DG.Tweening;
using TileGameCore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FishFramework;
using cfg.Game;

namespace FishTilePlay
{
    public class UICompletePerNode : GPViewBase
    {
        [Header("Progress Track")]
        [SerializeField] RectTransform progressTrack;
        [SerializeField] RectTransform headNode;
        [SerializeField] RectTransform bodyTemplate;
        [SerializeField] RectTransform tailNode;
        [SerializeField] float fillRevealDuration = 0.5f;
        [SerializeField] float progressLabelSwitchDelay = 0.4f;

        [SerializeField] GameObject previewDlg;

        [SerializeField] Image previewIcon;

        [SerializeField] TextMeshProUGUI previewNumLb;

        [SerializeField] GameObject touchCover;

        [SerializeField] GameObject proBgNode;
        [SerializeField] GameObject btnNode;
        [SerializeField] GameObject duigouNode;

        [SerializeField] SpineGraphicChange spineGraphicChange;
        [SerializeField] SkeletonGraphic spine;
        [SerializeField] TextMeshProUGUI spineNumLb;
        [SerializeField] TextMeshProUGUI progressUILb;

        private readonly List<RectTransform> bodyInstances = new();
        private readonly List<RectTransform> segmentNodes = new();
        private readonly List<float> segmentTargetWidths = new();
        private readonly Dictionary<RectTransform, SlicedFilledImage> slicedFilledMap = new();

        private Vector3 initLocalScale;
        private Vector2 initAnchoredPosition;
        private bool bSavedSpineTran = false;
        private bool bGetBoxAward = false;
        private int mProgressMaxCnt;
        private Vector3 mSpineNumLbDefaultScale;
        private bool mHasSpineNumLbDefaultScale;
        private int mBoxAnimGeneration;
        private int mProgressAnimGeneration;
        private float mSegmentHeight;
        private bool bCachedSegmentSize;
        private Action mOnReadyForBgProgress;
        private int mPendingFromFilled;
        private int mPendingToFilled;
        private int mPendingCurCnt;
        private bool mProgressPrepared;
        private SkeletonDataAsset cachedDefaultGiftSpine;

        private const float SpineNumLbAppearDelay = 0.6f;
        private const float SpineNumLbScaleDuration = 6f / 30f;
        private const string VNodeInSpineEventName = "vNode_In";
        private const string VNodeDefaultAnimState = "vnodeNext";
        private const string VNodeFirstThreeLevelsAnimState = "vnodeNext1-3";
        private const string VNodeAdvButtonName = "advButton";

        public int AwardNum;
        public int AwardID;

        public void ApplyTheme()
        {
            if (spine == null)
            {
                return;
            }

            if (cachedDefaultGiftSpine == null)
            {
                cachedDefaultGiftSpine = spine.skeletonDataAsset;
            }

            SkeletonDataAsset asset = ThemeUiApplier.Get()?.winGift;
            if (asset == null)
            {
                asset = cachedDefaultGiftSpine;
            }

            ThemeUiApplier.ApplySkeleton(spine, asset);
        }


        public void SaveInitTransform()
        {
            if (bSavedSpineTran || spineGraphicChange == null)
            {
                return;
            }

            // 用本地坐标缓存，避免 SafeArea / Canvas 变化后世界坐标还原偏上
            initLocalScale = spineGraphicChange.transform.localScale;
            var rt = spineGraphicChange.transform as RectTransform;
            initAnchoredPosition = rt != null
                ? rt.anchoredPosition
                : (Vector2)spineGraphicChange.transform.localPosition;
            bSavedSpineTran = true;
        }

        private void Start()
        {
            Utility.DGDelayedCall(0.5f, () =>
            {
                SaveInitTransform();
            });
        }

        public void HideClaimButton()
        {
            mBoxAnimGeneration++;
            spineNumLb.SetActiveEx(false);
            SetBoxVisible(false);
        }

        public void ClearPendingBoxAward()
        {
            bGetBoxAward = false;
        }

        void SetBoxVisible(bool visible)
        {
            var boxTran = GetBoxTransform();
            if (boxTran == null)
            {
                return;
            }

            DOTween.Kill(boxTran, false);
            if (!visible)
            {
                boxTran.localScale = Vector3.zero;
                return;
            }

            RestoreBoxAtDefaultPose();
        }

        Transform GetBoxTransform()
        {
            if (spineGraphicChange != null)
            {
                return spineGraphicChange.transform;
            }

            return spine != null ? spine.transform : null;
        }

        void RestoreBoxAtDefaultPose()
        {
            if (spine != null)
            {
                spine.AnimationState.ClearTrack(0);
                spine.Skeleton.SetToSetupPose();
                var entry = spine.AnimationState.SetAnimation(0, "idle", true);
                if (entry != null)
                {
                    entry.MixDuration = 0f;
                    entry.TrackTime = 0f;
                }

                spine.Update(0);
            }

            ResetSpineInitTrans();
        }

        public void ResetSpineInitTrans()
        {
            if (spineGraphicChange == null)
            {
                return;
            }

            var spineTran = spineGraphicChange.transform;
            DOTween.Kill(spineTran, false);
            spineTran.localScale = initLocalScale;

            var rt = spineTran as RectTransform;
            if (rt != null)
            {
                rt.anchoredPosition = initAnchoredPosition;
            }
            else
            {
                spineTran.localPosition = initAnchoredPosition;
            }
        }

        public void MoveSpineToDstPosition(Vector3 dstPosition)
        {
            var spineTran = spineGraphicChange.transform;
            // 回弹：先反方向小幅移动，再加速到目标；回弹阶段不缩放，抵达时缩放到 0.01
            DOTween.Kill(spineTran, false);
            spineNumLb.SetActiveEx(false);

            float moveDuration = 0.6f;
            float shrinkScale = 0.01f; // 抵达时几乎消失

            var startPos = spineTran.position;
            var toDir = (dstPosition - startPos);
            if (toDir.sqrMagnitude < 0.0001f)
            {
                spineTran.position = dstPosition;
                spineTran.localScale = initLocalScale * shrinkScale;
                return;
            }

            var dirNorm = toDir.normalized;
            float backDistance = Mathf.Min(toDir.magnitude * 0.15f, 100f);
            var prePos = startPos - dirNorm * backDistance; // 反方向回拉位置

            // 回弹阶段不缩放
            spineTran.localScale = initLocalScale;

            float reboundDuration = moveDuration * 0.25f;
            float mainDuration = moveDuration - reboundDuration;

            var seq = DOTween.Sequence();
            // 回弹：先小幅反向移动
            seq.Append(spineTran.DOMove(prePos, reboundDuration).SetEase(DG.Tweening.Ease.OutQuad));
            // 主冲刺：加速到目标，同时缩小到 0.01
            seq.Append(
                DOTween.Sequence()
                    .Join(spineTran.DOMove(dstPosition, mainDuration).SetEase(DG.Tweening.Ease.InCubic))
                    .Join(spineTran.DOScale(initLocalScale * shrinkScale, mainDuration).SetEase(DG.Tweening.Ease.OutQuad))
            );
            seq.OnComplete(() => seq.Kill());
            seq.Play();

        }

        public void HandleAwardButtonTap()
        {
            bool activeSelf = previewDlg.activeSelf;
            previewDlg.SetActive(!activeSelf);
        }

        //隐藏预览道具窗
        public void HidePreviewDlg()
        {
            previewDlg.SetActive(false);
        }

        private string m_awardStr = "";
        //设置奖励数据
        public void InitRewardsData()
        {
            var levelPassCfg = GetCurLevelPassCfg();
            //预览内容设置
            m_awardStr = levelPassCfg.Award[0];
            var strArr = m_awardStr.Split(",").ToList();
            AwardNum = 0;
            AwardID = 0;
            if (strArr.Count == 2)
            {
                var id = strArr[0];
                var num = strArr[1];
                previewNumLb.text = "x" + num;
                previewIcon.sprite = LoadSprite("LevelAward", "item_" + id);
                spineNumLb.text = "x" + num;
                AwardNum = num.ToInt();
                AwardID = id.ToInt();
            }
            else
            {
                Debug.LogWarning("配置错误");
            }
        }

        LevelPassCfg GetCurLevelPassCfg()
        {
            int idx = GameDirector.levelController.GetLoadedLevelIndex();
            return GameData.LevelPass.GetCurrentStageLevelCfg(idx);
        }

        /// <summary>
        /// 打开结算时先定格到填充前进度（如 1/4），避免 processGroup 亮起时先闪上一局满格。
        /// </summary>
        public void PrepareProgressBarBeforeFill()
        {
            mProgressPrepared = false;
            if (!GameData.BFromStartScene)
            {
                return;
            }

            // 取消上一局开箱延时回调，避免结算复用后被切回 loop
            mBoxAnimGeneration++;
            mProgressAnimGeneration++;

            bGetBoxAward = false;
            mProgressMaxCnt = 0;

            if (!bSavedSpineTran)
            {
                SaveInitTransform();
            }

            HidePreviewDlg();

            btnNode.SetActive(false);
            duigouNode?.SetActive(false);
            proBgNode.SetActive(true);
            touchCover.SetActive(true);
            spineNumLb.SetActiveEx(false);

            var levelPassCfg = GetCurLevelPassCfg();
            RestoreBoxAtDefaultPose();

            int maxCnt = levelPassCfg.ProgressValue;
            mProgressMaxCnt = maxCnt;

            GPUserDataManager.Instance.IncrementLevelPassPoint(levelPassCfg.AddValue);

            int curCnt = 0;
            if (GPUserDataManager.Instance.GetBInfinitMode())
            {
                int loadedIdx = GPUserDataManager.Instance.GetLevelNum() + GPUserDataManager.Instance.GetInfinitLevelNum();
                curCnt = GameData.LevelPass.GetCurrentStagePoint(loadedIdx);
            }
            else
            {
                int loadedIdx = GameDirector.levelController.GetLoadedLevelIndex();
                curCnt = GameData.LevelPass.GetCurrentStagePoint(loadedIdx);
            }

            InitRewardsData();
            LayoutProgressSegments(maxCnt);

            mPendingFromFilled = Mathf.Clamp(curCnt - 1, 0, maxCnt);
            mPendingToFilled = Mathf.Clamp(curCnt, 0, maxCnt);
            mPendingCurCnt = curCnt;

            DOTween.Kill(this, false);
            SetFilledCount(mPendingFromFilled);
            RefreshProgressLabel(mPendingFromFilled, maxCnt);

            if (curCnt >= maxCnt)
            {
                GPUserDataManager.Instance.IncrementLevelPassStage();
                GPUserDataManager.Instance.ResetLevelPassPoint();
            }

            mProgressPrepared = true;
        }

        /// <summary>
        /// 帧事件触发：从已准备好的 from 格播放填充到 to 格。
        /// </summary>
        public void PlayPreparedProgressFill(Action onReadyForBgProgress)
        {
            if (!GameData.BFromStartScene)
            {
                onReadyForBgProgress?.Invoke();
                return;
            }

            if (!mProgressPrepared)
            {
                SetInitBar(onReadyForBgProgress);
                return;
            }

            mProgressPrepared = false;
            mOnReadyForBgProgress = onReadyForBgProgress;

            int animGen = mProgressAnimGeneration;
            int curCnt = mPendingCurCnt;
            int maxCnt = mProgressMaxCnt;
            int fromFilled = mPendingFromFilled;
            int toFilled = mPendingToFilled;

            DOTween.Kill(this, false);
            // 再保证一次起始态，防止中间被别的逻辑改过
            SetFilledCount(fromFilled);
            RefreshProgressLabel(fromFilled, maxCnt);

            PlayFillProgressAnim(fromFilled, toFilled, () =>
            {
                if (animGen != mProgressAnimGeneration)
                {
                    return;
                }

                // 满箱只标记，开箱改到 UICompleteShow 结束后再播
                if (curCnt >= maxCnt)
                {
                    bGetBoxAward = true;
                }

                mOnReadyForBgProgress?.Invoke();
            });
        }

        public void SetInitBar(Action onReadyForBgProgress)
        {
            PrepareProgressBarBeforeFill();
            PlayPreparedProgressFill(onReadyForBgProgress);
        }

        /// <summary>
        /// UICompleteShow 结束后调用：播原开箱 Spine，结束后回调（用于再显示 vNode）。
        /// </summary>
        public void PlayOpenBoxThen(Action onFinished)
        {
            if (!bGetBoxAward)
            {
                onFinished?.Invoke();
                return;
            }

            mOnReadyForBgProgress = onFinished;
            PlayOpenBoxAnim();
        }

        public void GrantBoxAward()
        {
            if (!bGetBoxAward)
            {
                return;
            }

            var strArr = m_awardStr.Split(",").ToList();
            var id = (int)strArr[0].ToInt();
            var num = (int)strArr[1].ToInt();

            if (id != 4)//金币单独在动画中处理
            {
                GPUserDataManager.Instance.AddCommonItemData(id, num);
            }
        }

        public void ShowProgressAfterClaim()
        {
            mBoxAnimGeneration++;
            mProgressAnimGeneration++;
            DOTween.Kill(this, false);

            bGetBoxAward = false;

            spineNumLb.SetActiveEx(false);
            duigouNode?.SetActive(false);

            proBgNode.SetActive(true);
            if (mProgressMaxCnt > 0)
            {
                LayoutProgressSegments(mProgressMaxCnt);
                SetFilledCount(mProgressMaxCnt);
            }

            RefreshProgressLabel(mProgressMaxCnt, mProgressMaxCnt);

            SetBoxVisible(true);
        }

        public void ShowDuigou()
        {
            duigouNode?.SetActive(true);
        }

        public void ShowBtnNode(bool showAdButton)
        {
            // 第 8 关结算才出广告。在那之前播 vnodeNext1-3，把下一关按钮上移并关掉 advButton。
            // 有广告才播 vnodeNext，并先恢复节点，避免沿用上一关的隐藏状态。
            var advButtonTransform = btnNode != null
                ? btnNode.transform.Find(VNodeAdvButtonName)
                : null;
            advButtonTransform?.gameObject.SetActive(showAdButton);

            btnNode.SetActive(true);

            var animator = btnNode != null ? btnNode.GetComponent<Animator>() : null;
            if (animator != null)
            {
                animator.enabled = true;
                string animationState = GameData.UnlockData.GetCompleteAdExtraAwardUnlock()
                    ? VNodeDefaultAnimState
                    : VNodeFirstThreeLevelsAnimState;
                animator.Play(animationState, 0, 0f);
                animator.Update(0f);
            }

            touchCover.SetActive(false);
        }

        /// <summary>
        /// vnodeNext1-3 写过 advButton.m_IsActive，Write Defaults 会在 vnodeNext 状态下每帧把按钮重新打开。
        /// 入场结束后关掉 Animator，后续才能用脚本隐藏广告按钮。
        /// </summary>
        public void StopBtnNodeAnimator()
        {
            if (btnNode == null)
            {
                return;
            }

            var animator = btnNode.GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = false;
            }
        }

        public bool GetBGetBoxAward()
        {
            return bGetBoxAward;
        }

        void RefreshProgressLabel(int currentLevel, int totalLevel)
        {
            progressUILb.SetActiveEx(true);
            progressUILb.text = $"{currentLevel}/{totalLevel}";
        }

        void CacheSegmentSizes()
        {
            if (bCachedSegmentSize)
            {
                return;
            }

            if (headNode != null)
            {
                mSegmentHeight = headNode.sizeDelta.y;
            }

            if (mSegmentHeight <= 0f && tailNode != null)
            {
                mSegmentHeight = tailNode.sizeDelta.y;
            }

            if (mSegmentHeight <= 0f && bodyTemplate != null)
            {
                mSegmentHeight = bodyTemplate.sizeDelta.y;
            }

            bCachedSegmentSize = true;
        }

        void EnsureBodyInstances(int bodyCount)
        {
            if (bodyTemplate == null || progressTrack == null)
            {
                return;
            }

            bodyTemplate.gameObject.SetActive(false);

            while (bodyInstances.Count < bodyCount)
            {
                var instance = Instantiate(bodyTemplate, progressTrack, false);
                instance.name = $"body_{bodyInstances.Count}";
                bodyInstances.Add(instance);
            }

            for (int i = 0; i < bodyInstances.Count; i++)
            {
                var body = bodyInstances[i];
                if (body == null)
                {
                    continue;
                }

                // 实际显隐由 SetFilledCount 控制；这里只保证实例存在
                if (i >= bodyCount)
                {
                    body.gameObject.SetActive(false);
                }
            }
        }

        void LayoutProgressSegments(int segmentCount)
        {
            if (progressTrack == null || headNode == null || tailNode == null)
            {
                Debug.LogWarning("UICompletePerNode: progressTrack/head/tail 未绑定");
                return;
            }

            CacheSegmentSizes();

            segmentCount = Mathf.Max(0, segmentCount);
            bool useHead = segmentCount >= 1;
            bool useTail = segmentCount >= 2;
            int bodyCount = segmentCount >= 2 ? segmentCount - 2 : 0;

            EnsureBodyInstances(bodyCount);
            RebuildSegmentNodes(useHead, bodyCount, useTail);

            float trackWidth = progressTrack.rect.width;
            if (trackWidth <= 0f)
            {
                trackWidth = progressTrack.sizeDelta.x;
            }

            // head + n*body + tail 等宽铺满 bg(3)
            float blockWidth = segmentCount > 0 ? trackWidth / segmentCount : 0f;
            segmentTargetWidths.Clear();
            float x = 0f;
            for (int i = 0; i < segmentNodes.Count; i++)
            {
                var node = segmentNodes[i];
                if (node == null)
                {
                    segmentTargetWidths.Add(0f);
                    continue;
                }

                float width = blockWidth;
                if (i == segmentNodes.Count - 1)
                {
                    // 最后一块吃掉浮点误差，保证总长正好等于底板
                    width = Mathf.Max(blockWidth, trackWidth - x);
                }

                // tail 预制体是 Y180 镜像，需要按镜像槽位排布，否则最后一格会画到左边
                bool mirrorX = useTail && node == tailNode;
                segmentTargetWidths.Add(width);
                SetSegmentRect(node, x, width, mirrorX);
                // 布局阶段先确保激活，再替换组件，避免未激活 AddComponent 失败
                bool wasActive = node.gameObject.activeSelf;
                if (!wasActive)
                {
                    node.gameObject.SetActive(true);
                }

                ApplySegmentFilled(node, 1f, mirrorX);
                if (!wasActive)
                {
                    node.gameObject.SetActive(false);
                }

                x += width;
            }
        }

        void SetSegmentRect(RectTransform rect, float x, float width, bool mirrorX = false)
        {
            float height = mSegmentHeight > 0f ? mSegmentHeight : rect.sizeDelta.y;
            rect.localRotation = Quaternion.identity;
            rect.localScale = mirrorX ? new Vector3(-1f, 1f, 1f) : Vector3.one;
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            // 负 scale.x 时从槽位右缘往左画，才能落在最后一格
            rect.anchoredPosition = new Vector2(mirrorX ? x + width : x, 0f);
            rect.sizeDelta = new Vector2(width, height);
        }

        void RebuildSegmentNodes(bool useHead, int bodyCount, bool useTail)
        {
            segmentNodes.Clear();

            if (useHead && headNode != null)
            {
                segmentNodes.Add(headNode);
            }

            for (int i = 0; i < bodyCount; i++)
            {
                if (bodyInstances[i] != null)
                {
                    segmentNodes.Add(bodyInstances[i]);
                }
            }

            if (useTail && tailNode != null)
            {
                segmentNodes.Add(tailNode);
            }
        }

        void SetFilledCount(int filledCount)
        {
            int total = segmentNodes.Count;
            filledCount = Mathf.Clamp(filledCount, 0, total);
            for (int i = 0; i < total; i++)
            {
                var node = segmentNodes[i];
                if (node == null)
                {
                    continue;
                }

                bool visible = i < filledCount;
                // 未完成块直接隐藏，不做置灰
                node.gameObject.SetActive(visible);
                if (visible)
                {
                    RestoreSegmentFullWidth(i, 1f);
                }
            }
        }

        bool IsMirrorSegment(RectTransform node)
        {
            return node != null && tailNode != null && node == tailNode;
        }

        SlicedFilledImage EnsureSlicedFilled(RectTransform node, bool mirrorX)
        {
            if (node == null)
            {
                return null;
            }

            if (slicedFilledMap.TryGetValue(node, out var cached) && cached != null)
            {
                ConfigureSlicedFilled(cached, mirrorX);
                return cached;
            }

            var sliced = node.GetComponent<SlicedFilledImage>();
            if (sliced != null)
            {
                ConfigureSlicedFilled(sliced, mirrorX);
                slicedFilledMap[node] = sliced;
                return sliced;
            }

            // Image 与 SlicedFilledImage 同节点会冲突，先拷贝再移除 Image
            var image = node.GetComponent<Image>();
            Sprite sprite = null;
            Color color = Color.white;
            float ppu = 1f;
            if (image != null)
            {
                sprite = image.sprite;
                color = image.color;
                ppu = image.pixelsPerUnitMultiplier;
                DestroyImmediate(image);
            }

            sliced = node.gameObject.AddComponent<SlicedFilledImage>();
            if (sliced == null)
            {
                Debug.LogError("UICompletePerNode: AddComponent<SlicedFilledImage> 失败");
                return null;
            }

            sliced.sprite = sprite;
            sliced.color = color;
            sliced.raycastTarget = false;
            sliced.pixelsPerUnitMultiplier = ppu;
            ConfigureSlicedFilled(sliced, mirrorX);
            slicedFilledMap[node] = sliced;
            return sliced;
        }

        static void ConfigureSlicedFilled(SlicedFilledImage sliced, bool mirrorX)
        {
            if (sliced == null)
            {
                return;
            }

            // 镜像块本地 +X 是屏幕向左，用 Left 才能视觉上从左到右填充
            sliced.fillDirection = mirrorX
                ? SlicedFilledImage.FillDirection.Left
                : SlicedFilledImage.FillDirection.Right;
            sliced.fillCenter = true;
        }

        void ApplySegmentFilled(RectTransform node, float fillAmount, bool? mirrorX = null)
        {
            bool mirror = mirrorX ?? IsMirrorSegment(node);
            var sliced = EnsureSlicedFilled(node, mirror);
            if (sliced == null)
            {
                return;
            }

            var color = sliced.color;
            color.a = 1f;
            sliced.color = color;
            sliced.fillAmount = Mathf.Clamp01(fillAmount);
            sliced.SetVerticesDirty();
            sliced.SetMaterialDirty();
        }

        void RestoreSegmentFullWidth(int index, float fillAmount = 1f)
        {
            if (index < 0 || index >= segmentNodes.Count)
            {
                return;
            }

            var node = segmentNodes[index];
            if (node == null)
            {
                return;
            }

            DOTween.Kill(node, false);
            float width = index < segmentTargetWidths.Count ? segmentTargetWidths[index] : node.sizeDelta.x;
            float x = node.anchoredPosition.x;
            // 从累计宽度反推槽位左缘（镜像块当前锚点在右缘）
            bool mirrorX = IsMirrorSegment(node);
            float slotX = mirrorX ? x - width : x;
            // 若尚未布局过，用前面块宽度累加
            if (index < segmentTargetWidths.Count)
            {
                slotX = 0f;
                for (int i = 0; i < index; i++)
                {
                    slotX += segmentTargetWidths[i];
                }
            }

            SetSegmentRect(node, slotX, width, mirrorX);
            ApplySegmentFilled(node, fillAmount, mirrorX);
        }

        void PlayFillProgressAnim(int fromFilled, int toFilled, Action onComplete)
        {
            if (toFilled <= fromFilled || toFilled > segmentNodes.Count)
            {
                SetFilledCount(toFilled);
                RefreshProgressLabel(toFilled, mProgressMaxCnt);
                onComplete?.Invoke();
                return;
            }

            SetFilledCount(fromFilled);

            int nextIndex = toFilled - 1;
            var nextNode = segmentNodes[nextIndex];
            if (nextNode == null)
            {
                SetFilledCount(toFilled);
                RefreshProgressLabel(toFilled, mProgressMaxCnt);
                onComplete?.Invoke();
                return;
            }

            float duration = Mathf.Max(0.01f, fillRevealDuration);
            // 先激活再挂组件，避免未激活节点 AddComponent 异常
            nextNode.gameObject.SetActive(true);
            RestoreSegmentFullWidth(nextIndex, 0f);

            bool mirrorX = IsMirrorSegment(nextNode);
            var sliced = EnsureSlicedFilled(nextNode, mirrorX);
            if (sliced == null)
            {
                SetFilledCount(toFilled);
                RefreshProgressLabel(toFilled, mProgressMaxCnt);
                onComplete?.Invoke();
                return;
            }

            sliced.fillAmount = 0f;
            sliced.SetVerticesDirty();

            DOTween.Kill(this, false);
            AudioController.PlaySound(AudioController.AudioClips.levelComplete);

            // 填充开始时先显示当前进度（如 1/4），延迟后再切到目标进度（如 2/4）
            RefreshProgressLabel(fromFilled, mProgressMaxCnt);
            int labelGen = mProgressAnimGeneration;
            float labelDelay = Mathf.Max(0f, progressLabelSwitchDelay);
            DOVirtual.DelayedCall(labelDelay, () =>
            {
                if (labelGen != mProgressAnimGeneration)
                {
                    return;
                }

                RefreshProgressLabel(toFilled, mProgressMaxCnt);
            }).SetTarget(this);

            DOTween.To(() => sliced.fillAmount, v =>
                {
                    sliced.fillAmount = v;
                }, 1f, duration)
                .SetEase(Ease.Linear)
                .SetTarget(this)
                .OnComplete(() =>
                {
                    ApplySegmentFilled(nextNode, 1f, mirrorX);
                    SetFilledCount(toFilled);
                    RefreshProgressLabel(toFilled, mProgressMaxCnt);
                    onComplete?.Invoke();
                });
        }

        private void PlayOpenBoxAnim()
        {
            mBoxAnimGeneration++;
            int gen = mBoxAnimGeneration;

            AudioController.PlaySound(AudioController.AudioClips.rewardGet);

            // 满格块填充播完后保留进度条，不再隐藏
            if (proBgNode != null)
            {
                proBgNode.SetActive(true);
            }

            if (mProgressMaxCnt > 0)
            {
                SetFilledCount(mProgressMaxCnt);
                RefreshProgressLabel(mProgressMaxCnt, mProgressMaxCnt);
            }

            var openTrackEntry = spine.AnimationState.SetAnimation(0, "open", false);
            spine.AnimationState.AddAnimation(0, "loop", true, 0f);
            if (openTrackEntry != null)
            {
                bool hasTriggeredVNode = false;
                openTrackEntry.Event += (_, spineEvent) =>
                {
                    if (gen != mBoxAnimGeneration || hasTriggeredVNode
                        || spineEvent?.Data == null || spineEvent.Data.Name != VNodeInSpineEventName)
                    {
                        return;
                    }

                    hasTriggeredVNode = true;
                    touchCover.SetActive(false);
                    mOnReadyForBgProgress?.Invoke();
                };
            }
            else
            {
                touchCover.SetActive(false);
                mOnReadyForBgProgress?.Invoke();
            }

            TileGameCore.Tween.DelayedCall(SpineNumLbAppearDelay, () =>
            {
                if (gen != mBoxAnimGeneration)
                {
                    return;
                }

                // 奖励弹出后不显示「点击领取」，直接进入下一关 / 翻倍按钮
                PlaySpineNumLbAppearAnim();
            });
        }

        private void PlaySpineNumLbAppearAnim()
        {
            if (spineNumLb == null)
            {
                return;
            }

            var numberTransform = spineNumLb.rectTransform;
            if (!mHasSpineNumLbDefaultScale)
            {
                mSpineNumLbDefaultScale = numberTransform.localScale;
                mHasSpineNumLbDefaultScale = true;
            }

            DOTween.Kill(numberTransform, false);
            numberTransform.localScale = Vector3.zero;
            spineNumLb.SetActiveEx(true);
            numberTransform.DOScale(mSpineNumLbDefaultScale, SpineNumLbScaleDuration)
                .SetEase(Ease.OutCubic)
                .SetTarget(numberTransform);
        }
    }
}
