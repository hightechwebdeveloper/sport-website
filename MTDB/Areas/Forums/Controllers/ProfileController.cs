using System.Collections.Generic;
using System.Web.Mvc;
using ApplicationBoilerplate.DataProvider;
using mvcForum.Web.Controllers;
using mvcForum.Web.Helpers;
using mvcForum.Web.Interfaces;
using mvcForum.Web.ViewModels;
using MTDB.Areas.Forum.Models;

namespace MTDB.Areas.Forum.Controllers
{
    public class ProfileController : ThemedForumBaseController
    {
        public ProfileController(IWebUserProvider userProvider, IContext context)
            : base(userProvider, context)
        {
        }

        [Authorize]
        public ActionResult Preferences()
        {
            var user = this.ActiveUser;
            var model = new UserPreferencesModel();
            model.User = new UserViewModel(user);
            model.Id = user.Id;
            model.Culture = user.Culture;
            model.Timezone = user.Timezone;
            model.Path = new Dictionary<string, string>();
            return View("UpdatePreferences", model);
        }

        [Authorize]
        [HttpPost]
        public ActionResult Preferences(UserPreferencesModel model)
        {
            var user = this.ActiveUser;
            if (ModelState.IsValid)
            {
                var ns = "mvcForum.Web.Profile.Update";

                user.Culture = model.Culture;
                user.Timezone = model.Timezone;
                
                this.ForumUserRepository.Update(user);
                this.Context.SaveChanges();

                // The profile was updated, let's tell the user!
                TempData.Add("Status", ForumHelper.GetString("ChangesSaved", null, ns));
            }
            model.Path = new Dictionary<string, string>();
            model.User = new UserViewModel(user);
            return View("UpdatePreferences", model);
        }
    }
}