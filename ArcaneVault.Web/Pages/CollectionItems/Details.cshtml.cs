// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using ArcaneVault.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace ArcaneVault.Web.Pages.CollectionItems
{
    public class DetailsModel : PageModel
    {
        private readonly IHttpClientFactory _http;

        public DetailsModel(IHttpClientFactory http) => _http = http;

        public ItemDto? Item { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToPage("/Account/Login");

            var client = _http.CreateClient("API");
            var response = await client.GetAsync($"api/collectionitems/{id}");

            if (response.IsSuccessStatusCode)
            {
                string raw = await response.Content.ReadAsStringAsync();
                Item = JsonSerializer.Deserialize<ItemDto>(raw,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            return Page();
        }

        public class ItemDto
        {
            public int    ItemId           { get; set; }
            public string ItemName         { get; set; } = string.Empty;
            public int    StartingQuantity { get; set; }
            public int    CurrentQuantity  { get; set; }
            public string UserName         { get; set; } = string.Empty;
            public List<CategoryRef> Categories { get; set; } = new();
        }

        public class CategoryRef
        {
            public string CategoryCode { get; set; } = string.Empty;
            public string CategoryName { get; set; } = string.Empty;
        }
    }
}
