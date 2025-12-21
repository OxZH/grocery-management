using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroceryManagement.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDB4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SupplierId",
                table: "Inventories",
                type: "nvarchar(6)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_SupplierId",
                table: "Inventories",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_Suppliers_SupplierId",
                table: "Inventories",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_Suppliers_SupplierId",
                table: "Inventories");

            migrationBuilder.DropIndex(
                name: "IX_Inventories_SupplierId",
                table: "Inventories");

            migrationBuilder.AlterColumn<string>(
                name: "SupplierId",
                table: "Inventories",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(6)");
        }
    }
}
