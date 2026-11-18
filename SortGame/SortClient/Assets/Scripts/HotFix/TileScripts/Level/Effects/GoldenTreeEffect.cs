using UnityEngine;
using TileGameCore;
using System;

namespace FishTilePlay
{

    public class GoldenTreeEffect : TileEffect, IStackOccludedTileEffect
    {
        [SerializeField] SpriteRenderer treeSpriteRenderer;
        [SerializeField] Sprite[] stageSprites;

        [SerializeField] SpriteRenderer cornerOverlayRenderer;
        [SerializeField] Sprite cornerSprite;

        [SerializeField] Transform[] dstPosTrans;

        [SerializeField] Particle hitParticle;

        private SpriteRenderer iconSpriteRenderer;
        private Vector3[] cornerWorldPoints;
        private TweenClip shakeTweenCase;

        private int stage;
        private bool isX2;

        /// <summary>x2 角标落在四个 pos 中的哪一个；关卡布局初始化时决定一次后冻结，-1 表示尚未决定。</summary>
        private int decidedCornerIndex = -1;

        private static int particleHash;

        public Renderer OverlayRenderer => treeSpriteRenderer;
        public bool IsX2 => isX2;

        public override void Init()
        {
            particleHash = ParticlesDirector.RegisterParticle(hitParticle);
        }

        public override void OnCreated(TileActor tileBehavior)
        {
            iconSpriteRenderer = linkedTile.IconSpriteRenderer;

            stage = 0;
            isX2 = false;
            decidedCornerIndex = -1;
            treeSpriteRenderer.sprite = stageSprites[0];
            EnsureCornerOverlayRenderer();
            if (cornerOverlayRenderer != null && cornerSprite != null)
            {
                cornerOverlayRenderer.sprite = cornerSprite;
            }
            ApplyOverlaySortingOrder();
            HideCornerOverlay();
            HideAllPosNodes();
            RefreshVisibility();
        }

        public override void OnStateChanged(bool state)
        {
            RefreshVisibility();
        }

        public override void OnSortingOrderChanged(int order)
        {
            ApplyOverlaySortingOrder();
            RefreshActivePosNodeSortingOrder();
        }

        public override void ApplyTintColor(Color color)
        {
            if (treeSpriteRenderer != null)
                treeSpriteRenderer.color = color;

            if (cornerOverlayRenderer != null)
                cornerOverlayRenderer.color = color;

            if (dstPosTrans == null)
                return;

            for (int i = 0; i < dstPosTrans.Length; i++)
            {
                Transform posTransform = dstPosTrans[i];
                if (posTransform == null)
                    continue;

                var spriteRenderers = posTransform.GetComponentsInChildren<SpriteRenderer>(true);
                for (int j = 0; j < spriteRenderers.Length; j++)
                {
                    if (spriteRenderers[j] != null)
                        spriteRenderers[j].color = color;
                }
            }
        }

        /// <summary>解冻角标落点，使下一次刷新按当前棋盘重新决定（如打乱道具重排棋盘后调用）。</summary>
        public void ResetDecidedCornerIndex()
        {
            decidedCornerIndex = -1;
        }

        public void RefreshVisibility()
        {
            if (treeSpriteRenderer == null || linkedTile == null)
            {
                return;
            }

            HideCornerOverlay();
            gameObject.SetActive(true);

            LevelGenService levelGenFunc = LevelDirector.LevelGenService;
            if (levelGenFunc != null && IsOverlayBlocked(levelGenFunc))
            {
                SetTreeSpriteVisible(false);
                HideAllPosNodes();
                return;
            }

            SetTreeSpriteVisible(true);

            if (!isX2)
            {
                HideAllPosNodes();
                return;
            }

            if (levelGenFunc != null && levelGenFunc.IsGoldenTreeCornerLockDeferred)
            {
                HideAllPosNodes();
                return;
            }

            int corner = GetDecidedCornerIndex(levelGenFunc);
            ShowX2PosNode(corner);
        }

        /// <summary>
        /// x2 角标显示在四个 pos 中的哪一个：关卡布局初始化时（IsLevelLoaded 后）决定一次便冻结，
        /// 之后棋盘变化（上层棋子被消除等）不再重新计算，避免角标位置跳动。
        /// </summary>
        int GetDecidedCornerIndex(LevelGenService levelGenFunc)
        {
            if (decidedCornerIndex >= 0)
            {
                return decidedCornerIndex;
            }

            int corner;
            if (levelGenFunc == null)
            {
                corner = 0;
            }
            else if (!TileStackUtil.HasUpperOverlap(linkedTile, levelGenFunc.ActTiles))
            {
                corner = 0;
            }
            else
            {
                RefreshCornerWorldPoints();
                corner = TileStackUtil.GetHighestPriorityExposedCornerIndex(
                    linkedTile, cornerWorldPoints, levelGenFunc.ActTiles);
            }

            // 棋盘生成完成（IsBoardSpawned）后即锁定，使角标在棋子生成时就定位、无需等关卡弹窗结束；
            // 生成过程中（棋盘尚不完整）不锁定，corner < 0（无可用角）时也不锁定，等下次可见时再决定。
            // 打乱动画期间延迟锁定，避免 scale 未到位时按错误遮挡锁死角标。
            if (corner >= 0 && levelGenFunc != null && levelGenFunc.IsBoardSpawned
                && !levelGenFunc.IsGoldenTreeCornerLockDeferred)
            {
                decidedCornerIndex = corner;
            }

            return corner;
        }

        bool IsOverlayBlocked(LevelGenService levelGenFunc)
        {
            if (TileStackUtil.HasStrongStackUpperBlock(linkedTile, levelGenFunc.ActTiles))
                return true;

            // 角标能藏、Sprite 不能藏：四角点已被上层盖住，但面积比没到 80%。
            // Sprite 与角标用同一套「没有露出角」判定。
            if (!TileStackUtil.HasUpperOverlap(linkedTile, levelGenFunc.ActTiles))
                return false;

            RefreshCornerWorldPoints();
            return TileStackUtil.GetHighestPriorityExposedCornerIndex(
                linkedTile, cornerWorldPoints, levelGenFunc.ActTiles) < 0;
        }

        void SetTreeSpriteVisible(bool visible)
        {
            if (treeSpriteRenderer == null)
                return;
            treeSpriteRenderer.enabled = visible;
            treeSpriteRenderer.gameObject.SetActive(visible);
        }

        void HideAllPosNodes()
        {
            if (dstPosTrans == null)
            {
                return;
            }

            for (int i = 0; i < dstPosTrans.Length; i++)
            {
                Transform cornerTransform = dstPosTrans[i];
                if (cornerTransform != null)
                {
                    cornerTransform.gameObject.SetActive(false);
                }
            }
        }

        void ShowX2PosNode(int cornerIndex)
        {
            HideAllPosNodes();
            if (cornerIndex < 0 || dstPosTrans == null || !dstPosTrans.IsInRange(cornerIndex))
            {
                return;
            }

            Transform cornerTransform = dstPosTrans[cornerIndex];
            if (cornerTransform != null)
            {
                cornerTransform.gameObject.SetActive(true);
                ApplyPosNodeSortingOrder(cornerTransform);
            }
        }

        void ApplyPosNodeSortingOrder(Transform posTransform)
        {
            // pos 角标需高于 treeSprite（icon+1），长按时 SetSortingOrder 会抬高层级，此处同步刷新。
            int sortingOrder = linkedTile.IconSpriteRenderer.sortingOrder + 2;
            // 用 Renderer 覆盖 pos 底图(SpriteRenderer)与其下 Text(TMP)(MeshRenderer)，让文字与 pos 同层级。
            var renderers = posTransform.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sortingOrder = sortingOrder;
            }
        }

        void RefreshActivePosNodeSortingOrder()
        {
            if (dstPosTrans == null || !isX2)
            {
                return;
            }

            int corner = decidedCornerIndex;
            if (corner >= 0 && dstPosTrans.IsInRange(corner))
            {
                Transform cornerTransform = dstPosTrans[corner];
                if (cornerTransform != null && cornerTransform.gameObject.activeSelf)
                {
                    ApplyPosNodeSortingOrder(cornerTransform);
                    return;
                }
            }

            for (int i = 0; i < dstPosTrans.Length; i++)
            {
                Transform cornerTransform = dstPosTrans[i];
                if (cornerTransform != null && cornerTransform.gameObject.activeSelf)
                {
                    ApplyPosNodeSortingOrder(cornerTransform);
                    return;
                }
            }
        }

        void EnsureCornerOverlayRenderer()
        {
            if (cornerOverlayRenderer != null)
            {
                return;
            }

            Transform overlayTransform = transform.Find("CornerOverlay");
            if (overlayTransform != null)
            {
                cornerOverlayRenderer = overlayTransform.GetComponent<SpriteRenderer>();
            }
        }

        void RefreshCornerWorldPoints()
        {
            if (dstPosTrans == null)
            {
                return;
            }

            if (cornerWorldPoints == null || cornerWorldPoints.Length != dstPosTrans.Length)
            {
                cornerWorldPoints = new Vector3[dstPosTrans.Length];
            }

            for (int i = 0; i < dstPosTrans.Length; i++)
            {
                Transform cornerTransform = dstPosTrans[i];
                cornerWorldPoints[i] = cornerTransform != null ? cornerTransform.position : Vector3.zero;
            }
        }

        void ApplyOverlaySortingOrder()
        {
            int sortingOrder = linkedTile.IconSpriteRenderer.sortingOrder + 1;
            treeSpriteRenderer.sortingOrder = sortingOrder;

            if (cornerOverlayRenderer != null)
            {
                cornerOverlayRenderer.sortingOrder = sortingOrder;
            }
        }

        void HideCornerOverlay()
        {
            if (cornerOverlayRenderer != null)
            {
                cornerOverlayRenderer.enabled = false;
            }
        }

        public override void OnDisabled(TileActor tileBehavior)
        {
            KillShakeTween();
        }

        void KillShakeTween()
        {
            shakeTweenCase.KillActive();
            shakeTweenCase = null;
        }

        public override void OnAnyTileSubmitted(bool bFromHintItem = false)
        {
            // GoldTree should only change when the GoldTree tile itself is collected.
        }

        public override void Clear()
        {
            KillShakeTween();

            Destroy(gameObject);
        }

        public override bool IsClickAllowed()
        {
            return true;
        }

        public static string SerializeState(bool isX2, int stage = 0)
        {
            var data = new EffectModel { stage = stage, isX2 = isX2 };
            return JsonUtility.ToJson(data);
        }

        public static bool ParseIsX2(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return false;
            }

            try
            {
                var effectData = JsonUtility.FromJson<EffectModel>(data);
                return effectData.isX2;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 获取Ice效果的序列化数据
        /// </summary>
        /// <returns>包含stage信息的JSON数据</returns>
        public override string GetEffectData()
        {
            return SerializeState(isX2, stage);
        }

        /// <summary>
        /// 从序列化数据恢复Ice效果状态
        /// </summary>
        /// <param name="data">包含stage信息的JSON数据</param>
        public override void SetEffectData(string data)
        {
            if (string.IsNullOrEmpty(data)) return;

            try
            {
                var effectData = JsonUtility.FromJson<EffectModel>(data);
                stage = effectData.stage;
                isX2 = effectData.isX2;

                // 恢复对应的sprite状态
                if (stageSprites.IsInRange(stage))
                {
                    treeSpriteRenderer.sprite = stageSprites[stage];
                }

                RefreshVisibility();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to restore Ice effect data: {e.Message}");
            }
        }

        [System.Serializable]
        private struct EffectModel
        {
            public int stage;
            public bool isX2;
        }
    }
}
