using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FishFramework;

namespace FishTilePlay
{
    public enum GoalHudStyle
    {
        Collection = 0,
        IQChallenge = 1
    }

    /// <summary>Shared dock goal HUD: BoxCover collection or IQ Challenge progress.</summary>
    public sealed class BoxCoverHUD : MonoBehaviour
    {
        public const string PrefabPath = "Assets/CABRes/Prefabs/BoxCover/BoxCoverHUD.prefab";
        public const string IQPrefabPath = "Assets/CABRes/Prefabs/IQChallenge/IQChallengeHUD.prefab";

        [SerializeField] RectTransform root;
        [SerializeField] RectTransform contentRoot;
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] Image pandaIcon;
        [SerializeField] Image collectibleIcon;
        [SerializeField] RectTransform baseBoard;
        [SerializeField] TextMeshProUGUI progressText;
        [SerializeField] TextMeshProUGUI fullCollectionText;
        [Header("收集物飞行")]
        [SerializeField] BoxCoverFlySettings flySettings = new BoxCoverFlySettings();

        const string IQCharacterPath = "Assets/CABRes/Sprites/IQChallenge/IQ_Character.png";
        static readonly string[] IQDogPaths =
        {
            "Assets/CABRes/Sprites/IQChallenge/IQ_Dog_0.png",
            "Assets/CABRes/Sprites/IQChallenge/IQ_Dog_1.png",
            "Assets/CABRes/Sprites/IQChallenge/IQ_Dog_2.png",
            "Assets/CABRes/Sprites/IQChallenge/IQ_Dog_3.png",
            "Assets/CABRes/Sprites/IQChallenge/IQ_Dog_4.png"
        };
        const string IQBubblePath = "Assets/CABRes/Sprites/IQChallenge/IQ_Bubble.png";
        const string IQBrainPath = "Assets/CABRes/Sprites/IQChallenge/IQ_IconBrain.png";
        const string IQBulbPath = "Assets/CABRes/Sprites/IQChallenge/IQ_IconBulb.png";
        const string IQTrackPath = "Assets/CABRes/Sprites/IQChallenge/IQ_BarTrack.png";
        const string IQFillPath = "Assets/CABRes/Sprites/IQChallenge/IQ_BarFill.png";

        DG.Tweening.Sequence feedbackSequence;
        DG.Tweening.Tween iqStageFillTween;
        Image[] iqStageFills;
        RectTransform iqProgressParticle;
        bool iqStageBarsBound;
        string sourcePrefabPath = PrefabPath;
        Tween appearTween;
        bool appearFading;
        Vector2 restAnchoredPosition;
        bool hasRestAnchoredPosition;
        GoalHudStyle currentStyle;
        Sprite defaultCollectibleSprite;
        Sprite collectibleOverride;
        RectTransform iqRoot;
        Image iqCharacter;
        Image iqBubble;
        Image iqIcon;
        Image iqBarTrack;
        Image iqBarFill;

        public string SourcePrefabPath => sourcePrefabPath;

        public Color ReadNamedImageColor(string nodeName, Color fallback)
        {
            if (string.IsNullOrEmpty(nodeName))
                return fallback;

            Transform node = transform.Find(nodeName);
            if (node == null)
                return fallback;

            Image image = node.GetComponent<Image>();
            if (image == null)
                return fallback;

            return image.color;
        }

        public static string PrefabPathFor(GoalHudStyle style)
        {
            return style == GoalHudStyle.IQChallenge ? IQPrefabPath : PrefabPath;
        }

        public static BoxCoverHUD Create(Transform parent, TMP_FontAsset font)
        {
            return Create(parent, font, PrefabPath);
        }

        public static BoxCoverHUD Create(Transform parent, TMP_FontAsset font, string prefabPath)
        {
            string path = string.IsNullOrEmpty(prefabPath) ? PrefabPath : prefabPath;
            GameObject prefab = UILoadHelper.LoadAsset<GameObject>(path);
#if UNITY_EDITOR
            if (prefab == null)
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
#endif
            if (prefab == null)
            {
                Debug.LogError($"[GoalHud] HUD prefab is missing: {path}");
                return null;
            }

            GameObject instance = Object.Instantiate(prefab, parent, false);
            instance.name = path == IQPrefabPath ? "IQChallengeHUD" : "BoxCoverHUD";
            if (parent != null)
                instance.layer = parent.gameObject.layer;

            BoxCoverHUD hud = instance.GetComponent<BoxCoverHUD>();
            if (hud == null)
                hud = instance.AddComponent<BoxCoverHUD>();

            hud.sourcePrefabPath = path;
            hud.BindIfNeeded();
            hud.ApplyRuntimeSetup(font);
            hud.Hide();
            return hud;
        }

        public void LayoutToDock(RectTransform canvasParent, Vector3 anchorWorld)
        {
            BindIfNeeded();
            if (root == null || canvasParent == null)
                return;

            Camera gameCamera = Camera.main;
            if (gameCamera == null)
                return;

            Rect parentRect = canvasParent.rect;
            if (parentRect.width < 1f || parentRect.height < 1f)
                return;

            Vector3 anchorViewport = gameCamera.WorldToViewportPoint(anchorWorld);
            float localX = ViewportToParentLocal(parentRect.xMin, parentRect.xMax, anchorViewport.x);
            float localY = ViewportToParentLocal(parentRect.yMin, parentRect.yMax, anchorViewport.y);

            root.anchorMin = new Vector2(1f, 1f);
            root.anchorMax = new Vector2(1f, 1f);
            root.pivot = new Vector2(1f, 1f);
            restAnchoredPosition = new Vector2(localX - parentRect.xMax, localY - parentRect.yMax);
            hasRestAnchoredPosition = true;
            root.anchoredPosition = restAnchoredPosition;
        }

        static float ViewportToParentLocal(float min, float max, float viewport)
        {
            return Mathf.Lerp(min, max, Mathf.Clamp01(viewport));
        }

        void BindIfNeeded()
        {
            if (root == null)
                root = transform as RectTransform;
            if (contentRoot == null)
            {
                Transform content = transform.Find("Content");
                if (content != null)
                    contentRoot = content as RectTransform;
            }
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (pandaIcon == null)
            {
                Transform panda = FindHudChild("Panda");
                if (panda != null)
                    pandaIcon = panda.GetComponent<Image>();
            }
            if (collectibleIcon == null)
            {
                Transform collectible = FindHudChild("Collectible");
                if (collectible == null)
                    collectible = FindHudChild("Donut");
                if (collectible != null)
                    collectibleIcon = collectible.GetComponent<Image>();
            }
            if (baseBoard == null)
            {
                Transform baseNode = FindHudChild("Base");
                if (baseNode != null)
                    baseBoard = baseNode as RectTransform;
            }
            if (progressText == null)
            {
                Transform progress = FindHudChild("Progress");
                if (progress != null)
                    progressText = progress.GetComponent<TextMeshProUGUI>();
            }
            if (fullCollectionText == null)
            {
                Transform fullCollection = FindHudChild("FullCollection");
                if (fullCollection != null)
                    fullCollectionText = fullCollection.GetComponent<TextMeshProUGUI>();
            }
        }

        Transform FindHudChild(string childName)
        {
            if (contentRoot != null)
            {
                Transform underContent = contentRoot.Find(childName);
                if (underContent != null)
                    return underContent;
            }

            return transform.Find(childName);
        }

        void ApplyRuntimeSetup(TMP_FontAsset font)
        {
            TMP_FontAsset resolvedFont = ResolveFont(font);
            ApplyFont(progressText, resolvedFont);
            ApplyFont(fullCollectionText, resolvedFont);

            if (fullCollectionText != null)
                fullCollectionText.gameObject.SetActive(false);
        }

        static TMP_FontAsset ResolveFont(TMP_FontAsset font)
        {
            if (font != null && font.material != null)
                return font;
            TMP_FontAsset fallback = TMP_Settings.defaultFontAsset;
            return fallback != null && fallback.material != null ? fallback : font;
        }

        static void ApplyFont(TextMeshProUGUI tmp, TMP_FontAsset font)
        {
            if (tmp == null || font == null || font.material == null)
                return;

            tmp.font = font;
            tmp.fontSharedMaterial = font.material;
        }

        public void Show(int collected, int total, bool animate)
        {
            Show(collected, total, animate, GoalHudStyle.Collection);
        }

        public void Show(int collected, int total, bool animate, GoalHudStyle style)
        {
            BindIfNeeded();
            if (root == null)
                return;

            currentStyle = style;
            bool iqBars = style == GoalHudStyle.IQChallenge && HasIqStageBars();
            if (iqBars)
            {
                if (iqRoot != null)
                    iqRoot.gameObject.SetActive(false);
            }
            else
            {
                ApplyHudStyle(style);
            }

            bool alreadyVisible = gameObject.activeInHierarchy;
            gameObject.SetActive(true);
            root.SetAsLastSibling();
            if (!alreadyVisible || !animate)
                LayoutToCurrentDock();
            if (canvasGroup != null && !appearFading)
                canvasGroup.alpha = 1f;
            int safeTotal = Mathf.Max(0, total);
            int safeCollected = Mathf.Clamp(collected, 0, safeTotal);

            if (iqBars)
            {
                ApplyCollectibleSprite();
                if (progressText != null)
                    progressText.gameObject.SetActive(false);
                ApplyIqStageBars(safeCollected, animate);
            }
            else if (style == GoalHudStyle.IQChallenge)
            {
                EnsureIQVisual();
                UpdateIQProgress(safeCollected, safeTotal);
                if (progressText != null)
                    progressText.gameObject.SetActive(false);
                if (animate)
                    PlayProgressFeedback();
            }
            else
            {
                if (progressText != null)
                {
                    progressText.gameObject.SetActive(true);
                    ApplyCollectibleSprite();
                    if (progressText.font != null && progressText.font.material != null)
                        progressText.text = $"{safeCollected}/{safeTotal}";
                }

                if (animate)
                    PlayProgressFeedback();
            }

            if (fullCollectionText != null)
                fullCollectionText.gameObject.SetActive(false);
        }

        public BoxCoverFlySettings FlySettings
        {
            get
            {
                if (flySettings == null)
                    flySettings = new BoxCoverFlySettings();
                flySettings.EnsureDefaults();
                return flySettings;
            }
        }

        public void CancelAppear()
        {
            appearTween?.Kill();
            appearTween = null;
            appearFading = false;
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
        }

        public void PlayAppearFade(float duration, float delay = 0f)
        {
            BindIfNeeded();
            if (canvasGroup == null)
                return;

            appearTween?.Kill();
            appearFading = true;
            canvasGroup.alpha = 0f;
            float fadeDuration = Mathf.Max(0.05f, duration);
            float wait = Mathf.Max(0f, delay);
            Sequence sequence = DOTween.Sequence().SetTarget(canvasGroup);
            if (wait > 0f)
                sequence.AppendInterval(wait);
            sequence.Append(canvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.Linear));
            sequence.OnComplete(() =>
            {
                appearFading = false;
                appearTween = null;
            });
            sequence.OnKill(() =>
            {
                appearFading = false;
                appearTween = null;
            });
            appearTween = sequence;
        }

        public void Hide()
        {
            KillTweens();
            if (gameObject != null)
                gameObject.SetActive(false);
        }

        public void FadeOut(float duration, System.Action onHidden = null)
        {
            BindIfNeeded();
            if (root == null)
            {
                onHidden?.Invoke();
                return;
            }

            gameObject.SetActive(true);
            KillTweens();
            if (canvasGroup == null)
            {
                Hide();
                onHidden?.Invoke();
                return;
            }

            float fadeDuration = Mathf.Max(0.05f, duration);
            canvasGroup.DOFade(0f, fadeDuration)
                .SetEase(Ease.InQuad)
                .SetUpdate(true)
                .SetTarget(this)
                .OnComplete(() =>
                {
                    Hide();
                    onHidden?.Invoke();
                });
        }

        public void LayoutToCurrentDock()
        {
            RectTransform parent = transform.parent as RectTransform;
            DockActor dock = BoxCoverLayout.FindDock();
            if (parent == null || dock == null)
                return;

            LayoutToDock(parent, dock.GetHudAnchorLayoutWorldPosition());
            SyncToDockBreathingMotion();
        }

        /// <summary>可见条矩形中心的世界 Y。未完成码头投影时返回 false。</summary>
        public bool TryGetRowCenterWorldY(out float worldY)
        {
            worldY = 0f;
            if (!hasRestAnchoredPosition)
                return false;

            BindIfNeeded();
            RectTransform row = root;
            if (baseBoard != null && baseBoard.gameObject.activeInHierarchy)
                row = baseBoard;
            else if (contentRoot != null)
                row = contentRoot;
            if (row == null)
                return false;

            worldY = RectCenterWorldY(row);
            return true;
        }

        public static float RectCenterWorldY(RectTransform rect)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return (corners[0].y + corners[1].y) * 0.5f;
        }

        /// <summary> 将满呼吸时跟随熊猫的世界位移，HUD 与熊猫相对位置保持不变。 </summary>
        public void SyncToDockBreathingMotion()
        {
            if (root == null || !isActiveAndEnabled || !hasRestAnchoredPosition)
                return;

            RectTransform parent = transform.parent as RectTransform;
            DockActor dock = BoxCoverLayout.FindDock();
            if (parent == null || dock == null)
                return;

            if (!dock.TryGetBoxCoverBreathingFollow(out Vector3 restWorld, out Vector3 currentWorld))
            {
                root.anchoredPosition = restAnchoredPosition;
                return;
            }

            Vector2 restLocal = WorldToAnchored(parent, restWorld);
            Vector2 currentLocal = WorldToAnchored(parent, currentWorld);
            root.anchoredPosition = restAnchoredPosition + (currentLocal - restLocal);
        }

        public void RestoreDockBreathingLayout()
        {
            if (root != null && hasRestAnchoredPosition)
                root.anchoredPosition = restAnchoredPosition;
        }

        static Vector2 WorldToAnchored(RectTransform canvasParent, Vector3 world)
        {
            Camera gameCamera = Camera.main;
            if (gameCamera == null || canvasParent == null)
                return Vector2.zero;

            Rect parentRect = canvasParent.rect;
            Vector3 viewport = gameCamera.WorldToViewportPoint(world);
            float localX = ViewportToParentLocal(parentRect.xMin, parentRect.xMax, viewport.x);
            float localY = ViewportToParentLocal(parentRect.yMin, parentRect.yMax, viewport.y);
            return new Vector2(localX - parentRect.xMax, localY - parentRect.yMax);
        }

        public void SetCollectibleOverride(Sprite sprite)
        {
            BindIfNeeded();
            collectibleOverride = sprite;
            ApplyCollectibleSprite();
        }

        void ApplyCollectibleSprite()
        {
            if (collectibleIcon == null)
                return;
            if (defaultCollectibleSprite == null)
                defaultCollectibleSprite = collectibleIcon.sprite;
            Sprite sprite = collectibleOverride != null ? collectibleOverride : defaultCollectibleSprite;
            if (sprite != null)
                collectibleIcon.sprite = sprite;
        }

        void ApplyHudStyle(GoalHudStyle style)
        {
            bool iq = style == GoalHudStyle.IQChallenge;
            Transform basePlate = FindHudChild("Base");
            if (basePlate != null)
                basePlate.gameObject.SetActive(!iq);
            if (collectibleIcon != null)
                collectibleIcon.gameObject.SetActive(!iq);
            if (iqRoot != null)
                iqRoot.gameObject.SetActive(iq);
        }

        void EnsureIQVisual()
        {
            if (iqRoot != null)
            {
                iqRoot.gameObject.SetActive(true);
                return;
            }

            Transform parent = contentRoot != null ? (Transform)contentRoot : transform;
            GameObject rootObject = new GameObject("IQCluster", typeof(RectTransform));
            iqRoot = (RectTransform)rootObject.transform;
            iqRoot.SetParent(parent, false);
            iqRoot.anchorMin = new Vector2(0.5f, 0.5f);
            iqRoot.anchorMax = new Vector2(0.5f, 0.5f);
            iqRoot.pivot = new Vector2(0.5f, 0.5f);
            iqRoot.sizeDelta = new Vector2(300f, 110f);
            iqRoot.anchoredPosition = new Vector2(-10f, 12f);
            if (parent != null)
                rootObject.layer = parent.gameObject.layer;

            iqCharacter = CreateHudImage(iqRoot, "Character", new Vector2(-118f, 8f), new Vector2(96f, 96f));
            iqBubble = CreateHudImage(iqRoot, "Bubble", new Vector2(48f, 12f), new Vector2(228f, 82f));
            iqIcon = CreateHudImage(iqRoot, "Icon", new Vector2(-16f, 14f), new Vector2(42f, 42f));
            iqBarTrack = CreateHudImage(iqRoot, "BarTrack", new Vector2(66f, 12f), new Vector2(136f, 24f));
            iqBarFill = CreateHudImage(iqRoot, "BarFill", new Vector2(66f, 12f), new Vector2(122f, 16f));

            iqCharacter.sprite = LoadHudSprite(ResolveIQDogPath(0));
            iqBubble.sprite = LoadHudSprite(IQBubblePath);
            iqIcon.sprite = LoadHudSprite(IQBrainPath);
            iqBarTrack.sprite = LoadHudSprite(IQTrackPath);
            iqBarFill.sprite = LoadHudSprite(IQFillPath);

            iqCharacter.preserveAspect = true;
            iqBubble.preserveAspect = true;
            iqIcon.preserveAspect = true;
            iqBarTrack.preserveAspect = true;
            iqBarFill.preserveAspect = true;

            iqBarFill.type = Image.Type.Filled;
            iqBarFill.fillMethod = Image.FillMethod.Horizontal;
            iqBarFill.fillOrigin = 0;
            iqBarFill.fillAmount = 0f;
        }

        static Image CreateHudImage(RectTransform parent, string childName, Vector2 anchored, Vector2 size)
        {
            GameObject child = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = (RectTransform)child.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchored;
            rect.sizeDelta = size;
            child.layer = parent.gameObject.layer;
            Image image = child.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        static Sprite LoadHudSprite(string path)
        {
            Sprite sprite = UILoadHelper.LoadAsset<Sprite>(path);
#if UNITY_EDITOR
            if (sprite == null)
                sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
#endif
            return sprite;
        }

        static string ResolveIQDogPath(int stage)
        {
            int index = Mathf.Clamp(stage, 0, IQDogPaths.Length - 1);
            return IQDogPaths[index];
        }

        void UpdateIQProgress(int collected, int total)
        {
            float ratio = total > 0 ? collected / (float)total : 0f;
            if (iqBarFill != null)
                iqBarFill.fillAmount = Mathf.Clamp01(ratio);
            if (iqCharacter != null)
            {
                Sprite dog = LoadHudSprite(ResolveIQDogPath(collected));
                if (dog == null)
                    dog = LoadHudSprite(IQCharacterPath);
                if (dog != null)
                    iqCharacter.sprite = dog;
            }
            if (iqIcon != null)
            {
                Sprite icon = ratio >= 0.25f
                    ? LoadHudSprite(IQBulbPath)
                    : LoadHudSprite(IQBrainPath);
                if (icon != null)
                    iqIcon.sprite = icon;
            }
        }

        void PlayProgressFeedback()
        {
            BindIfNeeded();
            RectTransform target = currentStyle == GoalHudStyle.IQChallenge && iqCharacter != null
                ? iqCharacter.rectTransform
                : baseBoard;
            if (target == null)
                return;

            Vector3 restScale = Vector3.one;
            target.DOKill();
            target.localScale = restScale;
            feedbackSequence?.Kill();
            feedbackSequence = DOTween.Sequence().SetTarget(this);
            feedbackSequence.Append(target.DOScale(restScale * 1.12f, 0.12f).SetEase(Ease.OutBack));
            feedbackSequence.Append(target.DOScale(restScale, 0.18f).SetEase(Ease.OutSine));
            feedbackSequence.OnComplete(() => feedbackSequence = null);
        }

        public RectTransform GetCollectibleRect()
        {
            BindIfNeeded();
            if (currentStyle == GoalHudStyle.IQChallenge && iqIcon != null)
                return iqIcon.rectTransform;
            if (collectibleIcon != null)
                return collectibleIcon.rectTransform;
            return root;
        }

        public Vector3 GetTargetWorldPosition(Vector3 sourceWorldPosition)
        {
            Camera gameCamera = Camera.main;
            RectTransform target = GetCollectibleRect();
            if (gameCamera == null || target == null)
                return sourceWorldPosition + Vector3.up * 3f;

            Canvas canvas = GetComponentInParent<Canvas>();
            Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(uiCamera, target.position);
            float depth = gameCamera.WorldToScreenPoint(sourceWorldPosition).z;
            if (depth <= 0.01f)
                depth = Mathf.Abs(sourceWorldPosition.z - gameCamera.transform.position.z);
            return gameCamera.ScreenToWorldPoint(
                new Vector3(screenPosition.x, screenPosition.y, depth));
        }

        bool HasIqStageBars()
        {
            BindIqStageBars();
            return iqStageFills != null && iqStageFills.Length > 0 && iqStageFills[0] != null;
        }

        void BindIqStageBars()
        {
            if (iqStageBarsBound)
                return;
            iqStageBarsBound = true;

            Transform track = FindChild(transform, "ProgressBar") ?? FindChild(transform, "progressbar");
            if (track == null)
                return;

            var fills = new Image[4];
            int found = 0;
            for (int i = 0; i < 4; i++)
            {
                string numbered = "ProgressBar_" + (i + 1);
                string lower = "progressbar_" + (i + 1);
                Transform node = track.Find(numbered) ?? track.Find(lower)
                    ?? FindChild(transform, numbered) ?? FindChild(transform, lower);
                if (node == null)
                    continue;
                Image image = node.GetComponent<Image>();
                if (image == null)
                    continue;
                image.type = Image.Type.Filled;
                image.fillMethod = Image.FillMethod.Horizontal;
                image.fillOrigin = (int)Image.OriginHorizontal.Left;
                fills[i] = image;
                found++;
            }

            if (found > 0)
                iqStageFills = fills;

            Transform particle = FindChild(transform, "ProgressBar_Par");
            if (particle != null)
                iqProgressParticle = particle as RectTransform;
        }

        static Transform FindChild(Transform root, string name)
        {
            if (root == null)
                return null;
            if (root.name == name)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChild(root.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }

        void ApplyIqStageBars(int collected, bool animate)
        {
            BindIqStageBars();
            if (iqStageFills == null)
                return;

            iqStageFillTween?.Kill();
            iqStageFillTween = null;
            int shown = Mathf.Clamp(collected, 0, iqStageFills.Length);
            Image filling = null;
            for (int i = 0; i < iqStageFills.Length; i++)
            {
                Image fill = iqStageFills[i];
                if (fill == null)
                    continue;
                fill.gameObject.SetActive(true);
                float target = i < shown ? 1f : 0f;
                bool fillingNow = animate && i == shown - 1;
                if (!fillingNow)
                {
                    fill.fillAmount = target;
                    continue;
                }

                filling = fill;
                fill.fillAmount = 0f;
                int fillIndex = i;
                iqStageFillTween = fill.DOFillAmount(1f, 0.5f)
                    .SetEase(Ease.Linear)
                    .SetUpdate(true)
                    .SetTarget(this)
                    .OnUpdate(() => PlaceIqProgressParticle(fill))
                    .OnComplete(() =>
                    {
                        if (fillIndex == iqStageFills.Length - 1)
                            HideIqProgressParticle();
                        else
                            PlaceIqProgressParticle(fill);
                        PlayCollectibleBounce();
                    });
            }

            if (filling != null)
                PlaceIqProgressParticle(filling);
            else if (shown > 0 && shown < iqStageFills.Length)
                PlaceIqProgressParticle(iqStageFills[shown - 1]);
            else
                HideIqProgressParticle();
        }

        void PlaceIqProgressParticle(Image fill)
        {
            if (iqProgressParticle == null || fill == null)
                return;

            if (!iqProgressParticle.gameObject.activeSelf)
            {
                iqProgressParticle.gameObject.SetActive(true);
                if (iqProgressParticle.localScale.sqrMagnitude < 0.001f)
                    iqProgressParticle.localScale = Vector3.one;
            }

            RectTransform bar = fill.rectTransform;
            Rect rect = bar.rect;
            float edge = rect.xMin + rect.width * Mathf.Clamp01(fill.fillAmount);
            Vector3 world = bar.TransformPoint(new Vector3(edge, rect.center.y, 0f));
            Vector3 position = iqProgressParticle.position;
            position.x = world.x;
            position.y = world.y;
            iqProgressParticle.position = position;
        }

        void HideIqProgressParticle()
        {
            if (iqProgressParticle != null)
                iqProgressParticle.gameObject.SetActive(false);
        }

        void PlayCollectibleBounce()
        {
            RectTransform icon = collectibleIcon != null ? collectibleIcon.rectTransform : null;
            if (icon == null)
                return;

            icon.localScale = Vector3.one;
            feedbackSequence?.Kill();
            feedbackSequence = DOTween.Sequence().SetUpdate(true).SetTarget(icon).SetLink(icon.gameObject, LinkBehaviour.KillOnDestroy);
            feedbackSequence.Append(icon.DOScale(1.34f, 0.18f).SetEase(Ease.OutSine));
            feedbackSequence.Append(icon.DOScale(0.84f, 0.16f).SetEase(Ease.InOutSine));
            feedbackSequence.Append(icon.DOScale(1.16f, 0.18f).SetEase(Ease.OutSine));
            feedbackSequence.Append(icon.DOScale(0.94f, 0.16f).SetEase(Ease.InOutSine));
            feedbackSequence.Append(icon.DOScale(1.06f, 0.16f).SetEase(Ease.OutSine));
            feedbackSequence.Append(icon.DOScale(Vector3.one, 0.18f).SetEase(Ease.OutSine));
            feedbackSequence.OnComplete(() => feedbackSequence = null);
        }

        void KillTweens()
        {
            iqStageFillTween?.Kill();
            iqStageFillTween = null;
            appearTween?.Kill();
            appearTween = null;
            appearFading = false;
            feedbackSequence?.Kill();
            feedbackSequence = null;
            DOTween.Kill(this);
            if (root != null)
            {
                root.DOKill();
                root.localScale = Vector3.one;
            }
            if (baseBoard != null)
            {
                baseBoard.DOKill();
                baseBoard.localScale = Vector3.one;
            }
            if (collectibleIcon != null)
            {
                collectibleIcon.rectTransform.DOKill();
                collectibleIcon.rectTransform.localScale = Vector3.one;
            }
            if (iqCharacter != null)
            {
                iqCharacter.rectTransform.DOKill();
                iqCharacter.rectTransform.localScale = Vector3.one;
            }
            if (iqIcon != null)
            {
                iqIcon.rectTransform.DOKill();
                iqIcon.rectTransform.localScale = Vector3.one;
            }
            if (fullCollectionText != null)
                fullCollectionText.transform.DOKill();
        }

        void OnDisable()
        {
            KillTweens();
        }

        void OnDestroy()
        {
            KillTweens();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (flySettings == null)
                flySettings = new BoxCoverFlySettings();
            flySettings.EnsureDefaults();
        }
#endif
    }
}
