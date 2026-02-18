using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMSECommerce.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCartAndProfileLogic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "bd3a99fd-1969-4468-b1a7-44edbcb5ca35", "AQAAAAIAAYagAAAAECRQHLQHyDwmUZexoAaz0lwFQtCzLQn1oIivX4LOrjrrV4qyAG7exilOxhNbOjpI0A==" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "2183b129-7f46-44ca-b0a4-d4c96533ea98", "AQAAAAIAAYagAAAAEDkzhAolIUdI1XkSG9B+6XvWZUsBN7FdFeA9boTybvWyLq/vtujFJLrGVRRP1hv/xg==" });
        }
    }
}
