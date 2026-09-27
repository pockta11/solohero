using System.Collections.Generic;
using SoloHero.Core.Common;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Common
{
    /// <summary>One toast at a time, 1.5 s each, at most 4 waiting (oldest dropped). Architecture UI host rules.</summary>
    public sealed class ToastQueue : MonoBehaviour
    {
        public const int MaxPending = 4;
        public const float ShowSeconds = 1.5f;

        [SerializeField] private GameObject _root;
        [SerializeField] private Text _label;

        private readonly Queue<string> _pending = new Queue<string>();
        private float _remaining;

        private void Awake()
        {
            if (_root != null) _root.SetActive(false);
        }

        public void Show(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            if (_pending.Count >= MaxPending) _pending.Dequeue();
            _pending.Enqueue(message);
        }

        public void ShowFailure(FailReason reason) => Show(MessageFor(reason));

        /// <summary>Placeholder English text until the Strings table (E7-17) supplies Korean values.</summary>
        public static string MessageFor(FailReason reason)
        {
            switch (reason)
            {
                case FailReason.NotEnoughGold: return "Not enough gold";
                case FailReason.NotEnoughGem: return "Not enough gems";
                case FailReason.MaxLevel: return "Max level";
                case FailReason.OnCooldown: return "On cooldown";
                case FailReason.Locked: return "Locked";
                case FailReason.Busy: return "Busy";
                case FailReason.DailyLimit: return "No more today";
                case FailReason.AdUnavailable: return "Ad not available right now";
                default: return "";
            }
        }

        private void Update()
        {
            if (_remaining > 0f)
            {
                _remaining -= Time.unscaledDeltaTime;
                if (_remaining > 0f) return;
                if (_root != null) _root.SetActive(false);
            }

            if (_pending.Count == 0) return;
            if (_label != null) _label.text = _pending.Dequeue();
            if (_root != null) _root.SetActive(true);
            _remaining = ShowSeconds;
        }
    }
}
