using SoloHero.Core.Combat;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Jobs;
using SoloHero.Core.Progression;
using SoloHero.Core.Save;
using SoloHero.Core.Settings;
using SoloHero.Core.Stage;
using SoloHero.Game.Audio;
using SoloHero.Game.Pooling;
using SoloHero.Game.UI.Common;
using SoloHero.Game.UI.Panels;
using SoloHero.Game.View;
using UnityEngine;

namespace SoloHero.Game.Combat
{
    /// <summary>
    /// Combat juice (E8-08, E8-11, E8-13): listens to Core and the combat view and plays pooled VFX, SFX, camera
    /// shake and music. It never changes combat. Low-effect mode (E8-15) drops hit sparks, death puffs, shake and
    /// the skill screen flash; damage numbers, hit flashes, skill and level-up effects stay because they carry
    /// information. D-096: hit-stop on crits and kills, a bigger sword wave on the basic skill.
    /// D-112: pixel shards burst from every hit (more and gold on crits) and a coloured spray from every kill.
    /// D-146: skills (casts, impacts, skill hits and their crits) moved to SkillFx; the job's main attack draws over a
    /// faint additive glow. D-171: progress moments get their payoff here - LEVEL UP over the hero with the level's
    /// rewards flying from it, and a ribbon + fanfare for a chapter's first clear, a new tier, a job advance and a
    /// finished collection set, their gems and tickets flying to the counters (RewardFx).
    /// </summary>
    public sealed class CombatFx : MonoBehaviour
    {
        private const float EffectY = 0.7f;
        private const string BasicWaveClip = "wave";
        private const float BasicWaveOffset = 1.1f;
        private const float BasicWaveScale = 1.35f;
        private const float CritStop = 0.045f;
        private const float KillStop = 0.06f;
        private const float BossKillStop = 0.18f;
        private const float CritShake = 0.06f;
        private const float CritShakeSeconds = 0.12f;
        private const float BossShake = 0.18f;
        private const float MainGlowAlpha = 0.38f;
        private static readonly Color CritTint = new Color(1f, 0.85f, 0.3f, 1f);
        private static readonly Color LevelTint = new Color(0.55f, 1f, 0.6f, 1f);
        private static readonly Color ComboTint = new Color(0.86f, 0.55f, 1f, 1f);
        private static readonly Color TierOpenColor = new Color(1f, 0.42f, 0.3f, 1f);
        private const float TierOpenSeconds = 2.4f;
        private static readonly Color LevelUpText = new Color(0.6f, 1f, 0.55f, 1f);
        private static readonly Color ChapterClearColor = new Color(1f, 0.8f, 0.28f, 1f);
        private static readonly Color JobColor = new Color(1f, 0.86f, 0.4f, 1f);
        private static readonly Color JobFlash = new Color(1f, 0.95f, 0.7f, 1f);
        private static readonly Color SetColor = new Color(0.62f, 0.85f, 1f, 1f);
        // D-178: "X opened!" after the chapter-clear ribbon, in the tier-open red so it reads as a new door.
        private static readonly Color SystemOpenColor = new Color(0.55f, 0.9f, 1f, 1f);
        private const float SystemOpenDelay = 0.6f;
        private readonly SystemGate[] _openedGates = new SystemGate[SystemUnlocks.All.Length];
        private const float JobRingScale = 3f;
        private const float SetPollSeconds = 1f;
        private const float ComboFxGap = 0.2f;
        // Warm, saturated shards read on both the bright meadow and the dark dusk chapters (white vanished on snow).
        private const float ShardSize = 0.14f;
        private static readonly Color HitShard = new Color(1f, 0.88f, 0.4f, 1f);
        private static readonly Color CritShard = new Color(1f, 0.55f, 0.12f, 1f);
        private static readonly Color KillShardA = new Color(1f, 0.62f, 0.2f, 1f);
        private static readonly Color KillShardB = new Color(1f, 0.95f, 0.55f, 1f);
        private float _lastComboFx = -1f;

        [SerializeField] private CombatSession _session;
        [SerializeField] private CombatWorldView _view;
        [SerializeField] private VfxPool _vfx;
        [SerializeField] private VfxSet _set;
        [SerializeField] private CameraShake _shake;
        [SerializeField] private ChapterThemeSet _themes;
        [SerializeField] private ToastQueue _toast;
        [SerializeField] private RewardFx _rewardFx;
        [SerializeField] private HitParticles _particles;

        [Tooltip("D-153: the ribbon that announces a new difficulty tier (found in the scene when empty).")]
        [SerializeField] private SkillCutIn _cutIn;

        private AudioService _audio;
        private SettingsService _settings;
        private SaveDataV2 _save;
        private BalanceValues _balance;
        private StageRunner _hooked;
        private int _shownLevel = -1;
        private double _shownGem = -1d;
        private int _musicChapter = -1;
        private bool _bossMusic;
        private string _shownJob;
        private int _shownSets = -1;
        private float _setPoll;

        private bool LowEffect => _settings != null && _settings.LowEffect;

        private void Awake()
        {
            _audio = Get<AudioService>();
            _settings = Get<SettingsService>();
            _save = Get<SaveDataV2>();
            _balance = Get<BalanceValues>() ?? new BalanceValues();
            if (_cutIn == null) _cutIn = FindObjectOfType<SkillCutIn>(true);
        }

        private void OnEnable()
        {
            if (_view == null) return;
            _view.EnemyDied += OnEnemyDied;
            _view.HeroHurt += OnHeroHurt;
        }

        private void OnDisable()
        {
            if (_view != null)
            {
                _view.EnemyDied -= OnEnemyDied;
                _view.HeroHurt -= OnHeroHurt;
            }

            Unhook();
        }

        private void LateUpdate()
        {
            StageRunner runner = _session != null ? _session.Runner : null;
            if (runner != _hooked)
            {
                Unhook();
                Hook(runner);
            }

            if (runner == null) return;
            UpdateMusic(runner);
            CheckLevelUp(runner);
            CheckJob(runner);
            CheckCollection();
            if (_save != null) _shownGem = _save.gem;
        }

        private void Hook(StageRunner runner)
        {
            _hooked = runner;
            if (runner == null) return;
            runner.World.HitLanded += OnHitLanded;
            runner.Hero.AttackRequested += OnBasicSwing;
            runner.StateChanged += OnStateChanged;
            runner.StageCleared += OnStageCleared;
            runner.RaidWarned += OnRaidWarned;
            runner.RaidStruck += OnRaidStruck;
            _shownLevel = _save != null ? _save.heroLevel : -1;
        }

        private void Unhook()
        {
            if (_hooked == null) return;
            _hooked.World.HitLanded -= OnHitLanded;
            _hooked.Hero.AttackRequested -= OnBasicSwing;
            _hooked.StateChanged -= OnStateChanged;
            _hooked.StageCleared -= OnStageCleared;
            _hooked.RaidWarned -= OnRaidWarned;
            _hooked.RaidStruck -= OnRaidStruck;
            _hooked = null;
        }

        /// <summary>
        /// D-093 basic skill: a sword wave sweeps forward from the hero on every swing. D-104: a job's main attack
        /// plays its own clip and tint instead (bolt, fire, ice, arrow), ranged ones further in front.
        /// </summary>
        private void OnBasicSwing()
        {
            if (_hooked == null || LowEffect || _set == null || _vfx == null) return;
            MainAttack main = _save != null ? JobCatalog.Find(_save.jobId).Main : null;
            VfxClip clip = _set.Find(main != null ? main.Vfx : BasicWaveClip) ?? _set.Find(BasicWaveClip);
            if (clip == null) return;
            float scale = main != null ? main.VfxScale : BasicWaveScale;
            float x = (float)_hooked.Hero.X + (main != null && main.Range > 3d ? (float)main.Range * 0.4f : BasicWaveOffset);
            float y = clip.ground ? clip.halfHeight * scale : EffectY;
            Color tint = main != null && main.Tint != 0 ? Tint(main.Tint) : Color.white;
            if (clip.glow != null && clip.glow.Length > 0)
            {
                Color glow = tint;
                glow.a = MainGlowAlpha;
                _vfx.Play(clip.glow, clip.fps, new Vector3(x, y, 0f), scale, glow, VfxPool.SortingOrder - 1, true);
            }

            _vfx.Play(clip.frames, clip.fps, new Vector3(x, y, 0f), scale, tint);
        }

        private void OnHitLanded(EnemyBrain target, double amount, HitKind kind)
        {
            // Skill hits bring their own clip and sound; burn ticks stay quiet.
            // D-102: PetView plays the pet's own effect; only the hit sound here.
            if (kind == HitKind.Pet)
            {
                Play(SfxId.Hit);
                return;
            }

            if (kind == HitKind.Combo)
            {
                // D-103: one combo burst (sound, stop, sparkle) per moment, not one per wave of a multi-hit skill.
                if (Time.time - _lastComboFx < ComboFxGap) return;
                _lastComboFx = Time.time;
                Play(SfxId.Crit);
                HitStop.Trigger(CritStop);
                // D-108: the spark sheet is 40 px now (was 16), so the scale drops to keep it a punchy accent.
                if (!LowEffect) PlayVfx(_set != null ? _set.spark : null, (float)target.X, 0.9f, ComboTint, EffectY + DepthLanes.For(target));
                return;
            }

            // D-146: skill hits and skill crits are SkillFx's (element sparks); burn ticks stay quiet.
            if (kind == HitKind.Dot || kind == HitKind.Skill || kind == HitKind.SkillCrit) return;

            bool crit = kind == HitKind.Crit;
            Play(crit ? SfxId.Crit : SfxId.Hit);
            if (crit) HitStop.Trigger(CritStop);
            if (LowEffect) return;
            Shards(target, crit ? 8 : 4, crit ? CritShard : HitShard, crit ? 4.8f : 3.4f);
            // D-108: the slash sheet is 56 px (was 32); 1.0 / 1.6 keeps the old reach with a thicker swing.
            PlayVfx(_set != null ? _set.slash : null, (float)target.X, crit ? 1.6f : 1.0f, crit ? CritTint : Color.white, EffectY + DepthLanes.For(target));
            if (crit) Shake(CritShake, CritShakeSeconds);
        }

        private static Color Tint(uint rgb) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);

        private void OnEnemyDied(Vector3 position, bool boss, bool hasDeathClip)
        {
            Play(SfxId.EnemyDeath);
            HitStop.Trigger(boss ? BossKillStop : KillStop);
            if (LowEffect || _set == null) return;
            // Looks without a death clip vanish into a puff; the others get a small puff over their own clip.
            float scale = boss ? 2f : hasDeathClip ? 0.6f : 1f;
            PlayVfx(_set.boom, position.x, scale, Color.white, 0.2f + position.y);
            if (_particles != null)
            {
                Vector3 at = new Vector3(position.x, position.y + 0.5f, 0f);
                _particles.Burst(at, boss ? 18 : 7, KillShardA, boss ? 6f : 4.4f, ShardSize, 80f);
                _particles.Burst(at, boss ? 10 : 4, KillShardB, boss ? 5f : 3.6f, ShardSize * 0.8f, 80f);
            }

            if (boss) Shake(BossShake, 0.4f);
        }

        /// <summary>D-112: a few pixel shards thrown up from a struck enemy (none in low-effect mode).</summary>
        private void Shards(EnemyBrain target, int count, Color color, float speed)
        {
            if (_particles == null || LowEffect || target == null) return;
            _particles.Burst(new Vector3((float)target.X, EffectY + DepthLanes.For(target), 0f), count, color, speed, ShardSize);
        }

        private void OnHeroHurt() => Play(SfxId.HeroHurt);

        private void OnStageCleared(int g)
        {
            Play(SfxId.Gold);
            if (_hooked == null || !_hooked.IsBoss || _save == null || _shownGem < 0d) return;
            // A chapter's first boss kill pays CHAPTER_CLEAR_GEM (StageRunner applies it before this event).
            double gained = _save.gem - _shownGem;
            if (gained <= 0d) return;
            StageIndex.FromGlobal(g, _balance.STAGES_PER_CHAPTER, out int chapter, out _);
            Play(SfxId.GradeEpic);
            // D-153 (L-03): the last boss of a tier also opens the next tier, with its own gift.
            bool tierBoss = DifficultyTier.IsLastBoss(_balance, g) && gained >= _balance.TIER_CLEAR_GEM;
            // D-171: the first-clear gems (and a tier's tickets) fly from the field to their counters.
            if (_rewardFx != null)
            {
                _rewardFx.Play(RewardKind.Gem, gained, RewardFx.BattleCenter);
                if (tierBoss) _rewardFx.Play(RewardKind.Ticket, _balance.TIER_CLEAR_GEAR_TICKETS + _balance.TIER_CLEAR_SKILL_TICKETS, RewardFx.BattleCenter, 1);
            }

            // D-178: the systems this first clear opens (dungeon, tower, pet summon ...) get their own ribbon after the
            // chapter's, so the player learns the new door the moment it opens (the rail lock comes off at the same time).
            int opened = !_hooked.InDungeon ? SystemUnlocks.OpenedBy(_balance, g, _openedGates) : 0;
            if (opened > 0) StartCoroutine(SystemOpenRibbon(opened, TierOpenSeconds + SystemOpenDelay));

            if (tierBoss)
            {
                string tier = DifficultyTier.Name(DifficultyTier.Of(_balance, g + 1));
                // The boss kill often fires the ultimate's band too: the ribbon waits for it and holds a little longer.
                Ribbon(Strings.Format("tier.open", tier), TierOpenColor, TierOpenSeconds);
                if (_toast != null)
                    _toast.Show(Strings.Format("toast.tier_clear", tier, gained, _balance.TIER_CLEAR_GEAR_TICKETS, _balance.TIER_CLEAR_SKILL_TICKETS));
                return;
            }

            string label = DifficultyTier.ChapterLabel(_balance, chapter);
            Ribbon(Strings.Format("fx.chapter_clear", label), ChapterClearColor, TierOpenSeconds);
            // The ribbon and the flying "+N" say it all; the toast is the fallback without the reward layer.
            if (_rewardFx == null && _toast != null) _toast.Show(Strings.Format("toast.chapter_clear", label, gained));
        }

        /// <summary>D-178: names the systems a first clear opened ("dungeon, tower, pet summon opened!") once the chapter ribbon is gone.</summary>
        private System.Collections.IEnumerator SystemOpenRibbon(int count, float delay)
        {
            var names = new System.Text.StringBuilder();
            for (int i = 0; i < count; i++)
            {
                if (i > 0) names.Append(' ').Append(Strings.Get("fx.system_sep")).Append(' ');
                names.Append(Strings.Get(SystemUnlocks.NameKey(_openedGates[i])));
            }

            string text = Strings.Format("fx.system_open", names.ToString());
            yield return new WaitForSecondsRealtime(delay);
            Ribbon(text, SystemOpenColor, TierOpenSeconds);
            if (_toast != null) _toast.Show(text);
        }

        private void OnStateChanged(StageState state)
        {
            if (state != StageState.BossIntro) return;
            Play(SfxId.BossIntro);
            Shake(BossShake, _balance.BOSS_INTRO_TIME * 0.5f);
        }

        private void UpdateMusic(StageRunner runner)
        {
            if (_audio == null) return;
            // D-100: a dungeon run plays the boss track too - it is the short, intense part of the day.
            bool boss = runner.InDungeon || (runner.IsBoss && (runner.State == StageState.BossIntro || runner.State == StageState.BossTimer));
            StageIndex.FromGlobal(runner.GlobalStage, _balance.STAGES_PER_CHAPTER, out int chapter, out _);
            if (boss == _bossMusic && chapter == _musicChapter) return;
            _bossMusic = boss;
            _musicChapter = chapter;
            if (boss)
            {
                _audio.PlayBossBgm();
                return;
            }

            if (_themes == null || _themes.themes.Length == 0) return;
            ChapterTheme theme = _themes.themes[StageIndex.ThemeIndex(chapter, _themes.themes.Length)];
            if (theme != null) _audio.PlayBgm(theme.bgm);
        }

        private void CheckLevelUp(StageRunner runner)
        {
            if (_save == null) return;
            if (_shownLevel < 0) _shownLevel = _save.heroLevel;
            if (_save.heroLevel <= _shownLevel) return;
            int from = _shownLevel;
            _shownLevel = _save.heroLevel;
            Play(SfxId.LevelUp);
            if (_set != null) PlayVfx(_set.ring, (float)runner.Hero.X, 2.2f, LevelTint, 0.4f);
            Vector2 head = HeroScreen();
            // D-171: LEVEL UP rises over the hero (the toast stays for the reward levels).
            _rewardFx?.Label(Strings.Get("fx.level_up"), head, LevelUpText);

            // D-154: a reward level shows what HeroLevelService paid for it.
            int gems = 0, gear = 0, rare = 0, stones = 0;
            for (int level = from + 1; level <= _shownLevel; level++)
            {
                LevelRewardPay pay = LevelReward.For(_balance, level);
                gems += pay.Gems;
                gear += pay.GearTickets;
                rare += pay.SkillTickets;
                stones += pay.Stones;
            }

            if (gems <= 0 && gear <= 0)
            {
                if (_rewardFx == null) _toast?.Show(Strings.Format("toast.level_up", _shownLevel));
                return;
            }

            if (_rewardFx != null)
            {
                _rewardFx.Play(RewardKind.Gem, gems, head, 1);
                _rewardFx.Play(RewardKind.Ticket, gear + rare, head, 2);
                _rewardFx.Play(RewardKind.Stone, stones, head, 3);
            }

            if (_toast == null) return;
            if (rare > 0) _toast.Show(Strings.Format("toast.level_reward_big", _shownLevel, gems, gear, rare, stones));
            else _toast.Show(Strings.Format("toast.level_reward", _shownLevel, gems, gear, stones));
        }

        /// <summary>D-171: a job advance (the save's job changes): a golden ring, the hero glows, ribbon and fanfare.</summary>
        private void CheckJob(StageRunner runner)
        {
            if (_save == null) return;
            string job = _save.jobId ?? "";
            if (_shownJob == null)
            {
                _shownJob = job;
                return;
            }

            if (job == _shownJob) return;
            _shownJob = job;
            JobDef def = JobCatalog.Find(job);
            if (def == null || def.Tier < 1) return;
            if (_view != null) _view.FlashHero(JobFlash, 0.9f);
            if (_set != null) PlayVfx(_set.ring, (float)runner.Hero.X, JobRingScale, JobColor, 0.4f);
            Ribbon(Strings.Format("fx.job_advance", Strings.Get(def.NameKey)), JobColor, TierOpenSeconds);
        }

        /// <summary>D-171: a collection set completes when its last piece is pulled; celebrate it once.</summary>
        private void CheckCollection()
        {
            if (_save == null) return;
            _setPoll -= Time.unscaledDeltaTime;
            if (_setPoll > 0f) return;
            _setPoll = SetPollSeconds;
            int done = CollectionCatalog.CompleteCount(_save);
            bool more = _shownSets >= 0 && done > _shownSets;
            _shownSets = done;
            if (!more) return;
            Ribbon(Strings.Get("fx.set_complete"), SetColor, TierOpenSeconds);
            _toast?.Show(Strings.Get("toast.set_complete"));
        }

        /// <summary>The big-moment ribbon with flash and fanfare; without the reward layer, the plain ribbon.</summary>
        private void Ribbon(string text, Color color, float seconds)
        {
            if (_rewardFx != null) _rewardFx.Celebrate(text, color, seconds);
            else if (_cutIn != null) _cutIn.PlayText(text, color, true, seconds);
        }

        private static readonly Color RaidWarnColor = new Color(1f, 0.36f, 0.3f, 1f);
        private static readonly Color RaidSummonColor = new Color(0.72f, 0.42f, 1f, 1f);

        /// <summary>
        /// D-175: a raid move's warning - the alarm, its name over the boss, and for the slam a red circle as wide as its
        /// reach (step back out of it, ranged heroes already stand clear); the summon draws a violet circle.
        /// </summary>
        private void OnRaidWarned(RaidPattern move)
        {
            Play(SfxId.Warn);
            Vector3 feet = _view != null ? _view.BossFeet : Vector3.zero;
            _rewardFx?.Label(Strings.Get(RaidWarnKey(move)), RewardFx.ScreenOf(feet + new Vector3(0f, 2.6f, 0f)), RaidWarnColor);
            if (LowEffect || _set == null) return;
            if (move == RaidPattern.Slam)
            {
                VfxClip circle = _set.Find("circle4");
                Color red = RaidWarnColor;
                red.a = 0.85f;
                if (circle != null) _vfx?.Play(circle.frames, circle.fps * 0.6f, new Vector3(feet.x, 0f, 0f), RaidCircleScale(), red, 3, false);
            }
            else if (move == RaidPattern.Summon)
            {
                VfxClip circle = _set.Find("circle3");
                if (circle != null) _vfx?.Play(circle.frames, circle.fps * 0.6f, new Vector3(feet.x, 0f, 0f), 1.3f, RaidSummonColor, 3, false);
            }
        }

        /// <summary>D-175: a landed move - the slam's shock ring, the charge's lunge, the summon's flash, and a shake.</summary>
        private void OnRaidStruck(RaidPattern move, double damage)
        {
            Vector3 feet = _view != null ? _view.BossFeet : Vector3.zero;
            switch (move)
            {
                case RaidPattern.Slam:
                    Play(SfxId.Slam);
                    if (!LowEffect && _set != null)
                    {
                        VfxClip shock = _set.Find("shock");
                        if (shock != null) _vfx?.Play(shock.frames, shock.fps, new Vector3(feet.x, 0.3f, 0f), RaidCircleScale(), RaidWarnColor, 4, false);
                    }

                    Shake(BossShake * 1.2f, 0.35f);
                    break;
                case RaidPattern.Charge:
                    Play(SfxId.Slam);
                    _view?.LungeBoss(1.6f);
                    Shake(BossShake, 0.3f);
                    break;
                case RaidPattern.Summon:
                    Play(SfxId.SkillMagic);
                    if (!LowEffect && _set != null)
                    {
                        VfxClip flash = _set.Find("flash");
                        if (flash != null) _vfx?.Play(flash.frames, flash.fps, new Vector3(feet.x, EffectY, 0f), 1.6f, RaidSummonColor, VfxPool.SortingOrder, true);
                    }

                    break;
            }
        }

        /// <summary>The magic circle art is about 2.4 units wide at scale 1; the slam reaches RAID_SLAM_RANGE either side.</summary>
        private float RaidCircleScale() => (float)(_balance.RAID_SLAM_RANGE * 2d / 2.4d);

        private static string RaidWarnKey(RaidPattern move) =>
            move == RaidPattern.Slam ? "raid.warn_slam" : move == RaidPattern.Charge ? "raid.warn_charge" : "raid.warn_summon";

        private Vector2 HeroScreen() => _view != null ? RewardFx.ScreenOf(_view.HeroHead) : RewardFx.BattleCenter;

        private void PlayVfx(Sprite[] clip, float x, float scale, Color tint, float y = EffectY)
        {
            if (_vfx == null || clip == null) return;
            _vfx.Play(clip, _set != null ? _set.fps : 18f, new Vector3(x, y, 0f), scale, tint);
        }

        private void Shake(float amplitude, float seconds)
        {
            if (_shake == null || LowEffect) return;
            _shake.Shake(amplitude, seconds);
        }

        private void Play(SfxId id)
        {
            if (_audio != null) _audio.Play(id);
        }

        private static T Get<T>() where T : class
        {
            try
            {
                return Services.Get<T>();
            }
            catch (System.InvalidOperationException)
            {
                return null;
            }
        }
    }
}
