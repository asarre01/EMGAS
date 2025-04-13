using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace EMGAS.Pages.Account
{
    [Authorize]  // Ensure only authenticated users can access logout
    public class LogoutModel : PageModel
    {
        public LogoutModel()
        {
        }

        public IActionResult OnGetAsync()
        {
            // Supprimer le cookie d'authentification JWT avec les bonnes options
            Response.Cookies.Delete("AuthToken", new Microsoft.AspNetCore.Http.CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict,
                Path = "/"
            });
            
            return RedirectToPage("/Index");
        }

        public IActionResult OnPostAsync(string returnUrl = "")
        {
            // Supprimer le cookie d'authentification JWT avec les bonnes options
            Response.Cookies.Delete("AuthToken", new Microsoft.AspNetCore.Http.CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict,
                Path = "/"
            });
            
            returnUrl ??= Url.Content("~/");
            return LocalRedirect(returnUrl);
        }
    }
} 