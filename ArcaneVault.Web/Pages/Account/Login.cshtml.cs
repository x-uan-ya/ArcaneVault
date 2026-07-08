// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using ArcaneVault.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;

namespace ArcaneVault.Web.Pages.Account
{
    // Page model for the login page. Handles GET and POST requests for user sign-in.
    public class LoginModel : PageModel
    {
        // IHttpClientFactory is injected to create named HttpClient instances.
        private readonly IHttpClientFactory _http;

        // Constructor receives DI services. Keep dependencies minimal for testability.
        public LoginModel(IHttpClientFactory http) => _http = http;

        // Bound input model holds the user-submitted username and password.
        [BindProperty]
        public LoginInputModel Input { get; set; } = new();

        // Messages shown on the page (not persisted across requests by default).
        public string? ErrorMessage   { get; set; }
        public string? SuccessMessage { get; set; }

        // Handle GET: if already logged in, redirect to Index; otherwise display page.
        public IActionResult OnGet(string? message)
        {
            if (SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToPage("/Index");

            SuccessMessage = message;
            return Page();
        }

        // Handle POST: attempt to authenticate against the API.
        public async Task<IActionResult> OnPostAsync()
        {
            // Validate posted form values. If invalid, re-render page with validation messages.
            if (!ModelState.IsValid) return Page();

            // Create the named HttpClient configured in Program.cs ("API").
            var client = _http.CreateClient("API");

            // Prepare request body as JSON with the username and password.
            var body = new StringContent(
                JsonSerializer.Serialize(new
                {
                    userName = Input.UserName,
                    password = Input.Password
                }),
                Encoding.UTF8, "application/json");

            // Call the API endpoint. This may throw if the API is not reachable.
            var response = await client.PostAsync("api/users/login", body);

            // If authentication succeeded, deserialize response and store session info.
            if (response.IsSuccessStatusCode)
            {
                string raw = await response.Content.ReadAsStringAsync();
                var user = JsonSerializer.Deserialize<LoginResponse>(raw,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (user != null)
                    SessionHelper.SetUser(HttpContext.Session,
                        user.UserName, user.Email, user.RoleId, user.RoleName, user.Token);

                // Redirect to home page after successful login.
                return RedirectToPage("/Index");
            }

            // Authentication failed: show a generic error message.
            ErrorMessage = "Invalid username or password.";
            return Page();
        }

        // Input model used for data binding on the login form.
        public class LoginInputModel
        {
            [Required(ErrorMessage = "Username is required.")]
            [Display(Name = "Username")]
            public string UserName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Password is required.")]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = string.Empty;
        }

        // Internal DTO representing the API's login response.
        private class LoginResponse
        {
            public string UserName { get; set; } = string.Empty;
            public string Email    { get; set; } = string.Empty;
            public int    RoleId   { get; set; }
            public string RoleName { get; set; } = string.Empty;
            public string Token    { get; set; } = string.Empty;
        }
    }
}
