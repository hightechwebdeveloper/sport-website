using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using PagedList;

namespace MTDB.Core.ViewModels
{
    public class PagedAndSorted<T> : Paged<T>
    {
        public PagedAndSorted(IEnumerable<T> results, int pageNumber, int pageSize, int recordCount, string sortedBy, SortOrder sortOrder)
            : base(results, pageNumber, pageSize, recordCount)
        {
            SortedBy = sortedBy;
            SortOrder = sortOrder;
        }

        public string SortedBy { get; set; }
        public SortOrder SortOrder { get; set; }
    }

    public class Paged<T>
    {
        public Paged(IEnumerable<T> results, int pageSize, int pageNumber, int recordCount)
        {
            if (results == null)
            {
                results = new List<T>();
                recordCount = 0;
            }

            Results = new StaticPagedList<T>(results, pageNumber, pageSize, recordCount);
        }

        public IPagedList<T>  Results { get; set; }
        public int PageSize { get; set; }
    }
}
