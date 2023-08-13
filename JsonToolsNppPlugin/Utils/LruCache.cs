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
        public LinkedList<K> use_order;
        public bool isFull { get { return cache.Count == capacity; } }
        public int Count { get { return cache.Count; } }

        public LruCache(int capacity = 64)
        {
            cache = new Dictionary<K, V>();
            this.capacity = capacity;
            this.use_order = new LinkedList<K>();
        }

        /// <summary>
        /// Checks the cache to see if the key has already been stored.
        /// If so, return the value associated with it.
        /// If not, return null.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool TryGetValue(K key, out V existing_value)
        {
            if (cache.TryGetValue(key, out existing_value))
                return true;
            existing_value = default(V);
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
                if (use_order.Count == capacity)
                {
                    K oldest_query = use_order.First();
                    use_order.RemoveFirst();
                    cache.Remove(oldest_query);
                }
                use_order.AddLast(key);
                cache[key] = value;
            }
        }

        public K OldestKey()
        {
            if (use_order.Count == 0)
                return default(K);
            return use_order.First();
        }

        public K NewestKey()
        {
            if (use_order.Count == 0)
                return default(K);
            return use_order.Last();
        }

        /// <summary>
        /// return the value associated with the most recently added key in LruCache,
        /// then remove that key from the cache
        /// </summary>
        /// <returns></returns>
        public V PopNewest()
        {
            if (use_order.Count == 0)
                return default(V);
            K lastKey = use_order.Last();
            V lastVal = cache[lastKey];
            cache.Remove(lastKey);
            use_order.RemoveLast();
            return lastVal;
        }
    }
}