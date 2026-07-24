// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using ArcaneVault.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace ArcaneVault.Web.Pages.Marketplace
{
    public class MyOffersModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        private readonly ILogger<MyOffersModel> _logger;

        public MyOffersModel(IHttpClientFactory http, ILogger<MyOffersModel> logger)
        {
            _http = http;
            _logger = logger;
        }

        public List<OfferDto> MyOffers { get; set; } = new();
        public decimal WalletBalance { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToPage("/Account/Login");

            await LoadMyOffersAsync();
            await LoadWalletBalanceAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostWithdrawOfferAsync(int offerId)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToPage("/Account/Login");

            try
            {
                var client = _http.CreateClient("API");
                client.SetAuthorizationToken(HttpContext.Session);

                var response = await client.DeleteAsync($"api/marketplace/offers/{offerId}");

                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = "Offer withdrawn successfully.";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    try
                    {
                        var errorObj = JsonSerializer.Deserialize<ErrorResponse>(errorContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        ErrorMessage = errorObj?.Message ?? "Failed to withdraw offer.";
                    }
                    catch
                    {
                        ErrorMessage = $"Failed to withdraw offer: {response.StatusCode}";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error withdrawing offer: {ex.Message}");
                ErrorMessage = "An error occurred withdrawing the offer.";
            }

            await LoadMyOffersAsync();
            await LoadWalletBalanceAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostConfirmPaymentAsync(int offerId)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToPage("/Account/Login");

            try
            {
                var client = _http.CreateClient("API");
                client.SetAuthorizationToken(HttpContext.Session);

                var response = await client.PostAsync($"api/marketplace/offers/{offerId}/confirm-payment", null);

                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = "Payment confirmed! The item has been added to your collection.";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    try
                    {
                        var errorObj = JsonSerializer.Deserialize<PaymentErrorResponse>(errorContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (errorObj?.Cancelled == true)
                            ErrorMessage = "The payment window has expired and this transaction has been cancelled.";
                        else if (errorObj?.InsufficientFunds == true)
                            ErrorMessage = $"Still not enough funds. You need ${errorObj.Required:F2} but have ${errorObj.Available:F2}. Please <a href='/Wallet/Index'>top up ${errorObj.Shortfall:F2} more</a>.";
                        else
                            ErrorMessage = errorObj?.Message ?? "Payment confirmation failed.";
                    }
                    catch
                    {
                        ErrorMessage = "Payment confirmation failed. Please try again.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error confirming payment: {ex.Message}");
                ErrorMessage = "An error occurred processing your payment.";
            }

            await LoadMyOffersAsync();
            await LoadWalletBalanceAsync();
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
                    var result = System.Text.Json.JsonSerializer.Deserialize<WalletBalanceDto>(json,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    WalletBalance = result?.WalletBalance ?? 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading wallet: {ex.Message}");
            }
        }

        private async Task LoadMyOffersAsync()
        {
            try
            {
                var client = _http.CreateClient("API");
                client.SetAuthorizationToken(HttpContext.Session);

                var response = await client.GetAsync("api/marketplace/my-offers");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    MyOffers = JsonSerializer.Deserialize<List<OfferDto>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading offers: {ex.Message}");
            }
        }

        public class OfferDto
        {
            public int OfferId { get; set; }
            public int ListingId { get; set; }
            public string ListingTitle { get; set; } = string.Empty;
            public string OfferType { get; set; } = string.Empty;
            public decimal? OfferedPrice { get; set; }
            public int QuantityRequested { get; set; }
            public string Status { get; set; } = string.Empty;
            public DateTime OfferedDate { get; set; }
            public DateTime? ResponseDate { get; set; }
            public DateTime? PaymentDeadline { get; set; }
            public string SellerUserName { get; set; } = string.Empty;
        }

        public class ErrorResponse
        {
            public string Message { get; set; } = string.Empty;
        }

        public class PaymentErrorResponse
        {
            public string Message { get; set; } = string.Empty;
            public bool InsufficientFunds { get; set; }
            public bool Cancelled { get; set; }
            public decimal Required { get; set; }
            public decimal Available { get; set; }
            public decimal Shortfall { get; set; }
            public DateTime? PaymentDeadline { get; set; }
        }

        public class WalletBalanceDto
        {
            public decimal WalletBalance { get; set; }
        }
    }
}
