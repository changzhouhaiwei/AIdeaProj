using UnityEngine;

namespace PotatoSiege
{
    public class StatusHost : MonoBehaviour
    {
        public float burnLayers;
        public float poisonLayers;
        public float shockLayers;
        public float burnTimer;
        public float poisonTimer;
        public float shockTimer;
        public float vulnerability; // punch stacks * 0.06
        public int punchStacks;
        public float punchTargetId; // unused marker

        EnemyBrain _enemy;
        bool _isPlayer;

        public void InitEnemy(EnemyBrain e)
        {
            _enemy = e;
            _isPlayer = false;
        }

        public void InitPlayer()
        {
            _isPlayer = true;
        }

        public void AddElement(ElementKind kind, float layers, bool daoist)
        {
            int maxBurn = daoist ? 7 : 5;
            int maxPoison = daoist ? 7 : 5;
            int maxShock = daoist ? 6 : 4;
            switch (kind)
            {
                case ElementKind.Burn:
                    burnLayers = Mathf.Min(maxBurn, burnLayers + layers);
                    burnTimer = 3f;
                    break;
                case ElementKind.Poison:
                    poisonLayers = Mathf.Min(maxPoison, poisonLayers + layers);
                    poisonTimer = 4f;
                    break;
                case ElementKind.Shock:
                    shockLayers = Mathf.Min(maxShock, shockLayers + layers);
                    shockTimer = 2.5f;
                    break;
            }
        }

        public void AddPunchStack()
        {
            punchStacks = Mathf.Min(5, punchStacks + 1);
            vulnerability = punchStacks * 0.06f;
        }

        public void ClearPunch()
        {
            punchStacks = 0;
            vulnerability = 0f;
        }

        public int DistinctElements()
        {
            int n = 0;
            if (burnLayers > 0 && burnTimer > 0) n++;
            if (poisonLayers > 0 && poisonTimer > 0) n++;
            if (shockLayers > 0 && shockTimer > 0) n++;
            return n;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (burnTimer > 0)
            {
                burnTimer -= dt;
                if (_enemy != null && burnLayers > 0)
                {
                    float dps = (2f + burnLayers) * (1f + GameSession.I.Stats.elementPct / 100f);
                    _enemy.ApplyDot(dps * dt);
                }
                if (burnTimer <= 0) burnLayers = 0;
            }
            if (poisonTimer > 0)
            {
                poisonTimer -= dt;
                if (_enemy != null && poisonLayers > 0)
                {
                    float dps = (1.5f + poisonLayers * 0.8f) * (1f + GameSession.I.Stats.elementPct / 100f);
                    _enemy.ApplyDot(dps * dt);
                    _enemy.SlowMul = 1f - poisonLayers * 0.03f;
                }
                if (poisonTimer <= 0)
                {
                    poisonLayers = 0;
                    if (_enemy != null) _enemy.SlowMul = 1f;
                }
            }
            if (shockTimer > 0)
            {
                shockTimer -= dt;
                if (shockTimer <= 0) shockLayers = 0;
            }
        }

        public float ConsumeShockBonus()
        {
            if (shockLayers <= 0 || shockTimer <= 0) return 0f;
            float bonus = 4f * shockLayers;
            return bonus;
        }
    }
}
