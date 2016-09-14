using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using mvcForum.Web.Areas.Forum;
using mvcForum.Web.Areas.ForumAdmin;
using mvcForum.Web.Areas.ForumAPI;
using MTDB.Areas.Forum;

namespace MTDB.Forums
{
    public class Global : HttpApplication
    {
        private static void RegisterArea<T>(RouteCollection routes, object state) where T : AreaRegistration
        {
            var registration = (AreaRegistration)Activator.CreateInstance(typeof(T));
            var context = new AreaRegistrationContext(registration.AreaName, routes, state);

            var tNamespace = registration.GetType().Namespace;
            if (tNamespace != null)
            {
                context.Namespaces.Add(tNamespace + ".*");
            }

            registration.RegisterArea(context);
        }

        protected void Application_Start(object sender, EventArgs e)
        {
            mvcForum.Web.ApplicationConfiguration.Initialize();

            RegisterArea<ExtraForumAreaRegistration>(RouteTable.Routes, null);
            RegisterArea<ForumsAreaRegistration>(RouteTable.Routes, null);
            RegisterArea<ForumAdminAreaRegistration>(RouteTable.Routes, null);
            RegisterArea<ForumAPIAreaRegistration>(RouteTable.Routes, null);
        }
    }
}