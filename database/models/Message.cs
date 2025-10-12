using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Comms.Models
{
    [Table("messages", Schema = "communications")]
    public class Message
    {
        [Key]
        [Column("message_id")]
        public Guid MessageId { get; set; } = Guid.NewGuid();

        [Column("chat_id")]
        public Guid ChatId { get; set; }

        [Column("sender_id")]
        public Guid? SenderId { get; set; }

        [Column("content")]
        public string Content { get; set; } = string.Empty;

        [Column("media_urls")]
        public string[] MediaUrls { get; set; } = Array.Empty<string>();

        [Column("type")]
        public MessageType Type { get; set; } = MessageType.TEXT;

        [Column("status")]
        public MessageStatus Status { get; set; } = MessageStatus.SENT;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }

        // For audio messages
        [Column("duration")]
        public int? Duration { get; set; }

        // For file/image messages
        [Column("file_url")]
        public string? FileUrl { get; set; }

        [Column("file_size")]
        public int? FileSize { get; set; }

        [Column("file_type")]
        public string? FileType { get; set; }

        [NotMapped]
        public object? Sender { get; set; }

        // Navigation properties
        [ForeignKey("ChatId")]
        public virtual Chat Chat { get; set; } = null!;

        public virtual ICollection<MessageRead> ReadReceipts { get; set; } =
            new List<MessageRead>();
    }
}
