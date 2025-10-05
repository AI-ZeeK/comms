using System.ComponentModel.DataAnnotations;

namespace Comms.Models.DTOs
{
    public class CreatePushSubscriptionDto
    {
        [Required]
        public string PublicKey { get; set; } = string.Empty; // "WEB", "ANDROID", "IOS"
    }
    public class PushSubscriptionDto
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public SubscriptionDto Subscription { get; set; } = new();

        [Required]
        public string Platform { get; set; } = string.Empty; // "WEB", "ANDROID", "IOS"
    }

    public class SubscriptionDto
    {
        [Required]
        public string Endpoint { get; set; } = string.Empty;

        [Required]
        public SubscriptionKeysDto Keys { get; set; } = new();

    }

    public class SubscriptionKeysDto
    {
        [Required]
        public string P256dh { get; set; } = string.Empty;

        [Required]
        public string Auth { get; set; } = string.Empty;
    }
}
