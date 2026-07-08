// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArcaneVault.API.Data
{
    /// <summary>
    /// Records all transactions on a Fixed Deposit account:
    /// - Interest accruals (quarterly/annually)
    /// - Premature withdrawals
    /// - Maturity settlement
    /// - Penalties applied
    /// </summary>
    public class FixedDepositTransactions
    {
        [Key]
        public int TransactionId { get; set; }

        [Required]
        [ForeignKey("FDAccount")]
        public int FDAccountId { get; set; }

        /// <summary>
        /// Type of transaction: "InterestAccrual", "PrematureWithdrawal", "MaturitySettlement", "Penalty"
        /// </summary>
        [Required]
        [StringLength(30)]
        public string TransactionType { get; set; } = string.Empty;

        /// <summary>
        /// Description of the transaction (e.g., "Quarterly Interest - Q1 2026").
        /// </summary>
        [StringLength(200)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Amount involved in the transaction.
        /// For interest accrual: interest earned.
        /// For withdrawal: amount withdrawn.
        /// For penalty: penalty amount.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(15,2)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Balance after the transaction.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(15,2)")]
        public decimal BalanceAfter { get; set; }

        /// <summary>
        /// Date when the transaction occurred.
        /// </summary>
        [Required]
        public DateTime TransactionDate { get; set; }

        /// <summary>
        /// For interest transactions, the accrual period (e.g., "Q1-2026").
        /// </summary>
        [StringLength(20)]
        public string? AccrualPeriod { get; set; }

        public bool IsDeleted { get; set; } = false;

        // Navigation
        public FixedDepositAccounts? FDAccount { get; set; }
    }
}
