using EMGAS.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace EMGAS.Services
{
    public class AuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly EMGContext _context;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            EMGContext context)
        {
            _userManager = userManager;
            _configuration = configuration;
            _context = context;
        }

        /// <summary>
        /// Authentifie un utilisateur et génère un token JWT
        /// </summary>
        public async Task<AuthResponseModel> LoginAsync(LoginModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return new AuthResponseModel
                {
                    Success = false,
                    Message = "Utilisateur ou mot de passe incorrect"
                };
            }

            var result = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!result)
            {
                return new AuthResponseModel
                {
                    Success = false,
                    Message = "Utilisateur ou mot de passe incorrect"
                };
            }

            var token = await GenerateJwtTokenAsync(user);
            return new AuthResponseModel
            {
                Success = true,
                Token = token,
                ExpiresAt = DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpiryInMinutes"]))
            };
        }

        /// <summary>
        /// Génère un token JWT pour l'utilisateur authentifié
        /// </summary>
        private async Task<string> GenerateJwtTokenAsync(ApplicationUser user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Récupérer les rôles de l'utilisateur
            var roles = await _userManager.GetRolesAsync(user);
            
            // Récupérer les claims de l'utilisateur
            var userClaims = await _userManager.GetClaimsAsync(user);
            
            // Vérifier si l'utilisateur est un administrateur
            var isAdmin = roles.Contains("Administrator");
            
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim("FirstName", user.FirstName ?? string.Empty),
                new Claim("LastName", user.LastName ?? string.Empty)
            };

            // Ajouter les rôles comme claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Ajouter les claims personnalisés
            claims.AddRange(userClaims);

            // Si c'est un admin, récupérer son niveau
            if (isAdmin)
            {
                var admin = await _context.Admins
                    .FirstOrDefaultAsync(a => a.UserId == user.Id);
                
                if (admin != null)
                {
                    claims.Add(new Claim("AdminId", admin.Id.ToString()));
                    if (!userClaims.Any(c => c.Type == "AdminLevel"))
                    {
                        claims.Add(new Claim("AdminLevel", admin.Level.ToString()));
                    }
                }
            }

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpiryInMinutes"])),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class LoginModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class AuthResponseModel
    {
        public bool Success { get; set; }
        public string Token { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string Message { get; set; }
    }
} 