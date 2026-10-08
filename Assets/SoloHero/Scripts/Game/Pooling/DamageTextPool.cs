using SoloHero.Core.Combat;
using SoloHero.Core.Common;
using SoloHero.Core.Stage;
using SoloHero.Game.Combat;
using UnityEngine;

namespace SoloHero.Game.Pooling
{
    /// <summary>
    /// Shows every hero hit as a pooled floating number over the enemy (E2-10, E8-09): normal white, crit gold and
    /// large, skill hits sky blue, burn ticks small orange (D-078). Skill names pop over the hero through
    /// <see cref="ShowLabel"/>. Pre-warmed at scene start; when all texts are busy a hit is simply not shown.
    /// D-096: hits on the same enemy close together stack upward instead of piling on one spot.
    /// D-103: multi-wave skills, burn ticks and pet hits on the same enemy merge into one growing number
    /// (within MergeWindowSeconds, same kind) instead of a pile of identical numbers.
    /// </summary>
    public sealed class DamageTextPool : MonoBehaviour
    {
        public const int Capacity = 32;
        private const float EnemyHeadOffset = 0.9f;

        [SerializeField] private DamageText _template;
        [SerializeField] private CombatSession _session;
        [SerializeField] private Camera _camera;
        [SerializeField] private RectTransform _layer;
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _critColor = new Color(1f, 0.77f, 0.19f, 1f);
        [SerializeField] private int _normalSize = 34;
        [SerializeField] private int _critSize = 46;
        [SerializeField] private Color _skillColor = new Color(0.55f, 0.9f, 1f, 1f);
        [SerializeField] private Color _dotColor = new Color(1f, 0.6f, 0.3f, 1f);
        [SerializeField] private int _skillSize = 44;
        [SerializeField] private int _dotSize = 33;
        [SerializeField] private Color _comboColor = new Color(0.86f, 0.55f, 1f, 1f);
        [SerializeField] private int _comboSize = 46;
        [SerializeField] private Color _petColor = new Color(0.6f, 1f, 0.5f, 1f);
        [SerializeField] private int _petSize = 38;

        private const float NormalJitterPixels = 10f;
        private const int StackSlots = 8;
        private const float StackWindowSeconds = 0.35f;
        private const float StackStepPixels = 42f;
        private const int StackMax = 4;

        private readonly EnemyBrain[] _stackTarget = new EnemyBrain[StackSlots];
        private readonly float[] _stackTime = new float[StackSlots];
        private readonly int[] _stackCount = new int[StackSlots];

        private const float MergeWindowSeconds = 0.35f;
        private readonly EnemyBrain[] _mergeTarget = new EnemyBrain[StackSlots];
        private readonly HitKind[] _mergeKind = new HitKind[StackSlots];
        private readonly DamageText[] _mergeText = new DamageText[StackSlots];
        private readonly int[] _mergeSerial = new int[StackSlots];
        private readonly double[] _mergeAmount = new double[StackSlots];
        private readonly float[] _mergeTime = new float[StackSlots];

        private ViewPool<DamageText> _pool;
        private CombatWorld _world;
        private float _nextDrift = 1f;

        public int ActiveCount => _pool != null ? _pool.CountActive : 0;

        private void Awake()
        {
            if (_camera == null) _camera = Camera.main;
            if (_layer == null) _layer = (RectTransform)transform;
            if (_template == null) return;
            _template.gameObject.SetActive(false);
            _pool = new ViewPool<DamageText>(_template, _layer, Capacity);
        }

        private void OnDestroy()
        {
            if (_world != null) _world.HitLanded -= OnHitLanded;
        }

        private void LateUpdate()
        {
            if (_world != null || _session == null) return;
            StageRunner runner = _session.Runner;
            if (runner == null) return;
            _world = runner.World;
            _world.HitLanded += OnHitLanded;
        }

        public void Return(DamageText text) => _pool.Release(text);

        /// <summary>How many numbers this enemy showed within the stack window (0 for a fresh one), capped.</summary>
        private int NextStack(EnemyBrain target)
        {
            float now = Time.time;
            int free = 0;
            float oldest = float.MaxValue;
            for (int i = 0; i < StackSlots; i++)
            {
                if (_stackTarget[i] == target)
                {
                    int step = now - _stackTime[i] < StackWindowSeconds ? (_stackCount[i] + 1) % StackMax : 0;
                    _stackCount[i] = step;
                    _stackTime[i] = now;
                    return step;
                }

                if (_stackTime[i] < oldest)
                {
                    oldest = _stackTime[i];
                    free = i;
                }
            }

            _stackTarget[free] = target;
            _stackTime[free] = now;
            _stackCount[free] = 0;
            return 0;
        }

        /// <summary>A short text on a dark plate over a world point (a skill name over the hero); holds, then fades.</summary>
        public void ShowLabel(Vector3 world, string label, Color color, int size)
        {
            if (_pool == null || _camera == null || string.IsNullOrEmpty(label) || !_pool.TryGet(out DamageText text)) return;
            Vector3 screen = _camera.WorldToScreenPoint(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_layer, screen, null, out Vector2 local);
            text.Show(this, local, label, color, size, false, 0f, true);
        }

        /// <summary>Adds the hit to a live number for the same enemy and kind, if one is recent enough.</summary>
        private bool TryMerge(EnemyBrain target, double amount, HitKind kind)
        {
            if (kind == HitKind.Normal || kind == HitKind.Crit) return false;
            float now = Time.time;
            for (int i = 0; i < StackSlots; i++)
            {
                if (_mergeTarget[i] != target || _mergeKind[i] != kind) continue;
                DamageText live = _mergeText[i];
                if (now - _mergeTime[i] > MergeWindowSeconds || live == null || !live.Showing || live.Serial != _mergeSerial[i]) return false;
                _mergeAmount[i] += amount;
                _mergeTime[i] = now;
                live.Merge(BigNumberFormat.Format(_mergeAmount[i]));
                return true;
            }

            return false;
        }

        private void Remember(EnemyBrain target, HitKind kind, DamageText text, double amount)
        {
            int slot = 0;
            float oldest = float.MaxValue;
            for (int i = 0; i < StackSlots; i++)
            {
                if (_mergeTarget[i] == target && _mergeKind[i] == kind)
                {
                    slot = i;
                    break;
                }

                if (_mergeTime[i] < oldest)
                {
                    oldest = _mergeTime[i];
                    slot = i;
                }
            }

            _mergeTarget[slot] = target;
            _mergeKind[slot] = kind;
            _mergeText[slot] = text;
            _mergeSerial[slot] = text.Serial;
            _mergeAmount[slot] = amount;
            _mergeTime[slot] = Time.time;
        }

        private void OnHitLanded(EnemyBrain target, double amount, HitKind kind)
        {
            if (TryMerge(target, amount, kind)) return;
            if (_pool == null || _camera == null || !_pool.TryGet(out DamageText text)) return;

            bool crit = kind == HitKind.Crit || kind == HitKind.SkillCrit;
            Vector3 screen = _camera.WorldToScreenPoint(new Vector3((float)target.X, EnemyHeadOffset + DepthLanes.For(target), 0f));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_layer, screen, null, out Vector2 local);
            // Small sideways jitter keeps numbers from stacking on one spot; crits alternate their arc direction.
            if (!crit) local.x += Random.Range(-NormalJitterPixels, NormalJitterPixels);
            if (kind == HitKind.Dot) local.y -= NormalJitterPixels;
            local.y += StackStepPixels * NextStack(target);
            _nextDrift = -_nextDrift;
            Color color;
            int size;
            switch (kind)
            {
                case HitKind.Crit:
                case HitKind.SkillCrit: color = _critColor; size = _critSize; break;
                case HitKind.Skill: color = _skillColor; size = _skillSize; break;
                case HitKind.Dot: color = _dotColor; size = _dotSize; break;
                case HitKind.Combo: color = _comboColor; size = _comboSize; break;
                case HitKind.Pet: color = _petColor; size = _petSize; break;
                default: color = _normalColor; size = _normalSize; break;
            }

            text.Show(this, local, BigNumberFormat.Format(amount), color, size, crit, _nextDrift);
            Remember(target, kind, text, amount);
        }
    }
}
