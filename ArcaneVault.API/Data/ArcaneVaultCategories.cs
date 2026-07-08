// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using System.ComponentModel.DataAnnotations;

namespace ArcaneVault.API.Data
{
    public class ArcaneVaultCategories
    {
        [Key]
        [StringLength(20)]
        public string CategoryCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        // Navigation
        public ICollection<ArcaneVaultCollectionItemCategories> CollectionItemCategories { get; set; } = new List<ArcaneVaultCollectionItemCategories>();
    }
}
