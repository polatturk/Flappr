using System.ComponentModel.DataAnnotations;

namespace Flappr.Dto
{
    public class PwResetRequest
    {
        [Required]
        public string Token { get; set; }

        [Required]
        public string Pw { get; set; }
    }
}
