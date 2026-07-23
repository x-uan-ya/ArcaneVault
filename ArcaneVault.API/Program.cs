// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using ArcaneVault.API.Data;
using ArcaneVault.API.Services;
using Amazon.RDS.Util;
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
            if (builder.Environment.IsDevelopment())
            {
                // ---- LOCAL DEV: SQLite (no PostgreSQL install required) ----
                var sqlitePath = builder.Configuration.GetConnectionString("DefaultConnection")
                    ?? "Data Source=ArcaneVault.db";

                builder.Services.AddDbContext<ArcaneVaultDbContext>(options =>
                    options.UseSqlite(sqlitePath));
            }
            else
            {
                // ---- PRODUCTION: PostgreSQL on AWS RDS with IAM auth token ----
                var rdsHost = builder.Configuration["RDS:Host"]
                    ?? "database-1.cluster-cmdu886so4ms.us-east-1.rds.amazonaws.com";
                var rdsPort = int.Parse(builder.Configuration["RDS:Port"] ?? "5432");
                var rdsUser = builder.Configuration["RDS:User"] ?? "postgres";
                var rdsRegion = builder.Configuration["RDS:Region"] ?? "us-east-1";
                var rdsDb = builder.Configuration["RDS:Database"] ?? "arcanevault";

                var authToken = RDSAuthTokenGenerator.GenerateAuthToken(
                    Amazon.RegionEndpoint.GetBySystemName(rdsRegion),
                    rdsHost, rdsPort, rdsUser);

                var pgConnectionString = $"Host={rdsHost};Port={rdsPort};Database={rdsDb};Username={rdsUser};Password={authToken};SSL Mode=Require;Trust Server Certificate=true";

                builder.Services.AddDbContext<ArcaneVaultDbContext>(options =>
                    options.UseNpgsql(pgConnectionString));
            }

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
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ArcaneVaultDbContext>();

                if (app.Environment.IsDevelopment())
                {
                    // SQLite: create the database file and all tables if they don't exist yet
                    db.Database.EnsureCreated();
                }
                else
                {
                    // PostgreSQL on AWS: apply any pending EF migrations
                    db.Database.Migrate();
                }

                // Ensure the seeded admin password is valid (verify, don't compare hashes directly —
                // BCrypt hashes are non-deterministic so two hashes of the same password never match)
                var adminUser = db.Users.FirstOrDefault(u => u.UserName == "admin");
                if (adminUser != null && !BCrypt.Net.BCrypt.Verify("Admin@123", adminUser.PasswordHash))
                {
                    adminUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
                    db.Users.Update(adminUser);
                    db.SaveChanges();
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
        }
    }
}
