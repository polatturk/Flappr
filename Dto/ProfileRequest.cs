using Flappr.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace Flappr.Dto
{
    public class ProfileRequest
    {
        public List<FlapDto> Flaps { get; set; } = new List<FlapDto>();
        public UserDto User { get; set; }
        public int FollowersCount { get; set; }   
        public int FollowingCount { get; set; } 
    }
    public class FlapDto
    {
        public Guid Id { get; set; }
        public string Detail { get; set; }
        public string Username { get; set; }
        public string? Nickname { get; set; }
        public string? ImgUrl { get; set; }
        public bool Visibility { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CommentsCount { get; set; }
        public int LikeCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
    }

    public class UserDto
    {
        public Guid Id { get; set; }
        public string Nickname { get; set; }
        public string Username { get; set; }
        public string Mail { get; set; }
        public string? ImgUrl { get; set; }
        [NotMapped]
        public IFormFile? Image { get; set; }
        public Guid RoleId { get; set; }
        public string? RoleName { get; set; }
        public string Password { get; set; }

    }
}
