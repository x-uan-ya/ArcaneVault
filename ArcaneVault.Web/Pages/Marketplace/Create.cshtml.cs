// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using ArcaneVault.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using System.Text.Json;

namespace ArcaneVault.Web.Pages.Marketplace
{
    public class CreateModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(IHttpClientFactory http, ILogger<CreateModel> logger)
        {
            _http = http;
            _logger = logger;
        }

        public List<CollectionItemDto> MyItems { get; set; } = new();
        public string? ErrorMessage { get; set; }

        [BindProperty]
        public int ItemId { get; set; }

        [BindProperty]
        public string? Title { get; set; }

        [BindProperty]
        public string Description { get; set; } = string.Empty;

        [BindProperty]
        public decimal? AskingPrice { get; set; }

        [BindProperty]
        public string ListingType { get; set; } = "Sale";

        [BindProperty]
        public string? TradePreferences { get; set; }

        [BindProperty]
        public int QuantityAvailable { get; set; } = 1;

        [BindProperty]
        public int? ExpirationDays { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToPage("/Account/Login");

            await LoadUserItemsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToPage("/Account/Login");

            var token = SessionHelper.GetJwtToken(HttpContext.Session);
            var username = SessionHelper.GetUserName(HttpContext.Session);
            _logger.LogInformation($"Creating listing for user: {username}, Has Token: {!string.IsNullOrEmpty(token)}");

            await LoadUserItemsAsync();

            // Validation
            if (ItemId == 0)
            {
                ErrorMessage = "Please select an item to list.";
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Description))
            {
                ErrorMessage = "Description is required.";
                return Page();
            }

            if (ListingType == "Sale" && (!AskingPrice.HasValue || AskingPrice <= 0))
            {
                ErrorMessage = "Asking price is required for sale listings.";
                return Page();
            }

            if (ListingType == "Trade" && string.IsNullOrWhiteSpace(TradePreferences))
            {
                ErrorMessage = "Trade preferences are required for trade listings.";
                return Page();
            }

            if (QuantityAvailable <= 0)
            {
                ErrorMessage = "Quantity must be at least 1.";
                return Page();
            }

            try
            {
                var client = _http.CreateClient("API");
                client.SetAuthorizationToken(HttpContext.Session);

                var listingData = new
                {
                    itemId = ItemId,
                    title = Title,
                    description = Description,
                    askingPrice = AskingPrice,
                    listingType = ListingType,
                    tradePreferences = TradePreferences,
                    quantityAvailable = QuantityAvailable,
                    expirationDays = ExpirationDays
                };

                var json = JsonSerializer.Serialize(listingData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("api/marketplace", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Listing created successfully!";
                    return RedirectToPage("MyListings");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    try
                    {
                        var errorObj = JsonSerializer.Deserialize<ErrorResponse>(errorContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        ErrorMessage = errorObj?.Message ?? "Failed to create listing.";
                    }
                    catch
                    {
                        ErrorMessage = $"Failed to create listing: {response.StatusCode}. Details: {errorContent}";
                    }
                    _logger.LogError($"Failed to create listing. Status: {response.StatusCode}, Content: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating listing: {ex.Message}");
                ErrorMessage = "An error occurred creating your listing.";
            }

            return Page();
        }

        private async Task LoadUserItemsAsync()
        {
            try
            {
                var client = _http.CreateClient("API");
                client.SetAuthorizationToken(HttpContext.Session);

                var username = SessionHelper.GetUserName(HttpContext.Session);
                var token = SessionHelper.GetJwtToken(HttpContext.Session);
                
                _logger.LogInformation($"Loading items for user: {username}, Has Token: {!string.IsNullOrEmpty(token)}");

                // Don't pass username - API will automatically filter by authenticated user
                var response = await client.GetAsync($"api/collectionitems");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var items = JsonSerializer.Deserialize<List<CollectionItemDto>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

                    // Filter items that have available quantity and aren't already listed
                    MyItems = items.Where(i => i.CurrentQuantity > 0).ToList();
                }
                else
                {
                    _logger.LogWarning($"Failed to load items. Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading user items: {ex.Message}");
            }
        }

        public class CollectionItemDto
        {
            public int ItemId { get; set; }
            public string ItemName { get; set; } = string.Empty;
            public int StartingQuantity { get; set; }
            public int CurrentQuantity { get; set; }
            public string UserName { get; set; } = string.Empty;
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
