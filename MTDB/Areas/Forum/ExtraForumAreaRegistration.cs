//using System.Web.Mvc;

//namespace MTDB.Areas.Forum
//{
//    public class ExtraForumAreaRegistration : AreaRegistration
//    {
//        public override string AreaName
//        {
//            get
//            {
//                return "Forum";
//            }
//        }

//        public override void RegisterArea(AreaRegistrationContext context)
//        {
//            context.MapRoute(
//                "ForumProfilePreferences",
//                "forum/profile/preferences",
//                new { controller = "ExtraProfile", action = "Preferences" },
//                namespaces: new[] { "mvcForum.Web.Areas.Forum.Controllers" }
//            );
//        }
//    }
//}