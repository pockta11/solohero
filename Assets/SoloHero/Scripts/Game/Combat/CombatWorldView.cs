using System;
using SoloHero.Core.Combat;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Stage;
using SoloHero.Game.View;
using UnityEngine;

namespace SoloHero.Game.Combat
{
    /// <summary>
    /// Draws the combat state (E2, E8-02..04): hero and the 4 enemy slots follow Core positions and play sprite
    /// clips chosen from Core state - run while advancing, attack on each swing, hit when HP drops, dead on death.
    /// Each enemy takes its look from the chapter roster by spawn order (E8-03); looks without a hit or dead clip
    /// flash / vanish instead. Enemies keep their slot renderer for the death clip after Core frees the slot. The hit
    /// lands on the swing (D-065), so animation timing never changes combat results.
    /// </summary>
    public sealed class CombatWorldView : MonoBehaviour
    {
        private const int EnemySlotVisualCount = 4;
        private const float HitFlashSeconds = 0.08f;

        /// <summary>Bosses are drawn this far behind their Core position so the 2x body does not cover the hero (view only).</summary>
        private const float BossDrawOffset = 0.4f;
        private static readonly Color HitFlash = new Color(1f, 0.55f, 0.55f, 1f);
        private static readonly Color FrozenTint = new Color(0.55f, 0.8f, 1f, 1f);
        private static readonly Color BurnTint = new Color(1f, 0.62f, 0.38f, 1f);
        private static readonly Color ShieldTint = new Color(0.78f, 0.92f, 1f, 1f);
        private const float BurnPulseSpeed = 9f;

        [SerializeField] private CombatSession _session;
        [SerializeField] private Camera _camera;
        [SerializeField] private CameraShake _shake;
        [SerializeField] private SpriteRenderer _heroRenderer;
        [SerializeField] private SpriteRenderer[] _enemyRenderers = new SpriteRenderer[EnemySlotVisualCount];

        [Header("Ground shadows (D-082): index 0 hero, 1.. enemy slots")]
        [SerializeField] private SpriteRenderer[] _shadows = new SpriteRenderer[0];
        [SerializeField] private float _shadowWidth = 1.1f;
        [SerializeField] private CharacterArt _heroArt;
        [SerializeField] private CharacterArt _enemyArt;
        [SerializeField] private CharacterArt _bossArt;
        [SerializeField] private ChapterThemeSet _themes;

        [Header("Enemy HP bars (shown once hurt; bosses use the HUD bar)")]
        [SerializeField] private SpriteRenderer[] _hpBacks = new SpriteRenderer[EnemySlotVisualCount];
        [SerializeField] private SpriteRenderer[] _hpFills = new SpriteRenderer[EnemySlotVisualCount];
        [SerializeField] private float _hpBarWidth = 1.1f;
        [SerializeField] private float _hpBarHeight = 0.12f;

        private BalanceValues _balance;
        private StageRunner _hooked;
        private bool _heroAttackPending;
        private double _heroLastHp;
        private float _heroFlash;
        private int _lastKills;
        private float _cameraY = float.NaN;
        private readonly EnemySlotState[] _slots = new EnemySlotState[EnemySlotVisualCount];

        /// <summary>A killed enemy (not one cleared by a stage reset): world position, boss, whether it has a death clip.</summary>
        public event Action<Vector3, bool, bool> EnemyDied;

        /// <summary>The hero lost HP this frame.</summary>
        public event Action HeroHurt;

        private sealed class EnemySlotState
        {
            public bool Active;
            public bool Dying;
            public bool Boss;
            public int Generation;
            public double LastHp;
            public int LastAttacks;
            public float Flash;
            public float X;
            public float Y;
            public CharacterArt Art;
        }

        private void Awake()
        {
            if (_session == null)
                _session = GetComponent<CombatSession>();
            if (_camera == null)
                _camera = Camera.main;
            for (int i = 0; i < _slots.Length; i++) _slots[i] = new EnemySlotState();

            try
            {
                _balance = Services.Get<BalanceValues>();
            }
            catch (Exception)
            {
                _balance = new BalanceValues();
            }
        }

        private void OnDestroy()
        {
            if (_hooked != null) _hooked.Hero.AttackRequested -= OnHeroAttack;
        }

        private void LateUpdate()
        {
            StageRunner runner = _session != null ? _session.Runner : null;
            CombatWorld world = runner != null ? runner.World : null;
            if (runner == null || world == null)
            {
                HideAll();
                return;
            }

            if (_hooked != runner)
            {
                if (_hooked != null) _hooked.Hero.AttackRequested -= OnHeroAttack;
                _hooked = runner;
                _hooked.Hero.AttackRequested += OnHeroAttack;
                _heroLastHp = runner.Hero.Hp;
                _lastKills = runner.Kills;
            }

            // Only slots freed while the kill count rises are kills; a stage reset frees slots without kills.
            int newKills = runner.Kills - _lastKills;
            if (newKills < 0) newKills = 0;
            _lastKills = runner.Kills;

            ChapterTheme theme = ThemeFor(runner.GlobalStage);
            DrawHero(runner.Hero);
            FollowCamera((float)runner.Hero.X);
            for (int i = 0; i < EnemySlotVisualCount; i++)
            {
                EnemyBrain enemy = i < world.SlotCount ? world.GetSlot(i) : null;
                DrawEnemy(i, enemy, theme, ref newKills);
                DrawHpBar(i, enemy);
                DrawShadow(i + 1, i < _enemyRenderers.Length ? _enemyRenderers[i] : null, (_slots[i].Boss ? 1.6f : 1f) * DepthLanes.Scale(_slots[i].Y));
            }

            DrawShadow(0, _heroRenderer, 1f);
        }

        /// <summary>A soft blob on the ground line under a drawn character, sized in world units.</summary>
        private void DrawShadow(int index, SpriteRenderer body, float size)
        {
            SpriteRenderer shadow = index < _shadows.Length ? _shadows[index] : null;
            if (shadow == null) return;
            bool show = body != null && body.enabled;
            if (shadow.enabled != show) shadow.enabled = show;
            if (!show || shadow.sprite == null) return;
            float unit = shadow.sprite.bounds.size.x;
            float width = _shadowWidth * size;
            Vector3 feet = body.transform.position;
            shadow.transform.position = new Vector3(feet.x, feet.y - 0.1f * size, 0f);
            shadow.transform.localScale = new Vector3(width / unit, width / unit, 1f);
        }

        /// <summary>World HP bar over a hurt normal enemy, sized in world units whatever the sprite scale.</summary>
        private void DrawHpBar(int index, EnemyBrain enemy)
        {
            SpriteRenderer back = index < _hpBacks.Length ? _hpBacks[index] : null;
            SpriteRenderer fill = index < _hpFills.Length ? _hpFills[index] : null;
            if (back == null || fill == null) return;
            SpriteRenderer body = index < _enemyRenderers.Length ? _enemyRenderers[index] : null;
            bool show = enemy != null && enemy.IsAlive && !enemy.IsBoss && enemy.Hp < enemy.MaxHp && body != null && body.enabled;
            if (back.enabled != show) back.enabled = show;
            if (fill.enabled != show) fill.enabled = show;
            if (!show || back.sprite == null) return;

            float unit = back.sprite.bounds.size.x;
            float ratio = enemy.MaxHp > 0d ? Mathf.Clamp01((float)(enemy.Hp / enemy.MaxHp)) : 0f;
            CharacterArt art = _slots[index].Art;
            float top = art != null && art.headHeight > 0f
                ? body.transform.position.y + art.headHeight * body.transform.lossyScale.y + 0.14f
                : body.bounds.max.y + 0.12f;
            float left = (float)enemy.X - _hpBarWidth * 0.5f;
            back.transform.position = new Vector3((float)enemy.X, top, 0f);
            back.transform.localScale = new Vector3(_hpBarWidth / unit, _hpBarHeight / unit, 1f);
            float inner = _hpBarWidth - 0.04f;
            fill.transform.position = new Vector3(left + 0.02f + inner * ratio * 0.5f, top, 0f);
            fill.transform.localScale = new Vector3(inner * ratio / unit, (_hpBarHeight - 0.04f) / unit, 1f);
        }

        private void OnHeroAttack() => _heroAttackPending = true;

        private ChapterTheme ThemeFor(int globalStage)
        {
            if (_themes == null || _themes.themes.Length == 0 || _balance == null) return null;
            StageIndex.FromGlobal(globalStage, _balance.STAGES_PER_CHAPTER, out int chapter, out _);
            return _themes.themes[StageIndex.ThemeIndex(chapter, _themes.themes.Length)];
        }

        private void DrawHero(HeroBrain hero)
        {
            if (_heroRenderer == null) return;
            _heroRenderer.enabled = true;
            _heroRenderer.transform.position = new Vector3((float)hero.X, 0f, 0f);
            SetOrder(_heroRenderer, 0f);
            SpriteFlipbook book = Book(_heroRenderer);
            bool tookHit = hero.Hp < _heroLastHp;
            _heroLastHp = hero.Hp;

            if (_heroArt != null && book != null)
            {
                SetScale(_heroRenderer, _heroArt.pixelScale);
                if (hero.State == HeroState.Dead)
                {
                    book.Play(_heroArt.dead, _heroArt.fps, loop: false);
                }
                else if (_heroAttackPending)
                {
                    // Faster swings play the clip faster so it never lags behind the attack speed.
                    float fps = Mathf.Max(_heroArt.fps, _heroArt.attack.Length * (float)hero.Stats.AtkSpd);
                    book.Play(_heroArt.attack, fps, loop: false, restart: true);
                }
                else if (tookHit && (book.Current != _heroArt.attack || book.Finished))
                {
                    book.Play(_heroArt.hit, _heroArt.fps, loop: false, restart: true);
                }
                else if (book.Current == _heroArt.dead || book.Finished || IsLoop(book.Current))
                {
                    book.Play(hero.State == HeroState.Advance ? _heroArt.run : _heroArt.idle, _heroArt.fps, loop: true);
                }
            }

            _heroAttackPending = false;
            if (tookHit)
            {
                _heroFlash = HitFlashSeconds;
                HeroHurt?.Invoke();
            }

            _heroFlash -= Time.deltaTime;
            _heroRenderer.color = _heroFlash > 0f ? HitFlash : hero.Shield > 0d ? ShieldTint : Color.white;
        }

        private bool IsLoop(Sprite[] clip) =>
            _heroArt != null && (clip == _heroArt.idle || clip == _heroArt.run);

        private void DrawEnemy(int index, EnemyBrain enemy, ChapterTheme theme, ref int newKills)
        {
            SpriteRenderer renderer = index < _enemyRenderers.Length ? _enemyRenderers[index] : null;
            if (renderer == null) return;
            EnemySlotState slot = _slots[index];
            SpriteFlipbook book = Book(renderer);

            // A slot freed and refilled inside one Core tick shows up as a new generation: finish the old one first.
            if (enemy != null && enemy.IsActive && slot.Active && slot.Generation != enemy.Generation)
                BeginDeath(slot, renderer, book, ref newKills);

            if (enemy != null && enemy.IsActive)
            {
                bool fresh = !slot.Active || slot.Dying || slot.Generation != enemy.Generation;
                if (fresh) slot.Art = ArtFor(enemy, theme);
                CharacterArt art = slot.Art;
                slot.Active = true;
                slot.Dying = false;
                slot.Boss = enemy.IsBoss;
                slot.Generation = enemy.Generation;
                slot.X = (float)enemy.X + (enemy.IsBoss ? BossDrawOffset : 0f);
                slot.Y = DepthLanes.For(enemy);
                renderer.enabled = true;
                renderer.transform.position = new Vector3(slot.X, slot.Y, 0f);
                SetOrder(renderer, slot.Y);

                bool attacked = enemy.AttackCount != slot.LastAttacks;
                bool hurt = !fresh && enemy.Hp < slot.LastHp;
                slot.LastAttacks = enemy.AttackCount;
                slot.LastHp = enemy.Hp;
                if (hurt) slot.Flash = HitFlashSeconds;

                if (art != null && book != null)
                {
                    SetScale(renderer, art.pixelScale * DepthLanes.Scale(slot.Y));
                    if (fresh) book.Play(art.idle, art.fps, loop: true, restart: true);
                    else if (attacked) book.Play(art.attack, art.fps, loop: false, restart: true);
                    else if (hurt && art.hit.Length > 0 && (book.Current != art.attack || book.Finished)) book.Play(art.hit, art.fps, loop: false, restart: true);
                    else if (book.Finished) book.Play(art.idle, art.fps, loop: true);
                }
            }
            else if (slot.Active)
            {
                // Core freed the slot this frame: keep drawing it until the death clip ends.
                BeginDeath(slot, renderer, book, ref newKills);
            }
            else if (slot.Dying && (book == null || book.Finished))
            {
                slot.Dying = false;
                renderer.enabled = false;
            }
            else if (!slot.Dying)
            {
                renderer.enabled = false;
            }

            slot.Flash -= Time.deltaTime;
            renderer.color = slot.Flash > 0f ? HitFlash : StatusTint(enemy);
        }

        /// <summary>D-078 status on the body: frozen / stunned blue, burning or poisoned pulses orange.</summary>
        private static Color StatusTint(EnemyBrain enemy)
        {
            if (enemy == null || !enemy.IsAlive) return Color.white;
            if (enemy.IsStunned) return FrozenTint;
            if (!enemy.HasDot) return Color.white;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * BurnPulseSpeed);
            return Color.Lerp(Color.white, BurnTint, pulse);
        }

        private void BeginDeath(EnemySlotState slot, SpriteRenderer renderer, SpriteFlipbook book, ref int newKills)
        {
            slot.Active = false;
            CharacterArt art = slot.Art;
            bool hasClip = art != null && art.dead.Length > 0 && book != null;
            if (hasClip)
            {
                slot.Dying = true;
                book.Play(art.dead, art.fps, loop: false, restart: true);
            }
            else
            {
                slot.Dying = false;
                renderer.enabled = false;
            }

            if (newKills <= 0) return;
            newKills--;
            EnemyDied?.Invoke(new Vector3(slot.X, slot.Y, 0f), slot.Boss, hasClip);
        }

        private CharacterArt ArtFor(EnemyBrain enemy, ChapterTheme theme)
        {
            if (enemy.IsBoss) return theme != null && theme.boss != null ? theme.boss : _bossArt;
            CharacterArt art = theme != null ? theme.EnemyFor(enemy.SpawnIndex) : null;
            return art != null ? art : _enemyArt;
        }

        private static SpriteFlipbook Book(SpriteRenderer renderer) =>
            renderer != null ? renderer.GetComponent<SpriteFlipbook>() : null;

        /// <summary>D-091: characters sort by depth lane (shadows stay under everyone).</summary>
        private static void SetOrder(SpriteRenderer renderer, float y)
        {
            int order = DepthLanes.Order(y);
            if (renderer.sortingOrder != order) renderer.sortingOrder = order;
        }

        private static void SetScale(SpriteRenderer renderer, float scale)
        {
            float s = scale <= 0f ? 1f : scale;
            Transform t = renderer.transform;
            if (t.localScale.x != s) t.localScale = new Vector3(s, s, 1f);
        }

        private void FollowCamera(float heroX)
        {
            if (_camera == null || _balance == null) return;

            float viewWidth = (float)_balance.PIXEL_REF_WIDTH / _balance.PPU;
            float cameraX = heroX + (0.5f - _balance.HERO_SCREEN_X / 100f) * viewWidth;
            Vector3 pos = _camera.transform.position;
            if (float.IsNaN(_cameraY)) _cameraY = pos.y;
            Vector2 shake = _shake != null ? _shake.Offset : Vector2.zero;
            pos.x = cameraX + shake.x;
            pos.y = _cameraY + shake.y;
            _camera.transform.position = pos;
        }

        private void HideAll()
        {
            if (_heroRenderer != null)
                _heroRenderer.enabled = false;

            for (int i = 0; i < _hpBacks.Length; i++)
            {
                if (_hpBacks[i] != null) _hpBacks[i].enabled = false;
                if (i < _hpFills.Length && _hpFills[i] != null) _hpFills[i].enabled = false;
            }

            if (_enemyRenderers == null) return;
            for (int i = 0; i < _enemyRenderers.Length; i++)
            {
                if (_enemyRenderers[i] != null)
                    _enemyRenderers[i].enabled = false;
            }
        }
    }
}
