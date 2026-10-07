using System;
using SoloHero.Core.Config;
using SoloHero.Core.Pets;

namespace SoloHero.Core.Combat
{
    /// <summary>
    /// D-102 / D-114: the equipped pet attacks the nearest enemy in its range every Interval seconds for hero ATK x
    /// AttackMult x its level and enhance scale, with the D-098 combo bonus, and applies its burn / stun. Ticks with
    /// the stage.
    /// </summary>
    public sealed class PetCaster
    {
        private readonly BalanceValues _balance;
        private PetDef _def;
        private double _scale = 1d;
        private float _timer;

        public PetCaster(BalanceValues balance)
        {
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
        }

        public PetDef Def => _def;

        /// <summary>The pet struck: target, damage dealt.</summary>
        public event Action<EnemyBrain, double> Attacked;

        /// <param name="scale">Level and enhance multiplier (PetService.AttackScale).</param>
        public void Set(PetDef def, double scale)
        {
            if (def != _def) _timer = def != null ? def.Interval : 0f;
            _def = def;
            _scale = scale > 0d ? scale : 1d;
        }

        public void Reset() => _timer = _def != null ? _def.Interval : 0f;

        public void Tick(float dt, HeroBrain hero, ICombatWorld world)
        {
            if (_def == null || hero == null || world == null || hero.State == HeroState.Dead) return;
            _timer -= dt;
            if (_timer > 0f) return;
            EnemyBrain target = world.NearestEnemyInRange(_def.Range);
            if (target == null)
            {
                _timer = 0f;
                return;
            }

            _timer = _def.Interval;
            double damage = DamageCalc.SkillHit(hero.Stats, _def.AttackMult * _scale)
                * DamageCalc.SkillCombo(_balance, target.IsStunned, target.HasDot) * target.DamageTakenMult;
            target.TakeDamage(damage);
            world.ReportHit(target, damage, HitKind.Pet);
            if (_def.DotPercent > 0d) target.ApplyDot(DamageCalc.SkillDot(hero.Stats, _def.DotPercent, _scale), _def.DotSeconds);
            if (_def.StunSeconds > 0f) target.Stun(_def.StunSeconds);
            Attacked?.Invoke(target, damage);
        }
    }
}
