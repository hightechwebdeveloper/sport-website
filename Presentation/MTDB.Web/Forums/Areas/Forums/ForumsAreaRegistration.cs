using System.Web.Mvc;

namespace MTDB.Forums.Areas.Forums
{
    public class ForumsAreaRegistration : AreaRegistration
    {
        public override string AreaName => "Forums";

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                "ForumProfilePreferences",
                "profile/preferences",
                new { controller = "Profile", action = "Preferences" },
                namespaces: new[] { "MTDB.Forums.Areas.Forums.Controllers" }
            );
            context.MapRoute(
                "ShowTopic", 
                "viewtopic/{title}/{id}/{additional}", 
                new { area = "forums", controller = "Topic", action = "Index", additional = UrlParameter.Optional }
            );
            context.MapRoute(
                "ShowCategory", 
                "viewcategory/{title}/{id}", 
                new { area = "forums", controller = "Category", action = "Index" }
            );
            context.MapRoute(
                "ShowProfile", 
                "viewprofile/{id}/{name}", 
                new { area = "forums", controller = "Profile", action = "Index", id = UrlParameter.Optional }
            );
            context.MapRoute(
                "NoAccess", 
                "noaccess", 
                new { area = "forums", controller = "NoAccess", action = "Index" }
            );
            context.MapRoute(
                "ShowForum", 
                "viewforum/{title}/{id}", 
                new { area = "forums", controller = "Forum", action = "Index" }
            );
            context.MapRoute(
                "Forum_default", 
                "{controller}/{action}/{id}", 
                new { area = "forums", controller = "home", action = "index", id = UrlParameter.Optional }, 
                new[] { "MTDB.Forums.Areas.Forums.Controllers" }
            );
        }
    }
}