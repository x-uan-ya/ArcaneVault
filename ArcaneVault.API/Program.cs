// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using ArcaneVault.API.Data;
using ArcaneVault.API.Middleware;
using ArcaneVault.API.Services;
using BCrypt.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ArcaneVault.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ========== DATABASE CONFIGURATION ==========
            // EF Core with SQLite
            builder.Services.AddDbContext<ArcaneVaultDbContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

            // ========== AUTHENTICATION & AUTHORIZATION ==========
            // JWT Configuration
            var jwtSecret = builder.Configuration["JwtSecret"] ?? "YourVerySecureSecretKeyThatIsAtLeast32CharactersLongForHS256!";
            var jwtIssuer = builder.Configuration["JwtIssuer"] ?? "ArcaneVault";
            var jwtAudience = builder.Configuration["JwtAudience"] ?? "ArcaneVaultUsers";

            var key = Encoding.ASCII.GetBytes(jwtSecret);
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

            builder.Services.AddAuthorization();

            // ========== DEPENDENCY INJECTION ==========
            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
            builder.Services.AddScoped<IFixedDepositService, FixedDepositService>();

            builder.Services.AddControllers();
            builder.Services.AddOpenApi();

            // ========== CORS CONFIGURATION ==========
            // Allow the Razor Pages web app to call this API
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowWeb", policy =>
                    policy.WithOrigins("https://localhost:7088", "http://localhost:5245")
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials());
            });

            // ========== LOGGING CONFIGURATION ==========
            builder.Services.AddLogging(options =>
            {
                options.ClearProviders();
                options.AddConsole();
                options.AddDebug();
                options.SetMinimumLevel(LogLevel.Information);
            });

            // ========== HTTP CLIENT CONFIGURATION ==========
            builder.Services.AddHttpClient();
            // Global HTTP client timeout configuration
            builder.Services.Configure<SocketsHttpHandler>(handler =>
            {
                handler.ConnectTimeout = TimeSpan.FromSeconds(30);
            });

            var app = builder.Build();

            // ========== DATABASE INITIALIZATION ==========
            // Auto-apply migrations and seed data on startup
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ArcaneVaultDbContext>();
                db.Database.Migrate();

                // Ensure admin account exists with correct password hash
                var adminUser = db.Users.FirstOrDefault(u => u.UserName == "admin");
                if (adminUser != null)
                {
                    // Generate a fresh BCrypt hash for "Admin@123"
                    string hashedPassword = BCrypt.Net.BCrypt.HashPassword("Admin@123");
                    if (adminUser.PasswordHash != hashedPassword)
                    {
                        adminUser.PasswordHash = hashedPassword;
                        db.Users.Update(adminUser);
                        db.SaveChanges();
                    }
                }
            }

            // ========== MIDDLEWARE PIPELINE ==========
            // Global error handling middleware
            app.UseMiddleware<ErrorHandlingMiddleware>();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();
            app.UseCors("AllowWeb");

            // Authentication & Authorization middleware
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapGet("/", () => Results.Ok("ArcaneVault API is running")).WithName("Root").WithOpenApi();

            app.MapControllers();
            app.Run();
        }
    }
}
