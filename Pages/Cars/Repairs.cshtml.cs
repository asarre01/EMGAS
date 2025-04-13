using EMGAS.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace EMGAS.Pages.Cars
{
    [Authorize(Policy = "AdminOnly")]
    public class RepairsModel : PageModel
    {
        private readonly EMGContext _context;

        public RepairsModel(EMGContext context)
        {
            _context = context;
        }

        [BindProperty]
        public RepairDetail RepairDetail { get; set; }

        public Car Car { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Car = await _context.Cars
                .Include(c => c.RepairDetails)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (Car == null)
            {
                return NotFound();
            }

            // Initialiser un nouvel objet de réparation
            RepairDetail = new RepairDetail 
            { 
                CarId = Car.Id,
                RepairDate = DateTime.Today
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadCarAsync(RepairDetail.CarId);
                return Page();
            }

            _context.RepairDetails.Add(RepairDetail);
            await _context.SaveChangesAsync();

            // Mettre à jour le statut de la voiture si nécessaire
            var car = await _context.Cars.FindAsync(RepairDetail.CarId);
            if (car != null && car.Status != CarStatus.UnderRepair)
            {
                car.Status = CarStatus.UnderRepair;
                car.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Repairs", new { id = RepairDetail.CarId });
        }

        public async Task<IActionResult> OnPostCompleteRepairsAsync(int id)
        {
            var car = await _context.Cars.FindAsync(id);
            
            if (car == null)
            {
                return NotFound();
            }

            car.Status = CarStatus.ForSale;
            car.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            return RedirectToPage("./Details", new { id = id });
        }

        public async Task<IActionResult> OnPostDeleteRepairAsync(int repairId, int carId)
        {
            var repair = await _context.RepairDetails.FindAsync(repairId);
            
            if (repair == null)
            {
                return NotFound();
            }

            _context.RepairDetails.Remove(repair);
            await _context.SaveChangesAsync();
            
            return RedirectToPage("./Repairs", new { id = carId });
        }

        private async Task LoadCarAsync(int carId)
        {
            Car = await _context.Cars
                .Include(c => c.RepairDetails)
                .FirstOrDefaultAsync(c => c.Id == carId);
        }
    }
} 