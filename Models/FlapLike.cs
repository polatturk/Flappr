using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Flappr.Models
{
    public class FlapLike
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [ForeignKey("Flap")]
        public Guid FlapId { get; set; }
        public Flap Flap { get; set; } = null!;

        [ForeignKey("User")]
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
