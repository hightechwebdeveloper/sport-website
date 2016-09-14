using System;
using System.Threading.Tasks;

namespace MTDB.Core.Caching
{
    public static class CacheExtensions
    {
        public static async Task<T> GetAsync<T>(this ICacheManager cacheManager, string key, Func<Task<T>> acquire)
        {
            return await GetAsync(cacheManager, key, 60, acquire);
        }

        public static async Task<T> GetAsync<T>(this ICacheManager cacheManager, string key, int cacheTime, Func<Task<T>> acquire)
        {
            if (cacheManager.IsSet(key))
            {
                return cacheManager.Get<T>(key);
            }

            var result = await acquire();
            if (cacheTime > 0)
                cacheManager.Set(key, result, cacheTime);
            return result;
        }
    }
}
