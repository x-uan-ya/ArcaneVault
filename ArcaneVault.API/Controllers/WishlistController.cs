// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using ArcaneVault.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArcaneVault.API.Controllers
{
    /// <summary>
    /// Wishlist controller for saving favorite collectibles.
    /// Users can add items to their wishlist and see when they become available in marketplace.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WishlistController : ControllerBase
    {
        private readonly ArcaneVaultDbContext _db;
        private readonly ILogger<WishlistController> _logger;

        public WishlistController(ArcaneVaultDbContext db, ILogger<WishlistController> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// GET /api/wishlist - Get current user's wishlist
        /// Shows if items are available in marketplace
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMyWishlist()
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized();

                var wishlist = await _db.Wishlists
                    .Include(w => w.CollectionItem)
                        .ThenInclude(i => i!.CollectionItemCategories!)
                            .ThenInclude(c => c.Category)
                    .Where(w => w.UserName == username && !w.IsDeleted)
                    .OrderByDescending(w => w.CreatedDate)
                    .ToListAsync();

                var result = new List<object>();

                foreach (var item in wishlist)
                {
                    // Check if item is available in marketplace (not owned by current user)
                    var availableListing = await _db.MarketplaceListings
                        .Where(m => m.ItemId == item.ItemId &&
                                   m.Status == "Active" &&
                                   m.SellerUserName != username &&
                                   !m.IsDeleted)
                        .FirstOrDefaultAsync();

                    result.Add(new
                    {
                        item.WishlistId,
                        item.ItemId,
                        ItemName = item.CollectionItem!.ItemName,
                        Categories = item.CollectionItem.CollectionItemCategories!
                            .Select(c => new { c.CategoryCode, c.Category!.CategoryName }),
                        item.CreatedDate,
                        IsAvailableNow = availableListing != null,
                        AvailableListingId = availableListing?.ListingId,
                        AvailablePrice = availableListing?.AskingPrice,
                        AvailableSeller = availableListing?.SellerUserName
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving wishlist: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred retrieving your wishlist." });
            }
        }

        /// <summary>
        /// POST /api/wishlist - Add item to wishlist
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddToWishlist([FromBody] AddWishlistRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized();

                // Check if item exists
                var item = await _db.CollectionItems
                    .FirstOrDefaultAsync(i => i.ItemId == request.ItemId && !i.IsDeleted);

                if (item == null)
                    return NotFound(new { message = "Item not found." });

                // Check if already in wishlist
                var existing = await _db.Wishlists
                    .FirstOrDefaultAsync(w => w.UserName == username && 
                                             w.ItemId == request.ItemId && 
                                             !w.IsDeleted);

                if (existing != null)
                    return Conflict(new { message = "Item is already in your wishlist." });

                // Check if user already owns this item
                var userOwnsItem = await _db.CollectionItems
                    .AnyAsync(i => i.ItemId == request.ItemId && 
                                  i.UserName == username && 
                                  !i.IsDeleted);

                if (userOwnsItem)
                    return BadRequest(new { message = "You already own this item." });

                var wishlistItem = new Wishlist
                {
                    UserName = username,
                    ItemId = request.ItemId,
                    CreatedDate = DateTime.UtcNow
                };

                _db.Wishlists.Add(wishlistItem);
                await _db.SaveChangesAsync();

                _logger.LogInformation($"User '{username}' added item {request.ItemId} to wishlist.");

                return CreatedAtAction(nameof(GetMyWishlist), new { id = wishlistItem.WishlistId },
                    new { wishlistItem.WishlistId, wishlistItem.ItemId, wishlistItem.CreatedDate });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding to wishlist: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred adding to wishlist." });
            }
        }

        /// <summary>
        /// DELETE /api/wishlist/{id} - Remove item from wishlist
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveFromWishlist(int id)
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized();

                var wishlistItem = await _db.Wishlists
                    .FirstOrDefaultAsync(w => w.WishlistId == id && !w.IsDeleted);

                if (wishlistItem == null)
                    return NotFound();

                // Authorization: only owner can remove
                if (wishlistItem.UserName != username)
                    return Forbid();

                wishlistItem.IsDeleted = true;
                await _db.SaveChangesAsync();

                _logger.LogInformation($"User '{username}' removed item from wishlist (ID: {id}).");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error removing from wishlist: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred removing from wishlist." });
            }
        }
    }

    public class AddWishlistRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        public int ItemId { get; set; }
    }
}
