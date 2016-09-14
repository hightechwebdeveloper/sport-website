using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Threading;
using System.Threading.Tasks;

namespace MTDB.Data
{
    public interface IDbContext
    {
        DbSet<TEntity> Set<TEntity>() where TEntity : class;

        int SaveChanges();

        Task<int> SaveChangesAsync(CancellationToken cancellation);

        DbEntityEntry Entry(object entity);

        DbRawSqlQuery<TElement> SqlQuery<TElement>(string sql, params object[] parameters);
    }
}
