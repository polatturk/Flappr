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
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ResetPwToken> ResetPwTokens { get; set; }

        public FlapprContext(DbContextOptions<FlapprContext> options) : base(options)
        {

        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Flap - User
            modelBuilder.Entity<Flap>()
                .HasOne(f => f.User)
                .WithMany(u => u.Flaps)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // FlapLike - Flap
            modelBuilder.Entity<FlapLike>()
                .HasOne(fl => fl.Flap)
                .WithMany(f => f.Likes)
                .HasForeignKey(fl => fl.FlapId)
                .OnDelete(DeleteBehavior.Restrict);

            // Comment - Flap
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Flap)
                .WithMany(f => f.Comments)
                .HasForeignKey(c => c.FlapId)
                .OnDelete(DeleteBehavior.Cascade);

            // FlapLike - User
            modelBuilder.Entity<FlapLike>()
                .HasOne(fl => fl.User)
                .WithMany(u => u.FlapLikes)
                .HasForeignKey(fl => fl.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Comment - User
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
