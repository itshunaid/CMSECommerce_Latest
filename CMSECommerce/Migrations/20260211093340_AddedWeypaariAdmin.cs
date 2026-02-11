using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMSECommerce.Migrations
{
    /// <inheritdoc />
    public partial class AddedWeypaariAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "8e448304-2185-442e-a342-6e210168d87d",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEJC6T0ISCNOPfA/hHM1gY13HYDPuau6w1XR8Z0sx2v7Ou+h5aCN+pBBLnE++TIiMEg==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a02-8265d3-05b8-4766-adcc-ca43d3960192",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEHKL8sotoHyJ7kU2UhhQpzgEwe5KEbMie3r8/9MQf3yIQjBDBFHXKgYq/f1nvaGcfQ==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a05-8265d3-05b8-4766-adcc-ca43d3960195",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEHsPZ772izLrDa7MHJVqsrV6BpYrsAI4UMZ5brYoA3S5V6SK9c5RalcTEu1MXOavwA==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEMxBz74WHGvKRNfU5ISzomptuErUsTH7DrEuuoLu9EWlZvnpEV+HXEIWyXU6pRF0Ww==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b72c9184-e4d2-4e5a-9391-7241065162a0",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEJG8Dx60vtTq7mVWNfmty047tySN37en5+82VgDDqx75qCX2W6ob3hpsne1piNB9dg==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "m01-8265d3-05b8-4766-adcc-ca43d3960191",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEIOIwxpRD00OQD/I6fRilRvxtP+lCBHU0L8H+wRu+aQq6WNgsghZMeufM+wFfQS4+Q==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "t03-8265d3-05b8-4766-adcc-ca43d3960193",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAENdT5mGqFDQ404IuhlU9Wu0uzpQPXubUZ00joptTzwGq4tue9OqGMF/ocJjRb6gnQQ==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "t04-8265d3-05b8-4766-adcc-ca43d3960194",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEMTdEHG4+NOsnT9KBVi8WpyxQXvQSfsO2+34E+HRDIDNy3hzrk6T0LOeDWhK+YY2hw==");

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "Email", "EmailConfirmed", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UserName" },
                values: new object[] { "h07-8265d3-05b8-4766-adcc-ca43d3960197", 0, "h07-8265d3-05b8-4766-adcc-ca43d3960197", "weypaari@gmail.com", true, false, null, "WEYPAARI@GMAIL.COM", "WEYPAARI@GMAIL.COM", "AQAAAAIAAYagAAAAECSQyre47SzRnqUsoLKS9lNovFkWjQLfyAiulVRqwcW/aP72at4dQmNcnBm/KSId6g==", "9603302152", false, "c07fd76f-e4de-6578-3bf8-423548845c13", false, "weypaari@gmail.com" });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[] { "5f90378b-3001-443b-8736-411a91341c2c", "h07-8265d3-05b8-4766-adcc-ca43d3960197" });

            migrationBuilder.InsertData(
                table: "UserProfiles",
                columns: new[] { "Id", "About", "BusinessAddress", "BusinessPhoneNumber", "CurrentProductLimit", "CurrentTierId", "FacebookUrl", "FirstName", "GpayQRCodePath", "HomeAddress", "HomePhoneNumber", "ITSNumber", "InstagramUrl", "IsDeactivated", "IsImageApproved", "IsImagePending", "IsProfileVisible", "LastName", "LinkedInUrl", "PendingProfileImagePath", "PhonePeQRCodePath", "Profession", "ProfileImagePath", "ServicesProvided", "StoreId", "SubscriptionEndDate", "SubscriptionStartDate", "UserId", "WhatsAppNumber" },
                values: new object[] { 107, "Admin", "Hyderabad", null, 1000, null, null, "Weypaari", null, "Hyderabad", null, "100010", null, false, false, false, true, "Admin", null, null, null, "Admin", null, null, null, null, null, "h07-8265d3-05b8-4766-adcc-ca43d3960197", "9603302152" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "5f90378b-3001-443b-8736-411a91341c2c", "h07-8265d3-05b8-4766-adcc-ca43d3960197" });

            migrationBuilder.DeleteData(
                table: "UserProfiles",
                keyColumn: "Id",
                keyValue: 107);

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "h07-8265d3-05b8-4766-adcc-ca43d3960197");

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
                keyValue: "b72c9184-e4d2-4e5a-9391-7241065162a0",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAELlcZ9IOHalRFzMmlQf5WcFDxPE7at6ZTC4EF/xNEcY8APscJ1Yr9DDV3Qi/bwd5jw==");

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
        }
    }
}
