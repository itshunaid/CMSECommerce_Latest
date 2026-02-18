using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMSECommerce.Migrations
{
    /// <inheritdoc />
    public partial class RevertedCodeCgangesToCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "505e4aa1-63e9-442b-bf3f-07f68749b6d0", "AQAAAAIAAYagAAAAEPDrzLCDxdQwoPDf6p8wG2U60yUemc2UtK7z/g+L6CtDDEP5zzAamGsr4qGZANTJwg==" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "b2a96425-c88a-4d95-ba5a-24c4ad6645ca", "AQAAAAIAAYagAAAAEHuUHn/4F8/m9TudY3BXPfI6VH7DPWyPqdjzU7Ou5RtH9x+3I4qAtAD9AvZEj5EM/w==" });
        }
    }
}
