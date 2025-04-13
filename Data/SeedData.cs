using EMGAS.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace EMGAS.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider, string adminEmail, string adminPassword)
        {
            using var context = new EMGContext(
                serviceProvider.GetRequiredService<DbContextOptions<EMGContext>>());
            
            // Assurez-vous que la base de données est créée
            context.Database.EnsureCreated();

            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Créer le rôle administrateur s'il n'existe pas déjà
            if (!await roleManager.RoleExistsAsync("Administrator"))
            {
                await roleManager.CreateAsync(new IdentityRole("Administrator"));
            }

            // Vérifier si l'administrateur existe déjà
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                // Créer l'utilisateur administrateur
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FirstName = "Admin",
                    LastName = "User",
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    // Ajouter l'utilisateur au rôle administrateur
                    await userManager.AddToRoleAsync(adminUser, "Administrator");

                    // Ajouter une revendication AdminLevel pour le Super Admin
                    await userManager.AddClaimAsync(adminUser, new System.Security.Claims.Claim("AdminLevel", "SuperAdmin"));

                    // Créer un enregistrement Admin correspondant
                    var admin = new Admin
                    {
                        UserId = adminUser.Id,
                        Level = AdminLevel.SuperAdmin,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    context.Admins.Add(admin);
                    await context.SaveChangesAsync();

                    Console.WriteLine($"Administrateur créé: {adminEmail}");
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    Console.WriteLine($"Erreur lors de la création de l'administrateur: {errors}");
                }
            }
            else
            {
                Console.WriteLine($"L'administrateur {adminEmail} existe déjà.");
            }

            // Ajouter des voitures de démonstration si la table est vide
            if (!context.Cars.Any())
            {
                var demoCarModels = new[]
                {
                    new { Make = "Peugeot", Model = "308", Year = 2020, Color = "Bleu", Mileage = 35000, SellingPrice = 16500m },
                    new { Make = "Renault", Model = "Clio", Year = 2019, Color = "Rouge", Mileage = 42000, SellingPrice = 12900m },
                    new { Make = "Citroen", Model = "C3", Year = 2021, Color = "Blanc", Mileage = 15000, SellingPrice = 14500m },
                    new { Make = "Toyota", Model = "Yaris", Year = 2018, Color = "Gris", Mileage = 58000, SellingPrice = 11800m },
                    new { Make = "Volkswagen", Model = "Golf", Year = 2020, Color = "Noir", Mileage = 27000, SellingPrice = 18900m }
                };

                foreach (var model in demoCarModels)
                {
                    var car = new Car
                    {
                        Make = model.Make,
                        Model = model.Model,
                        Year = model.Year,
                        Color = model.Color,
                        Mileage = model.Mileage,
                        VIN = $"VIN{Guid.NewGuid().ToString().Substring(0, 10).ToUpper()}",
                        Description = $"Voiture {model.Make} {model.Model} {model.Year} en excellent état.",
                        PurchasePrice = model.SellingPrice * 0.8m,
                        SellingPrice = model.SellingPrice,
                        PurchaseDate = DateTime.UtcNow.AddMonths(-3),
                        Status = CarStatus.ForSale,
                        CreatedAt = DateTime.UtcNow.AddDays(-30),
                        UpdatedAt = DateTime.UtcNow
                    };

                    context.Cars.Add(car);
                }

                await context.SaveChangesAsync();
                Console.WriteLine("Voitures de démonstration ajoutées.");
            }
        }
    }
} 