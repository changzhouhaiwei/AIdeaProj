using System;
using System.Collections.Generic;
using DG.Tweening;
using Game.GamePlay.ColorSort;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RopeSort
{
    public sealed class RopeBoardView
    {
        public const int FlowerCount = 8;

        static readonly Color[] Palette =
        {
            new Color(1f, 0.55f, 0.62f),
            new Color(0.35f, 0.75f, 0.38f),
            new Color(0.35f, 0.62f, 0.95f),
            new Color(0.98f, 0.55f, 0.20f),
            new Color(0.98f, 0.86f, 0.25f),
            new Color(0.30f, 0.82f, 0.78f),
            new Color(0.70f, 0.50f, 0.90f),
            new Color(0.92f, 0.32f, 0.28f)
        };

        readonly RopeGamePage page;
        readonly RectTransform root;
        readonly Sprite[] flowers;
        readonly Sprite hidden;
        readonly Sprite pot;
        readonly Sprite ring;
        readonly Sprite rope;
        readonly Sprite lockIcon;
        readonly TMP_FontAsset font;
        readonly Material fontMaterial;
        readonly List<RectTransform> columns = new List<RectTransform>();
        readonly List<RopeBeadInput> beads = new List<RopeBeadInput>();
        readonly List<Sequence> motions = new List<Sequence>();

        int hintSrc = -1;
        int motionGen;
        int hintCut = -1;
        int hintDst = -1;

        public RopeBoardView(RopeGamePage page, RectTransform root, Sprite[] flowers, Sprite hidden, Sprite pot, Sprite ring, Sprite rope, Sprite lockIcon, TMP_FontAsset font, Material fontMaterial)
        {
            this.page = page;
            this.root = root;
            this.flowers = flowers;
            this.hidden = hidden;
            this.pot = pot;
            this.ring = ring;
            this.rope = rope;
            this.lockIcon = lockIcon;
            this.font = font;
            this.fontMaterial = fontMaterial;
        }

        public void SetHint(int src, int cut, int dst)
        {
            hintSrc = src;
            hintCut = cut;
            hintDst = dst;
        }

        public void ClearHint()
        {
            hintSrc = hintCut = hintDst = -1;
        }

        public void Rebuild(ColorSortState state, ColorSortLevel level)
        {
            StopMotion();
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                GameObject child = root.GetChild(i).gameObject;
                child.SetActive(false);
                UnityEngine.Object.Destroy(child);
            }
            columns.Clear();
            beads.Clear();
            if (state == null || level == null)
                return;

            int count = state.Cols.Count;
            float width = root.rect.width;
            float height = root.rect.height;
            if (width < 2f)
                width = 704f;
            if (height < 2f)
                height = 1100f;
            int visible = 0;
            for (int i = 0; i < count; i++)
            {
                if (!state.Gone[i])
                    visible++;
            }
            if (visible <= 0)
                return;

            float colW = width / visible;
            int cap = Mathf.Max(level.Capacity, 1);
            float size = Mathf.Min(colW * 0.82f, height / (cap + 2.6f));
            size = Mathf.Max(36f, size);
            int drawn = 0;
            for (int c = 0; c < count; c++)
            {
                if (state.Gone[c])
                    continue;

                float x = -width * 0.5f + colW * (drawn + 0.5f);
                drawn++;
                var col = NewRect("Col" + c, root);
                col.anchorMin = col.anchorMax = new Vector2(0.5f, 0.5f);
                col.pivot = new Vector2(0.5f, 1f);
                col.sizeDelta = new Vector2(colW, height);
                col.anchoredPosition = new Vector2(x, height * 0.5f);
                columns.Add(col);

                float y = -size * 0.05f;
                Image ringImage = NewImage("Ring", col, ring, Color.white);
                Place(ringImage.rectTransform, 0f, y, size * 0.42f, size * 0.42f);
                y -= size * 0.55f;

                bool locked = ColorSortRules.IsLocked(state, c);
                int jar = state.Jars[c];
                Color potColor = jar < 0 ? new Color(0.78f, 0.76f, 0.72f) : Palette[Mathf.Abs(jar) % Palette.Length];
                if (c == hintDst)
                    potColor = Color.Lerp(potColor, Color.white, 0.45f);
                Image potImage = NewImage("Pot", col, pot, potColor);
                Place(potImage.rectTransform, 0f, y, size * 0.92f, size * 0.72f);
                if (locked && lockIcon != null)
                {
                    Image lockImage = NewImage("Lock", potImage.rectTransform, lockIcon, Color.white);
                    Place(lockImage.rectTransform, size * 0.28f, size * 0.16f, size * 0.34f, size * 0.34f);
                }

                y -= size * 0.78f;
                int beadCount = state.Cols[c].Count;
                float ropeLen = size * 0.92f * Mathf.Max(cap, beadCount);
                if (rope != null && ropeLen > 4f)
                {
                    Image ropeImage = NewImage("Rope", col, rope, Color.white);
                    ropeImage.preserveAspect = false;
                    ropeImage.rectTransform.anchorMin = ropeImage.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                    ropeImage.rectTransform.pivot = new Vector2(0.5f, 1f);
                    ropeImage.rectTransform.sizeDelta = new Vector2(size * 0.16f, ropeLen);
                    ropeImage.rectTransform.anchoredPosition = new Vector2(0f, y + size * 0.2f);
                    ropeImage.transform.SetAsFirstSibling();
                }

                for (int j = 0; j < beadCount; j++)
                {
                    int value = state.Cols[c][j];
                    bool mystery = ColorSortLevel.IsHidden(value);
                    int type = ColorSortLevel.TrueType(value);
                    Sprite sprite = mystery ? hidden : FlowerSprite(type);
                    Color tint = Color.white;
                    bool hinted = c == hintSrc && j >= hintCut && hintCut >= 0;
                    if (hinted)
                        tint = new Color(1f, 0.95f, 0.55f);
                    Image bead = NewImage("Bead", col, sprite, tint);
                    Place(bead.rectTransform, 0f, y, size * 0.9f, size * 0.9f);
                    if (mystery)
                        MakeText("?", bead.rectTransform, size * 0.42f, FontStyles.Bold);
                    var input = bead.gameObject.AddComponent<RopeBeadInput>();
                    input.Column = c;
                    input.Index = j;
                    input.Page = page;
                    input.Locked = locked;
                    beads.Add(input);
                    y -= size * 0.92f;
                }
            }
        }

        public int ColumnAt(Vector2 screenPoint, Camera camera)
        {
            for (int i = 0; i < columns.Count; i++)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(columns[i], screenPoint, camera))
                {
                    string name = columns[i].name;
                    if (name.StartsWith("Col") && int.TryParse(name.Substring(3), out int index))
                        return index;
                }
            }
            return -1;
        }

        public Sprite FlowerSprite(int type)
        {
            if (flowers == null || flowers.Length == 0)
                return hidden;
            int index = Mathf.Abs(type) % flowers.Length;
            return flowers[index] != null ? flowers[index] : flowers[0];
        }

        public Color FlowerTint(int type)
        {
            return Palette[Mathf.Abs(type) % Palette.Length];
        }

        public void StopMotion()
        {
            motionGen++;
            Sequence[] running = motions.ToArray();
            motions.Clear();
            for (int i = 0; i < running.Length; i++)
            {
                if (running[i] != null && running[i].IsActive())
                    running[i].Kill(false);
            }
        }

        public void PlayLevelEnter(Action onComplete)
        {
            var ids = new List<int>(columns.Count);
            for (int i = 0; i < columns.Count; i++)
            {
                if (TryColumnId(columns[i], out int id))
                    ids.Add(id);
            }
            PlayColumns(ids, 0.06f, onComplete);
        }

        public void PlayArrivals(List<int> newColumns, List<ColorSortSpawnLog> spawns, Action onComplete)
        {
            var entering = new List<int>();
            if (newColumns != null)
            {
                for (int i = 0; i < newColumns.Count; i++)
                {
                    if (!entering.Contains(newColumns[i]))
                        entering.Add(newColumns[i]);
                }
            }

            var beadCols = new List<int>();
            var beadCounts = new List<int>();
            if (spawns != null)
            {
                for (int i = 0; i < spawns.Count; i++)
                {
                    ColorSortSpawnLog log = spawns[i];
                    if (log == null || log.Types == null || log.Types.Count == 0 || entering.Contains(log.Col))
                        continue;
                    int index = beadCols.IndexOf(log.Col);
                    if (index < 0)
                    {
                        beadCols.Add(log.Col);
                        beadCounts.Add(log.Types.Count);
                    }
                    else
                    {
                        beadCounts[index] += log.Types.Count;
                    }
                }
            }

            int gen = ++motionGen;
            if (entering.Count == 0 && beadCols.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            int pending = entering.Count + beadCols.Count;
            Action done = () =>
            {
                if (gen != motionGen)
                    return;
                pending--;
                if (pending <= 0)
                    onComplete?.Invoke();
            };

            float stagger = entering.Count > 1 ? 0.08f : 0f;
            for (int i = 0; i < entering.Count; i++)
                TweenColumn(ColumnOf(entering[i]), i * stagger, gen, done);
            for (int i = 0; i < beadCols.Count; i++)
                TweenNewBeads(ColumnOf(beadCols[i]), beadCounts[i], 0.12f, gen, done);
        }

        void PlayColumns(List<int> ids, float stagger, Action onComplete)
        {
            int gen = ++motionGen;
            if (ids == null || ids.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            int pending = ids.Count;
            Action done = () =>
            {
                if (gen != motionGen)
                    return;
                pending--;
                if (pending <= 0)
                    onComplete?.Invoke();
            };
            for (int i = 0; i < ids.Count; i++)
                TweenColumn(ColumnOf(ids[i]), i * stagger, gen, done);
        }

        void TweenColumn(RectTransform col, float delay, int gen, Action done)
        {
            if (col == null)
            {
                if (gen == motionGen)
                    done?.Invoke();
                return;
            }

            var heads = new List<RectTransform>();
            RectTransform ropeRt = null;
            var beadRts = new List<RectTransform>();
            var beadImages = new List<Image>();
            for (int i = 0; i < col.childCount; i++)
            {
                var child = (RectTransform)col.GetChild(i);
                if (child.name == "Ring" || child.name == "Pot")
                    heads.Add(child);
                else if (child.name == "Rope")
                    ropeRt = child;
                else if (child.name == "Bead")
                {
                    beadRts.Add(child);
                    Image image = child.GetComponent<Image>();
                    if (image != null)
                    {
                        image.raycastTarget = false;
                        beadImages.Add(image);
                    }
                }
            }

            const float lift = 300f;
            for (int i = 0; i < heads.Count; i++)
                heads[i].anchoredPosition += new Vector2(0f, lift);
            if (ropeRt != null)
                ropeRt.localScale = new Vector3(1f, 0.02f, 1f);
            for (int i = 0; i < beadRts.Count; i++)
                beadRts[i].localScale = Vector3.zero;

            var seq = DOTween.Sequence();
            motions.Add(seq);
            seq.AppendInterval(delay);
            if (heads.Count > 0)
            {
                seq.Append(heads[0].DOAnchorPosY(heads[0].anchoredPosition.y - lift, 0.42f).SetEase(Ease.OutBack));
                for (int i = 1; i < heads.Count; i++)
                {
                    float restY = heads[i].anchoredPosition.y - lift;
                    seq.Join(heads[i].DOAnchorPosY(restY, 0.42f).SetEase(Ease.OutBack));
                }
            }
            else
            {
                seq.AppendInterval(0.42f);
            }

            if (ropeRt != null)
                seq.Insert(delay + 0.16f, ropeRt.DOScaleY(1f, 0.28f).SetEase(Ease.OutCubic));
            float beadAt = delay + 0.3f;
            for (int i = 0; i < beadRts.Count; i++)
                seq.Insert(beadAt + i * 0.045f, beadRts[i].DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack));

            seq.OnComplete(() =>
            {
                motions.Remove(seq);
                for (int i = 0; i < beadImages.Count; i++)
                {
                    if (beadImages[i] != null)
                        beadImages[i].raycastTarget = true;
                }
                if (gen == motionGen)
                    done?.Invoke();
            });
        }

        void TweenNewBeads(RectTransform col, int count, float delay, int gen, Action done)
        {
            if (col == null || count <= 0)
            {
                if (gen == motionGen)
                    done?.Invoke();
                return;
            }

            var beadRts = new List<RectTransform>();
            for (int i = 0; i < col.childCount; i++)
            {
                var child = (RectTransform)col.GetChild(i);
                if (child.name == "Bead")
                    beadRts.Add(child);
            }

            int start = Mathf.Max(0, beadRts.Count - count);
            if (start >= beadRts.Count)
            {
                if (gen == motionGen)
                    done?.Invoke();
                return;
            }

            var seq = DOTween.Sequence();
            motions.Add(seq);
            seq.AppendInterval(delay + (beadRts.Count - start) * 0.05f + 0.28f);
            for (int i = start; i < beadRts.Count; i++)
            {
                RectTransform bead = beadRts[i];
                Vector2 rest = bead.anchoredPosition;
                bead.anchoredPosition = rest + new Vector2(0f, 64f);
                bead.localScale = Vector3.zero;
                Image image = bead.GetComponent<Image>();
                if (image != null)
                    image.raycastTarget = false;
                float at = delay + (i - start) * 0.05f;
                int captured = i;
                seq.Insert(at, bead.DOAnchorPos(rest, 0.28f).SetEase(Ease.OutBack));
                seq.Insert(at, bead.DOScale(Vector3.one, 0.24f).SetEase(Ease.OutBack)
                    .OnComplete(() =>
                    {
                        if (beadRts[captured] == null)
                            return;
                        Image mark = beadRts[captured].GetComponent<Image>();
                        if (mark != null)
                            mark.raycastTarget = true;
                    }));
            }

            seq.OnComplete(() =>
            {
                motions.Remove(seq);
                if (gen == motionGen)
                    done?.Invoke();
            });
        }

        RectTransform ColumnOf(int index)
        {
            for (int i = 0; i < columns.Count; i++)
            {
                if (TryColumnId(columns[i], out int id) && id == index)
                    return columns[i];
            }
            return null;
        }

        static bool TryColumnId(RectTransform column, out int id)
        {
            id = -1;
            if (column == null)
                return false;
            string name = column.name;
            return name.StartsWith("Col") && int.TryParse(name.Substring(3), out id);
        }

        RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        Image NewImage(string name, Transform parent, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = true;
            return image;
        }

        static void Place(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(w, h);
            rect.anchoredPosition = new Vector2(x, y);
        }

        void MakeText(string value, RectTransform parent, float size, FontStyles style)
        {
            var go = new GameObject("Mark", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var text = go.AddComponent<TextMeshProUGUI>();
            if (font != null)
                text.font = font;
            if (fontMaterial != null)
                text.fontSharedMaterial = fontMaterial;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.2f, 0.16f, 0.18f);
            text.raycastTarget = false;
        }
    }

    public sealed class RopeBeadInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public int Column;
        public int Index;
        public RopeGamePage Page;
        public bool Locked;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!Locked)
                Page.BeginDrag(Column, Index, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Page.Drag(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Page.EndDrag(eventData);
        }
    }
}
