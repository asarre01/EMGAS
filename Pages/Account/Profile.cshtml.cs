using EMGAS.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EMGAS.Pages.Account
{
    [Authorize]
    public class ProfileModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly EMGContext _context;

        public ProfileModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            EMGContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [TempData]
        public bool IsSuccess { get; set; }

        [BindProperty]
        public ProfileInputModel Input { get; set; }

        [BindProperty]
        public ChangePasswordInputModel PasswordInput { get; set; }

        public string AdminLevel { get; set; }
        public DateTime CreatedAt { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Set up the profile input model
            Input = new ProfileInputModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email
            };

            // Get additional user info
            CreatedAt = user.CreatedAt;

            // Get admin level if applicable
            if (User.IsInRole("Administrator"))
            {
                var admin = await _context.Admins
                    .FirstOrDefaultAsync(a => a.UserId == user.Id);
                
                if (admin != null)
                {
                    AdminLevel = admin.Level.ToString();
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateProfileAsync()
        {
            if (!ModelState.IsValid)
            {
                IsSuccess = false;
                StatusMessage = "Des erreurs de validation ont été rencontrées.";
                return RedirectToPage();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Update user properties
            user.FirstName = Input.FirstName;
            user.LastName = Input.LastName;

            // Save changes
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                IsSuccess = false;
                StatusMessage = "Erreur lors de la mise à jour du profil.";
                return RedirectToPage();
            }

            // Refresh sign-in to update claims
            await _signInManager.RefreshSignInAsync(user);

            IsSuccess = true;
            StatusMessage = "Profil mis à jour avec succès.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostChangePasswordAsync()
        {
            if (!ModelState.IsValid)
            {
                IsSuccess = false;
                StatusMessage = "Des erreurs de validation ont été rencontrées.";
                return RedirectToPage();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user,
                PasswordInput.CurrentPassword, PasswordInput.NewPassword);
            
            if (!changePasswordResult.Succeeded)
            {
                IsSuccess = false;
                StatusMessage = $"Erreur lors du changement de mot de passe : {string.Join(", ", changePasswordResult.Errors.Select(e => e.Description))}";
                return RedirectToPage();
            }

            await _signInManager.RefreshSignInAsync(user);
            IsSuccess = true;
            StatusMessage = "Mot de passe changé avec succès.";
            return RedirectToPage();
        }
    }

    public class ProfileInputModel
    {
        [Required(ErrorMessage = "Le prénom est requis")]
        [Display(Name = "Prénom")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Le nom est requis")]
        [Display(Name = "Nom")]
        public string LastName { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }
    }

    public class ChangePasswordInputModel
    {
        [Required(ErrorMessage = "Le mot de passe actuel est requis")]
        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe actuel")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Le nouveau mot de passe est requis")]
        [StringLength(100, ErrorMessage = "Le {0} doit comporter au moins {2} caractères et au maximum {1} caractères.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Nouveau mot de passe")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmer le nouveau mot de passe")]
        [Compare("NewPassword", ErrorMessage = "Le nouveau mot de passe et la confirmation ne correspondent pas.")]
        public string ConfirmPassword { get; set; }
    }
} 