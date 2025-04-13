using EMGAS.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EMGAS.Pages;

[AllowAnonymous]
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly EMGContext _context;

    public IndexModel(ILogger<IndexModel> logger, EMGContext context)
    {
        _logger = logger;
        _context = context;
    }

    public List<Car> FeaturedCars { get; set; } = new List<Car>();

    public async Task OnGetAsync()
    {
        // Get the 3 most recent cars that are for sale
        FeaturedCars = await _context.Cars
            .Where(c => c.Status == CarStatus.ForSale)
            .Include(c => c.Images)
            .OrderByDescending(c => c.CreatedAt)
            .Take(3)
            .ToListAsync();
    }
}
