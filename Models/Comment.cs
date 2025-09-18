using System.ComponentModel.DataAnnotations.Schema;

namespace Flappr.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public string Summary { get; set; }
        public DateTime CreatedTime { get; set; }
        public int FlapId { get; set; }
        public string? Nickname { get; set; }
        public string? ImgUrl { get; set; }
        public string? Username { get; set; }
        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; }

    }
}
