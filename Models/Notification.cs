namespace Flappr.Models
{
    public class Notification
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } 
        public string SenderId { get; set; }
        public string Type { get; set; } // "Like", "Comment", "Follow"
        public string Message { get; set; } 
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow; 
        public bool IsRead { get; set; } = false; // Okundu mu?

    }
}
