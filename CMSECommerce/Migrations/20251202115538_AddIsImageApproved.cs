using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMSECommerce.Migrations
{
    /// <inheritdoc />
    public partial class AddIsImageApproved : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsImageApproved",
                table: "UserProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsImageApproved",
                table: "UserProfiles");
        }
    }
}
