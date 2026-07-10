// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using ArcaneVault.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;

namespace ArcaneVault.Web.Pages.Categories
{
    public class EditModel : PageModel
    {
        private readonly IHttpClientFactory _http;

        public EditModel(IHttpClientFactory http) => _http = http;

        [BindProperty]
        public EditInputModel Input { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string code)
        {
            if (!SessionHelper.IsStaff(HttpContext.Session))
                return RedirectToPage("/Account/Login");

            var client = _http.CreateClient("API");
            var response = await client.GetAsync($"api/categories/{Uri.EscapeDataString(code)}");

            if (!response.IsSuccessStatusCode)
                return RedirectToPage("Index");

            string raw = await response.Content.ReadAsStringAsync();
            var cat = JsonSerializer.Deserialize<CategoryDto>(raw,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (cat == null) return RedirectToPage("Index");

            Input = new EditInputModel
            {
                CategoryCode = cat.CategoryCode,
                CategoryName = cat.CategoryName
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string code)
        {
            if (!SessionHelper.IsStaff(HttpContext.Session))
                return RedirectToPage("/Account/Login");

            if (!ModelState.IsValid) return Page();

            var client = _http.CreateClient("API");
            client.SetAuthorizationToken(HttpContext.Session);

            var body = new StringContent(
                JsonSerializer.Serialize(new { categoryName = Input.CategoryName }),
                Encoding.UTF8, "application/json");

            var response = await client.PutAsync(
                $"api/categories/{Uri.EscapeDataString(code)}", body);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Category updated.";
                return RedirectToPage("Index");
            }

            ErrorMessage = "Failed to update category.";
            return Page();
        }

        public class EditInputModel
        {
            public string CategoryCode { get; set; } = string.Empty;

            [Required(ErrorMessage = "Category name is required.")]
            [StringLength(100)]
            [Display(Name = "Category Name")]
            public string CategoryName { get; set; } = string.Empty;
        }

        private class CategoryDto
        {
            public string CategoryCode { get; set; } = string.Empty;
            public string CategoryName { get; set; } = string.Empty;
        }
    }
}
