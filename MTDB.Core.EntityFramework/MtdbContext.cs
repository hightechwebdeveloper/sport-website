using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using MTDB.Core.EntityFramework.Entities;

namespace MTDB.Core.EntityFramework
{

    public class MtdbContext : IdentityDbContext<ApplicationUser>
    {
        public MtdbContext(string connectionString) : base(connectionString, throwIfV1Schema: false)
        {

        }

        public MtdbContext() : base("MTDBRepository", throwIfV1Schema: false)
        {
            this.Database.CommandTimeout = int.MaxValue;
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Player>()
                .Property(puc => puc.TierId)
                .HasColumnName("Tier_Id");
            modelBuilder.Entity<Player>()
               .Property(puc => puc.ThemeId)
               .HasColumnName("Theme_Id");
            modelBuilder.Entity<Player>()
                .Property(puc => puc.TeamId)
                .HasColumnName("Team_Id");
            modelBuilder.Entity<Player>()
                .Property(puc => puc.CollectionId)
                .HasColumnName("Collection_Id");

            modelBuilder.Entity<PlayerStat>()
                .Property(puc => puc.PlayerId)
                .HasColumnName("Player_Id");
            modelBuilder.Entity<PlayerStat>()
                .Property(puc => puc.StatId)
                .HasColumnName("Stat_Id");

            modelBuilder.Entity<Team>()
                .Property(puc => puc.DivisionId)
                .HasColumnName("Division_Id");

            modelBuilder.Entity<PlayerUpdateChange>()
                .Property(puc => puc.PlayerUpdateId)
                .HasColumnName("PlayerUpdate_Id");
            modelBuilder.Entity<PlayerUpdateChange>()
                .HasRequired(puc => puc.PlayerUpdate)
                .WithMany(pu => pu.Changes)
                .HasForeignKey(puc => puc.PlayerUpdateId);

            modelBuilder.Entity<PlayerUpdateChange>()
                .Property(puc => puc.PlayerId)
                .HasColumnName("Player_Id");
            modelBuilder.Entity<PlayerUpdateChange>()
                .HasRequired(puc => puc.Player)
                .WithMany(p => p.PlayerUpdateChanges)
                .HasForeignKey(puc => puc.PlayerId);

            modelBuilder.Entity<Badge>()
                      .HasOptional(x => x.BadgeGroup)
                      .WithMany()
                      .HasForeignKey(x => x.BadgeGroupId);
            modelBuilder.Entity<PlayerBadge>()
                .HasKey(pb => new { pb.PlayerId, pb.BadgeId });
            modelBuilder.Entity<PlayerBadge>()
                      .HasRequired(x => x.Player)
                      .WithMany(x => x.PlayerBadges)
                      .HasForeignKey(x => x.PlayerId);
            modelBuilder.Entity<PlayerBadge>()
                      .HasRequired(x => x.Badge)
                      .WithMany()
                      .HasForeignKey(x => x.BadgeId);

            modelBuilder.Entity<PlayerTendency>()
                .HasKey(pb => new { pb.PlayerId, pb.TendencyId });
            modelBuilder.Entity<PlayerTendency>()
                      .HasRequired(x => x.Player)
                      .WithMany(x => x.PlayerTendencies)
                      .HasForeignKey(x => x.PlayerId);
            modelBuilder.Entity<PlayerTendency>()
                      .HasRequired(x => x.Tendency)
                      .WithMany()
                      .HasForeignKey(x => x.TendencyId);

            modelBuilder.Entity<CardPack>()
                .Ignore(cp => cp.CardPackType);

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Player> Players { get; set; }
        public DbSet<PlayerStat> PlayerStats { get; set; }
        public DbSet<PlayerUpdate> PlayerUpdates { get; set; }
        public DbSet<Stat> Stats { get; set; }
        public DbSet<StatCategory> StatCategories { get; set; }
        public DbSet<Lineup> Lineups { get; set; }
        public DbSet<LineupPlayer> LineupPlayers { get; set; }

        public DbSet<Tier> Tiers { get; set; }
        public DbSet<Theme> Themes { get; set; }
        public DbSet<Conference> Conferences { get; set; }
        public DbSet<Division> Divisions { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<CardPack> CardPacks { get; set; }
        public DbSet<CardPackPlayer> CardPackPlayers { get; set; }
        public DbSet<Collection> Collections { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<PlayerUpdateChange> PlayerUpdateChanges { get; set; }

        public DbSet<Badge> Badges { get; set; }
        public DbSet<PlayerBadge> PlayerBadges { get; set; }
        public DbSet<BadgeGroup> BadgeGroups { get; set; }

        public DbSet<Tendency> Tendencies { get; set; }

        public static MtdbContext Create()
        {
            return new MtdbContext();
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
