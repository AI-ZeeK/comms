using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Comms.Models
{
    [Table("message_reads", Schema = "communications")]
    public class MessageRead
    {
        [Column("message_id")]
        public Guid MessageId { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("read_at")]
        public DateTime? ReadAt { get; set; }

        // Navigation properties
        [ForeignKey("MessageId")]
        public virtual Message Message { get; set; } = null!;
    }
} 