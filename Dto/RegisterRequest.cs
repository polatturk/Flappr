using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Flappr.Dto
{
    public class RegisterRequest
    {
        [Required]
        public string Nickname { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string Pwconfirmend { get; set; }

        [Required]
        public string Mail { get; set; }

        public IFormFile? Image { get; set; }

        [FromForm(Name = "g-recaptcha-response")]
        public string RecaptchaToken { get; set; }
    }
}
