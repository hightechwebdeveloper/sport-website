using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PagedList;

namespace MTDB.Core.ViewModels
{
    public class PagedResults<T>
    {

        public PagedResults(IEnumerable<T> results, int pageNumber, int pageSize, int recordCount)
        {
            if (results == null)
            {
                results = new List<T>();
                recordCount = 0;
            }

            Results = new StaticPagedList<T>(results, pageNumber, pageSize, recordCount);
        }

        public PagedResults(IEnumerable<T> results, int pageNumber, int pageSize, int recordCount, string sortedBy, SortOrder sortOrder)
            : this(results, pageNumber, pageSize, recordCount)
        {
            SortedBy = sortedBy;
            SortOrder = sortOrder;
        }

        public string SortedBy { get; set; }
        public SortOrder SortOrder { get; set; }
        public IPagedList<T> Results { get; set; }
    }
}
