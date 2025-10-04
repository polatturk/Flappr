using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Flappr.Dto
{
    public class LoginRequest
    {
        [Required]
        public string Mail { get; set; }
        [Required]
        public string Password { get; set; }
        //[FromForm(Name = "g-recaptcha-response")]
        //public string RecaptchaToken { get; set; }
    }
}
