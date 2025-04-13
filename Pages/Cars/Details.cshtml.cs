using EMGAS.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace EMGAS.Pages.Cars
{
    // No authorization required for viewing car details
    [AllowAnonymous]
    public class DetailsModel : PageModel
    {
        private readonly EMGContext _context;

        public DetailsModel(EMGContext context)
        {
            _context = context;
        }

        public Car Car { get; set; }
        public bool IsInManager { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id, bool? manager)
        {
            if (id == null)
            {
                return NotFound();
            }

            Car = await _context.Cars
                .Include(c => c.Images)
                .Include(c => c.RepairDetails)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (Car == null)
            {
                return NotFound();
            }

            // Pour déterminer si on affiche la page en mode gestion ou client
            IsInManager = manager ?? false;

            return Page();
        }
    }
} 