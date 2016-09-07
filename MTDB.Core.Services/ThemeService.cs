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
    public class ThemeService
    {
        #region Constants

        private const string THEMES_All = "MTDB.theme.all";
        private const string THEMES_PATTERN_KEY = "MTDB.theme.";

        #endregion

        #region Fields

        private readonly MtdbContext _dbContext;
        private readonly MemoryCacheManager _memoryCacheManager;

        #endregion

        #region ctor

        public ThemeService(MtdbContext dbContext)
        {
            _dbContext = dbContext;
            _memoryCacheManager = new MemoryCacheManager();
        }

        #endregion

        #region Methods

        public async Task<IEnumerable<Theme>> GetThemes(CancellationToken token)
        {
            var themes = await
                _memoryCacheManager.GetAsync(THEMES_All, async () =>
                    await _dbContext.Themes.ToListAsync(token));
            return themes;
        }

        public async Task CreateTheme(Theme theme, CancellationToken token)
        {
            _dbContext.Themes.Add(theme);
            await _dbContext.SaveChangesAsync(token);

            //cache
            _memoryCacheManager.RemoveByPattern(THEMES_PATTERN_KEY);
        }

        public async Task UpdateTheme(Theme theme, CancellationToken token)
        {
            var entity = await _dbContext.Themes.FirstAsync(t => t.Id == theme.Id, token);
            _dbContext.Entry(entity).CurrentValues.SetValues(theme);
            await _dbContext.SaveChangesAsync(token);

            //cache
            _memoryCacheManager.RemoveByPattern(THEMES_PATTERN_KEY);
        }

        public async Task DeleteTheme(int id, CancellationToken token)
        {
            var entity = await _dbContext.Themes.SingleOrDefaultAsync(t => t.Id == id, token);
            _dbContext.Themes.Remove(entity);
            await _dbContext.SaveChangesAsync(token);
            
            //cache
            _memoryCacheManager.RemoveByPattern(THEMES_PATTERN_KEY);
        }

        #endregion
    }
}
