using EMGAS.Data;
using EMGAS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace EMGAS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly AdminService _adminService;

        public AuthController(
            AuthService authService,
            AdminService adminService)
        {
            _authService = authService;
            _adminService = adminService;
        }

        /// <summary>
        /// Authentifie un utilisateur et génère un token JWT
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.LoginAsync(model);
            if (!result.Success)
            {
                return Unauthorized(new { message = result.Message });
            }

            return Ok(result);
        }

        /// <summary>
        /// Crée un nouvel administrateur (accessible uniquement par les super-admins)
        /// </summary>
        [HttpPost("create-admin")]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _adminService.CreateAdminAsync(
                model.Email,
                model.Password,
                model.FirstName,
                model.LastName,
                model.Level);

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    message = "Échec lors de la création de l'administrateur",
                    errors = result.Errors
                });
            }

            return Ok(new { message = "Administrateur créé avec succès" });
        }

        /// <summary>
        /// Récupère tous les administrateurs (accessible uniquement par les super-admins)
        /// </summary>
        [HttpGet("admins")]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> GetAllAdmins()
        {
            var admins = await _adminService.GetAllAdminsAsync();
            return Ok(admins);
        }

        /// <summary>
        /// Met à jour le niveau d'un administrateur (accessible uniquement par les super-admins)
        /// </summary>
        [HttpPut("admins/{id}/level")]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> UpdateAdminLevel(int id, [FromBody] UpdateAdminLevelModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _adminService.UpdateAdminLevelAsync(id, model.Level);
            if (!result)
            {
                return NotFound(new { message = "Administrateur non trouvé" });
            }

            return Ok(new { message = "Niveau d'administrateur mis à jour avec succès" });
        }

        /// <summary>
        /// Supprime un administrateur (accessible uniquement par les super-admins)
        /// </summary>
        [HttpDelete("admins/{id}")]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> DeleteAdmin(int id)
        {
            var result = await _adminService.DeleteAdminAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Administrateur non trouvé" });
            }

            return Ok(new { message = "Administrateur supprimé avec succès" });
        }
    }

    public class CreateAdminModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public AdminLevel Level { get; set; }
    }

    public class UpdateAdminLevelModel
    {
        public AdminLevel Level { get; set; }
    }
} 