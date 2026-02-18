using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMSECommerce.Migrations
{
    /// <inheritdoc />
    public partial class AddFairCreditFieldsToSubscriptionRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CreditApplied",
                table: "SubscriptionRequests",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalAmount",
                table: "SubscriptionRequests",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "f8d43312-2613-4ab0-9aad-cf69efb6c7b4", "AQAAAAIAAYagAAAAEK2ML37ZMAfgbslF8natbTJ/2Qg0vS1RiuzIJ1LUlFr0DiEWVbUgKWaov61gYFSDUA==" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreditApplied",
                table: "SubscriptionRequests");

            migrationBuilder.DropColumn(
                name: "FinalAmount",
                table: "SubscriptionRequests");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "c0dcc47e-500b-4758-99c7-9d236ef78974", "AQAAAAIAAYagAAAAEOuP+9vM8Xmz40pgGlN/ddIvzNYyKW36KFzCpmZVPih8nfQxtTZ1r+n4XQyAXFCkrw==" });
        }
    }
}
