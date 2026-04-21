using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMSECommerce.Migrations
{
    /// <inheritdoc />
    public partial class AddedMoreProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JammatName",
                table: "AsharaAzamEntries",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "AsharaAzamEntries",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "8e448304-2185-442e-a342-6e210168d87d",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEKCPUPc8uIFUnCxEfpobcAvfih6rqTGN5ZLKUi117bmOUOhZr/FRVrCGeCeZpxaxEg==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a02-8265d3-05b8-4766-adcc-ca43d3960192",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEDbYDVQ9LMrqtH8cUywdQLX4W/z6RrIPh7epnB+a+yRXUMvW4Xb9C1ywmuZN0DHE8Q==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEHuo+94KfHfawv7lKweNal9Zqh4uZUC7RJ3uMgKFBdBuIub0w4s6Ph6G/vn0Wji/JA==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "ab05-8265d3-05b8-4766-adcc-ca43d3960101",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEBkFn8R2mAT1tI9VJ0mX51Fz+UdBDwiOmiKf3i+e6CUt2BNlUHdo8PvROy7ZJ8R0jQ==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "ak09-8265d3-05b8-4766-adcc-ca43d3960103",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEBdZYy25dsy7l4Kn/5XmIUwcBZuO5ziR6SU0DgTOz3Ne3xQRSgZM+d1zrR6Oivm4dw==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "al08-8265d3-05b8-4766-adcc-ca43d3960102",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEKDDEvA+0nmEdXkwQu36L1yWFuNpox9bU7FQdO9oVg2p3aLm7U7Fx+KhvdbCHXIbJA==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b72c9184-e4d2-4e5a-9391-7241065162a0",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAELzCuQJqlizlUbSvYpbdQXgYHW0OUz23w4fVkkg8GnI8D8I4q42iwaBJmXiD4p7URQ==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "h07-8265d3-05b8-4766-adcc-ca43d3960197",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEAQ7VGMvUuFHl4tGqAYxW2EnEfPBuMStm5BYkqdpQ7/iKFe2QI2Hpe+CgkeROFfqRw==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "kh10-8265d3-05b8-4766-adcc-ca43d3960104",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEC5bldaRSZKSZ9vDDxQglDazmWof2Y5hltRaZrZE+kW0O2pjo5G/Lw5GHyTAaj5TCg==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "m01-8265d3-05b8-4766-adcc-ca43d3960191",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEKz7lME6slfFKG9V8Mbi6rde8licOZk18fLH+5lStyoHYy26HKM5CXCUdePC70EFBA==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "t03-8265d3-05b8-4766-adcc-ca43d3960193",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEIA+n2C9Qam/J+KGcHyh2lA4KLI4i4cVZ+IMVzhzQxrMOUS7zQO4gDE+j8VzDH3LSg==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "t04-8265d3-05b8-4766-adcc-ca43d3960194",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEPb9r+rIdaOcq01NCdYHTmoT6af9+Jf4A8rTfrjzlAUoOnLmqFbwT1nIlMDKkkYHTg==");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JammatName",
                table: "AsharaAzamEntries");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "AsharaAzamEntries");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "8e448304-2185-442e-a342-6e210168d87d",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEN/pTY5Q/GXqNalQUJJcCGlQ0emPy8OhlE+LMA0Fanv4CuRTCnX8dBr4MVo+M20PiQ==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a02-8265d3-05b8-4766-adcc-ca43d3960192",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEHGZtIQ2/I9uaiCmqPVOKC3fSX3eHjBl7FWb+XUMcUU8o199Q5UfWrWF5Cv5TNcx/A==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEFcEqJbq2jAwIWBUg851ugy7t6TT16MLVphoKbDXsk+ctn5LucYYuB6Z7DDx2fa4tw==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "ab05-8265d3-05b8-4766-adcc-ca43d3960101",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEHe0IwEihk+eynvtBGbOCQv1o0cpk4b7/Lquag+HSCqhICU1BYikSJOmWWSe3Ca0Vw==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "ak09-8265d3-05b8-4766-adcc-ca43d3960103",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEGMlNMiibc0iItlInWhT79SxBtryQ5uD1HkTyI7qZQplYEptfkOu+B1Fma6a2RoCiA==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "al08-8265d3-05b8-4766-adcc-ca43d3960102",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEJsJaXulf15BUmgMk2igtNvmM/5GOLnaO7Ijqbka4mM+MCgj0fDmJ9Odp3tn/1DH9Q==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b72c9184-e4d2-4e5a-9391-7241065162a0",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAELiChjFsGJXph9Q5R9/4OamW+cn9xJKBVUMV/dDG7DiUoPPcSqJDBsK3PrkCrr+vtw==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "h07-8265d3-05b8-4766-adcc-ca43d3960197",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEII5HSnVP7p5iv3Tk4gneJhFFRt630SoccQ0E6+TtcSzIKOvZIO8VwgphpYJ0VVmsQ==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "kh10-8265d3-05b8-4766-adcc-ca43d3960104",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEAURLPngEYGwWCtGPuutP8xJgxY8aV4AM8z119pQu882Wh97+oQflVi2F+q/HZJebQ==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "m01-8265d3-05b8-4766-adcc-ca43d3960191",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEPc74OenilnOYj+bnavLdFJoTsTCHoHsnK6PjGdrVNSBnKlBQr/qxD4JsaX2QH46Bg==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "t03-8265d3-05b8-4766-adcc-ca43d3960193",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEIiYgnLp6nhgNy9m0Obm1cqpgl0KrtRWM9xUYr7JiLgGdz8YpcWWjQewNV78qw/vwA==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "t04-8265d3-05b8-4766-adcc-ca43d3960194",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAENsN8z6Bolgxa+iJodEO4j77cY0INrL2E6IZNKdE1WYp/oiff2+hq4Dj26IrvG2j7Q==");
        }
    }
}
