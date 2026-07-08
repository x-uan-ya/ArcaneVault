// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArcaneVault.API.Data
{
    public class ArcaneVaultUsers
    {
        [Key]
        [StringLength(50)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public bool IsDeleted { get; set; } = false;

        [ForeignKey("Role")]
        public int RoleId { get; set; }

        // Navigation
        public ArcaneVaultRoles? Role { get; set; }
        public ICollection<ArcaneVaultCollectionItems> CollectionItems { get; set; } = new List<ArcaneVaultCollectionItems>();
    }
}
