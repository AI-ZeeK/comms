using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Comms.Models    
{
    [Table("vapid_keys", Schema = "communications")]
    public class VapidKeys
    {
        [Key]
        [Column("vapid_key_id")]
        public Guid SubscriptionId { get; set; } = Guid.NewGuid();

        [Column("public_key")]
        public string PublicKey { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
} 