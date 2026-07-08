// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArcaneVault.API.Data
{
    /// <summary>
    /// Represents a Fixed Deposit account opened by a user.
    /// Stores the principal amount, interest rate, tenure, and current balance.
    /// </summary>
    public class FixedDepositAccounts
    {
        [Key]
        public int FDAccountId { get; set; }

        [Required]
        [StringLength(50)]
        [ForeignKey("User")]
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Type of FD: "Regular", "TaxSaver", "SeniorCitizen"
        /// </summary>
        [Required]
        [StringLength(20)]
        public string AccountType { get; set; } = "Regular";

        /// <summary>
        /// Principal amount deposited (in rupees).
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(15,2)")]
        public decimal PrincipalAmount { get; set; }

        /// <summary>
        /// Annual interest rate (as percentage). E.g., 7.5 means 7.5%.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal AnnualInterestRate { get; set; }

        /// <summary>
        /// Tenure in months.
        /// </summary>
        [Required]
        public int TenureMonths { get; set; }

        /// <summary>
        /// Opening date of the FD.
        /// </summary>
        [Required]
        public DateTime OpenedDate { get; set; }

        /// <summary>
        /// Maturity date (calculated from OpenedDate + TenureMonths).
        /// </summary>
        [Required]
        public DateTime MaturityDate { get; set; }

        /// <summary>
        /// Current balance including accrued interest.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(15,2)")]
        public decimal CurrentBalance { get; set; }

        /// <summary>
        /// Total interest earned so far.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(15,2)")]
        public decimal AccruedInterest { get; set; }

        /// <summary>
        /// Status: "Active", "Matured", "Closed", "Withdrawn"
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Active";

        /// <summary>
        /// If true, interest is compounded quarterly (default); else annually.
        /// </summary>
        public bool IsQuarterlyCompounding { get; set; } = true;

        /// <summary>
        /// If set, FD was prematurely withdrawn on this date.
        /// </summary>
        public DateTime? WithdrawalDate { get; set; }

        /// <summary>
        /// Amount deducted as penalty for premature withdrawal.
        /// </summary>
        [Column(TypeName = "decimal(15,2)")]
        public decimal PenaltyAmount { get; set; } = 0;

        /// <summary>
        /// Amount actually received on withdrawal (CurrentBalance - PenaltyAmount).
        /// </summary>
        [Column(TypeName = "decimal(15,2)")]
        public decimal? AmountReceived { get; set; }

        public bool IsDeleted { get; set; } = false;

        // Navigation
        public ArcaneVaultUsers? User { get; set; }
        public ICollection<FixedDepositTransactions> Transactions { get; set; } = new List<FixedDepositTransactions>();
    }
}
