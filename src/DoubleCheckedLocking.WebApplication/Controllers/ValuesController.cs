using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace DoubleCheckedLocking.WebApplication.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private static readonly object padlock = new object();
        private IMemoryCache _cache;

        public ValuesController(IMemoryCache memoryCache)
        {
            this._cache = memoryCache;
        }

        // GET api/values
        [HttpGet]
        public ActionResult<string> Get()
        {
            // 取得快取值
            var cacheEntry = this._cache.Get<DateTime>("key");

            // 第一次檢查
            if (cacheEntry == null)
            {
                // 鎖定
                lock (padlock)
                {
                    // 第二次檢查
                    if (this._cache.Get<DateTime>("key") == null)
                    {
                        // 無快取, 所以重新取值
                        cacheEntry = DateTime.Now;

                        // 設定快取過期時間
                        var cacheEntryOptions = new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(TimeSpan.FromSeconds(3));

                        // 加入快取
                        _cache.Set("key", cacheEntry, cacheEntryOptions);
                    }
                }
            }

            return cacheEntry.ToString();
        }
    }
}