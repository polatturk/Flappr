namespace Flappr.Dto
{
    public class FollowRequest
    {
        public Guid FollowerId { get; set; }
        public Guid FollowingId { get; set; }
    }
}
