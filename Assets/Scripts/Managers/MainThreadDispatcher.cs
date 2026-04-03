using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Executes actions on the Unity main thread.
/// Required when AdMob (or Firebase) callbacks arrive on background threads.
/// Place one instance in the first scene (LoginScene) on a persistent GameObject.
/// </summary>
public class MainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> _queue = new Queue<Action>();
    private static MainThreadDispatcher _instance;

    void Awake()
    {
        if (_instance != null) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        lock (_queue)
        {
            while (_queue.Count > 0)
                _queue.Dequeue()?.Invoke();
        }
    }

    /// <summary>Schedule <paramref name="action"/> to run on the main thread next Update.</summary>
    public static void Post(Action action)
    {
        if (action == null) return;
        lock (_queue) { _queue.Enqueue(action); }
    }
}
