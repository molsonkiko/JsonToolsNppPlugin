using System.Collections.Generic;
using System.Linq;

namespace JSON_Tools.Utils
{
    /// <summary>
    /// Least-recently used cache.
    /// </summary>
    public class LruCache<K, V>
    {
        public int capacity;
        public Dictionary<K, V> cache;
        public LinkedList<K> useOrder;
        public bool isFull { get { return cache.Count == capacity; } }
        public int Count { get { return cache.Count; } }

        public LruCache(int capacity = 64)
        {
            cache = new Dictionary<K, V>();
            this.capacity = capacity;
            this.useOrder = new LinkedList<K>();
        }

        /// <summary>
        /// Checks the cache to see if the key has already been stored.
        /// If so, return the value associated with it.
        /// If not, return null.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool TryGetValue(K key, out V existingValue)
        {
            if (cache.TryGetValue(key, out existingValue))
                return true;
            existingValue = default(V);
            return false;
        }

        public bool ContainsKey(K key)
        {
            return cache.ContainsKey(key);
        }

        /// <summary>
        /// Return value and add key-value pair if key was not already in cache.<br></br>
        /// Otherwise, do nothing and return the value already associated with key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public V SetDefault(K key, V value)
        {
            if (cache.TryGetValue(key, out V existing))
                return existing;
            this[key] = value;
            return value;
        }

        public V this[K key]
        {
            // return cached value for this key
            get { return cache[key]; }
            /// <summary>
            /// Check if the key is already in the cache.<br></br>
            /// If it isn't, and capacity is full,
            /// purge the oldest key and then add the key-value pair.<br></br>
            /// </summary>
            set
            {
                if (useOrder.Count == capacity)
                {
                    K oldestQuery = useOrder.First();
                    useOrder.RemoveFirst();
                    cache.Remove(oldestQuery);
                }
                useOrder.AddLast(key);
                cache[key] = value;
            }
        }

        public K OldestKey()
        {
            if (useOrder.Count == 0)
                return default(K);
            return useOrder.First();
        }

        public K NewestKey()
        {
            if (useOrder.Count == 0)
                return default(K);
            return useOrder.Last();
        }

        /// <summary>
        /// return the value associated with the most recently added key in LruCache,
        /// then remove that key from the cache
        /// </summary>
        /// <returns></returns>
        public V PopNewest()
        {
            if (useOrder.Count == 0)
                return default(V);
            K lastKey = useOrder.Last();
            V lastVal = cache[lastKey];
            cache.Remove(lastKey);
            useOrder.RemoveLast();
            return lastVal;
        }
    }
}