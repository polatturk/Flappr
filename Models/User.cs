using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Flappr.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string Nickname { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string Mail { get; set; }

        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }

        public string? ImgUrl { get; set; }
        public ICollection<Flap> Flaps { get; set; }

    }

}
