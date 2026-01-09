using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMSECommerce.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovedAtApprovedByInSubscrictionRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "SubscriptionRequests",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy",
                table: "SubscriptionRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "e1492f17-d960-42c6-b506-3ea67211cbc3", "AQAAAAIAAYagAAAAEDUAZOeulxVqQLKguefVM0h3Rao+qNsbRf2aNw1BJUO11T7ldvKQx4jtlA7do2wgew==" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "SubscriptionRequests");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "SubscriptionRequests");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "72d6893b-8a14-43b1-8171-1da96908b88c", "AQAAAAIAAYagAAAAEPI15Qi7jhrm3xySbJfRPRiOPAuEPf3/z+JB+LAjj3AzjtKg4+s71xOeItULm7Ld3g==" });
        }
    }
}
