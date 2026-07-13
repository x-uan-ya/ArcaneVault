// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using ArcaneVault.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;

namespace ArcaneVault.Web.Pages.Account
{
    public class ChangePasswordModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        private readonly ILogger<ChangePasswordModel> _logger;

        public ChangePasswordModel(IHttpClientFactory http, ILogger<ChangePasswordModel> logger)
        {
            _http = http;
            _logger = logger;
        }

        [BindProperty]
        public string CurrentPassword { get; set; } = string.Empty;

        [BindProperty]
        public string NewPassword { get; set; } = string.Empty;

        [BindProperty]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public IActionResult OnGet()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToPage("/Account/Login");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToPage("/Account/Login");

            // Validate new password and confirm match
            if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 6)
            {
                ErrorMessage = "New password must be at least 6 characters.";
                return Page();
            }

            if (NewPassword != ConfirmPassword)
            {
                ErrorMessage = "New password and confirm password do not match.";
                return Page();
            }

            if (NewPassword == CurrentPassword)
            {
                ErrorMessage = "New password must be different from your current password.";
                return Page();
            }

            try
            {
                var client = _http.CreateClient("API");
                client.SetAuthorizationToken(HttpContext.Session);

                var body = new StringContent(
                    JsonSerializer.Serialize(new
                    {
                        currentPassword = CurrentPassword,
                        newPassword = NewPassword
                    }),
                    Encoding.UTF8, "application/json");

                var response = await client.PutAsync("api/users/me/password", body);

                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = "Password changed successfully!";
                    // Clear fields
                    CurrentPassword = string.Empty;
                    NewPassword = string.Empty;
                    ConfirmPassword = string.Empty;
                    return Page();
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                try
                {
                    var errorObj = JsonSerializer.Deserialize<ErrorResponse>(errorContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    ErrorMessage = errorObj?.Message ?? "Failed to change password.";
                }
                catch
                {
                    ErrorMessage = "Failed to change password. Please try again.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error changing password: {ex.Message}");
                ErrorMessage = "An error occurred. Please try again.";
            }

            return Page();
        }

        private class ErrorResponse
        {
            public string Message { get; set; } = string.Empty;
        }
    }
}
