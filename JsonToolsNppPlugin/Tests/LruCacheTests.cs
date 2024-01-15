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
                int testCount = size * 3;
                LruCache<char, char> cache = new LruCache<char, char>(size);
                StringBuilder keysUsed = new StringBuilder();
                for (; ii < testCount && failures < 10; ii++)
                {
                    char key = RemesPathFuzzTester.RandomChoice(keys);
                    keysUsed.Append(key);
                    bool wasAtCap = cache.isFull;
                    char oldest = cache.OldestKey();
                    bool keyAlreadyIn = cache.ContainsKey(key);
                    cache.SetDefault(key, key);
                    if (cache.cache.Count != cache.useOrder.Count)
                    {
                        failures++;
                        Npp.AddLine($"LruCache linked list size ({cache.useOrder.Count}) != dictionary size ({cache.cache.Count})");
                    }
                    if (cache.cache.Count > cache.capacity)
                    {
                        failures++;
                        Npp.AddLine($"LruCache exceeded capacity {size}");
                    }
                    if (!cache.ContainsKey(key) || !cache.useOrder.Contains(key))
                    {
                        failures++;
                        Npp.AddLine($"LruCache did not contain key '{key}' after adding key '{key}'");
                    }
                    // verify that the oldest was evicted if cache was at capacity and a key not already present was added
                    if (wasAtCap && !keyAlreadyIn && (key != oldest) && (cache.ContainsKey(oldest) || cache.useOrder.Contains(oldest)))
                    {
                        failures++;
                        Npp.AddLine($"After reaching capacity {size}, LruCache did not evict oldest key '{oldest}' after adding key '{key}'");
                    }
                }
                Npp.AddLine($"Ran {testCount} tests for LruCache with size {size} and failed {failures}");
                failed |= failures > 0;
            }
            return failed;
        }
    }
}
