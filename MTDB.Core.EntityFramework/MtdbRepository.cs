using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using MTDB.Core.EntityFramework.Entities;

namespace MTDB.Core.EntityFramework
{

    public class MtdbRepository : IdentityDbContext<ApplicationUser>
    {
        public MtdbRepository(string connectionString) : base(connectionString)
        {

        }

        public MtdbRepository() : base("MTDBRepository")
        {

        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PlayerUpdate>().HasMany(p => p.Changes);
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Player> Players { get; set; }
        public DbSet<PlayerStat> PlayerStats { get; set; }
        public DbSet<PlayerUpdate> PlayerUpdates { get; set; }
        public DbSet<Stat> Stats { get; set; }
        public DbSet<StatCategory> StatCategories { get; set; }
        public DbSet<Lineup> Lineups { get; set; }

        public IQueryable<Player> PlayersWithStats
        {
            get { return Players?.Include(p => p.Stats.Select(y => y.Stat.Category)).Include(p => p.Team).Include(p => p.Theme).Include(p => p.Tier).Include(p => p.Collection); }
        }

        public IQueryable<Lineup> LineupsWithPlayers
        {
            get { return Lineups?.Include(p => p.Players.Select(y => y.Player.Stats.Select(z => z.Stat.Category))).Include(l => l.User); }
        }

        public DbSet<LineupPlayer> LineupPlayers { get; set; }


        public DbSet<Tier> Tiers { get; set; }
        public DbSet<Theme> Themes { get; set; }
        public DbSet<Conference> Conferences { get; set; }
        public DbSet<Division> Divisions { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<CardPack> CardPacks { get; set; }

        public DbSet<Collection> Collections { get; set; }

        public DbSet<Comment> Comments { get; set; }
        public DbSet<PlayerUpdateChange> PlayerUpdateChanges { get; set; }

        public IQueryable<Comment> CommentsWithUsers
        {
            get { return Comments.Include(x => x.User); }
        }

        public static MtdbRepository Create()
        {
            return new MtdbRepository();
        }
    }

    public class ApplicationUser : IdentityUser
    {
        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }
    }

}
