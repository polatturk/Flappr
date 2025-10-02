using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Flappr.Models
{
    public class Flap
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Detail { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Required]
        public bool Visibility { get; set; }

        public int CommentCount { get; set; }

        public int LikeCount { get; set; }

        [ForeignKey("User")]
        public Guid UserId { get; set; }

        public User User { get; set; }

        public ICollection<FlapLike> Likes { get; set; } = new List<FlapLike>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();


    }
}
