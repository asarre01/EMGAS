using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace EMGAS.Pages
{
    [AllowAnonymous]
    public class AboutModel : PageModel
    {
        public void OnGet()
        {
        }
    }
} 