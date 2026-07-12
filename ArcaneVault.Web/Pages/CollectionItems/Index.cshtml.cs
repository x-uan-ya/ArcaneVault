// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using ArcaneVault.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace ArcaneVault.Web.Pages.CollectionItems
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _http;

        public IndexModel(IHttpClientFactory http) => _http = http;

        public List<ItemDto> Items { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToPage("/Account/Login");

            var client = _http.CreateClient("API");
            client.SetAuthorizationToken(HttpContext.Session); // Add JWT token
            
            string userName = SessionHelper.GetUserName(HttpContext.Session)!;

            // Since API now automatically filters by authenticated user, just call the endpoint
            string url = "api/collectionitems";

            // Append search term if provided
            if (!string.IsNullOrWhiteSpace(Search))
                url += $"?search={Uri.EscapeDataString(Search)}";

            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                string raw = await response.Content.ReadAsStringAsync();
                Items = JsonSerializer.Deserialize<List<ItemDto>>(raw,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new();
            }

            return Page();
        }

        public class ItemDto
        {
            public int    ItemId          { get; set; }
            public string ItemName        { get; set; } = string.Empty;
            public int    StartingQuantity { get; set; }
            public int    CurrentQuantity  { get; set; }
            public string UserName        { get; set; } = string.Empty;
            public List<CategoryRef> Categories { get; set; } = new();
        }

        public class CategoryRef
        {
            public string CategoryCode { get; set; } = string.Empty;
            public string CategoryName { get; set; } = string.Empty;
        }
    }
}
