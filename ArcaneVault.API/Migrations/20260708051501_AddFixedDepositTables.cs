using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArcaneVault.API.Migrations
{
    /// <inheritdoc />
    public partial class AddFixedDepositTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateIndex(
                name: "IX_FixedDepositAccounts_UserName",
                table: "FixedDepositAccounts",
                column: "UserName");

            migrationBuilder.CreateIndex(
                name: "IX_FixedDepositTransactions_FDAccountId",
                table: "FixedDepositTransactions",
                column: "FDAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FixedDepositTransactions");

            migrationBuilder.DropTable(
                name: "FixedDepositAccounts");
        }
    }
}
