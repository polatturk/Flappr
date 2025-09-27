using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Flappr.Models
{
    public class Follow
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid FollowerId { get; set; }    
        public Guid FollowingId { get; set; } 
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    }
}
