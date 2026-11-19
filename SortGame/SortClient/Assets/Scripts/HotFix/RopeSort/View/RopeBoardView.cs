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
        readonly List<GameObject> flying = new List<GameObject>();

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
            Rebuild(state, level, null, null);
        }

        public void Rebuild(ColorSortState state, ColorSortLevel level, List<int> order, List<ColorSortDepositLog> hold)
        {
            StopMotion();
            ClearColumns();
            if (state == null || level == null)
                return;

            List<int> shown = BuildShown(state, order, hold);
            if (shown.Count == 0)
                return;

            int cap = Mathf.Max(level.Capacity, 1);
            BoardMetrics(shown.Count, cap, out float width, out float height, out float colW, out float size);
            for (int slot = 0; slot < shown.Count; slot++)
            {
                int c = shown[slot];
                float x = -width * 0.5f + colW * (slot + 0.5f);
                CreateColumn(state, c, x, colW, height, size, cap, FindHold(hold, c));
            }
        }

        public void ConcealSpawns(List<ColorSortSpawnLog> spawns, List<ColorSortDepositLog> deposits)
        {
            var skip = new HashSet<int>();
            if (deposits != null)
            {
                for (int i = 0; i < deposits.Count; i++)
                {
                    if (deposits[i] != null)
                        skip.Add(deposits[i].Col);
                }
            }

            GatherSpawnCounts(spawns, skip, out List<int> cols, out List<int> counts);
            for (int i = 0; i < cols.Count; i++)
                ConcealTrailing(ColumnOf(cols[i]), counts[i]);
        }

        public void PlayCollect(List<ColorSortDepositLog> deposits, RectTransform progressIcon, Action<ColorSortDepositLog> onFlowers, Action onComplete)
        {
            int gen = ++motionGen;
            int pending = 0;
            if (deposits != null)
            {
                for (int i = 0; i < deposits.Count; i++)
                {
                    if (deposits[i] != null)
                        pending++;
                }
            }
            if (pending == 0)
            {
                onComplete?.Invoke();
                return;
            }

            var grouped = new List<List<ColorSortDepositLog>>();
            var seen = new List<int>();
            for (int i = 0; i < deposits.Count; i++)
            {
                ColorSortDepositLog deposit = deposits[i];
                if (deposit == null)
                    continue;
                int index = seen.IndexOf(deposit.Col);
                if (index < 0)
                {
                    seen.Add(deposit.Col);
                    grouped.Add(new List<ColorSortDepositLog>());
                    index = grouped.Count - 1;
                }
                grouped[index].Add(deposit);
            }

            pending = grouped.Count;
            Action done = () =>
            {
                if (gen != motionGen)
                    return;
                pending--;
                if (pending <= 0)
                    onComplete?.Invoke();
            };

            for (int i = 0; i < grouped.Count; i++)
            {
                List<ColorSortDepositLog> batch = grouped[i];
                PlayOneCollect(batch[0], progressIcon, 0.1f + i * 0.08f, gen, _ =>
                {
                    for (int k = 0; k < batch.Count; k++)
                        onFlowers?.Invoke(batch[k]);
                }, done);
            }
        }

        public void PlayShiftAndArrive(List<int> order, List<int> arriving, List<ColorSortSpawnLog> spawns, ColorSortState state, ColorSortLevel level, Action onComplete)
        {
            int gen = ++motionGen;
            if (state == null || level == null)
            {
                onComplete?.Invoke();
                return;
            }

            if (arriving != null)
            {
                for (int i = 0; i < arriving.Count; i++)
                {
                    RectTransform stale = ColumnOf(arriving[i]);
                    if (stale != null)
                        ReleaseColumn(stale);
                }
            }

            int visible = 0;
            if (order != null)
            {
                for (int i = 0; i < order.Count; i++)
                {
                    int id = order[i];
                    if (id >= 0 && id < state.Cols.Count && !state.Gone[id])
                        visible++;
                }
            }
            if (visible <= 0)
            {
                onComplete?.Invoke();
                return;
            }

            int cap = Mathf.Max(level.Capacity, 1);
            BoardMetrics(visible, cap, out float width, out float height, out float colW, out float size);
            float beadSize = SampleBeadSize();
            if (beadSize > 8f)
                size = beadSize;

            var moving = new List<RectTransform>();
            var targets = new List<float>();
            var spawnIds = new List<int>();
            var spawnX = new List<float>();
            int slot = 0;
            for (int i = 0; order != null && i < order.Count; i++)
            {
                int id = order[i];
                if (id < 0 || id >= state.Cols.Count || state.Gone[id])
                    continue;
                float x = -width * 0.5f + colW * (slot + 0.5f);
                slot++;
                if (ContainsId(arriving, id))
                {
                    spawnIds.Add(id);
                    spawnX.Add(x);
                    continue;
                }

                RectTransform col = ColumnOf(id);
                if (col == null)
                    continue;
                col.sizeDelta = new Vector2(colW, height);
                moving.Add(col);
                targets.Add(x);
            }

            void Finish()
            {
                if (gen != motionGen)
                    return;
                for (int i = 0; i < spawnIds.Count; i++)
                    CreateColumn(state, spawnIds[i], spawnX[i], colW, height, size, cap, null);
                PlayArrivals(spawnIds, spawns, onComplete);
            }

            bool needsMove = false;
            for (int i = 0; i < moving.Count; i++)
            {
                if (Mathf.Abs(moving[i].anchoredPosition.x - targets[i]) > 0.5f)
                {
                    needsMove = true;
                    break;
                }
            }
            if (moving.Count == 0 || !needsMove)
            {
                Finish();
                return;
            }

            int pending = moving.Count;
            for (int i = 0; i < moving.Count; i++)
            {
                RectTransform col = moving[i];
                float x = targets[i];
                var seq = DOTween.Sequence();
                motions.Add(seq);
                seq.Append(col.DOAnchorPosX(x, 0.32f).SetEase(Ease.OutCubic));
                seq.OnComplete(() =>
                {
                    motions.Remove(seq);
                    if (gen != motionGen)
                        return;
                    pending--;
                    if (pending <= 0)
                        Finish();
                });
            }
        }

        void PlayOneCollect(ColorSortDepositLog deposit, RectTransform progressIcon, float delay, int gen, Action<ColorSortDepositLog> onFlowers, Action onDone)
        {
            RectTransform col = ColumnOf(deposit.Col);
            if (col == null)
            {
                if (gen == motionGen)
                {
                    onFlowers?.Invoke(deposit);
                    onDone?.Invoke();
                }
                return;
            }

            List<RectTransform> beadsOnRope = BeadsOf(col);
            var ordered = new List<RectTransform>();
            for (int i = beadsOnRope.Count - 1; i >= 0; i--)
                ordered.Add(beadsOnRope[i]);
            if (ordered.Count == 0)
            {
                if (gen == motionGen)
                    onFlowers?.Invoke(deposit);
                RiseColumn(col, gen, onDone);
                return;
            }

            Vector3 target = progressIcon != null
                ? progressIcon.TransformPoint(progressIcon.rect.center)
                : col.position + new Vector3(0f, 320f, 0f);
            const float stagger = 0.05f;
            const float fly = 0.34f;
            var seq = DOTween.Sequence();
            motions.Add(seq);
            for (int i = 0; i < ordered.Count; i++)
            {
                RectTransform bead = ordered[i];
                bead.SetParent(page.transform, true);
                bead.SetAsLastSibling();
                bead.gameObject.layer = page.gameObject.layer;
                flying.Add(bead.gameObject);
                Image image = bead.GetComponent<Image>();
                if (image != null)
                    image.raycastTarget = false;
                float at = delay + i * stagger;
                seq.Insert(at, bead.DOMove(target, fly).SetEase(Ease.InCubic));
                seq.Insert(at, bead.DOScale(0.12f, fly).SetEase(Ease.InQuad));
            }

            seq.OnComplete(() =>
            {
                motions.Remove(seq);
                ClearFlown(ordered);
                if (gen != motionGen)
                    return;
                onFlowers?.Invoke(deposit);
                RiseColumn(col, gen, onDone);
            });
        }

        void RiseColumn(RectTransform col, int gen, Action onDone)
        {
            if (col == null)
            {
                if (gen == motionGen)
                    onDone?.Invoke();
                return;
            }

            CanvasGroup group = col.GetComponent<CanvasGroup>();
            if (group == null)
                group = col.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            float distance = Mathf.Max(col.rect.height, 480f);
            float y = col.anchoredPosition.y + distance;
            var seq = DOTween.Sequence();
            motions.Add(seq);
            seq.Append(col.DOAnchorPosY(y, 0.42f).SetEase(Ease.InCubic));
            seq.Join(group.DOFade(0f, 0.34f));
            seq.OnComplete(() =>
            {
                motions.Remove(seq);
                ReleaseColumn(col);
                if (gen == motionGen)
                    onDone?.Invoke();
            });
        }

        void ClearColumns()
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                GameObject child = root.GetChild(i).gameObject;
                child.SetActive(false);
                UnityEngine.Object.Destroy(child);
            }
            columns.Clear();
            beads.Clear();
        }

        List<int> BuildShown(ColorSortState state, List<int> order, List<ColorSortDepositLog> hold)
        {
            int count = state.Cols.Count;
            var shown = new List<int>();
            if (order != null && order.Count > 0)
            {
                for (int i = 0; i < order.Count; i++)
                {
                    int id = order[i];
                    if (id < 0 || id >= count || ContainsId(shown, id))
                        continue;
                    if (!state.Gone[id] || FindHold(hold, id) != null)
                        shown.Add(id);
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    if (!state.Gone[i] || FindHold(hold, i) != null)
                        shown.Add(i);
                }
            }

            for (int i = 0; i < count; i++)
            {
                if (!state.Gone[i] && !ContainsId(shown, i))
                    shown.Add(i);
            }

            if (hold != null)
            {
                for (int i = 0; i < hold.Count; i++)
                {
                    ColorSortDepositLog log = hold[i];
                    if (log == null || log.Col < 0 || log.Col >= count || ContainsId(shown, log.Col))
                        continue;
                    shown.Add(log.Col);
                }
            }
            return shown;
        }

        RectTransform CreateColumn(ColorSortState state, int index, float x, float colW, float height, float size, int cap, ColorSortDepositLog pose)
        {
            IList<int> beadValues;
            int jar;
            bool locked;
            if (pose != null)
            {
                var posed = new List<int>(Mathf.Max(0, pose.Q));
                for (int i = 0; i < pose.Q; i++)
                    posed.Add(pose.BeadType);
                beadValues = posed;
                jar = pose.BeadType;
                locked = false;
            }
            else
            {
                beadValues = state.Cols[index];
                jar = state.Jars[index];
                locked = ColorSortRules.IsLocked(state, index);
            }

            var col = NewRect("Col" + index, root);
            col.anchorMin = col.anchorMax = new Vector2(0.5f, 0.5f);
            col.pivot = new Vector2(0.5f, 1f);
            col.sizeDelta = new Vector2(colW, height);
            col.anchoredPosition = new Vector2(x, height * 0.5f);
            columns.Add(col);

            float y = -size * 0.05f;
            Image ringImage = NewImage("Ring", col, ring, Color.white);
            Place(ringImage.rectTransform, 0f, y, size * 0.42f, size * 0.42f);
            y -= size * 0.55f;

            Color potColor = jar < 0 ? new Color(0.78f, 0.76f, 0.72f) : Palette[Mathf.Abs(jar) % Palette.Length];
            if (pose == null && index == hintDst)
                potColor = Color.Lerp(potColor, Color.white, 0.45f);
            Image potImage = NewImage("Pot", col, pot, potColor);
            Place(potImage.rectTransform, 0f, y, size * 0.92f, size * 0.72f);
            if (locked && lockIcon != null)
            {
                Image lockImage = NewImage("Lock", potImage.rectTransform, lockIcon, Color.white);
                Place(lockImage.rectTransform, size * 0.28f, size * 0.16f, size * 0.34f, size * 0.34f);
            }

            y -= size * 0.78f;
            int beadCount = beadValues.Count;
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
                int value = beadValues[j];
                bool mystery = ColorSortLevel.IsHidden(value);
                int type = ColorSortLevel.TrueType(value);
                Sprite sprite = mystery ? hidden : FlowerSprite(type);
                Color tint = Color.white;
                bool hinted = pose == null && index == hintSrc && j >= hintCut && hintCut >= 0;
                if (hinted)
                    tint = new Color(1f, 0.95f, 0.55f);
                Image bead = NewImage("Bead", col, sprite, tint);
                Place(bead.rectTransform, 0f, y, size * 0.9f, size * 0.9f);
                if (mystery)
                    MakeText("?", bead.rectTransform, size * 0.42f, FontStyles.Bold);
                var input = bead.gameObject.AddComponent<RopeBeadInput>();
                input.Column = index;
                input.Index = j;
                input.Page = page;
                input.Locked = locked || pose != null;
                if (pose != null)
                    bead.raycastTarget = false;
                beads.Add(input);
                y -= size * 0.92f;
            }
            return col;
        }

        void BoardMetrics(int visible, int cap, out float width, out float height, out float colW, out float size)
        {
            width = root.rect.width;
            height = root.rect.height;
            if (width < 2f)
                width = 704f;
            if (height < 2f)
                height = 1100f;
            int count = Mathf.Max(visible, 1);
            colW = width / count;
            size = Mathf.Min(colW * 0.82f, height / (cap + 2.6f));
            size = Mathf.Max(36f, size);
        }

        float SampleBeadSize()
        {
            for (int i = 0; i < columns.Count; i++)
            {
                List<RectTransform> found = BeadsOf(columns[i]);
                if (found.Count > 0 && found[0] != null)
                    return found[0].sizeDelta.x / 0.9f;
            }
            return 0f;
        }

        static List<RectTransform> BeadsOf(RectTransform col)
        {
            var list = new List<RectTransform>();
            if (col == null)
                return list;
            for (int i = 0; i < col.childCount; i++)
            {
                var child = (RectTransform)col.GetChild(i);
                if (child.name == "Bead")
                    list.Add(child);
            }
            return list;
        }

        void ConcealTrailing(RectTransform col, int count)
        {
            if (col == null || count <= 0)
                return;
            List<RectTransform> found = BeadsOf(col);
            int start = Mathf.Max(0, found.Count - count);
            for (int i = start; i < found.Count; i++)
            {
                found[i].localScale = Vector3.zero;
                Image image = found[i].GetComponent<Image>();
                if (image != null)
                    image.raycastTarget = false;
            }
        }

        static void GatherSpawnCounts(List<ColorSortSpawnLog> spawns, HashSet<int> skip, out List<int> cols, out List<int> counts)
        {
            cols = new List<int>();
            counts = new List<int>();
            if (spawns == null)
                return;
            for (int i = 0; i < spawns.Count; i++)
            {
                ColorSortSpawnLog log = spawns[i];
                if (log == null || log.Types == null || log.Types.Count == 0)
                    continue;
                if (skip != null && skip.Contains(log.Col))
                    continue;
                int index = cols.IndexOf(log.Col);
                if (index < 0)
                {
                    cols.Add(log.Col);
                    counts.Add(log.Types.Count);
                }
                else
                {
                    counts[index] += log.Types.Count;
                }
            }
        }

        void ClearFlown(List<RectTransform> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                RectTransform item = items[i];
                if (item == null)
                    continue;
                flying.Remove(item.gameObject);
                UnityEngine.Object.Destroy(item.gameObject);
            }
        }

        void ReleaseColumn(RectTransform col)
        {
            if (col == null)
                return;
            columns.Remove(col);
            UnityEngine.Object.Destroy(col.gameObject);
        }

        static ColorSortDepositLog FindHold(List<ColorSortDepositLog> hold, int col)
        {
            if (hold == null)
                return null;
            for (int i = 0; i < hold.Count; i++)
            {
                ColorSortDepositLog log = hold[i];
                if (log != null && log.Col == col)
                    return log;
            }
            return null;
        }

        static bool ContainsId(List<int> ids, int id)
        {
            if (ids == null)
                return false;
            for (int i = 0; i < ids.Count; i++)
            {
                if (ids[i] == id)
                    return true;
            }
            return false;
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
            for (int i = flying.Count - 1; i >= 0; i--)
            {
                if (flying[i] != null)
                    UnityEngine.Object.Destroy(flying[i]);
            }
            flying.Clear();
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
