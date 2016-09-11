using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Autofac;
using Autofac.Integration.Mvc;
using Microsoft.ApplicationInsights.Extensibility;
using MTDB.Controllers;
using MTDB.Core.Caching;
using MTDB.Data;
using MTDB.Core.Services.Catalog;
using MTDB.Core.Services.Common;
using MTDB.Core.Services.Lineups;
using MTDB.Core.Services.Packs;

namespace MTDB
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<AccountController>().InstancePerRequest();
            builder.RegisterType<CollectionController>().InstancePerRequest();
            builder.RegisterType<CommentsController>().InstancePerRequest();
            builder.RegisterType<LineupController>().InstancePerRequest();
            builder.RegisterType<ManageController>().InstancePerRequest();
            builder.RegisterType<MiscController>().InstancePerRequest();
            builder.RegisterType<PackController>().InstancePerRequest();
            builder.RegisterType<PlayerController>().InstancePerRequest();
            builder.RegisterType<PlayerUpdateController>().InstancePerRequest();
            //builder.RegisterControllers(typeof(MvcApplication).Assembly);

            builder.Register<IDbContext>(c => new MtdbContext()).InstancePerLifetimeScope();
            
            builder.RegisterType<RedisCacheManager>().InstancePerLifetimeScope();
            builder.RegisterType<MemoryCacheManager>().SingleInstance();
            builder.RegisterType<PerRequestCacheManager>().InstancePerLifetimeScope();

            builder.RegisterType<CollectionService>().InstancePerRequest();
            builder.RegisterType<CommentService>().InstancePerRequest();
            builder.RegisterType<DivisionService>().InstancePerRequest();
            builder.RegisterType<LineupService>().InstancePerRequest();
            builder.RegisterType<PackService>().InstancePerRequest();
            builder.RegisterType<PlayerService>().InstancePerRequest();
            builder.RegisterType<PlayerUpdateService>().InstancePerRequest();
            builder.RegisterType<ProfileService>().InstancePerRequest();
            builder.RegisterType<StatService>().InstancePerRequest();
            builder.RegisterType<TeamService>().InstancePerRequest();
            builder.RegisterType<ThemeService>().InstancePerRequest();
            builder.RegisterType<TierService>().InstancePerRequest();

            var container = builder.Build();
            DependencyResolver.SetResolver(new AutofacDependencyResolver(container));

            RouteConfig.RegisterRoutes(RouteTable.Routes);

            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            MvcHandler.DisableMvcResponseHeader = true;

            #if DEBUG
            TelemetryConfiguration.Active.DisableTelemetry = true;
            #endif
        }
    }
}
