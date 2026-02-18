using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMSECommerce.Migrations
{
    /// <inheritdoc />
    public partial class PurgeAbdulSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "5f90378b-3001-443b-8736-411a91341c2c", "a05-8265d3-05b8-4766-adcc-ca43d3960195" });

            migrationBuilder.DeleteData(
                table: "UserProfiles",
                keyColumn: "Id",
                keyValue: 104);

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a05-8265d3-05b8-4766-adcc-ca43d3960195");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "8e448304-2185-442e-a342-6e210168d87d",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEN/G5FGak+1GGrG4Ii6mjuuoF2kHa/09BWR6unWEgXGjSIQXRQA14wlRg9OnYQYzvA==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a02-8265d3-05b8-4766-adcc-ca43d3960192",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEGh9WMtRfWMnBSaqy3SRc+IqoESjLIl5A34KEmoTKq4BU6l/ZJwU0t0fnj57dlyqng==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAELRkDPZiDGTB9ZxqzOsYAOvDRbmW6wViMc78pZt6ghe2ovboHGUqswVlhU8NPYExYQ==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b72c9184-e4d2-4e5a-9391-7241065162a0",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAECDYUkwktvEA0FZXmyLeQXVuKp8Z6zatdE7a1/6BAtB13PkKKw1anXe2WoEY66Fg7Q==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "h07-8265d3-05b8-4766-adcc-ca43d3960197",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEGqUIHNXQs6VSBI1+3V8FBj8/y6yjRxi2r6qpdp9/tai7KxcxSYxGD8aCLNRIKUPgg==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "m01-8265d3-05b8-4766-adcc-ca43d3960191",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEDzuoLXdvx7yIWZr5kZzr7QH6zOVj0ac+Paw+snRy0P8or8DuGe4E2oAyw31CX3dMg==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "t03-8265d3-05b8-4766-adcc-ca43d3960193",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEFMNxiY6KhYWahlUYCWOVKZiovefYWmZE0BdRbSHUYGRpPCWBf4cQdBY8SWY4tmd7Q==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "t04-8265d3-05b8-4766-adcc-ca43d3960194",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEPM20SGcWDERiDLrVMBHjddi7zRzwu+mVytwhpI9Aunm/fkD8W/CMEvcNuFy+7wzMw==");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
                keyValue: "h07-8265d3-05b8-4766-adcc-ca43d3960197",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAECSQyre47SzRnqUsoLKS9lNovFkWjQLfyAiulVRqwcW/aP72at4dQmNcnBm/KSId6g==");

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
                values: new object[] { "a05-8265d3-05b8-4766-adcc-ca43d3960195", 0, "a05-8265d3-05b8-4766-adcc-ca43d3960195", "abdulqadirlokhandwalaandwala@gmail.com", true, false, null, "ABDULQADIRLOKHANDWALAANDWALA@GMAIL.COM", "ABDULQADIRLOKHANDWALAANDWALA@GMAIL.COM", "AQAAAAIAAYagAAAAEHsPZ772izLrDa7MHJVqsrV6BpYrsAI4UMZ5brYoA3S5V6SK9c5RalcTEu1MXOavwA==", "9121835054", false, "87f1eeee-c426-76d5-d41b-ce297eb8f35d", false, "abdulqadirlokhandwalaandwala@gmail.com" });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[] { "5f90378b-3001-443b-8736-411a91341c2c", "a05-8265d3-05b8-4766-adcc-ca43d3960195" });

            migrationBuilder.InsertData(
                table: "UserProfiles",
                columns: new[] { "Id", "About", "BusinessAddress", "BusinessPhoneNumber", "CurrentProductLimit", "CurrentTierId", "FacebookUrl", "FirstName", "GpayQRCodePath", "HomeAddress", "HomePhoneNumber", "ITSNumber", "InstagramUrl", "IsDeactivated", "IsImageApproved", "IsImagePending", "IsProfileVisible", "LastName", "LinkedInUrl", "PendingProfileImagePath", "PhonePeQRCodePath", "Profession", "ProfileImagePath", "ServicesProvided", "StoreId", "SubscriptionEndDate", "SubscriptionStartDate", "UserId", "WhatsAppNumber" },
                values: new object[] { 104, "Admin", "Hyderabad", null, 1000, null, null, "Abdulqadir", null, "Hyderabad", null, "100005", null, false, false, false, true, "Lokhandwala", null, null, null, "Admin", null, null, null, null, null, "a05-8265d3-05b8-4766-adcc-ca43d3960195", "9121835054" });
        }
    }
}
