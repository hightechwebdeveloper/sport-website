using System.Web;
using MTDB.Core;

namespace MTDB.Web.Framework
{
    public class WebLocationContext : ILocationContext
    {
        public Location CurrentLocation
        {
            get
            {
                return HttpContext.Current != null && HttpContext.Current.Request.Url.AbsolutePath.StartsWith("/17")
                    ? Location.K17
                    : Location.Default;
            }
        }
    }
}
