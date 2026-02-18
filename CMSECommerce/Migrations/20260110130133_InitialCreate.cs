using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMSECommerce.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "674f263f-f5ce-4d56-9ceb-408af876894d", "AQAAAAIAAYagAAAAEPGXQlZaCDSyhLHlKnypDkIL65j681yToAaWhGaBAJ81YCDoFgBd/0W35X9EkImlqw==" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "78088d52-5b2d-4f5b-a03a-9537fdc27ebb", "AQAAAAIAAYagAAAAEDyYSctV3EVzR4SZ+JZXHAYr5GKAm2E80fdeBG4PmkVzyd0J0JwbgQb85aw0BEGL3Q==" });
        }
    }
}
