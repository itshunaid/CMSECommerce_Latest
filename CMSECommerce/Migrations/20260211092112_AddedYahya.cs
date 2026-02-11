using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMSECommerce.Migrations
{
    /// <inheritdoc />
    public partial class AddedYahya : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "8e448304-2185-442e-a342-6e210168d87d",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEDOmqodBGrId2rP9V3sfGD3aHswe2k7Iv3XczzCiXsAIuKesAoeDPOXkkPbBnvNRNQ==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a02-8265d3-05b8-4766-adcc-ca43d3960192",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEGNe7Ibw3jCOM6ZsVKwCNwAnmWcJO0LAkGEccwMyfDceDzKfqyBg1Fi2kwxsRKhJXg==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a05-8265d3-05b8-4766-adcc-ca43d3960195",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEFIDQcfuO76WswSNhr3/tuFZX4w9Zq2wIlNfNXHonDNz9Wr/aKEygDvWMyiSzjrPyA==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEOlHYSDru+857kTzVlNT6FtO6puAYEx4Brb+pBCz1Nu6defT88PPzLLQLdKBlm5DBg==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "m01-8265d3-05b8-4766-adcc-ca43d3960191",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEKdWy3jCGDc+zTCeOfDZLxTC0IbQMF0CeuxAI50/OwA9SPpxaQjlmoKjqmcWeB5bcA==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "t03-8265d3-05b8-4766-adcc-ca43d3960193",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEGSBahU6DaMrD0MivJlCyqWWWqYeWWPgopO84KiONlVFb8Wj4SOmb/89Npdo2/cSqQ==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "t04-8265d3-05b8-4766-adcc-ca43d3960194",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAENm5z8FV369okpjD7veCGfo7+RmUkRVejtTX62wq7PxKwY6lQDTx5WzyhdTO6j5m8w==");

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "Email", "EmailConfirmed", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UserName" },
                values: new object[] { "b72c9184-e4d2-4e5a-9391-7241065162a0", 0, "b72c9184-e4d2-4e5a-9391-7241065162a0", "yaliasger@yahoo.co.in", true, false, null, "YALIASGER@YAHOO.CO.IN", "YALIASGER@YAHOO.CO.IN", "AQAAAAIAAYagAAAAELlcZ9IOHalRFzMmlQf5WcFDxPE7at6ZTC4EF/xNEcY8APscJ1Yr9DDV3Qi/bwd5jw==", "9130211052", false, "868c12ff-161a-73f1-97dd-b9283583441c", false, "yaliasger@yahoo.co.in" });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[] { "5f90378b-3001-443b-8736-411a91341c2c", "b72c9184-e4d2-4e5a-9391-7241065162a0" });

            migrationBuilder.InsertData(
                table: "UserProfiles",
                columns: new[] { "Id", "About", "BusinessAddress", "BusinessPhoneNumber", "CurrentProductLimit", "CurrentTierId", "FacebookUrl", "FirstName", "GpayQRCodePath", "HomeAddress", "HomePhoneNumber", "ITSNumber", "InstagramUrl", "IsDeactivated", "IsImageApproved", "IsImagePending", "IsProfileVisible", "LastName", "LinkedInUrl", "PendingProfileImagePath", "PhonePeQRCodePath", "Profession", "ProfileImagePath", "ServicesProvided", "StoreId", "SubscriptionEndDate", "SubscriptionStartDate", "UserId", "WhatsAppNumber" },
                values: new object[] { 105, "Admin", "Hyderabad", null, 1000, null, null, "Yahya", null, "Hyderabad", null, "100009", null, false, false, false, true, "Aliasger", null, null, null, "Admin", null, null, null, null, null, "b72c9184-e4d2-4e5a-9391-7241065162a0", "9130211052" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "5f90378b-3001-443b-8736-411a91341c2c", "b72c9184-e4d2-4e5a-9391-7241065162a0" });

            migrationBuilder.DeleteData(
                table: "UserProfiles",
                keyColumn: "Id",
                keyValue: 105);

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b72c9184-e4d2-4e5a-9391-7241065162a0");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "8e448304-2185-442e-a342-6e210168d87d",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEJImCI3uZ1Y5xfI121LKphwM2BlR4Q/E+6UaPmvwDg+a0SgJHM3LI2ePfDW2rBGiaw==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a02-8265d3-05b8-4766-adcc-ca43d3960192",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEBcBkY5ps2zTsTkkT1WsdSB0eOvW0or2v+ibBKRh054sjSMn4UP9pFNd5OO1lA4i5w==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a05-8265d3-05b8-4766-adcc-ca43d3960195",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEN00A8gScuHZMHUcCfrGb5r9X0zho4HdpF0bz9SFkw4zMLFiD4SXs29QYPQcE6Pc1A==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEJjjFRjQgvzZd4c76ykn/u6Yyg+PbA90hG7MKdG+hkIiGHwaju8hcfjtZy1XVuHiwQ==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "m01-8265d3-05b8-4766-adcc-ca43d3960191",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEFd2VN8WcZy91LJV+wQEJ4zRu4v0zHG9kI1K/43GfYfOgq54nHMQvMY4PWHFaNQ/CA==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "t03-8265d3-05b8-4766-adcc-ca43d3960193",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAELPEyRbX5DUb1uKzzQVnrhA7UounCW1xF0jG2ryWoq+9Sadw7z6VT4ZX/L7dK4vyCQ==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "t04-8265d3-05b8-4766-adcc-ca43d3960194",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAELBCrOsG240aek/muXHrWMe5NjmJdmMu1lnaqIoRrtQL0+kGcs47U5eTUI/83aS7Pg==");
        }
    }
}
