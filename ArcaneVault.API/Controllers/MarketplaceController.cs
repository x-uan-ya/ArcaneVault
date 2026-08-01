// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using ArcaneVault.API.Data;
using ArcaneVault.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ArcaneVault.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MarketplaceController : ControllerBase
    {
        private readonly ArcaneVaultDbContext _db;
        private readonly ILogger<MarketplaceController> _logger;
        private readonly INotificationService _notify;

        public MarketplaceController(ArcaneVaultDbContext db, ILogger<MarketplaceController> logger, INotificationService notify)
        {
            _db = db;
            _logger = logger;
            _notify = notify;
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

                await _notify.SendAsync(username,
                    $"📢 Your listing \"{listing.Title}\" has been published to the marketplace.",
                    "marketplace", "/Marketplace/MyListings");

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

                // One offer per buyer per listing: check for existing active (non-withdrawn/non-rejected) offer
                var existingOffer = await _db.Offers
                    .FirstOrDefaultAsync(o => o.ListingId == listingId &&
                                             o.BuyerUserName == username &&
                                             !o.IsDeleted &&
                                             o.Status == "Pending");

                if (existingOffer != null)
                    return Conflict(new
                    {
                        message = "You already have a pending offer on this listing. Please withdraw or edit your existing offer.",
                        existingOfferId = existingOffer.OfferId,
                        alreadyOffered = true
                    });

                // Validate quantity
                if (request.QuantityRequested > listing.QuantityAvailable)
                    return BadRequest(new { message = "Requested quantity exceeds available quantity." });

                // ── POINT 1: Pre-flight wallet check for purchase offers ──────────────
                if (request.OfferType == "Purchase" && request.OfferedPrice.HasValue)
                {
                    var buyer = await _db.Users.FirstOrDefaultAsync(u => u.UserName == username && !u.IsDeleted);
                    if (buyer == null)
                        return Unauthorized();

                    if (buyer.WalletBalance < request.OfferedPrice.Value)
                        return StatusCode(StatusCodes.Status402PaymentRequired, new
                        {
                            message = $"Insufficient wallet balance. You need ${request.OfferedPrice.Value:F2} but only have ${buyer.WalletBalance:F2}. Please top up your wallet first.",
                            insufficientFunds = true,
                            required = request.OfferedPrice.Value,
                            available = buyer.WalletBalance,
                            shortfall = request.OfferedPrice.Value - buyer.WalletBalance
                        });
                }
                // ─────────────────────────────────────────────────────────────────────

                // If trade listing, verify buyer has the required item (case insensitive)
                if ((listing.ListingType == "Trade" || listing.ListingType == "Both") && 
                    !string.IsNullOrWhiteSpace(listing.TradePreferences))
                {
                    var requiredItemName = listing.TradePreferences.Trim();
                    
                    var buyerHasItem = await _db.CollectionItems
                        .AnyAsync(i => i.UserName == username && 
                                      i.ItemName.ToLower() == requiredItemName.ToLower() &&
                                      i.CurrentQuantity > 0 &&
                                      !i.IsDeleted);

                    if (!buyerHasItem && request.OfferType == "Trade")
                        return BadRequest(new { message = $"You do not have the item '{requiredItemName}' required for this trade." });
                }

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
                    ExpirationDate = DateTime.UtcNow.AddDays(7)
                };

                _db.Offers.Add(offer);
                await _db.SaveChangesAsync();

                _logger.LogInformation($"Offer made by '{username}' on listing {listingId}.");

                // Notify buyer that their offer was submitted
                await _notify.SendAsync(username,
                    $"📨 Your offer on \"{listing.Title}\" has been submitted.",
                    "offer", "/Marketplace/MyOffers");

                // Notify seller they received a new offer
                await _notify.SendAsync(listing.SellerUserName,
                    $"🔔 You received a new offer on your listing \"{listing.Title}\".",
                    "offer", "/Marketplace/MyListings");

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
                        o.PaymentDeadline,
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
        /// PUT /api/marketplace/offers/{id}/accept - Seller accepts offer.
        /// For purchase offers this transitions to "AwaitingPayment" so the buyer has
        /// 15 minutes to top-up and confirm. Trade offers complete immediately.
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

                if (offer.Listing!.SellerUserName != username)
                    return Forbid();

                if (offer.Status != "Pending")
                    return BadRequest(new { message = $"Offer is already {offer.Status}." });

                if (offer.Listing.Status != "Active")
                    return BadRequest(new { message = "Listing is no longer active." });

                // ── Trade offers complete immediately (no wallet involved) ───────────
                if (offer.OfferType != "Purchase")
                {
                    await CompleteOfferTransferAsync(offer, username);
                    await _db.SaveChangesAsync();
                    _logger.LogInformation($"Trade offer {id} accepted. Item transferred to '{offer.BuyerUserName}'.");
                    return Ok(new { message = "Offer accepted and item transferred successfully.", offer.OfferId, offer.Status });
                }

                // ── Purchase offer: check buyer balance ───────────────────────────────
                var buyer = await _db.Users.FirstOrDefaultAsync(u => u.UserName == offer.BuyerUserName && !u.IsDeleted);
                if (buyer == null)
                    return BadRequest(new { message = "Buyer account not found." });

                if (buyer.WalletBalance >= offer.OfferedPrice!.Value)
                {
                    // Buyer already has enough — complete immediately
                    await ProcessPurchasePaymentAsync(offer, buyer, username);
                    await CompleteOfferTransferAsync(offer, username);
                    await _db.SaveChangesAsync();

                    await _notify.SendAsync(offer.BuyerUserName,
                        $"✅ Your offer on \"{offer.Listing!.Title}\" was accepted and payment processed. Item added to your collection!",
                        "offer", "/Marketplace/MyOffers");
                    await _notify.SendAsync(username,
                        $"💰 You accepted an offer on \"{offer.Listing!.Title}\" and received payment.",
                        "offer", "/Marketplace/MyListings");

                    _logger.LogInformation($"Purchase offer {id} accepted and payment processed immediately.");
                    return Ok(new { message = "Offer accepted and payment processed successfully.", offer.OfferId, offer.Status });
                }
                else
                {
                    // ── POINT 2: Buyer doesn't have enough — enter AwaitingPayment ───
                    offer.Status = "AwaitingPayment";
                    offer.ResponseDate = DateTime.UtcNow;
                    offer.SellerResponse = request.Response;
                    offer.PaymentDeadline = DateTime.UtcNow.AddMinutes(15);

                    await _db.SaveChangesAsync();

                    await _notify.SendAsync(offer.BuyerUserName,
                        $"⚠️ Your offer on \"{offer.Listing!.Title}\" was accepted but your wallet balance is insufficient. Please top up within 15 minutes.",
                        "wallet", "/Wallet/Index");
                    await _notify.SendAsync(username,
                        $"⏳ You accepted an offer on \"{offer.Listing!.Title}\". Waiting for buyer to complete payment (15 min).",
                        "offer", "/Marketplace/MyListings");

                    _logger.LogInformation($"Purchase offer {id} accepted by seller but buyer has insufficient funds. Awaiting payment until {offer.PaymentDeadline}.");

                    return Ok(new
                    {
                        message = "Offer accepted. Buyer has been notified to top up their wallet within 15 minutes.",
                        offer.OfferId,
                        offer.Status,
                        paymentDeadline = offer.PaymentDeadline,
                        required = offer.OfferedPrice.Value,
                        buyerCurrentBalance = buyer.WalletBalance,
                        shortfall = offer.OfferedPrice.Value - buyer.WalletBalance,
                        awaitingPayment = true
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error accepting offer {id}: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred accepting the offer." });
            }
        }

        /// <summary>
        /// POST /api/marketplace/offers/{id}/confirm-payment
        /// Buyer confirms payment after topping up. Re-checks balance and completes the deal.
        /// Also used as a poll target — if deadline has passed, auto-cancels.
        /// </summary>
        [Authorize]
        [HttpPost("offers/{id}/confirm-payment")]
        public async Task<IActionResult> ConfirmPayment(int id)
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized();

                var offer = await _db.Offers
                    .Include(o => o.Listing)
                        .ThenInclude(l => l!.CollectionItem)
                    .FirstOrDefaultAsync(o => o.OfferId == id && !o.IsDeleted);

                if (offer == null)
                    return NotFound();

                // Only the buyer can confirm payment
                if (offer.BuyerUserName != username)
                    return Forbid();

                if (offer.Status != "AwaitingPayment")
                    return BadRequest(new { message = $"Offer is not awaiting payment. Current status: {offer.Status}." });

                // AwaitingPayment only applies to purchase offers
                if (offer.OfferType != "Purchase" || !offer.OfferedPrice.HasValue)
                    return BadRequest(new { message = "This offer does not require payment confirmation." });

                // Check if deadline has passed
                if (offer.PaymentDeadline.HasValue && DateTime.UtcNow > offer.PaymentDeadline.Value)
                {
                    offer.Status = "Cancelled";
                    offer.SellerResponse = "Transaction cancelled: buyer did not complete payment within the time limit.";
                    await _db.SaveChangesAsync();
                    _logger.LogInformation($"Offer {id} auto-cancelled — payment deadline expired.");
                    return BadRequest(new
                    {
                        message = "The payment window has expired. This transaction has been cancelled.",
                        cancelled = true
                    });
                }

                var buyer = await _db.Users.FirstOrDefaultAsync(u => u.UserName == username && !u.IsDeleted);
                if (buyer == null)
                    return Unauthorized();

                if (buyer.WalletBalance < offer.OfferedPrice!.Value)
                    return StatusCode(StatusCodes.Status402PaymentRequired, new
                    {
                        message = $"Still insufficient funds. You need ${offer.OfferedPrice.Value:F2} but have ${buyer.WalletBalance:F2}. Top up ${offer.OfferedPrice.Value - buyer.WalletBalance:F2} more.",
                        insufficientFunds = true,
                        required = offer.OfferedPrice.Value,
                        available = buyer.WalletBalance,
                        shortfall = offer.OfferedPrice.Value - buyer.WalletBalance,
                        paymentDeadline = offer.PaymentDeadline
                    });

                // Balance is sufficient — complete the transaction
                var sellerName = offer.Listing!.SellerUserName;
                await ProcessPurchasePaymentAsync(offer, buyer, sellerName);
                await CompleteOfferTransferAsync(offer, sellerName);
                await _db.SaveChangesAsync();

                _logger.LogInformation($"Offer {id} payment confirmed by buyer '{username}'. Transaction complete.");

                return Ok(new { message = "Payment confirmed! The item has been transferred to your collection.", offer.OfferId, offer.Status });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error confirming payment for offer {id}: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred processing your payment." });
            }
        }

        /// <summary>
        /// GET /api/marketplace/offers/{id}/payment-status
        /// Seller polls this to see if payment came through or if the deadline expired.
        /// </summary>
        [Authorize]
        [HttpGet("offers/{id}/payment-status")]
        public async Task<IActionResult> GetPaymentStatus(int id)
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

                // Must be seller or buyer
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                if (offer.Listing!.SellerUserName != username && offer.BuyerUserName != username && userRole != "Staff")
                    return Forbid();

                // Auto-cancel if deadline passed and still awaiting payment
                if (offer.Status == "AwaitingPayment" &&
                    offer.PaymentDeadline.HasValue &&
                    DateTime.UtcNow > offer.PaymentDeadline.Value)
                {
                    offer.Status = "Cancelled";
                    offer.SellerResponse = "Transaction cancelled: buyer did not complete payment within the time limit.";
                    await _db.SaveChangesAsync();
                    _logger.LogInformation($"Offer {id} auto-cancelled via payment-status poll.");
                }

                return Ok(new
                {
                    offer.OfferId,
                    offer.Status,
                    offer.PaymentDeadline,
                    deadlineExpired = offer.PaymentDeadline.HasValue && DateTime.UtcNow > offer.PaymentDeadline.Value,
                    secondsRemaining = offer.PaymentDeadline.HasValue
                        ? Math.Max(0, (int)(offer.PaymentDeadline.Value - DateTime.UtcNow).TotalSeconds)
                        : (int?)null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting payment status for offer {id}: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred retrieving payment status." });
            }
        }

        // ── Private helpers ──────────────────────────────────────────────────────────

        /// <summary>Deducts buyer wallet and credits seller. Sets offer status to Accepted.</summary>
        private async Task ProcessPurchasePaymentAsync(Offer offer, ArcaneVaultUsers buyer, string sellerName)
        {
            buyer.WalletBalance -= offer.OfferedPrice!.Value;
            _db.WalletTransactions.Add(new WalletTransaction
            {
                UserName = offer.BuyerUserName,
                Type = "Purchase",
                Amount = -offer.OfferedPrice.Value,
                Description = $"Bought {offer.QuantityRequested}x {offer.Listing!.CollectionItem!.ItemName} from {sellerName}",
                BalanceAfter = buyer.WalletBalance,
                TransactionDate = DateTime.UtcNow
            });

            var seller = await _db.Users.FirstOrDefaultAsync(u => u.UserName == sellerName && !u.IsDeleted);
            if (seller != null)
            {
                seller.WalletBalance += offer.OfferedPrice.Value;
                _db.WalletTransactions.Add(new WalletTransaction
                {
                    UserName = sellerName,
                    Type = "Sale",
                    Amount = offer.OfferedPrice.Value,
                    Description = $"Sold {offer.QuantityRequested}x {offer.Listing.CollectionItem.ItemName} to {offer.BuyerUserName}",
                    BalanceAfter = seller.WalletBalance,
                    TransactionDate = DateTime.UtcNow
                });
            }
        }

        /// <summary>Transfers item ownership and updates listing/offer status.</summary>
        private async Task CompleteOfferTransferAsync(Offer offer, string sellerName)
        {
            var item = offer.Listing!.CollectionItem!;

            // Create new item for buyer
            var newItem = new ArcaneVaultCollectionItems
            {
                ItemName = item.ItemName,
                StartingQuantity = offer.QuantityRequested,
                CurrentQuantity = offer.QuantityRequested,
                UserName = offer.BuyerUserName,
                IsDeleted = false
            };
            _db.CollectionItems.Add(newItem);

            // Reduce seller quantity
            item.CurrentQuantity -= offer.QuantityRequested;
            if (item.CurrentQuantity <= 0)
                item.IsDeleted = true;

            // Update offer
            offer.Status = "Accepted";
            if (!offer.ResponseDate.HasValue)
                offer.ResponseDate = DateTime.UtcNow;

            // Update listing
            offer.Listing.QuantityAvailable -= offer.QuantityRequested;
            if (offer.Listing.QuantityAvailable <= 0)
            {
                offer.Listing.Status = "Sold";
                offer.Listing.CompletedDate = DateTime.UtcNow;

                // Cancel all other pending/awaiting offers on this listing
                var otherOffers = await _db.Offers
                    .Where(o => o.ListingId == offer.ListingId &&
                                o.OfferId != offer.OfferId &&
                                (o.Status == "Pending" || o.Status == "AwaitingPayment") &&
                                !o.IsDeleted)
                    .ToListAsync();

                foreach (var other in otherOffers)
                {
                    other.Status = "ItemSold";
                    other.ResponseDate = DateTime.UtcNow;
                    other.SellerResponse = "Thanks for your offer, but this item has been sold to another buyer.";
                }
            }

            // Copy categories to new item (save first to get newItem.ItemId)
            await _db.SaveChangesAsync();

            var categories = await _db.CollectionItemCategories
                .Where(c => c.ItemId == item.ItemId)
                .ToListAsync();

            foreach (var cat in categories)
            {
                _db.CollectionItemCategories.Add(new ArcaneVaultCollectionItemCategories
                {
                    ItemId = newItem.ItemId,
                    CategoryCode = cat.CategoryCode
                });
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

                await _notify.SendAsync(offer.BuyerUserName,
                    $"❌ Your offer on \"{offer.Listing!.Title}\" has been rejected.",
                    "offer", "/Marketplace/MyOffers");

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

                // Notify the seller their offer was withdrawn
                var listing = await _db.MarketplaceListings.FindAsync(offer.ListingId);
                if (listing != null)
                {
                    await _notify.SendAsync(listing.SellerUserName,
                        $"↩️ An offer on your listing \"{listing.Title}\" has been withdrawn by the buyer.",
                        "offer", "/Marketplace/MyListings");
                }

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
