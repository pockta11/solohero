using System;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Talents;

namespace SoloHero.Core.Combat
{
    /// <summary>
    /// The hero (E2). D-093: there is no plain attack; every swing is the basic skill - it fires on the attack-speed
    /// timer (near-zero cooldown, like a MapleStory main skill) and hits up to BASIC_SKILL_TARGETS enemies within
    /// BASIC_SKILL_RANGE, nearest first, each for ATK x BASIC_SKILL_MULT with its own crit roll.
    /// </summary>
    public sealed class HeroBrain
    {
        private const int MaxSwingTargets = 8;

        private readonly BalanceValues _balance;
        private readonly IRandom _random;
        private HeroStats _stats;
        private float _attackTimer;
        private bool _attackPending;
        private double _atkBuffFraction;
        private double _atkSpdBuffFraction;
        private double _critBuffPoints;
        private double _guardFraction;
        private float _shieldTimer;
        private TalentEffects _talents = TalentEffects.None;
        private bool _lastStandUsed;
        private readonly EnemyBrain[] _swing = new EnemyBrain[MaxSwingTargets];

        public HeroBrain(BalanceValues balance, IRandom random, HeroStats stats)
        {
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            Reset(stats);
        }

        public HeroState State { get; private set; }
        public double X { get; private set; }
        public double Hp { get; private set; }
        public double MaxHp => _stats.Hp;
        public double Def => _stats.Def;
        public HeroStats Stats => _stats;

        /// <summary>Damage the active skill shield still absorbs before HP.</summary>
        public double Shield { get; private set; }

        public bool HasGuard => _guardFraction > 0d;

        public event Action<HeroState> StateChanged;
        public event Action AttackRequested;
        public event Action<double, bool> DealtDamage;

        /// <summary>D-087 Last Stand fired: a lethal hit left the hero alive.</summary>
        public event Action LastStandTriggered;

        public void Reset(HeroStats stats)
        {
            _stats = stats;
            Hp = stats.Hp;
            X = 0d;
            _attackTimer = 0f;
            _attackPending = false;
            Shield = 0d;
            _shieldTimer = 0f;
            _lastStandUsed = false;
            SetState(HeroState.Advance);
        }

        public void SetStats(HeroStats stats)
        {
            _stats = stats;
            if (Hp > stats.Hp) Hp = stats.Hp;
            if (Hp < 0d) Hp = 0d;
        }

        public void Tick(float dt, ICombatWorld world)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));

            if (_shieldTimer > 0f)
            {
                _shieldTimer -= dt;
                if (_shieldTimer <= 0f)
                {
                    _shieldTimer = 0f;
                    Shield = 0d;
                }
            }

            switch (State)
            {
                case HeroState.Advance:
                    TickAdvance(dt, world);
                    break;
                case HeroState.Engage:
                    TickEngage(dt, world);
                    break;
                case HeroState.Hit:
                    SetState(world.HasEnemyInRange(_balance.ATTACK_RANGE) ? HeroState.Engage : HeroState.Advance);
                    break;
                case HeroState.Skill:
                    break;
                case HeroState.Dead:
                    break;
            }
        }

        public void OnHitFrame(ICombatWorld world)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (State != HeroState.Engage && State != HeroState.Advance) return;
            if (!_attackPending) return;

            _attackPending = false;
            int count = PickSwingTargets(world);
            int hits = MainHits;
            double mult = MainMult;
            for (int i = 0; i < count; i++)
            {
                EnemyBrain target = _swing[i];
                _swing[i] = null;
                for (int h = 0; h < hits && target.IsAlive; h++)
                {
                    bool crit = DamageCalc.RollCrit(_stats, _random, _critBuffPoints);
                    double dmg = DamageCalc.HeroHit(_stats, crit, _balance, mult)
                        * (1d + _atkBuffFraction) * TalentHitMult(target);
                    target.TakeDamage(dmg);
                    world.ReportHit(target, dmg, crit ? HitKind.Crit : HitKind.Normal);
                    DealtDamage?.Invoke(dmg, crit);
                }

                // D-104 second-job main attacks: a burn (DoT) or a short stun on every target.
                if (_main != null && _main.DotPercent > 0d)
                    target.ApplyDot(DamageCalc.SkillDot(_stats, _main.DotPercent, 1d) * (1d + _talents.DotPct), _main.DotSeconds);
                if (_main != null && _main.StunSeconds > 0f) target.Stun(_main.StunSeconds);
            }
        }

        /// <summary>Seconds until the next basic skill swing (0 while advancing).</summary>
        public float SwingCooldown => _attackTimer > 0f ? _attackTimer : 0f;

        /// <summary>Seconds between basic skill swings at the current attack speed.</summary>
        public float SwingInterval
        {
            get
            {
                double spd = _stats.AtkSpd * (1d + _atkSpdBuffFraction);
                return spd > 0d ? (float)(1d / spd) : 0f;
            }
        }

        /// <summary>Nearest enemies in front of the hero within the basic skill range, up to its target count.</summary>
        private int PickSwingTargets(ICombatWorld world)
        {
            int max = MainTargets < 1 ? 1 : Math.Min(MaxSwingTargets, MainTargets);
            double range = Math.Max(MainRange, _balance.ATTACK_RANGE);
            double heroX = world.HeroX;
            int count = 0;
            while (count < max)
            {
                EnemyBrain best = null;
                double bestDistance = double.MaxValue;
                int slots = world.SlotCount;
                for (int s = 0; s < slots; s++)
                {
                    EnemyBrain e = world.GetSlot(s);
                    if (e == null || !e.IsAlive || e.X < heroX) continue;
                    double d = e.X - heroX;
                    if (d > range || d >= bestDistance || Picked(e, count)) continue;
                    best = e;
                    bestDistance = d;
                }

                if (best == null) break;
                _swing[count++] = best;
            }

            return count;
        }

        private bool Picked(EnemyBrain e, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (_swing[i] == e) return true;
            }

            return false;
        }

        public void SetTalents(TalentEffects talents) => _talents = talents ?? TalentEffects.None;

        /// <summary>D-104: the job's main attack; null keeps the beginner flash slash (BASIC_SKILL_*).</summary>
        public void SetMainAttack(SoloHero.Core.Jobs.MainAttack main) => _main = main;

        private SoloHero.Core.Jobs.MainAttack _main;

        private int MainTargets => _main != null ? _main.Targets : _balance.BASIC_SKILL_TARGETS;
        private double MainMult => _main != null ? _main.Mult : _balance.BASIC_SKILL_MULT;
        private double MainRange => _main != null ? _main.Range : _balance.BASIC_SKILL_RANGE;
        private int MainHits => _main != null && _main.Hits > 1 ? _main.Hits : 1;

        /// <summary>Talent damage factor against one target: boss damage, and Execute under its HP threshold.</summary>
        public double TalentHitMult(EnemyBrain target)
        {
            if (target == null) return 1d;
            double mult = 1d;
            if (target.IsBoss) mult *= 1d + _talents.BossDamagePct;
            if (_talents.Execute && target.MaxHp > 0d && target.Hp < target.MaxHp * TalentCatalog.ExecuteThreshold)
                mult *= 1d + TalentCatalog.ExecuteBonus;
            return mult;
        }

        public void SetAtkBuffFraction(double fraction)
        {
            if (fraction < 0d) fraction = 0d;
            _atkBuffFraction = fraction;
        }

        /// <summary>Skill buffs, set by the owner every tick (D-078): ATK / attack speed fractions, crit points, guard.</summary>
        public void SetSkillBuffs(double atk, double atkSpd, double critPoints, double guard)
        {
            _atkBuffFraction = atk < 0d ? 0d : atk;
            _atkSpdBuffFraction = atkSpd < 0d ? 0d : atkSpd;
            _critBuffPoints = critPoints < 0d ? 0d : critPoints;
            _guardFraction = guard < 0d ? 0d : guard;
        }

        /// <summary>Adds a shield on top of the current one; it lasts until absorbed or the longer timer ends.</summary>
        public void AddShield(double amount, float seconds)
        {
            if (State == HeroState.Dead || amount <= 0d || seconds <= 0f) return;
            Shield += amount;
            if (seconds > _shieldTimer) _shieldTimer = seconds;
        }

        public void ApplyDamage(double amount)
        {
            if (State == HeroState.Dead) return;
            if (amount < 0d) amount = 0d;
            amount = DamageCalc.Guarded(amount, _guardFraction);
            if (_talents.DamageTakenPct > 0d) amount *= 1d - Math.Min(0.5d, _talents.DamageTakenPct);
            if (Shield > 0d)
            {
                double absorbed = Math.Min(Shield, amount);
                Shield -= absorbed;
                amount -= absorbed;
                if (Shield <= 0d)
                {
                    Shield = 0d;
                    _shieldTimer = 0f;
                }

                if (amount <= 0d) return;
            }

            Hp -= amount;
            if (Hp <= 0d && _talents.LastStand && !_lastStandUsed)
            {
                _lastStandUsed = true;
                Hp = MaxHp * TalentCatalog.LastStandHealPct;
                LastStandTriggered?.Invoke();
                return;
            }

            if (Hp <= 0d)
            {
                Hp = 0d;
                _attackPending = false;
                SetState(HeroState.Dead);
                return;
            }

            if (State != HeroState.Skill)
                SetState(HeroState.Hit);
        }

        public void Heal(double amount)
        {
            if (State == HeroState.Dead) return;
            if (amount < 0d) amount = 0d;
            Hp += amount;
            if (Hp > MaxHp) Hp = MaxHp;
        }

        public Result TryBeginSkill()
        {
            if (State == HeroState.Dead) return Result.Fail(FailReason.Busy);
            if (State == HeroState.Skill) return Result.Fail(FailReason.Busy);
            _attackPending = false;
            SetState(HeroState.Skill);
            return Result.Success;
        }

        public void EndSkill(ICombatWorld world)
        {
            if (State != HeroState.Skill) return;
            if (Hp <= 0d)
            {
                SetState(HeroState.Dead);
                return;
            }

            SetState(world != null && world.HasEnemyInRange(_balance.ATTACK_RANGE)
                ? HeroState.Engage
                : HeroState.Advance);
        }

        public void Kill()
        {
            Hp = 0d;
            _attackPending = false;
            SetState(HeroState.Dead);
        }

        private void TickAdvance(float dt, ICombatWorld world)
        {
            if (world.HasEnemyInRange(_balance.ATTACK_RANGE))
            {
                SetState(HeroState.Engage);
                return;
            }

            X += _balance.MOVE_SPEED * dt;
        }

        private void TickEngage(float dt, ICombatWorld world)
        {
            if (!world.HasEnemyInRange(_balance.ATTACK_RANGE))
            {
                SetState(HeroState.Advance);
                return;
            }

            _attackTimer -= dt;
            if (_attackTimer > 0f) return;

            double spd = _stats.AtkSpd * (1d + _atkSpdBuffFraction);
            _attackTimer = spd > 0d ? (float)(1d / spd) : float.MaxValue;
            _attackPending = true;
            AttackRequested?.Invoke();
        }

        private void SetState(HeroState next)
        {
            if (State == next) return;
            State = next;
            StateChanged?.Invoke(next);
        }
    }
}
