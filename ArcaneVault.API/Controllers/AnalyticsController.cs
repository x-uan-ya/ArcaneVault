// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using ArcaneVault.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArcaneVault.API.Controllers
{
    /// <summary>
    /// Analytics endpoints providing insights into collections and user activity.
    /// All endpoints require authentication and Staff role.
    /// </summary>
    [Authorize(Roles = "Staff")]
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly ArcaneVaultDbContext _db;
        private readonly ILogger<AnalyticsController> _logger;

        public AnalyticsController(ArcaneVaultDbContext db, ILogger<AnalyticsController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // GET api/analytics/summary
        // Returns overall platform statistics (Staff only)
        [HttpGet("summary")]
        public async Task<IActionResult> Summary()
        {
            try
            {
                int totalUsers = await _db.Users.CountAsync(u => !u.IsDeleted && u.RoleId == 1);
                int totalItems = await _db.CollectionItems.CountAsync(i => !i.IsDeleted);
                int totalCategories = await _db.Categories.CountAsync();

                _logger.LogInformation($"Analytics summary retrieved by user '{User.Identity?.Name}'.");

                return Ok(new { totalUsers, totalItems, totalCategories });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving analytics summary: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred retrieving analytics." });
            }
        }

        // GET api/analytics/items-per-category
        // Returns item count grouped by category (for bar/pie chart) (Staff only)
        [HttpGet("items-per-category")]
        public async Task<IActionResult> ItemsPerCategory()
        {
            try
            {
                var data = await _db.CollectionItemCategories
                    .Where(c => !c.CollectionItem!.IsDeleted)
                    .GroupBy(c => new { c.CategoryCode, c.Category!.CategoryName })
                    .Select(g => new
                    {
                        categoryCode = g.Key.CategoryCode,
                        categoryName = g.Key.CategoryName,
                        itemCount = g.Count()
                    })
                    .OrderByDescending(x => x.itemCount)
                    .ToListAsync();

                _logger.LogInformation($"Items per category analytics retrieved by user '{User.Identity?.Name}'.");

                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving items per category: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred retrieving analytics." });
            }
        }

        // GET api/analytics/top-collectors
        // Returns top 10 users by total current quantity of items (Staff only)
        [HttpGet("top-collectors")]
        public async Task<IActionResult> TopCollectors()
        {
            try
            {
                var data = await _db.CollectionItems
                    .Where(i => !i.IsDeleted)
                    .GroupBy(i => i.UserName)
                    .Select(g => new
                    {
                        userName = g.Key,
                        totalItems = g.Count(),
                        totalCurrentQty = g.Sum(i => i.CurrentQuantity)
                    })
                    .OrderByDescending(x => x.totalCurrentQty)
                    .Take(10)
                    .ToListAsync();

                _logger.LogInformation($"Top collectors analytics retrieved by user '{User.Identity?.Name}'.");

                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving top collectors: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred retrieving analytics." });
            }
        }

        // GET api/analytics/collection-growth
        // Returns starting vs current quantity per category (for comparison chart) (Staff only)
        [HttpGet("collection-growth")]
        public async Task<IActionResult> CollectionGrowth()
        {
            try
            {
                var data = await _db.CollectionItemCategories
                    .Where(c => !c.CollectionItem!.IsDeleted)
                    .GroupBy(c => new { c.CategoryCode, c.Category!.CategoryName })
                    .Select(g => new
                    {
                        categoryName = g.Key.CategoryName,
                        totalStarting = g.Sum(c => c.CollectionItem!.StartingQuantity),
                        totalCurrent = g.Sum(c => c.CollectionItem!.CurrentQuantity)
                    })
                    .OrderBy(x => x.categoryName)
                    .ToListAsync();

                _logger.LogInformation($"Collection growth analytics retrieved by user '{User.Identity?.Name}'.");

                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving collection growth: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred retrieving analytics." });
            }
        }

        // GET api/analytics/export-csv
        // Exports items per category as CSV (Staff only)
        [HttpGet("export-csv")]
        public async Task<IActionResult> ExportCSV()
        {
            try
            {
                var data = await _db.CollectionItemCategories
                    .Where(c => !c.CollectionItem!.IsDeleted)
                    .GroupBy(c => new { c.CategoryCode, c.Category!.CategoryName })
                    .Select(g => new
                    {
                        categoryCode = g.Key.CategoryCode,
                        categoryName = g.Key.CategoryName,
                        itemCount = g.Count(),
                        totalStarting = g.Sum(c => c.CollectionItem!.StartingQuantity),
                        totalCurrent = g.Sum(c => c.CollectionItem!.CurrentQuantity)
                    })
                    .OrderByDescending(x => x.itemCount)
                    .ToListAsync();

                // Generate CSV content
                var csv = new System.Text.StringBuilder();
                csv.AppendLine("Category Code,Category Name,Item Count,Total Starting Qty,Total Current Qty");

                foreach (var item in data)
                {
                    csv.AppendLine($"\"{item.categoryCode}\",\"{item.categoryName}\",{item.itemCount},{item.totalStarting},{item.totalCurrent}");
                }

                _logger.LogInformation($"Analytics CSV exported by user '{User.Identity?.Name}'.");

                var fileBytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
                return File(fileBytes, "text/csv", $"analytics-export-{DateTime.UtcNow:yyyy-MM-dd-HHmmss}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error exporting analytics CSV: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred exporting analytics." });
            }
        }
    }
}
