using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EMGAS.Data;
using Microsoft.AspNetCore.Authorization;

namespace EMGAS.Pages.Reports
{
    [Authorize(Roles = "Administrator,Manager")]
    public class InventoryModel : PageModel
    {
        private readonly EMGContext _context;

        public InventoryModel(EMGContext context)
        {
            _context = context;
        }

        public List<Car> ForSaleCars { get; set; }
        public List<Car> UnderRepairCars { get; set; }
        public List<Car> SoldCars { get; set; }
        public int TotalCarsCount { get; set; }
        public decimal TotalInventoryValue { get; set; }
        public decimal AverageCarPrice { get; set; }
        public Dictionary<string, int> CarsByMake { get; set; }
        public Dictionary<string, int> CarsByColor { get; set; }

        public async Task OnGetAsync()
        {
            ForSaleCars = await _context.Cars
                .Where(c => c.Status == CarStatus.ForSale)
                .Include(c => c.Images)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            UnderRepairCars = await _context.Cars
                .Where(c => c.Status == CarStatus.UnderRepair)
                .Include(c => c.Images)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            SoldCars = await _context.Cars
                .Where(c => c.Status == CarStatus.Sold)
                .Include(c => c.Images)
                .OrderByDescending(c => c.SaleDate)
                .Take(10)
                .ToListAsync();

            TotalCarsCount = await _context.Cars.CountAsync();
            
            TotalInventoryValue = await _context.Cars
                .Where(c => c.Status == CarStatus.ForSale || c.Status == CarStatus.UnderRepair)
                .SumAsync(c => c.SellingPrice ?? 0);

            var carCount = await _context.Cars
                .Where(c => c.Status == CarStatus.ForSale || c.Status == CarStatus.UnderRepair)
                .CountAsync();

            AverageCarPrice = carCount > 0 
                ? await _context.Cars
                    .Where(c => c.Status == CarStatus.ForSale || c.Status == CarStatus.UnderRepair)
                    .AverageAsync(c => c.SellingPrice ?? 0)
                : 0;

            CarsByMake = await _context.Cars
                .GroupBy(c => c.Make)
                .Select(g => new { Make = g.Key, Count = g.Count() })
                .ToDictionaryAsync(k => k.Make, v => v.Count);

            CarsByColor = await _context.Cars
                .GroupBy(c => c.Color)
                .Select(g => new { Color = g.Key, Count = g.Count() })
                .ToDictionaryAsync(k => k.Color, v => v.Count);
        }
    }
}
