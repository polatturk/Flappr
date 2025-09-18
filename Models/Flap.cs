using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Flappr.Models
{
    public class Flap
    {
        public int Id { get; set; }
        [Required]
        public string Detail { get; set; }
        public DateTime CreatedDate { get; set; }
        [Required]
        public bool Visibility { get; set; }
        public int YorumSayisi { get; set; }
        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; }
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
