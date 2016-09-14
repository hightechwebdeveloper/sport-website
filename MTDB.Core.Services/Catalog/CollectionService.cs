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
    public class CollectionService
    {
        #region Constants

        private const string COLLECTIONS_All = "MTDB.collection.all";
        private const string COLLECTIONS_PATTERN_KEY = "MTDB.collection.";

        #endregion

        #region Fields

        private readonly IDbContext _dbContext;
        private readonly MemoryCacheManager _memoryCacheManager;

        #endregion

        #region ctor

        public CollectionService(IDbContext dbContext,
            MemoryCacheManager memoryCacheManager)
        {
            this._dbContext = dbContext;
            this._memoryCacheManager = memoryCacheManager;
        }

        #endregion

        #region Methods

        public async Task<IList<Collection>> GetCollections(CancellationToken token)
        {
            var collections = await
                _memoryCacheManager.GetAsync(COLLECTIONS_All, int.MaxValue, async () =>
                    (await _dbContext.Set<Collection>().ToListAsync(token)).OrderBy(t => t.Name).ToList());
            return collections;
        }

        public async Task UpdateCollection(Collection collection, CancellationToken token)
        {
            var entity = await _dbContext.Set<Collection>().FirstAsync(t => t.Id == collection.Id, token);
            _dbContext.Entry(entity).CurrentValues.SetValues(collection);
            await _dbContext.SaveChangesAsync(token);

            //cache
            _memoryCacheManager.RemoveByPattern(COLLECTIONS_PATTERN_KEY);
        }

        public async Task CreateCollection(Collection collection, CancellationToken token)
        {
            _dbContext.Set<Collection>().Add(collection);
            await _dbContext.SaveChangesAsync(token);

            //cache
            _memoryCacheManager.RemoveByPattern(COLLECTIONS_PATTERN_KEY);
        }

        public async Task DeleteCollection(int id, CancellationToken token)
        {
            var entity = await _dbContext.Set<Collection>().SingleOrDefaultAsync(t => t.Id == id, token);
            _dbContext.Set<Collection>().Remove(entity);
            await _dbContext.SaveChangesAsync(token);

            //cache
            _memoryCacheManager.RemoveByPattern(COLLECTIONS_PATTERN_KEY);
        }

        #endregion
    }
}
