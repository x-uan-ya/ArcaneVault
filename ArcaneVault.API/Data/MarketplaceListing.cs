// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArcaneVault.API.Data
{
    /// <summary>
    /// Represents a marketplace listing where users can sell or trade their collection items.
    /// </summary>
    public class MarketplaceListing
    {
        [Key]
        public int ListingId { get; set; }

        /// <summary>
        /// The collection item being listed for sale/trade.
        /// </summary>
        [Required]
        [ForeignKey("CollectionItem")]
        public int ItemId { get; set; }

        /// <summary>
        /// Owner of the listing (same as item owner).
        /// </summary>
        [Required]
        [StringLength(50)]
        public string SellerUserName { get; set; } = string.Empty;

        /// <summary>
        /// Title of the listing (defaults to item name but can be customized).
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description of the listing, condition notes, etc.
        /// </summary>
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Asking price (null if trade-only, no price).
        /// </summary>
        [Column(TypeName = "decimal(10,2)")]
        public decimal? AskingPrice { get; set; }

        /// <summary>
        /// Listing type: "Sale", "Trade", "Both"
        /// </summary>
        [Required]
        [StringLength(20)]
        public string ListingType { get; set; } = "Sale";

        /// <summary>
        /// If trade is accepted, what the seller is looking for.
        /// </summary>
        [StringLength(500)]
        public string? TradePreferences { get; set; }

        /// <summary>
        /// Quantity available for sale/trade.
        /// </summary>
        [Required]
        [Range(1, int.MaxValue)]
        public int QuantityAvailable { get; set; } = 1;

        /// <summary>
        /// Status: "Active", "Sold", "Expired", "Cancelled"
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Active";

        /// <summary>
        /// When the listing was created.
        /// </summary>
        [Required]
        public DateTime ListedDate { get; set; }

        /// <summary>
        /// Optional expiration date for the listing.
        /// </summary>
        public DateTime? ExpirationDate { get; set; }

        /// <summary>
        /// When the listing was sold/completed.
        /// </summary>
        public DateTime? CompletedDate { get; set; }

        /// <summary>
        /// Number of views this listing has received.
        /// </summary>
        public int ViewCount { get; set; } = 0;

        /// <summary>
        /// Featured/promoted listing flag (for future enhancement).
        /// </summary>
        public bool IsFeatured { get; set; } = false;

        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        public ArcaneVaultCollectionItems? CollectionItem { get; set; }
        public ICollection<Offer> Offers { get; set; } = new List<Offer>();
    }
}
