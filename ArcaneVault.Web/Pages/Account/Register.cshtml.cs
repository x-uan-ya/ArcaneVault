// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using ArcaneVault.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;
using System.Text.Json;

namespace ArcaneVault.Web.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly IHttpClientFactory _http;

        public RegisterModel(IHttpClientFactory http) => _http = http;

        [BindProperty]
        public RegisterInputModel Input { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public IActionResult OnGet()
        {
            // Redirect if already logged in
            if (SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToPage("/Index");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var client = _http.CreateClient("API");

            var body = new StringContent(
                JsonSerializer.Serialize(new
                {
                    userName = Input.UserName,
                    email    = Input.Email,
                    password = Input.Password
                }),
                Encoding.UTF8, "application/json");

            var response = await client.PostAsync("api/users/register", body);

            if (response.IsSuccessStatusCode)
                return RedirectToPage("/Account/Login",
                    new { message = "Account created! Please log in." });

            // Handle API error messages
            string raw = await response.Content.ReadAsStringAsync();
            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                var err = JsonSerializer.Deserialize<ApiError>(raw,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                ErrorMessage = err?.Message ?? "Registration failed.";
            }
            else
            {
                ErrorMessage = "Registration failed. Please try again.";
            }

            return Page();
        }

        public class RegisterInputModel
        {
            [Required(ErrorMessage = "Username is required.")]
            [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be 3–50 characters.")]
            [Display(Name = "Username")]
            public string UserName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Email is required.")]
            [EmailAddress(ErrorMessage = "Enter a valid email address.")]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Password is required.")]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = string.Empty;
        }

        private class ApiError { public string? Message { get; set; } }
    }
}
