using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Flappr.Models
{
    public class ResetPwToken
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid UserId { get; set; }  

        [Required]
        public string Token { get; set; } = string.Empty;

        public DateTime Created { get; set; } = DateTime.UtcNow;

        public bool Used { get; set; } = false;
    }
}
