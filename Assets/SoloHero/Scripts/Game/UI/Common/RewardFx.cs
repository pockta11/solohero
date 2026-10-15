using SoloHero.Core.Common;
using SoloHero.Core.Save;
using SoloHero.Game.Audio;
using SoloHero.Game.UI.Panels;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Common
{
    /// <summary>What a reward icon is, and where it flies (index into the serialized arrays).</summary>
    public enum RewardKind
    {
        Gold = 0,
        Gem = 1,
        Ticket = 2,
        Stone = 3,
    }

    /// <summary>Gold, gems, tickets and stones at one moment; the difference to a later save is what a claim paid.</summary>
    public readonly struct RewardSnapshot
    {
        public readonly double Gold;
        public readonly double Gem;
        public readonly int Tickets;
        public readonly int Stones;

        public RewardSnapshot(SaveDataV2 save)
        {
            Gold = save != null ? save.gold : 0d;
            Gem = save != null ? save.gem : 0d;
            Tickets = save != null ? save.gearTickets + save.skillTickets + save.petTickets : 0;
            Stones = save != null ? save.breakStones : 0;
        }
    }

    /// <summary>
    /// D-171 reward feedback (genre standard): icons burst out where the reward was claimed, arc to the counter they
    /// add to (gold and gems to the top bar, tickets to the summon tab, stones to the character tab), the counter
    /// punches and a chime plays as they land; a "+N" rises from the claim. Big moments get a ribbon, a screen flash
    /// and a fanfare. A fixed pool of Images and Texts made by the scene builder, on its own top canvas so the icons
    /// fly over popups; everything runs on unscaled time and nothing takes taps.
    /// </summary>
    public sealed class RewardFx : MonoBehaviour
    {
        private const int KindCount = 4;
        private const float SpreadSeconds = 0.22f;
        private const float FlySeconds = 0.5f;
        private const float Stagger = 0.035f;
        private const float LabelSeconds = 1.0f;
        private const float LabelRise = 110f;
        private const float LabelGap = 64f;
        private const float ChimeGap = 0.07f;

        [SerializeField] private Image[] _icons = new Image[0];
        [SerializeField] private Text[] _labels = new Text[0];
        [Tooltip("Indexed by RewardKind: gold, gem, ticket, stone.")]
        [SerializeField] private Sprite[] _sprites = new Sprite[KindCount];
        [SerializeField] private RectTransform[] _targets = new RectTransform[KindCount];
        [SerializeField] private UiPunch[] _punches = new UiPunch[KindCount];
        [SerializeField] private Color[] _colors = new Color[KindCount];
        [SerializeField] private UiBurst _burst;
        [SerializeField] private ScreenFlash _flash;
        [SerializeField] private SkillCutIn _cutIn;

        private Vector2[] _from = new Vector2[0];
        private Vector2[] _spread = new Vector2[0];
        private float[] _age = new float[0];
        private int[] _kind = new int[0];
        private bool[] _lead = new bool[0];
        private bool[] _flying = new bool[0];
        private Vector2[] _labelFrom = new Vector2[0];
        private float[] _labelAge = new float[0];
        private Color[] _labelColor = new Color[0];
        private int _nextIcon;
        private int _nextLabel;
        private float _lastChime = -1f;
        private Vector2 _lastTap;
        private AudioService _audio;

        /// <summary>Screen position of the last touch (the claim button the player just pressed).</summary>
        public Vector2 LastTap => _lastTap;

        private void Awake()
        {
            int n = _icons.Length;
            _from = new Vector2[n];
            _spread = new Vector2[n];
            _age = new float[n];
            _kind = new int[n];
            _lead = new bool[n];
            _flying = new bool[n];
            for (int i = 0; i < n; i++)
            {
                if (_icons[i] != null) _icons[i].gameObject.SetActive(false);
            }

            int m = _labels.Length;
            _labelFrom = new Vector2[m];
            _labelAge = new float[m];
            _labelColor = new Color[m];
            for (int i = 0; i < m; i++)
            {
                _labelAge[i] = -1f;
                if (_labels[i] != null) _labels[i].gameObject.SetActive(false);
            }

            _lastTap = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }

        private void OnEnable() => _audio = PanelServices.TryGet<AudioService>();

        /// <summary>Plays every gain since <paramref name="before"/> from the last tap.</summary>
        public void PlayGain(RewardSnapshot before, SaveDataV2 save) => PlayGain(before, save, _lastTap);

        public void PlayGain(RewardSnapshot before, SaveDataV2 save, Vector2 screen)
        {
            if (save == null) return;
            var now = new RewardSnapshot(save);
            int row = 0;
            if (now.Gold > before.Gold) Play(RewardKind.Gold, now.Gold - before.Gold, screen, row++);
            if (now.Gem > before.Gem) Play(RewardKind.Gem, now.Gem - before.Gem, screen, row++);
            if (now.Tickets > before.Tickets) Play(RewardKind.Ticket, now.Tickets - before.Tickets, screen, row++);
            if (now.Stones > before.Stones) Play(RewardKind.Stone, now.Stones - before.Stones, screen, row);
        }

        /// <summary>
        /// <paramref name="count"/> icons of <paramref name="kind"/> from <paramref name="screen"/> to their counter,
        /// with "+amount" on label row <paramref name="row"/>. More reward, more icons (3 to 8).
        /// </summary>
        public void Play(RewardKind kind, double amount, Vector2 screen, int row = 0)
        {
            if (amount <= 0d) return;
            int k = (int)kind;
            int count = amount >= 1000d ? 8 : amount >= 100d ? 6 : amount >= 10d ? 5 : 3;
            for (int i = 0; i < count; i++) Launch(k, screen, i, i == 0);
            ShowLabel("+" + BigNumberFormat.Format(amount), screen + new Vector2(0f, (70f + row * LabelGap) * Scale), ColorOf(k));
            Burst(screen, ColorOf(k));
        }

        /// <summary>A sparkle and a short label at the last tap (an upgrade, a learned node, a level up).</summary>
        public void Sparkle(Color color, string text = null)
        {
            Burst(_lastTap, color);
            if (!string.IsNullOrEmpty(text)) ShowLabel(text, _lastTap + new Vector2(0f, 70f * Scale), color);
        }

        /// <summary>A rising label anywhere (the hero's head for LEVEL UP).</summary>
        public void Label(string text, Vector2 screen, Color color) => ShowLabel(text, screen, color);

        /// <summary>A big moment: the ribbon (after a playing cut-in band), a soft flash and the fanfare.</summary>
        public void Celebrate(string text, Color color, float seconds = 2.2f)
        {
            if (_cutIn != null) _cutIn.PlayText(text, color, true, seconds);
            if (_flash != null) _flash.Flash(new Color(1f, 1f, 1f, 0.45f), 0.35f);
            if (_audio == null) _audio = PanelServices.TryGet<AudioService>();
            _audio?.Play(SfxId.Fanfare);
        }

        /// <summary>Over the battle field, a little above the ground line (characters stand at 50 % of the height).</summary>
        public static Vector2 BattleCenter => new Vector2(Screen.width * 0.5f, Screen.height * 0.58f);

        /// <summary>The screen position of a world point (the hero) for the main camera.</summary>
        public static Vector2 ScreenOf(Vector3 world)
        {
            Camera cam = Camera.main;
            return cam != null ? (Vector2)cam.WorldToScreenPoint(world) : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }

        private void Launch(int kind, Vector2 screen, int index, bool lead)
        {
            if (_icons.Length == 0) return;
            int slot = _nextIcon;
            _nextIcon = (_nextIcon + 1) % _icons.Length;
            Image icon = _icons[slot];
            if (icon == null) return;
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Random.Range(60f, 150f) * Scale;
            _from[slot] = screen;
            _spread[slot] = screen + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * 0.7f + 0.4f) * radius;
            _age[slot] = -index * Stagger;
            _kind[slot] = kind;
            _lead[slot] = lead;
            _flying[slot] = true;
            icon.sprite = SpriteOf(kind);
            icon.color = Color.white;
            icon.rectTransform.position = screen;
            icon.rectTransform.localScale = Vector3.zero;
            icon.gameObject.SetActive(true);
        }

        private void ShowLabel(string text, Vector2 screen, Color color)
        {
            if (_labels.Length == 0) return;
            int slot = _nextLabel;
            _nextLabel = (_nextLabel + 1) % _labels.Length;
            Text label = _labels[slot];
            if (label == null) return;
            label.text = text;
            _labelFrom[slot] = screen;
            _labelAge[slot] = 0f;
            _labelColor[slot] = color;
            label.color = color;
            label.rectTransform.position = screen;
            label.gameObject.SetActive(true);
        }

        private void Burst(Vector2 screen, Color color)
        {
            if (_burst == null) return;
            var parent = (RectTransform)_burst.transform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, null, out Vector2 local))
                _burst.Play(local, color);
        }

        /// <summary>Canvas scale: offsets are authored in 1080-wide canvas units.</summary>
        private float Scale => transform.lossyScale.x > 0f ? transform.lossyScale.x : 1f;

        private Color ColorOf(int kind) => kind < _colors.Length ? _colors[kind] : Color.white;

        private Sprite SpriteOf(int kind) => kind < _sprites.Length ? _sprites[kind] : null;

        private Vector2 TargetOf(int kind, Vector2 fallback)
        {
            RectTransform target = kind < _targets.Length ? _targets[kind] : null;
            if (target == null || !target.gameObject.activeInHierarchy) return fallback + new Vector2(0f, 220f * Scale);
            return target.position;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (Input.GetMouseButtonDown(0)) _lastTap = Input.mousePosition;

            for (int i = 0; i < _icons.Length; i++)
            {
                if (!_flying[i]) continue;
                _age[i] += dt;
                float a = _age[i];
                Image icon = _icons[i];
                if (a < 0f) continue;
                RectTransform rect = icon.rectTransform;
                if (a < SpreadSeconds)
                {
                    float u = 1f - Mathf.Pow(1f - a / SpreadSeconds, 3f);
                    rect.position = Vector2.Lerp(_from[i], _spread[i], u);
                    rect.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.15f, u);
                    continue;
                }

                float t = (a - SpreadSeconds) / FlySeconds;
                Vector2 target = TargetOf(_kind[i], _spread[i]);
                if (t >= 1f)
                {
                    Land(i);
                    continue;
                }

                float e = t * t;
                Vector2 p = Vector2.Lerp(_spread[i], target, e);
                Vector2 side = target - _spread[i];
                p += new Vector2(-side.y, side.x).normalized * Mathf.Sin(t * Mathf.PI) * 70f * Scale * (i % 2 == 0 ? 1f : -1f);
                rect.position = p;
                rect.localScale = Vector3.one * Mathf.Lerp(1.15f, 0.65f, e);
            }

            for (int i = 0; i < _labels.Length; i++)
            {
                if (_labelAge[i] < 0f) continue;
                _labelAge[i] += dt;
                float t = _labelAge[i] / LabelSeconds;
                Text label = _labels[i];
                if (t >= 1f)
                {
                    _labelAge[i] = -1f;
                    label.gameObject.SetActive(false);
                    continue;
                }

                float rise = 1f - Mathf.Pow(1f - t, 2f);
                label.rectTransform.position = _labelFrom[i] + new Vector2(0f, LabelRise * Scale * rise);
                float pop = t < 0.12f ? Mathf.Lerp(1.5f, 1f, t / 0.12f) : 1f;
                label.rectTransform.localScale = Vector3.one * pop;
                Color c = _labelColor[i];
                c.a = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
                label.color = c;
            }
        }

        private void Land(int i)
        {
            _flying[i] = false;
            _icons[i].gameObject.SetActive(false);
            int kind = _kind[i];
            if (kind < _punches.Length && _punches[kind] != null) _punches[kind].Play();
            if (!_lead[i] && Time.unscaledTime - _lastChime < ChimeGap) return;
            _lastChime = Time.unscaledTime;
            if (_audio == null) _audio = PanelServices.TryGet<AudioService>();
            _audio?.Play(SfxId.Collect);
        }
    }

    /// <summary>
    /// D-171: a claim button accepts one tap per window, so a burst of taps cannot run claims faster than their reward
    /// animation (the player sees each reward land). Unscaled time: the ad 2x speed does not shorten it.
    /// </summary>
    public sealed class ClaimGate
    {
        private float _until;

        public bool Pass(float seconds)
        {
            float now = Time.unscaledTime;
            if (now < _until) return false;
            _until = now + seconds;
            return true;
        }
    }
}
