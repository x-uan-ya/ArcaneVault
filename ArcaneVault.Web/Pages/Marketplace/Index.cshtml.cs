// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using ArcaneVault.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace ArcaneVault.Web.Pages.Marketplace
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

        public List<ListingDto> Listings { get; set; } = new();
        public string? SearchTerm { get; set; }
        public string? ListingType { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        public async Task<IActionResult> OnGetAsync(string? search, string? listingType, decimal? minPrice, decimal? maxPrice)
        {
            SearchTerm = search;
            ListingType = listingType;
            MinPrice = minPrice;
            MaxPrice = maxPrice;

            try
            {
                var client = _http.CreateClient("API");
                client.SetAuthorizationToken(HttpContext.Session);

                // Build query string
                var queryParams = new List<string>();
                if (!string.IsNullOrWhiteSpace(search))
                    queryParams.Add($"search={Uri.EscapeDataString(search)}");
                if (!string.IsNullOrWhiteSpace(listingType))
                    queryParams.Add($"listingType={listingType}");
                if (minPrice.HasValue)
                    queryParams.Add($"minPrice={minPrice}");
                if (maxPrice.HasValue)
                    queryParams.Add($"maxPrice={maxPrice}");

                var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
                var response = await client.GetAsync($"api/marketplace{queryString}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    Listings = JsonSerializer.Deserialize<List<ListingDto>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }
                else
                {
                    _logger.LogWarning($"Failed to fetch marketplace listings: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching marketplace: {ex.Message}");
            }

            return Page();
        }

        public class ListingDto
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
            public string ItemName { get; set; } = string.Empty;
            public List<CategoryDto>? Categories { get; set; }
            public int OfferCount { get; set; }
        }

        public class CategoryDto
        {
            public string CategoryCode { get; set; } = string.Empty;
            public string CategoryName { get; set; } = string.Empty;
        }
    }
}
