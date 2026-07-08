// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using ArcaneVault.API.Data;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ArcaneVault.API.Services
{
    /// <summary>
    /// Centralized service for user authentication and JWT token generation/validation.
    /// Handles user authentication and token-based authorization.
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// Validates user credentials and returns a JWT token if valid.
        /// </summary>
        Task<(bool success, string? token, string? message)> AuthenticateAsync(string username, string password);

        /// <summary>
        /// Validates a JWT token and extracts claims.
        /// </summary>
        (bool success, ClaimsPrincipal? principal) ValidateToken(string token);

        /// <summary>
        /// Gets the user's role from their username.
        /// </summary>
        Task<string?> GetUserRoleAsync(string username);

        /// <summary>
        /// Checks if a user has a specific role.
        /// </summary>
        Task<bool> UserHasRoleAsync(string username, string roleName);
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly ArcaneVaultDbContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthenticationService> _logger;

        // JWT secret (should be at least 32 characters)
        private string JwtSecret => _config["JwtSecret"] ?? "YourVerySecureSecretKeyThatIsAtLeast32CharactersLongForHS256!";
        private string JwtIssuer => _config["JwtIssuer"] ?? "ArcaneVault";
        private string JwtAudience => _config["JwtAudience"] ?? "ArcaneVaultUsers";

        public AuthenticationService(ArcaneVaultDbContext db, IConfiguration config, ILogger<AuthenticationService> logger)
        {
            _db = db;
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// Authenticates a user and generates a JWT token.
        /// </summary>
        public async Task<(bool success, string? token, string? message)> AuthenticateAsync(string username, string password)
        {
            try
            {
                // Find user in database
                var user = await _db.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserName == username && !u.IsDeleted);

                if (user == null)
                {
                    _logger.LogWarning($"Login attempt failed: User '{username}' not found.");
                    return (false, null, "Invalid username or password.");
                }

                // Verify password
                if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                {
                    _logger.LogWarning($"Login attempt failed: Invalid password for user '{username}'.");
                    return (false, null, "Invalid username or password.");
                }

                // Generate JWT token
                var token = GenerateJwtToken(user);
                _logger.LogInformation($"User '{username}' successfully authenticated.");

                return (true, token, null);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Authentication error for user '{username}': {ex.Message}");
                return (false, null, "An error occurred during authentication.");
            }
        }

        /// <summary>
        /// Validates a JWT token and extracts claims.
        /// </summary>
        public (bool success, ClaimsPrincipal? principal) ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(JwtSecret);

                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = JwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = JwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return (true, principal);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Token validation failed: {ex.Message}");
                return (false, null);
            }
        }

        /// <summary>
        /// Gets the user's role name.
        /// </summary>
        public async Task<string?> GetUserRoleAsync(string username)
        {
            try
            {
                var user = await _db.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserName == username && !u.IsDeleted);

                return user?.Role?.RoleName;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving role for user '{username}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Checks if a user has a specific role.
        /// </summary>
        public async Task<bool> UserHasRoleAsync(string username, string roleName)
        {
            try
            {
                var user = await _db.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserName == username && !u.IsDeleted);

                return user?.Role?.RoleName == roleName;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking role for user '{username}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Generates a JWT token for the authenticated user.
        /// </summary>
        private string GenerateJwtToken(ArcaneVaultUsers user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(JwtSecret);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "User")
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(24),
                Issuer = JwtIssuer,
                Audience = JwtAudience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
