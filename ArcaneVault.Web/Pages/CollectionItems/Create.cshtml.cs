// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using ArcaneVault.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;

namespace ArcaneVault.Web.Pages.CollectionItems
{
    public class CreateModel : PageModel
    {
        private readonly IHttpClientFactory _http;

        public CreateModel(IHttpClientFactory http) => _http = http;

        [BindProperty]
        public ItemInputModel Input { get; set; } = new();

        public List<CategoryDto> AllCategories { get; set; } = new();
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToPage("/Account/Login");

            await LoadCategoriesAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
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
                    itemName         = Input.ItemName,
                    startingQuantity = Input.StartingQuantity,
                    currentQuantity  = Input.CurrentQuantity,
                    userName         = SessionHelper.GetUserName(HttpContext.Session),
                    categoryCodes    = Input.SelectedCategoryCodes
                }),
                Encoding.UTF8, "application/json");

            var response = await client.PostAsync("api/collectionitems", body);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Item added to your collection.";
                return RedirectToPage("Index");
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            ErrorMessage = $"Failed to add item. Status: {response.StatusCode}. Details: {errorContent}";
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

        public class ItemInputModel
        {
            [Required(ErrorMessage = "Item name is required.")]
            [StringLength(200)]
            [Display(Name = "Item Name")]
            public string ItemName { get; set; } = string.Empty;

            [Range(0, int.MaxValue, ErrorMessage = "Must be 0 or more.")]
            [Display(Name = "Starting Quantity")]
            public int StartingQuantity { get; set; }

            [Range(0, int.MaxValue, ErrorMessage = "Must be 0 or more.")]
            [Display(Name = "Current Quantity")]
            public int CurrentQuantity { get; set; }

            public List<string> SelectedCategoryCodes { get; set; } = new();
        }

        public class CategoryDto
        {
            public string CategoryCode { get; set; } = string.Empty;
            public string CategoryName { get; set; } = string.Empty;
        }
    }
}
