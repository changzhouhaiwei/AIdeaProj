using System;
using System.Collections.Generic;
using UnityEngine;

namespace PotatoSiege
{
    [Serializable]
    public class WeaponDef
    {
        public string id;
        public string displayName;
        public WeaponTag tag;
        public ElementKind element;
        public float baseDamage;
        public float interval;
        public float range;
        public float projectileSpeed = 14f;
        public float projectileRadius = 0.12f;
        public int pierce;
        public int bounce;
        public float arcDegrees; // melee cone
        public int price;
        public Rarity rarity = Rarity.Common;
        public Color color = Color.white;
    }

    [Serializable]
    public class ItemDef
    {
        public string id;
        public string displayName;
        public string desc;
        public int price;
        public Rarity rarity;
        public StatBlock bonus = new StatBlock();
        public ClassId? affinity;
    }

    public class WeaponInstance
    {
        public WeaponDef def;
        public int level = 1;
        public float cooldown;

        public float Damage => def.baseDamage * Mathf.Pow(1.18f, level - 1);
        public float Interval => def.interval * Mathf.Pow(0.97f, level - 1);
        public int UpgradePrice => Mathf.RoundToInt(def.price * 0.7f * level);
    }

    public static class ContentDatabase
    {
        public static readonly List<WeaponDef> Weapons = new List<WeaponDef>();
        public static readonly List<ItemDef> Items = new List<ItemDef>();

        static bool _ready;

        public static void Ensure()
        {
            if (_ready) return;
            _ready = true;
            BuildWeapons();
            BuildItems();
        }

        public static WeaponDef GetWeapon(string id)
        {
            Ensure();
            foreach (var w in Weapons)
                if (w.id == id) return w;
            return Weapons[0];
        }

        public static ItemDef GetItem(string id)
        {
            Ensure();
            foreach (var i in Items)
                if (i.id == id) return i;
            return null;
        }

        static void BuildWeapons()
        {
            Weapons.Add(W("pistol", "手枪", WeaponTag.Ranged, 8, 0.45f, 7.5f, 18, Color.yellow));
            Weapons.Add(W("smg", "冲锋枪", WeaponTag.Ranged, 5, 0.18f, 6f, 22, new Color(1f, 0.9f, 0.4f)));
            Weapons.Add(W("sniper", "狙击枪", WeaponTag.Ranged, 28, 1.1f, 12f, 40, new Color(0.6f, 0.9f, 1f), pierce: 1));
            Weapons.Add(W("gloves", "拳套", WeaponTag.Melee, 12, 0.35f, 1.8f, 18, new Color(1f, 0.6f, 0.4f), arc: 90));
            Weapons.Add(W("sword", "阔剑", WeaponTag.Melee, 18, 0.55f, 2.3f, 24, new Color(0.8f, 0.85f, 1f), arc: 120));
            Weapons.Add(W("axe", "战斧", WeaponTag.Melee, 22, 0.7f, 2.4f, 26, new Color(1f, 0.4f, 0.3f), arc: 100));
            Weapons.Add(W("dagger", "匕首", WeaponTag.Melee, 10, 0.28f, 1.6f, 20, new Color(0.9f, 0.3f, 0.5f), arc: 70));
            Weapons.Add(W("scalpel", "解剖刀", WeaponTag.Melee, 9, 0.4f, 1.7f, 18, new Color(0.7f, 1f, 0.8f), arc: 80));
            Weapons.Add(W("pitchfork", "干草叉", WeaponTag.Melee, 14, 0.5f, 2.2f, 18, new Color(0.7f, 0.9f, 0.3f), arc: 60));
            Weapons.Add(W("dicegun", "骰枪", WeaponTag.Ranged, 9, 0.5f, 7f, 20, new Color(0.9f, 0.5f, 1f)));
            Weapons.Add(W("firescroll", "火符", WeaponTag.Element, 6, 0.5f, 6.5f, 22, new Color(1f, 0.45f, 0.1f), ElementKind.Burn));
            Weapons.Add(W("poisonflask", "毒瓶", WeaponTag.Element, 4, 0.6f, 5.5f, 24, new Color(0.4f, 1f, 0.3f), ElementKind.Poison));
            Weapons.Add(W("sparkcore", "电芯", WeaponTag.Element, 7, 0.55f, 6.2f, 28, new Color(0.5f, 0.8f, 1f), ElementKind.Shock, bounce: 1));
            Weapons.Add(W("turret", "随身炮塔", WeaponTag.Engineer, 6, 0.4f, 6.5f, 0, new Color(0.6f, 0.9f, 1f)));
            Weapons.Add(W("shotgun", "霰弹枪", WeaponTag.Ranged, 6, 0.7f, 4.5f, 30, new Color(1f, 0.8f, 0.5f)));
            Weapons.Add(W("crossbow", "弩", WeaponTag.Ranged, 16, 0.85f, 9f, 32, new Color(0.8f, 0.6f, 0.4f), pierce: 1));
        }

        static WeaponDef W(string id, string name, WeaponTag tag, float dmg, float interval, float range, int price,
            Color color, ElementKind el = ElementKind.None, int pierce = 0, int bounce = 0, float arc = 0)
        {
            return new WeaponDef
            {
                id = id,
                displayName = name,
                tag = tag,
                baseDamage = dmg,
                interval = interval,
                range = range,
                price = price,
                color = color,
                element = el,
                pierce = pierce,
                bounce = bounce,
                arcDegrees = arc,
                rarity = price >= 100 ? Rarity.Legendary : price >= 60 ? Rarity.Rare : price >= 35 ? Rarity.Uncommon : Rarity.Common
            };
        }

        static void BuildItems()
        {
            Items.Add(I("coffee", "咖啡", "攻速+12%", 18, Rarity.Common, b => b.attackSpeedPct = 12));
            Items.Add(I("steroid", "激素", "伤害+15%", 22, Rarity.Common, b => b.damagePct = 15));
            Items.Add(I("sneakers", "跑鞋", "移速+12%", 16, Rarity.Common, b => b.moveSpeedPct = 12));
            Items.Add(I("lens", "瞄准镜", "射程+20%", 20, Rarity.Common, b => b.rangePct = 20));
            Items.Add(I("magnet", "磁铁", "拾取+25", 15, Rarity.Common, b => b.pickupRange = 25));
            Items.Add(I("bandage", "绷带", "生命+25 回复+0.5", 18, Rarity.Common, b => { b.maxHp = 25; b.hpRegen = 0.5f; }));
            Items.Add(I("clover", "四叶草", "幸运+10", 24, Rarity.Uncommon, b => b.luck = 10, ClassId.Gambler));
            Items.Add(I("wallet", "钱包", "金钱获取+20%", 28, Rarity.Uncommon, b => b.goldPct = 20, ClassId.Gambler));
            Items.Add(I("scope", "高倍镜", "远程+18% 射程+10%", 35, Rarity.Uncommon, b => { b.rangedPct = 18; b.rangePct = 10; }, ClassId.Gunner));
            Items.Add(I("brassknuckle", "铜指虎", "近战+20%", 32, Rarity.Uncommon, b => b.meleePct = 20, ClassId.Puncher));
            Items.Add(I("medkit", "医疗箱", "生命+40 回复+1.5", 40, Rarity.Uncommon, b => { b.maxHp = 40; b.hpRegen = 1.5f; }, ClassId.Medic));
            Items.Add(I("wrench", "扳手", "工程+15", 30, Rarity.Uncommon, b => b.engineering = 15, ClassId.Engineer));
            Items.Add(I("cloak", "斗篷", "暴击+8% 闪避+5%", 38, Rarity.Uncommon, b => { b.critChance = 8; b.dodge = 5; }, ClassId.Assassin));
            Items.Add(I("fertilizer", "肥料", "经验+25%", 28, Rarity.Uncommon, b => b.xpPct = 25, ClassId.Farmer));
            Items.Add(I("bloodcharm", "血咒", "伤害+20% 生命-15", 36, Rarity.Rare, b => { b.damagePct = 20; b.maxHp = -15; }, ClassId.Berserker));
            Items.Add(I("talisman", "破咒符", "元素+25%", 42, Rarity.Rare, b => b.elementPct = 25, ClassId.Daoist));
            Items.Add(I("armorplate", "装甲板", "护甲+10 生命+20", 40, Rarity.Uncommon, b => { b.armor = 10; b.maxHp = 20; }));
            Items.Add(I("sharpammo", "穿甲弹", "伤害+10% 暴击伤害+25%", 48, Rarity.Rare, b => { b.damagePct = 10; b.critDamage = 25; }));
            Items.Add(I("energybar", "能量棒", "攻速+20% 移速+5%", 45, Rarity.Rare, b => { b.attackSpeedPct = 20; b.moveSpeedPct = 5; }));
            Items.Add(I("crown", "幸运王冠", "幸运+20 金钱+15%", 70, Rarity.Rare, b => { b.luck = 20; b.goldPct = 15; }, ClassId.Gambler));
            Items.Add(I("potatoheart", "土豆之心", "生命+80 回复+2", 90, Rarity.Legendary, b => { b.maxHp = 80; b.hpRegen = 2; }));
            Items.Add(I("overclock", "超频芯片", "攻速+30% 工程+10", 100, Rarity.Legendary, b => { b.attackSpeedPct = 30; b.engineering = 10; }, ClassId.Engineer));
            Items.Add(I("deathmark", "死亡印记", "暴击+15% 暴伤+40% 生命-25", 110, Rarity.Legendary, b => { b.critChance = 15; b.critDamage = 40; b.maxHp = -25; }, ClassId.Assassin));
            Items.Add(I("harvestidol", "丰收神像", "经验+40% 生命+30", 95, Rarity.Legendary, b => { b.xpPct = 40; b.maxHp = 30; }, ClassId.Farmer));
        }

        static ItemDef I(string id, string name, string desc, int price, Rarity r, Action<StatBlock> mut, ClassId? aff = null)
        {
            var item = new ItemDef
            {
                id = id,
                displayName = name,
                desc = desc,
                price = price,
                rarity = r,
                affinity = aff,
                bonus = new StatBlock()
            };
            mut(item.bonus);
            return item;
        }

        public static int ShopPrice(WeaponDef w, int luck)
        {
            float mul = 1f - Mathf.Clamp(luck, 0, 80) * 0.002f;
            return Mathf.Max(5, Mathf.RoundToInt(w.price * mul));
        }

        public static int ShopPrice(ItemDef i, int luck, ClassId cls)
        {
            float mul = 1f - Mathf.Clamp(luck, 0, 80) * 0.002f;
            if (i.affinity.HasValue && i.affinity.Value == cls) mul *= 0.8f;
            return Mathf.Max(5, Mathf.RoundToInt(i.price * mul));
        }
    }
}
