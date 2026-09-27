using System;
using UnityEngine;
using UnityEngine.Pool;

namespace SoloHero.Game.Pooling
{
    /// <summary>
    /// Fixed-size pool over <see cref="ObjectPool{T}"/> (E2-10). Everything is created in the constructor
    /// (pre-warm); <see cref="TryGet"/> never instantiates, it returns false when the pool is exhausted.
    /// This is the only place gameplay views are instantiated.
    /// </summary>
    public sealed class ViewPool<T> where T : Component
    {
        private readonly ObjectPool<T> _pool;
        private readonly int _capacity;

        public ViewPool(T template, Transform parent, int capacity)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));
            _capacity = capacity < 1 ? 1 : capacity;
            _pool = new ObjectPool<T>(
                () => UnityEngine.Object.Instantiate(template, parent),
                item => item.gameObject.SetActive(true),
                item => item.gameObject.SetActive(false),
                item => UnityEngine.Object.Destroy(item.gameObject),
                collectionCheck: false,
                defaultCapacity: _capacity,
                maxSize: _capacity);

            var warm = new T[_capacity];
            for (int i = 0; i < _capacity; i++) warm[i] = _pool.Get();
            for (int i = 0; i < _capacity; i++) _pool.Release(warm[i]);
        }

        public int CountActive => _pool.CountActive;

        public int CountInactive => _pool.CountInactive;

        public bool TryGet(out T item)
        {
            if (_pool.CountInactive == 0)
            {
                item = null;
                return false;
            }

            item = _pool.Get();
            return true;
        }

        public void Release(T item) => _pool.Release(item);
    }
}
