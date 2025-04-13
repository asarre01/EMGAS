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
    public class EditModel : PageModel
    {
        private readonly EMGContext _context;
        private readonly IWebHostEnvironment _environment;

        public EditModel(EMGContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [BindProperty]
        public CarEditModel Car { get; set; }

        public List<CarImage> CarImages { get; set; } = new List<CarImage>();
        public List<RepairDetail> RepairDetails { get; set; } = new List<RepairDetail>();

        public SelectList Makes { get; set; }
        public SelectList Models { get; set; }
        public SelectList Colors { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var car = await _context.Cars
                .Include(c => c.Images)
                .Include(c => c.RepairDetails)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (car == null)
            {
                return NotFound();
            }

            Car = new CarEditModel
            {
                Id = car.Id,
                Make = car.Make,
                Model = car.Model,
                Year = car.Year,
                VIN = car.VIN,
                Color = car.Color,
                Mileage = car.Mileage,
                Description = car.Description,
                PurchasePrice = car.PurchasePrice,
                SellingPrice = car.SellingPrice,
                PurchaseDate = car.PurchaseDate,
                Status = car.Status
            };

            CarImages = car.Images.ToList();
            RepairDetails = car.RepairDetails.OrderByDescending(r => r.RepairDate).ToList();

            await LoadDropdownLists();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdownLists();
                await LoadRelatedData();
                return Page();
            }

            var car = await _context.Cars.FindAsync(Car.Id);
            if (car == null)
            {
                return NotFound();
            }

            // Mettre à jour les propriétés de la voiture
            car.Make = Car.Make;
            car.Model = Car.Model;
            car.Year = Car.Year;
            car.VIN = Car.VIN;
            car.Color = Car.Color;
            car.Mileage = Car.Mileage;
            car.Description = Car.Description;
            car.PurchasePrice = Car.PurchasePrice;
            car.SellingPrice = Car.SellingPrice;
            car.PurchaseDate = Car.PurchaseDate;
            
            // Si le statut passe à "Vendu", enregistrer la date de vente
            if (Car.Status == CarStatus.Sold && car.Status != CarStatus.Sold)
            {
                car.SaleDate = DateTime.UtcNow;
            }
            else if (Car.Status != CarStatus.Sold)
            {
                car.SaleDate = null;
            }
            
            car.Status = Car.Status;
            car.UpdatedAt = DateTime.UtcNow;

            _context.Cars.Update(car);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostUploadImageAsync(int carId, IFormFile newImage, bool setAsPrimary)
        {
            if (newImage == null || newImage.Length == 0)
            {
                ModelState.AddModelError("newImage", "Une image est requise");
                await LoadRelatedData();
                await LoadDropdownLists();
                return Page();
            }

            var car = await _context.Cars.FindAsync(carId);
            if (car == null)
            {
                return NotFound();
            }

            // Créer le dossier pour les images s'il n'existe pas
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "cars");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Générer un nom de fichier unique
            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(newImage.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Enregistrer l'image
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await newImage.CopyToAsync(fileStream);
            }

            // Si c'est l'image principale, désactiver les autres images principales
            if (setAsPrimary)
            {
                var existingPrimaryImages = await _context.CarImages
                    .Where(i => i.CarId == carId && i.IsPrimary)
                    .ToListAsync();

                foreach (var image in existingPrimaryImages)
                {
                    image.IsPrimary = false;
                    _context.CarImages.Update(image);
                }
            }

            // Créer l'entrée d'image dans la base de données
            var carImage = new CarImage
            {
                CarId = carId,
                ImagePath = $"/uploads/cars/{uniqueFileName}",
                IsPrimary = setAsPrimary,
                UploadedAt = DateTime.UtcNow
            };

            _context.CarImages.Add(carImage);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Edit", new { id = carId });
        }

        public async Task<IActionResult> OnPostAddRepairAsync(int carId, string repairDescription, decimal repairCost, DateTime? repairDate, bool updateStatus)
        {
            var car = await _context.Cars.FindAsync(carId);
            if (car == null)
            {
                return NotFound();
            }

            // Ajouter la réparation
            var repair = new RepairDetail
            {
                CarId = carId,
                Description = repairDescription,
                Cost = repairCost,
                RepairDate = repairDate ?? DateTime.UtcNow
            };

            _context.RepairDetails.Add(repair);

            // Mettre à jour le statut de la voiture si demandé
            if (updateStatus && car.Status != CarStatus.UnderRepair)
            {
                car.Status = CarStatus.UnderRepair;
                car.UpdatedAt = DateTime.UtcNow;
                _context.Cars.Update(car);
            }

            await _context.SaveChangesAsync();

            return RedirectToPage("./Edit", new { id = carId });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var car = await _context.Cars
                .Include(c => c.Images)
                .Include(c => c.RepairDetails)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (car == null)
            {
                return NotFound();
            }

            // Supprimer les fichiers d'images physiques
            foreach (var image in car.Images)
            {
                var filePath = Path.Combine(_environment.WebRootPath, image.ImagePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _context.Cars.Remove(car);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        [HttpPost]
        public async Task<IActionResult> OnPostSetPrimaryImageAsync(int imageId, int carId)
        {
            var images = await _context.CarImages.Where(i => i.CarId == carId).ToListAsync();
            foreach (var image in images)
            {
                image.IsPrimary = image.Id == imageId;
                _context.CarImages.Update(image);
            }

            await _context.SaveChangesAsync();

            return RedirectToPage("./Edit", new { id = carId });
        }

        [HttpPost]
        public async Task<IActionResult> OnPostDeleteRepairAsync(int repairId)
        {
            var repair = await _context.RepairDetails
                .Include(r => r.Car)
                .FirstOrDefaultAsync(r => r.Id == repairId);

            if (repair == null)
            {
                return NotFound();
            }

            var carId = repair.CarId;
            _context.RepairDetails.Remove(repair);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Edit", new { id = carId });
        }

        private async Task LoadDropdownLists()
        {
            // Marques
            var makes = await _context.Cars
                .Select(c => c.Make)
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync();
            
            // Ajouter des marques communes si la liste est vide ou ne contient pas la marque actuelle
            if (!makes.Any() || (Car?.Make != null && !makes.Contains(Car.Make)))
            {
                var commonMakes = new List<string>
                {
                    "Audi", "BMW", "Citroen", "Dacia", "Fiat", "Ford", "Honda", "Hyundai", 
                    "Kia", "Mercedes", "Nissan", "Opel", "Peugeot", "Renault", "Seat", 
                    "Skoda", "Toyota", "Volkswagen", "Volvo"
                };
                
                foreach (var make in commonMakes)
                {
                    if (!makes.Contains(make))
                    {
                        makes.Add(make);
                    }
                }
                
                makes = makes.OrderBy(m => m).ToList();
            }
            
            Makes = new SelectList(makes);

            // Modèles
            var modelsQuery = _context.Cars.AsQueryable();
            if (!string.IsNullOrEmpty(Car?.Make))
            {
                modelsQuery = modelsQuery.Where(c => c.Make == Car.Make);
            }
            
            var models = await modelsQuery
                .Select(c => c.Model)
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync();
                
            // Ajouter le modèle actuel s'il n'est pas dans la liste
            if (Car?.Model != null && !models.Contains(Car.Model))
            {
                models.Add(Car.Model);
                models = models.OrderBy(m => m).ToList();
            }
            
            Models = new SelectList(models);

            // Couleurs
            var colors = new List<string>
            {
                "Noir", "Blanc", "Gris", "Rouge", "Bleu", "Vert", "Jaune", "Orange", 
                "Marron", "Beige", "Argent", "Or", "Bronze", "Bordeaux", "Violet"
            };
            
            // Ajouter la couleur actuelle si elle n'est pas dans la liste
            if (Car?.Color != null && !colors.Contains(Car.Color))
            {
                colors.Add(Car.Color);
                colors = colors.OrderBy(c => c).ToList();
            }
            
            Colors = new SelectList(colors);
        }

        private async Task LoadRelatedData()
        {
            if (Car != null && Car.Id > 0)
            {
                var car = await _context.Cars
                    .Include(c => c.Images)
                    .Include(c => c.RepairDetails)
                    .FirstOrDefaultAsync(c => c.Id == Car.Id);

                if (car != null)
                {
                    CarImages = car.Images.ToList();
                    RepairDetails = car.RepairDetails.OrderByDescending(r => r.RepairDate).ToList();
                }
            }
        }
    }

    public class CarEditModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "La marque est obligatoire")]
        public string Make { get; set; }

        [Required(ErrorMessage = "Le modèle est obligatoire")]
        public string Model { get; set; }

        [Required(ErrorMessage = "L'année est obligatoire")]
        [Range(2018, 2100, ErrorMessage = "L'année doit être 2018 ou plus récente")]
        public int Year { get; set; }

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
        public DateTime PurchaseDate { get; set; }

        public CarStatus Status { get; set; }
    }
} 