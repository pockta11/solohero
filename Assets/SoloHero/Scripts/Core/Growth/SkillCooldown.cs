using System;
using SoloHero.Core.Common;
using SoloHero.Core.Config;

namespace SoloHero.Core.Growth
{
    public sealed class SkillCooldown
    {
        private float _remaining1;
        private float _remaining2;
        private float _remaining3;

        public void Tick(float dt)
        {
            if (dt <= 0f) return;
            _remaining1 = TickOne(_remaining1, dt);
            _remaining2 = TickOne(_remaining2, dt);
            _remaining3 = TickOne(_remaining3, dt);
        }

        public float Remaining(SkillSlot slot)
        {
            switch (slot)
            {
                case SkillSlot.Slot1: return _remaining1;
                case SkillSlot.Slot2: return _remaining2;
                case SkillSlot.Slot3: return _remaining3;
                default: throw new ArgumentOutOfRangeException(nameof(slot));
            }
        }

        public bool IsReady(SkillSlot slot) => Remaining(slot) <= 0f;

        public static float Duration(BalanceValues balance, SkillSlot slot)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            switch (slot)
            {
                case SkillSlot.Slot1: return balance.SKILL_CD_1;
                case SkillSlot.Slot2: return balance.SKILL_CD_2;
                case SkillSlot.Slot3: return balance.SKILL_CD_3;
                default: throw new ArgumentOutOfRangeException(nameof(slot));
            }
        }

        public Result TryBegin(SkillSlot slot, BalanceValues balance)
        {
            if (!IsReady(slot)) return Result.Fail(FailReason.OnCooldown);
            float duration = Duration(balance, slot);
            SetRemaining(slot, duration);
            return Result.Success;
        }

        public void ResetAll()
        {
            _remaining1 = 0f;
            _remaining2 = 0f;
            _remaining3 = 0f;
        }

        private void SetRemaining(SkillSlot slot, float value)
        {
            switch (slot)
            {
                case SkillSlot.Slot1: _remaining1 = value; break;
                case SkillSlot.Slot2: _remaining2 = value; break;
                case SkillSlot.Slot3: _remaining3 = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(slot));
            }
        }

        private static float TickOne(float remaining, float dt)
        {
            if (remaining <= 0f) return 0f;
            remaining -= dt;
            return remaining < 0f ? 0f : remaining;
        }
    }
}
