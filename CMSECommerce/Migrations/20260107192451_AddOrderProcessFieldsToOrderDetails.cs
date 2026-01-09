using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMSECommerce.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderProcessFieldsToOrderDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeliveryImageUrl",
                table: "OrderDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdated",
                table: "OrderDetails",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "SellerNote",
                table: "OrderDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "OrderDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "e02f042f-60e2-4c72-9a11-90c2be4a7f9b", "AQAAAAIAAYagAAAAEGi/W4FYke5XGPqEMo/N4MTKP9xX93ARGwtD6gyvvZ34aHyYMcH/icygIgQNqklCIw==" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryImageUrl",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "SellerNote",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "OrderDetails");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "e1492f17-d960-42c6-b506-3ea67211cbc3", "AQAAAAIAAYagAAAAEDUAZOeulxVqQLKguefVM0h3Rao+qNsbRf2aNw1BJUO11T7ldvKQx4jtlA7do2wgew==" });
        }
    }
}
