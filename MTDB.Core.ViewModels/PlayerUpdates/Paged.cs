using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTDB.Core.ViewModels.PlayerUpdates
{

    public class Paged<T>
    {
        public IEnumerable<T> Results { get; set; } 
        public int TotalCount { get; set; }
    }
}
