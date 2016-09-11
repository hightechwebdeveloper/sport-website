using System.Collections.Generic;
using System.Data.Entity;
using System.Threading;
using System.Threading.Tasks;
using MTDB.Core.Caching;
using MTDB.Data;
using MTDB.Data.Entities;

namespace MTDB.Core.Services.Catalog
{
    public class ThemeService
    {
        #region Constants

        private const string THEMES_All = "MTDB.theme.all";
        private const string THEMES_PATTERN_KEY = "MTDB.theme.";

        #endregion

        #region Fields

        private readonly IDbContext _dbContext;
        private readonly MemoryCacheManager _memoryCacheManager;

        #endregion

        #region ctor

        public ThemeService(IDbContext dbContext,
            MemoryCacheManager memoryCacheManager)
        {
            this._dbContext = dbContext;
            this._memoryCacheManager = memoryCacheManager;
        }

        #endregion

        #region Methods

        public async Task<IEnumerable<Theme>> GetThemes(CancellationToken token)
        {
            var themes = await
                _memoryCacheManager.GetAsync(THEMES_All, int.MaxValue, async () =>
                    await _dbContext.Set<Theme>().ToListAsync(token));
            return themes;
        }

        public async Task CreateTheme(Theme theme, CancellationToken token)
        {
            _dbContext.Set<Theme>().Add(theme);
            await _dbContext.SaveChangesAsync(token);

            //cache
            _memoryCacheManager.RemoveByPattern(THEMES_PATTERN_KEY);
        }

        public async Task UpdateTheme(Theme theme, CancellationToken token)
        {
            var entity = await _dbContext.Set<Theme>().FirstAsync(t => t.Id == theme.Id, token);
            _dbContext.Entry(entity).CurrentValues.SetValues(theme);
            await _dbContext.SaveChangesAsync(token);

            //cache
            _memoryCacheManager.RemoveByPattern(THEMES_PATTERN_KEY);
        }

        public async Task DeleteTheme(int id, CancellationToken token)
        {
            var entity = await _dbContext.Set<Theme>().SingleOrDefaultAsync(t => t.Id == id, token);
            _dbContext.Set<Theme>().Remove(entity);
            await _dbContext.SaveChangesAsync(token);
            
            //cache
            _memoryCacheManager.RemoveByPattern(THEMES_PATTERN_KEY);
        }

        #endregion
    }
}
