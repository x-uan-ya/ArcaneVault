// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArcaneVault.API.Data
{
    public class ArcaneVaultCollectionItemCategories
    {
        // Composite PK: ItemId + CategoryCode (configured in DbContext)
        public int ItemId { get; set; }

        [StringLength(20)]
        public string CategoryCode { get; set; } = string.Empty;

        // Navigation
        public ArcaneVaultCollectionItems? CollectionItem { get; set; }
        public ArcaneVaultCategories? Category { get; set; }
    }
}
