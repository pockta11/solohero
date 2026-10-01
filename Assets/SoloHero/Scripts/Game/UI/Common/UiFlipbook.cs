using SoloHero.Game.View;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Common
{
    /// <summary>
    /// Character portrait on a UI Image (character panel): loops the idle clip at an integer pixel scale with the
    /// sprite's foot pivot, and plays the attack clip once when poked. Runs on unscaled time like the panels.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class UiFlipbook : MonoBehaviour
    {
        [SerializeField] private CharacterArt _art;

        [Tooltip("D-101: promotion looks for tiers 1..4, picked from the saved tier; empty keeps _art.")]
        [SerializeField] private CharacterArt[] _tiers = new CharacterArt[0];
        [Tooltip("Canvas units per sprite pixel; keep it an integer so the pixel grid stays even.")]
        [SerializeField] private float _scale = 3f;

        private Image _image;
        private Sprite[] _clip;
        private float _time;
        private int _shown = -1;
        private bool _oneShot;

        private CharacterArt _baseArt;

        private void Awake()
        {
            _image = GetComponent<Image>();
            _baseArt = _art;
        }

        private SoloHero.Core.Save.SaveDataV2 _save;
        private int _tierShown = -1;

        private void OnEnable()
        {
            _save = SoloHero.Game.UI.Panels.PanelServices.TryGet<SoloHero.Core.Save.SaveDataV2>();
            PickTier();
            PlayIdle();
        }

        /// <summary>Switches to the art of the saved promotion tier; true when it changed.</summary>
        private bool PickTier()
        {
            int tier = _save != null ? _save.promotionTier : 0;
            if (tier == _tierShown) return false;
            _tierShown = tier;
            if (tier >= 1 && tier <= _tiers.Length && _tiers[tier - 1] != null) _art = _tiers[tier - 1];
            else if (_tiers.Length > 0 && _baseArt != null) _art = _baseArt;
            return true;
        }

        public void Poke()
        {
            if (_art == null || _art.attack == null || _art.attack.Length == 0 || _oneShot) return;
            Play(_art.attack, true);
        }

        private void PlayIdle()
        {
            if (_art != null) Play(_art.idle, false);
        }

        private void Play(Sprite[] clip, bool oneShot)
        {
            if (clip == null || clip.Length == 0) return;
            _clip = clip;
            _oneShot = oneShot;
            _time = 0f;
            _shown = -1;
            Show(0);
        }

        private void Update()
        {
            if (PickTier())
            {
                PlayIdle();
                return;
            }

            if (_clip == null || _art == null) return;
            float fps = _art.fps > 0f ? _art.fps : 10f;
            _time += Time.unscaledDeltaTime;
            int index = (int)(_time * fps);
            if (index >= _clip.Length)
            {
                if (_oneShot)
                {
                    PlayIdle();
                    return;
                }

                _time -= _clip.Length / fps;
                index %= _clip.Length;
            }

            if (index != _shown) Show(index);
        }

        private void Show(int index)
        {
            _shown = index;
            Sprite sprite = _clip[index];
            if (_image == null) _image = GetComponent<Image>();
            if (_image.sprite == sprite || sprite == null) return;
            _image.sprite = sprite;
            var rect = (RectTransform)transform;
            Vector2 size = sprite.rect.size;
            rect.sizeDelta = size * _scale;
            rect.pivot = new Vector2(sprite.pivot.x / size.x, sprite.pivot.y / size.y);
        }
    }
}
