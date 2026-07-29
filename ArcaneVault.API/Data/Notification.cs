// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArcaneVault.API.Data
{
    /// <summary>
    /// A notification message sent to a specific user.
    /// </summary>
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        /// <summary>The user this notification is for.</summary>
        [Required]
        [StringLength(50)]
        public string UserName { get; set; } = string.Empty;

        /// <summary>Short message shown in the bell dropdown.</summary>
        [Required]
        [StringLength(300)]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Category for icon/colour: "collection", "marketplace", "offer", "wallet", "system"
        /// </summary>
        [StringLength(30)]
        public string Category { get; set; } = "system";

        /// <summary>Optional deep-link URL inside the web app.</summary>
        [StringLength(300)]
        public string? LinkUrl { get; set; }

        public bool IsRead { get; set; } = false;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ArcaneVaultUsers? User { get; set; }
    }
}
