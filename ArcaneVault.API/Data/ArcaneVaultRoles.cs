// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using System.ComponentModel.DataAnnotations;

namespace ArcaneVault.API.Data
{
    public class ArcaneVaultRoles
    {
        [Key]
        public int RoleId { get; set; }

        [Required]
        public string RoleName { get; set; } = string.Empty;

        // Navigation
        public ICollection<ArcaneVaultUsers> Users { get; set; } = new List<ArcaneVaultUsers>();
    }
}
