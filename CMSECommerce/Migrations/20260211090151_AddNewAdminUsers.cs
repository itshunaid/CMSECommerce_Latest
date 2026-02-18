using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CMSECommerce.Migrations
{
    /// <inheritdoc />
    public partial class AddNewAdminUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "8e448304-2185-442e-a342-6e210168d87d",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "8e448304-2185-442e-a342-6e210168d87d", "AQAAAAIAAYagAAAAEIAET8Q8H4d/p55OAvqhCvCZDLIN2BHopDEOtgth3953iAM+IO7TuVd9C5b09nzcIw==", "ddcd9342-0bad-2c22-3198-510a21175506" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "a18265d3-05b8-4766-adcc-ca43d3960199", "AQAAAAIAAYagAAAAEC2w02BxXotR3xolOEJvbP2CPRrkq/ot7t4TUaIKKGPqBv3VnCpcUTfMqbXMAEykZA==", "1840e320-7e7e-31c7-f6b2-291807391fc8" });

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "Email", "EmailConfirmed", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UserName" },
                values: new object[,]
                {
                    { "a02-8265d3-05b8-4766-adcc-ca43d3960192", 0, "a02-8265d3-05b8-4766-adcc-ca43d3960192", "bharmalprojects@gmail.com", true, false, null, "BHARMALPROJECTS@GMAIL.COM", "BHARMALPROJECTS@GMAIL.COM", "AQAAAAIAAYagAAAAEDCeCinDZsQt+rbfqSJmy5EN62Xs2fRSjKb5EPv1umAt4u+oTZzz84WTIeUKmtBDmw==", "9963107763", false, "8c6cdeef-d708-9ad2-479a-02c60a8086b3", false, "bharmalprojects@gmail.com" },
                    { "a05-8265d3-05b8-4766-adcc-ca43d3960195", 0, "a05-8265d3-05b8-4766-adcc-ca43d3960195", "abdulqadirlokhandwalaandwala@gmail.com", true, false, null, "ABDULQADIRLOKHANDWALAANDWALA@GMAIL.COM", "ABDULQADIRLOKHANDWALAANDWALA@GMAIL.COM", "AQAAAAIAAYagAAAAENWcMRazm1VGoUPUeXDWoKDdBaIrFrjupGSus/BfMp3Jcb3n8nHQBJWYePoY4kfhPw==", "9121835054", false, "87f1eeee-c426-76d5-d41b-ce297eb8f35d", false, "abdulqadirlokhandwalaandwala@gmail.com" },
                    { "m01-8265d3-05b8-4766-adcc-ca43d3960191", 0, "m01-8265d3-05b8-4766-adcc-ca43d3960191", "murtazahussain166@gmail.com", true, false, null, "MURTAZAHUSSAIN166@GMAIL.COM", "MURTAZAHUSSAIN166@GMAIL.COM", "AQAAAAIAAYagAAAAELKXpiKVOWdE8Nr0pF29hzzz1rdIpQBJF4xsQkCB9KonQpyVuS3AratYaVqqIMxT8g==", "9700081831", false, "57b37af0-60a9-478a-ae26-86cd81ed7c27", false, "murtazahussain166@gmail.com" },
                    { "t03-8265d3-05b8-4766-adcc-ca43d3960193", 0, "t03-8265d3-05b8-4766-adcc-ca43d3960193", "mailbox.taher@gmail.com", true, false, null, "MAILBOX.TAHER@GMAIL.COM", "MAILBOX.TAHER@GMAIL.COM", "AQAAAAIAAYagAAAAEJkeS1wtaIRoLDtwyztwp235YWZ//wmK2VCA1insQDgPW+x1dmIqTKAcq3/GuBTynw==", "8885216302", false, "b651f1d9-b39f-25ec-1788-67530ec34d56", false, "mailbox.taher@gmail.com" },
                    { "t04-8265d3-05b8-4766-adcc-ca43d3960194", 0, "t04-8265d3-05b8-4766-adcc-ca43d3960194", "thussain98490@gmail.com", true, false, null, "THUSSAIN98490@GMAIL.COM", "THUSSAIN98490@GMAIL.COM", "AQAAAAIAAYagAAAAEIVIO+MaTXDL1b+05pM+1idMSBS41/EmQKXpY59cddceTWI7IF6/cpLezwFqElmwzA==", "9849217820", false, "531979be-47fa-a133-ae4b-ca858da22655", false, "thussain98490@gmail.com" },
                    { "y06-8265d3-05b8-4766-adcc-ca43d3960196", 0, "y06-8265d3-05b8-4766-adcc-ca43d3960196", "yaliasger@yahoo.co.in", true, false, null, "YALIASGER@YAHOO.CO.IN", "YALIASGER@YAHOO.CO.IN", "AQAAAAIAAYagAAAAEIbuXRwb9i+Jatq8fXb80FYbGggiwzH11790Fssyp7ETY7l1UYyGzNCB8Ysvi03HGg==", "9130211052", false, "20c7e39f-6e3b-a63f-1357-f322dc482561", false, "yaliasger@yahoo.co.in" }
                });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[,]
                {
                    { "5f90378b-3001-443b-8736-411a91341c2c", "a02-8265d3-05b8-4766-adcc-ca43d3960192" },
                    { "5f90378b-3001-443b-8736-411a91341c2c", "a05-8265d3-05b8-4766-adcc-ca43d3960195" },
                    { "5f90378b-3001-443b-8736-411a91341c2c", "m01-8265d3-05b8-4766-adcc-ca43d3960191" },
                    { "5f90378b-3001-443b-8736-411a91341c2c", "t03-8265d3-05b8-4766-adcc-ca43d3960193" },
                    { "5f90378b-3001-443b-8736-411a91341c2c", "t04-8265d3-05b8-4766-adcc-ca43d3960194" },
                    { "5f90378b-3001-443b-8736-411a91341c2c", "y06-8265d3-05b8-4766-adcc-ca43d3960196" }
                });

            migrationBuilder.InsertData(
                table: "UserProfiles",
                columns: new[] { "Id", "About", "BusinessAddress", "BusinessPhoneNumber", "CurrentProductLimit", "CurrentTierId", "FacebookUrl", "FirstName", "GpayQRCodePath", "HomeAddress", "HomePhoneNumber", "ITSNumber", "InstagramUrl", "IsDeactivated", "IsImageApproved", "IsImagePending", "IsProfileVisible", "LastName", "LinkedInUrl", "PendingProfileImagePath", "PhonePeQRCodePath", "Profession", "ProfileImagePath", "ServicesProvided", "StoreId", "SubscriptionEndDate", "SubscriptionStartDate", "UserId", "WhatsAppNumber" },
                values: new object[,]
                {
                    { 100, "Admin", "Hyderabad", null, 1000, null, null, "Murtaza", null, "Hyderabad", null, "100001", null, false, false, false, true, "Sagarwala", null, null, null, "Admin", null, null, null, null, null, "m01-8265d3-05b8-4766-adcc-ca43d3960191", "9700081831" },
                    { 101, "Admin", "Hyderabad", null, 1000, null, null, "Abbas", null, "Hyderabad", null, "100002", null, false, false, false, true, "Shajapurwala", null, null, null, "Admin", null, null, null, null, null, "a02-8265d3-05b8-4766-adcc-ca43d3960192", "9963107763" },
                    { 102, "Admin", "Hyderabad", null, 1000, null, null, "Taher", null, "Hyderabad", null, "100003", null, false, false, false, true, "Bensabwala", null, null, null, "Admin", null, null, null, null, null, "t03-8265d3-05b8-4766-adcc-ca43d3960193", "8885216302" },
                    { 103, "Admin", "Hyderabad", null, 1000, null, null, "Taher", null, "Hyderabad", null, "100004", null, false, false, false, true, "Hyderabadwala", null, null, null, "Admin", null, null, null, null, null, "t04-8265d3-05b8-4766-adcc-ca43d3960194", "9849217820" },
                    { 104, "Admin", "Hyderabad", null, 1000, null, null, "Abdulqadir", null, "Hyderabad", null, "100005", null, false, false, false, true, "Lokhandwala", null, null, null, "Admin", null, null, null, null, null, "a05-8265d3-05b8-4766-adcc-ca43d3960195", "9121835054" },
                    { 105, "Admin", "Hyderabad", null, 1000, null, null, "Yahya", null, "Hyderabad", null, "100009", null, false, false, false, true, "Aliasger", null, null, null, "Admin", null, null, null, null, null, "y06-8265d3-05b8-4766-adcc-ca43d3960196", "9130211052" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "5f90378b-3001-443b-8736-411a91341c2c", "a02-8265d3-05b8-4766-adcc-ca43d3960192" });

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "5f90378b-3001-443b-8736-411a91341c2c", "a05-8265d3-05b8-4766-adcc-ca43d3960195" });

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "5f90378b-3001-443b-8736-411a91341c2c", "m01-8265d3-05b8-4766-adcc-ca43d3960191" });

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "5f90378b-3001-443b-8736-411a91341c2c", "t03-8265d3-05b8-4766-adcc-ca43d3960193" });

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "5f90378b-3001-443b-8736-411a91341c2c", "t04-8265d3-05b8-4766-adcc-ca43d3960194" });

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "5f90378b-3001-443b-8736-411a91341c2c", "y06-8265d3-05b8-4766-adcc-ca43d3960196" });

            migrationBuilder.DeleteData(
                table: "UserProfiles",
                keyColumn: "Id",
                keyValue: 100);

            migrationBuilder.DeleteData(
                table: "UserProfiles",
                keyColumn: "Id",
                keyValue: 101);

            migrationBuilder.DeleteData(
                table: "UserProfiles",
                keyColumn: "Id",
                keyValue: 102);

            migrationBuilder.DeleteData(
                table: "UserProfiles",
                keyColumn: "Id",
                keyValue: 103);

            migrationBuilder.DeleteData(
                table: "UserProfiles",
                keyColumn: "Id",
                keyValue: 104);

            migrationBuilder.DeleteData(
                table: "UserProfiles",
                keyColumn: "Id",
                keyValue: 105);

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a02-8265d3-05b8-4766-adcc-ca43d3960192");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a05-8265d3-05b8-4766-adcc-ca43d3960195");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "m01-8265d3-05b8-4766-adcc-ca43d3960191");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "t03-8265d3-05b8-4766-adcc-ca43d3960193");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "t04-8265d3-05b8-4766-adcc-ca43d3960194");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "y06-8265d3-05b8-4766-adcc-ca43d3960196");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "8e448304-2185-442e-a342-6e210168d87d",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "bb0f007a-1758-4842-bcef-9d193b6e8754", "AQAAAAIAAYagAAAAELCxcgFKUfjpbPF83bLq6wUvc7FV6iGmIqBhQZPOt5uVpgMX/By20N9HcbU/o5cPqw==", "" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "cfdbf2ca-676a-4e62-a2f0-1f8ebddd862a", "AQAAAAIAAYagAAAAEIHfhB9oZUYxHkdWQp7S96en/RwZVafFrzTKCPF7lF8tEWzx+bdd6MG57hmo91RXFQ==", "" });
        }
    }
}
