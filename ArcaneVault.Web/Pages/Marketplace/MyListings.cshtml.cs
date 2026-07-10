// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using ArcaneVault.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace ArcaneVault.Web.Pages.Marketplace
{
    public class MyListingsModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        private readonly ILogger<MyListingsModel> _logger;

        public MyListingsModel(IHttpClientFactory http, ILogger<MyListingsModel> logger)
        {
            _http = http;
            _logger = logger;
        }

        public List<ListingDto> Listings { get; set; } = new();
        public List<OfferDto> ReceivedOffers { get; set; } = new();
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToPage("/Account/Login");

            // Check for success message from TempData
            if (TempData["SuccessMessage"] != null)
                SuccessMessage = TempData["SuccessMessage"]?.ToString();

            await LoadMyListingsAsync();
            await LoadReceivedOffersAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostCancelListingAsync(int listingId)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToPage("/Account/Login");

            try
            {
                var client = _http.CreateClient("API");
                client.SetAuthorizationToken(HttpContext.Session);

                var response = await client.DeleteAsync($"api/marketplace/{listingId}");

                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = "Listing cancelled successfully.";
                }
                else
                {
                    ErrorMessage = "Failed to cancel listing.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error cancelling listing: {ex.Message}");
                ErrorMessage = "An error occurred cancelling the listing.";
            }

            await LoadMyListingsAsync();
            await LoadReceivedOffersAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAcceptOfferAsync(int offerId, string? response)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToPage("/Account/Login");

            try
            {
                var client = _http.CreateClient("API");
                client.SetAuthorizationToken(HttpContext.Session);

                var offerResponse = new { response = response ?? "Offer accepted" };
                var json = JsonSerializer.Serialize(offerResponse);
                var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var httpResponse = await client.PutAsync($"api/marketplace/offers/{offerId}/accept", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    SuccessMessage = "Offer accepted! Item has been transferred to the buyer.";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    try
                    {
                        var errorObj = JsonSerializer.Deserialize<ErrorResponse>(errorContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        ErrorMessage = errorObj?.Message ?? "Failed to accept offer.";
                    }
                    catch
                    {
                        ErrorMessage = $"Failed to accept offer: {httpResponse.StatusCode}";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error accepting offer: {ex.Message}");
                ErrorMessage = "An error occurred accepting the offer.";
            }

            await LoadMyListingsAsync();
            await LoadReceivedOffersAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostRejectOfferAsync(int offerId, string? response)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToPage("/Account/Login");

            try
            {
                var client = _http.CreateClient("API");
                client.SetAuthorizationToken(HttpContext.Session);

                var offerResponse = new { response = response ?? "Offer rejected" };
                var json = JsonSerializer.Serialize(offerResponse);
                var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var httpResponse = await client.PutAsync($"api/marketplace/offers/{offerId}/reject", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    SuccessMessage = "Offer rejected.";
                }
                else
                {
                    ErrorMessage = "Failed to reject offer.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error rejecting offer: {ex.Message}");
                ErrorMessage = "An error occurred rejecting the offer.";
            }

            await LoadMyListingsAsync();
            await LoadReceivedOffersAsync();

            return Page();
        }

        private async Task LoadMyListingsAsync()
        {
            try
            {
                var client = _http.CreateClient("API");
                client.SetAuthorizationToken(HttpContext.Session);

                var response = await client.GetAsync("api/marketplace/my-listings");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    Listings = JsonSerializer.Deserialize<List<ListingDto>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading listings: {ex.Message}");
            }
        }

        private async Task LoadReceivedOffersAsync()
        {
            try
            {
                var client = _http.CreateClient("API");
                client.SetAuthorizationToken(HttpContext.Session);

                var response = await client.GetAsync("api/marketplace/offers-received");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    ReceivedOffers = JsonSerializer.Deserialize<List<OfferDto>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading offers: {ex.Message}");
            }
        }

        public class ListingDto
        {
            public int ListingId { get; set; }
            public string Title { get; set; } = string.Empty;
            public decimal? AskingPrice { get; set; }
            public string ListingType { get; set; } = string.Empty;
            public int QuantityAvailable { get; set; }
            public string Status { get; set; } = string.Empty;
            public DateTime ListedDate { get; set; }
            public int ViewCount { get; set; }
            public string ItemName { get; set; } = string.Empty;
            public int PendingOffers { get; set; }
        }

        public class OfferDto
        {
            public int OfferId { get; set; }
            public int ListingId { get; set; }
            public string ListingTitle { get; set; } = string.Empty;
            public string BuyerUserName { get; set; } = string.Empty;
            public string OfferType { get; set; } = string.Empty;
            public decimal? OfferedPrice { get; set; }
            public int QuantityRequested { get; set; }
            public string Status { get; set; } = string.Empty;
            public DateTime OfferedDate { get; set; }
            public string? Message { get; set; }
        }

        public class ErrorResponse
        {
            public string Message { get; set; } = string.Empty;
        }
    }
}
