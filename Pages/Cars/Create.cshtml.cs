using EMGAS.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EMGAS.Pages.Cars
{
    [Authorize(Policy = "AdminOnly")]
    public class CreateModel : PageModel
    {
        private readonly EMGContext _context;
        private readonly IWebHostEnvironment _environment;

        public CreateModel(EMGContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public SelectList Makes { get; set; }
        public SelectList Models { get; set; }
        public SelectList Colors { get; set; }

        [BindProperty]
        public CarInputModel Car { get; set; }

        public async Task OnGetAsync()
        {
            await LoadDropdownLists();
        }

        public async Task<IActionResult> OnPostAsync(List<IFormFile> images)
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdownLists();
                return Page();
            }

            // Créer une nouvelle voiture
            var car = new Car
            {
                Make = Car.Make,
                Model = Car.Model,
                Year = Car.Year,
                VIN = Car.VIN,
                Color = Car.Color,
                Mileage = Car.Mileage,
                Description = Car.Description,
                PurchasePrice = Car.PurchasePrice,
                SellingPrice = Car.SellingPrice,
                PurchaseDate = Car.PurchaseDate,
                Status = Car.Status,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Cars.Add(car);
            await _context.SaveChangesAsync();

            // Traiter les images si elles existent
            if (images != null && images.Count > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "cars");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                foreach (var image in images)
                {
                    if (image.Length > 0)
                    {
                        // Générer un nom de fichier unique
                        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        // Enregistrer l'image
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await image.CopyToAsync(fileStream);
                        }

                        // Ajouter l'image à la base de données
                        var carImage = new CarImage
                        {
                            CarId = car.Id,
                            ImagePath = $"/uploads/cars/{uniqueFileName}",
                            IsPrimary = images.IndexOf(image) == 0, // La première image est l'image principale
                            UploadedAt = DateTime.UtcNow
                        };

                        _context.CarImages.Add(carImage);
                    }
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }

        private async Task LoadDropdownLists()
        {
            // Marques
            var makes = await _context.Cars
                .Select(c => c.Make)
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync();
            
            // Ajouter des marques communes si la liste est vide
            if (!makes.Any())
            {
                makes = new List<string>
                {
                    "Audi", "BMW", "Citroen", "Dacia", "Fiat", "Ford", "Honda", "Hyundai", 
                    "Kia", "Mercedes", "Nissan", "Opel", "Peugeot", "Renault", "Seat", 
                    "Skoda", "Toyota", "Volkswagen", "Volvo"
                };
            }
            
            Makes = new SelectList(makes);

            // Modèles (vide par défaut, sera rempli par JavaScript)
            Models = new SelectList(Enumerable.Empty<string>());

            // Couleurs
            var colors = new List<string>
            {
                "Noir", "Blanc", "Gris", "Rouge", "Bleu", "Vert", "Jaune", "Orange", 
                "Marron", "Beige", "Argent", "Or", "Bronze", "Bordeaux", "Violet"
            };
            Colors = new SelectList(colors);
        }
    }

    public class CarInputModel
    {
        [Required(ErrorMessage = "La marque est obligatoire")]
        public string Make { get; set; }

        [Required(ErrorMessage = "Le modèle est obligatoire")]
        public string Model { get; set; }

        [Required(ErrorMessage = "L'année est obligatoire")]
        [Range(2018, 2100, ErrorMessage = "L'année doit être 2018 ou plus récente")]
        public int Year { get; set; } = DateTime.Now.Year;

        [Required(ErrorMessage = "Le numéro VIN est obligatoire")]
        public string VIN { get; set; }

        [Required(ErrorMessage = "La couleur est obligatoire")]
        public string Color { get; set; }

        [Required(ErrorMessage = "Le kilométrage est obligatoire")]
        [Range(0, int.MaxValue, ErrorMessage = "Le kilométrage doit être positif")]
        public int Mileage { get; set; }

        [Required(ErrorMessage = "La description est obligatoire")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Le prix d'achat est obligatoire")]
        [Range(0, double.MaxValue, ErrorMessage = "Le prix d'achat doit être positif")]
        public decimal PurchasePrice { get; set; }

        public decimal? SellingPrice { get; set; }

        [Required(ErrorMessage = "La date d'achat est obligatoire")]
        [DataType(DataType.Date)]
        public DateTime PurchaseDate { get; set; } = DateTime.Now;

        public CarStatus Status { get; set; } = CarStatus.InInventory;
    }
} 