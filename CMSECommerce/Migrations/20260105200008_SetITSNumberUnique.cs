using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMSECommerce.Migrations
{
    /// <inheritdoc />
    public partial class SetITSNumberUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ITSNumber",
                table: "UserProfiles",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "ac7392ed-43f7-48b4-b996-f78467dd9396", "AQAAAAIAAYagAAAAEL6lpepKWsd1dYr7TmPyP28LeYli/sxqJP3Ixm7nH9l+8qUmUlkPbFVSBJfOObKKcg==" });

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_ITSNumber",
                table: "UserProfiles",
                column: "ITSNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_ITSNumber",
                table: "UserProfiles");

            migrationBuilder.AlterColumn<string>(
                name: "ITSNumber",
                table: "UserProfiles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "d147a5d1-60b5-4592-b6a0-f72648523fbd", "AQAAAAIAAYagAAAAEG6yblYRHZKsdmrxvlg2LomImiBymLL9cr1qqCWTEvZQlS0T2cNsmCP29vYA5H+Xfg==" });
        }
    }
}
