// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using ArcaneVault.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using System.Text.Json;

namespace ArcaneVault.Web.Pages.Marketplace
{
    public class DetailsModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(IHttpClientFactory http, ILogger<DetailsModel> logger)
        {
            _http = http;
            _logger = logger;
        }

        public ListingDetailDto? Listing { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        [BindProperty]
        public string OfferType { get; set; } = "Purchase";

        [BindProperty]
        public decimal? OfferedPrice { get; set; }

        [BindProperty]
        public int QuantityRequested { get; set; } = 1;

        [BindProperty]
        public string? Message { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            await LoadListingAsync(id);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToPage("/Account/Login");

            await LoadListingAsync(id);

            if (Listing == null)
            {
                ErrorMessage = "Listing not found.";
                return Page();
            }

            try
            {
                var client = _http.CreateClient("API");
                client.SetAuthorizationToken(HttpContext.Session);

                var offerData = new
                {
                    offerType = OfferType,
                    offeredPrice = OfferType == "Purchase" ? OfferedPrice : null,
                    tradeItemId = (int?)null, // Could be extended to allow trade item selection
                    quantityRequested = QuantityRequested,
                    message = Message
                };

                var json = JsonSerializer.Serialize(offerData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"api/marketplace/{id}/offers", content);

                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = "Your offer has been submitted successfully!";
                    
                    // Clear form
                    OfferedPrice = null;
                    QuantityRequested = 1;
                    Message = null;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    try
                    {
                        var errorObj = JsonSerializer.Deserialize<ErrorResponse>(errorContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        ErrorMessage = errorObj?.Message ?? "Failed to submit offer.";
                    }
                    catch
                    {
                        ErrorMessage = $"Failed to submit offer: {response.StatusCode}";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error submitting offer: {ex.Message}");
                ErrorMessage = "An error occurred submitting your offer.";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAddToWishlistAsync(int id, int itemId)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToPage("/Account/Login");

            try
            {
                var client = _http.CreateClient("API");
                client.SetAuthorizationToken(HttpContext.Session);

                var wishlistData = new { itemId = itemId };
                var json = JsonSerializer.Serialize(wishlistData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("api/wishlist", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Item added to your wishlist!";
                    return RedirectToPage("/Wishlist/Index");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    try
                    {
                        var errorObj = JsonSerializer.Deserialize<ErrorResponse>(errorContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        ErrorMessage = errorObj?.Message ?? "Failed to add to wishlist.";
                    }
                    catch
                    {
                        ErrorMessage = $"Failed to add to wishlist: {response.StatusCode}";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding to wishlist: {ex.Message}");
                ErrorMessage = "An error occurred adding to wishlist.";
            }

            await LoadListingAsync(id);
            return Page();
        }

        private async Task LoadListingAsync(int id)
        {
            try
            {
                var client = _http.CreateClient("API");
                client.SetAuthorizationToken(HttpContext.Session);

                var response = await client.GetAsync($"api/marketplace/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    Listing = JsonSerializer.Deserialize<ListingDetailDto>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading listing: {ex.Message}");
            }
        }

        public class ListingDetailDto
        {
            public int ListingId { get; set; }
            public int ItemId { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public decimal? AskingPrice { get; set; }
            public string ListingType { get; set; } = string.Empty;
            public string? TradePreferences { get; set; }
            public int QuantityAvailable { get; set; }
            public string Status { get; set; } = string.Empty;
            public DateTime ListedDate { get; set; }
            public DateTime? ExpirationDate { get; set; }
            public int ViewCount { get; set; }
            public bool IsFeatured { get; set; }
            public string SellerUserName { get; set; } = string.Empty;
            public ItemDto Item { get; set; } = new();
            public int PendingOfferCount { get; set; }
        }

        public class ItemDto
        {
            public int ItemId { get; set; }
            public string ItemName { get; set; } = string.Empty;
            public int StartingQuantity { get; set; }
            public int CurrentQuantity { get; set; }
            public List<CategoryDto>? Categories { get; set; }
        }

        public class CategoryDto
        {
            public string CategoryCode { get; set; } = string.Empty;
            public string CategoryName { get; set; } = string.Empty;
        }

        public class ErrorResponse
        {
            public string Message { get; set; } = string.Empty;
        }
    }
}
