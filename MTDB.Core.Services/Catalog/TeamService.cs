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
    public class TeamService
    {
        #region Constants

        private const string TEAMS_All = "MTDB.team.all";
        private const string TEAMS_PATTERN_KEY = "MTDB.team.";

        #endregion

        #region Fields

        private readonly IDbContext _dbContext;
        private readonly MemoryCacheManager _memoryCacheManager;

        #endregion

        #region ctor

        public TeamService(IDbContext dbContext,
            MemoryCacheManager memoryCacheManager)
        {
            this._dbContext = dbContext;
            this._memoryCacheManager = memoryCacheManager;
        }

        #endregion

        #region Methods

        public async Task<IList<Team>> GetTeams(CancellationToken token)
        {
            var teams = await
                _memoryCacheManager.GetAsync(TEAMS_All, int.MaxValue, async () =>
                    (await _dbContext.Set<Team>().Include(t => t.Division.Conference).ToListAsync(token)).OrderBy(t => t.Name).ToList());
            return teams;
        }

        public async Task UpdateTeam(Team team, CancellationToken token)
        {
            var entity = await _dbContext.Set<Team>().FirstAsync(t => t.Id == team.Id, token);
            _dbContext.Entry(entity).CurrentValues.SetValues(team);
            await _dbContext.SaveChangesAsync(token);

            //cache
            _memoryCacheManager.RemoveByPattern(TEAMS_PATTERN_KEY);
        }

        public async Task CreateTeam(Team team, CancellationToken token)
        {
            _dbContext.Set<Team>().Add(team);
            await _dbContext.SaveChangesAsync(token);

            //cache
            _memoryCacheManager.RemoveByPattern(TEAMS_PATTERN_KEY);
        }

        public async Task DeleteTeam(int id, CancellationToken token)
        {
            var entity = await _dbContext.Set<Team>().SingleOrDefaultAsync(t => t.Id == id, token);
            _dbContext.Set<Team>().Remove(entity);
            await _dbContext.SaveChangesAsync(token);

            //cache
            _memoryCacheManager.RemoveByPattern(TEAMS_PATTERN_KEY);
        }

        #endregion
    }
}
