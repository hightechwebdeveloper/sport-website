using System.Collections.Generic;
using System.Data.Entity;
using System.Threading;
using System.Threading.Tasks;
using MTDB.Core.Caching;
using MTDB.Data;
using MTDB.Core.Domain;

namespace MTDB.Core.Services.Catalog
{
    public class DivisionService
    {
        #region Constants

        private const string DIVISIONS_All = "MTDB.division.all";
        private const string DIVISIONS_PATTERN_KEY = "MTDB.division.";

        #endregion

        #region Fields

        private readonly IDbContext _dbContext;
        private readonly MemoryCacheManager _memoryCacheManager;

        #endregion

        #region ctor

        public DivisionService(IDbContext dbContext,
            MemoryCacheManager memoryCacheManager)
        {
            this._dbContext = dbContext;
            this._memoryCacheManager = memoryCacheManager;
        }

        #endregion

        #region Methods

        public async Task<IList<Division>> GetDivisions(CancellationToken token)
        {
            var divisions = await
                _memoryCacheManager.GetAsync(DIVISIONS_All, int.MaxValue, async () =>
                    await _dbContext.Set<Division>().Include(d => d.Conference).ToListAsync(token));
            return divisions;
        }

        public async Task CreateDivision(Division division, CancellationToken token)
        {
            _dbContext.Set<Division>().Add(division);
            await _dbContext.SaveChangesAsync(token);

            //cache
            _memoryCacheManager.RemoveByPattern(DIVISIONS_PATTERN_KEY);
        }

        public async Task DeleteDivision(int id, CancellationToken token)
        {
            var entity = await _dbContext.Set<Division>()
                .SingleOrDefaultAsync(t => t.Id == id, token);
            _dbContext.Set<Division>().Remove(entity);
            await _dbContext.SaveChangesAsync(token);

            //cache
            _memoryCacheManager.RemoveByPattern(DIVISIONS_PATTERN_KEY);
        }

        #endregion
    }
}