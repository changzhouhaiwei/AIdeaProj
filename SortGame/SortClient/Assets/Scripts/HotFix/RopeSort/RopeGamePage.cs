using System.Collections;
using System.Collections.Generic;
using FishTilePlay;
using Game.GamePlay.ColorSort;
using TileGameCore;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RopeSort
{
    public class RopeGamePage : UIPage
    {
        [SerializeField] TMP_FontAsset fontAsset;
        [SerializeField] Material fontMaterial;

        RectTransform playfield;
        RectTransform safeArea;
        RectTransform bottomBar;
        TextMeshProUGUI coinText;
        TextMeshProUGUI levelText;
        TextMeshProUGUI progressText;
        TextMeshProUGUI statusText;
        TextMeshProUGUI undoCountText;
        TextMeshProUGUI hintCountText;
        TextMeshProUGUI ropeCountText;
        Button undoButton;
        Button hintButton;
        Button ropeButton;
        RopeBoardView board;
        GameObject settlementRoot;
        Sprite[] flowers;
        Sprite hiddenFlower;
        Sprite potSprite;
        Sprite ringSprite;
        Sprite ropeSprite;

        ColorSortLevel level;
        ColorSortState state;
        readonly Stack<Hist> history = new Stack<Hist>();

        struct Hist
        {
            public ColorSortState State;
            public int Hint;
            public int Rope;
        }
        int undoCount = 3;
        int hintCount = 3;
        int ropeCount = 3;
        bool built;
        bool dragging;
        int dragCol = -1;
        int dragCut = -1;
        RectTransform ghost;
        int hintSrc = -1;
        int hintCut = -1;
        int hintDst = -1;
        bool outcomeSent;

        public override void Init()
        {
            BuildIfNeeded();
            if (safeArea != null)
                NotchSaveArea.RegisterRectTransform(safeArea);
        }

        public override void PlayShowAnimation()
        {
            if (state == null)
                Prepare(RopeGameController.DisplayedLevelIndex);
            ApplyChromeLayout();
            Canvas.ForceUpdateCanvases();
            Rebuild();
            if (isActiveAndEnabled)
                StartCoroutine(RebuildAfterLayout());
        }

        IEnumerator RebuildAfterLayout()
        {
            yield return null;
            ApplyChromeLayout();
            Canvas.ForceUpdateCanvases();
            Rebuild();
        }

        public override void PlayHideAnimation()
        {
            CancelDrag();
            canvas.enabled = false;
            isPageDisplayed = false;
            UIController.OnPageClosed(this);
        }

        public void Prepare(int levelIndex)
        {
            BuildIfNeeded();
            outcomeSent = false;
            history.Clear();
            hintSrc = hintCut = hintDst = -1;
            PullItems();
            string raw = RopeLevelCatalog.GetRaw(levelIndex);
            if (string.IsNullOrEmpty(raw))
            {
                statusText.text = "关卡没读到";
                state = null;
                return;
            }

            level = ColorSortLatticeParser.Parse(raw, levelIndex + 1);
            if (!level.Validate(out string error))
            {
                statusText.text = error;
                state = null;
                return;
            }

            state = ColorSortRules.MakeState(level);
            statusText.text = "";
            if (settlementRoot != null)
                settlementRoot.SetActive(false);
            if (levelText != null)
                levelText.text = "第" + (levelIndex + 1) + "关";
            RefreshHud();
            Rebuild();
        }

        public void ShowWinSettlement()
        {
            BuildIfNeeded();
            outcomeSent = true;
            CancelDrag();
            settlementRoot.SetActive(true);
            settlementRoot.transform.SetAsLastSibling();
        }

        void OnNextLevel()
        {
            if (settlementRoot != null)
                settlementRoot.SetActive(false);
            GameDirector.LoadNextLevel();
        }

        public void BeginDrag(int column, int index, PointerEventData eventData)
        {
            if (state == null || outcomeSent || dragging)
                return;
            if (ColorSortRules.IsLocked(state, column))
                return;
            if (column < 0 || column >= state.Cols.Count || index < 0 || index >= state.Cols[column].Count)
                return;

            dragging = true;
            dragCol = column;
            dragCut = index;
            ghost = new GameObject("Ghost", typeof(RectTransform)).GetComponent<RectTransform>();
            ghost.SetParent(transform, false);
            ghost.anchorMin = ghost.anchorMax = new Vector2(0.5f, 0.5f);
            ghost.pivot = new Vector2(0.5f, 0.5f);
            var group = ghost.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            List<int> beads = state.Cols[column];
            float size = 86f;
            for (int i = index; i < beads.Count; i++)
            {
                int value = beads[i];
                bool mystery = ColorSortLevel.IsHidden(value);
                Sprite sprite = mystery ? hiddenFlower : board.FlowerSprite(ColorSortLevel.TrueType(value));
                var go = new GameObject("G", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(ghost, false);
                var rect = go.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(size, size);
                rect.anchoredPosition = new Vector2(0f, -(i - index) * size * 0.92f);
                Image image = go.GetComponent<Image>();
                image.sprite = sprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
            Drag(eventData);
        }

        public void Drag(PointerEventData eventData)
        {
            if (!dragging || ghost == null)
                return;
            Camera cam = eventData.pressEventCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(transform as RectTransform, eventData.position, cam, out Vector2 local))
                ghost.anchoredPosition = local;
        }

        public void EndDrag(PointerEventData eventData)
        {
            if (!dragging)
                return;
            int destination = board != null ? board.ColumnAt(eventData.position, eventData.pressEventCamera) : -1;
            int source = dragCol;
            int cut = dragCut;
            CancelDrag();
            if (destination >= 0)
                TryMove(source, cut, destination);
        }

        void TryMove(int source, int cut, int destination)
        {
            ColorSortReject reason = ColorSortRules.RejectReason(state, source, cut, destination);
            if (reason != ColorSortReject.None)
            {
                statusText.text = ReasonText(reason);
                return;
            }

            history.Push(Snap());
            ColorSortRules.ApplyMove(state, level, source, cut, destination);
            hintSrc = hintCut = hintDst = -1;
            statusText.text = "";
            AfterAction();
        }

        void OnUndo()
        {
            if (state == null || outcomeSent || history.Count == 0 || undoCount <= 0)
                return;
            undoCount--;
            Hist hist = history.Pop();
            ColorSortRules.Restore(state, hist.State);
            hintCount = hist.Hint;
            ropeCount = hist.Rope;
            hintSrc = hintCut = hintDst = -1;
            statusText.text = "";
            PushItems();
            AfterAction();
        }

        void OnHint()
        {
            if (state == null || outcomeSent || hintCount <= 0)
                return;
            var moves = new List<ColorSortHintMove>();
            ColorSortRules.CollectUsefulMoves(state, moves);
            if (moves.Count == 0)
            {
                statusText.text = "没有可提示的步";
                return;
            }

            hintCount--;
            ColorSortHintMove move = moves[0];
            hintSrc = move.Src;
            hintCut = move.J;
            hintDst = move.Dst;
            statusText.text = "高亮的是建议的一步";
            PushItems();
            Rebuild();
        }

        void OnExtraRope()
        {
            if (state == null || outcomeSent || ropeCount <= 0 || state.ExtraRopeUsed)
                return;
            Hist hist = Snap();
            if (ColorSortRules.AddRope(state, level.Capacity) < 0)
            {
                statusText.text = "这局不能再加绳";
                return;
            }

            history.Push(hist);
            ropeCount--;
            hintSrc = hintCut = hintDst = -1;
            statusText.text = "";
            PushItems();
            AfterAction();
        }

        void OnSettings()
        {
            UIController.ShowPage<UISettings>();
        }

        void AfterAction()
        {
            RefreshHud();
            Rebuild();
            if (outcomeSent || level == null || state == null)
                return;
            if (ColorSortRules.IsWin(state, level))
            {
                outcomeSent = true;
                statusText.text = "过关";
                RopeGameController.NotifyWin();
                return;
            }

            if (ColorSortRules.IsDead(state, level))
            {
                outcomeSent = true;
                RopeGameController.NotifyFail();
            }
        }

        void Rebuild()
        {
            if (board == null)
                return;
            board.SetHint(hintSrc, hintCut, hintDst);
            board.Rebuild(state, level);
            RefreshHud();
        }

        void RefreshHud()
        {
            if (coinText != null)
            {
                int coins = GPUserDataManager.HasInit() ? GPUserDataManager.Instance.UserData.coin : 0;
                coinText.text = coins.ToString();
            }

            if (progressText != null && state != null && level != null)
                progressText.text = state.Done + "/" + level.GroupsTotal;
            if (undoCountText != null)
                undoCountText.text = undoCount.ToString();
            if (hintCountText != null)
                hintCountText.text = hintCount.ToString();
            if (ropeCountText != null)
                ropeCountText.text = ropeCount.ToString();
            if (undoButton != null)
                undoButton.interactable = undoCount > 0 && history.Count > 0 && !outcomeSent;
            if (hintButton != null)
                hintButton.interactable = hintCount > 0 && !outcomeSent;
            if (ropeButton != null)
                ropeButton.interactable = ropeCount > 0 && state != null && !state.ExtraRopeUsed && state.Cols.Count < 9 && !outcomeSent;
        }

        void PullItems()
        {
            if (!GPUserDataManager.HasInit())
                return;
            GPUserData data = GPUserDataManager.Instance.UserData;
            data.EnsureRopeItems();
            undoCount = data.ropeUndoCnt;
            hintCount = data.ropeHintCnt;
            ropeCount = data.ropeExtraCnt;
            PushItems();
        }

        Hist Snap()
        {
            return new Hist
            {
                State = ColorSortRules.Snapshot(state),
                Hint = hintCount,
                Rope = ropeCount
            };
        }

        void PushItems()
        {
            if (!GPUserDataManager.HasInit())
                return;
            GPUserData data = GPUserDataManager.Instance.UserData;
            data.ropeUndoCnt = undoCount;
            data.ropeHintCnt = hintCount;
            data.ropeExtraCnt = ropeCount;
            GPUserDataManager.Instance.SaveData();
        }

        void CancelDrag()
        {
            dragging = false;
            dragCol = -1;
            dragCut = -1;
            if (ghost != null)
            {
                Destroy(ghost.gameObject);
                ghost = null;
            }
        }

        static string ReasonText(ColorSortReject reason)
        {
            switch (reason)
            {
                case ColorSortReject.Full: return "这一列放不下";
                case ColorSortReject.Color: return "颜色接不上";
                case ColorSortReject.Locked: return "这列还锁着";
                case ColorSortReject.Mystery: return "问号不能接到有花的绳子上";
                default: return "这一步不行";
            }
        }

        void BuildIfNeeded()
        {
            if (built)
                return;
            built = true;
            LoadArt();
            var root = transform as RectTransform;
            var bg = CreateImage("Bg", root, null, new Color(0.96f, 0.91f, 0.84f));
            Stretch(bg.rectTransform);

            playfield = CreateRect("Playfield", root);
            Stretch(playfield);

            safeArea = CreateRect("SafeArea", root);
            Stretch(safeArea);
            var top = CreateRect("TopBar", safeArea);
            top.anchorMin = new Vector2(0f, 1f);
            top.anchorMax = new Vector2(1f, 1f);
            top.pivot = new Vector2(0.5f, 1f);
            top.sizeDelta = new Vector2(0f, 150f);
            top.anchoredPosition = Vector2.zero;

            Sprite pill = LoadSprite("pill");
            Sprite coin = LoadSprite("coin");
            Sprite gear = LoadSprite("gear");
            CreateHudChip(top, "Coin", pill, coin, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, -24f), new Vector2(168f, 64f), out coinText);
            levelText = CreateLabel(top, "Level", "第1关", 34, TextAlignmentOptions.Center);
            var levelRect = levelText.rectTransform;
            levelRect.anchorMin = levelRect.anchorMax = new Vector2(0.5f, 0.5f);
            levelRect.sizeDelta = new Vector2(220f, 70f);
            levelRect.anchoredPosition = new Vector2(0f, -20f);
            CreateHudChip(top, "Progress", pill, flowers != null && flowers.Length > 0 ? flowers[0] : null, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-196f, -24f), new Vector2(150f, 64f), out progressText);
            Button settings = CreateIconButton(top, "Settings", gear, new Vector2(1f, 0.5f), new Vector2(-24f, -24f), new Vector2(72f, 72f));
            settings.onClick.AddListener(OnSettings);

            statusText = CreateLabel(safeArea, "Status", "", 26, TextAlignmentOptions.Center);
            var statusRect = statusText.rectTransform;
            statusRect.anchorMin = new Vector2(0f, 1f);
            statusRect.anchorMax = new Vector2(1f, 1f);
            statusRect.pivot = new Vector2(0.5f, 1f);
            statusRect.sizeDelta = new Vector2(-40f, 48f);
            statusRect.anchoredPosition = new Vector2(0f, -156f);

            bottomBar = CreateRect("BottomBar", root);
            bottomBar.anchorMin = new Vector2(0f, 0f);
            bottomBar.anchorMax = new Vector2(1f, 0f);
            bottomBar.pivot = new Vector2(0.5f, 0f);
            bottomBar.sizeDelta = new Vector2(0f, 230f);
            bottomBar.anchoredPosition = Vector2.zero;
            var buttonRow = CreateRect("Buttons", bottomBar);
            buttonRow.anchorMin = new Vector2(0.5f, 1f);
            buttonRow.anchorMax = new Vector2(0.5f, 1f);
            buttonRow.pivot = new Vector2(0.5f, 1f);
            buttonRow.sizeDelta = new Vector2(560f, 150f);
            buttonRow.anchoredPosition = new Vector2(0f, -12f);

            undoButton = CreateToolButton(buttonRow, "Undo", LoadSprite("btn_undo"), new Vector2(-190f, 0f), out undoCountText);
            hintButton = CreateToolButton(buttonRow, "Hint", LoadSprite("btn_hint"), Vector2.zero, out hintCountText);
            ropeButton = CreateToolButton(buttonRow, "Rope", LoadSprite("btn_rope"), new Vector2(190f, 0f), out ropeCountText);
            undoButton.onClick.AddListener(OnUndo);
            hintButton.onClick.AddListener(OnHint);
            ropeButton.onClick.AddListener(OnExtraRope);

            flowers = new Sprite[RopeBoardView.FlowerCount];
            for (int i = 0; i < flowers.Length; i++)
                flowers[i] = LoadSprite("flower_" + i);
            hiddenFlower = LoadSprite("flower_hidden");
            potSprite = LoadSprite("pot");
            ringSprite = LoadSprite("ring");
            ropeSprite = LoadSprite("rope");
            if (ropeSprite != null && ropeSprite.texture != null)
                ropeSprite.texture.wrapMode = TextureWrapMode.Repeat;
            board = new RopeBoardView(this, playfield, flowers, hiddenFlower, potSprite, ringSprite, ropeSprite, LoadSprite("lock"), fontAsset, fontMaterial);
            BuildSettlement();
        }

        void BuildSettlement()
        {
            var root = CreateRect("WinSettlement", transform);
            Stretch(root);
            settlementRoot = root.gameObject;
            Image dim = CreateImage("Dim", root, null, new Color(0.15f, 0.1f, 0.08f, 0.55f));
            Stretch(dim.rectTransform);
            dim.raycastTarget = true;

            var card = CreateRect("Card", root);
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(460f, 320f);
            Image cardImage = card.gameObject.AddComponent<Image>();
            cardImage.sprite = LoadSprite("pill");
            cardImage.color = new Color(1f, 0.97f, 0.93f);
            cardImage.raycastTarget = true;

            TextMeshProUGUI title = CreateLabel(card, "Title", "过关", 52, TextAlignmentOptions.Center);
            var titleRect = title.rectTransform;
            titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(400f, 90f);
            titleRect.anchoredPosition = new Vector2(0f, -36f);

            var next = CreateRect("Next", card);
            next.anchorMin = next.anchorMax = new Vector2(0.5f, 0f);
            next.pivot = new Vector2(0.5f, 0f);
            next.sizeDelta = new Vector2(280f, 88f);
            next.anchoredPosition = new Vector2(0f, 36f);
            Image nextImage = next.gameObject.AddComponent<Image>();
            nextImage.sprite = LoadSprite("btn_hint");
            nextImage.color = new Color(1f, 0.78f, 0.28f);
            nextImage.preserveAspect = false;
            var nextButton = next.gameObject.AddComponent<Button>();
            nextButton.targetGraphic = nextImage;
            nextButton.onClick.AddListener(OnNextLevel);
            TextMeshProUGUI nextLabel = CreateLabel(next, "Label", "下一关", 32, TextAlignmentOptions.Center);
            nextLabel.color = new Color(0.35f, 0.2f, 0.08f);
            Stretch(nextLabel.rectTransform);
            settlementRoot.SetActive(false);
        }

        void ApplyChromeLayout()
        {
            if (playfield == null || bottomBar == null)
                return;
            float bottom = 0f;
            float top = 0f;
            Canvas host = GetComponentInParent<Canvas>();
            if (host != null && Screen.width > 0 && Screen.height > 0)
            {
                Camera cam = host.renderMode == RenderMode.ScreenSpaceOverlay ? null : host.worldCamera;
                RectTransform canvasRt = host.transform as RectTransform;
                Rect safe = Screen.safeArea;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, new Vector2(0f, safe.yMin), cam, out Vector2 localMin)
                    && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, new Vector2(0f, safe.yMax), cam, out Vector2 localMax))
                {
                    bottom = Mathf.Max(0f, localMin.y - canvasRt.rect.yMin);
                    top = Mathf.Max(0f, canvasRt.rect.yMax - localMax.y);
                }
            }

            bottomBar.sizeDelta = new Vector2(0f, 210f + bottom);
            playfield.offsetMin = new Vector2(8f, 220f + bottom);
            playfield.offsetMax = new Vector2(-8f, -(170f + top));
        }

        void LoadArt()
        {
            if (fontAsset == null)
                fontAsset = Resources.Load<TMP_FontAsset>("RopeSort/Font");
        }

        static Sprite LoadSprite(string name)
        {
            return Resources.Load<Sprite>("RopeSort/" + name);
        }

        RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = gameObject.layer;
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = gameObject.layer;
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        TextMeshProUGUI CreateLabel(Transform parent, string name, string value, float size, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null)
                text.font = fontAsset;
            if (fontMaterial != null)
                text.fontSharedMaterial = fontMaterial;
            text.text = value;
            text.fontSize = size;
            text.alignment = align;
            text.color = new Color(0.45f, 0.28f, 0.16f);
            text.raycastTarget = false;
            return text;
        }

        void CreateHudChip(Transform parent, string name, Sprite back, Sprite icon, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, out TextMeshProUGUI label)
        {
            var root = CreateRect(name, parent);
            root.anchorMin = anchorMin;
            root.anchorMax = anchorMax;
            root.pivot = new Vector2(anchorMin.x, 0.5f);
            root.sizeDelta = size;
            root.anchoredPosition = pos;
            if (back != null)
            {
                Image plate = CreateImage("Plate", root, back, Color.white);
                Stretch(plate.rectTransform);
            }
            if (icon != null)
            {
                Image mark = CreateImage("Icon", root, icon, Color.white);
                mark.preserveAspect = true;
                mark.rectTransform.anchorMin = mark.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                mark.rectTransform.pivot = new Vector2(0f, 0.5f);
                mark.rectTransform.sizeDelta = new Vector2(size.y * 0.72f, size.y * 0.72f);
                mark.rectTransform.anchoredPosition = new Vector2(8f, 0f);
            }
            label = CreateLabel(root, "Value", "0", 28, TextAlignmentOptions.MidlineRight);
            var rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(size.y * 0.7f, 0f);
            rect.offsetMax = new Vector2(-12f, 0f);
        }

        Button CreateIconButton(Transform parent, string name, Sprite icon, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var root = CreateRect(name, parent);
            root.anchorMin = root.anchorMax = anchor;
            root.pivot = new Vector2(anchor.x, 0.5f);
            root.sizeDelta = size;
            root.anchoredPosition = pos;
            Image image = root.gameObject.AddComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            image.raycastTarget = true;
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        Button CreateToolButton(Transform parent, string name, Sprite sprite, Vector2 pos, out TextMeshProUGUI count)
        {
            var root = CreateRect(name, parent);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(132f, 132f);
            root.anchoredPosition = pos;
            Image image = root.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var badge = CreateRect("Badge", root);
            badge.anchorMin = badge.anchorMax = new Vector2(1f, 0f);
            badge.pivot = new Vector2(1f, 0f);
            badge.sizeDelta = new Vector2(42f, 42f);
            badge.anchoredPosition = new Vector2(6f, -6f);
            Image badgeImage = badge.gameObject.AddComponent<Image>();
            badgeImage.color = new Color(0.96f, 0.42f, 0.38f);
            badgeImage.raycastTarget = false;
            count = CreateLabel(badge, "Count", "3", 22, TextAlignmentOptions.Center);
            count.color = Color.white;
            Stretch(count.rectTransform);
            return button;
        }
    }
}
