using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMSECommerce.Migrations
{
    /// <inheritdoc />
    public partial class HussainaAddedAdminRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[] { "5f90378b-3001-443b-8736-411a91341c2c", "8e448304-2185-442e-a342-6e210168d87d" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "8e448304-2185-442e-a342-6e210168d87d",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "bb0f007a-1758-4842-bcef-9d193b6e8754", "AQAAAAIAAYagAAAAELCxcgFKUfjpbPF83bLq6wUvc7FV6iGmIqBhQZPOt5uVpgMX/By20N9HcbU/o5cPqw==" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "cfdbf2ca-676a-4e62-a2f0-1f8ebddd862a", "AQAAAAIAAYagAAAAEIHfhB9oZUYxHkdWQp7S96en/RwZVafFrzTKCPF7lF8tEWzx+bdd6MG57hmo91RXFQ==" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "5f90378b-3001-443b-8736-411a91341c2c", "8e448304-2185-442e-a342-6e210168d87d" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "8e448304-2185-442e-a342-6e210168d87d",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "5a7b233f-f1dc-4474-9074-054e058e79b2", "AQAAAAIAAYagAAAAECm82fjfqgdt4wdAcNAPuJanu0yiv0ATlFAtMCqEhcIaBYzPNnZOfJFoa1qxup25kw==" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "c0ddee31-54b6-4951-96cc-755d3db7479c", "AQAAAAIAAYagAAAAEBLOabEu19XtBimD/ShAhHjznlxOJEX8BwEupRUQ29hVwH8jDTcl3a+Zx3EOBcQ1Ng==" });
        }
    }
}
