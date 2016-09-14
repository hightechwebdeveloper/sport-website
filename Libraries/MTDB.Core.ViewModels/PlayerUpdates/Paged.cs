using System.Collections.Generic;

namespace MTDB.Core.ViewModels.PlayerUpdates
{

    public class Paged<T>
    {
        public IEnumerable<T> Results { get; set; } 
        public int TotalCount { get; set; }
    }
}
