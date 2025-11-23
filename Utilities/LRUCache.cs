using System.Collections.Generic;

namespace PerformanceTweaks.Utilities
{
    // Combo LinkedList and Dictionary cache for O(1) reads and O(1) writes
    // TODO: maybe use a non-locking data structure?
    internal class LRUCache<TKey, TValue>
    {
        private readonly object _lock = new();
        private readonly int _capacity;
        private readonly Dictionary<TKey, LinkedListNode<CacheItem<TKey, TValue>>> _cache;
        private readonly LinkedList<CacheItem<TKey, TValue>> _lruList;

        public LRUCache(int capacity)
        {
            _capacity = capacity;
            _cache = new Dictionary<TKey, LinkedListNode<CacheItem<TKey, TValue>>>(_capacity);
            _lruList = new LinkedList<CacheItem<TKey, TValue>>();
        }

        public TValue Get(TKey key)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var node))
                {
                    _lruList.Remove(node);
                    _lruList.AddFirst(node);
                    //Debug.Log($"Shifted existing node in list (via Get): {node.Value.Key}");
                    return node.Value.Value;
                }

                return default;
            }
        }

        public void Add(TKey key, TValue value)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var node))
                {
                    node.Value = new CacheItem<TKey, TValue>(key, value);
                    _lruList.Remove(node);
                    _lruList.AddFirst(node);
                    //Debug.Log($"Shifted existing node in list (via Add): {node.Value.Key}");
                }
                else
                {
                    if (_cache.Count >= _capacity)
                    {
                        var lastNode = _lruList.Last;
                        _cache.Remove(lastNode.Value.Key);
                        _lruList.RemoveLast();
                        //Debug.Log($"Cache at capacity, removing last before add: {lastNode.Value.Key}");
                    }

                    var newItem = new CacheItem<TKey, TValue>(key, value);
                    var newNode = new LinkedListNode<CacheItem<TKey, TValue>>(newItem);
                    _lruList.AddFirst(newNode);
                    _cache.Add(key, newNode);
                    //Debug.Log($"Added new node: {newNode.Value.Key}");
                }
            }
        }

        public void Clear()
        {
            _lruList.Clear();
            _cache.Clear();
        }
    }

    internal class CacheItem<TKey, TValue>
    {
        public TKey Key { get; }
        public TValue Value { get; set; }

        public CacheItem(TKey key, TValue value) { Key = key; Value = value; }
    }
}
