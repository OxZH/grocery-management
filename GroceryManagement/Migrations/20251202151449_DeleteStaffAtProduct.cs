using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroceryManagement.Migrations
{
    /// <inheritdoc />
    public partial class DeleteStaffAtProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Users_StaffId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_StaffId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "StaffId",
                table: "Products");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StaffId",
                table: "Products",
                type: "nvarchar(4)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_StaffId",
                table: "Products",
                column: "StaffId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Users_StaffId",
                table: "Products",
                column: "StaffId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
