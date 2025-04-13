using EMGAS.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EMGAS.Pages.Cars
{
    public class IndexModel : PageModel
    {
        private readonly EMGContext _context;

        public IndexModel(EMGContext context)
        {
            _context = context;
        }

        public IList<CarViewModel> Cars { get; set; } = new List<CarViewModel>();
        public SelectList Makes { get; set; }
        public SelectList Models { get; set; }
        public SelectList Statuses { get; set; }

        [BindProperty(SupportsGet = true)]
        public CarFilterModel Filter { get; set; } = new CarFilterModel();

        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalCars { get; set; }

        public async Task OnGetAsync()
        {
            // Définir la page courante
            CurrentPage = Filter.PageNumber ?? 1;
            
            // Construire la requête avec les filtres
            var query = _context.Cars
                .Include(c => c.Images)
                .AsQueryable();

            // Appliquer les filtres
            if (!string.IsNullOrEmpty(Filter.Make))
            {
                query = query.Where(c => c.Make == Filter.Make);
            }

            if (!string.IsNullOrEmpty(Filter.Model))
            {
                query = query.Where(c => c.Model == Filter.Model);
            }

            if (Filter.YearFrom.HasValue)
            {
                query = query.Where(c => c.Year >= Filter.YearFrom.Value);
            }

            if (Filter.YearTo.HasValue)
            {
                query = query.Where(c => c.Year <= Filter.YearTo.Value);
            }

            if (Filter.Status.HasValue)
            {
                query = query.Where(c => c.Status == Filter.Status.Value);
            }
            else if (!Filter.IncludeUnavailable)
            {
                // Par défaut, exclure les voitures non disponibles
                query = query.Where(c => c.Status != CarStatus.Sold && c.Status != CarStatus.NotAvailable);
            }

            // Calculer le nombre total de voitures et de pages
            TotalCars = await query.CountAsync();
            var pageSize = 9; // Nombre de voitures par page
            TotalPages = (int)Math.Ceiling(TotalCars / (double)pageSize);

            // Trier les résultats
            query = Filter.SortBy?.ToLower() switch
            {
                "price_asc" => query.OrderBy(c => c.SellingPrice),
                "price_desc" => query.OrderByDescending(c => c.SellingPrice),
                "year_asc" => query.OrderBy(c => c.Year),
                "year_desc" => query.OrderByDescending(c => c.Year),
                "newest" => query.OrderByDescending(c => c.CreatedAt),
                _ => query.OrderByDescending(c => c.CreatedAt) // Par défaut: les plus récentes
            };

            // Appliquer la pagination
            var cars = await query
                .Skip((CurrentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Convertir en ViewModel
            Cars = cars.Select(car => new CarViewModel
            {
                Id = car.Id,
                Make = car.Make,
                Model = car.Model,
                Year = car.Year,
                Color = car.Color,
                Mileage = car.Mileage,
                Description = car.Description,
                SellingPrice = car.SellingPrice,
                Status = car.Status,
                PrimaryImage = car.Images.FirstOrDefault(i => i.IsPrimary)?.ImagePath ?? 
                              car.Images.FirstOrDefault()?.ImagePath,
                ImageCount = car.Images.Count,
                CreatedAt = car.CreatedAt
            }).ToList();

            // Charger les listes déroulantes
            await LoadDropdownLists();
        }

        private async Task LoadDropdownLists()
        {
            // Obtenir les marques uniques
            var makes = await _context.Cars
                .Select(c => c.Make)
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync();
            Makes = new SelectList(makes);

            // Obtenir les modèles pour la marque sélectionnée
            var modelsQuery = _context.Cars.AsQueryable();
            if (!string.IsNullOrEmpty(Filter.Make))
            {
                modelsQuery = modelsQuery.Where(c => c.Make == Filter.Make);
            }
            var models = await modelsQuery
                .Select(c => c.Model)
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync();
            Models = new SelectList(models);

            // Préparer la liste des statuts
            var statuses = Enum.GetValues(typeof(CarStatus))
                .Cast<CarStatus>()
                .Select(s => new
                {
                    Value = (int)s,
                    Text = s.ToString()
                })
                .ToList();
            Statuses = new SelectList(statuses, "Value", "Text");
        }
    }

    public class CarViewModel
    {
        public int Id { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public int Year { get; set; }
        public string Color { get; set; }
        public int Mileage { get; set; }
        public string Description { get; set; }
        public decimal? SellingPrice { get; set; }
        public CarStatus Status { get; set; }
        public string PrimaryImage { get; set; }
        public int ImageCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CarFilterModel
    {
        public string Make { get; set; }
        public string Model { get; set; }
        public int? YearFrom { get; set; }
        public int? YearTo { get; set; }
        public CarStatus? Status { get; set; }
        public bool IncludeUnavailable { get; set; } = false;
        public string SortBy { get; set; } = "newest";
        public int? PageNumber { get; set; } = 1;
    }
} 