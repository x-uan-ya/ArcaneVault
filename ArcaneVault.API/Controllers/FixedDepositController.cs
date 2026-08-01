// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using ArcaneVault.API.Data;
using ArcaneVault.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArcaneVault.API.Controllers
{
    /// <summary>
    /// Fixed Deposit (FD) management endpoints.
    /// Handles account creation, interest accrual, withdrawals, and maturity processing.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FixedDepositController : ControllerBase
    {
        private readonly ArcaneVaultDbContext _db;
        private readonly IFixedDepositService _fdService;
        private readonly ILogger<FixedDepositController> _logger;

        public FixedDepositController(
            ArcaneVaultDbContext db, 
            IFixedDepositService fdService, 
            ILogger<FixedDepositController> logger)
        {
            _db = db;
            _fdService = fdService;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new Fixed Deposit account.
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateFDAccount([FromBody] CreateFDRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Always use authenticated user unless Staff is creating on behalf of another
                var authenticatedUser = User.Identity?.Name;
                if (string.IsNullOrEmpty(authenticatedUser))
                    return Unauthorized();

                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                var targetUser = (userRole == "Staff" && !string.IsNullOrWhiteSpace(request.UserName) && request.UserName != authenticatedUser)
                    ? request.UserName
                    : authenticatedUser;

                // Validate the target user exists
                if (!await _db.Users.AnyAsync(u => u.UserName == targetUser && !u.IsDeleted))
                    return BadRequest(new { message = $"User '{targetUser}' not found." });

                // Get interest rate for the account type and tenure
                decimal interestRate = _fdService.GetInterestRateForType(request.AccountType, request.TenureMonths);

                // Create FD account
                var fdAccount = await _fdService.CreateFDAccountAsync(
                    targetUser,
                    request.AccountType,
                    request.PrincipalAmount,
                    interestRate,
                    request.TenureMonths);

                _logger.LogInformation($"FD account created for user '{targetUser}' by '{authenticatedUser}'.");

                return CreatedAtAction(nameof(GetFDAccount), new { id = fdAccount.FDAccountId },
                    new
                    {
                        fdAccount.FDAccountId,
                        fdAccount.UserName,
                        fdAccount.AccountType,
                        fdAccount.PrincipalAmount,
                        fdAccount.AnnualInterestRate,
                        fdAccount.TenureMonths,
                        fdAccount.OpenedDate,
                        fdAccount.MaturityDate,
                        fdAccount.CurrentBalance,
                        fdAccount.Status
                    });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Invalid FD creation: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating FD account: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred creating the FD account." });
            }
        }

        /// <summary>
        /// Gets details of a specific FD account.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetFDAccount(int id)
        {
            try
            {
                var fdAccount = await _db.FixedDepositAccounts
                    .Include(f => f.Transactions)
                    .FirstOrDefaultAsync(f => f.FDAccountId == id && !f.IsDeleted);

                if (fdAccount == null)
                    return NotFound();

                // Verify user ownership or staff access
                var username = User.Identity?.Name;
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                
                if (fdAccount.UserName != username && userRole != "Staff")
                    return Forbid();

                return Ok(new
                {
                    fdAccount.FDAccountId,
                    fdAccount.UserName,
                    fdAccount.AccountType,
                    fdAccount.PrincipalAmount,
                    fdAccount.AnnualInterestRate,
                    fdAccount.TenureMonths,
                    fdAccount.OpenedDate,
                    fdAccount.MaturityDate,
                    fdAccount.CurrentBalance,
                    fdAccount.AccruedInterest,
                    fdAccount.Status,
                    fdAccount.IsQuarterlyCompounding,
                    fdAccount.WithdrawalDate,
                    fdAccount.PenaltyAmount,
                    fdAccount.AmountReceived,
                    TransactionCount = fdAccount.Transactions?.Count ?? 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving FD account {id}: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred retrieving the FD account." });
            }
        }

        /// <summary>
        /// Gets all FD accounts for the current user or all users (Staff only).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFDAccounts()
        {
            try
            {
                var username = User.Identity?.Name;
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

                IQueryable<FixedDepositAccounts> query = _db.FixedDepositAccounts
                    .Where(f => !f.IsDeleted);

                // If not staff, only show user's own accounts
                if (userRole != "Staff")
                    query = query.Where(f => f.UserName == username);

                var accounts = await query
                    .OrderByDescending(f => f.OpenedDate)
                    .Select(f => new
                    {
                        f.FDAccountId,
                        f.UserName,
                        f.AccountType,
                        f.PrincipalAmount,
                        f.AnnualInterestRate,
                        f.TenureMonths,
                        f.OpenedDate,
                        f.MaturityDate,
                        f.CurrentBalance,
                        f.AccruedInterest,
                        f.Status
                    })
                    .ToListAsync();

                return Ok(accounts);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving FD accounts: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred retrieving FD accounts." });
            }
        }

        /// <summary>
        /// Manually triggers interest accrual for a specific FD account.
        /// </summary>
        [HttpPost("{id}/accrue-interest")]
        public async Task<IActionResult> AccrueInterest(int id)
        {
            try
            {
                var fdAccount = await _db.FixedDepositAccounts.FindAsync(id);
                if (fdAccount == null || fdAccount.IsDeleted)
                    return NotFound();

                // Verify authorization
                var username = User.Identity?.Name;
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                
                if (fdAccount.UserName != username && userRole != "Staff")
                    return Forbid();

                var (interestEarned, newBalance) = await _fdService.AccrueInterestAsync(id);

                _logger.LogInformation($"Interest accrued on FD {id}: {interestEarned:C}");

                return Ok(new
                {
                    message = "Interest accrued successfully",
                    interestEarned,
                    newBalance,
                    fdAccountId = id
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Invalid interest accrual: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Cannot accrue interest: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error accruing interest on FD {id}: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred accruing interest." });
            }
        }

        /// <summary>
        /// Withdraws FD amount prematurely (with penalty calculation).
        /// </summary>
        [HttpPost("{id}/withdraw-premature")]
        public async Task<IActionResult> WithdrawPrematurel(int id)
        {
            try
            {
                var fdAccount = await _db.FixedDepositAccounts.FindAsync(id);
                if (fdAccount == null || fdAccount.IsDeleted)
                    return NotFound();

                // Verify authorization
                var username = User.Identity?.Name;
                if (fdAccount.UserName != username)
                    return Forbid();

                var (amountReceived, penalty) = await _fdService.WithdrawPrematurelyAsync(id);

                _logger.LogWarning($"Premature withdrawal from FD {id}: Received: {amountReceived:C}, Penalty: {penalty:C}");

                return Ok(new
                {
                    message = "Premature withdrawal processed",
                    amountReceived,
                    penaltyAmount = penalty,
                    fdAccountId = id
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Invalid withdrawal: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Cannot withdraw: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing withdrawal on FD {id}: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred processing the withdrawal." });
            }
        }

        /// <summary>
        /// Processes maturity settlement.
        /// </summary>
        [HttpPost("{id}/process-maturity")]
        public async Task<IActionResult> ProcessMaturity(int id)
        {
            try
            {
                var fdAccount = await _db.FixedDepositAccounts.FindAsync(id);
                if (fdAccount == null || fdAccount.IsDeleted)
                    return NotFound();

                // Staff only can process maturity for others
                var username = User.Identity?.Name;
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                
                if (fdAccount.UserName != username && userRole != "Staff")
                    return Forbid();

                var settlementAmount = await _fdService.ProcessMaturityAsync(id);

                _logger.LogInformation($"FD {id} matured. Settlement: {settlementAmount:C}");

                return Ok(new
                {
                    message = "Maturity processed successfully",
                    settlementAmount,
                    fdAccountId = id
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Invalid maturity processing: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Cannot process maturity: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing maturity on FD {id}: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred processing maturity." });
            }
        }

        /// <summary>
        /// Gets transaction history for an FD account.
        /// </summary>
        [HttpGet("{id}/transactions")]
        public async Task<IActionResult> GetTransactionHistory(int id)
        {
            try
            {
                var fdAccount = await _db.FixedDepositAccounts.FindAsync(id);
                if (fdAccount == null || fdAccount.IsDeleted)
                    return NotFound();

                // Verify authorization
                var username = User.Identity?.Name;
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                
                if (fdAccount.UserName != username && userRole != "Staff")
                    return Forbid();

                var transactions = await _db.FixedDepositTransactions
                    .Where(t => t.FDAccountId == id && !t.IsDeleted)
                    .OrderByDescending(t => t.TransactionDate)
                    .Select(t => new
                    {
                        t.TransactionId,
                        t.TransactionType,
                        t.Description,
                        t.Amount,
                        t.BalanceAfter,
                        t.TransactionDate,
                        t.AccrualPeriod
                    })
                    .ToListAsync();

                return Ok(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving transactions for FD {id}: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred retrieving transactions." });
            }
        }
    }

    // DTOs for FD operations
    public class CreateFDRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string UserName { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(20)]
        public string AccountType { get; set; } = "Regular";

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.Range(10000, 100000000)]
        public decimal PrincipalAmount { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.Range(6, 120)]
        public int TenureMonths { get; set; }
    }
}
