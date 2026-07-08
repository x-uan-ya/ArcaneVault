// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using ArcaneVault.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace ArcaneVault.Web.Pages.Categories
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        public List<CategoryDto> Categories { get; set; } = new();

        public IndexModel(IHttpClientFactory http) => _http = http;

        public async Task<IActionResult> OnGetAsync()
        {
            // Only Staff can access this page
            if (!SessionHelper.IsStaff(HttpContext.Session))
                return RedirectToPage("/Account/Login");

            var client = _http.CreateClient("API");
            var response = await client.GetAsync("api/categories");

            if (response.IsSuccessStatusCode)
            {
                string raw = await response.Content.ReadAsStringAsync();
                Categories = JsonSerializer.Deserialize<List<CategoryDto>>(raw,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new();
            }

            return Page();
        }

        public class CategoryDto
        {
            public string CategoryCode { get; set; } = string.Empty;
            public string CategoryName { get; set; } = string.Empty;
        }
    }
}
