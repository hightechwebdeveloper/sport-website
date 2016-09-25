using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Web.Mvc;
using ApplicationBoilerplate.DataProvider;
using mvcForum.Core;
using mvcForum.Core.Abstractions.Interfaces;
using mvcForum.Core.Interfaces.Services;
using mvcForum.Core.Specifications;
using mvcForum.Web.Controllers;
using mvcForum.Web.Helpers;
using mvcForum.Web.Interfaces;
using mvcForum.Web.ViewModels;
using mvcForum.Web.ViewModels.Delete;
using mvcForum.Web.ViewModels.Update;
using MTDB.Areas.Forum.Models;

namespace MTDB.Forums.Areas.Forums.Controllers
{
    public class ProfileController : ThemedForumBaseController
    {
        private readonly IConfiguration _config;
        private readonly IMembershipService _membershipService;

        public ProfileController(IWebUserProvider userProvider, IContext context, IConfiguration config, IMembershipService membershipService)
          : base(userProvider, context)
        {
            this._config = config;
            this._membershipService = membershipService;
        }

        public ActionResult Index(int id, string name)
        {
            try
            {
                var userViewModel = new UserViewModel(this.GetRepository<ForumUser>().Read(id));
                userViewModel.Path = new Dictionary<string, string>();
                return this.View(userViewModel);
            }
            catch
            {
            }
            return this.View();
        }

        [Authorize]
        public ActionResult Update()
        {
            return this.View(new UpdateUserViewModel(this.ActiveUser, this.ActiveUser.ExternalAccount, this._config.AllowUserDefinedTheme, this.Server.MapPath("~/themes")));
        }

        [HttpPost]
        [Authorize]
        public ActionResult Update(UpdateUserViewModel model)
        {
            if (this.ModelState.IsValid)
            {
                var namespc = "mvcForum.Web.Profile.Update";
                if (model.Id == this.ActiveUser.Id)
                {
                    var flag1 = false;
                    if (this.ActiveUser.ExternalAccount)
                    {
                        if (this.ActiveUser.EmailAddress.ToLowerInvariant().EndsWith("repl@ce.this"))
                        {
                            if (string.IsNullOrWhiteSpace(model.Name))
                            {
                                this.ModelState.AddModelError("Name", ForumHelper.GetString("MissingUsernameExternalUser", null, namespc));
                                flag1 = true;
                            }
                            else if (this.ForumUserRepository.ReadMany(new ForumUserSpecifications.SpecificUsername(model.Name)).Any(fu => fu.Id != this.ActiveUser.Id))
                            {
                                this.ModelState.AddModelError("Name", ForumHelper.GetString("NameInUseExternalUser", null, namespc));
                                flag1 = true;
                            }
                        }
                        if (string.IsNullOrWhiteSpace(model.Email))
                        {
                            this.ModelState.AddModelError("Email", ForumHelper.GetString("MissingEmailExternalUser", null, namespc));
                            flag1 = true;
                        }
                        else
                        {
                            try
                            {
                                var mailAddress = new MailAddress(model.Email);
                                if (!string.IsNullOrWhiteSpace(this._membershipService.GetAccountNameByEmailAddress(model.Email)))
                                {
                                    this.ModelState.AddModelError("Email", ForumHelper.GetString("EmailInUse", null, namespc));
                                    flag1 = true;
                                }
                            }
                            catch
                            {
                                this.ModelState.AddModelError("Email", ForumHelper.GetString("InvalidEmailExternalUser", null, namespc));
                                flag1 = true;
                            }
                        }
                        if (!flag1)
                        {
                            var accountByName = this._membershipService.GetAccountByName(this.ControllerContext.RequestContext.HttpContext.User.Identity.Name, false);
                            var entity = this.ForumUserRepository.ReadOne(new ForumUserSpecifications.SpecificProviderUserKey(accountByName.ProviderUserKey.ToString()));
                            if (model.Email != accountByName.EmailAddress)
                            {
                                accountByName.EmailAddress = model.Email.ToLowerInvariant();
                                entity.EmailAddress = accountByName.EmailAddress;
                                this._membershipService.UpdateAccount(accountByName);
                            }
                            entity.Name = model.Name;
                            entity.FullName = model.FullName;
                            entity.UseFullName = !string.IsNullOrWhiteSpace(model.FullName);
                            entity.Culture = model.Culture;
                            entity.Timezone = model.Timezone;
                            if (this._config.AllowUserDefinedTheme)
                                entity.Theme = model.Theme;
                            this.ForumUserRepository.Update(entity);
                            this.Context.SaveChanges();
                            this.TempData.Add("Status", ForumHelper.GetString("ChangesSaved", null, namespc));
                        }
                    }
                    else
                    {
                        var flag2 = false;
                        if (!string.IsNullOrEmpty(model.OldPassword))
                            flag2 = this._membershipService.ValidateAccount(this.ControllerContext.RequestContext.HttpContext.User.Identity.Name, model.OldPassword);
                        var accountByName = this._membershipService.GetAccountByName(this.ControllerContext.RequestContext.HttpContext.User.Identity.Name, false);
                        if (this.ActiveUser.EmailAddress != model.Email && !string.IsNullOrWhiteSpace(this._membershipService.GetAccountNameByEmailAddress(model.Email)))
                        {
                            this.ModelState.AddModelError("Email", ForumHelper.GetString("EmailInUse", null, namespc));
                            flag1 = true;
                        }
                        if (this.ActiveUser.EmailAddress != model.Email)
                        {
                            if (!flag2)
                            {
                                this.ModelState.AddModelError("Email", ForumHelper.GetString("EmailChangeMissingPassword", null, namespc));
                                flag1 = true;
                            }
                            else
                                accountByName.EmailAddress = model.Email;
                        }
                        if (!string.IsNullOrWhiteSpace(model.NewPassword) && model.NewPassword != model.RepeatedNewPassword)
                        {
                            this.ModelState.AddModelError("NewPassword", ForumHelper.GetString("PasswordsDoesNotMatch", null, namespc));
                            flag1 = true;
                        }
                        if (!string.IsNullOrWhiteSpace(model.NewPassword) && model.NewPassword == model.RepeatedNewPassword && !flag2)
                        {
                            this.ModelState.AddModelError("Password", ForumHelper.GetString("OldPasswordsRequired", null, namespc));
                            flag1 = true;
                        }
                        if (!flag1)
                        {
                            try
                            {
                                if (!string.IsNullOrWhiteSpace(model.OldPassword) && !string.IsNullOrWhiteSpace(model.NewPassword))
                                    accountByName.ChangePassword(model.OldPassword, model.NewPassword);
                                this._membershipService.UpdateAccount(accountByName);
                                var forumUser = this.ForumUserRepository.ReadOne(new ForumUserSpecifications.SpecificProviderUserKey(accountByName.ProviderUserKey.ToString()));
                                if (forumUser != null)
                                {
                                    forumUser.Culture = model.Culture;
                                    forumUser.Timezone = model.Timezone;
                                    forumUser.FullName = model.FullName;
                                    forumUser.UseFullName = !string.IsNullOrWhiteSpace(forumUser.FullName);
                                    forumUser.EmailAddress = model.Email;
                                    if (this._config.AllowUserDefinedTheme)
                                        forumUser.Theme = model.Theme;
                                }
                                this.Context.SaveChanges();
                                this.TempData.Add("Status", ForumHelper.GetString("ChangesSaved", null, namespc));
                            }
                            catch (FormatException ex)
                            {
                                this.ModelState.AddModelError("", ForumHelper.GetString("EditProfile.InvalidEmailAddress"));
                            }
                        }
                    }
                }
            }
            model = new UpdateUserViewModel(this.ActiveUser, this.ActiveUser.ExternalAccount, this._config.AllowUserDefinedTheme, this.Server.MapPath("~/themes"));
            return this.View(model);
        }

        [Authorize]
        public ActionResult Delete(int id)
        {
            return this.View(new DeleteUserViewModel());
        }

        [Authorize]
        [HttpPost]
        public ActionResult Delete(DeleteUserViewModel model)
        {
            if (!model.Confirm)
                return this.View(model);
            this._membershipService.DeleteAccount(this.ActiveUser.Name, true);
            return this.Redirect("/");
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