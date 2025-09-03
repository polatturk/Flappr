using Flappr.Models;

namespace Flappr.Dto
{
    public class ProfileRequest
    {
        public List<FlapDto> Flaps { get; set; } = new List<FlapDto>();
        public UserDto User { get; set; }
    }
    public class FlapDto
    {
        public int Id { get; set; }
        public string Detail { get; set; }
        public string Username { get; set; }
        public string? Nickname { get; set; }
        public string? ImgUrl { get; set; }
        public bool Visibility { get; set; }
        public DateTime CreatedDate { get; set; }
        public int YorumSayisi { get; set; }
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Nickname { get; set; }
        public string Username { get; set; }
        public string Mail { get; set; }
        public string? ImgUrl { get; set; }
        public int RoleId { get; set; }
        public string? RoleName { get; set; }
    }
}
