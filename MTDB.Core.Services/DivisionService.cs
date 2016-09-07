using System.Collections.Generic;
using System.Data.Entity;
using System.Threading;
using System.Threading.Tasks;
using MTDB.Core.Caching;
using MTDB.Core.EntityFramework;
using MTDB.Core.EntityFramework.Entities;

namespace MTDB.Core.Services
{
    public class DivisionService
    {
        #region Constants

        private const string DIVISIONS_All = "MTDB.division.all";
        private const string DIVISIONS_PATTERN_KEY = "MTDB.division.";

        #endregion

        #region Fields

        private readonly MtdbContext _dbContext;
        private readonly MemoryCacheManager _memoryCacheManager;

        #endregion

        #region ctor

        public DivisionService(MtdbContext dbContext)
        {
            _dbContext = dbContext;
            _memoryCacheManager = new MemoryCacheManager();
        }

        #endregion

        #region Methods

        public async Task<IList<Division>> GetDivisions(CancellationToken token)
        {
            var divisions = await
                _memoryCacheManager.GetAsync(DIVISIONS_All, async () =>
                    await _dbContext.Divisions.Include(d => d.Conference).ToListAsync(token));
            return divisions;
        }

        public async Task CreateDivision(Division division, CancellationToken token)
        {
            _dbContext.Divisions.Add(division);
            await _dbContext.SaveChangesAsync(token);

            //cache
            _memoryCacheManager.RemoveByPattern(DIVISIONS_PATTERN_KEY);
        }

        public async Task DeleteDivision(int id, CancellationToken token)
        {
            var entity = await _dbContext.Divisions
                .SingleOrDefaultAsync(t => t.Id == id, token);
            _dbContext.Divisions.Remove(entity);
            await _dbContext.SaveChangesAsync(token);

            //cache
            _memoryCacheManager.RemoveByPattern(DIVISIONS_PATTERN_KEY);
        }

        #endregion
    }
}