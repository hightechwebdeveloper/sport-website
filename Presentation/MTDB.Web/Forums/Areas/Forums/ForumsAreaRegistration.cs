using System.Web.Mvc;

namespace mvcForum.Web.Areas.Forum
{
    public class ForumsAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get
            {
                return "Forums";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute("ShowTopic", "viewtopic/{title}/{id}/{additional}", (object)new
            {
                area = "forums",
                controller = "Topic",
                action = "Index",
                additional = UrlParameter.Optional
            });
            context.MapRoute("ShowCategory", "viewcategory/{title}/{id}", (object)new
            {
                area = "forums",
                controller = "Category",
                action = "Index"
            });
            context.MapRoute("ShowProfile", "viewprofile/{id}/{name}", (object)new
            {
                area = "forums",
                controller = "Profile",
                action = "Index",
                id = UrlParameter.Optional
            });
            context.MapRoute("NoAccess", "noaccess", (object)new
            {
                area = "forums",
                controller = "NoAccess",
                action = "Index"
            });
            context.MapRoute("ShowForum", "viewforum/{title}/{id}", (object)new
            {
                area = "forums",
                controller = "Forum",
                action = "Index"
            });
            context.MapRoute("Forum_default", "{controller}/{action}/{id}", (object)new
            {
                area = "forums",
                controller = "home",
                action = "index",
                id = UrlParameter.Optional
            }, new string[1]
            {
                "mvcForum.Web.Areas.Forum.Controllers"
            });
        }
    }
}