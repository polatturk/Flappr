using Flappr.Models;

namespace Flappr.Dto
{
    public class FlapRequest
    {
        public Flap Flap { get; set; }
        public List<Comment> Comments { get; set; } = new List<Comment>();
    }

    public class FlapInfoDto
    {
        public string Username { get; set; }
        public string Detail { get; set; }
        public string Mail { get; set; }
    }
}
