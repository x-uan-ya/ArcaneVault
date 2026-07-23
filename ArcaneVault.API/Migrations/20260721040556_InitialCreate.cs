using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ArcaneVault.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CategoryName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryCode);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    WalletBalance = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    RoleId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserName);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CollectionItems",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    StartingQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionItems", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_CollectionItems_Users_UserName",
                        column: x => x.UserName,
                        principalTable: "Users",
                        principalColumn: "UserName",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FixedDepositAccounts",
                columns: table => new
                {
                    FDAccountId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    AccountType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PrincipalAmount = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    AnnualInterestRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    TenureMonths = table.Column<int>(type: "INTEGER", nullable: false),
                    OpenedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MaturityDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CurrentBalance = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    AccruedInterest = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IsQuarterlyCompounding = table.Column<bool>(type: "INTEGER", nullable: false),
                    WithdrawalDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PenaltyAmount = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    AmountReceived = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FixedDepositAccounts", x => x.FDAccountId);
                    table.ForeignKey(
                        name: "FK_FixedDepositAccounts_Users_UserName",
                        column: x => x.UserName,
                        principalTable: "Users",
                        principalColumn: "UserName",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WalletTransactions",
                columns: table => new
                {
                    TransactionId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletTransactions", x => x.TransactionId);
                    table.ForeignKey(
                        name: "FK_WalletTransactions_Users_UserName",
                        column: x => x.UserName,
                        principalTable: "Users",
                        principalColumn: "UserName",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CollectionItemCategories",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    CategoryCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionItemCategories", x => new { x.ItemId, x.CategoryCode });
                    table.ForeignKey(
                        name: "FK_CollectionItemCategories_Categories_CategoryCode",
                        column: x => x.CategoryCode,
                        principalTable: "Categories",
                        principalColumn: "CategoryCode",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CollectionItemCategories_CollectionItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "CollectionItems",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarketplaceListings",
                columns: table => new
                {
                    ListingId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    SellerUserName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    AskingPrice = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    ListingType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    TradePreferences = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    QuantityAvailable = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ListedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ViewCount = table.Column<int>(type: "INTEGER", nullable: false),
                    IsFeatured = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketplaceListings", x => x.ListingId);
                    table.ForeignKey(
                        name: "FK_MarketplaceListings_CollectionItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "CollectionItems",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Wishlist",
                columns: table => new
                {
                    WishlistId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wishlist", x => x.WishlistId);
                    table.ForeignKey(
                        name: "FK_Wishlist_CollectionItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "CollectionItems",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Wishlist_Users_UserName",
                        column: x => x.UserName,
                        principalTable: "Users",
                        principalColumn: "UserName",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FixedDepositTransactions",
                columns: table => new
                {
                    TransactionId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FDAccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    TransactionType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AccrualPeriod = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FixedDepositTransactions", x => x.TransactionId);
                    table.ForeignKey(
                        name: "FK_FixedDepositTransactions_FixedDepositAccounts_FDAccountId",
                        column: x => x.FDAccountId,
                        principalTable: "FixedDepositAccounts",
                        principalColumn: "FDAccountId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Offers",
                columns: table => new
                {
                    OfferId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ListingId = table.Column<int>(type: "INTEGER", nullable: false),
                    BuyerUserName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    OfferType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    OfferedPrice = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    TradeItemId = table.Column<int>(type: "INTEGER", nullable: true),
                    QuantityRequested = table.Column<int>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    OfferedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ResponseDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SellerResponse = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ParentOfferId = table.Column<int>(type: "INTEGER", nullable: true),
                    ExpirationDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PaymentDeadline = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offers", x => x.OfferId);
                    table.ForeignKey(
                        name: "FK_Offers_CollectionItems_TradeItemId",
                        column: x => x.TradeItemId,
                        principalTable: "CollectionItems",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Offers_MarketplaceListings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "MarketplaceListings",
                        principalColumn: "ListingId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Offers_Offers_ParentOfferId",
                        column: x => x.ParentOfferId,
                        principalTable: "Offers",
                        principalColumn: "OfferId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "RoleId", "RoleName" },
                values: new object[,]
                {
                    { 1, "User" },
                    { 2, "Staff" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserName", "Email", "IsDeleted", "PasswordHash", "RoleId", "WalletBalance" },
                values: new object[] { "admin", "admin@arcanevault.com", false, "$2a$12$R9h/cIPz0gi.URNNV3kh2OPST9/PgBkqquzi.Ee4kGXpVuLxNs.lq", 2, 0m });

            migrationBuilder.CreateIndex(
                name: "IX_CollectionItemCategories_CategoryCode",
                table: "CollectionItemCategories",
                column: "CategoryCode");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionItems_UserName",
                table: "CollectionItems",
                column: "UserName");

            migrationBuilder.CreateIndex(
                name: "IX_FixedDepositAccounts_UserName",
                table: "FixedDepositAccounts",
                column: "UserName");

            migrationBuilder.CreateIndex(
                name: "IX_FixedDepositTransactions_FDAccountId",
                table: "FixedDepositTransactions",
                column: "FDAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceListings_ItemId",
                table: "MarketplaceListings",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_ListingId",
                table: "Offers",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_ParentOfferId",
                table: "Offers",
                column: "ParentOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_TradeItemId",
                table: "Offers",
                column: "TradeItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_UserName",
                table: "WalletTransactions",
                column: "UserName");

            migrationBuilder.CreateIndex(
                name: "IX_Wishlist_ItemId",
                table: "Wishlist",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Wishlist_UserName",
                table: "Wishlist",
                column: "UserName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollectionItemCategories");

            migrationBuilder.DropTable(
                name: "FixedDepositTransactions");

            migrationBuilder.DropTable(
                name: "Offers");

            migrationBuilder.DropTable(
                name: "WalletTransactions");

            migrationBuilder.DropTable(
                name: "Wishlist");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "FixedDepositAccounts");

            migrationBuilder.DropTable(
                name: "MarketplaceListings");

            migrationBuilder.DropTable(
                name: "CollectionItems");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
