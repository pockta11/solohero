using SoloHero.Core.Combat;
using SoloHero.Core.Pets;
using SoloHero.Core.Stage;
using SoloHero.Game.Pooling;
using SoloHero.Game.View;
using UnityEngine;

namespace SoloHero.Game.Combat
{
    /// <summary>
    /// D-102 / D-114: draws the equipped pet floating behind the hero's shoulder with a gentle bob, plays its attack
    /// clip and the def's VFX on the target when it strikes. View only - Core decides when and whom it hits.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public sealed class PetView : MonoBehaviour
    {
        private const float BehindHero = 0.85f;
        private const float FloatY = 0.35f;
        private const float BobHeight = 0.08f;
        private const float BobSpeed = 3f;

        [SerializeField] private CombatSession _session;
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private VfxPool _vfx;
        [SerializeField] private VfxSet _set;

        [Tooltip("Index-aligned with PetCatalog.")]
        [SerializeField] private CharacterArt[] _arts = new CharacterArt[0];

        private SpriteFlipbook _book;
        private StageRunner _hooked;
        private PetDef _shown;
        private bool _attackPending;

        private void Awake()
        {
            if (_renderer != null) _book = _renderer.GetComponent<SpriteFlipbook>();
            if (_book != null) _book.FreezeOnHitStop = true;
            if (_renderer != null) _renderer.enabled = false;
        }

        private void OnDisable() => Unhook();

        private void Unhook()
        {
            if (_hooked != null) _hooked.Pet.Attacked -= OnAttacked;
            _hooked = null;
        }

        private void OnAttacked(EnemyBrain target, double damage)
        {
            _attackPending = true;
            PetDef def = _hooked != null ? _hooked.Pet.Def : null;
            if (def == null || _vfx == null || _set == null) return;
            Color tint = new Color(((def.Tint >> 16) & 0xFF) / 255f, ((def.Tint >> 8) & 0xFF) / 255f, (def.Tint & 0xFF) / 255f, 1f);
            VfxClip clip = _set.Find(def.Vfx);
            Vector3 at = new Vector3((float)target.X, 0.7f + DepthLanes.For(target), 0f);
            if (clip != null) _vfx.Play(clip.frames, clip.fps, clip.ground ? new Vector3(at.x, clip.halfHeight * 0.6f, 0f) : at, 0.6f, tint);
            else _vfx.Play(_set.spark, _set.fps, at, 1.5f, tint);
        }

        private void LateUpdate()
        {
            StageRunner runner = _session != null ? _session.Runner : null;
            if (runner != _hooked)
            {
                Unhook();
                _hooked = runner;
                if (runner != null) runner.Pet.Attacked += OnAttacked;
            }

            PetDef def = runner != null ? runner.Pet.Def : null;
            int index = def != null ? PetCatalog.IndexOf(def.Id) : -1;
            CharacterArt art = index >= 0 && index < _arts.Length ? _arts[index] : null;
            bool show = art != null && runner.Hero.State != HeroState.Dead;
            if (_renderer == null) return;
            if (_renderer.enabled != show) _renderer.enabled = show;
            if (!show)
            {
                _shown = null;
                return;
            }

            if (def != _shown)
            {
                _shown = def;
                _book?.Play(art.idle, art.fps, loop: true, restart: true);
            }

            if (_attackPending && _book != null)
            {
                _attackPending = false;
                _book.Play(art.attack, art.fps, loop: false, restart: true);
            }
            else if (_book != null && _book.Finished)
            {
                _book.Play(art.idle, art.fps, loop: true);
            }

            float bob = Mathf.Sin(Time.time * BobSpeed) * BobHeight;
            _renderer.transform.position = new Vector3((float)runner.Hero.X - BehindHero, FloatY + bob, 0f);
        }
    }
}
