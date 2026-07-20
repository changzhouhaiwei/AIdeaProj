using UnityEngine;

namespace PotatoSiege
{
    public class ClassSkillDriver : MonoBehaviour
    {
        PlayerController _player;
        ClassId _id;

        public float MoveMul { get; private set; } = 1f;
        public float DamageMul { get; private set; } = 1f;
        public float DamageTakenMul { get; private set; } = 1f;
        public float BonusAspd { get; private set; }
        public float BonusCrit { get; private set; }
        public float LifestealPct { get; private set; }
        public bool IsInvulnerable { get; private set; }

        float _skillCd;
        float _buffTimer;
        float _invulnTimer;
        float _farmerXpBuff;
        int _daoistSpreadLeft;

        static readonly (string name, System.Action<StatBlock> apply)[] FarmerPool =
        {
            ("HP", s => s.maxHp += 4),
            ("DMG", s => s.damagePct += 2),
            ("ASPD", s => s.attackSpeedPct += 2),
            ("REG", s => s.hpRegen += 0.2f),
            ("CRIT", s => s.critChance += 1),
            ("MOVE", s => s.moveSpeedPct += 2),
            ("PICK", s => s.pickupRange += 4),
            ("GOLD", s => s.goldPct += 3),
        };

        public void Bind(PlayerController p)
        {
            _player = p;
            _id = GameSession.I.ClassId;
        }

        public void TickCombat(float dt)
        {
            if (_skillCd > 0) _skillCd -= dt;
            MoveMul = 1f;
            DamageMul = 1f;
            DamageTakenMul = 1f;
            BonusAspd = 0f;
            BonusCrit = 0f;
            LifestealPct = 0f;

            if (_invulnTimer > 0)
            {
                _invulnTimer -= dt;
                IsInvulnerable = _invulnTimer > 0;
            }
            else IsInvulnerable = false;

            if (_buffTimer > 0)
            {
                _buffTimer -= dt;
                ApplyBuffTick();
            }

            if (_farmerXpBuff > 0) _farmerXpBuff -= dt;

            // farmer early mitigation
            if (_id == ClassId.Farmer && GameSession.I.Wave <= 5)
                DamageTakenMul *= 0.9f;

            // gunner bunker active handled in buff
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Q) || Input.GetMouseButtonDown(2))
                TryActive();

            GameSession.I.Ui?.SetSkillCd(_skillCd, SkillMaxCd());
        }

        float SkillMaxCd()
        {
            switch (_id)
            {
                case ClassId.Potato: return 12f;
                case ClassId.Gunner: return 14f;
                case ClassId.Puncher: return 10f;
                case ClassId.Medic: return 16f;
                case ClassId.Engineer: return 18f;
                case ClassId.Assassin: return 7f;
                case ClassId.Farmer: return 20f;
                case ClassId.Berserker: return 15f;
                case ClassId.Daoist: return 12f;
                default: return 999f;
            }
        }

        void TryActive()
        {
            if (_skillCd > 0) return;
            if (_id == ClassId.Gambler) return; // shop skill only

            switch (_id)
            {
                case ClassId.Potato:
                    _buffTimer = 3f;
                    _skillCd = 12f;
                    break;
                case ClassId.Gunner:
                    _buffTimer = 1.5f;
                    _skillCd = 14f;
                    break;
                case ClassId.Puncher:
                    DashPunch();
                    _skillCd = 10f;
                    break;
                case ClassId.Medic:
                    float heal = _player.Stats.maxHp * 0.12f + _player.Stats.hpRegen * 3f;
                    _player.Heal(heal);
                    _skillCd = 16f;
                    break;
                case ClassId.Engineer:
                    _buffTimer = 5f;
                    _skillCd = 18f;
                    break;
                case ClassId.Assassin:
                    DashAssassin();
                    _skillCd = 7f;
                    break;
                case ClassId.Farmer:
                    _farmerXpBuff = 10f;
                    _skillCd = 20f;
                    break;
                case ClassId.Berserker:
                {
                    float pay = Mathf.Max(1f, _player.CurrentHp * 0.15f);
                    GameSession.I.PlayerDirectHpLoss(pay);
                    _buffTimer = 4f;
                    _skillCd = 15f;
                    break;
                }
                case ClassId.Daoist:
                    _daoistSpreadLeft = 3;
                    _skillCd = 12f;
                    break;
            }
        }

        void ApplyBuffTick()
        {
            switch (_id)
            {
                case ClassId.Potato:
                    MoveMul *= 1.15f;
                    DamageTakenMul *= 0.85f;
                    break;
                case ClassId.Gunner:
                    MoveMul *= 0.7f;
                    BonusAspd += 35f;
                    break;
                case ClassId.Engineer:
                    BonusAspd += 50f; // turrets / all
                    MoveMul *= 1.1f;
                    break;
                case ClassId.Assassin:
                    BonusCrit += 15f;
                    break;
                case ClassId.Berserker:
                    LifestealPct = 0.08f;
                    DamageTakenMul *= 0.9f;
                    break;
            }
        }

        void DashPunch()
        {
            Vector2 dir = _player.transform.right;
            _player.transform.position += (Vector3)(dir * 2.5f);
            _invulnTimer = 0.2f;
            var target = EnemyBrain.FindNearest(_player.transform.position);
            if (target != null)
            {
                float dmg = 24f * (1f + _player.Stats.damagePct / 100f) * (1f + _player.Stats.meleePct / 100f);
                target.TakeDamage(dmg, WeaponTag.Melee, ElementKind.None);
                target.Status?.AddPunchStack();
                target.Status?.AddPunchStack();
            }
        }

        void DashAssassin()
        {
            Vector2 dir = _player.transform.right;
            _player.transform.position += (Vector3)(dir * 2.2f);
            _invulnTimer = 0.3f;
            _buffTimer = 2f;
        }

        public void OnLevelUp()
        {
            if (_id != ClassId.Farmer) return;
            var pick = FarmerPool[Random.Range(0, FarmerPool.Length)];
            pick.apply(_player.Stats);
            if (pick.name == "HP")
                // heal a bit
                _player.Heal(4f);
            GameSession.I.Ui?.Toast($"丰收：{pick.name}");
        }

        public float XpMul => _id == ClassId.Farmer && _farmerXpBuff > 0 ? 1.5f : 1f;

        public bool ConsumeDaoistSpread()
        {
            if (_daoistSpreadLeft <= 0) return false;
            _daoistSpreadLeft--;
            return true;
        }

        public void OnElementHit(EnemyBrain enemy)
        {
            if (_id != ClassId.Daoist || !ConsumeDaoistSpread()) return;
            if (enemy?.Status == null) return;
            float shareBurn = enemy.Status.burnLayers * 0.5f;
            float sharePoison = enemy.Status.poisonLayers * 0.5f;
            float shareShock = enemy.Status.shockLayers * 0.5f;
            foreach (var e in EnemyBrain.All)
            {
                if (e == null || e == enemy) continue;
                if (Vector2.Distance(e.transform.position, enemy.transform.position) > 2.5f) continue;
                if (shareBurn > 0) e.Status.AddElement(ElementKind.Burn, shareBurn, true);
                if (sharePoison > 0) e.Status.AddElement(ElementKind.Poison, sharePoison, true);
                if (shareShock > 0) e.Status.AddElement(ElementKind.Shock, shareShock, true);
            }
        }
    }
}
