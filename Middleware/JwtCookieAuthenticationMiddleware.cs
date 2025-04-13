using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace EMGAS.Middleware
{
    public class JwtCookieAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;

        public JwtCookieAuthenticationMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Vérifier si le token JWT est présent dans le cookie
            if (context.Request.Cookies.TryGetValue("AuthToken", out string? token))
            {
                if (!string.IsNullOrEmpty(token))
                {
                    try
                    {
                        // Ajouter le token aux en-têtes HTTP pour être traité par le middleware JWT
                        context.Request.Headers.Append("Authorization", $"Bearer {token}");
                    }
                    catch (Exception ex)
                    {
                        // Log the exception or handle it appropriately
                        Console.WriteLine($"Error adding JWT to Authorization header: {ex.Message}");
                    }
                }
            }

            await _next(context);
        }
    }

    // Extension method pour faciliter l'ajout du middleware
    public static class JwtCookieAuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseJwtCookieAuthentication(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<JwtCookieAuthenticationMiddleware>();
        }
    }
} 