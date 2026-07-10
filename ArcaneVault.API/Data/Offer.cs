// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArcaneVault.API.Data
{
    /// <summary>
    /// Represents an offer made on a marketplace listing.
    /// Can be a purchase offer (price-based) or trade offer (item-based).
    /// </summary>
    public class Offer
    {
        [Key]
        public int OfferId { get; set; }

        /// <summary>
        /// The marketplace listing this offer is for.
        /// </summary>
        [Required]
        [ForeignKey("Listing")]
        public int ListingId { get; set; }

        /// <summary>
        /// User making the offer.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string BuyerUserName { get; set; } = string.Empty;

        /// <summary>
        /// Offer type: "Purchase", "Trade", "Counter"
        /// </summary>
        [Required]
        [StringLength(20)]
        public string OfferType { get; set; } = "Purchase";

        /// <summary>
        /// Offered price (null if pure trade).
        /// </summary>
        [Column(TypeName = "decimal(10,2)")]
        public decimal? OfferedPrice { get; set; }

        /// <summary>
        /// If trade offer, which collection item is being offered in exchange.
        /// </summary>
        [ForeignKey("TradeItem")]
        public int? TradeItemId { get; set; }

        /// <summary>
        /// Quantity requested (default 1).
        /// </summary>
        [Required]
        [Range(1, int.MaxValue)]
        public int QuantityRequested { get; set; } = 1;

        /// <summary>
        /// Message/notes from the buyer to the seller.
        /// </summary>
        [StringLength(500)]
        public string? Message { get; set; }

        /// <summary>
        /// Status: "Pending", "Accepted", "Rejected", "Countered", "Withdrawn", "Expired"
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// When the offer was made.
        /// </summary>
        [Required]
        public DateTime OfferedDate { get; set; }

        /// <summary>
        /// When the offer was responded to (accepted/rejected).
        /// </summary>
        public DateTime? ResponseDate { get; set; }

        /// <summary>
        /// Seller's response message.
        /// </summary>
        [StringLength(500)]
        public string? SellerResponse { get; set; }

        /// <summary>
        /// If this is a counter-offer, reference to the original offer.
        /// </summary>
        [ForeignKey("OriginalOffer")]
        public int? ParentOfferId { get; set; }

        /// <summary>
        /// Offer expiration (auto-expires after X days if not responded).
        /// </summary>
        public DateTime? ExpirationDate { get; set; }

        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        public MarketplaceListing? Listing { get; set; }
        public ArcaneVaultCollectionItems? TradeItem { get; set; }
        public Offer? OriginalOffer { get; set; }
        public ICollection<Offer> CounterOffers { get; set; } = new List<Offer>();
    }
}
