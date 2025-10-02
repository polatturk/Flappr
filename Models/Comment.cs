using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Flappr.Models
{
    public class Comment
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Summary { get; set; }

        public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

        public Guid FlapId { get; set; }
        public Flap Flap { get; set; }

        [ForeignKey("User")]
        public Guid UserId { get; set; }
        public User User { get; set; }

        public string? Nickname { get; set; }
        public string? ImgUrl { get; set; }
        public string? Username { get; set; }
    }
}

