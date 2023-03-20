using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

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
        public V Get(K key)
        {
            if (cache.TryGetValue(key, out V existing_value))
            {
                return existing_value;
            }
            return default(V);
        }

        /// <summary>
        /// If the key is already in the cache, do nothing.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(K key, V value)
        {
            if (cache.ContainsKey(key)) { return; }
            this[key] = value;
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
            return use_order.First();
        }
    }
}