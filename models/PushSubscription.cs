using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Comms.Models    
{
    [Table("push_subscriptions", Schema = "communications")]
    public class PushSubscription
    {
        [Key]
        [Column("subscription_id")]
        public Guid SubscriptionId { get; set; } = Guid.NewGuid();

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("endpoint")]
        public string Endpoint { get; set; } = string.Empty;

        [Column("p256dh")]
        public string? P256dh { get; set; }

        [Column("auth")]
        public string? Auth { get; set; }

        [Column("platform")]
        public string Platform { get; set; } = string.Empty; // "WEB", "ANDROID", "IOS"

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
} 