// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using ArcaneVault.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;
using System.Text.Json;

namespace ArcaneVault.Web.Pages.Categories
{
    public class CreateModel : PageModel
    {
        private readonly IHttpClientFactory _http;

        public CreateModel(IHttpClientFactory http) => _http = http;

        [BindProperty]
        public CategoryInputModel Input { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public IActionResult OnGet()
        {
            if (!SessionHelper.IsStaff(HttpContext.Session))
                return RedirectToPage("/Account/Login");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!SessionHelper.IsStaff(HttpContext.Session))
                return RedirectToPage("/Account/Login");

            if (!ModelState.IsValid) return Page();

            var client = _http.CreateClient("API");

            var body = new StringContent(
                JsonSerializer.Serialize(new
                {
                    categoryCode = Input.CategoryCode,
                    categoryName = Input.CategoryName
                }),
                Encoding.UTF8, "application/json");

            var response = await client.PostAsync("api/categories", body);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = $"Category '{Input.CategoryCode}' created.";
                return RedirectToPage("Index");
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
                ErrorMessage = "A category with this code already exists.";
            else
                ErrorMessage = "Failed to create category.";

            return Page();
        }

        public class CategoryInputModel
        {
            [Required(ErrorMessage = "Category code is required.")]
            [StringLength(20, ErrorMessage = "Max 20 characters.")]
            [Display(Name = "Category Code")]
            public string CategoryCode { get; set; } = string.Empty;

            [Required(ErrorMessage = "Category name is required.")]
            [StringLength(100, ErrorMessage = "Max 100 characters.")]
            [Display(Name = "Category Name")]
            public string CategoryName { get; set; } = string.Empty;
        }
    }
}
