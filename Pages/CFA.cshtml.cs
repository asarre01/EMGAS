using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace EMGAS.Pages
{
    [AllowAnonymous]
    public class CFAModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}