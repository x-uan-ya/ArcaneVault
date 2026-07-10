// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using ArcaneVault.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace ArcaneVault.Web.Pages.Categories
{
    public class DeleteModel : PageModel
    {
        private readonly IHttpClientFactory _http;

        public DeleteModel(IHttpClientFactory http) => _http = http;

        public CategoryDto? Category { get; set; }

        public async Task<IActionResult> OnGetAsync(string code)
        {
            if (!SessionHelper.IsStaff(HttpContext.Session))
                return RedirectToPage("/Account/Login");

            var client = _http.CreateClient("API");
            var response = await client.GetAsync($"api/categories/{Uri.EscapeDataString(code)}");

            if (response.IsSuccessStatusCode)
            {
                string raw = await response.Content.ReadAsStringAsync();
                Category = JsonSerializer.Deserialize<CategoryDto>(raw,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string code)
        {
            if (!SessionHelper.IsStaff(HttpContext.Session))
                return RedirectToPage("/Account/Login");

            var client = _http.CreateClient("API");
            client.SetAuthorizationToken(HttpContext.Session);
            var response = await client.DeleteAsync($"api/categories/{Uri.EscapeDataString(code)}");

            TempData["Success"] = response.IsSuccessStatusCode
                ? "Category deleted."
                : "Failed to delete category.";

            return RedirectToPage("Index");
        }

        public class CategoryDto
        {
            public string CategoryCode { get; set; } = string.Empty;
            public string CategoryName { get; set; } = string.Empty;
        }
    }
}
