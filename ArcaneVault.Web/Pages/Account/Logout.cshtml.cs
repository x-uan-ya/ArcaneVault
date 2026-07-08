// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using ArcaneVault.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ArcaneVault.Web.Pages.Account
{
    public class LogoutModel : PageModel
    {
        public IActionResult OnGet()
        {
            // Clear all session data and redirect to login
            SessionHelper.Clear(HttpContext.Session);
            HttpContext.Session.Clear();
            return RedirectToPage("/Account/Login");
        }
    }
}
