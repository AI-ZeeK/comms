using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Comms.Models
{
    [Table("notification", Schema = "notifications")]
    public class Notification
    {
        [Key]
        [Column("notification_id")]
        public Guid NotificationId { get; set; } = Guid.NewGuid();

        [Column("recipient_user_id")]
        public Guid? RecipientUserId { get; set; }

        [Column("sender_user_id")]
        public Guid? SenderUserId { get; set; }

        [Column("notification_type")]
        public NotificationType Type { get; set; }

        [Column("text")]
        public string Text { get; set; } = string.Empty;

        [Column("link_url")]
        public string? LinkUrl { get; set; }

        [Column("is_read")]
        public bool IsRead { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
