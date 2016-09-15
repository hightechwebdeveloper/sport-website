using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Reflection;
using System.Web.Mvc;
using Autofac;
using Autofac.Core;
using Autofac.Integration.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.DataProtection;
using MTDB.Core;
using MTDB.Core.Caching;
using MTDB.Core.Services.Catalog;
using MTDB.Core.Services.Common;
using MTDB.Core.Services.Lineups;
using MTDB.Core.Services.Packs;
using MTDB.Data;
using MTDB.Core.Domain;
using MTDB.Web.Framework;
using Owin;

namespace MTDB
{
    //todo move to MTDB.Web.Framework
    public class DependencyRegistrar
    {
        public static void ConfigureDependencyInjection(IAppBuilder app)
        {
            var builder = new ContainerBuilder();
            var executingAssembly = Assembly.GetExecutingAssembly();
            //builder.RegisterApiControllers(executingAssembly);
            builder.RegisterControllers(executingAssembly);

            RegisterComponents(builder, app);

            var container = builder.Build();

            app.UseAutofacMiddleware(container);

            //var apiResolver = new AutofacWebApiDependencyResolver(container);
            //apiConfig.DependencyResolver = apiResolver;
            //app.UseAutofacWebApi(apiConfig);
            
            DependencyResolver.SetResolver(new AutofacDependencyResolver(container));
            app.UseAutofacMvc();
        }

        private static void RegisterComponents(ContainerBuilder builder, IAppBuilder app)
        {
            builder.RegisterType<WebLocationContext>().As<ILocationContext>().InstancePerLifetimeScope();

            builder.Register(c => 
                c.Resolve<ILocationContext>().CurrentLocation == Location.K17 ?
                new K17DbContext() as IDbContext :
                new K16DbContext() as IDbContext)
               .As<IDbContext>()
               .InstancePerLifetimeScope();

            builder.RegisterType<K17DbContext>().As<DbContext>().InstancePerRequest(); //SignInManager
            builder.RegisterType<ApplicationSignInManager>().As<SignInManager<User, string>>().InstancePerRequest();
            builder.RegisterType<UserStore<User>>().As<IUserStore<User>>().InstancePerRequest();
            builder.Register<IAuthenticationManager>((c, p) => c.Resolve<IOwinContext>().Authentication).InstancePerRequest();
            builder.Register((c, p) => BuildUserManager(c, p, app.GetDataProtectionProvider()));

            builder.RegisterType<WebWorkContext>().As<IWorkContext>().InstancePerLifetimeScope();

            //builder.RegisterType<AccountController>().InstancePerRequest();
            //builder.RegisterType<CollectionController>().InstancePerRequest();
            //builder.RegisterType<CommentsController>().InstancePerRequest();
            //builder.RegisterType<LineupController>().InstancePerRequest();
            //builder.RegisterType<ManageController>().InstancePerRequest();
            //builder.RegisterType<MiscController>().InstancePerRequest();
            //builder.RegisterType<PackController>().InstancePerRequest();
            //builder.RegisterType<PlayerController>().InstancePerRequest();
            //builder.RegisterType<PlayerUpdateController>().InstancePerRequest();
            //builder.RegisterControllers(typeof(MvcApplication).Assembly);

            builder.RegisterType<PerRequestCacheManager>().InstancePerLifetimeScope();
            builder.Register(c =>
            {
                var predicat = c.Resolve<ILocationContext>().CurrentLocation == Location.K17
                    ? "2k17"
                    : string.Empty;
                var perRequestCache = c.Resolve<PerRequestCacheManager>(new NamedParameter("predicat", predicat));
                return new RedisCacheManager(perRequestCache, predicat);
            })
            .InstancePerLifetimeScope();
            //builder.RegisterType<MemoryCacheManager>().SingleInstance();
            builder.Register(c =>
            {
                var predicat = c.Resolve<ILocationContext>().CurrentLocation == Location.K17
                    ? "2k17"
                    : string.Empty;
                return new MemoryCacheManager(predicat);
            })
            .InstancePerLifetimeScope();

            builder.Register(c => c.Resolve<ILocationContext>().CurrentLocation == Location.K17
                ? new CdnSettings("push-20.cdn77.com", "user_mgoh0250", "lF9SKUp2d0M332IbHdeF", "/nba2k17/")
                : new CdnSettings("push-20.cdn77.com", "user_mgoh0250", "lF9SKUp2d0M332IbHdeF"))
            .InstancePerLifetimeScope();

            builder.RegisterType<CollectionService>().InstancePerRequest();
            builder.RegisterType<PlayerService>().InstancePerRequest();
            builder.RegisterType<StatService>().InstancePerRequest();
            builder.RegisterType<TeamService>().InstancePerRequest();
            builder.RegisterType<ThemeService>().InstancePerRequest();
            builder.RegisterType<TierService>().InstancePerRequest();

            builder.RegisterType<CommentService>().InstancePerRequest();
            builder.RegisterType<DivisionService>().InstancePerRequest();
            builder.RegisterType<LineupService>().InstancePerRequest();
            builder.RegisterType<PackService>().InstancePerRequest();
            builder.RegisterType<PlayerUpdateService>().InstancePerRequest();
            builder.RegisterType<ProfileService>().InstancePerRequest();
        }

        private static UserManager<User> BuildUserManager(IComponentContext context, IEnumerable<Parameter> parameters, IDataProtectionProvider dataProtectionProvider)
        {
            var manager = new UserManager(context.Resolve<IUserStore<User>>());

            manager.UserValidator = new UserValidator<User>(manager)
            {
                AllowOnlyAlphanumericUserNames = true,
                RequireUniqueEmail = true
            };

            manager.PasswordValidator = new PasswordValidator
            {
                RequiredLength = 6,
                RequireNonLetterOrDigit = false,
                RequireDigit = true,
                RequireLowercase = false,
                RequireUppercase = false,
            };

            manager.UserLockoutEnabledByDefault = true;
            manager.DefaultAccountLockoutTimeSpan = TimeSpan.FromMinutes(5);
            manager.MaxFailedAccessAttemptsBeforeLockout = 10;
            manager.EmailService = new SendGridMessageService();
           
            if (dataProtectionProvider != null)
                manager.UserTokenProvider = new DataProtectorTokenProvider<User>(dataProtectionProvider.Create("ASP.NET Identity"));

            return manager;
        }
    }
}