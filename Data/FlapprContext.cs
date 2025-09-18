using Flappr.Models;
using Microsoft.EntityFrameworkCore;
using static System.Collections.Specialized.BitVector32;

namespace Flappr.Data
{
    public class FlapprContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Flap> Flaps { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<ResetPwToken> ResetPwTokens { get; set; }

        public FlapprContext(DbContextOptions<FlapprContext> options) : base(options)
        {

        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Flap>()
                .HasOne(f => f.User) // Flap’ın 1 kullanıcısı var
                .WithMany(u => u.Flaps) // Kullanıcının birçok flap’ı var
                .HasForeignKey(f => f.UserId) // FK alanı
                .OnDelete(DeleteBehavior.Cascade); // User silinirse flaplar da silinsin
        }
    }
}
