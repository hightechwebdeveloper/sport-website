using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MTDB.Models.Shared
{
    public class StatModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Value { get; set; }
        public int CategoryId { get; set; }
    }
}