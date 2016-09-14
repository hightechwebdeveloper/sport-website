using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using MTDB.Data.Entities;

namespace MTDB.Data
{
    public class MtdbContext : IdentityDbContext<ApplicationUser>, IDbContext
    {
        public MtdbContext() : base("MTDBRepository", throwIfV1Schema: false)
        {
            this.Database.CommandTimeout = int.MaxValue;
        }
        public static MtdbContext Create()
        {
            return new MtdbContext();
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
            modelBuilder.Entity<Player>()
                .HasMany(p => p.PlayerTendencies)
                .WithRequired()
                .HasForeignKey(pt => pt.PlayerId);
            modelBuilder.Entity<Player>()
                .HasMany(p => p.PlayerBadges)
                .WithRequired()
                .HasForeignKey(pt => pt.PlayerId);

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
                .WithMany()
                .HasForeignKey(puc => puc.PlayerId);


            modelBuilder.Entity<Comment>()
                .Property(puc => puc.UserId)
                .HasColumnName("User_Id");

            modelBuilder.Entity<Lineup>()
                .Property(puc => puc.UserId)
                .HasColumnName("User_Id");

            modelBuilder.Entity<CardPack>()
                .Property(puc => puc.UserId)
                .HasColumnName("User_Id");

            modelBuilder.Entity<CardPackPlayer>()
                .Property(puc => puc.PlayerId)
                .HasColumnName("Player_Id");

            modelBuilder.Entity<Badge>()
                      .HasOptional(x => x.BadgeGroup)
                      .WithMany()
                      .HasForeignKey(x => x.BadgeGroupId);
            modelBuilder.Entity<PlayerBadge>()
                .HasKey(pb => new { pb.PlayerId, pb.BadgeId });
            modelBuilder.Entity<PlayerBadge>()
                      .HasRequired(x => x.Badge)
                      .WithMany()
                      .HasForeignKey(x => x.BadgeId);

            modelBuilder.Entity<PlayerTendency>()
                .HasKey(pb => new { pb.PlayerId, pb.TendencyId });
            modelBuilder.Entity<PlayerTendency>()
                      .HasRequired(x => x.Tendency)
                      .WithMany()
                      .HasForeignKey(x => x.TendencyId);

            modelBuilder.Entity<CardPack>()
                .Ignore(cp => cp.CardPackType);

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<TEntity> Set<TEntity>() where TEntity : class
        {
            return base.Set<TEntity>();
        }

        public DbRawSqlQuery<TElement> SqlQuery<TElement>(string sql, params object[] parameters)
        {
            return this.Database.SqlQuery<TElement>(sql, parameters);
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
