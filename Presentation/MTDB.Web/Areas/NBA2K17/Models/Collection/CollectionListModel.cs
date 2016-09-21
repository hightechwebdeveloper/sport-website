using System.Collections.Generic;

namespace MTDB.Areas.NBA2K17.Models.Collection
{
    public class CollectionListModel
    {
        public CollectionListModel()
        {
            this.Current = new List<CollectionItemModel>();
            this.CurrentFreeAgents = new List<CollectionItemModel>();
            this.Dynamic = new List<CollectionItemModel>();
            this.DynamicFreeAgents = new List<CollectionItemModel>();
            this.Historic = new List<CollectionItemModel>();
            this.Other = new List<CollectionItemModel>();
        }

        public IList<CollectionItemModel> Current { get; set; }
        public IList<CollectionItemModel> CurrentFreeAgents { get; set; }
        public IList<CollectionItemModel> Dynamic { get; set; }
        public IList<CollectionItemModel> DynamicFreeAgents { get; set; }
        public IList<CollectionItemModel> Historic { get; set; }
        public IList<CollectionItemModel> Other { get; set; }

        public class CollectionItemModel
        {
            public string Name { get; set; }
            public string Group { get; set; }
            public int DisplayOrder { get; set; }
        }
    }
}
