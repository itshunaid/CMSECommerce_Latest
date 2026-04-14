using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMSECommerce.Migrations
{
    /// <inheritdoc />
    public partial class AsharaMubarakaAzam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AsharaAzamEntries",
                columns: table => new
                {
                    ITSNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ConsentGiven = table.Column<bool>(type: "bit", nullable: false),
                    ConsentMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsharaAzamEntries", x => x.ITSNumber);
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "8e448304-2185-442e-a342-6e210168d87d",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAENPOswpeIrz2F7mK7/y9UlkNcyC1ZTcOGT53idUSzm8bVM3YItpcxmzSH5Qink4vpA==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a02-8265d3-05b8-4766-adcc-ca43d3960192",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEGwlycrnaW9eTsjX/0HIm7uGyFIsq67PuPVUvpgsPUEZjXtP7Wyar8o2Q47A1GkmOg==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEBrXcjaut9E2ICEVuaJ+PYBEovOUse44xQ0SIdg0cldsAjYaxPS38O8wYh10N6HEWw==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "ab05-8265d3-05b8-4766-adcc-ca43d3960101",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEOgkPp0qJwJaw6UF4A0JRP83+eLZ2UVvDXBZLeUbzdbTBMQDMIdQ93AJ++yrs1Jmfg==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "ak09-8265d3-05b8-4766-adcc-ca43d3960103",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEKc2LH8JRIkq17goQkZ+SAHTx4iAXUO8ARLtagXPAd3b0/jAfdaRlIIc+Ab0pt2Khg==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "al08-8265d3-05b8-4766-adcc-ca43d3960102",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEPsIyKUM2SGfuQg6qvtggBA4sH/sK9r4vs+dgOPg7Tb+zpHnkD2tZocL7atcFrnJdQ==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b72c9184-e4d2-4e5a-9391-7241065162a0",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEN//SvchfVRU+QfhkrA4/dYjEm5dEL8P351iQZqAS8qe2t7vijmpvYN/IGSLfrlQ+w==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "h07-8265d3-05b8-4766-adcc-ca43d3960197",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEHzbCxM/6Ncp5MAT2jEwC2UFwqPBUsgy3vCGCwUMrubeiVn4t4hgz91lzXo+ZLrEfg==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "kh10-8265d3-05b8-4766-adcc-ca43d3960104",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEAlwqPyjB+IMf3UE/+qEy+WIovCBxta+vVOGU9Qf0iXw6CeNS/vM3tAREVKB7Fou6A==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "m01-8265d3-05b8-4766-adcc-ca43d3960191",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEOzpiIgEEBjJuvUnU5+CB9MNgfKx3y/xIeJD6hTG3UKW37QPef8QQZ1yFOYAawxpPQ==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "t03-8265d3-05b8-4766-adcc-ca43d3960193",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEEgtZ0QdBJY97XVLET4wJNgF2a9jwjMDHtHskFYg2nkrwoMT4WTdV7bYVSB+SjSmSg==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "t04-8265d3-05b8-4766-adcc-ca43d3960194",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAECtMGjCMRaJSBF1KNgHq3lsZOa7bPVRbhizCBdn/wOIH21EfNc43dbs2xULSoNw9Cw==");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AsharaAzamEntries");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "8e448304-2185-442e-a342-6e210168d87d",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEHUpEee056ftXJ6I2h/jcXY244TuVMuZebABoMVi4zX40XVnLzWDzSokdk2VqN5+aA==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a02-8265d3-05b8-4766-adcc-ca43d3960192",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEOcPEya0QB+28Z+kzsCvk6C9NN2LZxfp40XmGMhaWE8vUFoaGv7JNubEat1gbZ4Cwg==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEGaFtNGOrkJQ2vv6ZdkPK2SJy8Qo+6XcB6wsT4GS4CtdmceTpwUFvWzAcuhSTtXezA==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "ab05-8265d3-05b8-4766-adcc-ca43d3960101",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEMGlqFzFp/tWvhBAAoAsEmEKHNDtgjD6MMYLf0iRGW9xyqe36uoZe4ODVuf3hN16Dg==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "ak09-8265d3-05b8-4766-adcc-ca43d3960103",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEHP/sS/AoDvgaFFG1eJgfwHR5qMYlClC1Z2DkrF/UCN8m8+nJ9ItumAQbktifEkaZA==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "al08-8265d3-05b8-4766-adcc-ca43d3960102",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAELFJpgX+Q+1ngv1neg4Rp2U6Dhx5chKjNKVp+Hq0RkIra6fc1b4LzWGGXEfl0jYtQA==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b72c9184-e4d2-4e5a-9391-7241065162a0",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEIhIwFE0EHLRxE8dbhs1SFkOwFPQv6gjRNiGAEaICXTUTa6lTwUDVhlmZGzzGWoG2w==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "h07-8265d3-05b8-4766-adcc-ca43d3960197",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEETp5sGeRgZJetZZ6zDeeFrbiNeECp4FmIFQOZKluvQkESzUhmsGDeBw+/RGeMbDPQ==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "kh10-8265d3-05b8-4766-adcc-ca43d3960104",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEDw6XEJklEMjLpwH+WosY5FKaxuP5JKtYp5A0iYBXBz8CFdrshuW2qX3kLGJKsd6aw==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "m01-8265d3-05b8-4766-adcc-ca43d3960191",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEDgIWhGO4K04y1GUKdx5hcdsaagz9E0pUltwma0a4z2m80OR0Y7rdOFCULyFhOjYkQ==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "t03-8265d3-05b8-4766-adcc-ca43d3960193",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEIHdavW70qHRKw78wv0yVWqCwtYLS9AZ/b//TwyHZGkl7h/J/CbvUgJgGG1t4ilYKA==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "t04-8265d3-05b8-4766-adcc-ca43d3960194",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEIAYhrQjjb3jI5OV7sGdq4ZI1viG+ktgDuDZpUGPVVIsIwX/yCd77tH1hrXjKXIisA==");
        }
    }
}
