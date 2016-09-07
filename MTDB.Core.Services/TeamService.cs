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
using MTDB.Core.ViewModels;

namespace MTDB.Core.Services
{
    public class TeamService
    {
        #region Constants

        private const string TEAMS_All = "MTDB.team.all";
        private const string TEAMS_PATTERN_KEY = "MTDB.team.";

        #endregion

        #region Fields

        private readonly MtdbContext _dbContext;
        private readonly MemoryCacheManager _memoryCacheManager;

        #endregion

        #region ctor

        public TeamService(MtdbContext dbContext)
        {
            _dbContext = dbContext;
            _memoryCacheManager = new MemoryCacheManager();
        }

        #endregion

        #region Methods

        public async Task<IList<Team>> GetTeams(CancellationToken token)
        {
            var teams = await
                _memoryCacheManager.GetAsync(TEAMS_All, async () =>
                    (await _dbContext.Teams.ToListAsync(token)).OrderBy(t => t.Name).ToList());
            return teams;
        }

        public async Task UpdateTeam(Team team, CancellationToken token)
        {
            var entity = await _dbContext.Teams.FirstAsync(t => t.Id == team.Id, token);
            _dbContext.Entry(entity).CurrentValues.SetValues(team);
            await _dbContext.SaveChangesAsync(token);

            //cache
            _memoryCacheManager.RemoveByPattern(TEAMS_PATTERN_KEY);
        }

        public async Task CreateTeam(Team team, CancellationToken token)
        {
            _dbContext.Teams.Add(team);
            await _dbContext.SaveChangesAsync(token);

            //cache
            _memoryCacheManager.RemoveByPattern(TEAMS_PATTERN_KEY);
        }

        public async Task DeleteTeam(int id, CancellationToken token)
        {
            var entity = await _dbContext.Teams.SingleOrDefaultAsync(t => t.Id == id, token);
            _dbContext.Teams.Remove(entity);
            await _dbContext.SaveChangesAsync(token);

            //cache
            _memoryCacheManager.RemoveByPattern(TEAMS_PATTERN_KEY);
        }

        #endregion
    }
}
