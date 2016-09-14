﻿using Microsoft.AspNet.Identity;
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

            //redirect www to non-www
            app.Use(async (context, next) =>
            {
                if (context.Request.Uri.Host.StartsWith("www."))
                {
                    var host = context.Request.Uri.Host;

                    context.Response.StatusCode = 301;
                    context.Response.Headers.Set("Location", context.Request.Uri.AbsoluteUri.Replace(host, host.Substring(4)));
                }
                await next();
            });

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/account/login"),
                CookieName = "authentication"
            });
        }
    }
}
