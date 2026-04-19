using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMSECommerce.Migrations
{
    /// <inheritdoc />
    public partial class RefactoredSabeelPaymentDetailsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ITSNumber",
                table: "SabeelPaymentDetails",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "SabeelPaymentDetails",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ITSNumber",
                table: "SabeelPaymentDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "SabeelPaymentDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "8e448304-2185-442e-a342-6e210168d87d",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEAnxScR1C8z3M6x636rUJVDT/38lqQv+yIu2I7cgdahXwMSK+fCQTQb4vH7NtgFD+w==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a02-8265d3-05b8-4766-adcc-ca43d3960192",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAELYE1XdHfTFocNXiIxLxiQljSpspsFXDCZAeTVbbsxHCMdcJ/080/cpVAPW5/WOAYg==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAELnWJ8P7Z8YQ/qLCEQnU0pjzy4w39cBPZfRmwXGzCbMrPryX+aX3oq7B9xeoLif+gg==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "ab05-8265d3-05b8-4766-adcc-ca43d3960101",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEFUE0Hbc34N7iU2R0GJgYR83lwie7+T70IDVDEOh2mFlV+dVRJaYispLeIpMbIyzNg==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "ak09-8265d3-05b8-4766-adcc-ca43d3960103",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEMc1xzcD9NEn0lNwqrAR9LHG6hGH8kKSbPGjTPAw2o7w2ebTtLQm8eWFOjwSrh/pEw==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "al08-8265d3-05b8-4766-adcc-ca43d3960102",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEI65/WCMXgO2xme5xU8/7JCMPv7EIgC8lXi5m3SbaWQuFcuWb9YK3SAt7oBux1ULyw==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b72c9184-e4d2-4e5a-9391-7241065162a0",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAELTkWTqqKOLUNefEwYxwUXnw6pB1HdkvtICeZVZUNDtS5JrIEZJnZPAr1Bu5+hSJQA==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "h07-8265d3-05b8-4766-adcc-ca43d3960197",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEBcn1MElDtRoDKtEIEj30cEhiRMNrj9zblXPaFwLhDRWTWHEPXVxiCEN15tCLGngkQ==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "kh10-8265d3-05b8-4766-adcc-ca43d3960104",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEHFOwogNaPYVfRU3uX85Xsr0feccOG1D0aUwePqPpWstkwsxb+sogs01ZS15Ik+3IQ==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "m01-8265d3-05b8-4766-adcc-ca43d3960191",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEO/Uc4vLuoDxrPzUA4oaxLu5NoKTHGwLRuzdcPEyMtqf9EpscLC+bORBq2xos7aCvw==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "t03-8265d3-05b8-4766-adcc-ca43d3960193",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEKrix4t1SQjhaUBJ7tpS6kEp0tznlxpKq+nDG3Fjn1mjN5s8kIypWq95MQl8HNiaFA==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "t04-8265d3-05b8-4766-adcc-ca43d3960194",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEI8VkwOGn3/s1laZQtYGXWKLl4Rkirv10BS4WHwCmibrH0QrLiesaxSTOXIxwGwHvg==");
        }
    }
}
