using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CMSECommerce.Migrations
{
    /// <inheritdoc />
    public partial class ReseedCorrectedUsersFinal : Migration
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
                value: "AQAAAAIAAYagAAAAEETlpxUk/VuI9w9EeAJL4vyZvG805WRA4kVDmWHbohjxT9Wx0hyar2Ba5ur7jNGlMw==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a02-8265d3-05b8-4766-adcc-ca43d3960192",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEG7tQtAomf2wiNwWc9lserRueZ/jtdI+r/VDwv2xKhiXassWimo+T9njg4gjMPp4QQ==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEGl265m+rxZj1BUrcKVTfFs8PLZWIC73z4bdtPcid73FE82JHVEFI9hTJBF3vjKYtg==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b72c9184-e4d2-4e5a-9391-7241065162a0",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEHjtA4Zlh2sWN8GPVzoBix9/Zbm7Tx7125vmzAHTQwf4D3ewNVrleQ9oOQWaZz1Ywg==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "h07-8265d3-05b8-4766-adcc-ca43d3960197",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEOjDXAjLoCXb9ZOxNvGibbLlLwUPDvEH1+Gzdqym6UG+0Tuw2J38cFQaYZWc5cX38Q==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "m01-8265d3-05b8-4766-adcc-ca43d3960191",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEKIsXHjzP9dOg9KZ8M0NAMuKUuJgmO6SbLALGDaCmZ8ZpxsA04jmVn0lD2qgKNXnoQ==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "t03-8265d3-05b8-4766-adcc-ca43d3960193",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEG5DgcJP1kUUEEcKRHPXBDtvf0gORLQMmXc1YSqiYZarfKwKUNbght/qLp2WOjj06A==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "t04-8265d3-05b8-4766-adcc-ca43d3960194",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAECPCzvlkHtnVSN8sZun6Ty+n51Lz4q2a8cWowSFWikdfwj/i0b8GF5chrFz4BIpDRA==");

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "Email", "EmailConfirmed", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UserName" },
                values: new object[,]
                {
                    { "ab05-8265d3-05b8-4766-adcc-ca43d3960101", 0, "ab05-8265d3-05b8-4766-adcc-ca43d3960101", "abdulqadirlokhandwalaandwala@gmail.com", true, false, null, "ABDULQADIRLOKHANDWALAANDWALA@GMAIL.COM", "ABDULQADIRLOKHANDWALAANDWALA@GMAIL.COM", "AQAAAAIAAYagAAAAEPxIXt4kddK2gXpFvEhEmq6K/puWnvzew0pYJqlISDGX+kroTw5Hye4sogMh3INd/A==", "9121835054", false, "e4ed174e-5570-fec3-a35b-9ace342637c7", false, "abdulqadirlokhandwalaandwala@gmail.com" },
                    { "ak09-8265d3-05b8-4766-adcc-ca43d3960103", 0, "ak09-8265d3-05b8-4766-adcc-ca43d3960103", "akframes@gmail.com", true, false, null, "AKFRAMES@GMAIL.COM", "AKFRAMES@GMAIL.COM", "AQAAAAIAAYagAAAAEIxE7niJWrgmrwE9D1iqDv3z8Dc4xDPtJz+BcALSMPqHitfdF2U5c2zZ0qS5dr+NaA==", "9949521090", false, "566a6d6b-ea65-a481-5f9d-6adbccda0222", false, "akframes@gmail.com" },
                    { "al08-8265d3-05b8-4766-adcc-ca43d3960102", 0, "al08-8265d3-05b8-4766-adcc-ca43d3960102", "alaqmarak0810@gmail.com", true, false, null, "ALAQMARAK0810@GMAIL.COM", "ALAQMARAK0810@GMAIL.COM", "AQAAAAIAAYagAAAAEHrFyl25AgN+uQdViRnKGMqLxxo7IYcGkjMnYQUP07zwQqmloZbMHWxpYyoQ/mW/eg==", "9618443558", false, "9e3226c9-7e1e-488b-ee65-cb487dc7b5ba", false, "alaqmarak0810@gmail.com" },
                    { "kh10-8265d3-05b8-4766-adcc-ca43d3960104", 0, "kh10-8265d3-05b8-4766-adcc-ca43d3960104", "Mohdkhuzaima@gmail.com", true, false, null, "MOHDKHUZAIMA@GMAIL.COM", "MOHDKHUZAIMA@GMAIL.COM", "AQAAAAIAAYagAAAAEL+FS7wy1wqTWVqwUtf3o1HW8Rmr4pgZ9n+pelf44+qJ3U2DRDc/sQsVto1aMtLBfg==", "9989664052", false, "c48607b7-474c-e30d-f8b6-b3d6f76445f1", false, "Mohdkhuzaima@gmail.com" }
                });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[,]
                {
                    { "5f90378b-3001-443b-8736-411a91341c2c", "ab05-8265d3-05b8-4766-adcc-ca43d3960101" },
                    { "5f90378b-3001-443b-8736-411a91341c2c", "ak09-8265d3-05b8-4766-adcc-ca43d3960103" },
                    { "5f90378b-3001-443b-8736-411a91341c2c", "al08-8265d3-05b8-4766-adcc-ca43d3960102" },
                    { "5f90378b-3001-443b-8736-411a91341c2c", "kh10-8265d3-05b8-4766-adcc-ca43d3960104" }
                });

            migrationBuilder.InsertData(
                table: "UserProfiles",
                columns: new[] { "Id", "About", "BusinessAddress", "BusinessPhoneNumber", "CurrentProductLimit", "CurrentTierId", "FacebookUrl", "FirstName", "GpayQRCodePath", "HomeAddress", "HomePhoneNumber", "ITSNumber", "InstagramUrl", "IsDeactivated", "IsImageApproved", "IsImagePending", "IsProfileVisible", "LastName", "LinkedInUrl", "PendingProfileImagePath", "PhonePeQRCodePath", "Profession", "ProfileImagePath", "ServicesProvided", "StoreId", "SubscriptionEndDate", "SubscriptionStartDate", "UserId", "WhatsAppNumber" },
                values: new object[,]
                {
                    { 110, "Admin", "Hyderabad", null, 1000, null, null, "Abdulqadir", null, "Hyderabad", null, "100011", null, false, false, false, true, "Lokhandwala", null, null, null, "Admin", null, null, null, null, null, "ab05-8265d3-05b8-4766-adcc-ca43d3960101", "9121835054" },
                    { 111, "Admin", "Hyderabad", null, 1000, null, null, "AL AQMAR", null, "Hyderabad", null, "100012", null, false, false, false, true, "KANCHWALA", null, null, null, "Admin", null, null, null, null, null, "al08-8265d3-05b8-4766-adcc-ca43d3960102", "9618443558" },
                    { 112, "Admin", "Hyderabad", null, 1000, null, null, "Abdul", null, "Hyderabad", null, "100013", null, false, false, false, true, "Khader Patanwala", null, null, null, "Admin", null, null, null, null, null, "ak09-8265d3-05b8-4766-adcc-ca43d3960103", "9949521090" },
                    { 113, "Admin", "Hyderabad", null, 1000, null, null, "Khuzaima", null, "Hyderabad", null, "100014", null, false, false, false, true, "Saeed", null, null, null, "Admin", null, null, null, null, null, "kh10-8265d3-05b8-4766-adcc-ca43d3960104", "9989664052" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "5f90378b-3001-443b-8736-411a91341c2c", "ab05-8265d3-05b8-4766-adcc-ca43d3960101" });

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "5f90378b-3001-443b-8736-411a91341c2c", "ak09-8265d3-05b8-4766-adcc-ca43d3960103" });

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "5f90378b-3001-443b-8736-411a91341c2c", "al08-8265d3-05b8-4766-adcc-ca43d3960102" });

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "5f90378b-3001-443b-8736-411a91341c2c", "kh10-8265d3-05b8-4766-adcc-ca43d3960104" });

            migrationBuilder.DeleteData(
                table: "UserProfiles",
                keyColumn: "Id",
                keyValue: 110);

            migrationBuilder.DeleteData(
                table: "UserProfiles",
                keyColumn: "Id",
                keyValue: 111);

            migrationBuilder.DeleteData(
                table: "UserProfiles",
                keyColumn: "Id",
                keyValue: 112);

            migrationBuilder.DeleteData(
                table: "UserProfiles",
                keyColumn: "Id",
                keyValue: 113);

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "ab05-8265d3-05b8-4766-adcc-ca43d3960101");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "ak09-8265d3-05b8-4766-adcc-ca43d3960103");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "al08-8265d3-05b8-4766-adcc-ca43d3960102");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "kh10-8265d3-05b8-4766-adcc-ca43d3960104");

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
