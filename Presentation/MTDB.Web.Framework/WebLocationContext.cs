using System.Web;
using MTDB.Core;

namespace MTDB.Web.Framework
{
    public class WebLocationContext : ILocationContext
    {
        public Location CurrentLocation => HttpContext.Current != null && HttpContext.Current.Request.Url.AbsolutePath.StartsWith("/16")
            ? Location.K16
            : Location.K17;
    }
}
