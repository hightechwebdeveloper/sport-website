using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MTDB.Core.Caching;
using MTDB.Data;
using MTDB.Data.Entities;

namespace MTDB.Core.Services.Catalog
{
    public class TierService
    {
        #region Constants

        private const string TIERS_All = "MTDB.tier.all";
        private const string TIERS_PATTERN_KEY = "MTDB.tier.";

        #endregion

        #region Fields

        private readonly IDbContext _dbContext;
        private readonly MemoryCacheManager _memoryCacheManager;

        #endregion

        #region ctor

        public TierService(IDbContext dbContext,
            MemoryCacheManager memoryCacheManager)
        {
            this._dbContext = dbContext;
            this._memoryCacheManager = memoryCacheManager;
        }

        #endregion

        #region Methods
        
        public async Task<IList<Tier>> GetTiers(CancellationToken token)
        {
            var tiers = await
                _memoryCacheManager.GetAsync(TIERS_All, int.MaxValue, async () =>
                    await _dbContext.Set<Tier>().OrderBy(p => p.SortOrder).ToListAsync(token));
            return tiers;
        }

        public async Task<Tier> GetTierFromOverall(int overall, CancellationToken token)
        {
            var tiers = await GetTiers(token);

            if (overall >= 95)
                return tiers.FirstOrDefault(p => p.Name == "Diamond");

            if (overall >= 90)
                return tiers.FirstOrDefault(p => p.Name == "Amethyst");

            if (overall >= 80)
                return tiers.FirstOrDefault(p => p.Name == "Gold");

            if (overall >= 70)
                return tiers.FirstOrDefault(p => p.Name == "Silver");

            return tiers.FirstOrDefault(p => p.Name == "Bronze");
        }

        public async Task UpdateTier(Tier tier, CancellationToken token)
        {
            var entity = await _dbContext.Set<Tier>().FirstAsync(t => t.Id == tier.Id, token);
            _dbContext.Entry(entity).CurrentValues.SetValues(tier);
            await _dbContext.SaveChangesAsync(token);

            //cache
            _memoryCacheManager.RemoveByPattern(TIERS_PATTERN_KEY);
        }

        public async Task CreateTier(Tier tier, CancellationToken token)
        {
            _dbContext.Set<Tier>().Add(tier);
            await _dbContext.SaveChangesAsync(token);

            //cache
            _memoryCacheManager.RemoveByPattern(TIERS_PATTERN_KEY);
        }

        public async Task DeleteTier(int id, CancellationToken token)
        {
            var entity = await _dbContext.Set<Tier>()
                .SingleOrDefaultAsync(t => t.Id == id, token);
            _dbContext.Set<Tier>().Remove(entity);
            await _dbContext.SaveChangesAsync(token);

            //cache
            _memoryCacheManager.RemoveByPattern(TIERS_PATTERN_KEY);
        }

        #endregion
    }
}
