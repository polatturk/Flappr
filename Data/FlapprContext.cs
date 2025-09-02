using Flappr.Models;
using Microsoft.EntityFrameworkCore;
using static System.Collections.Specialized.BitVector32;

namespace Flappr.Data
{
    public class FlapprContext : DbContext
    {
        public DbSet<Register> Users { get; set; }
        public DbSet<Flap> Flaps { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<ResetPwToken> ResetPwTokens { get; set; }

        public FlapprContext(DbContextOptions<FlapprContext> options) : base(options)
        {

        }
    }
}
