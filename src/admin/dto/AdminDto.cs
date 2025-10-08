using System.ComponentModel.DataAnnotations;

namespace Comms.Models.DTOs
{
    public class AdminMetaDataDto
    {
        [Required]
        public bool EnablePushNotifications { get; set; }

        [Required]
        public bool EnableNewUser { get; set; }

        [Required]
        public bool EnableSystemError { get; set; }

        [Required]
        public bool EnableWelcomeEmail { get; set; }

        [Required]
        public bool EnableFailedPayments { get; set; }

        [Required]
        public bool EnablePasswordReset { get; set; }

        [Required]
        public bool EnableNewsLetter { get; set; }

        [Required]
        public bool EnableSecurityAlerts { get; set; }
    }

    public class SubscriptionKeysDto
    {
        [Required]
        public string P256dh { get; set; } = string.Empty;

        [Required]
        public string Auth { get; set; } = string.Empty;
    }
}
