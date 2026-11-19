using System.Collections.Generic;
using UnityEngine;

namespace FishXSortPlay
{
    /// <summary>
    /// BoxCover 专用布局：收集条贴在顶栏下面，棋盘用剩下的高度，在 Dock 与技能按钮之间等比适配。
    /// </summary>
    public static class BoxCoverLayout
    {
        public const float DockHudReserveDown = 1f;
        const float LegacyPlayfieldBandPadding = 0.35f;
        public static readonly Vector3 DefaultHudAnchorLocal = new(-1.59f, 0.9f, 0f);

        public static void PlaceDockForHud(DockActor dock)
        {
            if (dock == null)
                return;

            dock.RestoreAuthoredPosition();
            dock.ApplyWorldYOffset(-DockHudReserveDown);
        }

        public static float ApplyPlayfield(
            DockActor dock,
            Transform boardRoot,
            LevelFitter scaler,
            UIGame gameUI)
        {
            PlaceBoxCoverDockForHud(dock, gameUI);

            if (boardRoot == null)
                throw new MissingReferenceException("BoxCover board root is required.");

            if (!TryGetBoardWorldYRange(boardRoot, out float minY, out float maxY))
                throw new System.InvalidOperationException(
                    "BoxCover board bounds are unavailable after level generation.");

            Camera gameCamera = RequireGameCamera();
            // 固定上移只改变 Dock 视觉位置；棋盘仍使用上移前的边界。
            float topY = dock.GetRequiredBoxCoverBoardTopWorldY()
                - dock.BoxCoverDockUpOffset;
            float bottomY = gameUI.GetRequiredBoxCoverBoardBottomWorldY(gameCamera);
            if (topY <= bottomY)
            {
                throw new System.InvalidOperationException(
                    $"Invalid BoxCover board band: top={topY:F3}, bottom={bottomY:F3}.");
            }

            return FitBoardInBand(
                boardRoot,
                minY,
                maxY,
                topY,
                bottomY,
                scaler,
                dock.BoxCoverBoardClearance);
        }

        /// <summary>
        /// 只供 BoxCover 使用：预制体锚点直接对齐，不读取动画包围盒，也没有估算兜底。
        /// </summary>
        public static void PlaceBoxCoverDockForHud(DockActor dock, UIGame gameUI)
        {
            if (dock == null)
                throw new MissingReferenceException("BoxCover DockActor is required.");
            if (gameUI == null)
                throw new MissingReferenceException("BoxCover UIGame reference is required.");

            dock.RestoreAuthoredPosition();
            BoxCoverManager.RelayoutHud();
            AlignDockToRequiredTopBoundary(dock, gameUI);
            BoxCoverManager.RelayoutHud();
        }

        /// <summary>
        /// IQ Challenge 保留修改前的特殊 Dock 与棋盘规则，避免 BoxCover 调整外溢。
        /// </summary>
        public static float ApplyLegacySpecialPlayfield(
            DockActor dock,
            Transform boardRoot,
            LevelFitter scaler)
        {
            PlaceDockForHud(dock);
            BoxCoverManager.RelayoutHud();

            if (boardRoot == null)
                return 1f;
            if (!TryGetBoardWorldYRange(boardRoot, out float minY, out float maxY))
                return 1f;
            if (!TryResolveVerticalBand(dock, scaler, out float topY, out float bottomY))
                return 1f;

            return FitBoardInBandLegacy(boardRoot, minY, maxY, topY, bottomY, scaler);
        }

        static void AlignDockToRequiredTopBoundary(DockActor dock, UIGame gameUI)
        {
            Camera gameCamera = RequireGameCamera();
            float barBottom = gameUI.GetRequiredBoxCoverTopBoundaryWorldY(gameCamera);
            float coverTop = dock.GetRequiredBoxCoverVisualTopWorldY();
            float delta = barBottom
                - dock.BoxCoverTopClearance
                - coverTop
                + dock.BoxCoverDockUpOffset;
            if (Mathf.Abs(delta) >= 0.01f)
                dock.ApplyWorldYOffset(delta);

#if UNITY_EDITOR
            Debug.Log(
                $"[BoxCoverFit] barBottom={barBottom:F2}, coverTop={coverTop:F2}, delta={delta:F2}, " +
                $"dockY={dock.transform.position.y:F2}");
#endif
        }

        static Camera RequireGameCamera()
        {
            Camera gameCamera = Camera.main;
            if (gameCamera == null)
                throw new MissingReferenceException("Main gameplay camera is required for BoxCover layout.");
            return gameCamera;
        }

        public static bool TryGetRectEdgeWorldY(
            RectTransform rect,
            Camera gameCamera,
            bool top,
            out float worldY)
        {
            worldY = 0f;
            if (rect == null || gameCamera == null)
                return false;

            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Camera uiCamera = null;
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                uiCamera = canvas.worldCamera;

            float edge = top ? float.MinValue : float.MaxValue;
            for (int i = 0; i < 4; i++)
            {
                float screenY = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[i]).y;
                edge = top ? Mathf.Max(edge, screenY) : Mathf.Min(edge, screenY);
            }

            if (top ? edge <= float.MinValue + 1f : edge >= float.MaxValue - 1f)
                return false;

            float depth = Mathf.Abs(gameCamera.transform.position.z);
            if (depth < 0.01f)
                depth = 10f;

            worldY = gameCamera.ScreenToWorldPoint(new Vector3(Screen.width * 0.5f, edge, depth)).y;
            return true;
        }

        public static float FitBoardInBand(
            Transform boardRoot,
            float boardMinY,
            float boardMaxY,
            float bandTopY,
            float bandBottomY,
            LevelFitter scaler,
            float padding)
        {
            float safePadding = Mathf.Max(0f, padding);
            float bandTop = bandTopY - safePadding;
            float bandBottom = bandBottomY + safePadding;
            float boardHeight = boardMaxY - boardMinY;
            float bandHeight = bandTop - bandBottom;
            return FitTransformYRange(
                boardRoot,
                boardMinY,
                boardMaxY,
                bandTop,
                bandBottom,
                ComputePlayfieldScale(boardHeight, bandHeight));
        }

        /// <summary>
        /// 直接按实际棋盘高度适配可用带；不放大，可用带已由 Dock / 道具边界决定。
        /// </summary>
        public static float ComputePlayfieldScale(float boardHeight, float bandHeight)
        {
            float reference = Mathf.Max(boardHeight, 0.01f);
            float fitScale = bandHeight > 0.01f ? bandHeight / reference : 1f;
            return Mathf.Clamp(fitScale, 0.35f, 1f);
        }

        public static float FitBoardInBandLegacy(
            Transform boardRoot,
            float boardMinY,
            float boardMaxY,
            float bandTopY,
            float bandBottomY,
            LevelFitter scaler)
        {
            float bandTop = bandTopY - LegacyPlayfieldBandPadding;
            float bandBottom = bandBottomY + LegacyPlayfieldBandPadding;
            float boardHeight = boardMaxY - boardMinY;
            float fieldHeight = scaler != null && scaler.LevelXSortsFieldSize.y > 0.01f
                ? scaler.LevelXSortsFieldSize.y
                : boardHeight;
            float bandHeight = bandTop - bandBottom;
            float reference = Mathf.Max(boardHeight, fieldHeight, 0.01f);
            float reserveScale = reference > DockHudReserveDown
                ? (reference - DockHudReserveDown) / reference
                : 0.5f;
            float fitScale = bandHeight > 0.01f ? bandHeight / reference : 1f;
            float scale = Mathf.Clamp(Mathf.Min(reserveScale, fitScale), 0.35f, 1f);
            return FitTransformYRange(
                boardRoot,
                boardMinY,
                boardMaxY,
                bandTop,
                bandBottom,
                scale);
        }

        public static bool TryGetBoardWorldYRange(Transform boardRoot, out float minY, out float maxY)
        {
            minY = float.MaxValue;
            maxY = float.MinValue;
            bool any = false;
            Vector2 XSortSize = LevelFitter.XSortSize;
            if (XSortSize.y <= 0.0001f)
                XSortSize = Vector2.one;

            float parentScaleY = boardRoot != null ? Mathf.Abs(boardRoot.lossyScale.y) : 1f;
            if (parentScaleY <= 1e-4f)
                parentScaleY = 1f;

            float XSortHalf = XSortSize.y * 0.5f * parentScaleY;
            List<XSortActor> XSorts = LevelDirector.LevelGenService?.ActXSorts;
            if (XSorts != null)
            {
                for (int i = 0; i < XSorts.Count; i++)
                {
                    XSortActor XSort = XSorts[i];
                    if (XSort == null || XSort.IsSubmitted)
                        continue;

                    float centerY = XSort.transform.position.y;
                    if (centerY - XSortHalf < minY)
                        minY = centerY - XSortHalf;
                    if (centerY + XSortHalf > maxY)
                        maxY = centerY + XSortHalf;
                    any = true;
                }
            }

            List<BoxCoverBehavior> boxes = new();
            BoxCoverManager.CollectActiveBehaviors(boxes);
            for (int i = 0; i < boxes.Count; i++)
            {
                BoxCoverBehavior box = boxes[i];
                if (box == null)
                    continue;

                float visualY = box.VisualLocalSize.y;
                float boxHalf = (visualY > 0.01f
                    ? visualY
                    : XSortSize.y * box.Placement.CellSize.y) * 0.5f * parentScaleY;
                float centerY = box.transform.position.y;
                if (centerY - boxHalf < minY)
                    minY = centerY - boxHalf;
                if (centerY + boxHalf > maxY)
                    maxY = centerY + boxHalf;
                any = true;
            }

            return any;
        }

        public static bool TryResolveVerticalBand(
            DockActor dock,
            LevelFitter scaler,
            out float topY,
            out float bottomY)
        {
            topY = dock != null ? dock.GetSlotsBottomWorldY() : float.NaN;
            bottomY = float.NaN;
            if (float.IsNaN(topY))
                return false;

            Camera gameCamera = Camera.main;
            float cameraHeight = gameCamera != null ? gameCamera.orthographicSize * 2f : 15.6f;
            float cameraBottom = gameCamera != null
                ? gameCamera.transform.position.y - gameCamera.orthographicSize
                : -cameraHeight * 0.5f;

            float skillTop = float.NaN;
            PUUIDirector powerUps = PUDirector.PowerUpsUIController;
            if (powerUps != null && powerUps.TryGetButtonsTopWorldY(gameCamera, out float measuredSkillTop))
                skillTop = measuredSkillTop;

            // 场底比真实按钮高一截，不能当技能上沿，否则棋盘会被顶上去。
            // 换算失败时按屏幕底部约 12% 估按钮顶。
            float estimatedSkillTop = cameraBottom + cameraHeight * 0.12f;
            if (!float.IsNaN(skillTop)
                && skillTop > cameraBottom + 0.05f
                && skillTop < topY - 0.05f)
            {
                bottomY = skillTop;
            }
            else
            {
                bottomY = estimatedSkillTop;
            }

            return !float.IsNaN(bottomY) && topY > bottomY + 0.05f;
        }

        public static float FitTransformYRange(
            Transform root,
            float currentMinY,
            float currentMaxY,
            float bandTopY,
            float bandBottomY,
            float uniformScale)
        {
            if (root == null)
                return 1f;

            float currentScale = Mathf.Abs(root.localScale.y);
            if (currentScale <= 1e-4f)
                currentScale = 1f;

            float scale = Mathf.Max(0.35f, uniformScale);
            float boardCenterY = (currentMinY + currentMaxY) * 0.5f;
            Vector3 position = root.position;
            float scaleRatio = scale / currentScale;
            float newBoardCenterY = position.y + (boardCenterY - position.y) * scaleRatio;
            float targetCenterY = (bandTopY + bandBottomY) * 0.5f;

            root.localScale = new Vector3(scale, scale, root.localScale.z);
            position.y += targetCenterY - newBoardCenterY;
            root.position = position;
            return scale;
        }

        public static DockActor FindDock()
        {
            if (LevelDirector.IsLevelLoaded)
                return LevelDirector.Dock;

            return Object.FindObjectOfType<DockActor>();
        }
    }
}
