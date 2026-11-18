using Unity.Mathematics;
using UnityEngine;

namespace FishTilePlay
{
    public class LevelFitter: MonoBehaviour
    {
        private const float DesignCameraHeight = 15.6f;
        private const float DesignCameraWidth = 7.2f;
        private const float DesignScreenHeightWidthRatio = 1560f / 720f;

        [SerializeField] Offset levelFieldOffset;
        [SerializeField] Offset levelTilesFieldOffset;

        [Header("屏幕适配设置")]
        [SerializeField] bool useFixedVerticalSize = true; // 是否使用固定纵向尺寸
        [SerializeField] float fixedCameraHeight = 15.6f; // 固定纵向尺寸值
        [SerializeField] Vector2 verticalSizeRange = new Vector2(8f, 12f); // 纵向尺寸范围（如果不使用固定值）

        public Vector2 LevelFieldCenter { get; private set; }
        public Vector2 LevelFieldSize { get; private set; }
        public Vector2 LevelTilesFieldCenter { get; private set; }
        public Vector2 LevelTilesFieldSize { get; private set; }

        public static Vector2 TileSize { get; private set; }
        public static Vector2 OddLevelSize { get; private set; }
        public static Vector2 EvenLevelSize { get; private set; }

        public static Vector2 SlotSize { get; private set; }
        private static Vector3 gDeltaPos;

        public void Init()
        {
            InitField(applyCameraSize: true);
        }

        /// <summary>
        /// Edit-mode preview: compute LevelFieldCenter/Size with the same rules as play mode,
        /// without writing Camera.orthographicSize into the scene.
        /// </summary>
        public void InitForEditorPreview()
        {
            InitField(applyCameraSize: false);
        }

        private void InitField(bool applyCameraSize)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = UnityEngine.Object.FindObjectOfType<Camera>();
            }

            if (mainCamera == null)
            {
                ApplyFieldsFromCameraSize(DesignCameraWidth, DesignCameraHeight);
                SlotSize = Vector2.one;
                return;
            }

            float cameraHeight = mainCamera.orthographicSize * 2;
            fixedCameraHeight = DesignCameraHeight;

            float screenHeightWidthRatio = Screen.width > 0 ? (float)Screen.height / Screen.width : 0f;
            if (screenHeightWidthRatio > DesignScreenHeightWidthRatio)
            {
                // 超过设计高宽比时，提升相机可视高度，保证设计宽度完整可见
                float targetHeight = DesignCameraWidth * screenHeightWidthRatio;
                cameraHeight = Mathf.Max(cameraHeight, targetHeight);
            }

            if (useFixedVerticalSize)
            {
                // 方案1：使用固定纵向尺寸，并在超长屏下做最小高度兜底
                cameraHeight = Mathf.Max(fixedCameraHeight, cameraHeight);
            }
            else
            {
                // 方案2：使用屏幕高度作为基准，但限制最小/最大值
                cameraHeight = Mathf.Clamp(cameraHeight, verticalSizeRange.x, verticalSizeRange.y);
            }

            if (applyCameraSize)
            {
                mainCamera.orthographicSize = cameraHeight * 0.5f;
            }

            // 计算横向尺寸（可以基于屏幕比例，但不影响纵向）
            float aspect = mainCamera.aspect > 0f ? mainCamera.aspect : (DesignCameraWidth / DesignCameraHeight);
            float cameraWidth = cameraHeight * aspect;
            if (cameraWidth > DesignCameraWidth)
            {
                cameraWidth = DesignCameraWidth;
            }

            // 相机可以加高，保证细长屏仍能看到设计宽度。
            // 棋子区高度锁在 720×1560，避免棋子跟着变大后从左右超出屏幕。
            float layoutCameraHeight = Mathf.Min(cameraHeight, DesignCameraHeight);
            ApplyFieldsFromCameraSize(cameraWidth, layoutCameraHeight);

            SlotSize = new Vector2(LevelFieldSize.x / 8, LevelFieldSize.x / 8);

            SlotSize = SlotSize * 1.5f;
            SlotSize = new Vector2(Mathf.Clamp(SlotSize.x, 0.4f, 1.8f), Mathf.Clamp(SlotSize.y, 0.4f, 1.8f));
        }

        /// <summary>
        /// Crop Level Field and Level Tiles Field from camera size using their respective offsets.
        /// Y center uses the same extra -3 shift as the original Level Field.
        /// </summary>
        private void ApplyFieldsFromCameraSize(float cameraWidth, float cameraHeight)
        {
            ComputeFieldFromOffset(levelFieldOffset, cameraWidth, cameraHeight, out Vector2 levelCenter, out Vector2 levelSize);
            LevelFieldCenter = levelCenter;
            LevelFieldSize = levelSize;

            ComputeFieldFromOffset(levelTilesFieldOffset, cameraWidth, cameraHeight, out Vector2 tilesCenter, out Vector2 tilesSize);
            LevelTilesFieldCenter = tilesCenter;
            LevelTilesFieldSize = tilesSize;
        }

        private static void ComputeFieldFromOffset(
            Offset offset,
            float cameraWidth,
            float cameraHeight,
            out Vector2 center,
            out Vector2 size)
        {
            float leftBorder = (offset.left - 0.5f) * cameraWidth;
            float rightBorder = (0.5f - offset.right) * cameraWidth;
            float bottomBorder = (offset.bottom - 0.5f) * cameraHeight;
            float topBorder = (0.5f - offset.top) * cameraHeight;

            center = new Vector2((leftBorder + rightBorder) / 2f, (bottomBorder + topBorder - 3f) / 2f);
            size = new Vector2(rightBorder - leftBorder, topBorder - bottomBorder);
        }

        public void Recalculate()
        {
            gDeltaPos = GameDirector.levelController.GetShowDelta();

            var biggerLayerSize = GameDirector.levelController.GetMinShowSize();

            var showLayerScale = GameDirector.levelController.GetShowScale();

            TileSize = CalculateTileSize(LevelTilesFieldSize, biggerLayerSize, showLayerScale, GameDirector.Data.MaxTileSize, GameDirector.Data.TileSize);
            OddLevelSize = new Vector2(TileSize.x * LevelDirector.OddLayerSize.x, TileSize.y * LevelDirector.OddLayerSize.y);
            EvenLevelSize = new Vector2(TileSize.x * LevelDirector.EvenLayerSize.x, TileSize.y * LevelDirector.EvenLayerSize.y);
        }

        /// <summary>
        /// Shared tile-size math for runtime and Level Editor GameScene preview.
        /// </summary>
        public static Vector2 CalculateTileSize(Vector2 levelFieldSize, Vector2Int biggerLayerSize, float showLayerScale, float maxTileSize, Vector2 dataTileSize)
        {
            int safeWidth = Mathf.Max(1, biggerLayerSize.x);
            int safeHeight = Mathf.Max(1, biggerLayerSize.y);

            var tileSizeX = levelFieldSize.x / safeWidth;
            var tileSizeY = levelFieldSize.y / safeHeight;

            var refTileSize = Vector2.one * Mathf.Clamp(Mathf.Min(tileSizeX, tileSizeY), 0, maxTileSize);

            Vector2 tileSize;
            if (dataTileSize == Vector2.one)
            {
                tileSize = refTileSize;
            }
            else if (dataTileSize.x > dataTileSize.y)
            {
                tileSize = new Vector2(refTileSize.x, refTileSize.y / dataTileSize.x * dataTileSize.y);
            }
            else
            {
                tileSize = new Vector2(refTileSize.x / dataTileSize.y * dataTileSize.x, refTileSize.y);
            }

            return tileSize * 1.05f * showLayerScale;
        }

        public static Vector3 GetPosition(ElementPosition elementPosition)
        {
            int layerID = elementPosition.LayerId;

            var halfSize = TileSize / 2f;

            // 获取该层级的delta偏移量
            Layer layer = LevelDirector.Level.GetLayer(layerID);
            float deltaX = layer != null ? layer.DeltaX : 0f;
            float deltaY = layer != null ? layer.DeltaY : 0f;

            CellModel cellData = layer != null ? layer.GetRow(elementPosition.Y)?.GetCell(elementPosition.X) : null;
            if (cellData != null)
            {
                deltaX += cellData.DeltaX;
                deltaY += cellData.DeltaY;
            }

            // 应用delta偏移量（乘以TileSize，与编辑器中的逻辑一致）
            float deltaOffsetX = deltaX * TileSize.x;
            float deltaOffsetY = deltaY * TileSize.y;

            Vector3 retVector;
            if ((LevelDirector.Level.AmountOfLayers + 1 - layerID) % 2 == 0)
            {
                retVector =
                new Vector3(-EvenLevelSize.x / 2f + elementPosition.X * TileSize.x + halfSize.x + deltaOffsetX,
                                    -EvenLevelSize.y / 2f + halfSize.y + elementPosition.Y * TileSize.y + deltaOffsetY,
                                    layerID); // deltaY正值向上移动

            }
            else
            {
                retVector = new Vector3(-OddLevelSize.x / 2f + elementPosition.X * TileSize.x + halfSize.x + deltaOffsetX,
                                   -OddLevelSize.y / 2f + halfSize.y + elementPosition.Y * TileSize.y + deltaOffsetY,
                                    layerID); // deltaY正值向上移动
            }

            return retVector + new Vector3(TileSize.x * gDeltaPos.x , TileSize.y * gDeltaPos.y);
        }

        /// <summary>
        /// Same local-position formula as play mode, for Level Editor preview (no LevelDirector dependency).
        /// </summary>
        public static Vector3 GetPreviewLocalPosition(
            ElementPosition elementPosition,
            int totalLayerCount,
            Vector2 tileSize,
            Vector2Int evenLayerSizeCells,
            Vector2Int oddLayerSizeCells,
            Vector2 layerAndCellDelta,
            Vector2 showLayerDelta)
        {
            Vector2 halfSize = tileSize / 2f;
            float deltaOffsetX = layerAndCellDelta.x * tileSize.x;
            float deltaOffsetY = layerAndCellDelta.y * tileSize.y;

            Vector2 evenLevelSize = new Vector2(tileSize.x * evenLayerSizeCells.x, tileSize.y * evenLayerSizeCells.y);
            Vector2 oddLevelSize = new Vector2(tileSize.x * oddLayerSizeCells.x, tileSize.y * oddLayerSizeCells.y);

            Vector3 retVector;
            if ((totalLayerCount + 1 - elementPosition.LayerId) % 2 == 0)
            {
                retVector = new Vector3(
                    -evenLevelSize.x / 2f + elementPosition.X * tileSize.x + halfSize.x + deltaOffsetX,
                    -evenLevelSize.y / 2f + halfSize.y + elementPosition.Y * tileSize.y + deltaOffsetY,
                    elementPosition.LayerId);
            }
            else
            {
                retVector = new Vector3(
                    -oddLevelSize.x / 2f + elementPosition.X * tileSize.x + halfSize.x + deltaOffsetX,
                    -oddLevelSize.y / 2f + halfSize.y + elementPosition.Y * tileSize.y + deltaOffsetY,
                    elementPosition.LayerId);
            }

            return retVector + new Vector3(tileSize.x * showLayerDelta.x, tileSize.y * showLayerDelta.y);
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying)
            {
                InitForEditorPreview();
            }

            // Level Field
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(LevelFieldCenter, LevelFieldSize);

            // Level Tiles Field
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(LevelTilesFieldCenter, LevelTilesFieldSize);
        }

        [System.Serializable]
        public struct Offset
        {
            [Range(0, 1)] public float left;
            [Range(0, 1)] public float right;
            [Range(0, 1)] public float top;
            [Range(0, 1)] public float bottom;
        }
    }
}