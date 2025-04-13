using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EMGAS.Data
{
    public class EMGContext : IdentityDbContext<ApplicationUser>
    {
        public EMGContext(DbContextOptions<EMGContext> options)
            : base(options)
        {
        }

        public DbSet<Car> Cars { get; set; }
        public DbSet<CarImage> CarImages { get; set; }
        public DbSet<RepairDetail> RepairDetails { get; set; }
        public DbSet<Admin> Admins { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuration des relations entre entités
            modelBuilder.Entity<Car>()
                .HasMany(c => c.Images)
                .WithOne(i => i.Car)
                .HasForeignKey(i => i.CarId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Car>()
                .HasMany(c => c.RepairDetails)
                .WithOne(r => r.Car)
                .HasForeignKey(r => r.CarId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configuration pour Admin
            modelBuilder.Entity<Admin>()
                .HasOne(a => a.User)
                .WithOne()
                .HasForeignKey<Admin>(a => a.UserId);
        }
    }

    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Admin
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public AdminLevel Level { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation property
        public ApplicationUser User { get; set; }
    }

    public enum AdminLevel
    {
        Junior,
        Senior,
        SuperAdmin
    }

    public class Car
    {
        public int Id { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        [Range(2018, 2100, ErrorMessage = "L'année doit être 2018 ou plus récente")]
        public int Year { get; set; }
        public string VIN { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal? SellingPrice { get; set; }
        public DateTime PurchaseDate { get; set; }
        public DateTime? SaleDate { get; set; }
        public string Description { get; set; }
        public string Color { get; set; }
        public int Mileage { get; set; }
        public CarStatus Status { get; set; } = CarStatus.InInventory;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public ICollection<CarImage> Images { get; set; } = new List<CarImage>();
        public ICollection<RepairDetail> RepairDetails { get; set; } = new List<RepairDetail>();
    }

    public class CarImage
    {
        public int Id { get; set; }
        public string ImagePath { get; set; }
        public bool IsPrimary { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        
        // Foreign key
        public int CarId { get; set; }
        
        // Navigation property
        public Car Car { get; set; }
    }

    public class RepairDetail
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public decimal Cost { get; set; }
        public DateTime RepairDate { get; set; }
        
        // Foreign key
        public int CarId { get; set; }
        
        // Navigation property
        public Car Car { get; set; }
    }

    public enum CarStatus
    {
        [Display(Name = "En inventaire")]
        InInventory,
        [Display(Name = "En réparation")]
        UnderRepair,
        [Display(Name = "À vendre")]
        ForSale,
        [Display(Name = "Vendue")]
        Sold,
        [Display(Name = "Non disponible")]
        NotAvailable
    }

    public class CarInputModel
    {
        [Required(ErrorMessage = "La marque est obligatoire")]
        public string Make { get; set; }
    }
}
