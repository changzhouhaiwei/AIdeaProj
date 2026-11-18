using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TileGameCore;
using TMPro;
using UnityEngine;
using FishFramework;

namespace FishTilePlay
{
    /// <summary>
    /// 裂变棋视觉：大脑花色、独立底板、感染拖尾、IQ UP 文案。
    /// </summary>
    public sealed class IQChallengeVisual : MonoBehaviour
    {
        public const string TileBrainPath = "Assets/CABRes/Sprites/IQChallenge/IQ_TileBrain.png";
        public const string TileBasePath = "Assets/CABRes/Sprites/IQChallenge/IQ_TileBase.png";
        public const string FallbackBrainPath = "Assets/CABRes/Sprites/IQChallenge/IQ_IconBrain.png";

        const string BadgeName = "IQPlusBadge";
        static readonly Color PlateTint = new Color(0.52f, 0.38f, 0.92f, 1f);
        static readonly Color InfectGreen = new Color(0.25f, 0.95f, 0.42f, 1f);
        static readonly Color DefaultConvertTint = new Color(0.591f, 0.314f, 0.916f, 1f);
        static readonly Color BoltYellow = new Color(1f, 0.82f, 0.08f, 0.3f);
        static readonly Color BoltOrange = new Color(0.78f, 0.24f, 0.02f, 0.5f);

        const string ThdSetPrefabPath = "Assets/Arts/NewEff/Prefabs/Thd_Set.prefab";
        const string IqBrokenPrefabPath = "Assets/Arts/NewEff/Prefabs/IQchallenge_Broken.prefab";
        const string IqBrokenParticleName = "IQchallenge_Broken";
        const float FissionWindupDuration = 0.1f;
        const float FissionConvergeDuration = 0.11f;
        const float FissionWindupTiles = 0.34f;
        const float FissionPopDuration = 0.1f;
        const float FissionVanishDuration = 0.14f;
        const float FissionPopScale = 1.35f;
        const float SoftWidth = 2.376f;
        const float CoreWidth = 0.2145f;
        const float BoltHitLifetimeScale = 0.9f;
        const float BoltFadeDuration = 0.12f;
        const float ShootDuration = 0.2f;
        const float TintInDuration = 0.24f;
        const float CrossfadeDuration = 0.36f;
        const float TintOutDuration = 0.32f;
        const float HitMinDuration = 0.4f;

        static IQChallengeVisual host;
        static Sprite cachedBrain;
        static Sprite cachedBase;
        static bool spritesResolved;
        static Material lightningAddMaterial;
        static Material lightningCoreMaterial;
        static Material lightningAlphaMaterial;
        static GameObject thdSetPrefab;
        static int thdSeed = 1;
        static int iqBrokenSortingOrder = 20;
        static float lastConvertSoundTime = -10f;

        public static void ApplyLook(TileActor tile)
        {
            ApplySprites(tile, true);
        }

        static void ApplySprites(TileActor tile, bool resetColor)
        {
            if (tile == null)
                return;

            EnsureSprites();
            SpriteRenderer icon = tile.IconSpriteRenderer;
            if (icon != null && cachedBrain != null)
                icon.sprite = cachedBrain;

            SpriteRenderer background = tile.BackgroundSpriteRenderer;
            if (background != null)
            {
                if (cachedBase != null)
                {
                    background.sprite = cachedBase;
                    if (resetColor)
                        background.color = Color.white;
                }
                else if (resetColor)
                {
                    background.color = PlateTint;
                }
            }

            if (resetColor && icon != null)
                icon.color = Color.white;

            HideBadge(tile);
        }

        public static void RestoreLook(TileActor tile)
        {
            HideBadge(tile);
            tile?.RestoreDefaultVisuals();
        }

        public static void HideBadge(TileActor tile)
        {
            if (tile == null)
                return;

            Transform found = tile.transform.Find(BadgeName);
            if (found == null && tile.VisualsTransform != null)
                found = tile.VisualsTransform.Find(BadgeName);
            if (found == null)
                return;

            SpriteRenderer badge = found.GetComponent<SpriteRenderer>();
            if (badge != null)
                badge.enabled = false;
            found.gameObject.SetActive(false);
        }

        public static void SyncSorting(TileActor tile, int order)
        {
            if (tile == null)
                return;

            Transform found = tile.transform.Find(BadgeName);
            if (found == null && tile.VisualsTransform != null)
                found = tile.VisualsTransform.Find(BadgeName);
            if (found == null)
                return;

            SpriteRenderer badge = found.GetComponent<SpriteRenderer>();
            if (badge == null)
                return;

            badge.sortingOrder = order * 3 + 1;
            if (tile.IconSpriteRenderer != null)
                badge.sortingLayerID = tile.IconSpriteRenderer.sortingLayerID;
        }

        public static void CancelPresentation()
        {
            if (host != null)
            {
                host.StopAllCoroutines();
                foreach (Transform child in host.transform)
                {
                    if (child.name == "IQInfectBolt")
                        UnityEngine.Object.Destroy(child.gameObject);
                }
            }
        }

        public static void PlayIntroConvert(TileActor tile)
        {
            if (tile == null)
                return;

            if (LevelDifficultySim.SkipPresentation)
            {
                ApplyLook(tile);
                return;
            }

            PlayLightningShift(tile, tile.VisualEpoch, null, null, shock: false);
        }

        public static void PlayConvert(TileActor tile)
        {
            if (tile == null)
                return;

            if (LevelDifficultySim.SkipPresentation)
            {
                ApplyLook(tile);
                return;
            }

            PlayLightningShift(tile, tile.VisualEpoch, null, null, shock: true);
        }

        public static void PlayFissionMerge(TileActor left, TileActor middle, TileActor right, Action onShatter, Action onComplete)
        {
            if (middle == null || LevelDifficultySim.SkipPresentation)
            {
                HideMergedTile(left);
                HideMergedTile(right);
                HideMergedTile(middle);
                onShatter?.Invoke();
                onComplete?.Invoke();
                return;
            }

            Vector3 center = middle.transform.position;
            int brokenLayer = middle.IconSpriteRenderer != null
                ? middle.IconSpriteRenderer.sortingLayerID
                : 0;
            RaiseMergeTiles(left, middle, right);
            left?.transform.DOKill();
            right?.transform.DOKill();
            middle.transform.DOKill();

            Vector3 baseScale = middle.transform.localScale;
            Sequence seq = DOTween.Sequence().SetTarget(middle.transform);
            AppendPair(seq, left, WindupPosition(left, center), right, WindupPosition(right, center), FissionWindupDuration, Ease.OutQuad);
            AppendPair(seq, left, center, right, center, FissionConvergeDuration, Ease.InCubic);
            seq.AppendCallback(() =>
            {
                HideMergedTile(left);
                HideMergedTile(right);
            });
            seq.Append(middle.transform.DOScale(baseScale * FissionPopScale, FissionPopDuration).SetEase(Ease.OutQuad));
            seq.AppendCallback(() =>
            {
                PlayIqBroken(center, brokenLayer);
                onShatter?.Invoke();
            });
            seq.Append(middle.transform.DOScale(Vector3.zero, FissionVanishDuration).SetEase(Ease.InQuad));
            seq.OnComplete(() => onComplete?.Invoke());
        }

        static void AppendPair(Sequence seq, TileActor a, Vector3 aDestination, TileActor b, Vector3 bDestination, float duration, Ease ease)
        {
            if (seq == null)
                return;

            DG.Tweening.Tween first = a != null ? a.transform.DOMove(aDestination, duration).SetEase(ease) : null;
            DG.Tweening.Tween second = b != null ? b.transform.DOMove(bDestination, duration).SetEase(ease) : null;
            if (first != null)
                seq.Append(first);
            if (second == null)
                return;
            if (first != null)
                seq.Join(second);
            else
                seq.Append(second);
        }

        static Vector3 WindupPosition(TileActor tile, Vector3 center)
        {
            if (tile == null)
                return center;

            Vector3 from = tile.transform.position;
            Vector3 away = from - center;
            if (away.sqrMagnitude < 0.0001f)
                away = Vector3.right;

            float tileSize = Mathf.Max(LevelFitter.TileSize.x, LevelFitter.TileSize.y);
            if (tileSize < 0.05f)
                tileSize = 1.1f;
            return from + away.normalized * (tileSize * FissionWindupTiles);
        }

        static void HideMergedTile(TileActor tile)
        {
            if (tile == null)
                return;

            tile.transform.DOKill();
            tile.transform.localScale = Vector3.zero;
        }

        static void RaiseMergeTiles(TileActor left, TileActor middle, TileActor right)
        {
            int ceiling = MaxOtherDockSortingOrder(left, middle, right);
            PlaceTileLayer(left, ceiling + 2);
            PlaceTileLayer(right, ceiling + 2);
            PlaceTileLayer(middle, ceiling + 6);
            iqBrokenSortingOrder = ceiling + 12;
        }

        static void PlaceTileLayer(TileActor tile, int backgroundOrder)
        {
            if (tile == null)
                return;

            SpriteRenderer background = tile.BackgroundSpriteRenderer;
            SpriteRenderer icon = tile.IconSpriteRenderer;
            if (background != null)
                background.sortingOrder = backgroundOrder;
            if (icon != null)
                icon.sortingOrder = backgroundOrder + 1;

            Renderer[] renderers = tile.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || renderer == background || renderer == icon)
                    continue;
                renderer.sortingOrder = backgroundOrder + 2;
            }
        }

        static int MaxOtherDockSortingOrder(TileActor left, TileActor middle, TileActor right)
        {
            int order = 0;
            DockActor dock = LevelDirector.Dock;
            if (dock == null)
                return order;

            TileActor[] tiles = dock.GetComponentsInChildren<TileActor>(true);
            for (int i = 0; i < tiles.Length; i++)
            {
                TileActor tile = tiles[i];
                if (tile == null || tile == left || tile == middle || tile == right)
                    continue;

                Renderer[] renderers = tile.GetComponentsInChildren<Renderer>(true);
                for (int r = 0; r < renderers.Length; r++)
                {
                    if (renderers[r] != null)
                        order = Mathf.Max(order, renderers[r].sortingOrder);
                }
            }

            return order;
        }

        static void PlayIqBroken(Vector3 position, int sortingLayerId)
        {
            if (!ParticlesDirector.HasParticle(IqBrokenParticleName))
            {
                GameObject prefab = UILoadHelper.LoadAsset<GameObject>(IqBrokenPrefabPath);
#if UNITY_EDITOR
                if (prefab == null)
                    prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(IqBrokenPrefabPath);
#endif
                if (prefab == null)
                {
                    Debug.LogError($"[IQ] broken prefab is missing: {IqBrokenPrefabPath}");
                    return;
                }

                ParticlesDirector.RegisterParticle(IqBrokenParticleName, prefab);
            }

            var particleCase = ParticlesDirector.PlayParticle(IqBrokenParticleName);
            if (particleCase == null || particleCase.ParticleSystem == null)
                return;

            particleCase.SetDuration(2)
                .SetPosition(position + Vector3.back * 0.2f)
                .SetRotation(Quaternion.Euler(Vector3.right * -90f));

            ParticleSystemRenderer[] renderers = particleCase.ParticleSystem.GetComponentsInChildren<ParticleSystemRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                    continue;
                renderers[i].sortingLayerID = sortingLayerId;
                renderers[i].sortingOrder = iqBrokenSortingOrder;
            }
        }

        public static void PlayInfect(Vector3 from, List<TileActor> targets, Action onComplete)
        {
            if (targets == null || targets.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            if (LevelDifficultySim.SkipPresentation)
            {
                for (int i = 0; i < targets.Count; i++)
                    ApplyLook(targets[i]);
                onComplete?.Invoke();
                return;
            }

            var epochs = new int[targets.Count];
            for (int i = 0; i < targets.Count; i++)
                epochs[i] = targets[i] != null ? targets[i].VisualEpoch : -1;
            Host().StartCoroutine(InfectRoutine(from, targets, epochs, onComplete));
        }

        public static void ShowIqUp(Vector3 worldPos, bool genius)
        {
            if (LevelDifficultySim.SkipPresentation)
                return;

            var go = new GameObject(genius ? "IQGeniusLabel" : "IQUpLabel");
            TextMeshPro tmp = go.AddComponent<TextMeshPro>();
            tmp.text = genius ? "GENIUS!" : "IQ UP!";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = genius ? 6.5f : 5.5f;
            tmp.color = genius ? new Color(1f, 0.85f, 0.2f, 1f) : InfectGreen;
            tmp.fontStyle = FontStyles.Bold;
            tmp.sortingOrder = 6000;
            go.transform.position = worldPos + Vector3.up * 0.55f;
            tmp.rectTransform.sizeDelta = new Vector2(6f, 1.6f);

            Sequence seq = DOTween.Sequence();
            seq.Append(go.transform.DOMoveY(go.transform.position.y + 0.7f, 0.55f).SetEase(Ease.OutCubic));
            seq.Join(tmp.DOFade(0f, 0.35f).SetDelay(0.35f));
            seq.OnComplete(() =>
            {
                if (go != null)
                    UnityEngine.Object.Destroy(go);
            });
        }

        static IEnumerator InfectRoutine(Vector3 from, List<TileActor> targets, int[] epochs, Action onComplete)
        {
            var shots = new List<BoltShot>(targets.Count);
            var hitTiles = new List<TileActor>(targets.Count);
            var hitEpochs = new List<int>(targets.Count);
            for (int i = 0; i < targets.Count; i++)
            {
                TileActor target = targets[i];
                if (target == null || target.VisualEpoch != epochs[i])
                    continue;

                BoltShot shot = CreateBolt(from, target.transform.position);
                SetBoltHead(shot, 0f);
                shots.Add(shot);
                hitTiles.Add(target);
                hitEpochs.Add(epochs[i]);
            }

            if (shots.Count == 0)
            {
                onComplete?.Invoke();
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < ShootDuration)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / ShootDuration);
                float head = 1f - (1f - u) * (1f - u);
                for (int i = 0; i < shots.Count; i++)
                    SetBoltHead(shots[i], head);
                yield return null;
            }

            for (int i = 0; i < shots.Count; i++)
                SetBoltHead(shots[i], 1f);

            int pending = hitTiles.Count;
            float revealDelay = float.PositiveInfinity;
            for (int i = 0; i < hitTiles.Count; i++)
            {
                TileActor target = hitTiles[i];
                int epoch = hitEpochs[i];
                PlayLightningShift(target, epoch, () =>
                {
                    if (target != null && target.VisualEpoch == epoch)
                        PlayThdSet(target);
                }, () =>
                {
                    pending--;
                    if (pending <= 0)
                        onComplete?.Invoke();
                }, shock: true, onRevealScheduled: delay => revealDelay = Mathf.Min(revealDelay, delay));
            }

            // Keep the tile conversion timing unchanged. Shorten only the bolt lifetime,
            // including its fade, relative to the earliest original reveal callback.
            float lifetime = float.IsPositiveInfinity(revealDelay) ? 0f : revealDelay * BoltHitLifetimeScale;
            float fadeDuration = Mathf.Min(BoltFadeDuration, lifetime);
            float fadeStart = lifetime - fadeDuration;
            elapsed = 0f;
            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                float alpha = fadeDuration > 0f
                    ? 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - fadeStart) / fadeDuration))
                    : 0f;
                foreach (BoltShot shot in shots)
                {
                    for (int layer = 0; layer < shot.Layers.Length; layer++)
                    {
                        if (shot.Layers[layer] == null)
                            continue;
                        Color tint = shot.Tints[layer];
                        tint.a *= alpha;
                        // Preserve the spatial fade at the origin while fading all layers together.
                        ApplyStartFade(shot.Layers[layer], tint, 1f, shot.Length);
                    }
                }
                yield return null;
            }
            ClearBolts(shots);
        }

        static void PlayLightningShift(TileActor tile, int epoch, Action onReveal, Action onDone, bool shock, Action<float> onRevealScheduled = null)
        {
            if (tile == null)
            {
                onRevealScheduled?.Invoke(0f);
                onReveal?.Invoke();
                onDone?.Invoke();
                return;
            }

            PlayConvertSound();
            Color tint = ResolveConvertTint();
            SpriteRenderer icon = tile.IconSpriteRenderer;
            SpriteRenderer background = tile.BackgroundSpriteRenderer;
            SpriteRenderer iconGhost = null;
            SpriteRenderer backgroundGhost = null;
            bool revealed = false;
            Transform motion = tile.VisualsTransform;
            Vector3 baseScale = motion != null ? motion.localScale : Vector3.one;
            Vector3 baseEuler = motion != null ? motion.localEulerAngles : Vector3.zero;
            Vector3 basePos = motion != null ? motion.localPosition : Vector3.zero;

            void DestroyGhosts()
            {
                if (iconGhost != null)
                    UnityEngine.Object.Destroy(iconGhost.gameObject);
                if (backgroundGhost != null)
                    UnityEngine.Object.Destroy(backgroundGhost.gameObject);
                iconGhost = null;
                backgroundGhost = null;
            }

            void RestoreMotion()
            {
                if (motion == null)
                    return;

                motion.localScale = baseScale;
                motion.localEulerAngles = baseEuler;
                motion.localPosition = basePos;
            }

            Sequence seq = DOTween.Sequence();
            float tintAt = 0f;
            if (shock && motion != null)
            {
                float lead = UnityEngine.Random.Range(0f, 0.04f);
                seq.AppendInterval(lead);
                AppendElectricJitter(seq, motion, baseScale, baseEuler, basePos);
                tintAt = lead + UnityEngine.Random.Range(0.03f, 0.08f);
            }
            if (icon != null)
                seq.Insert(tintAt, icon.DOColor(tint, TintInDuration).SetEase(Ease.InOutSine));
            if (background != null)
                seq.Insert(tintAt, background.DOColor(tint, TintInDuration).SetEase(Ease.InOutSine));
            onRevealScheduled?.Invoke(seq.Duration());
            seq.AppendCallback(() =>
            {
                if (tile == null || tile.VisualEpoch != epoch)
                    return;

                iconGhost = SpawnFadeGhost(icon);
                backgroundGhost = SpawnFadeGhost(background);
                ApplySprites(tile, false);
                SetLightningAlpha(icon, tint, 0f);
                SetLightningAlpha(background, tint, 0f);
                revealed = true;
                onReveal?.Invoke();
            });
            seq.Append(DOVirtual.Float(0f, 1f, CrossfadeDuration, t =>
            {
                if (tile == null || tile.VisualEpoch != epoch)
                    return;

                float fade = Mathf.SmoothStep(0f, 1f, t);
                SetAlpha(iconGhost, 1f - fade);
                SetAlpha(backgroundGhost, 1f - fade);
                SetLightningAlpha(icon, tint, fade);
                SetLightningAlpha(background, tint, fade);
            }).SetEase(Ease.Linear));
            if (shock && motion != null)
                seq.Join(motion.DOScale(baseScale, CrossfadeDuration).SetEase(Ease.InOutSine));
            seq.AppendCallback(DestroyGhosts);
            if (icon != null)
                seq.Append(icon.DOColor(Color.white, TintOutDuration).SetEase(Ease.InOutSine));
            if (background != null)
                seq.Join(background.DOColor(Color.white, TintOutDuration).SetEase(Ease.InOutSine));
            bool finished = false;
            seq.OnKill(() =>
            {
                DestroyGhosts();
                if (!finished)
                    RestoreMotion();
            });
            seq.OnComplete(() =>
            {
                finished = true;
                if (!revealed)
                    onReveal?.Invoke();
                onDone?.Invoke();
            });
        }

        static void AppendElectricJitter(Sequence seq, Transform motion, Vector3 baseScale, Vector3 baseEuler, Vector3 basePos)
        {
            float elapsed = 0f;
            float zap = UnityEngine.Random.Range(0.04f, 0.055f);
            seq.Append(motion.DOLocalMove(basePos + RandomHitOffset(0.05f, 0.08f), zap).SetEase(Ease.OutQuad));
            seq.Join(motion.DOLocalRotate(baseEuler + new Vector3(0f, 0f, RandomHitAngle(9f, 16f)), zap).SetEase(Ease.OutQuad));
            seq.Join(motion.DOScale(baseScale * UnityEngine.Random.Range(1.08f, 1.14f), zap).SetEase(Ease.OutQuad));
            elapsed += zap;

            while (elapsed < HitMinDuration)
            {
                float step = UnityEngine.Random.Range(0.028f, 0.052f);
                Vector3 pos = basePos + RandomHitOffset(0.02f, 0.055f);
                float angle = RandomHitAngle(4f, 12f);
                float scale = UnityEngine.Random.Range(0.95f, 1.08f);
                seq.Append(motion.DOLocalMove(pos, step).SetEase(Ease.Linear));
                seq.Join(motion.DOLocalRotate(baseEuler + new Vector3(0f, 0f, angle), step).SetEase(Ease.Linear));
                seq.Join(motion.DOScale(baseScale * scale, step).SetEase(Ease.Linear));
                elapsed += step;
            }

            float settle = UnityEngine.Random.Range(0.04f, 0.07f);
            seq.Append(motion.DOLocalMove(basePos, settle).SetEase(Ease.OutQuad));
            seq.Join(motion.DOLocalRotate(baseEuler, settle).SetEase(Ease.OutQuad));
            seq.Join(motion.DOScale(baseScale * UnityEngine.Random.Range(1.02f, 1.05f), settle).SetEase(Ease.OutQuad));
        }

        static Vector3 RandomHitOffset(float min, float max)
        {
            float reach = UnityEngine.Random.Range(min, max);
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            return new Vector3(Mathf.Cos(angle) * reach, Mathf.Sin(angle) * reach, 0f);
        }

        static float RandomHitAngle(float min, float max)
        {
            float angle = UnityEngine.Random.Range(min, max);
            return UnityEngine.Random.value < 0.5f ? -angle : angle;
        }

        static SpriteRenderer SpawnFadeGhost(SpriteRenderer source)
        {
            if (source == null || !source.enabled || source.sprite == null)
                return null;

            var go = new GameObject(source.gameObject.name + "_IqFade");
            go.layer = source.gameObject.layer;
            Transform parent = source.transform.parent;
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
                Vector3 local = source.transform.localPosition;
                local.z -= 0.02f;
                go.transform.localPosition = local;
                go.transform.localRotation = source.transform.localRotation;
                go.transform.localScale = source.transform.localScale;
            }
            else
            {
                go.transform.SetParent(source.transform, false);
                go.transform.localPosition = new Vector3(0f, 0f, -0.02f);
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
            }

            SpriteRenderer ghost = go.AddComponent<SpriteRenderer>();
            ghost.sprite = source.sprite;
            ghost.color = source.color;
            ghost.sharedMaterial = source.sharedMaterial;
            ghost.sortingLayerID = source.sortingLayerID;
            ghost.sortingOrder = source.sortingOrder;
            ghost.flipX = source.flipX;
            ghost.flipY = source.flipY;
            ghost.drawMode = source.drawMode;
            if (source.drawMode != SpriteDrawMode.Simple)
                ghost.size = source.size;
            ghost.maskInteraction = source.maskInteraction;
            ghost.spriteSortPoint = source.spriteSortPoint;
            return ghost;
        }

        static void PlayConvertSound()
        {
            float now = Time.unscaledTime;
            if (now - lastConvertSoundTime < 0.15f)
                return;

            AudioClip clip = AudioController.AudioClips != null
                ? AudioController.AudioClips.iqConvertElectric
                : null;
            if (clip == null)
                return;

            lastConvertSoundTime = now;
            AudioController.PlaySound(clip);
        }

        static Color ResolveConvertTint()
        {
            UIGame game = UIController.GetPage<UIGame>();
            Color tint = game != null ? game.GetIqConvertTint(DefaultConvertTint) : DefaultConvertTint;
            tint.a = 1f;
            return tint;
        }

        static void SetLightningAlpha(SpriteRenderer renderer, Color tint, float alpha)
        {
            if (renderer == null)
                return;

            tint.a = alpha;
            renderer.color = tint;
        }

        static void SetAlpha(SpriteRenderer renderer, float alpha)
        {
            if (renderer == null)
                return;

            Color color = renderer.color;
            color.a = alpha;
            renderer.color = color;
        }

        static void PlayThdSet(TileActor tile)
        {
            GameObject prefab = LoadThdSetPrefab();
            if (prefab == null || tile == null)
                return;

            Transform parent = tile.VisualsTransform != null ? tile.VisualsTransform : tile.transform;
            GameObject go = UnityEngine.Object.Instantiate(prefab, parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            int order = 5;
            if (tile.IconSpriteRenderer != null)
                order = tile.IconSpriteRenderer.sortingOrder + 5;

            float life = 1.5f;
            uint seed = NextThdSeed();
            ParticleSystem[] systems = go.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem system = systems[i];
                ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
                if (renderer != null)
                    renderer.sortingOrder = order;

                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                system.useAutoRandomSeed = false;
                system.randomSeed = seed;
                ParticleSystem.MainModule main = system.main;
                life = Mathf.Max(life, main.duration + main.startLifetime.constantMax);
                system.Play(true);
            }

            UnityEngine.Object.Destroy(go, life + 0.15f);
        }

        static GameObject LoadThdSetPrefab()
        {
            if (thdSetPrefab != null)
                return thdSetPrefab;

            thdSetPrefab = UILoadHelper.LoadAsset<GameObject>(ThdSetPrefabPath);
#if UNITY_EDITOR
            if (thdSetPrefab == null)
                thdSetPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(ThdSetPrefabPath);
#endif
            if (thdSetPrefab == null)
                Debug.LogError($"[IQ] Thd_Set prefab is missing: {ThdSetPrefabPath}");
            return thdSetPrefab;
        }

        static uint NextThdSeed()
        {
            thdSeed += UnityEngine.Random.Range(1, 9973);
            if (thdSeed <= 0)
                thdSeed = 1;
            return (uint)thdSeed;
        }

        sealed class BoltShot
        {
            public GameObject Root;
            public LineRenderer[] Layers;
            public Vector3[] Points;
            public Color[] Tints;
            public float[] Widths;
            public float Length;
        }

        static BoltShot CreateBolt(Vector3 from, Vector3 to)
        {
            var shot = new BoltShot();
            shot.Points = BuildBoltPoints(from, to, out shot.Length);
            shot.Tints = new[] { BoltOrange, BoltYellow, Color.white };
            shot.Widths = new[] { SoftWidth, SoftWidth, CoreWidth };

            var root = new GameObject("IQInfectBolt");
            root.transform.SetParent(Host().transform, false);
            shot.Root = root;
            shot.Layers = new LineRenderer[3];
            shot.Layers[0] = CreateBoltLayer(root.transform, "BoltBottom", LightningAlphaMaterial(), 5500, true);
            shot.Layers[1] = CreateBoltLayer(root.transform, "BoltMiddle", LightningAddMaterial(), 5501, true);
            shot.Layers[2] = CreateBoltLayer(root.transform, "BoltTop", LightningCoreMaterial(), 5502, false);
            return shot;
        }

        static LineRenderer CreateBoltLayer(Transform parent, string name, Material material, int sorting, bool soft)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.material = material;
            line.useWorldSpace = true;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.numCapVertices = soft ? 4 : 2;
            line.numCornerVertices = soft ? 3 : 1;
            line.sortingOrder = sorting;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        static void SetBoltHead(BoltShot shot, float head)
        {
            if (shot == null || shot.Points == null || shot.Points.Length < 2)
                return;

            head = Mathf.Clamp01(head);
            int segments = shot.Points.Length - 1;
            float scaled = head * segments;
            int last = Mathf.Clamp(Mathf.CeilToInt(scaled), 1, segments);
            float frac = last >= segments ? 1f : Mathf.Clamp01(scaled - (last - 1));
            int count = last + 1;
            Vector3 tip = Vector3.Lerp(shot.Points[last - 1], shot.Points[last], frac);
            float tipScale = TipScale(head);

            for (int layer = 0; layer < shot.Layers.Length; layer++)
            {
                LineRenderer line = shot.Layers[layer];
                if (line == null)
                    continue;

                line.positionCount = count;
                for (int i = 0; i < last; i++)
                    line.SetPosition(i, shot.Points[i]);
                line.SetPosition(count - 1, tip);
                float width = shot.Widths[layer] * tipScale;
                line.startWidth = width;
                line.endWidth = width;
                ApplyStartFade(line, shot.Tints[layer], head, shot.Length);
            }
        }

        static float TipScale(float head)
        {
            float u = Mathf.Clamp01((head - 0.42f) / 0.58f);
            u *= u;
            return Mathf.Lerp(0.88f, 0.015f, u);
        }

        static void ApplyStartFade(LineRenderer line, Color tint, float head, float length)
        {
            float visible = Mathf.Max(0.01f, head * length);
            float fade01 = Mathf.Clamp(0.9f * BoltTile() / visible, 0.08f, 0.98f);
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(tint, 0f), new GradientColorKey(tint, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(tint.a, fade01),
                    new GradientAlphaKey(tint.a, 1f)
                });
            line.colorGradient = gradient;
        }

        static Vector3[] BuildBoltPoints(Vector3 from, Vector3 to, out float length)
        {
            Vector3 delta = to - from;
            length = delta.magnitude;
            int count = BoltPointCount(length);
            var points = new Vector3[count];
            points[0] = from;
            points[count - 1] = to;
            if (count <= 2 || length < 0.01f)
                return points;

            float reach = BoltReach(length);
            Vector3 dir = delta / length;
            Vector3 side = Vector3.Cross(dir, Vector3.forward);
            if (side.sqrMagnitude < 0.01f)
                side = Vector3.right;
            side.Normalize();

            float freq = UnityEngine.Random.Range(Mathf.Lerp(1f, 3.2f, reach), Mathf.Lerp(1.7f, 8f, reach));
            float turnChance = 0.55f * Mathf.Lerp(0.22f, 1f, reach);
            float ampFloor = Mathf.Lerp(Mathf.Min(0.26f, length * 0.16f), 0.26f, reach);
            float phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float jag = UnityEngine.Random.Range(0.35f, 1f) * Mathf.Lerp(0.35f, 1f, reach);
            float sign = UnityEngine.Random.value < 0.5f ? -1f : 1f;

            for (int i = 1; i < count - 1; i++)
            {
                float t = i / (float)(count - 1);
                float envelope = Mathf.Sin(t * Mathf.PI);
                float wave = Mathf.Sin(phase + t * freq * Mathf.PI * 2f);
                if (UnityEngine.Random.value < turnChance)
                    sign = -sign;

                Vector3 pos = Vector3.Lerp(from, to, t);
                pos += side * (wave * ampFloor * envelope * jag * sign);
                points[i] = pos;
            }

            return points;
        }

        static int BoltPointCount(float length)
        {
            float reach = BoltReach(length);
            if (reach <= 0f)
                return UnityEngine.Random.Range(3, 7);
            if (reach >= 1f)
                return UnityEngine.Random.Range(16, 27);

            int min = Mathf.RoundToInt(Mathf.Lerp(5f, 12f, reach));
            int max = Mathf.RoundToInt(Mathf.Lerp(10f, 18f, reach));
            if (max <= min)
                max = min + 1;
            return UnityEngine.Random.Range(min, max + 1);
        }

        static float BoltReach(float length)
        {
            float tile = BoltTile();
            return Mathf.InverseLerp(1.4f * tile, 3.8f * tile, length);
        }

        static float BoltTile()
        {
            Vector2 size = LevelFitter.TileSize;
            float tile = Mathf.Max(size.x, size.y);
            return tile < 0.05f ? 1.1f : tile;
        }

        static void ClearBolts(List<BoltShot> shots)
        {
            for (int i = 0; i < shots.Count; i++)
            {
                if (shots[i] != null && shots[i].Root != null)
                    UnityEngine.Object.Destroy(shots[i].Root);
            }
        }

        static Material LightningAddMaterial()
        {
            if (lightningAddMaterial != null)
                return lightningAddMaterial;

            Shader shader = Shader.Find("Particles/Additive");
            if (shader == null)
                shader = Shader.Find("Mobile/Particles/Additive");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            lightningAddMaterial = new Material(shader);
            lightningAddMaterial.mainTexture = BoltTexture(additive: true, core: false);
            return lightningAddMaterial;
        }

        static Material LightningCoreMaterial()
        {
            if (lightningCoreMaterial != null)
                return lightningCoreMaterial;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            lightningCoreMaterial = new Material(shader);
            lightningCoreMaterial.mainTexture = BoltTexture(additive: false, core: true);
            return lightningCoreMaterial;
        }

        static Material LightningAlphaMaterial()
        {
            if (lightningAlphaMaterial != null)
                return lightningAlphaMaterial;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            lightningAlphaMaterial = new Material(shader);
            lightningAlphaMaterial.mainTexture = BoltTexture(additive: false, core: false);
            return lightningAlphaMaterial;
        }

        static Texture2D BoltTexture(bool additive, bool core)
        {
            var tex = new Texture2D(4, 128, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color[4 * 128];
            for (int y = 0; y < 128; y++)
            {
                float remain = 1f - Mathf.Abs((y / 127f) * 2f - 1f);
                float glow = core
                    ? Mathf.Pow(Mathf.Clamp01(remain / 0.31f), 2f)
                    : remain * remain * remain;
                Color color = additive
                    ? new Color(glow, glow, glow, glow)
                    : new Color(1f, 1f, 1f, glow);
                for (int x = 0; x < 4; x++)
                    pixels[y * 4 + x] = color;
            }

            tex.SetPixels(pixels);
            tex.Apply(false, true);
            return tex;
        }

        static void EnsureSprites()
        {
            if (spritesResolved)
                return;

            cachedBrain = LoadSprite(TileBrainPath) ?? LoadSprite(FallbackBrainPath);
            cachedBase = LoadSprite(TileBasePath);
            spritesResolved = true;
        }

        static Sprite LoadSprite(string path)
        {
            Sprite sprite = UILoadHelper.LoadAsset<Sprite>(path);
#if UNITY_EDITOR
            if (sprite == null)
                sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
#endif
            return sprite;
        }

        static IQChallengeVisual Host()
        {
            if (host != null)
                return host;

            var go = new GameObject("IQChallengeVisualHost");
            if (LevelDirector.Dock != null)
                go.transform.SetParent(LevelDirector.Dock.transform, false);
            host = go.AddComponent<IQChallengeVisual>();
            return host;
        }

        void OnDestroy()
        {
            if (host == this)
                host = null;
        }
    }
}
