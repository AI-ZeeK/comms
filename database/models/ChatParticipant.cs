using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Comms.Models
{
    [Table("chat_participants", Schema = "communications")]
    public class ChatParticipant
    {
        [Column("chat_id")]
        public Guid ChatId { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("joined_at")]
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        [Column("is_admin")]
        public bool IsAdmin { get; set; } = false;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("left_at")]
        public DateTime? LeftAt { get; set; }

        [Column("unread_count")]
        public int UnreadCount { get; set; } = 0;

        // Navigation properties
        [ForeignKey("ChatId")]
        public virtual Chat Chat { get; set; } = null!;
    }
} 