using System;
using UnityEngine;

namespace PotatoSiege
{
    [Serializable]
    public class ClassDef
    {
        public ClassId id;
        public string displayName;
        public string oneLiner;
        public int difficulty;
        public string spriteKey;
        public StatBlock stats = new StatBlock();
        public string[] startWeapons;
        public Color tint = Color.white;
    }

    public static class ClassDatabase
    {
        static ClassDef[] _cache;

        public static ClassDef[] All
        {
            get
            {
                if (_cache == null) Build();
                return _cache;
            }
        }

        public static ClassDef Get(ClassId id)
        {
            foreach (var c in All)
                if (c.id == id) return c;
            return All[0];
        }

        static void Build()
        {
            _cache = new[]
            {
                Make(ClassId.Potato, "原种土豆", "什么都能玩，什么都不极端", 1, "player",
                    s => { }, new[] { "pistol" }, new Color(0.95f, 0.75f, 0.35f)),

                Make(ClassId.Gunner, "枪手", "站桩不是菜，站桩是输出", 2, "player_soldier",
                    s =>
                    {
                        s.maxHp = 90; s.damagePct = 12; s.attackSpeedPct = 5; s.moveSpeedPct = -18;
                        s.rangePct = 25; s.critChance = 5; s.rangedPct = 10;
                    }, new[] { "pistol", "pistol" }, new Color(0.4f, 0.7f, 1f)),

                Make(ClassId.Puncher, "拳手", "贴上去才有输出", 3, "player_survivor",
                    s =>
                    {
                        s.maxHp = 130; s.hpRegen = 1; s.damagePct = 5; s.attackSpeedPct = 8;
                        s.moveSpeedPct = 5; s.rangePct = -30; s.meleePct = 25; s.rangedPct = -25;
                    }, new[] { "gloves" }, new Color(1f, 0.55f, 0.35f)),

                Make(ClassId.Medic, "医生", "血越厚越好玩", 2, "player_blue",
                    s =>
                    {
                        s.maxHp = 160; s.hpRegen = 3; s.damagePct = -15; s.moveSpeedPct = -5; s.armor = 5;
                    }, new[] { "scalpel" }, new Color(0.5f, 1f, 0.7f)),

                Make(ClassId.Gambler, "赌徒", "钱是用来翻盘的", 4, "player_hitman",
                    s =>
                    {
                        s.maxHp = 75; s.moveSpeedPct = 5; s.luck = 25; s.goldPct = 30;
                    }, new[] { "dicegun" }, new Color(0.85f, 0.4f, 0.95f)),

                Make(ClassId.Engineer, "工程师", "人可以怂，炮塔必须凶", 3, "player_robot",
                    s =>
                    {
                        s.maxHp = 95; s.damagePct = -20; s.engineering = 20;
                    }, new[] { "turret" }, new Color(0.7f, 0.85f, 1f)),

                Make(ClassId.Assassin, "刺客", "一秒杀一片，也可能一秒死", 4, "player_woman",
                    s =>
                    {
                        s.maxHp = 65; s.damagePct = 8; s.attackSpeedPct = 10; s.moveSpeedPct = 20;
                        s.critChance = 18; s.critDamage = 175; s.dodge = 8;
                    }, new[] { "dagger" }, new Color(0.9f, 0.35f, 0.45f)),

                Make(ClassId.Farmer, "农夫", "前几波忍一忍，后面越打越胖", 3, "player_old",
                    s =>
                    {
                        s.maxHp = 90; s.damagePct = -10; s.attackSpeedPct = -5; s.xpPct = 40; s.pickupRange = 50;
                    }, new[] { "pitchfork" }, new Color(0.7f, 0.9f, 0.4f)),

                Make(ClassId.Berserker, "狂战士", "血越少越能打", 5, "player",
                    s =>
                    {
                        s.maxHp = 110; s.attackSpeedPct = 5; s.moveSpeedPct = 5; s.rangePct = -10; s.critChance = 5;
                    }, new[] { "axe" }, new Color(1f, 0.25f, 0.2f)),

                Make(ClassId.Daoist, "道士", "靠点燃、毒爆、连锁", 3, "player_blue",
                    s =>
                    {
                        s.maxHp = 100; s.hpRegen = 1; s.rangePct = 5; s.elementPct = 20;
                    }, new[] { "firescroll" }, new Color(1f, 0.6f, 0.2f)),
            };
        }

        static ClassDef Make(ClassId id, string name, string line, int diff, string sprite,
            Action<StatBlock> mut, string[] weapons, Color tint)
        {
            var c = new ClassDef
            {
                id = id,
                displayName = name,
                oneLiner = line,
                difficulty = diff,
                spriteKey = sprite,
                startWeapons = weapons,
                tint = tint,
                stats = new StatBlock()
            };
            mut(c.stats);
            return c;
        }
    }
}
