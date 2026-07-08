// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using Microsoft.EntityFrameworkCore;

namespace ArcaneVault.API.Data
{
    public class ArcaneVaultDbContext : DbContext
    {
        public ArcaneVaultDbContext(DbContextOptions<ArcaneVaultDbContext> options)
            : base(options) { }

        public DbSet<ArcaneVaultRoles> Roles { get; set; }
        public DbSet<ArcaneVaultUsers> Users { get; set; }
        public DbSet<ArcaneVaultCategories> Categories { get; set; }
        public DbSet<ArcaneVaultCollectionItems> CollectionItems { get; set; }
        public DbSet<ArcaneVaultCollectionItemCategories> CollectionItemCategories { get; set; }
        public DbSet<FixedDepositAccounts> FixedDepositAccounts { get; set; }
        public DbSet<FixedDepositTransactions> FixedDepositTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Composite PK for CollectionItemCategories
            modelBuilder.Entity<ArcaneVaultCollectionItemCategories>()
                .HasKey(c => new { c.ItemId, c.CategoryCode });

            // CollectionItemCategories -> CollectionItem
            modelBuilder.Entity<ArcaneVaultCollectionItemCategories>()
                .HasOne(c => c.CollectionItem)
                .WithMany(i => i.CollectionItemCategories)
                .HasForeignKey(c => c.ItemId);

            // CollectionItemCategories -> Category
            modelBuilder.Entity<ArcaneVaultCollectionItemCategories>()
                .HasOne(c => c.Category)
                .WithMany(cat => cat.CollectionItemCategories)
                .HasForeignKey(c => c.CategoryCode);

            // FixedDepositAccounts -> Users
            modelBuilder.Entity<FixedDepositAccounts>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserName)
                .OnDelete(DeleteBehavior.Restrict);

            // FixedDepositTransactions -> FixedDepositAccounts
            modelBuilder.Entity<FixedDepositTransactions>()
                .HasOne(t => t.FDAccount)
                .WithMany(f => f.Transactions)
                .HasForeignKey(t => t.FDAccountId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed roles
            modelBuilder.Entity<ArcaneVaultRoles>().HasData(
                new ArcaneVaultRoles { RoleId = 1, RoleName = "User" },
                new ArcaneVaultRoles { RoleId = 2, RoleName = "Staff" }
            );

            // Seed default staff account
            // Password "Admin@123" hashed with BCrypt
            modelBuilder.Entity<ArcaneVaultUsers>().HasData(
                new ArcaneVaultUsers
                {
                    UserName = "admin",
                    Email = "admin@arcanevault.com",
                    // BCrypt hash of "Admin@123" - regenerated fresh
                    PasswordHash = "$2a$11$slYQmyNdGzirKEj7V5s1KODkT7XB8mHnHpF9X5K8vKJ5mK9B2Z7jW",
                    IsDeleted = false,
                    RoleId = 2
                }
            );

            base.OnModelCreating(modelBuilder);
        }
    }
}
