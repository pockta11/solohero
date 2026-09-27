using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoloHero.Game.Infrastructure
{
    /// <summary>
    /// Runs callbacks from native SDK threads (AdMob, Firebase) on the Unity main thread. Ported from Legacy.
    /// One instance lives on the Boot object (DontDestroyOnLoad).
    /// </summary>
    public sealed class MainThreadDispatcher : MonoBehaviour
    {
        private static readonly Queue<Action> Pending = new Queue<Action>();
        private static MainThreadDispatcher _instance;

        public static void Post(Action action)
        {
            if (action == null) return;
            lock (Pending) Pending.Enqueue(action);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }

            _instance = this;
        }

        private void Update()
        {
            while (true)
            {
                Action next;
                lock (Pending)
                {
                    if (Pending.Count == 0) return;
                    next = Pending.Dequeue();
                }

                next();
            }
        }
    }
}
