using System;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;

namespace SoloHero.Core.Combat
{
    public sealed class SkillAutoCaster
    {
        private readonly BalanceValues _balance;
        private readonly float[] _cooldown;
        private readonly int[] _levels;
        private float _sequenceGap;
        private double _atkBuffRemaining;
        private double _atkBuffAmount;

        public SkillAutoCaster(BalanceValues balance)
        {
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _cooldown = new float[3];
            _levels = new int[] { 1, 1, 1 };
            _sequenceGap = 0f;
        }

        public double AtkBuffSum => _atkBuffRemaining > 0d ? _atkBuffAmount : 0d;

        public void SetLevel(SkillSlot slot, int level)
        {
            int i = (int)slot;
            if (i < 0 || i >= _levels.Length) return;
            if (level < 1) level = 1;
            if (level > _balance.SKILL_MAX_LEVEL) level = _balance.SKILL_MAX_LEVEL;
            _levels[i] = level;
        }

        public void SetCooldown(SkillSlot slot, float seconds)
        {
            int i = (int)slot;
            if (i < 0 || i >= _cooldown.Length) return;
            _cooldown[i] = seconds < 0f ? 0f : seconds;
        }

        public float CooldownRemaining(SkillSlot slot)
        {
            int i = (int)slot;
            if (i < 0 || i >= _cooldown.Length) return 0f;
            return _cooldown[i];
        }

        public void ResetCooldowns()
        {
            for (int i = 0; i < _cooldown.Length; i++)
                _cooldown[i] = 0f;
            _sequenceGap = 0f;
            _atkBuffRemaining = 0d;
            _atkBuffAmount = 0d;
        }

        public void Tick(float dt, HeroBrain hero, ICombatWorld world, bool isBossFight)
        {
            for (int i = 0; i < _cooldown.Length; i++)
            {
                if (_cooldown[i] > 0f) _cooldown[i] -= dt;
            }

            if (_sequenceGap > 0f) _sequenceGap -= dt;
            if (_atkBuffRemaining > 0d)
            {
                _atkBuffRemaining -= dt;
                if (_atkBuffRemaining < 0d) _atkBuffRemaining = 0d;
            }

            if (hero == null || world == null) return;
            if (hero.State == HeroState.Dead || hero.State == HeroState.Skill) return;
            if (_sequenceGap > 0f) return;

            for (int i = 0; i < 3; i++)
            {
                if (_cooldown[i] > 0f) continue;
                if (!MeetsAutoCondition((SkillSlot)i, hero, world, isBossFight)) continue;
                if (TryCast((SkillSlot)i, hero, world, skipCondition: false).Ok)
                    return;
            }
        }

        public Result TryCast(SkillSlot slot, HeroBrain hero, ICombatWorld world, bool skipCondition = true)
        {
            if (hero == null || world == null) return Result.Fail(FailReason.Busy);
            if (hero.State == HeroState.Dead) return Result.Fail(FailReason.Busy);
            if (hero.State == HeroState.Skill) return Result.Fail(FailReason.Busy);

            int i = (int)slot;
            if (i < 0 || i >= _cooldown.Length) return Result.Fail(FailReason.Locked);
            if (_cooldown[i] > 0f) return Result.Fail(FailReason.OnCooldown);
            if (!skipCondition && !MeetsAutoCondition(slot, hero, world, false))
                return Result.Fail(FailReason.Busy);

            Result begin = hero.TryBeginSkill();
            if (!begin.Ok) return begin;

            ApplySkill(slot, hero, world);
            _cooldown[i] = CooldownOf(slot);
            _sequenceGap = _balance.SKILL_SEQUENCE_GAP;
            hero.EndSkill(world);
            return Result.Success;
        }

        private bool MeetsAutoCondition(SkillSlot slot, HeroBrain hero, ICombatWorld world, bool isBossFight)
        {
            switch (slot)
            {
                case SkillSlot.Slot1:
                    return world.HasEnemyInRange(_balance.ATTACK_RANGE);
                case SkillSlot.Slot2:
                    return world.CountEnemiesInRange(_balance.WHIRLWIND_RANGE) >= _balance.WHIRLWIND_MIN_TARGETS;
                case SkillSlot.Slot3:
                    double threshold = hero.MaxHp * (_balance.BATTLECRY_HP_THRESHOLD * 0.01d);
                    return isBossFight || hero.Hp <= threshold;
                default:
                    return false;
            }
        }

        private void ApplySkill(SkillSlot slot, HeroBrain hero, ICombatWorld world)
        {
            int level = _levels[(int)slot];
            double gain = 1d + _balance.SKILL_LEVEL_GAIN * 0.01d * (level - 1);

            switch (slot)
            {
                case SkillSlot.Slot1:
                {
                    EnemyBrain target = world.NearestEnemyInRange(_balance.ATTACK_RANGE);
                    if (target == null) return;
                    double dmg = DamageCalc.SkillHit(hero.Stats, _balance.SKILL_MULT_1 * gain);
                    target.TakeDamage(dmg);
                    break;
                }
                case SkillSlot.Slot2:
                {
                    double dmg = DamageCalc.SkillHit(hero.Stats, _balance.SKILL_MULT_2 * gain);
                    int slots = world.SlotCount;
                    for (int s = 0; s < slots; s++)
                    {
                        EnemyBrain e = world.GetSlot(s);
                        if (e == null || !e.IsAlive) continue;
                        if (e.X < world.HeroX) continue;
                        if (e.X - world.HeroX > _balance.WHIRLWIND_RANGE) continue;
                        e.TakeDamage(dmg);
                    }

                    break;
                }
                case SkillSlot.Slot3:
                {
                    double heal = hero.MaxHp * (_balance.BATTLECRY_HEAL * 0.01d);
                    hero.Heal(heal);
                    _atkBuffAmount = _balance.BATTLECRY_ATK_BUFF * 0.01d;
                    _atkBuffRemaining = _balance.BATTLECRY_DURATION;
                    break;
                }
            }
        }

        private float CooldownOf(SkillSlot slot)
        {
            switch (slot)
            {
                case SkillSlot.Slot1: return _balance.SKILL_CD_1;
                case SkillSlot.Slot2: return _balance.SKILL_CD_2;
                case SkillSlot.Slot3: return _balance.SKILL_CD_3;
                default: return 0f;
            }
        }
    }
}
