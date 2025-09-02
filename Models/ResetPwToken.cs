using System.ComponentModel.DataAnnotations;

namespace Flappr.Models
{
    public class ResetPwToken
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Token { get; set; }
        public DateTime Created { get; set; }
        public bool Used { get; set; }
    }
}
