// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using ArcaneVault.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArcaneVault.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly ArcaneVaultDbContext _db;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(ArcaneVaultDbContext db, ILogger<CategoriesController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // GET api/categories
        // Returns all categories (public endpoint)
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var categories = await _db.Categories
                    .OrderBy(c => c.CategoryCode)
                    .Select(c => new { c.CategoryCode, c.CategoryName })
                    .ToListAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving all categories: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred retrieving categories." });
            }
        }

        // GET api/categories/{code}
        // Returns a single category by its code (public endpoint)
        [HttpGet("{code}")]
        public async Task<IActionResult> GetByCode(string code)
        {
            try
            {
                var category = await _db.Categories.FindAsync(code);
                if (category == null) return NotFound();
                return Ok(new { category.CategoryCode, category.CategoryName });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving category '{code}': {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred retrieving the category." });
            }
        }

        // POST api/categories
        // Creates a new category (Staff role required)
        [Authorize(Roles = "Staff")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CategoryRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Duplicate category code check
                if (await _db.Categories.AnyAsync(c => c.CategoryCode == request.CategoryCode))
                    return Conflict(new { message = "A category with this code already exists." });

                var category = new ArcaneVaultCategories
                {
                    CategoryCode = request.CategoryCode,
                    CategoryName = request.CategoryName
                };

                _db.Categories.Add(category);
                await _db.SaveChangesAsync();

                _logger.LogInformation($"Category '{request.CategoryCode}' created successfully by user '{User.Identity?.Name}'.");

                return CreatedAtAction(nameof(GetByCode), new { code = category.CategoryCode },
                    new { category.CategoryCode, category.CategoryName });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating category: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred creating the category." });
            }
        }

        // PUT api/categories/{code}
        // Updates a category's name (Staff role required)
        [Authorize(Roles = "Staff")]
        [HttpPut("{code}")]
        public async Task<IActionResult> Update(string code, [FromBody] UpdateCategoryRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var category = await _db.Categories.FindAsync(code);
                if (category == null) return NotFound();

                category.CategoryName = request.CategoryName;
                await _db.SaveChangesAsync();

                _logger.LogInformation($"Category '{code}' updated successfully by user '{User.Identity?.Name}'.");

                return Ok(new { category.CategoryCode, category.CategoryName });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating category '{code}': {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred updating the category." });
            }
        }

        // DELETE api/categories/{code}
        // Deletes a category permanently (Staff role required)
        [Authorize(Roles = "Staff")]
        [HttpDelete("{code}")]
        public async Task<IActionResult> Delete(string code)
        {
            try
            {
                var category = await _db.Categories.FindAsync(code);
                if (category == null) return NotFound();

                _db.Categories.Remove(category);
                await _db.SaveChangesAsync();

                _logger.LogInformation($"Category '{code}' deleted successfully by user '{User.Identity?.Name}'.");

                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError($"Error deleting category '{code}' (likely due to foreign key constraint): {ex.Message}");
                return StatusCode(StatusCodes.Status409Conflict,
                    new { message = "Cannot delete category: it is referenced by collection items. Remove the associations first." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting category '{code}': {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred deleting the category." });
            }
        }
    }

    // DTO: Create category
    public class CategoryRequest
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Category code is required.")]
        [System.ComponentModel.DataAnnotations.StringLength(20,
            ErrorMessage = "Category code must be at most 20 characters.")]
        public string CategoryCode { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Category name is required.")]
        [System.ComponentModel.DataAnnotations.StringLength(100,
            ErrorMessage = "Category name must be at most 100 characters.")]
        public string CategoryName { get; set; } = string.Empty;
    }

    // DTO: Update category
    public class UpdateCategoryRequest
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Category name is required.")]
        [System.ComponentModel.DataAnnotations.StringLength(100,
            ErrorMessage = "Category name must be at most 100 characters.")]
        public string CategoryName { get; set; } = string.Empty;
    }
}
