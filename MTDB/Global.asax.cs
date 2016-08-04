using System;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using mvcForum.Web.Areas.Forum;
using mvcForum.Web.Areas.ForumAdmin;
using mvcForum.Web.Areas.ForumAPI;
using Microsoft.ApplicationInsights.Extensibility;
using MTDB.Areas.Forum;

namespace MTDB
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            mvcForum.Web.ApplicationConfiguration.Initialize();

            RouteConfig.RegisterRoutes(RouteTable.Routes);
            RegisterArea<ExtraForumAreaRegistration>(RouteTable.Routes, null);
            RegisterArea<ForumAreaRegistration>(RouteTable.Routes, null);
            RegisterArea<ForumAdminAreaRegistration>(RouteTable.Routes, null);
            RegisterArea<ForumAPIAreaRegistration>(RouteTable.Routes, null);

            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            MvcHandler.DisableMvcResponseHeader = true;

            #if DEBUG
            TelemetryConfiguration.Active.DisableTelemetry = true;
            #endif
        }

        public static void RegisterArea<T>(RouteCollection routes, object state) where T : AreaRegistration
        {
            AreaRegistration registration = (AreaRegistration)Activator.CreateInstance(typeof(T));

            AreaRegistrationContext context = new AreaRegistrationContext(registration.AreaName, routes, state);

            var tNamespace = registration.GetType().Namespace;
            if (tNamespace != null)
            {
                context.Namespaces.Add(tNamespace + ".*");
            }

            registration.RegisterArea(context);
        }
    }
}
