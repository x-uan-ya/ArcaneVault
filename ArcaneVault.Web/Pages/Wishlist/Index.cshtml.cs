// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using ArcaneVault.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace ArcaneVault.Web.Pages.Wishlist
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

        public List<WishlistItemDto> WishlistItems { get; set; } = new();
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToPage("/Account/Login");

            if (TempData["SuccessMessage"] != null)
                SuccessMessage = TempData["SuccessMessage"]?.ToString();

            await LoadWishlistAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostRemoveAsync(int wishlistId)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToPage("/Account/Login");

            try
            {
                var client = _http.CreateClient("API");
                client.SetAuthorizationToken(HttpContext.Session);

                var response = await client.DeleteAsync($"api/wishlist/{wishlistId}");

                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = "Item removed from wishlist.";
                }
                else
                {
                    ErrorMessage = "Failed to remove item from wishlist.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error removing from wishlist: {ex.Message}");
                ErrorMessage = "An error occurred removing the item.";
            }

            await LoadWishlistAsync();
            return Page();
        }

        private async Task LoadWishlistAsync()
        {
            try
            {
                var client = _http.CreateClient("API");
                client.SetAuthorizationToken(HttpContext.Session);

                var response = await client.GetAsync("api/wishlist");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    WishlistItems = JsonSerializer.Deserialize<List<WishlistItemDto>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading wishlist: {ex.Message}");
            }
        }

        public class WishlistItemDto
        {
            public int WishlistId { get; set; }
            public int ItemId { get; set; }
            public string ItemName { get; set; } = string.Empty;
            public List<CategoryDto>? Categories { get; set; }
            public DateTime CreatedDate { get; set; }
            public bool IsAvailableNow { get; set; }
            public int? AvailableListingId { get; set; }
            public decimal? AvailablePrice { get; set; }
            public string? AvailableSeller { get; set; }
        }

        public class CategoryDto
        {
            public string CategoryCode { get; set; } = string.Empty;
            public string CategoryName { get; set; } = string.Empty;
        }
    }
}
