// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using ArcaneVault.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ArcaneVault.API.Controllers
{
    /// <summary>
    /// Marketplace controller for buy/sell/trade functionality.
    /// Handles listings, offers, and transaction completion.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class MarketplaceController : ControllerBase
    {
        private readonly ArcaneVaultDbContext _db;
        private readonly ILogger<MarketplaceController> _logger;

        public MarketplaceController(ArcaneVaultDbContext db, ILogger<MarketplaceController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ==================== MARKETPLACE LISTINGS ====================

        /// <summary>
        /// GET /api/marketplace - Browse all active marketplace listings
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllListings(
            [FromQuery] string? search,
            [FromQuery] string? listingType,
            [FromQuery] string? category,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] string? seller)
        {
            try
            {
                var query = _db.MarketplaceListings
                    .Include(m => m.CollectionItem)
                        .ThenInclude(i => i!.CollectionItemCategories!)
                            .ThenInclude(c => c.Category)
                    .Where(m => !m.IsDeleted && m.Status == "Active");

                // Search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    string s = search.ToLower();
                    query = query.Where(m =>
                        m.Title.ToLower().Contains(s) ||
                        m.Description.ToLower().Contains(s) ||
                        m.CollectionItem!.ItemName.ToLower().Contains(s));
                }

                // Listing type filter
                if (!string.IsNullOrWhiteSpace(listingType))
                    query = query.Where(m => m.ListingType == listingType);

                // Category filter
                if (!string.IsNullOrWhiteSpace(category))
                    query = query.Where(m => m.CollectionItem!.CollectionItemCategories!
                        .Any(c => c.CategoryCode == category));

                // Price range filter
                if (minPrice.HasValue)
                    query = query.Where(m => m.AskingPrice >= minPrice.Value);
                if (maxPrice.HasValue)
                    query = query.Where(m => m.AskingPrice <= maxPrice.Value);

                // Seller filter
                if (!string.IsNullOrWhiteSpace(seller))
                    query = query.Where(m => m.SellerUserName == seller);

                var listings = await query
                    .OrderByDescending(m => m.ListedDate)
                    .Select(m => new
                    {
                        m.ListingId,
                        m.ItemId,
                        m.Title,
                        m.Description,
                        m.AskingPrice,
                        m.ListingType,
                        m.TradePreferences,
                        m.QuantityAvailable,
                        m.Status,
                        m.ListedDate,
                        m.ExpirationDate,
                        m.ViewCount,
                        m.IsFeatured,
                        SellerUserName = m.SellerUserName,
                        ItemName = m.CollectionItem!.ItemName,
                        Categories = m.CollectionItem!.CollectionItemCategories!
                            .Select(c => new { c.CategoryCode, c.Category!.CategoryName }),
                        OfferCount = m.Offers.Count(o => !o.IsDeleted && o.Status == "Pending")
                    })
                    .ToListAsync();

                return Ok(listings);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving marketplace listings: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred retrieving marketplace listings." });
            }
        }

        /// <summary>
        /// GET /api/marketplace/{id} - Get specific listing details
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetListing(int id)
        {
            try
            {
                var listing = await _db.MarketplaceListings
                    .Include(m => m.CollectionItem)
                        .ThenInclude(i => i!.CollectionItemCategories!)
                            .ThenInclude(c => c.Category)
                    .Include(m => m.Offers.Where(o => !o.IsDeleted))
                    .FirstOrDefaultAsync(m => m.ListingId == id && !m.IsDeleted);

                if (listing == null)
                    return NotFound();

                // Increment view count
                listing.ViewCount++;
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    listing.ListingId,
                    listing.ItemId,
                    listing.Title,
                    listing.Description,
                    listing.AskingPrice,
                    listing.ListingType,
                    listing.TradePreferences,
                    listing.QuantityAvailable,
                    listing.Status,
                    listing.ListedDate,
                    listing.ExpirationDate,
                    listing.ViewCount,
                    listing.IsFeatured,
                    SellerUserName = listing.SellerUserName,
                    Item = new
                    {
                        listing.CollectionItem!.ItemId,
                        listing.CollectionItem.ItemName,
                        listing.CollectionItem.StartingQuantity,
                        listing.CollectionItem.CurrentQuantity,
                        Categories = listing.CollectionItem.CollectionItemCategories!
                            .Select(c => new { c.CategoryCode, c.Category!.CategoryName })
                    },
                    PendingOfferCount = listing.Offers.Count(o => o.Status == "Pending")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving listing {id}: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred retrieving the listing." });
            }
        }

        /// <summary>
        /// GET /api/marketplace/my-listings - Get current user's listings
        /// </summary>
        [Authorize]
        [HttpGet("my-listings")]
        public async Task<IActionResult> GetMyListings()
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized();

                var listings = await _db.MarketplaceListings
                    .Include(m => m.CollectionItem)
                    .Where(m => m.SellerUserName == username && !m.IsDeleted)
                    .OrderByDescending(m => m.ListedDate)
                    .Select(m => new
                    {
                        m.ListingId,
                        m.Title,
                        m.AskingPrice,
                        m.ListingType,
                        m.QuantityAvailable,
                        m.Status,
                        m.ListedDate,
                        m.ViewCount,
                        ItemName = m.CollectionItem!.ItemName,
                        PendingOffers = m.Offers.Count(o => !o.IsDeleted && o.Status == "Pending")
                    })
                    .ToListAsync();

                return Ok(listings);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving user listings: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred retrieving your listings." });
            }
        }

        /// <summary>
        /// POST /api/marketplace - Create a new listing
        /// </summary>
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateListing([FromBody] CreateListingRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized();

                // Verify item exists and belongs to user
                var item = await _db.CollectionItems
                    .FirstOrDefaultAsync(i => i.ItemId == request.ItemId && 
                                             i.UserName == username && 
                                             !i.IsDeleted);

                if (item == null)
                    return BadRequest(new { message = "Item not found or you don't own it." });

                // Verify item not already listed
                var existingListing = await _db.MarketplaceListings
                    .AnyAsync(m => m.ItemId == request.ItemId && 
                                   m.Status == "Active" && 
                                   !m.IsDeleted);

                if (existingListing)
                    return BadRequest(new { message = "This item is already listed in the marketplace." });

                // Validate quantity
                if (request.QuantityAvailable > item.CurrentQuantity)
                    return BadRequest(new { message = "Quantity available cannot exceed current item quantity." });

                var listing = new MarketplaceListing
                {
                    ItemId = request.ItemId,
                    SellerUserName = username,
                    Title = string.IsNullOrWhiteSpace(request.Title) ? item.ItemName : request.Title,
                    Description = request.Description ?? "",
                    AskingPrice = request.AskingPrice,
                    ListingType = request.ListingType,
                    TradePreferences = request.TradePreferences,
                    QuantityAvailable = request.QuantityAvailable,
                    Status = "Active",
                    ListedDate = DateTime.UtcNow,
                    ExpirationDate = request.ExpirationDays.HasValue 
                        ? DateTime.UtcNow.AddDays(request.ExpirationDays.Value) 
                        : null
                };

                _db.MarketplaceListings.Add(listing);
                await _db.SaveChangesAsync();

                _logger.LogInformation($"Listing created by '{username}': {listing.Title}");

                return CreatedAtAction(nameof(GetListing), new { id = listing.ListingId },
                    new { listing.ListingId, listing.Title, listing.Status });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating listing: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred creating the listing." });
            }
        }

        /// <summary>
        /// DELETE /api/marketplace/{id} - Cancel/remove a listing
        /// </summary>
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> CancelListing(int id)
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized();

                var listing = await _db.MarketplaceListings
                    .FirstOrDefaultAsync(m => m.ListingId == id && !m.IsDeleted);

                if (listing == null)
                    return NotFound();

                // Authorization: owner or staff
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                if (listing.SellerUserName != username && userRole != "Staff")
                    return Forbid();

                listing.Status = "Cancelled";
                listing.IsDeleted = true;
                await _db.SaveChangesAsync();

                _logger.LogInformation($"Listing {id} cancelled by '{username}'.");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error cancelling listing {id}: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred cancelling the listing." });
            }
        }

        // ==================== OFFERS ====================

        /// <summary>
        /// POST /api/marketplace/{listingId}/offers - Make an offer on a listing
        /// </summary>
        [Authorize]
        [HttpPost("{listingId}/offers")]
        public async Task<IActionResult> MakeOffer(int listingId, [FromBody] CreateOfferRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized();

                // Verify listing exists and is active
                var listing = await _db.MarketplaceListings
                    .FirstOrDefaultAsync(m => m.ListingId == listingId && 
                                             m.Status == "Active" && 
                                             !m.IsDeleted);

                if (listing == null)
                    return NotFound(new { message = "Listing not found or not active." });

                // Can't make offer on own listing
                if (listing.SellerUserName == username)
                    return BadRequest(new { message = "You cannot make an offer on your own listing." });

                // Validate quantity
                if (request.QuantityRequested > listing.QuantityAvailable)
                    return BadRequest(new { message = "Requested quantity exceeds available quantity." });

                // If trade offer, verify trade item exists and belongs to user
                if (request.OfferType == "Trade" && request.TradeItemId.HasValue)
                {
                    var tradeItem = await _db.CollectionItems
                        .FirstOrDefaultAsync(i => i.ItemId == request.TradeItemId && 
                                                 i.UserName == username && 
                                                 !i.IsDeleted);

                    if (tradeItem == null)
                        return BadRequest(new { message = "Trade item not found or you don't own it." });
                }

                var offer = new Offer
                {
                    ListingId = listingId,
                    BuyerUserName = username,
                    OfferType = request.OfferType,
                    OfferedPrice = request.OfferedPrice,
                    TradeItemId = request.TradeItemId,
                    QuantityRequested = request.QuantityRequested,
                    Message = request.Message,
                    Status = "Pending",
                    OfferedDate = DateTime.UtcNow,
                    ExpirationDate = DateTime.UtcNow.AddDays(7) // Offers expire in 7 days
                };

                _db.Offers.Add(offer);
                await _db.SaveChangesAsync();

                _logger.LogInformation($"Offer made by '{username}' on listing {listingId}.");

                return CreatedAtAction(nameof(GetOffer), new { id = offer.OfferId },
                    new { offer.OfferId, offer.Status, offer.OfferedDate });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating offer: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred creating the offer." });
            }
        }

        /// <summary>
        /// GET /api/marketplace/offers/{id} - Get specific offer details
        /// </summary>
        [Authorize]
        [HttpGet("offers/{id}")]
        public async Task<IActionResult> GetOffer(int id)
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized();

                var offer = await _db.Offers
                    .Include(o => o.Listing)
                    .Include(o => o.TradeItem)
                    .FirstOrDefaultAsync(o => o.OfferId == id && !o.IsDeleted);

                if (offer == null)
                    return NotFound();

                // Authorization: must be buyer, seller, or staff
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                if (offer.BuyerUserName != username && 
                    offer.Listing!.SellerUserName != username && 
                    userRole != "Staff")
                    return Forbid();

                return Ok(new
                {
                    offer.OfferId,
                    offer.ListingId,
                    offer.BuyerUserName,
                    offer.OfferType,
                    offer.OfferedPrice,
                    offer.TradeItemId,
                    TradeItemName = offer.TradeItem?.ItemName,
                    offer.QuantityRequested,
                    offer.Message,
                    offer.Status,
                    offer.OfferedDate,
                    offer.ResponseDate,
                    offer.SellerResponse,
                    offer.ExpirationDate,
                    ListingTitle = offer.Listing!.Title,
                    SellerUserName = offer.Listing.SellerUserName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving offer {id}: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred retrieving the offer." });
            }
        }

        /// <summary>
        /// GET /api/marketplace/my-offers - Get offers made by current user
        /// </summary>
        [Authorize]
        [HttpGet("my-offers")]
        public async Task<IActionResult> GetMyOffers()
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized();

                var offers = await _db.Offers
                    .Include(o => o.Listing)
                    .Where(o => o.BuyerUserName == username && !o.IsDeleted)
                    .OrderByDescending(o => o.OfferedDate)
                    .Select(o => new
                    {
                        o.OfferId,
                        o.ListingId,
                        ListingTitle = o.Listing!.Title,
                        o.OfferType,
                        o.OfferedPrice,
                        o.QuantityRequested,
                        o.Status,
                        o.OfferedDate,
                        o.ResponseDate,
                        SellerUserName = o.Listing.SellerUserName
                    })
                    .ToListAsync();

                return Ok(offers);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving user offers: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred retrieving your offers." });
            }
        }

        /// <summary>
        /// GET /api/marketplace/offers-received - Get offers received on user's listings
        /// </summary>
        [Authorize]
        [HttpGet("offers-received")]
        public async Task<IActionResult> GetOffersReceived()
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized();

                var offers = await _db.Offers
                    .Include(o => o.Listing)
                    .Where(o => o.Listing!.SellerUserName == username && !o.IsDeleted)
                    .OrderByDescending(o => o.OfferedDate)
                    .Select(o => new
                    {
                        o.OfferId,
                        o.ListingId,
                        ListingTitle = o.Listing!.Title,
                        o.BuyerUserName,
                        o.OfferType,
                        o.OfferedPrice,
                        o.QuantityRequested,
                        o.Status,
                        o.OfferedDate,
                        o.Message
                    })
                    .ToListAsync();

                return Ok(offers);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving received offers: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred retrieving received offers." });
            }
        }

        /// <summary>
        /// PUT /api/marketplace/offers/{id}/accept - Accept an offer
        /// </summary>
        [Authorize]
        [HttpPut("offers/{id}/accept")]
        public async Task<IActionResult> AcceptOffer(int id, [FromBody] RespondToOfferRequest request)
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized();

                var offer = await _db.Offers
                    .Include(o => o.Listing)
                        .ThenInclude(l => l!.CollectionItem)
                    .Include(o => o.TradeItem)
                    .FirstOrDefaultAsync(o => o.OfferId == id && !o.IsDeleted);

                if (offer == null)
                    return NotFound();

                // Authorization: must be listing owner
                if (offer.Listing!.SellerUserName != username)
                    return Forbid();

                if (offer.Status != "Pending")
                    return BadRequest(new { message = $"Offer is already {offer.Status}." });

                // Verify listing still active
                if (offer.Listing.Status != "Active")
                    return BadRequest(new { message = "Listing is no longer active." });

                // Transfer item ownership
                var item = offer.Listing.CollectionItem!;
                
                // Create new collection item for buyer
                var newItem = new ArcaneVaultCollectionItems
                {
                    ItemName = item.ItemName,
                    StartingQuantity = offer.QuantityRequested,
                    CurrentQuantity = offer.QuantityRequested,
                    UserName = offer.BuyerUserName,
                    IsDeleted = false
                };

                _db.CollectionItems.Add(newItem);

                // Copy categories to new item
                var categories = await _db.CollectionItemCategories
                    .Where(c => c.ItemId == item.ItemId)
                    .ToListAsync();

                // Reduce seller's quantity
                item.CurrentQuantity -= offer.QuantityRequested;

                // Update offer status
                offer.Status = "Accepted";
                offer.ResponseDate = DateTime.UtcNow;
                offer.SellerResponse = request.Response;

                // Update listing
                offer.Listing.QuantityAvailable -= offer.QuantityRequested;
                if (offer.Listing.QuantityAvailable == 0)
                {
                    offer.Listing.Status = "Sold";
                    offer.Listing.CompletedDate = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync();

                // Now add categories for new item
                foreach (var cat in categories)
                {
                    _db.CollectionItemCategories.Add(new ArcaneVaultCollectionItemCategories
                    {
                        ItemId = newItem.ItemId,
                        CategoryCode = cat.CategoryCode
                    });
                }

                await _db.SaveChangesAsync();

                _logger.LogInformation($"Offer {id} accepted. Item transferred from '{username}' to '{offer.BuyerUserName}'.");

                return Ok(new { message = "Offer accepted and item transferred successfully.", offer.OfferId, offer.Status });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error accepting offer {id}: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred accepting the offer." });
            }
        }

        /// <summary>
        /// PUT /api/marketplace/offers/{id}/reject - Reject an offer
        /// </summary>
        [Authorize]
        [HttpPut("offers/{id}/reject")]
        public async Task<IActionResult> RejectOffer(int id, [FromBody] RespondToOfferRequest request)
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized();

                var offer = await _db.Offers
                    .Include(o => o.Listing)
                    .FirstOrDefaultAsync(o => o.OfferId == id && !o.IsDeleted);

                if (offer == null)
                    return NotFound();

                // Authorization: must be listing owner
                if (offer.Listing!.SellerUserName != username)
                    return Forbid();

                if (offer.Status != "Pending")
                    return BadRequest(new { message = $"Offer is already {offer.Status}." });

                offer.Status = "Rejected";
                offer.ResponseDate = DateTime.UtcNow;
                offer.SellerResponse = request.Response;

                await _db.SaveChangesAsync();

                _logger.LogInformation($"Offer {id} rejected by '{username}'.");

                return Ok(new { message = "Offer rejected.", offer.OfferId, offer.Status });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error rejecting offer {id}: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred rejecting the offer." });
            }
        }

        /// <summary>
        /// DELETE /api/marketplace/offers/{id} - Withdraw an offer
        /// </summary>
        [Authorize]
        [HttpDelete("offers/{id}")]
        public async Task<IActionResult> WithdrawOffer(int id)
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized();

                var offer = await _db.Offers
                    .FirstOrDefaultAsync(o => o.OfferId == id && !o.IsDeleted);

                if (offer == null)
                    return NotFound();

                // Authorization: must be offer maker
                if (offer.BuyerUserName != username)
                    return Forbid();

                if (offer.Status != "Pending")
                    return BadRequest(new { message = $"Cannot withdraw offer that is {offer.Status}." });

                offer.Status = "Withdrawn";
                offer.IsDeleted = true;

                await _db.SaveChangesAsync();

                _logger.LogInformation($"Offer {id} withdrawn by '{username}'.");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error withdrawing offer {id}: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred withdrawing the offer." });
            }
        }
    }

    // ==================== DTOs ====================

    public class CreateListingRequest
    {
        [Required]
        public int ItemId { get; set; }

        [StringLength(200)]
        public string? Title { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [Range(0.01, 999999.99)]
        public decimal? AskingPrice { get; set; }

        [Required]
        [StringLength(20)]
        public string ListingType { get; set; } = "Sale";

        [StringLength(500)]
        public string? TradePreferences { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int QuantityAvailable { get; set; } = 1;

        [Range(1, 365)]
        public int? ExpirationDays { get; set; }
    }

    public class CreateOfferRequest
    {
        [Required]
        [StringLength(20)]
        public string OfferType { get; set; } = "Purchase";

        [Range(0.01, 999999.99)]
        public decimal? OfferedPrice { get; set; }

        public int? TradeItemId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int QuantityRequested { get; set; } = 1;

        [StringLength(500)]
        public string? Message { get; set; }
    }

    public class RespondToOfferRequest
    {
        [StringLength(500)]
        public string? Response { get; set; }
    }
}
