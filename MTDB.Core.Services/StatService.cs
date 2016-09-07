using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MTDB.Core.Caching;
using MTDB.Core.EntityFramework;
using MTDB.Core.EntityFramework.Entities;

namespace MTDB.Core.Services
{
    public class StatService
    {
        #region Constants

        private const string STATS_All = "MTDB.stats.all";

        #endregion

        #region Fields

        private readonly MtdbContext _dbContext;
        private readonly MemoryCacheManager _memoryCacheManager;

        #endregion

        #region ctor

        public StatService(MtdbContext dbContext)
        {
            _dbContext = dbContext;
            _memoryCacheManager = new MemoryCacheManager();
        }

        #endregion

        #region Methods

        public async Task<IList<Stat>> GetStats(CancellationToken token)
        {
            var stats = await
                _memoryCacheManager.GetAsync(STATS_All, async () =>
                    await _dbContext.Stats.ToListAsync(token));
            return stats;
        }

        #endregion
    }
}
