using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTDB.Core.ViewModels
{
    public class CollectionsViewModel
    {
        public IEnumerable<CollectionViewModel> Current { get; set; }
        public IEnumerable<CollectionViewModel> CurrentFreeAgents { get; set; }
        public IEnumerable<CollectionViewModel> Dynamic { get; set; }
        public IEnumerable<CollectionViewModel>  DynamicFreeAgents { get; set; }

        public IEnumerable<CollectionViewModel> Historic { get; set; }
        public IEnumerable<CollectionViewModel> Other { get; set; }
    }

    public class CollectionViewModel
    {
        public string Name { get; set; }
        public string Group { get; set; }
        public int DisplayOrder { get; set; }
    }
}
