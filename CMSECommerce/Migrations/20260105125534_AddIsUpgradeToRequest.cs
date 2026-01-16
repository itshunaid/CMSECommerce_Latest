using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMSECommerce.Migrations
{
    /// <inheritdoc />
    public partial class AddIsUpgradeToRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsUpgrade",
                table: "SubscriptionRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "d147a5d1-60b5-4592-b6a0-f72648523fbd", "AQAAAAIAAYagAAAAEG6yblYRHZKsdmrxvlg2LomImiBymLL9cr1qqCWTEvZQlS0T2cNsmCP29vYA5H+Xfg==" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsUpgrade",
                table: "SubscriptionRequests");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "59b18b8c-5c86-4feb-b5ec-0a3fee8c1703", "AQAAAAIAAYagAAAAEGjpXhK1YeElmONzBkar6hvCRhqhvJKq9Ka8VbTF027NlYOLqXKbQeCGm2y1xo1CuQ==" });
        }
    }
}
