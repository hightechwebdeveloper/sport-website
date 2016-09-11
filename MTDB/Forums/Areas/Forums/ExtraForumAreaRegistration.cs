using System.Web.Mvc;

namespace MTDB.Areas.Forum
{
    public class ExtraForumAreaRegistration : AreaRegistration
    {
        public override string AreaName => "Forums";

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                "ForumProfilePreferences",
                "profile/preferences",
                new { controller = "Profile", action = "Preferences" },
                namespaces: new[] { "MTDB.Areas.Forum.Controllers" }
            );
        }
    }
}