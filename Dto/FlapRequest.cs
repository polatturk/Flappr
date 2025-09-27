using Flappr.Models;

namespace Flappr.Dto
{
    public class FlapRequest
    {
        public Guid Id { get; set; }
        public string Detail { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UserNickname { get; set; }
        public string UserUsername { get; set; }
        public string UserImgUrl { get; set; }
        public Flap Flap { get; set; }
        public bool Visibility { get; set; }
        public int CommentsCount { get; set; }
        public int LikeCount { get; set; }
        public List<Comment> Comments { get; set; } = new List<Comment>();
    }
    public class AddFlapDto
    {
        public string Detail { get; set; }
        public bool Visibility { get; set; }
    }
}
