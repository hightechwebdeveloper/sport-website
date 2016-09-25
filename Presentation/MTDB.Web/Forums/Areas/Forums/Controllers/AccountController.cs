using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using ApplicationBoilerplate.DataProvider;
using CreativeMinds.Security.Cryptography;
using mvcForum.Core;
using mvcForum.Core.Abstractions.Interfaces;
using mvcForum.Core.Interfaces.Services;
using mvcForum.Core.Specifications;
using mvcForum.Web;
using mvcForum.Web.Controllers;
using mvcForum.Web.Filters;
using mvcForum.Web.Helpers;
using mvcForum.Web.Interfaces;
using mvcForum.Web.ViewModels;
using MVCBootstrap.Web.Mvc.Interfaces;

namespace MTDB.Forums.Areas.Forums.Controllers
{
    [ConfigurationControlled(ConfigurationArea.AccountController, "")]
    public class AccountController : ThemedForumBaseController
    {
        private readonly IAuthenticationService _authService;
        private readonly mvcForum.Core.Interfaces.Services.IMembershipService _membershipService;
        private readonly IRepository<BannedIP> _bipRepo;
        private readonly IConfiguration _config;
        private readonly IMailService _mailService;

        public AccountController(IContext context, IWebUserProvider userProvider, IAuthenticationService authService, mvcForum.Core.Interfaces.Services.IMembershipService membershipService, IRepository<BannedIP> bipRepo, IConfiguration config, IMailService mailService)
          : base(userProvider, context)
        {
            this._authService = authService;
            this._membershipService = membershipService;
            this._config = config;
            this._bipRepo = bipRepo;
            this._mailService = mailService;
        }

        public ActionResult LogOff()
        {
            this._authService.SignOut();
            return (ActionResult)this.RedirectToAction("index", "home");
        }

        [HttpGet]
        [ConfigurationControlled(ConfigurationArea.AccountController, "SignUp")]
        public ActionResult Register()
        {
            if (!this._config.UseForumAccountController || !this._config.AllowLocalUsers || !this._config.AllowSignUp)
                return (ActionResult)new HttpNotFoundResult();
            return (ActionResult)this.View((object)new RegisterModel()
            {
                RequireRulesAccept = this._config.RequireRulesAccept,
                SignUpRules = this._config.SignUpRules
            });
        }

        [ConfigurationControlled(ConfigurationArea.AccountController, "SignUp")]
        [HttpPost]
        public ActionResult Register(RegisterModel model)
        {
            if (!this._config.UseForumAccountController || !this._config.AllowLocalUsers || !this._config.AllowSignUp)
                return (ActionResult)new HttpNotFoundResult();
            if (this.ModelState.IsValid)
            {
                if (this._config.RequireRulesAccept && model.RulesAccepted || !this._config.RequireRulesAccept)
                {
                    string errorMessage;
                    if (this._membershipService.CreateAccount(model.Username, model.Password, model.EmailAddress, out errorMessage))
                    {
                        if (this._config.RequireEmailValidation)
                        {
                            IAccount account = this._membershipService.GetAccount((object)model.Username);
                            string data = string.Format("{1}#|#{0}#|#{2}", (object)account.AccountName, (object)account.EmailAddress, account.ProviderUserKey);
                            PrivatePrivateCrypto privatePrivateCrypto = new PrivatePrivateCrypto();
                            privatePrivateCrypto.Phrase = this._config.DefaultTimezone;
                            string siteUrl = this._config.SiteURL;
                            if (!siteUrl.EndsWith("/"))
                                siteUrl += "/";
                            string newValue = string.Format("{0}forums/account/activate?code={1}", (object)siteUrl, (object)HttpUtility.UrlEncode(privatePrivateCrypto.Encrypt(data)));
                            this._mailService.Send(new MailAddress(this._config.RobotEmailAddress, this._config.RobotName), (IList<MailAddress>)((IEnumerable<MailAddress>)new MailAddress[1]
                            {
                new MailAddress(model.EmailAddress, model.Username)
                            }).ToList<MailAddress>(), this._config.ValidationSubject, this._config.ValidationBody.Replace("{Email}", model.EmailAddress).Replace("{Password}", model.Password).Replace("{Url}", newValue));
                            this.TempData.Add("Status", (object)ForumHelper.GetString<ForumConfigurator>("Register.EmailActivation"));
                        }
                        else
                        {
                            this.Context.GetRepository<ForumUser>().ReadOne((ISpecification<ForumUser>)new ForumUserSpecifications.SpecificEmailAddress(model.EmailAddress)).Active = true;
                            this.Context.SaveChanges();
                            this.TempData.Add("Status", (object)ForumHelper.GetString<ForumConfigurator>("Register.AccountReady"));
                        }
                        return (ActionResult)this.RedirectToAction("success", "account", (object)new { area = "forum" });
                    }
                    this.ModelState.AddModelError("", errorMessage);
                }
                else
                    this.ModelState.AddModelError("RulesAccepted", ForumHelper.GetString<ForumConfigurator>("Register.RulesMustBeAccepted"));
            }
            model.RequireRulesAccept = this._config.RequireRulesAccept;
            model.SignUpRules = this._config.SignUpRules;
            return (ActionResult)this.View((object)model);
        }

        [ConfigurationControlled(ConfigurationArea.AccountController, "LocalOrOpenAuth")]
        [HttpGet]
        public ActionResult LogOn()
        {
            if (!this._config.UseForumAccountController || !this._config.AllowLocalUsers && !this._config.AllowOpenAuthUsers)
                return (ActionResult)new HttpNotFoundResult();
            if (this._bipRepo.ReadOne((ISpecification<BannedIP>)new BannedIPSpecifications.SpecificIP(this.Request.UserHostAddress)) != null)
            {
                this.TempData.Add("Reason", (object)ForumHelper.GetString<ForumConfigurator>("NoAccess.BannedIP", (object)new
                {
                    IPAddress = this.Request.UserHostAddress
                }));
                return (ActionResult)this.RedirectToRoute("NoAccess");
            }
            return (ActionResult)this.View((object)new LogOnModel()
            {
                AllowLocalUsers = this._config.AllowLocalUsers,
                AllowSignUp = this._config.AllowSignUp,
                AllowOpenAuthUsers = this._config.AllowOpenAuthUsers
            });
        }

        [ConfigurationControlled(ConfigurationArea.AccountController, "Local")]
        [HttpPost]
        public ActionResult LogOn(LogOnModel model, string returnUrl)
        {
            if (!this._config.UseForumAccountController || !this._config.AllowLocalUsers)
                return (ActionResult)new HttpNotFoundResult();
            if (this.ModelState.IsValid)
            {
                if (this._bipRepo.ReadOne((ISpecification<BannedIP>)new BannedIPSpecifications.SpecificIP(this.Request.UserHostAddress)) != null)
                {
                    this.TempData.Add("Reason", (object)ForumHelper.GetString<ForumConfigurator>("NoAccess.BannedIP"));
                    return (ActionResult)this.RedirectToRoute("NoAccess");
                }
                string nameByEmailAddress = this._membershipService.GetAccountNameByEmailAddress(model.EmailAddress);
                if (!string.IsNullOrWhiteSpace(nameByEmailAddress) && this._membershipService.ValidateAccount(nameByEmailAddress, model.Password))
                {
                    ForumUser forumUser = this.ForumUserRepository.ReadOne((ISpecification<ForumUser>)new ForumUserSpecifications.SpecificUsername(nameByEmailAddress));
                    if (forumUser != null)
                    {
                        if (!forumUser.ExternalAccount)
                        {
                            this._authService.SignIn(this._membershipService.GetAccountByName(nameByEmailAddress), model.RememberMe);
                            if (this.Url.IsLocalUrl(returnUrl))
                                return (ActionResult)this.Redirect(returnUrl);
                            return (ActionResult)this.RedirectToAction("Index", "Home", (object)new { area = "forum" });
                        }
                        this.ModelState.AddModelError(string.Empty, "The account is an external account, please log on using the right provider.");
                    }
                }
                else
                    this.ModelState.AddModelError(string.Empty, "The user name or password provided is incorrect.");
            }
            model.AllowLocalUsers = this._config.AllowLocalUsers;
            model.AllowSignUp = this._config.AllowSignUp;
            model.AllowOpenAuthUsers = this._config.AllowOpenAuthUsers;
            return (ActionResult)this.View((object)model);
        }

        [HttpPost]
        [ConfigurationControlled(ConfigurationArea.AccountController, "Local")]
        public ActionResult ForgottenPassword(ForgottenPassword model)
        {
            if (this.ModelState.IsValid)
            {
                IAccount accountByEmailAddress = this._membershipService.GetAccountByEmailAddress(model.EmailAddress);
                string newValue = accountByEmailAddress.ResetPassword();
                this._membershipService.UpdateAccount(accountByEmailAddress);
                this._mailService.Send(new MailAddress(this._config.RobotEmailAddress, this._config.RobotName), (IList<MailAddress>)((IEnumerable<MailAddress>)new MailAddress[1]
                {
          new MailAddress(accountByEmailAddress.EmailAddress, accountByEmailAddress.AccountName)
                }).ToList<MailAddress>(), this._config.ForgottenPasswordSubject, this._config.ForgottenPasswordBody.Replace("{Email}", model.EmailAddress).Replace("{Password}", newValue));
                this.TempData.Add("ForgottenStatus", (object)ForumHelper.GetString("PasswordChanged", (object)null, "mvcForum.Web.ForgottenPassword"));
            }
            return (ActionResult)this.RedirectToAction("logon");
        }

        public ActionResult External()
        {
            return (ActionResult)this.View();
        }

        public ActionResult Success()
        {
            return (ActionResult)this.View();
        }

        [ConfigurationControlled(ConfigurationArea.AccountController, "SignUp")]
        public ActionResult Activate(string code)
        {
            bool flag = false;
            if (!string.IsNullOrWhiteSpace(code))
            {
                PrivatePrivateCrypto privatePrivateCrypto = new PrivatePrivateCrypto();
                privatePrivateCrypto.Phrase = this._config.DefaultTimezone;
                try
                {
                    string[] strArray = privatePrivateCrypto.Decrypt(code).Split(new string[1] { "#|#" }, StringSplitOptions.RemoveEmptyEntries);
                    if (strArray.Length == 3)
                    {
                        IAccount account = this._membershipService.GetAccount((object)strArray[1]);
                        if (account != null)
                        {
                            if (account.EmailAddress == strArray[0])
                            {
                                if (account.ProviderUserKey.ToString() == strArray[2])
                                {
                                    account.IsApproved = true;
                                    this._membershipService.UpdateAccount(account);
                                    flag = true;
                                    this.ForumUserRepository.ReadOne((ISpecification<ForumUser>)new ForumUserSpecifications.SpecificProviderUserKey(account.ProviderUserKey.ToString())).Active = true;
                                    this.Context.SaveChanges();
                                }
                            }
                        }
                    }
                }
                catch
                {
                }
            }
            return (ActionResult)this.View((object)flag);
        }
    }
}
