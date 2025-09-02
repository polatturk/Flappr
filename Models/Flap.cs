using System.ComponentModel.DataAnnotations;

namespace Flappr.Models
{
    public class Flap
    {
        public int Id { get; set; }
        public string? Nickname { get; set; }
        public string? ImgUrl { get; set; }
        [Required]
        public string? Detail { get; set; }
        public int UserId { get; set; }
        public string? Username { get; set; }
        public DateTime CreatedDate { get; set; }
        [Required]
        public bool Visibility { get; set; }
        public int YorumSayisi { get; set; }
    }

    public class FlapDetail
    {
        public Flap Flap { get; set; }
        public List<Comment>? Comments { get; set; }
    }

    public class FlapInfo
    {
        public string Username { get; set; }
        public string Detail { get; set; }
        public string Mail { get; set; }
    }

}
