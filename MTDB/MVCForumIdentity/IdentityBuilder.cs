using ApplicationBoilerplate.DataProvider;
using ApplicationBoilerplate.DependencyInjection;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using MTDB.Core.EntityFramework;
using MTDB.Models;
using mvcForum.DataProvider.EntityFramework;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;

namespace MTDB.MvcForumIdentity {

	public class IdentityBuilder : IDependencyBuilder {

		public virtual void Configure(IDependencyContainer container) {
			Database.SetInitializer<MVCForumContext>(new CreateDatabaseIfNotExists<MVCForumContext>());

			container.RegisterPerRequest<IContext, Context>();
			container.RegisterPerRequest<System.Data.Entity.DbContext, MVCForumContext>(new Dictionary<string, object> { { "nameOrConnectionString", ConfigurationManager.ConnectionStrings["mvcForum.DataProvider.MainDB"].ConnectionString } });
            container.RegisterPerRequest<Core.EntityFramework.MtdbContext, Core.EntityFramework.MtdbContext>();

            container.RegisterGeneric(typeof(IRepository<>), typeof(Repository<>));

			// TODO: Do this in some other way!!
			new SpecificRepositoryConfiguration().Configure(container);

			//container.RegisterGenericPerRequest(typeof(IUserStore<ApplicationUser>), typeof(UserStore<ApplicationUser>));
			//container.RegisterGenericPerRequest(typeof(UserManager<ApplicationUser>), typeof(UserManager<ApplicationUser>));
			//container.RegisterGenericPerRequest(typeof(RoleManager<IdentityRole>), typeof(RoleManager<IdentityRole>));
		}

		public void ValidateRequirements(IList<ApplicationRequirement> feedback) { }
	}
}