using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroceryManagement.Migrations
{
    /// <inheritdoc />
    public partial class addRegex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecord_Users_StaffId",
                table: "AttendanceRecord");

            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_Users_StaffId",
                table: "Inventories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AttendanceRecord",
                table: "AttendanceRecord");

            migrationBuilder.DropColumn(
                name: "StoreFrontQty",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "WareHouseQty",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "AttendanceRecord");

            migrationBuilder.AlterColumn<decimal>(
                name: "Salary",
                table: "Users",
                type: "decimal(7,2)",
                precision: 7,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldPrecision: 10,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "Products",
                type: "decimal(7,2)",
                precision: 7,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(6,2)",
                oldPrecision: 6,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Products",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "StoreFrontQty",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WareHouseQty",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "StaffId",
                table: "Inventories",
                type: "nvarchar(4)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "StaffId",
                table: "AttendanceRecord",
                type: "nvarchar(4)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(4)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "AttendanceRecord",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "CheckInTime",
                table: "AttendanceRecord",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "CheckOutTime",
                table: "AttendanceRecord",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "AttendanceRecord",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AttendanceRecord",
                table: "AttendanceRecord",
                column: "Date");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecord_Users_StaffId",
                table: "AttendanceRecord",
                column: "StaffId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_Users_StaffId",
                table: "Inventories",
                column: "StaffId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecord_Users_StaffId",
                table: "AttendanceRecord");

            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_Users_StaffId",
                table: "Inventories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AttendanceRecord",
                table: "AttendanceRecord");

            migrationBuilder.DropColumn(
                name: "StoreFrontQty",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "WareHouseQty",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "AttendanceRecord");

            migrationBuilder.DropColumn(
                name: "CheckInTime",
                table: "AttendanceRecord");

            migrationBuilder.DropColumn(
                name: "CheckOutTime",
                table: "AttendanceRecord");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AttendanceRecord");

            migrationBuilder.AlterColumn<decimal>(
                name: "Salary",
                table: "Users",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(7,2)",
                oldPrecision: 7,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "Products",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(7,2)",
                oldPrecision: 7,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "StaffId",
                table: "Inventories",
                type: "nvarchar(4)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(4)");

            migrationBuilder.AddColumn<int>(
                name: "StoreFrontQty",
                table: "Inventories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WareHouseQty",
                table: "Inventories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "StaffId",
                table: "AttendanceRecord",
                type: "nvarchar(4)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(4)");

            migrationBuilder.AddColumn<string>(
                name: "Id",
                table: "AttendanceRecord",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AttendanceRecord",
                table: "AttendanceRecord",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecord_Users_StaffId",
                table: "AttendanceRecord",
                column: "StaffId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_Users_StaffId",
                table: "Inventories",
                column: "StaffId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
