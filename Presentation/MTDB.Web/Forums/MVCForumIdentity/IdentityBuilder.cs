using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using ApplicationBoilerplate.DataProvider;
using ApplicationBoilerplate.DependencyInjection;
using mvcForum.DataProvider.EntityFramework;
using MTDB.Data;

namespace MTDB.Forums.MvcForumIdentity {

	public class IdentityBuilder : IDependencyBuilder {

		public virtual void Configure(IDependencyContainer container) {
			Database.SetInitializer(new CreateDatabaseIfNotExists<MVCForumContext>());

			container.RegisterPerRequest<IContext, Context>();
			container.RegisterPerRequest<DbContext, MVCForumContext>(new Dictionary<string, object> { { "nameOrConnectionString", ConfigurationManager.ConnectionStrings["mvcForum.DataProvider.MainDB"].ConnectionString } });
            container.RegisterPerRequest<IDbContext, K17DbContext>();

            container.RegisterGeneric(typeof(IRepository<>), typeof(Repository<>));

			// TODO: Do this in some other way!!
			new SpecificRepositoryConfiguration().Configure(container);

			//container.RegisterGenericPerRequest(typeof(IUserStore<User>), typeof(UserStore<User>));
			//container.RegisterGenericPerRequest(typeof(UserManager<User>), typeof(UserManager<User>));
			//container.RegisterGenericPerRequest(typeof(RoleManager<IdentityRole>), typeof(RoleManager<IdentityRole>));
		}

		public void ValidateRequirements(IList<ApplicationRequirement> feedback) { }
	}
}