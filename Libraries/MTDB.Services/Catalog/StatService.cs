using System.Collections.Generic;
using System.Data.Entity;
using System.Threading;
using System.Threading.Tasks;
using MTDB.Core.Caching;
using MTDB.Data;
using MTDB.Data.Entities;

namespace MTDB.Core.Services.Catalog
{
    public class StatService
    {
        #region Constants

        private const string STATS_All = "MTDB.stats.all";

        #endregion

        #region Fields

        private readonly IDbContext _dbContext;
        private readonly MemoryCacheManager _memoryCacheManager;

        #endregion

        #region ctor

        public StatService(IDbContext dbContext,
            MemoryCacheManager memoryCacheManager)
        {
            this._dbContext = dbContext;
            this._memoryCacheManager = memoryCacheManager;
        }

        #endregion

        #region Methods

        public async Task<IList<Stat>> GetStats(CancellationToken token)
        {
            var stats = await
                _memoryCacheManager.GetAsync(STATS_All, int.MaxValue, async () =>
                    await _dbContext.Set<Stat>().Include(s => s.Category).ToListAsync(token));
            return stats;
        }

        #endregion
    }
}
