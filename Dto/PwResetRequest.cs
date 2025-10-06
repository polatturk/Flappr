using System.ComponentModel.DataAnnotations;

namespace Flappr.Dto
{
    public class PwResetRequest
    {
        public string Token { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
}
