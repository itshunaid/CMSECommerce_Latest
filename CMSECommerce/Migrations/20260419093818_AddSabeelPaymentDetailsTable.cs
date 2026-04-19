using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMSECommerce.Migrations
{
    /// <inheritdoc />
    public partial class AddSabeelPaymentDetailsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SabeelPaymentDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ITSNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UTRNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SabeelPaymentDetails", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "8e448304-2185-442e-a342-6e210168d87d",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAECzBp3gFGXq4c13tlkqomzEeGL6E6bPSwFFogEJk8sAVA03FOZo4hocmTwbE1Uq+Qw==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a02-8265d3-05b8-4766-adcc-ca43d3960192",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAECiVVQU3QLSYJJ3sLPIjaQi4wqweZ7Y3kVw9Quit14N5lg6VOM80qYCZSK1HqKsRUw==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAENuz7t2vlOiPOxxOxv5B1dGY1+W+8XXCm+euyMKzzjmCcxwo3u64jAT23bk6IfqGRQ==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "ab05-8265d3-05b8-4766-adcc-ca43d3960101",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEHzsHwkIXppcVlzd6O6DsdI5xDRMHybrLZr8ZT6YCkBIy71IXzslZKNVFYRDaaIzhQ==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "ak09-8265d3-05b8-4766-adcc-ca43d3960103",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEPhDZFdjOZXBnkurEGUJfvOtrbRilBhP6LNUsp+G5E0uc1HCsSRjN1SLFaBfR9v9XQ==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "al08-8265d3-05b8-4766-adcc-ca43d3960102",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEKHsPMzcfYr0OgKubrSLI3o2SU8T6xlZ0QWew4v8yKu8bq1+Rav5fKqH7GrywGyXtg==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b72c9184-e4d2-4e5a-9391-7241065162a0",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAECEPrmN1cL+owzfdECtQaDqaS/BPkEWFeDYUoN+S+yGrx4VR1CYz7FnvxGyugFg0jw==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "h07-8265d3-05b8-4766-adcc-ca43d3960197",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEIhR5sihO++MNuxfncanks0v+5tdUgabBhhdVWgLnUXrUnCuKBN5oyyNWBZH5Q06lw==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "kh10-8265d3-05b8-4766-adcc-ca43d3960104",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEJCnWLluqHse9JZMvvZIiaD57LaF7YqSSajN3zDhjGndjENrdicqjV3rMWCC4p8BcQ==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "m01-8265d3-05b8-4766-adcc-ca43d3960191",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAENnDeEMZX7D0MOBuDMqiU3NyCGEZNu+v/IYwuZMC7Rxj7RT8gInmwpaJEaqNaI3B7Q==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "t03-8265d3-05b8-4766-adcc-ca43d3960193",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEDeAgkerTNdFmvkeRJYBNkOFrxpBkAKvP81C3W6nhX02ZHo/h0Nzy1tSLv8v9kjXFw==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "t04-8265d3-05b8-4766-adcc-ca43d3960194",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEBzT67d/pJg02CBN5F05jgfIhvAsFPEOmCAw5yloJKZqBBbs/KxEwOygNMlUT/7kYg==");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SabeelPaymentDetails");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "8e448304-2185-442e-a342-6e210168d87d",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEKFd1qcL39l8v9X+Owe90EJFwfgVNBKOYrVYKtaoluVOLGVgz86at5WhHh3FVetWYw==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a02-8265d3-05b8-4766-adcc-ca43d3960192",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAELHIrSbjZfRWPbISzUzsfesA4rpSGAmPV/GSBLHI6r+oycOGWI1m5ErLl3kuhVpFXw==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEP8MA/WmhjaWaHWTKS5qj6nxNCRxmVMRMDE0bSxxITWy6eSCJU1LjigSGitTAkO+Bg==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "ab05-8265d3-05b8-4766-adcc-ca43d3960101",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEO/BFlUbuV+nxyQotPw/9nIf1XUtUYxZjrKzF02nWP3+lzIEx4GTXCL887Kc9jbfuw==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "ak09-8265d3-05b8-4766-adcc-ca43d3960103",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAELtZ31auqI2ge/kTqunOl0o6oVKxR25F5fSkaKKtOR6co2Oz07hJltlj/ieTMDkZ0w==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "al08-8265d3-05b8-4766-adcc-ca43d3960102",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEJerluYMiIJ+KKj7liwZ8QhrPookKvuhZseE4DCFtJ62/aU7ysU0xieiOObww0Nn2A==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b72c9184-e4d2-4e5a-9391-7241065162a0",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAED1TWj2/4LOyqEVr8fKEReKnUrt18lghMeY1m5vkQ3GfraB7+0SLVDIl9/mN9xDxMw==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "h07-8265d3-05b8-4766-adcc-ca43d3960197",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEDY2os2mHa9yd3yQ3p5/W4S16DReKKFy/8GtR5LtUZ0rcys69FEUBLkt7muLzO3ucw==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "kh10-8265d3-05b8-4766-adcc-ca43d3960104",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEDgcdrUW6kmOFLFnwTreDqSL42GJ54PiSrx2Lz8nkmkpIrbcqNRLa+t2xuotynlMrw==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "m01-8265d3-05b8-4766-adcc-ca43d3960191",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEO10kHIheLL31CDiXg1epqh/KhiIXZ4qkHwSXkBwTEx3IWN1Yt5KlYMpDf1/U/v9Iw==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "t03-8265d3-05b8-4766-adcc-ca43d3960193",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEPFGEO1RBS/NCSYgFaqPmt3bq4igZlTgmkuHP4yaG4WVDK3cCia9FWE2G1KEDngVwg==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "t04-8265d3-05b8-4766-adcc-ca43d3960194",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAECycW80XHJiinpggv+mCtDGJgdIadhdsmWIDdrIU/InpXHTsRG5METTtPXZqtqlviA==");
        }
    }
}
