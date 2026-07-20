using System.Collections.Generic;
using UnityEngine;

namespace PotatoSiege
{
    public class GameSession : MonoBehaviour
    {
        public static GameSession I { get; private set; }

        public GamePhase Phase { get; private set; } = GamePhase.ClassSelect;
        public ClassId ClassId { get; private set; }
        public StatBlock Stats { get; private set; } = new StatBlock();
        public PlayerController Player { get; private set; }
        public PotatoUI Ui { get; private set; }

        public int Wave { get; private set; } = 1;
        public float WaveTimer { get; private set; }
        public int Gold { get; private set; }
        public int Level { get; private set; } = 1;
        public float Xp { get; private set; }
        public int Kills { get; private set; }
        public int RerollCountThisWave { get; set; }

        public readonly List<ShopOffer> ShopOffers = new List<ShopOffer>();
        public bool GambleUsedThisWave;

        WaveDirector _waves;
        Transform _world;

        void Awake()
        {
            I = this;
            ContentDatabase.Ensure();
        }

        public void BindUi(PotatoUI ui) => Ui = ui;

        public void StartWithClass(ClassId id)
        {
            ClassId = id;
            var def = ClassDatabase.Get(id);
            Stats = def.stats.Clone();
            Level = 1;
            Xp = 0;
            Gold = 15;
            Kills = 0;
            Wave = 1;
            GambleUsedThisWave = false;
            RerollCountThisWave = 0;

            if (Player != null) Destroy(Player.gameObject);
            var go = new GameObject("Player");
            go.transform.SetParent(_world, false);
            go.transform.position = Vector3.zero;
            Player = go.AddComponent<PlayerController>();
            Player.Init(def);

            Phase = GamePhase.Wave;
            BeginWave();
            Ui?.RefreshAll();
        }

        public void SetWorldRoot(Transform world)
        {
            _world = world;
            _waves = world.gameObject.AddComponent<WaveDirector>();
            _waves.Init(this);
        }

        public void BeginWave()
        {
            Phase = GamePhase.Wave;
            WaveTimer = PSConst.WaveTime;
            RerollCountThisWave = 0;
            GambleUsedThisWave = false;
            Player?.ResetWaveFlags();
            // clear enemies
            for (int i = EnemyBrain.All.Count - 1; i >= 0; i--)
            {
                if (EnemyBrain.All[i] != null) Destroy(EnemyBrain.All[i].gameObject);
            }
            _waves.StartWave(Wave, Player.transform);
            Ui?.ShowShop(false);
            Ui?.RefreshAll();
        }

        void Update()
        {
            if (Phase != GamePhase.Wave) return;
            WaveTimer -= Time.deltaTime;
            _waves.Tick(Time.deltaTime);
            Ui?.RefreshHud();

            if (WaveTimer <= 0f)
                OnWaveCleared();
        }

        void OnWaveCleared()
        {
            // clear remaining enemies without rewards spam optional - give clear bonus
            int clearBonus = 15 + Wave * 5;
            Gold += Mathf.RoundToInt(clearBonus * (1f + Stats.goldPct / 100f));

            for (int i = EnemyBrain.All.Count - 1; i >= 0; i--)
            {
                if (EnemyBrain.All[i] != null) Destroy(EnemyBrain.All[i].gameObject);
            }

            if (Wave >= PSConst.MaxWaves)
            {
                Phase = GamePhase.Victory;
                Ui?.ShowEnd(true);
                return;
            }

            Phase = GamePhase.Shop;
            BuildShop();
            Ui?.ShowShop(true);
            Ui?.RefreshAll();
        }

        public void OnPlayerDead()
        {
            if (Phase == GamePhase.Defeat) return;
            Phase = GamePhase.Defeat;
            Ui?.ShowEnd(false);
        }

        public void NextWaveFromShop()
        {
            Wave++;
            BeginWave();
        }

        public void AddKillReward(int gold, int xp, Vector3 pos)
        {
            Kills++;
            float gMul = 1f + Stats.goldPct / 100f;
            Gold += Mathf.Max(1, Mathf.RoundToInt(gold * gMul));

            float xMul = 1f + Stats.xpPct / 100f;
            var skill = Player != null ? Player.GetComponent<ClassSkillDriver>() : null;
            if (skill != null) xMul *= skill.XpMul;
            AddXp(xp * xMul);
        }

        public void AddXp(float amount)
        {
            Xp += amount;
            while (Xp >= CombatMath.XpToNext(Level))
            {
                Xp -= CombatMath.XpToNext(Level);
                Level++;
                Player?.OnLevelUp();
                Ui?.Toast($"升级！Lv{Level}");
            }
        }

        public void PlayerDirectHpLoss(float amount)
        {
            if (Player == null) return;
            // use reflection-free: temporary public method
            Player.ApplyDirectHpLoss(amount);
        }

        public void BuildShop()
        {
            ShopOffers.Clear();
            int luck = Mathf.RoundToInt(Stats.luck);
            for (int i = 0; i < 4; i++)
            {
                var w = RollWeapon(luck);
                ShopOffers.Add(ShopOffer.Weapon(w, ContentDatabase.ShopPrice(w, luck)));
            }
            for (int i = 0; i < 4; i++)
            {
                var item = RollItem(luck);
                ShopOffers.Add(ShopOffer.Item(item, ContentDatabase.ShopPrice(item, luck, ClassId)));
            }
        }

        WeaponDef RollWeapon(int luck)
        {
            // weight by rarity
            var list = ContentDatabase.Weapons;
            float r = Random.value;
            float rareBoost = luck * 0.002f;
            Rarity target;
            if (r < 0.05f + rareBoost) target = Rarity.Legendary;
            else if (r < 0.17f + rareBoost * 2) target = Rarity.Rare;
            else if (r < 0.45f) target = Rarity.Uncommon;
            else target = Rarity.Common;

            var pool = new List<WeaponDef>();
            foreach (var w in list)
            {
                if (w.price <= 0) continue; // skip free turret
                if (ApproxRarity(w) == target || pool.Count == 0) pool.Add(w);
            }
            return pool[Random.Range(0, pool.Count)];
        }

        ItemDef RollItem(int luck)
        {
            float r = Random.value;
            float rareBoost = luck * 0.002f;
            Rarity target;
            if (r < 0.05f + rareBoost) target = Rarity.Legendary;
            else if (r < 0.17f + rareBoost * 2) target = Rarity.Rare;
            else if (r < 0.45f) target = Rarity.Uncommon;
            else target = Rarity.Common;

            // affinity bias
            var pool = new List<ItemDef>();
            foreach (var it in ContentDatabase.Items)
            {
                if (it.rarity == target) pool.Add(it);
                if (it.affinity == ClassId && Random.value < 0.35f) pool.Add(it);
            }
            if (pool.Count == 0) pool.AddRange(ContentDatabase.Items);
            return pool[Random.Range(0, pool.Count)];
        }

        Rarity ApproxRarity(WeaponDef w)
        {
            if (w.price >= 100) return Rarity.Legendary;
            if (w.price >= 60) return Rarity.Rare;
            if (w.price >= 35) return Rarity.Uncommon;
            return Rarity.Common;
        }

        public int RerollCost()
        {
            int[] costs = { 8, 12, 18, 26, 36, 50, 70 };
            int idx = Mathf.Min(RerollCountThisWave, costs.Length - 1);
            int cost = costs[idx];
            if (ClassId == ClassId.Gambler)
            {
                if (RerollCountThisWave == 0) return 0;
                cost = Mathf.RoundToInt(cost * 0.7f);
            }
            return cost;
        }

        public bool TryReroll()
        {
            int cost = RerollCost();
            if (Gold < cost) return false;
            Gold -= cost;
            RerollCountThisWave++;
            BuildShop();
            Ui?.RefreshAll();
            return true;
        }

        public bool TryBuy(int offerIndex)
        {
            if (offerIndex < 0 || offerIndex >= ShopOffers.Count) return false;
            var o = ShopOffers[offerIndex];
            if (o.sold || Gold < o.price) return false;
            if (o.isWeapon)
            {
                if (!Player.TryAddWeapon(o.weapon)) { Ui?.Toast("武器栏已满"); return false; }
            }
            else
            {
                Player.AddItem(o.item);
            }
            Gold -= o.price;
            o.sold = true;
            Ui?.RefreshAll();
            return true;
        }

        public bool TryGamble()
        {
            if (ClassId != ClassId.Gambler || GambleUsedThisWave) return false;
            int cost = Mathf.Max(10, Mathf.RoundToInt(Gold * 0.2f));
            if (Gold < cost) return false;
            Gold -= cost;
            GambleUsedThisWave = true;
            float chance = 0.5f + Mathf.Min(0.14f, Stats.luck / 10f * 0.02f);
            if (Random.value < chance)
            {
                var rares = new List<ItemDef>();
                foreach (var it in ContentDatabase.Items)
                    if (it.rarity == Rarity.Rare || it.rarity == Rarity.Legendary) rares.Add(it);
                var pick = rares[Random.Range(0, rares.Count)];
                Player.AddItem(pick);
                Ui?.Toast($"赌赢了！获得 {pick.displayName}");
            }
            else Ui?.Toast("赌输了……");
            Ui?.RefreshAll();
            return true;
        }

        public void RestartToSelect()
        {
            for (int i = EnemyBrain.All.Count - 1; i >= 0; i--)
                if (EnemyBrain.All[i] != null) Destroy(EnemyBrain.All[i].gameObject);
            if (Player != null) Destroy(Player.gameObject);
            Player = null;
            Phase = GamePhase.ClassSelect;
            Ui?.ShowEnd(false, hide: true);
            Ui?.ShowShop(false);
            Ui?.ShowClassSelect(true);
            Ui?.RefreshAll();
        }
    }

    public class ShopOffer
    {
        public bool isWeapon;
        public WeaponDef weapon;
        public ItemDef item;
        public int price;
        public bool sold;

        public string Title => isWeapon ? weapon.displayName : item.displayName;
        public string Sub => isWeapon ? $"{weapon.tag} 伤{weapon.baseDamage}" : item.desc;

        public static ShopOffer Weapon(WeaponDef w, int p) => new ShopOffer { isWeapon = true, weapon = w, price = p };
        public static ShopOffer Item(ItemDef i, int p) => new ShopOffer { isWeapon = false, item = i, price = p };
    }
}
