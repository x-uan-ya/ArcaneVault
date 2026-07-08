// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using ArcaneVault.API.Data;

namespace ArcaneVault.API.Services
{
    /// <summary>
    /// Service for Fixed Deposit calculations, interest accrual, and business rule enforcement.
    /// Handles compound interest calculations, maturity processing, and penalty calculations.
    /// </summary>
    public interface IFixedDepositService
    {
        /// <summary>
        /// Creates a new FD account with initial balance and calculates maturity date.
        /// </summary>
        Task<FixedDepositAccounts> CreateFDAccountAsync(
            string userName, string accountType, decimal principal, 
            decimal annualRate, int tenureMonths);

        /// <summary>
        /// Calculates and applies quarterly or annual interest accrual.
        /// </summary>
        Task<(decimal InterestEarned, decimal NewBalance)> AccrueInterestAsync(int fdAccountId);

        /// <summary>
        /// Processes premature withdrawal with penalty calculation.
        /// Penalty: 1% per month remaining until maturity (max 5%).
        /// </summary>
        Task<(decimal AmountReceived, decimal Penalty)> WithdrawPrematurelyAsync(int fdAccountId);

        /// <summary>
        /// Processes maturity settlement - transfers amount to user account (simulated).
        /// </summary>
        Task<decimal> ProcessMaturityAsync(int fdAccountId);

        /// <summary>
        /// Gets current balance with all pending interest for a given date.
        /// </summary>
        Task<decimal> GetCurrentBalanceAsync(int fdAccountId, DateTime? asOfDate = null);

        /// <summary>
        /// Validates FD account eligibility based on account type and tenure.
        /// </summary>
        Task<(bool IsValid, string? ErrorMessage)> ValidateFDAccountAsync(
            string accountType, int tenureMonths, decimal principal);

        /// <summary>
        /// Gets interest rate for given account type and tenure.
        /// Senior Citizen: +0.5% bonus
        /// Tax-Saver: Locked for 5 years minimum
        /// </summary>
        decimal GetInterestRateForType(string accountType, int tenureMonths);
    }

    public class FixedDepositService : IFixedDepositService
    {
        private readonly ArcaneVaultDbContext _db;
        private readonly ILogger<FixedDepositService> _logger;

        public FixedDepositService(ArcaneVaultDbContext db, ILogger<FixedDepositService> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new FD account with business rule validation.
        /// </summary>
        public async Task<FixedDepositAccounts> CreateFDAccountAsync(
            string userName, string accountType, decimal principal, 
            decimal annualRate, int tenureMonths)
        {
            // Validate user exists
            var user = await _db.Users.FindAsync(userName);
            if (user == null || user.IsDeleted)
                throw new ArgumentException($"User '{userName}' not found or deleted.");

            // Validate FD parameters
            var (isValid, errorMessage) = await ValidateFDAccountAsync(accountType, tenureMonths, principal);
            if (!isValid)
                throw new ArgumentException(errorMessage);

            var openedDate = DateTime.UtcNow;
            var maturityDate = openedDate.AddMonths(tenureMonths);

            var fdAccount = new FixedDepositAccounts
            {
                UserName = userName,
                AccountType = accountType,
                PrincipalAmount = principal,
                AnnualInterestRate = annualRate,
                TenureMonths = tenureMonths,
                OpenedDate = openedDate,
                MaturityDate = maturityDate,
                CurrentBalance = principal,
                AccruedInterest = 0,
                Status = "Active",
                IsQuarterlyCompounding = true
            };

            _db.FixedDepositAccounts.Add(fdAccount);
            await _db.SaveChangesAsync();

            // Log creation
            _logger.LogInformation(
                $"FD Account created for user '{userName}': Principal={principal}, " +
                $"Rate={annualRate}%, Tenure={tenureMonths}M, Type={accountType}");

            return fdAccount;
        }

        /// <summary>
        /// Accrues interest based on compounding frequency.
        /// Uses compound interest formula: A = P(1 + r/n)^(nt)
        /// </summary>
        public async Task<(decimal InterestEarned, decimal NewBalance)> AccrueInterestAsync(int fdAccountId)
        {
            var fdAccount = await _db.FixedDepositAccounts.FindAsync(fdAccountId);
            if (fdAccount == null || fdAccount.IsDeleted)
                throw new ArgumentException($"FD Account {fdAccountId} not found.");

            if (fdAccount.Status != "Active")
                throw new InvalidOperationException($"Cannot accrue interest on {fdAccount.Status} account.");

            // Determine compounding frequency
            int compoundingPeriodsPerYear = fdAccount.IsQuarterlyCompounding ? 4 : 1;
            string period = fdAccount.IsQuarterlyCompounding ? "Q" : "A";

            // Get last accrual date from transaction history
            var lastTransaction = _db.FixedDepositTransactions
                .Where(t => t.FDAccountId == fdAccountId && t.TransactionType == "InterestAccrual")
                .OrderByDescending(t => t.TransactionDate)
                .FirstOrDefault();

            DateTime lastAccrualDate = lastTransaction?.TransactionDate ?? fdAccount.OpenedDate;
            
            // Calculate interest for the period
            decimal r = fdAccount.AnnualInterestRate / 100; // Convert to decimal (e.g., 0.075 for 7.5%)
            decimal n = compoundingPeriodsPerYear;
            decimal t = 1.0m / n; // Time period as fraction of year

            // A = P(1 + r/n)^(nt) for one compounding period
            decimal rate = (1 + r / n);
            decimal interestMultiplier = (decimal)Math.Pow((double)rate, (double)t);
            decimal newBalance = fdAccount.CurrentBalance * interestMultiplier;
            decimal interestEarned = newBalance - fdAccount.CurrentBalance;

            // Update account
            fdAccount.CurrentBalance = newBalance;
            fdAccount.AccruedInterest += interestEarned;

            // Check if maturity date has passed
            if (DateTime.UtcNow >= fdAccount.MaturityDate && fdAccount.Status == "Active")
            {
                fdAccount.Status = "Matured";
                _logger.LogInformation($"FD Account {fdAccountId} auto-matured.");
            }

            // Record transaction
            var transaction = new FixedDepositTransactions
            {
                FDAccountId = fdAccountId,
                TransactionType = "InterestAccrual",
                Description = $"Quarterly Interest - {period}{DateTime.UtcNow:yyyy}",
                Amount = interestEarned,
                BalanceAfter = newBalance,
                TransactionDate = DateTime.UtcNow,
                AccrualPeriod = $"{period}{DateTime.UtcNow:yyyy}"
            };

            _db.FixedDepositTransactions.Add(transaction);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                $"Interest accrued on FD {fdAccountId}: {interestEarned:C} " +
                $"New Balance: {newBalance:C}");

            return (interestEarned, newBalance);
        }

        /// <summary>
        /// Processes premature withdrawal with penalty.
        /// Penalty = 1% per month remaining (capped at 5%).
        /// </summary>
        public async Task<(decimal AmountReceived, decimal Penalty)> WithdrawPrematurelyAsync(int fdAccountId)
        {
            var fdAccount = await _db.FixedDepositAccounts.FindAsync(fdAccountId);
            if (fdAccount == null || fdAccount.IsDeleted)
                throw new ArgumentException($"FD Account {fdAccountId} not found.");

            if (fdAccount.Status != "Active")
                throw new InvalidOperationException($"Cannot withdraw from {fdAccount.Status} account.");

            // Calculate months remaining until maturity
            var today = DateTime.UtcNow;
            var monthsRemaining = (fdAccount.MaturityDate.Year - today.Year) * 12 + 
                                  (fdAccount.MaturityDate.Month - today.Month);

            // Calculate penalty: 1% per month remaining (max 5%)
            decimal penaltyPercentage = Math.Min(monthsRemaining * 1.0m / 100, 0.05m);
            decimal penaltyAmount = fdAccount.CurrentBalance * penaltyPercentage;
            decimal amountReceived = fdAccount.CurrentBalance - penaltyAmount;

            // Update account
            fdAccount.Status = "Withdrawn";
            fdAccount.WithdrawalDate = today;
            fdAccount.PenaltyAmount = penaltyAmount;
            fdAccount.AmountReceived = amountReceived;

            // Record penalty transaction
            var penaltyTransaction = new FixedDepositTransactions
            {
                FDAccountId = fdAccountId,
                TransactionType = "Penalty",
                Description = $"Premature Withdrawal Penalty ({monthsRemaining} months remaining)",
                Amount = penaltyAmount,
                BalanceAfter = amountReceived,
                TransactionDate = today
            };

            // Record withdrawal transaction
            var withdrawalTransaction = new FixedDepositTransactions
            {
                FDAccountId = fdAccountId,
                TransactionType = "PrematureWithdrawal",
                Description = "Premature Withdrawal",
                Amount = amountReceived,
                BalanceAfter = 0,
                TransactionDate = today
            };

            _db.FixedDepositTransactions.Add(penaltyTransaction);
            _db.FixedDepositTransactions.Add(withdrawalTransaction);
            await _db.SaveChangesAsync();

            _logger.LogWarning(
                $"Premature withdrawal from FD {fdAccountId}: " +
                $"Amount Received: {amountReceived:C}, Penalty: {penaltyAmount:C}");

            return (amountReceived, penaltyAmount);
        }

        /// <summary>
        /// Processes maturity - marks account as matured and ready for settlement.
        /// </summary>
        public async Task<decimal> ProcessMaturityAsync(int fdAccountId)
        {
            var fdAccount = await _db.FixedDepositAccounts.FindAsync(fdAccountId);
            if (fdAccount == null || fdAccount.IsDeleted)
                throw new ArgumentException($"FD Account {fdAccountId} not found.");

            if (DateTime.UtcNow < fdAccount.MaturityDate)
                throw new InvalidOperationException("FD has not matured yet.");

            if (fdAccount.Status == "Withdrawn")
                throw new InvalidOperationException("Account already withdrawn.");

            // Final interest accrual before maturity
            var (interestEarned, _) = await AccrueInterestAsync(fdAccountId);

            // Update status
            fdAccount.Status = "Matured";

            // Record maturity transaction
            var transaction = new FixedDepositTransactions
            {
                FDAccountId = fdAccountId,
                TransactionType = "MaturitySettlement",
                Description = "FD Maturity Settlement",
                Amount = fdAccount.CurrentBalance,
                BalanceAfter = fdAccount.CurrentBalance,
                TransactionDate = DateTime.UtcNow
            };

            _db.FixedDepositTransactions.Add(transaction);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                $"FD {fdAccountId} matured. Settlement Amount: {fdAccount.CurrentBalance:C}");

            return fdAccount.CurrentBalance;
        }

        /// <summary>
        /// Gets current balance with interest accrual up to specified date.
        /// </summary>
        public async Task<decimal> GetCurrentBalanceAsync(int fdAccountId, DateTime? asOfDate = null)
        {
            var fdAccount = await _db.FixedDepositAccounts.FindAsync(fdAccountId);
            if (fdAccount == null || fdAccount.IsDeleted)
                throw new ArgumentException($"FD Account {fdAccountId} not found.");

            asOfDate ??= DateTime.UtcNow;

            // If already withdrawn, return amount received
            if (fdAccount.Status == "Withdrawn")
                return fdAccount.AmountReceived ?? 0;

            // Calculate interest up to asOfDate
            decimal daysElapsed = (decimal)(asOfDate.Value - fdAccount.OpenedDate).TotalDays;
            decimal daysInYear = 365.25m;
            decimal yearsElapsed = daysElapsed / daysInYear;

            decimal r = fdAccount.AnnualInterestRate / 100;
            decimal n = fdAccount.IsQuarterlyCompounding ? 4 : 1;

            // A = P(1 + r/n)^(nt)
            decimal rate = (1 + r / n);
            decimal exponent = n * yearsElapsed;
            decimal balance = fdAccount.PrincipalAmount * (decimal)Math.Pow((double)rate, (double)exponent);

            return balance;
        }

        /// <summary>
        /// Validates FD account parameters against business rules.
        /// </summary>
        public async Task<(bool IsValid, string? ErrorMessage)> ValidateFDAccountAsync(
            string accountType, int tenureMonths, decimal principal)
        {
            // Validate account type
            if (!new[] { "Regular", "TaxSaver", "SeniorCitizen" }.Contains(accountType))
                return (false, "Invalid account type. Must be 'Regular', 'TaxSaver', or 'SeniorCitizen'.");

            // Validate minimum tenure
            if (tenureMonths < 6)
                return (false, "Minimum tenure is 6 months.");

            // Validate maximum tenure
            if (tenureMonths > 120)
                return (false, "Maximum tenure is 10 years (120 months).");

            // Tax-Saver must be minimum 5 years
            if (accountType == "TaxSaver" && tenureMonths < 60)
                return (false, "Tax-Saver FD requires minimum 5-year tenure.");

            // Validate minimum principal
            if (principal < 10000)
                return (false, "Minimum principal amount is ₹10,000.");

            // Validate maximum principal
            if (principal > 10000000)
                return (false, "Maximum principal amount is ₹1,00,00,000.");

            return (true, null);
        }

        /// <summary>
        /// Gets interest rate based on account type and tenure.
        /// Rates are base rates that can be overridden at creation.
        /// </summary>
        public decimal GetInterestRateForType(string accountType, int tenureMonths)
        {
            // Base rates (simplified - in production, these would be configurable)
            decimal baseRate = accountType switch
            {
                "TaxSaver" => 8.0m,       // Tax-Saver rates are typically higher
                "SeniorCitizen" => 7.5m,  // Senior Citizen: +0.5% bonus
                _ => 7.0m                 // Regular FD
            };

            // Add tenure bonus (higher tenure = higher rate)
            decimal tenureBonus = tenureMonths switch
            {
                >= 60 => 0.5m,  // 5+ years: +0.5%
                >= 36 => 0.25m, // 3+ years: +0.25%
                _ => 0m
            };

            // Senior Citizen bonus
            decimal seniorBonus = accountType == "SeniorCitizen" ? 0.5m : 0;

            return baseRate + tenureBonus + seniorBonus;
        }
    }
}
