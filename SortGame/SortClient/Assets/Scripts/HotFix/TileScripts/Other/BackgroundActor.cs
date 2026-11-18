using System;
using DG.Tweening;
using TileGameCore;
using UnityEngine;
using FishFramework;

namespace FishTilePlay
{
    [ExecuteAlways]
    public class BackgroundActor : GPViewBase
    {
        private const string RevealMaterialPath = "Assets/Arts/NewEff/Material/SceneBgCircleReveal.mat";
        private const string BgAssetGroup = "SceneBg";
        private const string CoinLevelBgAssetName = "bg_coinLevel";

        private static readonly int OldTexId = Shader.PropertyToID("_OldTex");
        private static readonly int NewTexId = Shader.PropertyToID("_NewTex");
        private static readonly int RevealProgressId = Shader.PropertyToID("_RevealProgress");
        private static readonly int EdgeSoftnessId = Shader.PropertyToID("_EdgeSoftness");

        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Material revealMaterialTemplate;

        [Header("Sprites")]
        [SerializeField] Sprite oldBgSprite;
        [SerializeField] Sprite newBgSprite;
        [SerializeField, Range(0f, 1f)] float revealProgress;

        [Header("Reveal Timing")]
        [SerializeField, Range(0f, 2f)] float edgeSoftness = 0.04f;
        [SerializeField, Min(0f)] float spreadDuration = 2.25f;
        [SerializeField, Min(0f)] float softnessFadeDuration = 0.75f;
        [SerializeField, Min(0f)] float levelTilesLoadDelay = 2.25f;

        [Header("Audio")]
        [SerializeField, Min(0f)] float bgChangeSoundDelay = 0.3f;

        private Material revealMaterialInstance;
        private DG.Tweening.Tween transitionTween;
        private float lastCameraHeight = -1f;
        private float lastCameraAspect = -1f;
        private int lastSpriteId = -1;

        public bool IsTransitioning { get; private set; }
        public Sprite OldBgSprite => oldBgSprite;
        public Sprite NewBgSprite => newBgSprite;
        public float TotalTransitionDuration => SpreadDuration + SoftnessFadeDuration;
        public float LevelTilesLoadDelay => levelTilesLoadDelay;
        public float BgChangeSoundDelay => bgChangeSoundDelay;

        private float SpreadDuration => Mathf.Max(0f, spreadDuration);
        private float SoftnessFadeDuration => Mathf.Max(0f, softnessFadeDuration);
        private float SpreadPortion
        {
            get
            {
                float total = TotalTransitionDuration;
                return total <= 0f ? 1f : SpreadDuration / total;
            }
        }

        #region Public API

        public bool NeedsTransitionTo(int levelIndex)
        {
            if (!GameData.BFromStartScene || spriteRenderer == null)
            {
                return false;
            }

            Sprite next = LoadBgSpriteForLevel(levelIndex);
            Sprite current = spriteRenderer.sprite;
            return next != null && current != next;
        }

        public void RefreshBg(int? levelIndexOverride = null)
        {
            if (IsTransitioning)
            {
                return;
            }

            if (GameData.BFromStartScene)
            {
                int levelIndex = levelIndexOverride
                    ?? GameDirector.levelController.GetLoadedLevelIndex();
                var sprite = LoadBgSpriteForLevel(levelIndex);
                if (sprite != null)
                {
                    SetOldBgSprite(sprite);
                    SetNewBgSprite(sprite);
                }
            }

            ResetRevealProgress();
            ApplyTransitionVisual();
            ApplyBackgroundFit();
        }

        public void PlayBgTransition(int levelIndex, Action onComplete = null)
        {
            if (!GameData.BFromStartScene || spriteRenderer == null)
            {
                onComplete?.Invoke();
                return;
            }

            StopTransitionTween();

            if (!TryLoadTransitionSprites(levelIndex, out Sprite oldSprite, out Sprite newSprite))
            {
                onComplete?.Invoke();
                return;
            }

            if (!EnsureRevealMaterialInstance())
            {
                RefreshBg(levelIndex);
                onComplete?.Invoke();
                return;
            }

            BeginTransitionVisual(oldSprite, newSprite);

            if (TotalTransitionDuration <= 0f)
            {
                FinishTransition(newSprite, onComplete);
                return;
            }

            PlayRevealTransition(newSprite, onComplete);
        }

        public void SetOldBgSprite(Sprite sprite)
        {
            oldBgSprite = sprite;
            if (spriteRenderer != null && sprite != null)
            {
                spriteRenderer.sprite = sprite;
            }
        }

        public void SetNewBgSprite(Sprite sprite)
        {
            newBgSprite = sprite;
        }

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            EnsureRevealMaterialInstance();
            ApplyTransitionVisual();
            ApplyBackgroundFit();
        }

        private void OnValidate()
        {
            spreadDuration = Mathf.Max(0f, spreadDuration);
            softnessFadeDuration = Mathf.Max(0f, softnessFadeDuration);
            bgChangeSoundDelay = Mathf.Max(0f, bgChangeSoundDelay);
            levelTilesLoadDelay = Mathf.Max(0f, levelTilesLoadDelay);
            EnsureRevealMaterialInstance();
            ApplyTransitionVisual();
        }

        private void LateUpdate()
        {
            if (!ShouldRefitBackground())
            {
                return;
            }

            ApplyBackgroundFit();
        }

        protected override void OnDestroy()
        {
            StopTransitionTween();

            if (revealMaterialInstance != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(revealMaterialInstance);
                else
                    Destroy(revealMaterialInstance);
#else
                Destroy(revealMaterialInstance);
#endif
                revealMaterialInstance = null;
            }

            base.OnDestroy();
        }

        #endregion

        #region Transition

        private bool TryLoadTransitionSprites(int levelIndex, out Sprite oldSprite, out Sprite newSprite)
        {
            oldSprite = spriteRenderer != null ? spriteRenderer.sprite : oldBgSprite;
            newSprite = LoadBgSpriteForLevel(levelIndex);
            if (newSprite == null)
            {
                return false;
            }

            if (oldSprite == null)
            {
                oldSprite = oldBgSprite;
            }

            return oldSprite != newSprite;
        }

        private void BeginTransitionVisual(Sprite oldSprite, Sprite newSprite)
        {
            SetOldBgSprite(oldSprite);
            SetNewBgSprite(newSprite);
            ResetRevealProgress();
            ApplyTransitionVisual();
            ApplyBackgroundFit();
        }

        private void PlayRevealTransition(Sprite newSprite, Action onComplete)
        {
            IsTransitioning = true;

            var revealTween = DOTween.To(
                    () => revealProgress,
                    value =>
                    {
                        revealProgress = value;
                        ApplyTransitionVisual();
                    },
                    1f,
                    TotalTransitionDuration)
                .SetEase(Ease.Linear);

            var bgChangeClip = AudioController.AudioClips?.bgChange2;
            transitionTween = DOTween.Sequence()
                .Join(revealTween)
                .InsertCallback(BgChangeSoundDelay, () => PlayBgChangeSound(bgChangeClip))
                .OnComplete(() => FinishTransition(newSprite, onComplete))
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        private void FinishTransition(Sprite newSprite, Action onComplete)
        {
            SetOldBgSprite(newSprite);
            SetNewBgSprite(newSprite);
            ResetRevealProgress();
            IsTransitioning = false;
            transitionTween = null;
            ApplyTransitionVisual();
            ApplyBackgroundFit();
            onComplete?.Invoke();
        }

        private void StopTransitionTween()
        {
            transitionTween?.Kill();
            transitionTween = null;
        }

        private void ResetRevealProgress()
        {
            revealProgress = 0f;
        }

        private static void PlayBgChangeSound(AudioClip clip)
        {
            if (clip != null)
            {
                AudioController.PlaySound(clip);
            }
        }

        private Sprite LoadBgSpriteForLevel(int levelIndex)
        {
            if (LevelDirector.IsCoinRewardLevelIndex(levelIndex))
            {
                return LoadBgSprite(BgAssetGroup, CoinLevelBgAssetName);
            }

            int tilebaseId = TileBaseThemeResolver.PeekTileBaseIdForLevelIndex(levelIndex);
            Sprite themeBg = ThemeUiApplier.Get(tilebaseId)?.levelBg;
            if (themeBg != null)
            {
                return themeBg;
            }

            string path = GameData.StoryBook.GetBgPathByLevelIndex(levelIndex);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            return LoadBgSprite(BgAssetGroup, "bg_" + path);
        }

        #endregion

        #region Reveal Material

        private bool EnsureRevealMaterialInstance()
        {
            if (revealMaterialInstance != null)
            {
                return true;
            }

            if (revealMaterialTemplate == null)
            {
                revealMaterialTemplate = LoadAsset<Material>(RevealMaterialPath);
            }

            if (revealMaterialTemplate == null || spriteRenderer == null)
            {
                return false;
            }

            revealMaterialInstance = Instantiate(revealMaterialTemplate);
            spriteRenderer.material = revealMaterialInstance;
            return true;
        }

        private void ApplyTransitionVisual()
        {
            if (spriteRenderer == null || !EnsureRevealMaterialInstance())
            {
                return;
            }

            if (oldBgSprite != null)
            {
                spriteRenderer.sprite = oldBgSprite;
                BindSpriteTexture(OldTexId, oldBgSprite);
            }

            Sprite revealSprite = newBgSprite != null ? newBgSprite : oldBgSprite;
            if (revealSprite != null)
            {
                BindSpriteTexture(NewTexId, revealSprite);
            }

            revealMaterialInstance.SetFloat(RevealProgressId, GetShaderRevealProgress());
            revealMaterialInstance.SetFloat(EdgeSoftnessId, GetShaderEdgeSoftness());
            ResetSpriteAlpha(spriteRenderer);
        }

        private float GetShaderRevealProgress()
        {
            if (SpreadPortion <= 0f)
            {
                return 1f;
            }

            return Mathf.Clamp01(revealProgress / SpreadPortion);
        }

        private float GetShaderEdgeSoftness()
        {
            if (revealProgress <= SpreadPortion)
            {
                return edgeSoftness;
            }

            if (SpreadPortion >= 1f || SoftnessFadeDuration <= 0f)
            {
                return 0f;
            }

            float fadeT = (revealProgress - SpreadPortion) / (1f - SpreadPortion);
            return Mathf.Lerp(edgeSoftness, 0f, fadeT);
        }

        private void BindSpriteTexture(int propertyId, Sprite sprite)
        {
            if (sprite == null || sprite.texture == null || revealMaterialInstance == null)
            {
                return;
            }

            var texture = sprite.texture;
            var rect = sprite.textureRect;

            revealMaterialInstance.SetTexture(propertyId, texture);
            revealMaterialInstance.SetTextureScale(propertyId, new Vector2(
                rect.width / texture.width,
                rect.height / texture.height));
            revealMaterialInstance.SetTextureOffset(propertyId, new Vector2(
                rect.x / texture.width,
                rect.y / texture.height));
        }

        private static void ResetSpriteAlpha(SpriteRenderer renderer)
        {
            if (renderer == null)
            {
                return;
            }

            var color = renderer.color;
            color.a = 1f;
            renderer.color = color;
        }

        #endregion

        #region Camera Fit

        private bool ShouldRefitBackground()
        {
            var camera = Camera.main;
            if (camera == null || Screen.width <= 0 || spriteRenderer == null)
            {
                return false;
            }

            float cameraHeight = camera.orthographicSize * 2f;
            float cameraAspect = camera.aspect;
            int spriteId = spriteRenderer.sprite != null
                ? spriteRenderer.sprite.GetInstanceID()
                : -1;

            return !Mathf.Approximately(lastCameraHeight, cameraHeight)
                || !Mathf.Approximately(lastCameraAspect, cameraAspect)
                || lastSpriteId != spriteId;
        }

        private void ApplyBackgroundFit()
        {
            if (spriteRenderer == null || spriteRenderer.sprite == null)
            {
                return;
            }

            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            var sprite = spriteRenderer.sprite;
            float pixelsPerUnit = sprite.pixelsPerUnit;
            if (pixelsPerUnit <= 0f)
            {
                return;
            }

            float spriteWidth = sprite.rect.width / pixelsPerUnit;
            float spriteHeight = sprite.rect.height / pixelsPerUnit;
            if (spriteWidth <= 0f || spriteHeight <= 0f)
            {
                return;
            }

            float cameraHeight = camera.orthographicSize * 2f;
            float cameraWidth = cameraHeight * camera.aspect;

            float uniformScale = Mathf.Max(cameraWidth / spriteWidth, cameraHeight / spriteHeight);
            transform.localScale = new Vector3(uniformScale, uniformScale, transform.localScale.z);
            transform.position = camera.transform.position + camera.transform.forward * (camera.farClipPlane - 0.01f);
            transform.forward = camera.transform.forward;

            if (spriteRenderer.drawMode != SpriteDrawMode.Simple)
            {
                spriteRenderer.size = new Vector2(spriteWidth * uniformScale, spriteHeight * uniformScale);
            }

            lastCameraHeight = cameraHeight;
            lastCameraAspect = camera.aspect;
            lastSpriteId = sprite.GetInstanceID();
        }

        #endregion
    }
}
