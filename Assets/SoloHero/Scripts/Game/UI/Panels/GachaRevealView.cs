using System.Collections;
using System.Collections.Generic;
using SoloHero.Core.Common;
using SoloHero.Core.Gacha;
using SoloHero.Core.Settings;
using SoloHero.Game.Audio;
using SoloHero.Game.UI.Common;
using SoloHero.Game.View;
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
    /// D-172: more results than cards (a x100 pull, a big ticket spend) skip the cards: small tiles pop into a grid, best
    /// grade first, in about a second, under a line of grade counts.
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
        private const float TilePop = 0.16f;
        private const float TileInterval = 0.011f;

        [SerializeField] private GameObject _root;
        [SerializeField] private RectTransform _circle;
        [SerializeField] private Image _circleImage;
        [SerializeField] private RectTransform _cardArea;
        [SerializeField] private GachaCard[] _cards = new GachaCard[10];
        [SerializeField] private UiBurst _burst;
        [SerializeField] private GameObject _skipButton;
        [SerializeField] private GameObject _closeHint;

        [Header("D-172 bulk grid")]
        [SerializeField] private GachaTile[] _tiles = new GachaTile[0];
        [SerializeField] private GradeFrameSet _frames;
        [SerializeField] private Text _summary;

        private readonly List<Coroutine> _flips = new List<Coroutine>();
        private RevealCard[] _items;
        private Coroutine _running;
        private bool _celebrated;
        private bool _skip;
        private bool _done;
        private AudioService _audio;
        private SettingsService _settings;
        private Color _circleBase = Color.white;
        private int[] _order = new int[0];
        private readonly System.Text.StringBuilder _sb = new System.Text.StringBuilder();

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
            if (_items.Length > _cards.Length && _tiles.Length > 0)
            {
                yield return RunGrid();
                yield break;
            }

            HideGrid();
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

        /// <summary>D-172: the bulk reveal - circle, then the tiles pop in best grade first, then the best one celebrates.</summary>
        private IEnumerator RunGrid()
        {
            for (int i = 0; i < _cards.Length; i++) _cards[i].Hide();
            if (_skipButton != null) _skipButton.SetActive(true);
            if (_closeHint != null) _closeHint.SetActive(false);
            int count = Mathf.Min(_items.Length, _tiles.Length);
            SortByGrade(count);
            for (int i = 0; i < _tiles.Length; i++) _tiles[i].Hide();
            if (_summary != null)
            {
                _summary.text = Summary();
                _summary.gameObject.SetActive(true);
            }

            GearGrade top = _items[_order[0]].Grade;
            if (_circleImage != null)
            {
                Color tint = top >= GearGrade.Legendary ? PanelServices.GradeColor(top) : _circleBase;
                tint.a = _circleImage.color.a;
                _circleImage.color = tint;
            }

            if (_circle != null) _circle.gameObject.SetActive(true);
            for (float t = 0f; t < SummonTen && !_skip; t += Time.unscaledDeltaTime)
            {
                float k = t / SummonTen;
                SetCircle(Mathf.SmoothStep(0.2f, 1.25f, k), k * 360f, Mathf.Sin(k * Mathf.PI));
                yield return null;
            }

            if (_circle != null) _circle.gameObject.SetActive(false);
            for (int i = 0; i < count; i++)
            {
                RevealCard item = _items[_order[i]];
                Sprite frame = _frames != null ? _frames.Get(item.Grade) : null;
                _tiles[i].Show(frame, item.Icon, item.IconScale, item.IsNew);
            }

            // One loop scales every tile: tile i starts TileInterval x i after the first, overshoots a little, then rests.
            float total = (count - 1) * TileInterval + TilePop;
            int sounded = -1;
            for (float t = 0f; t < total && !_skip; t += Time.unscaledDeltaTime)
            {
                for (int i = 0; i < count; i++) _tiles[i].Rect.localScale = Vector3.one * Pop((t - i * TileInterval) / TilePop);
                int row = Mathf.Min(count - 1, (int)(t / TileInterval)) / 10;
                if (row > sounded)
                {
                    sounded = row;
                    Sound(SfxId.CardFlip);
                }

                yield return null;
            }

            for (int i = 0; i < count; i++) _tiles[i].Rect.localScale = Vector3.one;
            if (IsHigh(top)) CelebrateTile(0, top);
            if (_skipButton != null) _skipButton.SetActive(false);
            if (_closeHint != null) _closeHint.SetActive(true);
            _done = true;
            _running = null;
        }

        /// <summary>0 before the start, a 1.15 overshoot at 70 %, 1 at the end.</summary>
        private static float Pop(float k)
        {
            if (k <= 0f) return 0f;
            if (k >= 1f) return 1f;
            return k < 0.7f ? Mathf.Lerp(0f, 1.15f, k / 0.7f) : Mathf.Lerp(1.15f, 1f, (k - 0.7f) / 0.3f);
        }

        private void CelebrateTile(int index, GearGrade grade)
        {
            if (_celebrated) return;
            _celebrated = true;
            bool top = grade >= GearGrade.Mythic;
            Sound(grade >= GearGrade.Legendary ? SfxId.GradeLegendary : SfxId.GradeEpic);
            UiPunch punch = _tiles[index].GetComponent<UiPunch>();
            if (punch != null) punch.Play(top ? 2.6f : 2f);
            if (_settings != null && _settings.LowEffect) return;
            if (_burst != null)
            {
                Vector2 local = (Vector2)_burst.transform.InverseTransformPoint(_tiles[index].Rect.position);
                _burst.Play(local, PanelServices.GradeColor(grade));
                if (top) StartCoroutine(SecondBurst(local, PanelServices.GradeColor(grade)));
            }

            StartCoroutine(Shake(top ? TopShakeScale : 1f));
        }

        /// <summary>Stable sort of the shown results, highest grade first (insertion sort; a hundred at most).</summary>
        private void SortByGrade(int count)
        {
            if (_order.Length < count) _order = new int[count];
            for (int i = 0; i < count; i++)
            {
                int j = i - 1;
                while (j >= 0 && _items[_order[j]].Grade < _items[i].Grade)
                {
                    _order[j + 1] = _order[j];
                    j--;
                }

                _order[j + 1] = i;
            }
        }

        /// <summary>The pull count, grade counts in grade colours (best first), then how many were new.</summary>
        private string Summary()
        {
            _sb.Clear();
            var counts = new int[GearGrades.Count];
            int fresh = 0;
            for (int i = 0; i < _items.Length; i++)
            {
                counts[(int)_items[i].Grade]++;
                if (_items[i].IsNew) fresh++;
            }

            _sb.Append(Strings.Format("gacha.bulk_title", _items.Length)).Append('\n');
            bool first = true;
            for (int g = counts.Length - 1; g >= 0; g--)
            {
                if (counts[g] == 0) continue;
                if (!first) _sb.Append("  ");
                first = false;
                _sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(PanelServices.GradeColor((GearGrade)g))).Append('>')
                    .Append(PanelServices.GradeName((GearGrade)g)).Append(' ').Append(counts[g]).Append("</color>");
            }

            if (fresh > 0) _sb.Append("  <color=#FFE066>").Append(Strings.Format("gacha.bulk_new", fresh)).Append("</color>");
            return _sb.ToString();
        }

        private void HideGrid()
        {
            for (int i = 0; i < _tiles.Length; i++) _tiles[i].Hide();
            if (_summary != null) _summary.gameObject.SetActive(false);
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
