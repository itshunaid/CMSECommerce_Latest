using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMSECommerce.Migrations
{
    /// <inheritdoc />
    public partial class RemovedYahya : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "5f90378b-3001-443b-8736-411a91341c2c", "y06-8265d3-05b8-4766-adcc-ca43d3960196" });

            migrationBuilder.DeleteData(
                table: "UserProfiles",
                keyColumn: "Id",
                keyValue: 105);

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "y06-8265d3-05b8-4766-adcc-ca43d3960196");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "8e448304-2185-442e-a342-6e210168d87d",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEIAET8Q8H4d/p55OAvqhCvCZDLIN2BHopDEOtgth3953iAM+IO7TuVd9C5b09nzcIw==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a02-8265d3-05b8-4766-adcc-ca43d3960192",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEDCeCinDZsQt+rbfqSJmy5EN62Xs2fRSjKb5EPv1umAt4u+oTZzz84WTIeUKmtBDmw==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a05-8265d3-05b8-4766-adcc-ca43d3960195",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAENWcMRazm1VGoUPUeXDWoKDdBaIrFrjupGSus/BfMp3Jcb3n8nHQBJWYePoY4kfhPw==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEC2w02BxXotR3xolOEJvbP2CPRrkq/ot7t4TUaIKKGPqBv3VnCpcUTfMqbXMAEykZA==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "m01-8265d3-05b8-4766-adcc-ca43d3960191",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAELKXpiKVOWdE8Nr0pF29hzzz1rdIpQBJF4xsQkCB9KonQpyVuS3AratYaVqqIMxT8g==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "t03-8265d3-05b8-4766-adcc-ca43d3960193",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEJkeS1wtaIRoLDtwyztwp235YWZ//wmK2VCA1insQDgPW+x1dmIqTKAcq3/GuBTynw==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "t04-8265d3-05b8-4766-adcc-ca43d3960194",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEIVIO+MaTXDL1b+05pM+1idMSBS41/EmQKXpY59cddceTWI7IF6/cpLezwFqElmwzA==");

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "Email", "EmailConfirmed", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UserName" },
                values: new object[] { "y06-8265d3-05b8-4766-adcc-ca43d3960196", 0, "y06-8265d3-05b8-4766-adcc-ca43d3960196", "yaliasger@yahoo.co.in", true, false, null, "YALIASGER@YAHOO.CO.IN", "YALIASGER@YAHOO.CO.IN", "AQAAAAIAAYagAAAAEIbuXRwb9i+Jatq8fXb80FYbGggiwzH11790Fssyp7ETY7l1UYyGzNCB8Ysvi03HGg==", "9130211052", false, "20c7e39f-6e3b-a63f-1357-f322dc482561", false, "yaliasger@yahoo.co.in" });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[] { "5f90378b-3001-443b-8736-411a91341c2c", "y06-8265d3-05b8-4766-adcc-ca43d3960196" });

            migrationBuilder.InsertData(
                table: "UserProfiles",
                columns: new[] { "Id", "About", "BusinessAddress", "BusinessPhoneNumber", "CurrentProductLimit", "CurrentTierId", "FacebookUrl", "FirstName", "GpayQRCodePath", "HomeAddress", "HomePhoneNumber", "ITSNumber", "InstagramUrl", "IsDeactivated", "IsImageApproved", "IsImagePending", "IsProfileVisible", "LastName", "LinkedInUrl", "PendingProfileImagePath", "PhonePeQRCodePath", "Profession", "ProfileImagePath", "ServicesProvided", "StoreId", "SubscriptionEndDate", "SubscriptionStartDate", "UserId", "WhatsAppNumber" },
                values: new object[] { 105, "Admin", "Hyderabad", null, 1000, null, null, "Yahya", null, "Hyderabad", null, "100009", null, false, false, false, true, "Aliasger", null, null, null, "Admin", null, null, null, null, null, "y06-8265d3-05b8-4766-adcc-ca43d3960196", "9130211052" });
        }
    }
}
