using System.Collections.Generic;
using UnityEngine;

namespace PotatoSiege
{
    public class PlayerController : MonoBehaviour
    {
        public StatBlock Stats => GameSession.I.Stats;
        public float CurrentHp { get; private set; }
        public float Shield { get; private set; }
        public float UnhurtTimer { get; private set; }
        public bool FocusActive { get; private set; }
        public EnemyBrain PunchFocus;

        public readonly List<WeaponInstance> Weapons = new List<WeaponInstance>();
        public readonly List<string> OwnedItems = new List<string>();

        SpriteRenderer _sr;
        ClassSkillDriver _skills;
        float _regenAcc;
        float _shieldDecayAcc;
        bool _luckSaved;
        readonly List<EnemyBrain> _nearBuf = new List<EnemyBrain>();

        public void Init(ClassDef cls)
        {
            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = SpriteBank.Get(cls.spriteKey);
            _sr.color = cls.tint;
            _sr.sortingOrder = 15;
            transform.localScale = Vector3.one * 1.1f;

            var rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = 0.35f;

            CurrentHp = Stats.maxHp;
            _skills = gameObject.AddComponent<ClassSkillDriver>();
            _skills.Bind(this);

            ContentDatabase.Ensure();
            Weapons.Clear();
            foreach (var wid in cls.startWeapons)
            {
                if (Weapons.Count >= PSConst.WeaponSlots) break;
                Weapons.Add(new WeaponInstance { def = ContentDatabase.GetWeapon(wid), level = 1 });
            }
        }

        void Update()
        {
            if (GameSession.I == null) return;
            if (GameSession.I.Phase != GamePhase.Wave)
            {
                FocusActive = false;
                return;
            }

            HandleMove();
            TickRegen();
            TickShield();
            AutoFire();
            _skills.TickCombat(Time.deltaTime);

            UnhurtTimer += Time.deltaTime;
            FocusActive = GameSession.I.ClassId == ClassId.Gunner && UnhurtTimer >= 3f;
        }

        void HandleMove()
        {
            Vector2 input = Vector2.zero;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) input.y += 1;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) input.y -= 1;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) input.x -= 1;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) input.x += 1;

            // touch / mouse drag fallback: hold right mouse or second touch not needed — mouse world follow optional
            if (input.sqrMagnitude < 0.01f && Input.GetMouseButton(1))
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    Vector3 mp = cam.ScreenToWorldPoint(Input.mousePosition);
                    mp.z = 0;
                    input = ((Vector2)(mp - transform.position)).normalized;
                }
            }

            if (input.sqrMagnitude > 1f) input.Normalize();
            float speed = CombatMath.MoveSpeed(4.2f, Stats.moveSpeedPct);
            if (GameSession.I.ClassId == ClassId.Berserker)
            {
                float ratio = CurrentHp / Mathf.Max(1f, Stats.maxHp);
                if (ratio < 0.25f) speed *= 1.1f;
            }
            if (_skills != null) speed *= _skills.MoveMul;

            transform.position += (Vector3)(input * speed * Time.deltaTime);
            var p = transform.position;
            p.x = Mathf.Clamp(p.x, -PSConst.ArenaHalf, PSConst.ArenaHalf);
            p.y = Mathf.Clamp(p.y, -PSConst.ArenaHalf, PSConst.ArenaHalf);
            transform.position = p;

            var target = EnemyBrain.FindNearest(transform.position);
            if (target != null)
            {
                Vector2 d = target.transform.position - transform.position;
                float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, ang);
            }
        }

        void TickRegen()
        {
            if (Stats.hpRegen <= 0) return;
            _regenAcc += Stats.hpRegen * Time.deltaTime;
            if (_regenAcc >= 0.25f)
            {
                Heal(_regenAcc);
                _regenAcc = 0;
            }
        }

        void TickShield()
        {
            if (Shield <= 0) return;
            _shieldDecayAcc += Time.deltaTime;
            if (_shieldDecayAcc >= 0.25f)
            {
                float decay = Mathf.Max(2f, Shield * 0.08f) * _shieldDecayAcc;
                Shield = Mathf.Max(0, Shield - decay);
                _shieldDecayAcc = 0;
            }
        }

        public void Heal(float amount)
        {
            float before = CurrentHp;
            CurrentHp = Mathf.Min(Stats.maxHp, CurrentHp + amount);
            float overflow = amount - (CurrentHp - before);
            if (overflow > 0 && GameSession.I.ClassId == ClassId.Medic)
            {
                float convert = overflow * PSConst.ShieldConvert;
                float cap = Stats.maxHp * PSConst.ShieldMaxRatio;
                Shield = Mathf.Min(cap, Shield + convert);
            }
        }

        public void ApplyDirectHpLoss(float amount)
        {
            CurrentHp = Mathf.Max(1f, CurrentHp - amount);
            UnhurtTimer = 0f;
        }

        public void TryContactHit(float dmg)
        {
            TakeDamage(dmg);
        }

        public bool TakeDamage(float raw)
        {
            if (GameSession.I.Phase != GamePhase.Wave) return false;
            if (_skills != null && _skills.IsInvulnerable) return false;

            if (Stats.dodge > 0 && Random.value * 100f < Stats.dodge)
                return false;

            float mitigated = raw * (1f - CombatMath.ArmorMitigation(Stats.armor));
            if (_skills != null) mitigated *= _skills.DamageTakenMul;

            UnhurtTimer = 0f;
            FocusActive = false;

            if (Shield > 0)
            {
                float absorb = Mathf.Min(Shield, mitigated);
                Shield -= absorb;
                mitigated -= absorb;
            }

            if (mitigated <= 0) return false;

            // gambler luck save once per wave
            if (CurrentHp - mitigated <= 0 && GameSession.I.ClassId == ClassId.Gambler && Stats.luck >= 20 && !_luckSaved)
            {
                _luckSaved = true;
                CurrentHp = 1f;
                return false;
            }

            CurrentHp -= mitigated;
            if (CurrentHp <= 0)
            {
                CurrentHp = 0;
                GameSession.I.OnPlayerDead();
            }
            return true;
        }

        public void ResetWaveFlags()
        {
            _luckSaved = false;
            UnhurtTimer = 0;
        }

        public void OnLevelUp()
        {
            Stats.maxHp += 2;
            Stats.damagePct += 1;
            CurrentHp = Mathf.Min(Stats.maxHp, CurrentHp + 2);
            _skills?.OnLevelUp();
        }

        void AutoFire()
        {
            for (int i = 0; i < Weapons.Count; i++)
            {
                var w = Weapons[i];
                w.cooldown -= Time.deltaTime;
                if (w.cooldown > 0) continue;
                if (TryFire(w))
                {
                    float aspd = Stats.attackSpeedPct;
                    if (_skills != null) aspd += _skills.BonusAspd;
                    if (FocusActive) { /* gunner focus is damage, not aspd */ }
                    if (GameSession.I.ClassId == ClassId.Berserker && CurrentHp / Stats.maxHp < 0.5f)
                        aspd += 20f;
                    if (GameSession.I.ClassId == ClassId.Engineer && w.def.tag == WeaponTag.Engineer)
                        aspd *= 0.5f; // turret half aspd benefit already in skill; keep base
                    w.cooldown = CombatMath.AttackInterval(w.Interval, aspd);
                }
                else w.cooldown = 0.1f;
            }
        }

        bool TryFire(WeaponInstance w)
        {
            float range = w.def.range * (1f + Stats.rangePct / 100f);
            var target = EnemyBrain.FindNearest(transform.position);
            if (target == null) return false;
            float dist = Vector2.Distance(transform.position, target.transform.position);
            if (dist > range * 1.05f && w.def.tag != WeaponTag.Engineer) return false;
            if (w.def.tag == WeaponTag.Engineer && dist > range * 1.2f) return false;

            float dmg = ComputeDamage(w, target);
            if (w.def.tag == WeaponTag.Melee)
                MeleeSwing(w, dmg, range);
            else if (w.def.id == "shotgun")
            {
                for (int i = -2; i <= 2; i++)
                    ShootProjectile(w, dmg * 0.7f, Quaternion.Euler(0, 0, i * 8f) * (target.transform.position - transform.position));
            }
            else
                ShootProjectile(w, dmg, target.transform.position - transform.position);

            // punch stacks
            if (GameSession.I.ClassId == ClassId.Puncher && w.def.tag == WeaponTag.Melee)
            {
                if (PunchFocus != target)
                {
                    if (PunchFocus != null && PunchFocus.Status != null) PunchFocus.Status.ClearPunch();
                    PunchFocus = target;
                }
                target.Status?.AddPunchStack();
            }

            return true;
        }

        float ComputeDamage(WeaponInstance w, EnemyBrain target)
        {
            float tagBonus = 0f;
            switch (w.def.tag)
            {
                case WeaponTag.Melee: tagBonus = Stats.meleePct; break;
                case WeaponTag.Ranged: tagBonus = Stats.rangedPct; break;
                case WeaponTag.Element: tagBonus = Stats.elementPct; break;
                case WeaponTag.Engineer:
                    tagBonus = Stats.engineering; // treated below
                    break;
            }

            float classMod = 0f;
            float mul = 1f;
            var cls = GameSession.I.ClassId;

            if (cls == ClassId.Puncher && w.def.tag == WeaponTag.Ranged) classMod -= 25f;
            if (cls == ClassId.Gunner && FocusActive)
            {
                mul *= 1.15f;
                if (w.def.tag == WeaponTag.Ranged) mul *= 1.10f;
            }
            if (cls == ClassId.Potato)
            {
                var tags = new HashSet<WeaponTag>();
                foreach (var x in Weapons) tags.Add(x.def.tag);
                mul *= 1f + Mathf.Min(3, tags.Count) * 0.03f;
            }
            if (cls == ClassId.Berserker)
            {
                float lost = 1f - CurrentHp / Mathf.Max(1f, Stats.maxHp);
                mul *= 1f + lost * 0.6f;
                if (CurrentHp / Stats.maxHp < 0.25f) mul *= 1.2f;
            }
            if (cls == ClassId.Engineer && w.def.tag != WeaponTag.Engineer)
                mul *= 0.8f;
            if (cls == ClassId.Engineer && w.def.tag == WeaponTag.Engineer)
                mul *= 1f + Stats.engineering / 50f;

            if (cls == ClassId.Assassin)
            {
                EnemyBrain.GetNearestN(transform.position, 3, _nearBuf);
                if (_nearBuf.Contains(target))
                {
                    // crit damage bonus applied in crit roll via extra
                }
            }

            if (cls == ClassId.Daoist)
            {
                int kinds = 0;
                foreach (var e in EnemyBrain.All)
                {
                    if (e?.Status == null) continue;
                    kinds = Mathf.Max(kinds, e.Status.DistinctElements());
                    if (e.Status.DistinctElements() >= 2) { mul *= 1.1f; break; }
                }
            }

            float baseDmg = w.Damage * (1f + (Stats.damagePct + tagBonus + classMod) / 100f) * mul;
            if (_skills != null) baseDmg *= _skills.DamageMul;

            // crit
            float crit = CombatMath.EffectiveCritChance(Stats.critChance);
            if (_skills != null) crit += _skills.BonusCrit;
            float critD = Stats.critDamage;
            if (cls == ClassId.Assassin)
            {
                EnemyBrain.GetNearestN(transform.position, 3, _nearBuf);
                if (_nearBuf.Contains(target)) critD += 40f;
            }
            if (Random.value * 100f < crit)
                baseDmg *= critD / 100f;

            if (target.Status != null) baseDmg *= 1f + target.Status.vulnerability;

            // lifesteal berserker active
            if (_skills != null && _skills.LifestealPct > 0)
                Heal(baseDmg * _skills.LifestealPct);

            return baseDmg;
        }

        void ShootProjectile(WeaponInstance w, float dmg, Vector2 dir)
        {
            if (dir.sqrMagnitude < 0.001f) dir = transform.right;
            var proj = Projectile.Spawn(transform.position, dir, w.def.projectileSpeed, dmg, w.def.color, w.def.element, w.def.tag, w.def.pierce, w.def.bounce);
            proj.onHitEnemy = (e, d) =>
            {
                if (_skills != null && _skills.LifestealPct > 0) Heal(d * _skills.LifestealPct);
                if (w.def.element != ElementKind.None) _skills?.OnElementHit(e);
            };
        }

        void MeleeSwing(WeaponInstance w, float dmg, float range)
        {
            float arc = w.def.arcDegrees > 0 ? w.def.arcDegrees : 90f;
            Vector2 forward = transform.right;
            foreach (var e in EnemyBrain.All)
            {
                if (e == null || e.hp <= 0) continue;
                Vector2 to = e.transform.position - transform.position;
                float dist = to.magnitude;
                if (dist > range) continue;
                float ang = Vector2.Angle(forward, to);
                if (ang > arc * 0.5f) continue;
                float final = dmg;
                if (e.Status != null) final *= 1f + e.Status.vulnerability;
                e.TakeDamage(final, w.def.tag, w.def.element);
                if (_skills != null && _skills.LifestealPct > 0) Heal(final * _skills.LifestealPct);
                if (w.def.element != ElementKind.None) _skills?.OnElementHit(e);
            }
        }

        public bool TryAddWeapon(WeaponDef def)
        {
            foreach (var w in Weapons)
            {
                if (w.def.id == def.id)
                {
                    w.level++;
                    return true;
                }
            }
            if (Weapons.Count >= PSConst.WeaponSlots) return false;
            Weapons.Add(new WeaponInstance { def = def, level = 1 });
            return true;
        }

        public void AddItem(ItemDef item)
        {
            OwnedItems.Add(item.id);
            Stats.Add(item.bonus);
            if (item.bonus.maxHp != 0)
                CurrentHp = Mathf.Clamp(CurrentHp + Mathf.Max(0, item.bonus.maxHp), 1, Stats.maxHp);
        }
    }
}
