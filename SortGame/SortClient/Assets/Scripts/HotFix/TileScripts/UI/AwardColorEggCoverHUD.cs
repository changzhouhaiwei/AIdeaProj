using DG.Tweening;
using FishFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FishTilePlay
{
    public sealed class AwardColorEggCoverHUD : MonoBehaviour
    {
        public const string PrefabPath = "Assets/CABRes/Prefabs/AwardColorEggCover/AwardColorEggCoverHUD.prefab";

        [SerializeField] RectTransform root;
        [SerializeField] Image eggIcon;
        [SerializeField] TextMeshProUGUI progress;
        [SerializeField] CanvasGroup group;
        DG.Tweening.Tween punch;

        public Vector3 TargetWorldPosition =>
            eggIcon != null ? eggIcon.transform.position : transform.position;

        public static AwardColorEggCoverHUD Create(Transform parent)
        {
            GameObject prefab = UILoadHelper.LoadAsset<GameObject>(PrefabPath);
#if UNITY_EDITOR
            if (prefab == null)
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
#endif
            if (prefab != null)
            {
                GameObject instance = Instantiate(prefab, parent, false);
                instance.name = "AwardColorEggCoverHUD";
                if (parent != null)
                    instance.layer = parent.gameObject.layer;
                AwardColorEggCoverHUD loaded = instance.GetComponent<AwardColorEggCoverHUD>();
                if (loaded == null)
                    loaded = instance.AddComponent<AwardColorEggCoverHUD>();
                loaded.EnsureRefs();
                loaded.Hide();
                return loaded;
            }

            return CreateNew(parent);
        }

        public static AwardColorEggCoverHUD CreateNew(Transform parent)
        {
            GameObject rootObject = new GameObject(
                "AwardColorEggCoverHUD",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(AwardColorEggCoverHUD));
            rootObject.layer = parent != null ? parent.gameObject.layer : 5;
            RectTransform rect = (RectTransform)rootObject.transform;
            rect.SetParent(parent, false);
            AwardColorEggCoverHUD hud = rootObject.GetComponent<AwardColorEggCoverHUD>();
            hud.Build(rect);
            hud.Hide();
            return hud;
        }

        public void Show(int collected, int total, bool animate)
        {
            gameObject.SetActive(true);
            if (group != null)
                group.alpha = 1f;
            if (progress != null)
                progress.text = collected + "/" + total;
            Layout();
            if (!animate || eggIcon == null)
                return;
            punch?.Kill();
            eggIcon.rectTransform.localScale = Vector3.one;
            punch = eggIcon.rectTransform.DOPunchScale(new Vector3(0.18f, 0.18f, 0f), 0.22f, 5, 0.6f);
        }

        public void Hide()
        {
            punch?.Kill();
            gameObject.SetActive(false);
        }

        public void Layout()
        {
            if (root == null)
                return;
            root.anchorMin = new Vector2(0.5f, 1f);
            root.anchorMax = new Vector2(0.5f, 1f);
            root.pivot = new Vector2(0.5f, 1f);
            root.anchoredPosition = new Vector2(40f, -150f);
            root.sizeDelta = new Vector2(210f, 92f);
        }

        void EnsureRefs()
        {
            if (root == null)
                root = transform as RectTransform;
            if (group == null)
                group = GetComponent<CanvasGroup>();
            if (eggIcon == null)
            {
                Transform found = transform.Find("Board/Egg");
                if (found != null)
                    eggIcon = found.GetComponent<Image>();
            }
            if (progress == null)
            {
                Transform found = transform.Find("Board/Progress");
                if (found != null)
                    progress = found.GetComponent<TextMeshProUGUI>();
            }
        }

        void Build(RectTransform rect)
        {
            root = rect;
            group = GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            GameObject board = CreateChild("Board", rect);
            RectTransform boardRect = (RectTransform)board.transform;
            boardRect.anchorMin = Vector2.zero;
            boardRect.anchorMax = Vector2.one;
            boardRect.offsetMin = Vector2.zero;
            boardRect.offsetMax = Vector2.zero;
            Image boardImage = board.AddComponent<Image>();
            boardImage.color = new Color(1f, 0.97f, 0.92f, 0.96f);
            boardImage.raycastTarget = false;

            eggIcon = CreateImage("Egg", boardRect, new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(58f, 70f));
            eggIcon.preserveAspect = true;

            Image dog = CreateImage("Dog", boardRect, new Vector2(1f, 0.5f), new Vector2(-8f, 8f), new Vector2(84f, 84f));
            dog.preserveAspect = true;

            progress = CreateText("Progress", boardRect, new Vector2(70f, 0f), 32f);
            progress.text = "0/3";
        }

        static GameObject CreateChild(string name, Transform parent)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            child.layer = parent.gameObject.layer;
            child.transform.SetParent(parent, false);
            return child;
        }

        static Image CreateImage(string name, RectTransform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            GameObject child = CreateChild(name, parent);
            RectTransform rect = (RectTransform)child.transform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = child.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        static TextMeshProUGUI CreateText(string name, RectTransform parent, Vector2 position, float size)
        {
            GameObject child = CreateChild(name, parent);
            RectTransform rect = (RectTransform)child.transform;
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(80f, 50f);
            TextMeshProUGUI text = child.AddComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.color = new Color(0.45f, 0.24f, 0.16f, 1f);
            text.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null)
                text.font = TMP_Settings.defaultFontAsset;
            return text;
        }
    }
}
