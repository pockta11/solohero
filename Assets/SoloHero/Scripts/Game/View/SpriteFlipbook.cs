using UnityEngine;

namespace SoloHero.Game.View
{
    /// <summary>
    /// Plays a frame array on a SpriteRenderer. Views drive it from Core state (architecture: state lives in Core,
    /// the view only shows it). Looping clips repeat; one-shot clips hold the last frame and report Finished.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpriteFlipbook : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private Sprite[] _frames;
        private float _fps = 10f;
        private bool _loop;
        private float _time;

        public Sprite[] Current => _frames;

        public bool Finished { get; private set; }

        public SpriteRenderer Renderer
        {
            get
            {
                if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
                return _renderer;
            }
        }

        /// <summary>Starts a clip. The same looping clip is not restarted unless <paramref name="restart"/>.</summary>
        public void Play(Sprite[] frames, float fps, bool loop, bool restart = false)
        {
            if (frames == null || frames.Length == 0) return;
            if (!restart && frames == _frames && loop == _loop) return;
            _frames = frames;
            _fps = fps > 0f ? fps : 10f;
            _loop = loop;
            _time = 0f;
            Finished = false;
            Renderer.sprite = frames[0];
        }

        private void Update()
        {
            if (_frames == null || Finished) return;
            _time += Time.deltaTime;
            int index = (int)(_time * _fps);
            if (index >= _frames.Length)
            {
                if (_loop)
                {
                    _time -= _frames.Length / _fps;
                    index %= _frames.Length;
                }
                else
                {
                    index = _frames.Length - 1;
                    Finished = true;
                }
            }

            Renderer.sprite = _frames[index];
        }
    }
}
