using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MTDB.Core.Services
{
    public class PagedList<T> : List<T>, IPagedList<T>
    {
        private PagedList()
        {
        }

        public static async Task<PagedList<T>> ExecuteAsync(IQueryable<T> source, int pageIndex, int pageSize,
            CancellationToken cancellation)
        {
            var pagedList = new PagedList<T>();

            var total = await source.CountAsync(cancellation);
            pagedList.TotalCount = total;
            pagedList.TotalPages = total / pageSize;

            if (total % pageSize > 0)
                pagedList.TotalPages++;

            pagedList.PageSize = pageSize;
            pagedList.PageIndex = pageIndex;
            pagedList.AddRange(await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync(cancellation));

            return pagedList;
        }

        public static PagedList<T> Execute(IList<T> source, int pageIndex, int pageSize)
        {
            var pagedList = new PagedList<T>();

            pagedList.TotalCount = source.Count;
            pagedList.TotalPages = pagedList.TotalCount / pageSize;

            if (pagedList.TotalCount % pageSize > 0)
                pagedList.TotalPages++;

            pagedList.PageSize = pageSize;
            pagedList.PageIndex = pageIndex;
            pagedList.AddRange(source.Skip(pageIndex * pageSize).Take(pageSize).ToList());

            return pagedList;
        }

        public static PagedList<T> Execute(IEnumerable<T> source, int pageIndex, int pageSize, int totalCount)
        {
            var pagedList = new PagedList<T>();

            pagedList.TotalCount = totalCount;
            pagedList.TotalPages = pagedList.TotalCount / pageSize;

            if (pagedList.TotalCount % pageSize > 0)
                pagedList.TotalPages++;

            pagedList.PageSize = pageSize;
            pagedList.PageIndex = pageIndex;
            pagedList.AddRange(source);

            return pagedList;
        }

        public int PageIndex { get; private set; }
        public int PageSize { get; private set; }
        public int TotalCount { get; private set; }
        public int TotalPages { get; private set; }

        public bool HasPreviousPage => PageIndex > 0;

        public bool HasNextPage => PageIndex + 1 < TotalPages;
    }
}
