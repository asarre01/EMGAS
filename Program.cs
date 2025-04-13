using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using EMGAS.Middleware;
using EMGAS.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers(); // Ajouter le support pour les contrôleurs API

// Add DbContext
builder.Services.AddDbContext<EMGAS.Data.EMGContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity (remove duplicate AddIdentityCore)
builder.Services.AddIdentity<EMGAS.Data.ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<EMGAS.Data.EMGContext>()
.AddDefaultTokenProviders();

// Add Authentication with JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "DefaultSecretKey12345678901234567890")),
        ClockSkew = TimeSpan.Zero
    };
});

// Add Authorization policies
builder.Services.AddAuthorization(options =>
{
    // Politique basée sur le rôle administrateur
    options.AddPolicy("AdminOnly", policy => 
        policy.RequireRole("Administrator"));
    
    // Politiques basées sur les niveaux d'administrateur
    options.AddPolicy("JuniorAdminOnly", policy => 
        policy.RequireRole("Administrator")
              .RequireClaim("AdminLevel", "Junior"));
    
    options.AddPolicy("SeniorAdminOnly", policy => 
        policy.RequireRole("Administrator")
              .RequireClaim("AdminLevel", "Senior"));
    
    options.AddPolicy("SuperAdminOnly", policy => 
        policy.RequireRole("Administrator")
              .RequireClaim("AdminLevel", "SuperAdmin"));
    
    // Politique combinant les administrateurs de niveau Senior et SuperAdmin
    options.AddPolicy("SeniorOrSuperAdminOnly", policy => 
        policy.RequireRole("Administrator")
              .RequireClaim("AdminLevel", new[] { "Senior", "SuperAdmin" }));
    
    // Définir la politique par défaut qui exige l'authentification
    // options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
    //     .RequireAuthenticatedUser()
    //     .Build();
});

// Register application services
builder.Services.AddScoped<EMGAS.Services.AdminService>();
builder.Services.AddScoped<EMGAS.Services.AuthService>();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins",
        builder => builder
            .WithOrigins("https://yourfrontendappurl.com") // Remplacer par l'URL de votre application frontend
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Initialiser les données
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Initialiser l'administrateur et les données de démo
        await SeedData.Initialize(
            services, 
            "admin@emgas.com", 
            "Admin123!"  // Mot de passe à changer en production !
        );
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Une erreur s'est produite lors de l'initialisation des données.");
    }
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Use CORS policy
app.UseCors("AllowSpecificOrigins");

app.UseRouting();

// Ajouter notre middleware de gestion des cookies JWT
app.UseJwtCookieAuthentication();

// Add middleware for authentication and authorization - Ordre important
app.UseAuthentication();
app.UseAuthorization();

// Ne pas exiger l'authentification par défaut pour toutes les pages Razor
app.MapRazorPages();

// Mapper les contrôleurs API
app.MapControllers();

app.Run();
