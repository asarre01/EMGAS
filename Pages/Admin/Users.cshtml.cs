using EMGAS.Data;
using EMGAS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace EMGAS.Pages.Admin
{
    [Authorize(Policy = "AdminOnly")]
    public class UsersModel : PageModel
    {
        private readonly AdminService _adminService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EMGContext _context;

        public UsersModel(
            AdminService adminService,
            UserManager<ApplicationUser> userManager,
            EMGContext context)
        {
            _adminService = adminService;
            _userManager = userManager;
            _context = context;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [TempData]
        public bool IsSuccess { get; set; }

        public List<AdminViewModel> Admins { get; set; } = new List<AdminViewModel>();

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadAdmins();
            return Page();
        }

        public async Task<IActionResult> OnPostCreateAdminAsync(CreateAdminInputModel model)
        {
            if (!ModelState.IsValid)
            {
                await LoadAdmins();
                IsSuccess = false;
                StatusMessage = "Des erreurs de validation ont été rencontrées.";
                return Page();
            }

            if (model.Password != model.ConfirmPassword)
            {
                await LoadAdmins();
                IsSuccess = false;
                StatusMessage = "Les mots de passe ne correspondent pas.";
                return Page();
            }

            var result = await _adminService.CreateAdminAsync(
                model.Email,
                model.Password,
                model.FirstName,
                model.LastName,
                model.Level);

            if (!result.Succeeded)
            {
                await LoadAdmins();
                IsSuccess = false;
                StatusMessage = $"Erreur lors de la création : {string.Join(", ", result.Errors.Select(e => e.Description))}";
                return Page();
            }

            IsSuccess = true;
            StatusMessage = "Administrateur créé avec succès.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateAdminAsync(UpdateAdminInputModel model)
        {
            if (!ModelState.IsValid)
            {
                await LoadAdmins();
                IsSuccess = false;
                StatusMessage = "Des erreurs de validation ont été rencontrées.";
                return Page();
            }

            var admin = await _context.Admins
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == model.Id);

            if (admin == null)
            {
                await LoadAdmins();
                IsSuccess = false;
                StatusMessage = "Administrateur non trouvé.";
                return Page();
            }

            // Mettre à jour les informations de base
            admin.User.FirstName = model.FirstName;
            admin.User.LastName = model.LastName;
            admin.User.Email = model.Email;
            admin.User.UserName = model.Email;
            admin.Level = model.Level;

            // Si un nouveau mot de passe est fourni, le mettre à jour
            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(admin.User);
                var passwordResult = await _userManager.ResetPasswordAsync(admin.User, token, model.NewPassword);
                
                if (!passwordResult.Succeeded)
                {
                    await LoadAdmins();
                    IsSuccess = false;
                    StatusMessage = $"Erreur lors de la mise à jour du mot de passe : {string.Join(", ", passwordResult.Errors.Select(e => e.Description))}";
                    return Page();
                }
            }

            // Mettre à jour les claims
            var claims = await _userManager.GetClaimsAsync(admin.User);
            var adminLevelClaim = claims.FirstOrDefault(c => c.Type == "AdminLevel");
            
            if (adminLevelClaim != null)
            {
                await _userManager.RemoveClaimAsync(admin.User, adminLevelClaim);
            }
            
            await _userManager.AddClaimAsync(admin.User, new System.Security.Claims.Claim("AdminLevel", admin.Level.ToString()));

            await _context.SaveChangesAsync();

            IsSuccess = true;
            StatusMessage = "Administrateur mis à jour avec succès.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAdminAsync(int id)
        {
            var result = await _adminService.DeleteAdminAsync(id);
            
            if (!result)
            {
                IsSuccess = false;
                StatusMessage = "Erreur lors de la suppression de l'administrateur.";
                return RedirectToPage();
            }

            IsSuccess = true;
            StatusMessage = "Administrateur supprimé avec succès.";
            return RedirectToPage();
        }

        private async Task LoadAdmins()
        {
            var adminEntities = await _context.Admins
                .Include(a => a.User)
                .OrderByDescending(a => a.Level)
                .ThenBy(a => a.User.LastName)
                .ToListAsync();

            Admins = adminEntities.Select(a => new AdminViewModel
            {
                Id = a.Id,
                UserId = a.UserId,
                FirstName = a.User.FirstName,
                LastName = a.User.LastName,
                Email = a.User.Email,
                Level = a.Level,
                CreatedAt = a.CreatedAt
            }).ToList();
        }
    }

    public class AdminViewModel
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public AdminLevel Level { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateAdminInputModel
    {
        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Le prénom est requis")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Le nom est requis")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Le mot de passe est requis")]
        [MinLength(8, ErrorMessage = "Le mot de passe doit comporter au moins 8 caractères")]
        public string Password { get; set; }

        [Required(ErrorMessage = "La confirmation du mot de passe est requise")]
        [Compare("Password", ErrorMessage = "Les mots de passe ne correspondent pas")]
        public string ConfirmPassword { get; set; }

        public AdminLevel Level { get; set; }
    }

    public class UpdateAdminInputModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Le prénom est requis")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Le nom est requis")]
        public string LastName { get; set; }

        [MinLength(8, ErrorMessage = "Le mot de passe doit comporter au moins 8 caractères")]
        public string NewPassword { get; set; }

        public AdminLevel Level { get; set; }
    }
} 