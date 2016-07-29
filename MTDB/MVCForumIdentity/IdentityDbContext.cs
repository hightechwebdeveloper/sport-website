//using Microsoft.AspNet.Identity.EntityFramework;
//using MTDB.Models;
//using mvcForum.DataProvider.EntityFramework;
//using System;
//using System.Data.Entity;
//using MTDB.Core.EntityFramework;

//namespace MTDB.MvcForumIdentity
//{

//    public class Identity1DbContext : MVCForumContext
//    {
//        public Identity1DbContext() : base("DefaultConnection") { }
//        public Identity1DbContext(String nameOrConnectionString) : base(nameOrConnectionString) { }

//        public DbSet<ApplicationUser> Users { get; set; }
//        public DbSet<IdentityRole> Roles { get; set; }

//        protected override void OnModelCreating(DbModelBuilder modelBuilder)
//        {
//            base.OnModelCreating(modelBuilder);

//            modelBuilder.Entity<IdentityUserLogin>().HasKey<String>(l => l.UserId);
//            modelBuilder.Entity<IdentityRole>().HasKey<String>(r => r.Id);
//            modelBuilder.Entity<IdentityUserRole>().HasKey(r => new { r.RoleId, r.UserId });
//        }
//    }
//}