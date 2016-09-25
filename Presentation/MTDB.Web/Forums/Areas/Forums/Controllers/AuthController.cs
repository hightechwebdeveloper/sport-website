using System;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using ApplicationBoilerplate.DataProvider;
using mvcForum.Core;
using mvcForum.Core.Specifications;
using MVCBootstrap.Web.Mvc.Interfaces;
using SimpleAuthentication.Mvc;

namespace MTDB.Forums.Areas.Forums.Controllers
{
    public class AuthController : IAuthenticationCallbackProvider
    {
        private readonly mvcForum.Core.Interfaces.Services.IMembershipService _memberService;
        private readonly IFormsAuthenticationService _forms;
        private readonly IContext _context;

        public AuthController(mvcForum.Core.Interfaces.Services.IMembershipService memberService, IContext context, IFormsAuthenticationService forms)
        {
            this._memberService = memberService;
            this._context = context;
            this._forms = forms;
        }

        public ActionResult OnRedirectToAuthenticationProviderError(HttpContextBase context, string errorMessage)
        {
            var viewResult = new ViewResult();
            viewResult.ViewName = "AuthenticateCallback";
            viewResult.ViewData = new ViewDataDictionary(new
            {
                ErrorMessage = errorMessage
            });
            return viewResult;
        }

        public ActionResult Process(HttpContextBase context, AuthenticateCallbackData model)
        {
            var str = $"[{model.AuthenticatedClient.ProviderName}][{model.AuthenticatedClient.UserInformation.Id}]";
            var account1 = this._memberService.GetAccount(str);
            if (account1 == null)
            {
                var mailAddress = new MailAddress($"{DateTime.UtcNow:ddMMyyyy-hhmmss}repl@ce.this");
                try
                {
                    mailAddress = new MailAddress(model.AuthenticatedClient.UserInformation.Email);
                }
                catch
                {
                }
                string errorMessage;
                if (!this._memberService.CreateAccount(str, new Guid().ToString(), mailAddress.Address, out errorMessage))
                    return new HttpStatusCodeResult(500);
                var account2 = this._memberService.GetAccount(str);
                if (account2 == null)
                    return new HttpStatusCodeResult(500);
                var repository = this._context.GetRepository<ForumUser>();
                var entity = repository.ReadOne(new ForumUserSpecifications.SpecificProviderUserKey(account2.ProviderUserKey.ToString()));
                if (entity != null)
                {
                    entity.Name = model.AuthenticatedClient.UserInformation.UserName;
                    entity.EmailAddress = account2.EmailAddress.EndsWith("repl@ce.this") ? account2.EmailAddress : account2.EmailAddress + "repl@ce.this";
                    entity.FullName = model.AuthenticatedClient.UserInformation.Name;
                    entity.Active = true;
                    entity.ExternalAccount = true;
                    entity.ExternalProvider = model.AuthenticatedClient.ProviderName;
                    entity.ExternalProviderId = model.AuthenticatedClient.UserInformation.Id;
                    repository.Update(entity);
                    this._context.SaveChanges();
                }
            }
            else
            {
                var forumUser = this._context.GetRepository<ForumUser>().ReadOne(new ForumUserSpecifications.SpecificProviderUserKey(account1.ProviderUserKey.ToString()));
                if (forumUser == null)
                    return new HttpStatusCodeResult(500);
                if (!forumUser.ExternalAccount || forumUser.ExternalAccount && forumUser.ExternalProvider != model.AuthenticatedClient.ProviderName)
                    return new RedirectResult("/forums/account/external");
            }
            this._forms.SignIn(str, false);
            return new RedirectResult("/forums");
        }
    }
}
