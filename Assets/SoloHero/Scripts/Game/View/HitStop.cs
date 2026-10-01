using UnityEngine;

namespace SoloHero.Game.View
{
    /// <summary>
    /// Hit-stop (D-096): character animations and knockback freeze for a few frames on a crit, a kill or a big skill
    /// so the hit lands with weight. View only - Core keeps ticking, so combat results never change. After a stop
    /// ends, new requests are ignored for a short gap so a stream of hits never turns into a frozen screen.
    /// </summary>
    public static class HitStop
    {
        private const float MinGapSeconds = 0.12f;
        private const float MaxSeconds = 0.2f;

        private static float _until;

        public static bool Active => Time.unscaledTime < _until;

        public static void Trigger(float seconds)
        {
            float now = Time.unscaledTime;
            if (now < _until)
            {
                _until = Mathf.Max(_until, now + Mathf.Min(seconds, MaxSeconds));
                return;
            }

            if (now < _until + MinGapSeconds) return;
            _until = now + Mathf.Min(seconds, MaxSeconds);
        }

        /// <summary>Frame time for frozen-able views: zero during a stop.</summary>
        public static float DeltaTime => Active ? 0f : Time.deltaTime;
    }
}
