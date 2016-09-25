using System.Linq;
using System.Web.Mvc;
using mvcForum.Web.Interfaces;

namespace mvcForum.Web.Areas.ForumAdmin
{
    public class AdminAreaRegistration : AreaRegistration
    {
        public override string AreaName => "ForumAdmin";

        public override void RegisterArea(AreaRegistrationContext context)
        {
            var source = new[] { "mvcForum.Web.Areas.ForumAdmin.Controllers"}
                .Concat(
                    DependencyResolver.Current.GetServices<IAntiSpamConfigurationController>()
                    .Select(c => c.GetType().Namespace)
                    .Distinct()
                    .Concat(
                        DependencyResolver.Current.GetServices<ISearchConfigurationController>()
                        .Select(c => c.GetType().Namespace)
                        .Distinct()
                    )
                );
            context.MapRoute(
                "ForumAdmin_default", 
                "admin/{controller}/{action}/{id}", 
                new { area = "ForumAdmin", controller = "Home", action = "Index", id = UrlParameter.Optional },
                source.Distinct().ToArray()
            );
        }
    }
}