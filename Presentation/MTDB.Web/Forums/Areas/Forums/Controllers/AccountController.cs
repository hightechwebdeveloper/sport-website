using System;
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
            return this.RedirectToAction("index", "home");
        }

        [HttpGet]
        [ConfigurationControlled(ConfigurationArea.AccountController, "SignUp")]
        public ActionResult Register()
        {
            if (!this._config.UseForumAccountController || !this._config.AllowLocalUsers || !this._config.AllowSignUp)
                return new HttpNotFoundResult();
            return this.View(new RegisterModel
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
                return new HttpNotFoundResult();
            if (this.ModelState.IsValid)
            {
                if (this._config.RequireRulesAccept && model.RulesAccepted || !this._config.RequireRulesAccept)
                {
                    string errorMessage;
                    if (this._membershipService.CreateAccount(model.Username, model.Password, model.EmailAddress, out errorMessage))
                    {
                        if (this._config.RequireEmailValidation)
                        {
                            var account = this._membershipService.GetAccount(model.Username);
                            var data = string.Format("{1}#|#{0}#|#{2}", account.AccountName, account.EmailAddress, account.ProviderUserKey);
                            var privatePrivateCrypto = new PrivatePrivateCrypto();
                            privatePrivateCrypto.Phrase = this._config.DefaultTimezone;
                            var siteUrl = this._config.SiteURL;
                            if (!siteUrl.EndsWith("/"))
                                siteUrl += "/";
                            var newValue =
                                $"{siteUrl}forums/account/activate?code={HttpUtility.UrlEncode(privatePrivateCrypto.Encrypt(data))}";
                            this._mailService.Send(new MailAddress(this._config.RobotEmailAddress, this._config.RobotName), new MailAddress[1]
                            {
                                new MailAddress(model.EmailAddress, model.Username)
                            }.ToList(), this._config.ValidationSubject, this._config.ValidationBody.Replace("{Email}", model.EmailAddress).Replace("{Password}", model.Password).Replace("{Url}", newValue));
                            this.TempData.Add("Status", ForumHelper.GetString<ForumConfigurator>("Register.EmailActivation"));
                        }
                        else
                        {
                            this.Context.GetRepository<ForumUser>().ReadOne(new ForumUserSpecifications.SpecificEmailAddress(model.EmailAddress)).Active = true;
                            this.Context.SaveChanges();
                            this.TempData.Add("Status", ForumHelper.GetString<ForumConfigurator>("Register.AccountReady"));
                        }
                        return this.RedirectToAction("success", "account", new { area = "forum" });
                    }
                    this.ModelState.AddModelError("", errorMessage);
                }
                else
                    this.ModelState.AddModelError("RulesAccepted", ForumHelper.GetString<ForumConfigurator>("Register.RulesMustBeAccepted"));
            }
            model.RequireRulesAccept = this._config.RequireRulesAccept;
            model.SignUpRules = this._config.SignUpRules;
            return this.View(model);
        }

        [ConfigurationControlled(ConfigurationArea.AccountController, "LocalOrOpenAuth")]
        [HttpGet]
        public ActionResult LogOn()
        {
            if (!this._config.UseForumAccountController || !this._config.AllowLocalUsers && !this._config.AllowOpenAuthUsers)
                return new HttpNotFoundResult();
            if (this._bipRepo.ReadOne(new BannedIPSpecifications.SpecificIP(this.Request.UserHostAddress)) != null)
            {
                this.TempData.Add("Reason", ForumHelper.GetString<ForumConfigurator>("NoAccess.BannedIP", new
                {
                    IPAddress = this.Request.UserHostAddress
                }));
                return this.RedirectToRoute("NoAccess");
            }
            return this.View(new LogOnModel
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
                return new HttpNotFoundResult();
            if (this.ModelState.IsValid)
            {
                if (this._bipRepo.ReadOne(new BannedIPSpecifications.SpecificIP(this.Request.UserHostAddress)) != null)
                {
                    this.TempData.Add("Reason", ForumHelper.GetString<ForumConfigurator>("NoAccess.BannedIP"));
                    return this.RedirectToRoute("NoAccess");
                }
                var nameByEmailAddress = this._membershipService.GetAccountNameByEmailAddress(model.EmailAddress);
                if (!string.IsNullOrWhiteSpace(nameByEmailAddress) && this._membershipService.ValidateAccount(nameByEmailAddress, model.Password))
                {
                    var forumUser = this.ForumUserRepository.ReadOne(new ForumUserSpecifications.SpecificUsername(nameByEmailAddress));
                    if (forumUser != null)
                    {
                        if (!forumUser.ExternalAccount)
                        {
                            this._authService.SignIn(this._membershipService.GetAccountByName(nameByEmailAddress), model.RememberMe);
                            if (this.Url.IsLocalUrl(returnUrl))
                                return this.Redirect(returnUrl);
                            return this.RedirectToAction("Index", "Home", new { area = "forum" });
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
            return this.View(model);
        }

        [HttpPost]
        [ConfigurationControlled(ConfigurationArea.AccountController, "Local")]
        public ActionResult ForgottenPassword(ForgottenPassword model)
        {
            if (this.ModelState.IsValid)
            {
                var accountByEmailAddress = this._membershipService.GetAccountByEmailAddress(model.EmailAddress);
                var newValue = accountByEmailAddress.ResetPassword();
                this._membershipService.UpdateAccount(accountByEmailAddress);
                this._mailService.Send(new MailAddress(this._config.RobotEmailAddress, this._config.RobotName), new MailAddress[1]
                {
                    new MailAddress(accountByEmailAddress.EmailAddress, accountByEmailAddress.AccountName)
                }.ToList(), this._config.ForgottenPasswordSubject, this._config.ForgottenPasswordBody.Replace("{Email}", model.EmailAddress).Replace("{Password}", newValue));
                this.TempData.Add("ForgottenStatus", ForumHelper.GetString("PasswordChanged", null, "mvcForum.Web.ForgottenPassword"));
            }
            return this.RedirectToAction("logon");
        }

        public ActionResult External()
        {
            return this.View();
        }

        public ActionResult Success()
        {
            return this.View();
        }

        [ConfigurationControlled(ConfigurationArea.AccountController, "SignUp")]
        public ActionResult Activate(string code)
        {
            var flag = false;
            if (!string.IsNullOrWhiteSpace(code))
            {
                var privatePrivateCrypto = new PrivatePrivateCrypto();
                privatePrivateCrypto.Phrase = this._config.DefaultTimezone;
                try
                {
                    var strArray = privatePrivateCrypto.Decrypt(code).Split(new string[1] { "#|#" }, StringSplitOptions.RemoveEmptyEntries);
                    if (strArray.Length == 3)
                    {
                        var account = this._membershipService.GetAccount(strArray[1]);
                        if (account != null)
                        {
                            if (account.EmailAddress == strArray[0])
                            {
                                if (account.ProviderUserKey.ToString() == strArray[2])
                                {
                                    account.IsApproved = true;
                                    this._membershipService.UpdateAccount(account);
                                    flag = true;
                                    this.ForumUserRepository.ReadOne(new ForumUserSpecifications.SpecificProviderUserKey(account.ProviderUserKey.ToString())).Active = true;
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
            return this.View(flag);
        }
    }
}
