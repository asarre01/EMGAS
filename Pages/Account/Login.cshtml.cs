using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using EMGAS.Data;
using EMGAS.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace EMGAS.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly AuthService _authService;

        public LoginModel(AuthService authService)
        {
            _authService = authService;
        }

        [BindProperty]
        public LoginInputModel Input { get; set; } = new LoginInputModel();

        public string ReturnUrl { get; set; } = string.Empty;

        [TempData]
        public string ErrorMessage { get; set; } = string.Empty;

        public void OnGet(string returnUrl = "")
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            ReturnUrl = returnUrl ?? Url.Content("~/");
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = "")
        {
            // Assurer que returnUrl a une valeur non vide
            if (string.IsNullOrEmpty(returnUrl))
            {
                returnUrl = Url.Content("~/");
            }
            
            // Garantir que même si Url.Content renvoie une chaîne vide, on a une valeur par défaut
            if (string.IsNullOrEmpty(returnUrl))
            {
                returnUrl = "/";
            }

            if (ModelState.IsValid)
            {
                var result = await _authService.LoginAsync(new EMGAS.Services.LoginModel
                {
                    Email = Input.Email,
                    Password = Input.Password
                });

                if (result.Success)
                {
                    // Pour les besoins de la démonstration, stockez le token dans un cookie
                    // Dans une application réelle, vous utiliseriez un mécanisme plus sécurisé
                    Response.Cookies.Append("AuthToken", result.Token, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict,
                        Expires = result.ExpiresAt
                    });

                    return LocalRedirect(returnUrl);
                }
                
                ModelState.AddModelError(string.Empty, result.Message);
            }

            // Si on arrive ici, quelque chose a échoué, réafficher le formulaire
            return Page();
        }
    }

    public class LoginInputModel
    {
        [Required(ErrorMessage = "L'adresse email est requise")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est requis")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Se souvenir de moi")]
        public bool RememberMe { get; set; }
    }
} 