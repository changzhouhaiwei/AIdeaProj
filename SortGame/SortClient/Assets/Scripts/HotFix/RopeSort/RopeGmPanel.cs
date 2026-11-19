using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RopeSort
{
    public static class RopeGmPanel
    {
        const int SortOrder = 32000;

        static GameObject root;
        static TMP_InputField input;
        static TextMeshProUGUI message;
        static Sprite white;

        public static void Tick()
        {
#if UNITY_EDITOR
            if (F1Pressed())
                Toggle();
#endif
            if (root != null && EscapePressed())
                Close();
        }

        public static void Toggle()
        {
            if (root != null)
                Close();
            else
                Open();
        }

        public static void Open()
        {
            if (root != null)
                return;
            Build();
            int level = RopeGameController.DisplayedLevelIndex + 1;
            if (input != null)
                input.text = level.ToString();
            if (message != null)
                message.text = "当前第 " + level + " 关，可跳 1-" + RopeLevelCatalog.Count;
        }

        public static void Close()
        {
            if (root == null)
                return;
            Object.Destroy(root);
            root = null;
            input = null;
            message = null;
        }

        static void Jump()
        {
            string raw = input != null && input.text != null ? input.text.Trim() : "";
            if (!int.TryParse(raw, out int display) || display < 1 || display > RopeLevelCatalog.Count)
            {
                if (message != null)
                    message.text = "关卡要在 1 到 " + RopeLevelCatalog.Count + " 之间";
                return;
            }

            Close();
            RopeGameController.JumpTo(display - 1);
        }

        static bool F1Pressed()
        {
            if (Keyboard.current != null)
                return Keyboard.current.f1Key.wasPressedThisFrame;
            return Input.GetKeyDown(KeyCode.F1);
        }

        static bool EscapePressed()
        {
            if (Keyboard.current != null)
                return Keyboard.current.escapeKey.wasPressedThisFrame;
            return Input.GetKeyDown(KeyCode.Escape);
        }

        static void Build()
        {
            root = new GameObject("RopeGm", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Object.DontDestroyOnLoad(root);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortOrder;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            var dim = CreateImage("Dim", root.transform, new Color(0f, 0f, 0f, 0.45f));
            Stretch(dim.rectTransform);
            dim.raycastTarget = true;

            var card = CreateImage("Card", root.transform, new Color(0.98f, 0.95f, 0.9f, 1f));
            var cardRect = card.rectTransform;
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(720f, 460f);

            var font = Resources.Load<TMP_FontAsset>("RopeSort/Font");
            if (font == null)
                font = TMP_Settings.defaultFontAsset;

            var title = CreateText("Title", card.transform, font, "GM", 48, TextAlignmentOptions.Center, new Color(0.45f, 0.28f, 0.16f));
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(640f, 70f));

            message = CreateText("Message", card.transform, font, "", 28, TextAlignmentOptions.Center, new Color(0.4f, 0.32f, 0.24f));
            Place(message.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(640f, 80f));

            input = CreateInput(card.transform, font);
            Place(input.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0f, -230f), new Vector2(420f, 80f));

            var jump = CreateButton(card.transform, font, "跳关", new Color(0.86f, 0.45f, 0.28f), Jump);
            Place(jump.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(-130f, 70f), new Vector2(220f, 80f));
            var close = CreateButton(card.transform, font, "关闭", new Color(0.55f, 0.48f, 0.42f), Close);
            Place(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(130f, 70f), new Vector2(220f, 80f));
        }

        static TMP_InputField CreateInput(Transform parent, TMP_FontAsset font)
        {
            var go = new GameObject("Level", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            go.transform.SetParent(parent, false);
            var plate = go.GetComponent<Image>();
            plate.sprite = White();
            plate.color = Color.white;
            plate.raycastTarget = true;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(go.transform, false);
            Stretch(viewport.GetComponent<RectTransform>(), 16f);

            var text = CreateText("Text", viewport.transform, font, "", 36, TextAlignmentOptions.MidlineLeft, new Color(0.25f, 0.16f, 0.1f));
            Stretch(text.rectTransform);
            text.raycastTarget = false;

            var placeholder = CreateText("Placeholder", viewport.transform, font, "关卡", 36, TextAlignmentOptions.MidlineLeft, new Color(0.6f, 0.55f, 0.5f));
            Stretch(placeholder.rectTransform);
            placeholder.raycastTarget = false;

            var field = go.GetComponent<TMP_InputField>();
            field.textViewport = viewport.GetComponent<RectTransform>();
            field.textComponent = text;
            field.placeholder = placeholder;
            field.fontAsset = font;
            field.pointSize = 36;
            field.contentType = TMP_InputField.ContentType.IntegerNumber;
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.characterLimit = 4;
            return field;
        }

        static Button CreateButton(Transform parent, TMP_FontAsset font, string label, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = White();
            image.color = color;
            image.raycastTarget = true;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            var text = CreateText("Label", go.transform, font, label, 32, TextAlignmentOptions.Center, Color.white);
            Stretch(text.rectTransform);
            return button;
        }

        static Image CreateImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = White();
            image.color = color;
            return image;
        }

        static TextMeshProUGUI CreateText(string name, Transform parent, TMP_FontAsset font, string value, float size, TextAlignmentOptions align, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            if (font != null)
                text.font = font;
            text.text = value;
            text.fontSize = size;
            text.alignment = align;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        static void Place(RectTransform rect, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, anchor.y);
            rect.sizeDelta = size;
            rect.anchoredPosition = pos;
        }

        static void Stretch(RectTransform rect, float pad = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(pad, pad);
            rect.offsetMax = new Vector2(-pad, -pad);
        }

        static Sprite White()
        {
            if (white != null)
                return white;
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            white = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            return white;
        }
    }
}
