// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using ArcaneVault.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;

namespace ArcaneVault.Web.Pages.Wallet
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IHttpClientFactory http, ILogger<IndexModel> logger)
        {
            _http = http;
            _logger = logger;
        }

        public decimal WalletBalance { get; set; }
        public List<TransactionDto> Transactions { get; set; } = new();
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Amount is required.")]
        [Range(0.01, 99999.99, ErrorMessage = "Amount must be between $0.01 and $99,999.99.")]
        public decimal TopUpAmount { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToPage("/Account/Login");

            if (TempData["InsufficientFunds"] != null)
                ErrorMessage = TempData["InsufficientFunds"]?.ToString();

            if (TempData["SuccessMessage"] != null)
                SuccessMessage = TempData["SuccessMessage"]?.ToString();

            await Task.WhenAll(LoadWalletBalanceAsync(), LoadTransactionsAsync());
            return Page();
        }

        public async Task<IActionResult> OnPostTopUpAsync()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToPage("/Account/Login");

            if (!ModelState.IsValid)
            {
                await Task.WhenAll(LoadWalletBalanceAsync(), LoadTransactionsAsync());
                return Page();
            }

            try
            {
                var client = _http.CreateClient("API");
                client.SetAuthorizationToken(HttpContext.Session);

                var body = JsonSerializer.Serialize(new { amount = TopUpAmount });
                var content = new StringContent(body, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("api/users/me/wallet/topup", content);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<WalletResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    WalletBalance = result?.WalletBalance ?? 0;
                    SuccessMessage = $"Successfully topped up ${TopUpAmount:F2}. New balance: ${WalletBalance:F2}";
                    TopUpAmount = 0;
                    ModelState.Clear();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    try
                    {
                        var errorObj = JsonSerializer.Deserialize<ErrorResponse>(errorContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        ErrorMessage = errorObj?.Message ?? "Top-up failed.";
                    }
                    catch
                    {
                        ErrorMessage = "Top-up failed. Please try again.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error topping up wallet: {ex.Message}");
                ErrorMessage = "An error occurred processing your top-up.";
            }

            await Task.WhenAll(LoadWalletBalanceAsync(), LoadTransactionsAsync());
            return Page();
        }

        private async Task LoadWalletBalanceAsync()
        {
            try
            {
                var client = _http.CreateClient("API");
                client.SetAuthorizationToken(HttpContext.Session);

                var response = await client.GetAsync("api/users/me/wallet");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<WalletResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    WalletBalance = result?.WalletBalance ?? 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading wallet balance: {ex.Message}");
            }
        }

        private async Task LoadTransactionsAsync()
        {
            try
            {
                var client = _http.CreateClient("API");
                client.SetAuthorizationToken(HttpContext.Session);

                var response = await client.GetAsync("api/users/me/wallet/transactions");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    Transactions = JsonSerializer.Deserialize<List<TransactionDto>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading wallet transactions: {ex.Message}");
            }
        }

        public class WalletResponse
        {
            public decimal WalletBalance { get; set; }
            public string? Message { get; set; }
        }

        public class TransactionDto
        {
            public int TransactionId { get; set; }
            public string Type { get; set; } = string.Empty;
            public decimal Amount { get; set; }
            public string Description { get; set; } = string.Empty;
            public decimal BalanceAfter { get; set; }
            public DateTime TransactionDate { get; set; }
        }

        public class ErrorResponse
        {
            public string Message { get; set; } = string.Empty;
        }
    }
}
