using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Comms.Models
{
    [Table("unread_message_counts", Schema = "communications")]
    public class UnreadMessageCount
    {
        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("chat_id")]
        public Guid ChatId { get; set; }

        [Column("count")]
        public int Count { get; set; } = 0;

        [Column("last_read_at")]
        public DateTime LastReadAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ChatId")]
        public virtual Chat Chat { get; set; } = null!;
    }
} 