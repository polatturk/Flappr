using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Flappr.Models
{
    public class User
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        public string Nickname { get; set; }
        [Required]
        public string Username { get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        public string Mail { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime Updated { get; set; }
        public string? ImgUrl { get; set; }
        public string? Biography { get; set; }
        public ICollection<Flap> Flaps { get; set; }
        public ICollection<FlapLike> FlapLikes { get; set; } = new List<FlapLike>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();

    }

}
