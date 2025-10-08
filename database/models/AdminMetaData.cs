using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Comms.Models
{
    [Table("meta_data", Schema = "admin")]
    public class MetaData
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // auto-increment
        [Column("meta_id")]
        public int Id { get; set; }

        [Column("enable_push_notifications")]
        public bool EnablePushNotifications { get; set; } = false;

        [Column("enable_new_user")]
        public bool EnableNewUser { get; set; } = false;

        [Column("enable_system_error")]
        public bool EnableSystemError { get; set; } = false;

        [Column("enable_welcome_email")]
        public bool EnableWelcomeEmail { get; set; } = false;

        [Column("enable_failed_payments")]
        public bool EnableFailedPayments { get; set; } = false;

        [Column("enable_password_reset")]
        public bool EnablePasswordReset { get; set; } = false;

        [Column("enable_newsletter")]
        public bool EnableNewsLetter { get; set; } = false;

        [Column("enable_security_alerts")]
        public bool EnableSecurityAlerts { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
