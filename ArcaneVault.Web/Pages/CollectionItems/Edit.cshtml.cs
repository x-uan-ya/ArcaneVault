// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using ArcaneVault.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;

namespace ArcaneVault.Web.Pages.CollectionItems
{
    public class EditModel : PageModel
    {
        private readonly IHttpClientFactory _http;

        public EditModel(IHttpClientFactory http) => _http = http;

        [BindProperty]
        public EditInputModel Input { get; set; } = new();

        public List<CategoryDto> AllCategories { get; set; } = new();
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToPage("/Account/Login");

            var client = _http.CreateClient("API");
            var response = await client.GetAsync($"api/collectionitems/{id}");

            if (!response.IsSuccessStatusCode) return RedirectToPage("Index");

            string raw = await response.Content.ReadAsStringAsync();
            var item = JsonSerializer.Deserialize<ItemDto>(raw,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (item == null) return RedirectToPage("Index");

            Input = new EditInputModel
            {
                ItemName            = item.ItemName,
                CurrentQuantity     = item.CurrentQuantity,
                SelectedCategoryCodes = item.Categories.Select(c => c.CategoryCode).ToList()
            };

            await LoadCategoriesAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToPage("/Account/Login");

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync();
                return Page();
            }

            var client = _http.CreateClient("API");
            client.SetAuthorizationToken(HttpContext.Session);

            var body = new StringContent(
                JsonSerializer.Serialize(new
                {
                    itemName        = Input.ItemName,
                    currentQuantity = Input.CurrentQuantity,
                    categoryCodes   = Input.SelectedCategoryCodes
                }),
                Encoding.UTF8, "application/json");

            var response = await client.PutAsync($"api/collectionitems/{id}", body);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Item updated.";
                return RedirectToPage("Index");
            }

            ErrorMessage = "Failed to update item.";
            await LoadCategoriesAsync();
            return Page();
        }

        private async Task LoadCategoriesAsync()
        {
            var client = _http.CreateClient("API");
            var response = await client.GetAsync("api/categories");
            if (response.IsSuccessStatusCode)
            {
                string raw = await response.Content.ReadAsStringAsync();
                AllCategories = JsonSerializer.Deserialize<List<CategoryDto>>(raw,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new();
            }
        }

        public class EditInputModel
        {
            [Required(ErrorMessage = "Item name is required.")]
            [StringLength(200)]
            [Display(Name = "Item Name")]
            public string ItemName { get; set; } = string.Empty;

            [Range(0, int.MaxValue, ErrorMessage = "Must be 0 or more.")]
            [Display(Name = "Current Quantity")]
            public int CurrentQuantity { get; set; }

            public List<string> SelectedCategoryCodes { get; set; } = new();
        }

        private class ItemDto
        {
            public string ItemName       { get; set; } = string.Empty;
            public int    CurrentQuantity { get; set; }
            public List<CategoryRef> Categories { get; set; } = new();
        }

        private class CategoryRef
        {
            public string CategoryCode { get; set; } = string.Empty;
        }

        public class CategoryDto
        {
            public string CategoryCode { get; set; } = string.Empty;
            public string CategoryName { get; set; } = string.Empty;
        }
    }
}
