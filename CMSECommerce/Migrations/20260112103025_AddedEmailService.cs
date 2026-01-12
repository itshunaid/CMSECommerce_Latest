using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMSECommerce.Migrations
{
    /// <inheritdoc />
    public partial class AddedEmailService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "fece526d-dd39-440b-8405-b15355a8118a", "AQAAAAIAAYagAAAAEK/i5wjNAp9f2cKeusgyo+d1QA+Tfsyx4hZeAGW4baGAzzPgQyoMcTTHJJCy2Oe/9A==" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "39d78532-7bd4-4ab5-a5a4-030dcfaeaf59", "AQAAAAIAAYagAAAAEImctZWYYGoHDnBNPkgFiSRkG/X7TUc0MdQNOs/Vl29Jj4emIicCY7X14e9iNuxPAw==" });
        }
    }
}
