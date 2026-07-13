// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArcaneVault.API.Data
{
    /// <summary>
    /// Represents a user's wishlist item - items they want to acquire.
    /// Users can add marketplace listings to their wishlist and get notified when available.
    /// </summary>
    [Table("Wishlist")]
    public class Wishlist
    {
        [Key]
        public int WishlistId { get; set; }

        [Required]
        [StringLength(50)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public int ItemId { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        [ForeignKey(nameof(UserName))]
        public ArcaneVaultUsers? User { get; set; }

        [ForeignKey(nameof(ItemId))]
        public ArcaneVaultCollectionItems? CollectionItem { get; set; }
    }
}
