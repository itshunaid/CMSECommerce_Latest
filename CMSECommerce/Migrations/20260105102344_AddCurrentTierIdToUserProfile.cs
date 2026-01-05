using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMSECommerce.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentTierIdToUserProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentTierId",
                table: "UserProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "59b18b8c-5c86-4feb-b5ec-0a3fee8c1703", "AQAAAAIAAYagAAAAEGjpXhK1YeElmONzBkar6hvCRhqhvJKq9Ka8VbTF027NlYOLqXKbQeCGm2y1xo1CuQ==" });

            migrationBuilder.UpdateData(
                table: "SubscriptionTiers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DurationMonths", "Name", "Price", "ProductLimit" },
                values: new object[] { 1, "Trial", 99m, 5 });

            migrationBuilder.UpdateData(
                table: "SubscriptionTiers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "DurationMonths", "Name", "Price", "ProductLimit" },
                values: new object[] { 6, "Basic", 499m, 25 });

           

            migrationBuilder.UpdateData(
                table: "UserProfiles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CurrentTierId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_CurrentTierId",
                table: "UserProfiles",
                column: "CurrentTierId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserProfiles_SubscriptionTiers_CurrentTierId",
                table: "UserProfiles",
                column: "CurrentTierId",
                principalTable: "SubscriptionTiers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserProfiles_SubscriptionTiers_CurrentTierId",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_CurrentTierId",
                table: "UserProfiles");

            migrationBuilder.DeleteData(
                table: "SubscriptionTiers",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DropColumn(
                name: "CurrentTierId",
                table: "UserProfiles");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "deed9f79-e63f-4d14-b2e1-52d120fa9cb6", "AQAAAAIAAYagAAAAEArR0VIQiaeg8x2sPANdjTDY8OjLFOluY478fWTNKHd3/TfYh9NYx4xLUI1rPZeFtg==" });

            migrationBuilder.UpdateData(
                table: "SubscriptionTiers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DurationMonths", "Name", "Price", "ProductLimit" },
                values: new object[] { 6, "Basic", 500m, 25 });

            migrationBuilder.UpdateData(
                table: "SubscriptionTiers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "DurationMonths", "Name", "Price", "ProductLimit" },
                values: new object[] { 12, "Intermediate", 900m, 50 });

            migrationBuilder.UpdateData(
                table: "SubscriptionTiers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Name", "Price", "ProductLimit" },
                values: new object[] { "Premium", 1500m, 120 });
        }
    }
}
