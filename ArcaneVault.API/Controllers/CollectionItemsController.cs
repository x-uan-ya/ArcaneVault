// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using ArcaneVault.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArcaneVault.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CollectionItemsController : ControllerBase
    {
        private readonly ArcaneVaultDbContext _db;
        private readonly ILogger<CollectionItemsController> _logger;

        public CollectionItemsController(ArcaneVaultDbContext db, ILogger<CollectionItemsController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // GET api/collectionitems?username=xxx&search=yyy
        // Returns all non-deleted items, optionally filtered by user and search term
        // Public endpoint (returns all items to all users)
        /// <summary>
        /// GET /api/collectionitems - Get collection items
        /// Requires authentication. Users can only see their own items unless they are Staff.
        /// </summary>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? user, [FromQuery] string? search)
        {
            try
            {
                var currentUser = User.Identity?.Name;
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(currentUser))
                    return Unauthorized();

                var query = _db.CollectionItems
                    .Include(i => i.CollectionItemCategories!)
                        .ThenInclude(c => c.Category)
                    .Where(i => !i.IsDeleted);

                // Staff can see all items, regular users can only see their own
                if (userRole != "Staff")
                {
                    query = query.Where(i => i.UserName == currentUser);
                }
                else if (!string.IsNullOrWhiteSpace(user))
                {
                    // Staff can filter by username
                    query = query.Where(i => i.UserName == user);
                }

                // Search across ItemName, CategoryName, and username
                if (!string.IsNullOrWhiteSpace(search))
                {
                    string s = search.ToLower();
                    query = query.Where(i =>
                        i.ItemName.ToLower().Contains(s) ||
                        i.UserName.ToLower().Contains(s) ||
                        i.CollectionItemCategories!.Any(c =>
                            c.Category!.CategoryName.ToLower().Contains(s) ||
                            c.CategoryCode.ToLower().Contains(s)));
                }

                var items = await query.OrderBy(i => i.ItemId).ToListAsync();

                return Ok(items.Select(i => new
                {
                    i.ItemId,
                    i.ItemName,
                    i.IsDeleted,
                    i.StartingQuantity,
                    i.CurrentQuantity,
                    i.UserName,
                    Categories = i.CollectionItemCategories!.Select(c => new
                    {
                        c.CategoryCode,
                        CategoryName = c.Category!.CategoryName
                    })
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving collection items: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred retrieving collection items." });
            }
        }

        // GET api/collectionitems/{id}
        // Returns a single item by ID (public endpoint)
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var item = await _db.CollectionItems
                    .Include(i => i.CollectionItemCategories!)
                        .ThenInclude(c => c.Category)
                    .FirstOrDefaultAsync(i => i.ItemId == id && !i.IsDeleted);

                if (item == null) return NotFound();

                return Ok(new
                {
                    item.ItemId,
                    item.ItemName,
                    item.IsDeleted,
                    item.StartingQuantity,
                    item.CurrentQuantity,
                    item.UserName,
                    Categories = item.CollectionItemCategories!.Select(c => new
                    {
                        c.CategoryCode,
                        CategoryName = c.Category!.CategoryName
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving collection item {id}: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred retrieving the collection item." });
            }
        }

        // POST api/collectionitems
        // Creates a new collection item for the logged-in user
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateItemRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Validate quantity constraint: CurrentQuantity <= StartingQuantity
                if (request.CurrentQuantity > request.StartingQuantity)
                    return BadRequest(new { message = "Current quantity cannot exceed starting quantity." });

                // Verify user exists
                if (!await _db.Users.AnyAsync(u => u.UserName == request.UserName && !u.IsDeleted))
                    return BadRequest(new { message = "User not found." });

                // Verify all category codes exist
                if (request.CategoryCodes != null)
                {
                    foreach (var code in request.CategoryCodes)
                    {
                        if (!await _db.Categories.AnyAsync(c => c.CategoryCode == code))
                            return BadRequest(new { message = $"Category code '{code}' does not exist." });
                    }
                }

                var item = new ArcaneVaultCollectionItems
                {
                    ItemName = request.ItemName,
                    StartingQuantity = request.StartingQuantity,
                    CurrentQuantity = request.CurrentQuantity,
                    IsDeleted = false,
                    UserName = request.UserName
                };

                _db.CollectionItems.Add(item);
                await _db.SaveChangesAsync();

                // Add category links
                if (request.CategoryCodes != null)
                {
                    foreach (var code in request.CategoryCodes.Distinct())
                    {
                        _db.CollectionItemCategories.Add(new ArcaneVaultCollectionItemCategories
                        {
                            ItemId = item.ItemId,
                            CategoryCode = code
                        });
                    }
                    await _db.SaveChangesAsync();
                }

                _logger.LogInformation($"Collection item '{request.ItemName}' created for user '{request.UserName}' by '{User.Identity?.Name}'.");

                return CreatedAtAction(nameof(GetById), new { id = item.ItemId },
                    new { item.ItemId, item.ItemName, item.UserName });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating collection item: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred creating the collection item." });
            }
        }

        // PUT api/collectionitems/{id}
        // Updates an existing collection item (owner or Staff only)
        [Authorize]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateItemRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var item = await _db.CollectionItems
                    .Include(i => i.CollectionItemCategories)
                    .FirstOrDefaultAsync(i => i.ItemId == id && !i.IsDeleted);

                if (item == null) return NotFound();

                // Authorization: check if user is owner or staff
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                var currentUsername = User.Identity?.Name;

                if (item.UserName != currentUsername && userRole != "Staff")
                {
                    _logger.LogWarning($"Unauthorized update attempt on item {id} by user '{currentUsername}'.");
                    return Forbid("You do not have permission to update this item.");
                }

                // Validate quantity constraint: CurrentQuantity <= StartingQuantity
                if (request.CurrentQuantity > item.StartingQuantity)
                    return BadRequest(new { message = "Current quantity cannot exceed starting quantity." });

                // Verify category codes
                if (request.CategoryCodes != null)
                {
                    foreach (var code in request.CategoryCodes)
                    {
                        if (!await _db.Categories.AnyAsync(c => c.CategoryCode == code))
                            return BadRequest(new { message = $"Category code '{code}' does not exist." });
                    }
                }

                item.ItemName = request.ItemName;
                item.CurrentQuantity = request.CurrentQuantity;

                // Replace categories
                _db.CollectionItemCategories.RemoveRange(item.CollectionItemCategories!);
                if (request.CategoryCodes != null)
                {
                    foreach (var code in request.CategoryCodes.Distinct())
                    {
                        _db.CollectionItemCategories.Add(new ArcaneVaultCollectionItemCategories
                        {
                            ItemId = item.ItemId,
                            CategoryCode = code
                        });
                    }
                }

                await _db.SaveChangesAsync();

                _logger.LogInformation($"Collection item {id} updated successfully by user '{currentUsername}'.");

                return Ok(new { item.ItemId, item.ItemName, item.CurrentQuantity });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating collection item {id}: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred updating the collection item." });
            }
        }

        // DELETE api/collectionitems/{id}
        // Soft-deletes a collection item (owner or Staff only)
        [Authorize]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var item = await _db.CollectionItems
                    .FirstOrDefaultAsync(i => i.ItemId == id && !i.IsDeleted);

                if (item == null) return NotFound();

                // Authorization: check if user is owner or staff
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                var currentUsername = User.Identity?.Name;

                if (item.UserName != currentUsername && userRole != "Staff")
                {
                    _logger.LogWarning($"Unauthorized delete attempt on item {id} by user '{currentUsername}'.");
                    return Forbid("You do not have permission to delete this item.");
                }

                item.IsDeleted = true;
                await _db.SaveChangesAsync();

                _logger.LogInformation($"Collection item {id} deleted by user '{currentUsername}'.");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting collection item {id}: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred deleting the collection item." });
            }
        }
    }

    // DTO: Create item
    public class CreateItemRequest
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Item name is required.")]
        [System.ComponentModel.DataAnnotations.StringLength(200)]
        public string ItemName { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Range(0, int.MaxValue)]
        public int StartingQuantity { get; set; }

        [System.ComponentModel.DataAnnotations.Range(0, int.MaxValue)]
        public int CurrentQuantity { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public string UserName { get; set; } = string.Empty;

        public List<string>? CategoryCodes { get; set; }
    }

    // DTO: Update item
    public class UpdateItemRequest
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Item name is required.")]
        [System.ComponentModel.DataAnnotations.StringLength(200)]
        public string ItemName { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Range(0, int.MaxValue)]
        public int CurrentQuantity { get; set; }

        public List<string>? CategoryCodes { get; set; }
    }
}
