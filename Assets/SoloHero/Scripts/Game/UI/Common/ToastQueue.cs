using System.Collections.Generic;
using SoloHero.Core.Common;
using SoloHero.Game.Audio;
using SoloHero.Game.UI.Panels;
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

        /// <summary>Architecture rule: every FailReason maps to one toast key.</summary>
        public static string KeyFor(FailReason reason)
        {
            switch (reason)
            {
                case FailReason.NotEnoughGold: return "toast.not_enough_gold";
                case FailReason.NotEnoughGem: return "toast.not_enough_gem";
                case FailReason.MaxLevel: return "toast.max_level";
                case FailReason.OnCooldown: return "toast.on_cooldown";
                case FailReason.Locked: return "toast.locked";
                case FailReason.Busy: return "toast.busy";
                case FailReason.SlotsFull: return "toast.slots_full";
                case FailReason.DailyLimit: return "toast.daily_limit";
                case FailReason.AdUnavailable: return "toast.ad_unavailable";
                case FailReason.NoTalentPoints: return "toast.no_talent_points";
                case FailReason.JobLocked: return "toast.job_locked";
                default: return null;
            }
        }

        public static string MessageFor(FailReason reason)
        {
            string key = KeyFor(reason);
            return key == null ? "" : Strings.Get(key);
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
            AudioService audio = PanelServices.TryGet<AudioService>();
            if (audio != null) audio.Play(SfxId.Toast);
        }
    }
}
