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

        [Tooltip("D-104: job looks index-aligned with JobCatalog.All, picked from the saved job; empty keeps _art.")]
        [SerializeField] private CharacterArt[] _jobs = new CharacterArt[0];
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
        private int _jobShown = -1;

        private void OnEnable()
        {
            _save = SoloHero.Game.UI.Panels.PanelServices.TryGet<SoloHero.Core.Save.SaveDataV2>();
            PickJob();
            PlayIdle();
        }

        /// <summary>Switches to the art of the saved job; true when it changed.</summary>
        private bool PickJob()
        {
            int job = _save != null ? SoloHero.Core.Jobs.JobCatalog.IndexOf(_save.jobId) : 0;
            if (job == _jobShown) return false;
            _jobShown = job;
            if (job > 0 && job < _jobs.Length && _jobs[job] != null) _art = _jobs[job];
            else if (_baseArt != null) _art = _baseArt;
            return true;
        }

        /// <summary>D-114: shows another character (the pet panel's selected pet) and loops its idle clip.</summary>
        public void SetArt(CharacterArt art)
        {
            if (art == null || (art == _art && art == _baseArt)) return;
            _art = _baseArt = art;
            PlayIdle();
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
            if (PickJob())
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
