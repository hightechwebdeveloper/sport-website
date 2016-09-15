using System.Web.Mvc;
using System.Web.Routing;

namespace MTDB
{
    public static class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            AreaRegistration.RegisterAllAreas();

            routes.AppendTrailingSlash = false;
            routes.LowercaseUrls = true;
            routes.MapMvcAttributeRoutes();
        }
    }
}
