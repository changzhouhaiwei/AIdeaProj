using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace PotatoSiege
{
    public class PotatoUI : MonoBehaviour
    {
        Canvas _canvas;
        Text _hudWave, _hudHp, _hudGold, _hudXp, _hudSkill, _toast;
        GameObject _classPanel, _shopPanel, _endPanel;
        readonly List<Button> _shopButtons = new List<Button>();
        readonly List<Text> _shopLabels = new List<Text>();
        float _toastTimer;

        public void Build()
        {
            var es = FindObjectOfType<EventSystem>();
            if (es == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
            }

            var canvasGo = new GameObject("PotatoUI");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1280, 720);
            canvasGo.AddComponent<GraphicRaycaster>();

            _hudWave = MakeText(canvasGo.transform, "HudWave", new Vector2(20, -20), 22, TextAnchor.UpperLeft);
            _hudHp = MakeText(canvasGo.transform, "HudHp", new Vector2(20, -50), 20, TextAnchor.UpperLeft);
            _hudGold = MakeText(canvasGo.transform, "HudGold", new Vector2(20, -80), 20, TextAnchor.UpperLeft);
            _hudXp = MakeText(canvasGo.transform, "HudXp", new Vector2(20, -110), 18, TextAnchor.UpperLeft);
            _hudSkill = MakeText(canvasGo.transform, "HudSkill", new Vector2(20, -140), 18, TextAnchor.UpperLeft);
            _toast = MakeText(canvasGo.transform, "Toast", new Vector2(0, 80), 24, TextAnchor.LowerCenter);
            _toast.alignment = TextAnchor.MiddleCenter;
            var rt = _toast.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0, 80);

            BuildClassSelect(canvasGo.transform);
            BuildShop(canvasGo.transform);
            BuildEnd(canvasGo.transform);
            ShowShop(false);
            ShowEnd(false, true);
            ShowClassSelect(true);
        }

        void BuildClassSelect(Transform parent)
        {
            _classPanel = MakePanel(parent, "ClassSelect", new Color(0.08f, 0.1f, 0.14f, 0.95f));
            var title = MakeText(_classPanel.transform, "Title", new Vector2(0, -20), 32, TextAnchor.UpperCenter);
            title.text = "土豆围城 · 选择职业";
            title.alignment = TextAnchor.MiddleCenter;
            var tr = title.GetComponent<RectTransform>();
            tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 1f);
            tr.anchoredPosition = new Vector2(0, -30);

            var hint = MakeText(_classPanel.transform, "Hint", new Vector2(0, -70), 16, TextAnchor.UpperCenter);
            hint.text = "WASD 移动 · 自动射击 · 空格/Q 主动技 · 右键朝向移动";
            hint.alignment = TextAnchor.MiddleCenter;
            var hr = hint.GetComponent<RectTransform>();
            hr.anchorMin = hr.anchorMax = new Vector2(0.5f, 1f);
            hr.anchoredPosition = new Vector2(0, -70);

            var grid = new GameObject("Grid");
            grid.transform.SetParent(_classPanel.transform, false);
            var grt = grid.AddComponent<RectTransform>();
            grt.anchorMin = new Vector2(0.05f, 0.08f);
            grt.anchorMax = new Vector2(0.95f, 0.82f);
            grt.offsetMin = grt.offsetMax = Vector2.zero;
            var layout = grid.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(220, 90);
            layout.spacing = new Vector2(12, 12);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 5;
            layout.childAlignment = TextAnchor.MiddleCenter;

            foreach (var c in ClassDatabase.All)
            {
                var local = c;
                var btn = MakeButton(grid.transform, local.displayName + "\n" + local.oneLiner, () =>
                {
                    ShowClassSelect(false);
                    GameSession.I.StartWithClass(local.id);
                });
                var colors = btn.colors;
                colors.normalColor = Color.Lerp(local.tint, Color.white, 0.35f);
                btn.colors = colors;
            }
        }

        void BuildShop(Transform parent)
        {
            _shopPanel = MakePanel(parent, "Shop", new Color(0.05f, 0.07f, 0.1f, 0.92f));
            var title = MakeText(_shopPanel.transform, "ShopTitle", new Vector2(0, -20), 28, TextAnchor.UpperCenter);
            title.text = "波次商店";
            title.alignment = TextAnchor.MiddleCenter;
            var tr = title.GetComponent<RectTransform>();
            tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 1f);
            tr.anchoredPosition = new Vector2(0, -24);

            var row = new GameObject("Offers");
            row.transform.SetParent(_shopPanel.transform, false);
            var rrt = row.AddComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0.05f, 0.25f);
            rrt.anchorMax = new Vector2(0.95f, 0.85f);
            rrt.offsetMin = rrt.offsetMax = Vector2.zero;
            var grid = row.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(260, 70);
            grid.spacing = new Vector2(10, 10);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;

            _shopButtons.Clear();
            _shopLabels.Clear();
            for (int i = 0; i < 8; i++)
            {
                int idx = i;
                var btn = MakeButton(row.transform, "商品", () => GameSession.I.TryBuy(idx));
                _shopButtons.Add(btn);
                _shopLabels.Add(btn.GetComponentInChildren<Text>());
            }

            var next = MakeButton(_shopPanel.transform, "进入下一波", () => GameSession.I.NextWaveFromShop());
            PlaceBottom(next.GetComponent<RectTransform>(), new Vector2(120, 50), new Vector2(0.7f, 0.08f));

            var reroll = MakeButton(_shopPanel.transform, "重随", () => GameSession.I.TryReroll());
            PlaceBottom(reroll.GetComponent<RectTransform>(), new Vector2(120, 50), new Vector2(0.3f, 0.08f));
            reroll.name = "RerollBtn";

            var gamble = MakeButton(_shopPanel.transform, "赌一把", () => GameSession.I.TryGamble());
            PlaceBottom(gamble.GetComponent<RectTransform>(), new Vector2(120, 50), new Vector2(0.5f, 0.08f));
            gamble.name = "GambleBtn";
        }

        void BuildEnd(Transform parent)
        {
            _endPanel = MakePanel(parent, "End", new Color(0.02f, 0.02f, 0.05f, 0.9f));
            var title = MakeText(_endPanel.transform, "EndTitle", Vector2.zero, 40, TextAnchor.MiddleCenter);
            title.text = "结束";
            title.alignment = TextAnchor.MiddleCenter;
            var tr = title.GetComponent<RectTransform>();
            tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0.6f);
            tr.sizeDelta = new Vector2(600, 80);

            var again = MakeButton(_endPanel.transform, "再来一局", () => GameSession.I.RestartToSelect());
            PlaceBottom(again.GetComponent<RectTransform>(), new Vector2(180, 55), new Vector2(0.5f, 0.35f));
        }

        static void PlaceBottom(RectTransform rt, Vector2 size, Vector2 anchor)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
        }

        public void ShowClassSelect(bool on)
        {
            if (_classPanel != null) _classPanel.SetActive(on);
        }

        public void ShowShop(bool on)
        {
            if (_shopPanel != null) _shopPanel.SetActive(on);
            if (on) RefreshShop();
        }

        public void ShowEnd(bool victory, bool hide = false)
        {
            if (_endPanel == null) return;
            if (hide) { _endPanel.SetActive(false); return; }
            _endPanel.SetActive(true);
            var t = _endPanel.transform.Find("EndTitle")?.GetComponent<Text>();
            if (t != null)
            {
                var s = GameSession.I;
                t.text = victory
                    ? $"胜利！击杀 {s.Kills} · 金钱 {s.Gold}"
                    : $"阵亡于第 {s.Wave} 波 · 击杀 {s.Kills}";
            }
        }

        public void RefreshAll()
        {
            RefreshHud();
            if (_shopPanel != null && _shopPanel.activeSelf) RefreshShop();
        }

        public void RefreshHud()
        {
            var s = GameSession.I;
            if (s == null || _hudWave == null) return;
            if (s.Phase == GamePhase.ClassSelect)
            {
                _hudWave.text = "土豆围城";
                _hudHp.text = "";
                _hudGold.text = "";
                _hudXp.text = "";
                return;
            }

            _hudWave.text = s.Phase == GamePhase.Shop
                ? $"商店 · 下一波 {s.Wave + 1}/{PSConst.MaxWaves}"
                : $"第 {s.Wave}/{PSConst.MaxWaves} 波  剩余 {Mathf.CeilToInt(Mathf.Max(0, s.WaveTimer))}s";

            if (s.Player != null)
            {
                float sh = s.Player.Shield;
                _hudHp.text = sh > 0
                    ? $"生命 {s.Player.CurrentHp:0}/{s.Stats.maxHp:0}  盾 {sh:0}"
                    : $"生命 {s.Player.CurrentHp:0}/{s.Stats.maxHp:0}";
            }
            _hudGold.text = $"金钱 {s.Gold}";
            _hudXp.text = $"Lv{s.Level}  XP {s.Xp:0}/{CombatMath.XpToNext(s.Level)}";
        }

        public void SetSkillCd(float cd, float max)
        {
            if (_hudSkill == null) return;
            if (max >= 900f) { _hudSkill.text = "主动：商店技/无"; return; }
            _hudSkill.text = cd > 0 ? $"主动技 CD {cd:0.0}s  (空格/Q)" : "主动技 就绪 (空格/Q)";
        }

        void RefreshShop()
        {
            var s = GameSession.I;
            for (int i = 0; i < _shopButtons.Count; i++)
            {
                if (i >= s.ShopOffers.Count)
                {
                    _shopButtons[i].gameObject.SetActive(false);
                    continue;
                }
                _shopButtons[i].gameObject.SetActive(true);
                var o = s.ShopOffers[i];
                string mark = o.sold ? "[已购] " : "";
                _shopLabels[i].text = $"{mark}{o.Title}\n{o.Sub}\n$ {o.price}";
                _shopButtons[i].interactable = !o.sold && s.Gold >= o.price;
            }

            var reroll = _shopPanel.transform.Find("RerollBtn")?.GetComponentInChildren<Text>();
            if (reroll != null) reroll.text = $"重随 (${s.RerollCost()})";
            var gambleBtn = _shopPanel.transform.Find("GambleBtn")?.GetComponent<Button>();
            if (gambleBtn != null)
            {
                bool show = s.ClassId == ClassId.Gambler;
                gambleBtn.gameObject.SetActive(show);
                gambleBtn.interactable = show && !s.GambleUsedThisWave;
            }
        }

        public void Toast(string msg)
        {
            if (_toast == null) return;
            _toast.text = msg;
            _toastTimer = 2.2f;
        }

        void Update()
        {
            if (_toastTimer > 0)
            {
                _toastTimer -= Time.deltaTime;
                if (_toastTimer <= 0 && _toast != null) _toast.text = "";
            }
        }

        static GameObject MakePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return go;
        }

        static Font UiFont()
        {
            var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null)
                f = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Arial", "Segoe UI" }, 16);
            return f;
        }

        static Text MakeText(Transform parent, string name, Vector2 anchored, int size, TextAnchor anchor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = UiFont();
            t.fontSize = size;
            t.color = Color.white;
            t.alignment = TextAnchor.UpperLeft;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var rt = t.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchored;
            rt.sizeDelta = new Vector2(700, 40);
            return t;
        }

        static Button MakeButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Btn");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.25f, 0.35f, 1f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 60);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var t = textGo.AddComponent<Text>();
            t.font = UiFont();
            t.fontSize = 14;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            t.text = label;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var trt = t.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(6, 4);
            trt.offsetMax = new Vector2(-6, -4);
            return btn;
        }
    }
}
