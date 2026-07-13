// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using ArcaneVault.API.Data;
using ArcaneVault.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArcaneVault.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ArcaneVaultDbContext _db;
        private readonly IAuthenticationService _authService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(ArcaneVaultDbContext db, IAuthenticationService authService, ILogger<UsersController> logger)
        {
            _db = db;
            _authService = authService;
            _logger = logger;
        }

        // POST api/users/register
        // Creates a new User-role account
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Check if username is taken by an ACTIVE (non-deleted) account
                bool userNameExists = await _db.Users
                    .AnyAsync(u => u.UserName == request.UserName && !u.IsDeleted);
                if (userNameExists)
                    return Conflict(new { message = "This username is already taken. Please choose a different username." });

                // Check if email is taken by an ACTIVE (non-deleted) account
                bool emailExists = await _db.Users
                    .AnyAsync(u => u.Email.ToLower() == request.Email.ToLower() && !u.IsDeleted);
                if (emailExists)
                    return Conflict(new { message = "An account with this email already exists." });

                // If a deleted account exists with same username/email, reuse it
                var deletedUser = await _db.Users
                    .FirstOrDefaultAsync(u => 
                        (u.UserName == request.UserName || u.Email.ToLower() == request.Email.ToLower()) 
                        && u.IsDeleted);

                if (deletedUser != null)
                {
                    // Reactivate the deleted account slot with new credentials
                    deletedUser.UserName = request.UserName;
                    deletedUser.Email = request.Email;
                    deletedUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                    deletedUser.IsDeleted = false;
                    deletedUser.RoleId = 1;
                    await _db.SaveChangesAsync();

                    _logger.LogInformation($"User '{request.UserName}' re-registered successfully.");
                    return CreatedAtAction(nameof(GetByUsername), new { username = deletedUser.UserName },
                        new { deletedUser.UserName, deletedUser.Email, deletedUser.RoleId });
                }

                // Create brand new account
                var user = new ArcaneVaultUsers
                {
                    UserName = request.UserName,
                    Email = request.Email,
                    IsDeleted = false,
                    RoleId = 1,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
                };

                _db.Users.Add(user);
                await _db.SaveChangesAsync();

                _logger.LogInformation($"User '{request.UserName}' registered successfully.");

                return CreatedAtAction(nameof(GetByUsername), new { username = user.UserName },
                    new { user.UserName, user.Email, user.RoleId });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during registration for user '{request.UserName}': {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "An error occurred during registration." });
            }
        }

        // POST api/users/login
        // Validates credentials and returns JWT token
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var (success, token, message) = await _authService.AuthenticateAsync(request.UserName, request.Password);

                if (!success)
                    return Unauthorized(new { message });

                var user = await _db.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserName == request.UserName && !u.IsDeleted);

                return Ok(new
                {
                    user?.UserName,
                    user?.Email,
                    user?.RoleId,
                    RoleName = user?.Role?.RoleName,
                    Token = token  // Capitalized to match LoginResponse in web
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during login for user '{request.UserName}': {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred during login." });
            }
        }

        // GET api/users/{username}
        // Returns user profile information (public endpoint)
        [HttpGet("{username}")]
        public async Task<IActionResult> GetByUsername(string username)
        {
            try
            {
                var user = await _db.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserName == username && !u.IsDeleted);

                if (user == null) return NotFound();

                return Ok(new
                {
                    user.UserName,
                    user.Email,
                    user.RoleId,
                    RoleName = user.Role!.RoleName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving user '{username}': {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred retrieving user information." });
            }
        }

        // DELETE api/users/me
        // Soft-deletes the currently authenticated user's account
        [Authorize]
        [HttpDelete("me")]
        public async Task<IActionResult> DeleteMyAccount()
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized();

                var user = await _db.Users
                    .FirstOrDefaultAsync(u => u.UserName == username && !u.IsDeleted);

                if (user == null)
                    return NotFound(new { message = "User not found." });

                // Soft delete: mark as deleted instead of removing from DB
                user.IsDeleted = true;
                await _db.SaveChangesAsync();

                _logger.LogInformation($"Account '{username}' deleted by user.");

                return Ok(new { message = "Account deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting account: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred deleting your account." });
            }
        }
    }

    // DTO: Register
    public class RegisterRequest
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Username is required.")]
        [System.ComponentModel.DataAnnotations.StringLength(50, MinimumLength = 3,
            ErrorMessage = "Username must be 3–50 characters.")]
        public string UserName { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Email is required.")]
        [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Password is required.")]
        [System.ComponentModel.DataAnnotations.StringLength(100, MinimumLength = 6,
            ErrorMessage = "Password must be at least 6 characters.")]
        public string Password { get; set; } = string.Empty;
    }

    // DTO: Login
    public class LoginRequest
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Username is required.")]
        public string UserName { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = string.Empty;
    }
}
