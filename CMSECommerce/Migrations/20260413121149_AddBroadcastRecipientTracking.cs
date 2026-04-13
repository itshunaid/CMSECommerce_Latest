using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMSECommerce.Migrations
{
    /// <inheritdoc />
    public partial class AddBroadcastRecipientTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BroadcastRecipients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BroadcastMessageId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BroadcastRecipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BroadcastRecipients_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BroadcastRecipients_BroadcastMessages_BroadcastMessageId",
                        column: x => x.BroadcastMessageId,
                        principalTable: "BroadcastMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_BroadcastRecipients_BroadcastMessageId",
                table: "BroadcastRecipients",
                column: "BroadcastMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_BroadcastRecipients_UserId",
                table: "BroadcastRecipients",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BroadcastRecipients");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "8e448304-2185-442e-a342-6e210168d87d",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEDcw6LhK9roT7sdmuw2YFumo7QxS/Hd5sjWjTbcQOCWrfu+JBb6AcyF6V5AAL200Hw==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a02-8265d3-05b8-4766-adcc-ca43d3960192",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAECuWM+pYD5Y0ExKwArcTYu4KF4KDgDadDKr0hwfz9mWNHxgMgFM1wtTvWKPfDMp0aA==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEKJWcRfLCS5r/xTIgIfJcAhfsmG8vYeWeilKSNtDFKRCU0Pifkw54/dyB2LZTIwcVg==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "ab05-8265d3-05b8-4766-adcc-ca43d3960101",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEGcb8Ahua2gk6kTRPxhUY+C5Rlfjx3SHdJ0EfbUgSPndWOwNWDw/ucCvx/A14IlWMA==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "ak09-8265d3-05b8-4766-adcc-ca43d3960103",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEKrY0gPcFyQ1oedY6KOWadEea0H9pZPlqsFQ5gkg4MMs4besTC2a8RYqy058UI3lLw==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "al08-8265d3-05b8-4766-adcc-ca43d3960102",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEIpPUDHoXGH1HQZ6GTqTrui7v/i/TiHeGvgkQhLHLAIhyUqY3KGMJJyWG6skimYDOQ==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b72c9184-e4d2-4e5a-9391-7241065162a0",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEN+X1K2uh+pRiRYDLioWlPbfXmc23GRdaHHxb1zU/IFeJ/GyPxc+SVlBuu/kpoi3PQ==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "h07-8265d3-05b8-4766-adcc-ca43d3960197",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEDGXvYX7X21lBBfcEQZacI4EKkQM1anrmk1Dgbxsemh913tfVyvUW8A1UpjI5081Dg==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "kh10-8265d3-05b8-4766-adcc-ca43d3960104",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAELTg8O0fB0S0YC4nkkQPajIZxb6XdAfmN0O1omMoHE743gJVfOGWYNEO8cj4B4kuBg==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "m01-8265d3-05b8-4766-adcc-ca43d3960191",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEKGQhO2tetmWguHk7Z64aN7kIKRkuXPmRizWchP6iSvzKuh21phAMQ4N1TyAzs/yKQ==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "t03-8265d3-05b8-4766-adcc-ca43d3960193",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEG1ASchweoBZR4w4JbkTukQ1MmNXOdssWii1TMrnkbvVpc64P9zfrkPWelZ9/h7XDA==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "t04-8265d3-05b8-4766-adcc-ca43d3960194",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEBlxO1TgidSRFB2CDv4MKSFVBUdnSkL2Tha/TL+AK4H2Um7C/2df2a1JcIQyadUpIA==");
        }
    }
}
