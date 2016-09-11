using Microsoft.AspNet.Identity.EntityFramework;
using MTDB.Forums.Models;
using mvcForum.DataProvider.EntityFramework;
using System;
using System.Data.Entity;

namespace MTDB.Forums.MvcForumIdentity {

	public class IdentityDbContext : MVCForumContext {
		public IdentityDbContext() : base("DefaultConnection") { }
		public IdentityDbContext(String nameOrConnectionString) : base(nameOrConnectionString) { }

		public DbSet<ApplicationUser> Users { get; set; }
		public DbSet<IdentityRole> Roles { get; set; }

		protected override void OnModelCreating(DbModelBuilder modelBuilder) {
			base.OnModelCreating(modelBuilder);

			modelBuilder.Entity<IdentityUserLogin>().HasKey(l => l.UserId);
			modelBuilder.Entity<IdentityRole>().HasKey(r => r.Id);
			modelBuilder.Entity<IdentityUserRole>().HasKey(r => new { r.RoleId, r.UserId });
		}
	}
}