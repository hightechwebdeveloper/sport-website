using System.Web.Mvc;
using System.Web.Mvc.Routing;
using System.Web.Routing;

namespace MTDB
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            //routes.MapRoute(
            //    "ForumProfilePreferences",
            //    "forum/profile/preferences",
            //    new { area = "forum", controller = "ExtraProfile", action = "Preferences" },
            //    namespaces: new[] { "mvcForum.Web.Areas.Forum.Controllers" }
            //);

            //routes.MapRoute(
            //    "ForumCurrentUser",
            //    "forum/profile/current",
            //    new { controller = "Profile", action = "Preferences" },
            //    namespaces: new[] { "MTDB.Areas.Forum.Controllers" }
            //);

            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("elmah.axd");

            routes.AppendTrailingSlash = false;
            routes.LowercaseUrls = true;
            //routes.MapMvcAttributeRoutes(new CentralizedPrefixProvider("nba2k16"));
            routes.MapMvcAttributeRoutes();
        }
    }

    public class CentralizedPrefixProvider : DefaultDirectRouteProvider
    {
        private readonly string _centralizedPrefix;

        public CentralizedPrefixProvider(string centralizedPrefix)
        {
            _centralizedPrefix = centralizedPrefix;
        }

        protected override string GetRoutePrefix(ControllerDescriptor controllerDescriptor)
        {
            var existingPrefix = base.GetRoutePrefix(controllerDescriptor);
            if (existingPrefix == null) return _centralizedPrefix;

            return string.Format("{0}/{1}", _centralizedPrefix, existingPrefix);
        }
    }

}
