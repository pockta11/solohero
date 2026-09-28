namespace SoloHero.Game.Audio
{
    /// <summary>
    /// Every sound effect (GDD Audio: 8 combat + 9 UI, plus level-up). The value indexes
    /// <see cref="SoundBank.sfx"/>; append new ids at the end so existing bank slots keep their clips.
    /// </summary>
    public enum SfxId
    {
        Hit = 0,
        Crit = 1,
        HeroHurt = 2,
        EnemyDeath = 3,
        BossIntro = 4,
        Skill1 = 5,
        Skill2 = 6,
        Skill3 = 7,
        Tap = 8,
        PanelOpen = 9,
        PanelClose = 10,
        Upgrade = 11,
        LevelUp = 12,
        CardFlip = 13,
        GradeEpic = 14,
        GradeLegendary = 15,
        Gold = 16,
        Toast = 17,
        SkillFire = 18,
        SkillThunder = 19,
        SkillIce = 20,
        SkillHeal = 21,
        SkillMagic = 22,
    }
}
