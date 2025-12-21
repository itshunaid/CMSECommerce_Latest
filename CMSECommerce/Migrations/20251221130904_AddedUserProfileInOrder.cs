using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMSECommerce.Migrations
{
    /// <inheritdoc />
    public partial class AddedUserProfileInOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WhatsappNumber",
                table: "UserProfiles",
                newName: "WhatsAppNumber");

            migrationBuilder.AddColumn<int>(
                name: "UserProfileId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "OwnerId", "StockQuantity" },
                values: new object[] { "admin", 100 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "OwnerId", "StockQuantity" },
                values: new object[] { "admin", 80 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "OwnerId", "StockQuantity" },
                values: new object[] { "admin", 150 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "OwnerId", "StockQuantity" },
                values: new object[] { "admin", 120 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "OwnerId", "StockQuantity" },
                values: new object[] { "admin", 45 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "OwnerId", "StockQuantity" },
                values: new object[] { "admin", 30 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "OwnerId", "StockQuantity" },
                values: new object[] { "admin", 25 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "OwnerId", "StockQuantity" },
                values: new object[] { "admin", 10 });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserProfileId",
                table: "Orders",
                column: "UserProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_UserProfiles_UserProfileId",
                table: "Orders",
                column: "UserProfileId",
                principalTable: "UserProfiles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_UserProfiles_UserProfileId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_UserProfileId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "UserProfileId",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "WhatsAppNumber",
                table: "UserProfiles",
                newName: "WhatsappNumber");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "OwnerId", "StockQuantity" },
                values: new object[] { null, 0 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "OwnerId", "StockQuantity" },
                values: new object[] { null, 0 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "OwnerId", "StockQuantity" },
                values: new object[] { null, 0 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "OwnerId", "StockQuantity" },
                values: new object[] { null, 0 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "OwnerId", "StockQuantity" },
                values: new object[] { null, 0 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "OwnerId", "StockQuantity" },
                values: new object[] { null, 0 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "OwnerId", "StockQuantity" },
                values: new object[] { null, 0 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "OwnerId", "StockQuantity" },
                values: new object[] { null, 0 });
        }
    }
}
