using System;
using UnityEngine;

namespace PotatoSiege
{
    [Serializable]
    public class StatBlock
    {
        public float maxHp = 100f;
        public float hpRegen;
        public float damagePct;
        public float attackSpeedPct;
        public float moveSpeedPct;
        public float rangePct;
        public float critChance = 3f;
        public float critDamage = 150f;
        public float pickupRange = 30f;
        public float luck;
        public float engineering;
        public float armor;
        public float dodge;
        public float xpPct;
        public float goldPct;
        public float meleePct;
        public float rangedPct;
        public float elementPct;

        public StatBlock Clone()
        {
            return (StatBlock)MemberwiseClone();
        }

        public void Add(StatBlock o)
        {
            maxHp += o.maxHp;
            hpRegen += o.hpRegen;
            damagePct += o.damagePct;
            attackSpeedPct += o.attackSpeedPct;
            moveSpeedPct += o.moveSpeedPct;
            rangePct += o.rangePct;
            critChance += o.critChance;
            critDamage += o.critDamage;
            pickupRange += o.pickupRange;
            luck += o.luck;
            engineering += o.engineering;
            armor += o.armor;
            dodge += o.dodge;
            xpPct += o.xpPct;
            goldPct += o.goldPct;
            meleePct += o.meleePct;
            rangedPct += o.rangedPct;
            elementPct += o.elementPct;
        }
    }

    public static class CombatMath
    {
        public static float ArmorMitigation(float armor) => armor / (armor + 50f);

        public static float EffectiveCritChance(float crit)
        {
            if (crit <= PSConst.CritSoftCap) return Mathf.Min(crit, PSConst.CritHardCap);
            float over = crit - PSConst.CritSoftCap;
            return Mathf.Min(PSConst.CritSoftCap + over * 0.4f, PSConst.CritHardCap);
        }

        public static float AttackInterval(float baseInterval, float aspdPct)
        {
            float i = baseInterval / (1f + aspdPct / 100f);
            return Mathf.Max(PSConst.AspdIntervalFloor, i);
        }

        public static float MoveSpeed(float baseSpeed, float movePct)
        {
            float soft = 50f;
            float hard = 80f;
            float pct = movePct;
            if (pct > soft) pct = soft + (pct - soft) * 0.5f;
            pct = Mathf.Min(pct, hard);
            return baseSpeed * (1f + pct / 100f);
        }

        public static int XpToNext(int level)
        {
            if (level < 1) level = 1;
            if (level == 1) return 12;
            if (level == 2) return 18;
            if (level == 3) return 26;
            if (level == 4) return 36;
            if (level == 5) return 48;
            if (level == 6) return 62;
            if (level == 7) return 78;
            if (level == 8) return 96;
            if (level == 9) return 116;
            return 116 + (level - 9) * 24;
        }

        public static float WaveHpMul(int wave)
        {
            if (wave <= 3) return 1f;
            if (wave <= 6) return 1.4f;
            if (wave <= 9) return 1.9f;
            if (wave == 10) return 2.4f;
            if (wave <= 14) return 2.8f;
            if (wave == 15) return 3.5f;
            if (wave <= 19) return 4f;
            return 6f;
        }

        public static float WaveDmgMul(int wave)
        {
            if (wave <= 3) return 1f;
            if (wave <= 6) return 1.15f;
            if (wave <= 9) return 1.3f;
            if (wave == 10) return 1.5f;
            if (wave <= 14) return 1.6f;
            if (wave == 15) return 1.8f;
            if (wave <= 19) return 2f;
            return 2.4f;
        }

        public static float WaveCountMul(int wave)
        {
            if (wave <= 3) return 1f;
            if (wave <= 6) return 1.3f;
            if (wave <= 9) return 1.6f;
            if (wave == 10) return 1.4f;
            if (wave <= 14) return 1.9f;
            if (wave == 15) return 1.5f;
            if (wave <= 19) return 2.2f;
            return 1.2f;
        }
    }
}
