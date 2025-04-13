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
    [Authorize(Roles = "Administrator")]
    public class SalesModel : PageModel
    {
        private readonly EMGContext _context;

        public SalesModel(EMGContext context)
        {
            _context = context;
        }

        public List<Car> RecentSales { get; set; }
        public int TotalSoldCars { get; set; }
        public decimal TotalSalesValue { get; set; }
        public decimal AverageSalePrice { get; set; }
        public decimal TotalProfit { get; set; }
        public Dictionary<string, int> SalesByMonth { get; set; }
        public Dictionary<string, decimal> ProfitByMonth { get; set; }
        public Dictionary<string, int> SalesByMake { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        public async Task OnGetAsync()
        {
            // Valeurs par défaut: 12 derniers mois
            if (!StartDate.HasValue)
            {
                StartDate = DateTime.Now.AddMonths(-12).Date;
            }

            if (!EndDate.HasValue)
            {
                EndDate = DateTime.Now.Date;
            }

            var soldCarsQuery = _context.Cars
                .Where(c => c.Status == CarStatus.Sold)
                .Where(c => c.SaleDate >= StartDate && c.SaleDate <= EndDate)
                .Include(c => c.Images);

            RecentSales = await soldCarsQuery
                .OrderByDescending(c => c.SaleDate)
                .ToListAsync();

            TotalSoldCars = RecentSales.Count;

            TotalSalesValue = RecentSales.Sum(c => c.SellingPrice ?? 0);

            AverageSalePrice = TotalSoldCars > 0 
                ? TotalSalesValue / TotalSoldCars 
                : 0;

            TotalProfit = RecentSales.Sum(c => (c.SellingPrice ?? 0) - c.PurchasePrice);

            // Ventes par mois
            SalesByMonth = new Dictionary<string, int>();
            ProfitByMonth = new Dictionary<string, decimal>();

            var startMonth = new DateTime(StartDate.Value.Year, StartDate.Value.Month, 1);
            var endMonth = new DateTime(EndDate.Value.Year, EndDate.Value.Month, 1);

            for (var month = startMonth; month <= endMonth; month = month.AddMonths(1))
            {
                var monthName = month.ToString("MMM yyyy");
                var nextMonth = month.AddMonths(1);
                
                var salesInMonth = RecentSales.Count(c => c.SaleDate >= month && c.SaleDate < nextMonth);
                SalesByMonth[monthName] = salesInMonth;
                
                var profitInMonth = RecentSales
                    .Where(c => c.SaleDate >= month && c.SaleDate < nextMonth)
                    .Sum(c => (c.SellingPrice ?? 0) - c.PurchasePrice);
                
                ProfitByMonth[monthName] = profitInMonth;
            }

            // Ventes par marque
            SalesByMake = RecentSales
                .GroupBy(c => c.Make)
                .ToDictionary(g => g.Key, g => g.Count());
        }
    }
}
