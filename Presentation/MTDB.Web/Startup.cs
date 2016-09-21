using System.Linq;
using Microsoft.AspNet.Identity;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;

[assembly: OwinStartupAttribute(typeof(MTDB.Startup))]
namespace MTDB
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            DependencyRegistrar.ConfigureDependencyInjection(app);
            
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/account/login"),
                CookieName = "authentication",
            });

            //redirect www to non-www
            app.Use(async (context, next) =>
            {
                if (context.Request.Uri.Host.StartsWith("www."))
                {
                    var host = context.Request.Uri.Host;

                    context.Response.StatusCode = 301;
                    context.Response.Headers.Set("Location", context.Request.Uri.AbsoluteUri.Replace(host, host.Substring(4)));
                    return;
                }
                await next();
            });

            //redirect / to /17
            app.Use(async (context, next) =>
            {
                var path = context.Request.Uri.AbsolutePath.TrimEnd('/');
                if (path == "")
                {
                    context.Response.StatusCode = 302;
                    context.Response.Headers.Set("Location", $"{context.Request.Uri.AbsoluteUri.TrimEnd('/')}/17");
                    return;
                }
                string[] redirects = { "/collections", "/lineups", "/packs", "/playerupdates", "/players" };
                if (redirects.Any(r => path.StartsWith(r)))
                {
                    context.Response.StatusCode = 301;
                    context.Response.Headers.Set("Location", context.Request.Uri.AbsoluteUri.Replace(path, $"/17{path}"));
                    return;
                }
                await next();
            });
        }
    }
}
