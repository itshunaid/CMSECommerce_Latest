using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMSECommerce.Migrations
{
    /// <inheritdoc />
    public partial class CreateUserAgreementWithContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FullContent",
                table: "UserAgreements",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "deed9f79-e63f-4d14-b2e1-52d120fa9cb6", "AQAAAAIAAYagAAAAEArR0VIQiaeg8x2sPANdjTDY8OjLFOluY478fWTNKHd3/TfYh9NYx4xLUI1rPZeFtg==" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FullContent",
                table: "UserAgreements");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "a9987d3b-36bc-4ef8-ada4-0ffc8008271b", "AQAAAAIAAYagAAAAELa6lPyUo8M158q5cf4KH1G4Du8VBGjbz2w3FTsoYwd7N8XbfgZpRuNhpX3uq7FK5g==" });
        }
    }
}
