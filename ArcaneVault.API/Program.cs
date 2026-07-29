// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using ArcaneVault.API.Data;
using ArcaneVault.API.Services;
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
            // EF Core with SQLite — use an absolute path so it works on AWS too
            var dbPath = builder.Configuration.GetConnectionString("DefaultConnection")
                         ?? "Data Source=ArcaneVault.db";
            // If the connection string is a relative filename, anchor it to a writable folder
            if (dbPath.StartsWith("Data Source=") && !dbPath.Contains('/') && !dbPath.Contains('\\'))
            {
                var folder = builder.Environment.IsDevelopment()
                    ? builder.Environment.ContentRootPath
                    : Path.Combine(Path.GetTempPath(), "ArcaneVault");
                Directory.CreateDirectory(folder);
                dbPath = $"Data Source={Path.Combine(folder, "ArcaneVault.db")}";
            }

            builder.Services.AddDbContext<ArcaneVaultDbContext>(options =>
                options.UseSqlite(dbPath));

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
            builder.Services.AddScoped<INotificationService, NotificationService>();

            builder.Services.AddControllers();
            builder.Services.AddOpenApi();

            // ========== CORS CONFIGURATION ==========
            // Allow the Razor Pages web app to call this API
            // Origins are read from config so they can be set per-environment
            var allowedOrigins = builder.Configuration
                .GetSection("AllowedOrigins")
                .Get<string[]>()
                ?? new[] { "https://localhost:7088", "http://localhost:5245" };

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowWeb", policy =>
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials());
            });

            var app = builder.Build();

            // ========== DATABASE INITIALIZATION ==========
            // Auto-apply migrations and seed data on startup
           using (var scope = app.Services.CreateScope())
                {
                    var services = scope.ServiceProvider;
                    try
                    {
                        var db = services.GetRequiredService<ArcaneVaultDbContext>();
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
                catch (Exception ex)
                    {
                        var logger = services.GetRequiredService<ILogger<Program>>();
                        logger.LogError(ex, "An error occurred while migrating the database.");
                    }
                }

                

            // ========== MIDDLEWARE PIPELINE ==========

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            // Only redirect to HTTPS in development — on AWS the load balancer handles SSL termination
            if (app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseCors("AllowWeb");

            // Authentication & Authorization middleware
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
