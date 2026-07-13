// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using ArcaneVault.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ArcaneVault.Web.Pages.Account
{
    public class DeleteAccountModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        private readonly ILogger<DeleteAccountModel> _logger;

        public DeleteAccountModel(IHttpClientFactory http, ILogger<DeleteAccountModel> logger)
        {
            _http = http;
            _logger = logger;
        }

        public string? ErrorMessage { get; set; }
        public string? UserName { get; set; }

        public IActionResult OnGet()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToPage("/Account/Login");

            UserName = SessionHelper.GetUserName(HttpContext.Session);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToPage("/Account/Login");

            try
            {
                var client = _http.CreateClient("API");
                client.SetAuthorizationToken(HttpContext.Session);

                var response = await client.DeleteAsync("api/users/me");

                if (response.IsSuccessStatusCode)
                {
                    // Clear session and redirect to login
                    SessionHelper.Clear(HttpContext.Session);
                    HttpContext.Session.Clear();
                    return RedirectToPage("/Account/Login",
                        new { message = "Your account has been deleted." });
                }

                ErrorMessage = "Failed to delete account. Please try again.";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting account: {ex.Message}");
                ErrorMessage = "An error occurred. Please try again.";
            }

            UserName = SessionHelper.GetUserName(HttpContext.Session);
            return Page();
        }
    }
}
