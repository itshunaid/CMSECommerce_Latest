using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMSECommerce.Migrations
{
    /// <inheritdoc />
    public partial class HussainaUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "c0ddee31-54b6-4951-96cc-755d3db7479c", "AQAAAAIAAYagAAAAEBLOabEu19XtBimD/ShAhHjznlxOJEX8BwEupRUQ29hVwH8jDTcl3a+Zx3EOBcQ1Ng==" });

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "Email", "EmailConfirmed", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UserName" },
                values: new object[] { "8e448304-2185-442e-a342-6e210168d87d", 0, "5a7b233f-f1dc-4474-9074-054e058e79b2", "hussaina@local.local", true, false, null, "HUSSAINA@LOCAL.LOCAL", "HUSSAINA", "AQAAAAIAAYagAAAAECm82fjfqgdt4wdAcNAPuJanu0yiv0ATlFAtMCqEhcIaBYzPNnZOfJFoa1qxup25kw==", null, false, "", false, "hussaina" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "8e448304-2185-442e-a342-6e210168d87d");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "338ec76a-c88e-4ac3-b3f0-ce030603e1be", "AQAAAAIAAYagAAAAECu4dTcIf47vGB2e485APsC9L8Ry8E/kGsSPMCuMUtkuRjC48mMVPZKYL63RyQilgg==" });
        }
    }
}
