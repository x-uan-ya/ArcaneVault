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
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserName",
                keyValue: "admin",
                column: "PasswordHash",
                value: "$2a$12$R9h/cIPz0gi.URNNV3kh2OPST9/PgBkqquzi.Ee4kGXpVuLxNs.lq");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserName",
                keyValue: "admin",
                column: "PasswordHash",
                value: "$2a$11$3QpoxpoCxm/OqrEjuFUxo.Y7TkNQ5rRbUFxLzFvWIIq0a5b5JKbFG");
        }
    }
}
