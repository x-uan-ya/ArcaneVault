// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArcaneVault.API.Data
{
    /// <summary>
    /// Records every credit/debit event on a user's wallet.
    /// Types: "TopUp", "Purchase", "Sale"
    /// </summary>
    public class WalletTransaction
    {
        [Key]
        public int TransactionId { get; set; }

        /// <summary>The wallet owner.</summary>
        [Required]
        [StringLength(50)]
        public string UserName { get; set; } = string.Empty;

        /// <summary>"TopUp", "Purchase", or "Sale"</summary>
        [Required]
        [StringLength(20)]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Absolute value of the change.
        /// Positive = credit (TopUp / Sale), Negative = debit (Purchase).
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        /// <summary>Human-readable note, e.g. "Bought 1x Black Lotus from seller123"</summary>
        [StringLength(300)]
        public string Description { get; set; } = string.Empty;

        /// <summary>Balance immediately after this transaction.</summary>
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal BalanceAfter { get; set; }

        [Required]
        public DateTime TransactionDate { get; set; }

        // Navigation
        public ArcaneVaultUsers? User { get; set; }
    }
}
