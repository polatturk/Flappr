using Flappr.Models;
using Microsoft.EntityFrameworkCore;
using static System.Collections.Specialized.BitVector32;

namespace Flappr.Data
{
    public class FlapprContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Follow> Follows { get; set; }
        public DbSet<Flap> Flaps { get; set; }
        public DbSet<FlapLike> FlapLike { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<ResetPwToken> ResetPwTokens { get; set; }

        public FlapprContext(DbContextOptions<FlapprContext> options) : base(options)
        {

        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Flap>()
                .HasOne(f => f.User)
                .WithMany(u => u.Flaps)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

           modelBuilder.Entity<FlapLike>()
               .HasOne(f => f.Flap)
               .WithMany(f => f.Likes)
               .HasForeignKey(f => f.FlapId)
               .OnDelete(DeleteBehavior.Restrict);


           modelBuilder.Entity<FlapLike>()
               .HasOne(f => f.User)
               .WithMany(u => u.FlapLikes)
               .HasForeignKey(f => f.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        }
    }
}
