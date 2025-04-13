using EMGAS.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EMGAS.Services
{
    public class AdminService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly EMGContext _context;

        public AdminService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            EMGContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        /// <summary>
        /// Crée un nouvel administrateur avec le niveau spécifié.
        /// </summary>
        public async Task<IdentityResult> CreateAdminAsync(string email, string password, string firstName, string lastName, AdminLevel level)
        {
            // Créer l'utilisateur
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true // Confirmer l'email par défaut pour simplifier le processus
            };

            // Créer l'utilisateur dans Identity
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                return result;
            }

            // Vérifier si le rôle "Administrator" existe
            if (!await _roleManager.RoleExistsAsync("Administrator"))
            {
                // Créer le rôle s'il n'existe pas
                await _roleManager.CreateAsync(new IdentityRole("Administrator"));
            }

            // Ajouter l'utilisateur au rôle "Administrator"
            await _userManager.AddToRoleAsync(user, "Administrator");

            // Ajouter le niveau d'administrateur comme claim
            await _userManager.AddClaimAsync(user, new Claim("AdminLevel", level.ToString()));

            // Créer l'entité Admin
            var admin = new Admin
            {
                UserId = user.Id,
                Level = level,
                CreatedAt = DateTime.UtcNow
            };

            // Ajouter l'admin à la base de données
            _context.Admins.Add(admin);
            await _context.SaveChangesAsync();

            return result;
        }

        /// <summary>
        /// Récupère tous les administrateurs avec leurs informations d'utilisateur.
        /// </summary>
        public async Task<List<AdminViewModel>> GetAllAdminsAsync()
        {
            var admins = await _context.Admins
                .Include(a => a.User)
                .ToListAsync();

            return admins.Select(a => new AdminViewModel
            {
                Id = a.Id,
                Email = a.User.Email,
                FirstName = a.User.FirstName,
                LastName = a.User.LastName,
                Level = a.Level,
                CreatedAt = a.CreatedAt
            }).ToList();
        }

        /// <summary>
        /// Met à jour le niveau d'un administrateur.
        /// </summary>
        public async Task<bool> UpdateAdminLevelAsync(int adminId, AdminLevel newLevel)
        {
            var admin = await _context.Admins
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == adminId);

            if (admin == null)
            {
                return false;
            }

            // Mettre à jour le niveau d'administration
            admin.Level = newLevel;

            // Mettre à jour le claim pour le niveau d'administrateur
            var user = admin.User;
            var claims = await _userManager.GetClaimsAsync(user);
            var adminLevelClaim = claims.FirstOrDefault(c => c.Type == "AdminLevel");

            if (adminLevelClaim != null)
            {
                await _userManager.RemoveClaimAsync(user, adminLevelClaim);
            }

            await _userManager.AddClaimAsync(user, new Claim("AdminLevel", newLevel.ToString()));

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Supprime un administrateur.
        /// </summary>
        public async Task<bool> DeleteAdminAsync(int adminId)
        {
            var admin = await _context.Admins
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == adminId);

            if (admin == null)
            {
                return false;
            }

            var user = admin.User;

            // Supprimer l'administrateur
            _context.Admins.Remove(admin);
            await _context.SaveChangesAsync();

            // Supprimer le rôle de l'utilisateur
            await _userManager.RemoveFromRoleAsync(user, "Administrator");

            // Option : supprimer complètement l'utilisateur
            // await _userManager.DeleteAsync(user);

            return true;
        }
    }

    public class AdminViewModel
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public AdminLevel Level { get; set; }
        public DateTime CreatedAt { get; set; }
    }
} 