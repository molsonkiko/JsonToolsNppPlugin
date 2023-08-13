using JSON_Tools.Utils;
using System.Text;

namespace JSON_Tools.Tests
{
    public class LruCacheTests
    {
        public static readonly char[] keys = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()".ToCharArray();
        
        public static bool Test()
        {
            int[] sizes = new int[] { 5, 10, 17, 45, 64 }; // include prime, non-prime odd, and some evens
            bool failed = false;
            foreach (int size in sizes)
            {
                int failures = 0;
                int ii = 0;
                int test_count = size * 3;
                LruCache<char, char> cache = new LruCache<char, char>(size);
                StringBuilder keys_used = new StringBuilder();
                for (; ii < test_count && failures < 10; ii++)
                {
                    char key = RemesPathFuzzTester.RandomChoice(keys);
                    keys_used.Append(key);
                    bool was_at_cap = cache.isFull;
                    char oldest = cache.OldestKey();
                    bool key_already_in = cache.ContainsKey(key);
                    cache.SetDefault(key, key);
                    if (cache.cache.Count != cache.use_order.Count)
                    {
                        failures++;
                        Npp.AddLine($"LruCache linked list size ({cache.use_order.Count}) != dictionary size ({cache.cache.Count})");
                    }
                    if (cache.cache.Count > cache.capacity)
                    {
                        failures++;
                        Npp.AddLine($"LruCache exceeded capacity {size}");
                    }
                    if (!cache.ContainsKey(key) || !cache.use_order.Contains(key))
                    {
                        failures++;
                        Npp.AddLine($"LruCache did not contain key '{key}' after adding key '{key}'");
                    }
                    // verify that the oldest was evicted if cache was at capacity and a key not already present was added
                    if (was_at_cap && !key_already_in && (key != oldest) && (cache.ContainsKey(oldest) || cache.use_order.Contains(oldest)))
                    {
                        failures++;
                        Npp.AddLine($"After reaching capacity {size}, LruCache did not evict oldest key '{oldest}' after adding key '{key}'");
                    }
                }
                Npp.AddLine($"Ran {test_count} tests for LruCache with size {size} and failed {failures}");
                failed |= failures > 0;
            }
            return failed;
        }
    }
}
