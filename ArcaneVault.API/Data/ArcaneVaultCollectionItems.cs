// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArcaneVault.API.Data
{
    public class ArcaneVaultCollectionItems
    {
        [Key]
        public int ItemId { get; set; }

        [Required]
        [StringLength(200)]
        public string ItemName { get; set; } = string.Empty;

        public bool IsDeleted { get; set; } = false;

        [Range(0, int.MaxValue)]
        public int StartingQuantity { get; set; }

        [Range(0, int.MaxValue)]
        public int CurrentQuantity { get; set; }

        [ForeignKey("User")]
        [StringLength(50)]
        public string UserName { get; set; } = string.Empty;

        // Navigation
        public ArcaneVaultUsers? User { get; set; }
        public ICollection<ArcaneVaultCollectionItemCategories> CollectionItemCategories { get; set; } = new List<ArcaneVaultCollectionItemCategories>();
    }
}
