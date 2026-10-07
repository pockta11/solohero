using System.Collections;
using System.Collections.Generic;
using SoloHero.Core.Gacha;
using SoloHero.Core.Settings;
using SoloHero.Game.Audio;
using SoloHero.Game.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Panels
{
    /// <summary>
    /// Gacha presentation (E5-11, E8-10), GDD steps 2-5: the summon circle grows (0.6 s single / 0.9 s ten), cards
    /// flip (one card 0.4 s; ten cards 0.15 s apart), an Epic+ result gets grade particles, a shake and a fanfare
    /// (ten-pull: only the best card), then a tap closes the overlay. `Skip all` jumps to the end. The pull is
    /// already settled and saved before this runs, so closing early loses nothing. Runs on unscaled time.
    /// Equipment and skill summons (D-078) both hand in ready-made <see cref="RevealCard"/>s. D-113: a Legendary or
    /// better result tints the summon circle in its grade colour before the cards turn (the genre's tell), and a
    /// Mythic or Ancient card bursts twice and shakes harder.
    /// </summary>
    public sealed class GachaRevealView : MonoBehaviour
    {
        private const float SummonSingle = 0.6f;
        private const float SummonTen = 0.9f;
        private const float FlipSingle = 0.4f;
        private const float FlipTen = 0.24f;
        private const float TenInterval = 0.15f;
        private const float ShakePixels = 18f;
        private const float ShakeSeconds = 0.35f;
        private const float TopShakeScale = 1.6f;

        [SerializeField] private GameObject _root;
        [SerializeField] private RectTransform _circle;
        [SerializeField] private Image _circleImage;
        [SerializeField] private RectTransform _cardArea;
        [SerializeField] private GachaCard[] _cards = new GachaCard[10];
        [SerializeField] private UiBurst _burst;
        [SerializeField] private GameObject _skipButton;
        [SerializeField] private GameObject _closeHint;

        private readonly List<Coroutine> _flips = new List<Coroutine>();
        private RevealCard[] _items;
        private Coroutine _running;
        private bool _celebrated;
        private bool _skip;
        private bool _done;
        private AudioService _audio;
        private SettingsService _settings;
        private Color _circleBase = Color.white;

        public bool IsShowing => _root != null && _root.activeSelf;

        private void Awake()
        {
            if (_root != null) _root.SetActive(false);
            if (_circleImage != null) _circleImage.color = _circleBase = _circleImage.color;
        }

        public void Show(RevealCard[] items)
        {
            if (items == null || items.Length == 0 || _root == null) return;
            _audio = PanelServices.TryGet<AudioService>();
            _settings = PanelServices.TryGet<SettingsService>();
            _items = items;
            _skip = false;
            _done = false;
            _celebrated = false;
            _root.SetActive(true);
            StopAllCoroutines();
            _flips.Clear();
            if (_cardArea != null) _cardArea.anchoredPosition = Vector2.zero;
            _running = StartCoroutine(Run());
        }

        /// <summary>Overlay tap: skips while cards are still turning, closes once everything is shown.</summary>
        public void Tap()
        {
            if (_done) Close();
            else _skip = true;
        }

        public void SkipAll() => _skip = true;

        public void Close()
        {
            StopAllCoroutines();
            _flips.Clear();
            _running = null;
            if (_cardArea != null) _cardArea.anchoredPosition = Vector2.zero;
            if (_root != null) _root.SetActive(false);
        }

        private IEnumerator Run()
        {
            bool ten = _items.Length > 1;
            Layout(_items.Length);
            if (_skipButton != null) _skipButton.SetActive(ten);
            if (_closeHint != null) _closeHint.SetActive(false);
            for (int i = 0; i < _cards.Length; i++) _cards[i].Hide();

            // Step 2: summon circle, tinted by the best grade when it is Legendary or better.
            float summon = ten ? SummonTen : SummonSingle;
            GearGrade top = _items[Best()].Grade;
            if (_circleImage != null)
            {
                Color tint = top >= GearGrade.Legendary ? PanelServices.GradeColor(top) : _circleBase;
                tint.a = _circleImage.color.a;
                _circleImage.color = tint;
            }

            if (_circle != null) _circle.gameObject.SetActive(true);
            for (float t = 0f; t < summon && !_skip; t += Time.unscaledDeltaTime)
            {
                float k = t / summon;
                SetCircle(Mathf.SmoothStep(0.2f, ten ? 1.25f : 1f, k), k * 360f, Mathf.Sin(k * Mathf.PI) * (ten ? 1f : 0.85f));
                yield return null;
            }

            if (_circle != null) _circle.gameObject.SetActive(false);

            // Step 3: cards.
            for (int i = 0; i < _items.Length; i++) _cards[i].ShowBack();
            float flip = ten ? FlipTen : FlipSingle;
            for (int i = 0; i < _items.Length; i++)
            {
                if (_skip) break;
                _flips.Add(StartCoroutine(Flip(_cards[i], _items[i], flip)));
                if (!ten && IsHigh(_items[i].Grade))
                {
                    yield return Wait(flip);
                    Celebrate(i);
                }

                if (ten) yield return Wait(TenInterval);
            }

            if (_skip)
            {
                StopFlips();
                for (int i = 0; i < _items.Length; i++) Face(_cards[i], _items[i]);
            }
            else
            {
                yield return Wait(flip);
            }

            // Step 4 for ten-pulls (and skipped singles): highlight only the best card.
            int best = Best();
            if ((ten || _skip) && IsHigh(_items[best].Grade)) Celebrate(best);

            // Step 5: wait for a tap.
            if (_skipButton != null) _skipButton.SetActive(false);
            if (_closeHint != null) _closeHint.SetActive(true);
            _done = true;
            _flips.Clear();
            _running = null;
        }

        private IEnumerator Flip(GachaCard card, RevealCard item, float seconds)
        {
            Sound(SfxId.CardFlip);
            float half = seconds * 0.5f;
            for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
            {
                card.Rect.localScale = new Vector3(1f - t / half, 1f, 1f);
                yield return null;
            }

            Face(card, item);
            for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
            {
                card.Rect.localScale = new Vector3(t / half, 1f, 1f);
                yield return null;
            }

            card.Rect.localScale = Vector3.one;
        }

        private void StopFlips()
        {
            for (int i = 0; i < _flips.Count; i++)
            {
                if (_flips[i] != null) StopCoroutine(_flips[i]);
            }

            _flips.Clear();
        }

        private void Face(GachaCard card, RevealCard item)
        {
            card.Rect.localScale = Vector3.one;
            card.ShowFace(PanelServices.GradeColor(item.Grade), PanelServices.GradeName(item.Grade), item.Title, item.Note, item.Icon, item.IconScale);
        }

        private void Celebrate(int index)
        {
            if (_celebrated) return;
            _celebrated = true;
            RevealCard item = _items[index];
            bool top = item.Grade >= GearGrade.Mythic;
            Sound(item.Grade >= GearGrade.Legendary ? SfxId.GradeLegendary : SfxId.GradeEpic);
            UiPunch punch = _cards[index].GetComponent<UiPunch>();
            if (punch != null) punch.Play(top ? 2.6f : 2f);
            if (_settings != null && _settings.LowEffect) return;
            if (_burst != null)
            {
                Vector2 local = (Vector2)_burst.transform.InverseTransformPoint(_cards[index].Rect.position);
                _burst.Play(local, PanelServices.GradeColor(item.Grade));
                if (top) StartCoroutine(SecondBurst(local, PanelServices.GradeColor(item.Grade)));
            }

            StartCoroutine(Shake(top ? TopShakeScale : 1f));
        }

        private IEnumerator SecondBurst(Vector2 local, Color color)
        {
            for (float t = 0f; t < 0.22f; t += Time.unscaledDeltaTime) yield return null;
            if (_burst != null) _burst.Play(local, Color.Lerp(color, Color.white, 0.5f));
        }

        private IEnumerator Shake(float scale)
        {
            if (_cardArea == null) yield break;
            float seconds = ShakeSeconds * scale;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float s = ShakePixels * scale * (1f - t / seconds);
                _cardArea.anchoredPosition = new Vector2(Random.Range(-s, s), Random.Range(-s, s));
                yield return null;
            }

            _cardArea.anchoredPosition = Vector2.zero;
        }

        private int Best()
        {
            int best = 0;
            for (int i = 1; i < _items.Length; i++)
            {
                if (_items[i].Grade > _items[best].Grade) best = i;
            }

            return best;
        }

        private static bool IsHigh(GearGrade grade) => grade >= GearGrade.Epic;

        private void Layout(int count)
        {
            if (count <= 1)
            {
                SetAnchors(_cards[0].Rect, 0.34f, 0.3f, 0.66f, 0.7f);
                return;
            }

            // 5 x 2 grid.
            for (int i = 0; i < _cards.Length; i++)
            {
                int col = i % 5;
                int row = i / 5;
                float x = 0.03f + col * 0.19f;
                float y = row == 0 ? 0.52f : 0.1f;
                SetAnchors(_cards[i].Rect, x, y, x + 0.17f, y + 0.36f);
            }
        }

        private static void SetAnchors(RectTransform rect, float xMin, float yMin, float xMax, float yMax)
        {
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void SetCircle(float scale, float angle, float alpha)
        {
            if (_circle == null) return;
            _circle.localScale = Vector3.one * scale;
            _circle.localRotation = Quaternion.Euler(0f, 0f, angle);
            if (_circleImage != null)
            {
                Color c = _circleImage.color;
                c.a = alpha;
                _circleImage.color = c;
            }
        }

        private IEnumerator Wait(float seconds)
        {
            for (float t = 0f; t < seconds && !_skip; t += Time.unscaledDeltaTime) yield return null;
        }

        private void Sound(SfxId id)
        {
            if (_audio != null) _audio.Play(id);
        }
    }
}
