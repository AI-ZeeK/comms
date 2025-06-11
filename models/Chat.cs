using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Comms.Models
{
    [Table("chats", Schema = "communications")]
    public class Chat
    {
        [Key]
        [Column("chat_id")]
        public Guid ChatId { get; set; } = Guid.NewGuid();

        [Column("name")]
        [MaxLength(100)]
        public string? Name { get; set; }

        [Column("avatar_url")]
        [MaxLength(255)]
        public string? AvatarUrl { get; set; }

        [Column("chat_type")]
        public ChatType ChatType { get; set; } = ChatType.DIRECT;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }

        [Column("status")]
        public ChatStatus Status { get; set; } = ChatStatus.PENDING;

        // Navigation properties
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
        public virtual ICollection<ChatParticipant> Participants { get; set; } = new List<ChatParticipant>();
        public virtual ICollection<UnreadMessageCount> UnreadMessageCounts { get; set; } = new List<UnreadMessageCount>();
    }
} 