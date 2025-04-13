using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace EMGAS.Pages.Account
{
    public class LogoutModel : PageModel
    {
        public LogoutModel()
        {
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Supprimer le cookie d'authentification
            Response.Cookies.Delete("AuthToken");
            
            return RedirectToPage("/Index");
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = "")
        {
            // Supprimer le cookie d'authentification
            Response.Cookies.Delete("AuthToken");
            
            returnUrl ??= Url.Content("~/");
            return LocalRedirect(returnUrl);
        }
    }
} 