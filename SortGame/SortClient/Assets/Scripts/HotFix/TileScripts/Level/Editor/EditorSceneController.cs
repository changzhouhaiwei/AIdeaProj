#pragma warning disable 649

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using TileGameCore;

namespace FishTilePlay
{
    public class EditorSceneController
    {
        private static readonly int OVERLAY_STRENGTH_ID = Shader.PropertyToID("_OverlayStrength");
        private const string BOX_COVER_PREVIEW_NAME = "[Box Cover preview]";
        private static readonly Dictionary<string, Sprite> boxCoverSprites = new();
        public GameObject[] prefabs;

        public Vector2Int EvenLayerSize;
        public Vector2Int OddLayerSize;
        public Vector2 TileSize = Vector2.one;
        public Vector2 ShowLayerDelta;

        private Dictionary<Vector3Int, GameObject> matchables; //z - layer index
        private List<GameObject> layerGameobjects;
        private GameObject tempGameObject;
        private Material enabledMaterial;
        private Material disabledMaterial;
        private List<int> enabledPrefabIndexes;
        public int setsAmount;

        private GameObject parentGameObject;
        private int totalLayerCount;
        private readonly List<BoxCoverPreview> boxCoverPreviews;
        private BoxCoverSize selectedBoxCoverSize = BoxCoverSize.Size2x2;

        private sealed class BoxCoverPreview
        {
            public GameObject gameObject;
            public SpriteRenderer renderer;
            public int layerIndex;
            public Vector2Int anchor;
            public BoxCoverSize size;
        }

        // Delta偏移量存储
        private Dictionary<int, Vector2> layerDeltaOffsets;
        private Dictionary<Vector3Int, Vector2> cellDeltaOffsets;

        /// <summary>
        /// False when the preview root was destroyed (e.g. after switching scenes).
        /// </summary>
        public bool IsValid => parentGameObject != null;

        /// <summary>HideAndDontSave preview root (null after Destroy).</summary>
        public GameObject Root => parentGameObject;

        public EditorSceneController(GameObject parentGameObject)
        {
            this.parentGameObject = parentGameObject;

            matchables = new Dictionary<Vector3Int, GameObject>();
            layerGameobjects = new List<GameObject>();
            enabledPrefabIndexes = new List<int>();
            layerDeltaOffsets = new Dictionary<int, Vector2>();
            cellDeltaOffsets = new Dictionary<Vector3Int, Vector2>();
            boxCoverPreviews = new List<BoxCoverPreview>();
            totalLayerCount = 0;
        }

        public Vector2 TilesFieldSize;
        Vector3 TilesFieldCenter;

        public void SetFieldCenter(Vector3 fieldCenter)
        {
            TilesFieldCenter = fieldCenter;
            if (parentGameObject != null)
            {
                parentGameObject.transform.localScale = Vector3.one;
                parentGameObject.transform.position = fieldCenter;
            }
        }

        public bool TryGetPreviewBoardYRange(out float minY, out float maxY)
        {
            minY = float.MaxValue;
            maxY = float.MinValue;
            bool any = false;
            foreach (KeyValuePair<Vector3Int, GameObject> pair in matchables)
            {
                GameObject tileObject = pair.Value;
                if (tileObject == null)
                    continue;

                Renderer renderer = tileObject.GetComponentInChildren<Renderer>();
                if (renderer == null)
                    continue;

                if (renderer.bounds.min.y < minY)
                    minY = renderer.bounds.min.y;
                if (renderer.bounds.max.y > maxY)
                    maxY = renderer.bounds.max.y;
                any = true;
            }

            for (int i = 0; i < boxCoverPreviews.Count; i++)
            {
                BoxCoverPreview preview = boxCoverPreviews[i];
                if (preview.renderer == null)
                    continue;

                if (preview.renderer.bounds.min.y < minY)
                    minY = preview.renderer.bounds.min.y;
                if (preview.renderer.bounds.max.y > maxY)
                    maxY = preview.renderer.bounds.max.y;
                any = true;
            }

            return any;
        }

        public void ApplyBoxCoverPlayfield(DockActor dock, UIGame gameUI)
        {
            BoxCoverLayout.PlaceBoxCoverDockForHud(dock, gameUI);
            if (parentGameObject == null || !TryGetPreviewBoardYRange(out float minY, out float maxY))
                return;

            LevelFitter scaler = UnityEngine.Object.FindObjectOfType<LevelFitter>();
            Camera gameCamera = Camera.main;
            if (gameCamera == null)
                throw new MissingReferenceException("Main gameplay camera is required for BoxCover preview.");
            float topY = dock.GetRequiredBoxCoverBoardTopWorldY();
            float bottomY = gameUI.GetRequiredBoxCoverBoardBottomWorldY(gameCamera);

            BoxCoverLayout.FitBoardInBand(
                parentGameObject.transform,
                minY,
                maxY,
                topY,
                bottomY,
                scaler,
                dock.BoxCoverBoardClearance);
        }

        public void ApplyLegacySpecialPlayfield(DockActor dock)
        {
            BoxCoverLayout.PlaceDockForHud(dock);
            if (parentGameObject == null || !TryGetPreviewBoardYRange(out float minY, out float maxY))
                return;

            LevelFitter scaler = UnityEngine.Object.FindObjectOfType<LevelFitter>();
            if (!BoxCoverLayout.TryResolveVerticalBand(dock, scaler, out float topY, out float bottomY))
            {
                bottomY = TilesFieldCenter.y - TilesFieldSize.y * 0.5f;
                topY = dock != null ? dock.GetSlotsBottomWorldY() : maxY + 1f;
                if (TilesFieldSize.y <= 0.0001f)
                    bottomY = minY - 1f;
            }

            BoxCoverLayout.FitBoardInBandLegacy(
                parentGameObject.transform,
                minY,
                maxY,
                topY,
                bottomY,
                scaler);
        }

        public Vector3 GetPosition(ElementPosition elementPosition, int arraySize)
        {
            float deltaX = 0f;
            float deltaY = 0f;

            if (layerDeltaOffsets.TryGetValue(elementPosition.LayerId, out Vector2 deltaOffset))
            {
                deltaX = deltaOffset.x;
                deltaY = deltaOffset.y;
            }

            if (cellDeltaOffsets.TryGetValue(new Vector3Int(elementPosition.X, elementPosition.Y, elementPosition.LayerId), out Vector2 cellDelta))
            {
                deltaX += cellDelta.x;
                deltaY += cellDelta.y;
            }

            return LevelFitter.GetPreviewLocalPosition(
                elementPosition,
                arraySize,
                TileSize,
                EvenLayerSize,
                OddLayerSize,
                new Vector2(deltaX, deltaY),
                ShowLayerDelta);
        }


        public void Clear()
        {
            if (parentGameObject != null)
            {
                for (int i = parentGameObject.transform.childCount - 1; i >= 0; i--)
                {
                    UnityEngine.Object.DestroyImmediate(parentGameObject.transform.GetChild(i).gameObject);
                }
            }

            matchables.Clear();
            layerGameobjects.Clear();
            enabledPrefabIndexes.Clear();
            layerDeltaOffsets.Clear();
            cellDeltaOffsets.Clear();
            boxCoverPreviews.Clear();
            totalLayerCount = 0;
        }

        public void SetLayerDeltaOffset(int layerIndex, float deltaX, float deltaY)
        {
            layerDeltaOffsets[layerIndex] = new Vector2(deltaX, deltaY);
        }

        public void SetCellDeltaOffset(int layer, int xIndex, int yIndex, float deltaX, float deltaY)
        {
            Vector3Int key = new Vector3Int(xIndex, yIndex, layer);
            if (Mathf.Approximately(deltaX, 0f) && Mathf.Approximately(deltaY, 0f))
            {
                cellDeltaOffsets.Remove(key);
            }
            else
            {
                cellDeltaOffsets[key] = new Vector2(deltaX, deltaY);
            }

            if (matchables.TryGetValue(key, out tempGameObject) && tempGameObject != null)
            {
                // Prefer total layer count from InitCells; layerGameobjects.Count is incomplete while layers are still being built.
                int arraySize = totalLayerCount > 0 ? totalLayerCount : layerGameobjects.Count;
                tempGameObject.transform.localPosition = GetPosition(new ElementPosition(xIndex, yIndex, layer), arraySize);
            }
        }

        public void ClearLayerDeltaOffset(int layerIndex)
        {
            if (layerDeltaOffsets.ContainsKey(layerIndex))
            {
                layerDeltaOffsets.Remove(layerIndex);
            }
        }

        public void SetState(GameObject layer, bool state)
        {
            if (!layer.activeSelf)
            {
                layer.SetActive(true);
            }

            // Avoid GetComponentsInChildren alloc on every layer switch: tint only direct tile roots' known renderers via TileActor.
            var tintColor = (Color.white * (state ? 1f : 0.7f)).SetAlpha(1f);
            Transform layerTransform = layer.transform;
            for (int i = 0; i < layerTransform.childCount; i++)
            {
                TileActor tileBehavior = layerTransform.GetChild(i).GetComponent<TileActor>();
                if (tileBehavior == null)
                {
                    continue;
                }

                if (tileBehavior.IconSpriteRenderer != null)
                {
                    tileBehavior.IconSpriteRenderer.color = tintColor;
                }

                if (tileBehavior.BackgroundSpriteRenderer != null)
                {
                    tileBehavior.BackgroundSpriteRenderer.color = tintColor;
                }
            }
        }

        public void UpdateCells(int layer, int xIndex, int yIndex, bool value)
        {
            Vector3Int key = new Vector3Int(xIndex, yIndex, layer);
            if (matchables.TryGetValue(key, out tempGameObject) && tempGameObject != null)
            {
                if (!value)
                {
                    matchables.Remove(key);
                    UnityEngine.Object.DestroyImmediate(tempGameObject);
                }
                else if (!tempGameObject.activeSelf)
                {
                    tempGameObject.SetActive(true);
                }

                return;
            }

            if (value)
            {
                int arraySize = totalLayerCount > 0 ? totalLayerCount : Mathf.Max(layer + 1, layerGameobjects.Count);
                InitCells(layer, xIndex, yIndex, true, arraySize);
            }
        }

        public void EnsureLayer(int layer)
        {
            while (layerGameobjects.Count <= layer)
            {
                GameObject newLayer = new GameObject($"Layer_{layerGameobjects.Count}");
                newLayer.transform.SetParent(parentGameObject.transform, false);
                newLayer.transform.localPosition = Vector3.zero;
                newLayer.transform.localRotation = Quaternion.identity;
                newLayer.transform.localScale = Vector3.one;
                layerGameobjects.Add(newLayer);
            }
        }

        public void RefreshBoxCovers(
            LevelModel level,
            int selectedLayerIndex,
            BoxCoverSize selectedSize)
        {
            ClearBoxCoverPreviews();
            selectedBoxCoverSize = selectedSize;

            if (level == null)
            {
                return;
            }

            totalLayerCount = level.AmountOfLayers;
            for (int layerIndex = 0; layerIndex < level.AmountOfLayers; layerIndex++)
            {
                Layer layer = level.GetLayer(layerIndex);
                if (layer == null)
                {
                    continue;
                }

                EnsureLayer(layerIndex);
                IReadOnlyList<BoxCoverPlacementData> configurations = layer.BoxCovers;
                for (int configIndex = 0; configIndex < configurations.Count; configIndex++)
                {
                    BoxCoverPlacementData configuration = configurations[configIndex];
                    if (configuration == null)
                    {
                        continue;
                    }

                    Sprite sprite = LoadBoxCoverSprite(level.BoxCoverType, configuration.Size);
                    if (sprite == null)
                    {
                        continue;
                    }

                    for (int anchorIndex = 0;
                         anchorIndex < configuration.CandidateAnchors.Count;
                         anchorIndex++)
                    {
                        Vector2Int anchor = configuration.CandidateAnchors[anchorIndex];
                        GameObject previewObject = new GameObject(
                            $"{BOX_COVER_PREVIEW_NAME} "
                            + $"{BoxCoverSizeUtility.GetDisplayName(configuration.Size)} "
                            + $"{anchor}");
                        previewObject.hideFlags = HideFlags.HideAndDontSave;
                        previewObject.transform.SetParent(layerGameobjects[layerIndex].transform, false);

                        SpriteRenderer spriteRenderer = previewObject.AddComponent<SpriteRenderer>();
                        spriteRenderer.sprite = sprite;

                        BoxCoverPreview preview = new BoxCoverPreview
                        {
                            gameObject = previewObject,
                            renderer = spriteRenderer,
                            layerIndex = layerIndex,
                            anchor = anchor,
                            size = configuration.Size,
                        };
                        boxCoverPreviews.Add(preview);
                        UpdateBoxCoverTransform(preview);
                    }
                }
            }

            UpdateBoxCoverSelection(selectedLayerIndex, selectedSize);
        }

        public void UpdateBoxCoverSelection(int selectedLayerIndex, BoxCoverSize selectedSize)
        {
            selectedBoxCoverSize = selectedSize;

            for (int i = 0; i < boxCoverPreviews.Count; i++)
            {
                BoxCoverPreview preview = boxCoverPreviews[i];
                if (preview == null || preview.renderer == null)
                {
                    continue;
                }

                bool isCurrentLayer = preview.layerIndex == selectedLayerIndex;
                bool isCurrentSize = preview.size == selectedSize;
                float alpha = isCurrentLayer
                    ? (isCurrentSize ? 0.95f : 0.68f)
                    : 0.38f;
                preview.renderer.color = new Color(1f, 1f, 1f, alpha);
            }
        }

        private void ClearBoxCoverPreviews()
        {
            for (int i = boxCoverPreviews.Count - 1; i >= 0; i--)
            {
                BoxCoverPreview preview = boxCoverPreviews[i];
                if (preview != null && preview.gameObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(preview.gameObject);
                }
            }

            boxCoverPreviews.Clear();
        }

        private void UpdateBoxCoverTransform(BoxCoverPreview preview)
        {
            if (preview == null || preview.gameObject == null || preview.renderer == null)
            {
                return;
            }

            Vector2 layerDelta = layerDeltaOffsets.TryGetValue(preview.layerIndex, out Vector2 delta)
                ? delta
                : Vector2.zero;
            Vector2Int cellSize = BoxCoverSizeUtility.GetCellSize(preview.size);
            Vector2Int oppositeCell = preview.anchor + cellSize - Vector2Int.one;

            Vector3 anchorPosition = LevelFitter.GetPreviewLocalPosition(
                new ElementPosition(preview.anchor.x, preview.anchor.y, preview.layerIndex),
                totalLayerCount,
                TileSize,
                EvenLayerSize,
                OddLayerSize,
                layerDelta,
                ShowLayerDelta);
            Vector3 oppositePosition = LevelFitter.GetPreviewLocalPosition(
                new ElementPosition(oppositeCell.x, oppositeCell.y, preview.layerIndex),
                totalLayerCount,
                TileSize,
                EvenLayerSize,
                OddLayerSize,
                layerDelta,
                ShowLayerDelta);

            preview.gameObject.transform.localPosition = (anchorPosition + oppositePosition) * 0.5f;
            preview.gameObject.transform.localRotation = Quaternion.identity;

            Vector2 spriteSize = preview.renderer.sprite != null
                ? preview.renderer.sprite.bounds.size
                : Vector2.one;
            float safeSpriteWidth = Mathf.Max(0.0001f, spriteSize.x);
            float safeSpriteHeight = Mathf.Max(0.0001f, spriteSize.y);
            preview.gameObject.transform.localScale = new Vector3(
                TileSize.x * cellSize.x / safeSpriteWidth,
                TileSize.y * cellSize.y / safeSpriteHeight,
                1f);

            int tileOrder = -preview.layerIndex * 100
                + preview.anchor.x
                - preview.anchor.y;
            preview.renderer.sortingOrder = tileOrder * 3 + 40;
        }

        private static Sprite LoadBoxCoverSprite(BoxCoverType type, BoxCoverSize size)
        {
            string assetPath = BoxCoverThemePaths.GetClosedSpritePath(type, size);
            if (boxCoverSprites.TryGetValue(assetPath, out Sprite cachedSprite) && cachedSprite != null)
            {
                return cachedSprite;
            }

            string sizeName = BoxCoverSizeUtility.GetDisplayName(size);
            Sprite sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                Object[] subAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
                for (int i = 0; i < subAssets.Length; i++)
                {
                    if (subAssets[i] is Sprite subAssetSprite)
                    {
                        sprite = subAssetSprite;
                        break;
                    }
                }
            }

            if (sprite == null)
            {
                Texture2D texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                if (texture != null)
                {
                    sprite = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        100f);
                    sprite.name = $"{BOX_COVER_PREVIEW_NAME} {sizeName}";
                    sprite.hideFlags = HideFlags.HideAndDontSave;
                }
            }

            if (sprite == null)
            {
                Debug.LogWarning($"[Level editor] Box cover preview sprite not found: {assetPath}");
                return null;
            }

            boxCoverSprites[assetPath] = sprite;
            return sprite;
        }

        public void InitCells(int layer, int xIndex, int yIndex, bool value, int arraySize)
        {
            totalLayerCount = arraySize;
            EnsureLayer(layer);

            // Empty cells are not instantiated — keeps Scene view and editor responsive.
            if (!value)
            {
                return;
            }

            Vector3Int key = new Vector3Int(xIndex, yIndex, layer);
            if (matchables.ContainsKey(key))
            {
                return;
            }

            if (enabledPrefabIndexes.Count == 0)
            {
                setsAmount -= prefabs.Length;

                if ((setsAmount > 0) && (setsAmount < prefabs.Length))//this whole thing makes it that our preview level is solvable
                {
                    for (int i = 0; i < setsAmount; i++)
                    {
                        enabledPrefabIndexes.Add(i);
                        enabledPrefabIndexes.Add(i);
                        enabledPrefabIndexes.Add(i);
                    }
                }
                else
                {
                    setsAmount -= prefabs.Length;

                    for (int i = 0; i < prefabs.Length; i++)
                    {
                        enabledPrefabIndexes.Add(i);
                        enabledPrefabIndexes.Add(i);
                        enabledPrefabIndexes.Add(i);
                    }
                }
            }

            int randomIndex = UnityEngine.Random.Range(0, enabledPrefabIndexes.Count);
            GameObject sourcePrefab = prefabs[enabledPrefabIndexes[randomIndex]];
            enabledPrefabIndexes.RemoveAt(randomIndex);
            if (sourcePrefab == null)
            {
                Debug.LogError("[Level editor] Null tile prefab in Level Database.tiles while building preview.");
                return;
            }

            GameObject newPrefab = UnityEngine.Object.Instantiate(sourcePrefab, layerGameobjects[layer].transform) as GameObject;
            newPrefab.transform.localPosition = GetPosition(new ElementPosition(xIndex, yIndex, layer), arraySize);
            newPrefab.transform.localRotation = Quaternion.identity;
            newPrefab.transform.localScale = Vector3.one;
            ApplyPreviewScale(newPrefab);
            ApplyPreviewVisuals(newPrefab, -layer * 100 + xIndex - yIndex);

            matchables.Add(key, newPrefab);
        }

        /// <summary>
        /// Reposition/rescale existing preview tiles without destroy/recreate (for showLayerScale/delta tweaks).
        /// </summary>
        public void RefreshLayout(int arraySize)
        {
            totalLayerCount = arraySize;
            List<Vector3Int> keys = new List<Vector3Int>(matchables.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                Vector3Int key = keys[i];
                if (!matchables.TryGetValue(key, out GameObject prefab) || prefab == null)
                {
                    continue;
                }

                prefab.transform.localPosition = GetPosition(new ElementPosition(key.x, key.y, key.z), arraySize);
                ApplyPreviewScale(prefab);
            }

            for (int i = 0; i < boxCoverPreviews.Count; i++)
            {
                UpdateBoxCoverTransform(boxCoverPreviews[i]);
            }
        }

        private void ApplyPreviewScale(GameObject prefab)
        {
            if (prefab == null)
            {
                return;
            }

            TileActor tileBehavior = prefab.GetComponent<TileActor>();
            if (tileBehavior != null)
            {
                tileBehavior.SetScale(TileSize);
                return;
            }

            // Fallback when TileActor is missing: scale root to match tile size.
            float smallerSize = TileSize.x > TileSize.y ? TileSize.y : TileSize.x;
            prefab.transform.localScale = Vector3.one * smallerSize;
        }

        private const string EditorPreviewBackgroundSpritePath = "Assets/CABRes/SpriteAtlas/TileEffect/tile_qizi.png";
        private static Sprite editorPreviewBackgroundSprite;

        /// <summary>
        /// Editor preview Instantiates tiles without TileActor.Init.
        /// After 9aff489c, Back sprites are no longer baked on the prefab / are Missing;
        /// assign a valid editor-loadable sprite so Scene view shows the white tile layout like tileexplorerlike.
        /// </summary>
        private void ApplyPreviewVisuals(GameObject prefab, int order)
        {
            if (prefab == null)
            {
                return;
            }

            TileActor tileBehavior = prefab.GetComponent<TileActor>();
            SpriteRenderer iconSpriteRenderer = tileBehavior != null ? tileBehavior.IconSpriteRenderer : null;
            SpriteRenderer backgroundSpriteRenderer = tileBehavior != null ? tileBehavior.BackgroundSpriteRenderer : null;

            if (iconSpriteRenderer == null || backgroundSpriteRenderer == null)
            {
                SpriteRenderer[] spriteRenderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
                for (int i = 0; i < spriteRenderers.Length; i++)
                {
                    string rendererName = spriteRenderers[i].gameObject.name;
                    if (iconSpriteRenderer == null && rendererName == "Icon")
                    {
                        iconSpriteRenderer = spriteRenderers[i];
                    }
                    else if (backgroundSpriteRenderer == null && rendererName == "Back")
                    {
                        backgroundSpriteRenderer = spriteRenderers[i];
                    }
                }
            }

            if (backgroundSpriteRenderer != null && backgroundSpriteRenderer.sprite == null)
            {
                // UILoadHelper/xasset is unavailable in edit mode; use AssetDatabase like a baked prefab sprite.
                if (editorPreviewBackgroundSprite == null)
                {
                    editorPreviewBackgroundSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(EditorPreviewBackgroundSpritePath);
                }

                if (editorPreviewBackgroundSprite != null)
                {
                    backgroundSpriteRenderer.sprite = editorPreviewBackgroundSprite;
                }
            }

            if (iconSpriteRenderer != null)
            {
                iconSpriteRenderer.sortingOrder = order * 3 - 1;
            }

            if (backgroundSpriteRenderer != null)
            {
                backgroundSpriteRenderer.sortingOrder = order * 3 - 2;
            }
        }

        public void SetSortingOrder(GameObject prefab, int order)
        {
            ApplyPreviewVisuals(prefab, order);
        }

        public void UpdateSelectedLayerIndex(int selectedLayerIndex)
        {
            for (int i = 0; i < layerGameobjects.Count; i++)
            {
                if (i == selectedLayerIndex)
                {
                    SetState(layerGameobjects[i], true);
                }
                else if (i > selectedLayerIndex)
                {
                    SetState(layerGameobjects[i], false);
                }
                else
                {
                    layerGameobjects[i].SetActive(false);
                }
            }

            UpdateBoxCoverSelection(selectedLayerIndex, selectedBoxCoverSize);
        }

        public void InitMaterials(GameObject prefab)
        {
            enabledMaterial = new Material(prefab.GetComponentInChildren<SpriteRenderer>().sharedMaterial);
            disabledMaterial = new Material(prefab.GetComponentInChildren<SpriteRenderer>().sharedMaterial);
            enabledMaterial.SetFloat(OVERLAY_STRENGTH_ID, 0);
            disabledMaterial.SetFloat(OVERLAY_STRENGTH_ID, 0.3f);
        }

        public void Destroy()
        {
            if (parentGameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(parentGameObject);
            }

            parentGameObject = null;

            if (enabledMaterial != null)
            {
                UnityEngine.Object.DestroyImmediate(enabledMaterial);
            }

            if (disabledMaterial != null)
            {
                UnityEngine.Object.DestroyImmediate(disabledMaterial);
            }

            enabledMaterial = null;
            disabledMaterial = null;
            boxCoverPreviews.Clear();
        }
    }
}
#endif
