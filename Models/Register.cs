using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Flappr.Models
{
    public class Register
    {
        public int Id { get; set; }
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
        public int RoleId { get; set; }
        public string? RoleName { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
        public string? ImgUrl { get; set; }
        public IFormFile? Image { get; set; }
        [FromForm(Name = "g-recaptcha-response")]
        public string RecaptchaToken { get; set; }
    }

    public class Login
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public string Nickname { get; set; }
        [Required]
        public string Password { get; set; }
        [FromForm(Name = "g-recaptcha-response")]
        public string RecaptchaToken { get; set; }
    }

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

    public class Profile
    {
        public List<Flap> Flaps { get; set; }
        public Register User { get; set; }
    }

    public class Comment
    {
        public int Id { get; set; }
        public string Summary { get; set; }
        public DateTime CreatedTime { get; set; }
        public int UserId { get; set; }
        public int FlapId { get; set; }
        public string? Nickname { get; set; }
        public string? ImgUrl { get; set; }
        public string? Username { get; set; }
    }

    public class DetailFlap
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

    public class ResetPwToken
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Token { get; set; }
        public DateTime Created { get; set; }
        public bool Used { get; set; }
    }

    public class PwReset
    {
        [Required]
        public string Token { get; set; }
        [Required]
        public string Pw { get; set; }
    }

    public class Arama
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Nickname { get; set; }
    }

    public class AramaModel
    {
        public string SearchTerm { get; set; }
        public List<Arama> Sonuc { get; set; }
    }
}
