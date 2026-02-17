using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMSECommerce.Migrations
{
    /// <inheritdoc />
    public partial class AddBroadcastMessageCustomerFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BroadcastMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Subject = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AttachmentFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    AttachmentPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SendToAllSellers = table.Column<bool>(type: "bit", nullable: false),
                    SelectedSellerIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SendToAllCustomers = table.Column<bool>(type: "bit", nullable: false),
                    SelectedCustomerIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SentByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DateSent = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecipientCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BroadcastMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BroadcastMessages_AspNetUsers_SentByUserId",
                        column: x => x.SentByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "8e448304-2185-442e-a342-6e210168d87d",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAENHwcTjuKHn5RsJAaDXZA/cr0TUVS6G8mR47AfejHNKgUz9gadAEnFNNg4w8BhjP2A==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a02-8265d3-05b8-4766-adcc-ca43d3960192",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEA1M6R8txi25J2n5TkfVqSD+B+j/hintR5IiPHjfZjtIwqKfig09IodyNS37cyywhQ==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEFsbrAkCOU8niKVKuQo7+b2uG5NAZGm1oQ2Q4ufsrfD1m9Eos50mFddfVM4o0vrIEQ==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "ab05-8265d3-05b8-4766-adcc-ca43d3960101",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEDGcXrJGTQZ/nCkVGtnpfDnim/JPGxl4lYKvuTarE3vDqIKmVzo45ox7AHREOypF3A==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "ak09-8265d3-05b8-4766-adcc-ca43d3960103",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEOOLZiNN0DKd3FTKcHtpINXoWitahYM9/WsJ9aPzO6pbCQQhq2OC00eYezGstb5UwQ==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "al08-8265d3-05b8-4766-adcc-ca43d3960102",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEDoIZS2D5BAIX4gOgcj8CFJvxKnx5HheYUjj3lw9Mn6OJU3020NDN8++kGequGZxgg==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b72c9184-e4d2-4e5a-9391-7241065162a0",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEFqPnirqkISUIJaVIqxFp1hwrh6h5MRcSjTCx2GFOYn7pCNGitXZ7yd3fCZirJyl/g==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "h07-8265d3-05b8-4766-adcc-ca43d3960197",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEADtoCWcuXbYztq+Q4+JUb+IOVmbFZwOiV7yztkjMO0FnUjzr8NBmQ6xnuGtj0XWuA==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "kh10-8265d3-05b8-4766-adcc-ca43d3960104",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEF3eNi8NGmH5KcBtM/Oj0DlAOpl/LDQ6CvFchm7vJSE9Czab/aRms5axi/EhwAo28Q==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "m01-8265d3-05b8-4766-adcc-ca43d3960191",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEG4XmhSz27C03XZz0oSOU3JhI327bFbos4QM8gjuKVxwryPOisBqm3TGKXW2igcgzg==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "t03-8265d3-05b8-4766-adcc-ca43d3960193",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEBqGm+vuco9ImFyErQ2e94rbYAt5qe4seoReaB6ndJa5HU6AmJP8sUXTBiyXXymFYQ==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "t04-8265d3-05b8-4766-adcc-ca43d3960194",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAENvOCc2EIBua/ixiN9zE03KNtcKEmRPz5CT4NCvRaGfH6HnJWyXec07WFu94JSzHFg==");

            migrationBuilder.CreateIndex(
                name: "IX_BroadcastMessages_SentByUserId",
                table: "BroadcastMessages",
                column: "SentByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BroadcastMessages");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "8e448304-2185-442e-a342-6e210168d87d",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEGEA/k3QSLRcX/AldwjY2PV4ik/ialgQxXib4+sVS56eljG0C8CQro5QaPpvsRAWxQ==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a02-8265d3-05b8-4766-adcc-ca43d3960192",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEPeJ9STYb1fi3xu5Up8nnGH7Cu8hMdsoiYzSOPKtoKirmVrtMLL78rKi3qLrSkwxtQ==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a18265d3-05b8-4766-adcc-ca43d3960199",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEFToCF8ywT9hKm8+YBIc7+ZW4eZ8/Pn3dzfr/6oMtqYXWfSnDIMJpAnwTSl9U9l5VQ==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "ab05-8265d3-05b8-4766-adcc-ca43d3960101",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEL0E8CtGujPE8t/1XkT2p4bvoQimgjy3Wrl4yYcQ90JX9PIg6vcuqfYuGADyymO0sA==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "ak09-8265d3-05b8-4766-adcc-ca43d3960103",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEAgoGqDIsQ8XFfBPQWDZBBw2mdePGFPt4wnq6MWEp9GQhh2P6ohO1JegxhRUjf6TAQ==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "al08-8265d3-05b8-4766-adcc-ca43d3960102",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEEazbEz3oR3NMTWzKZWCMhtAOjWh6GLqsSBTSMKKKh0aPnikliEd+Empk7US0AeF/w==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b72c9184-e4d2-4e5a-9391-7241065162a0",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEIe3VeDTYDYdE/ycIsqxlZZyFxL/DIqKtxPbFCjZCksbfZ/t/DJLywzgnedtwKMW7A==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "h07-8265d3-05b8-4766-adcc-ca43d3960197",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEPD3l7mPNHFbzSUTC/OkkG36BX569iy672wDs+5D3cWmO0TdrNHD4X4CP4sKxPNCGQ==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "kh10-8265d3-05b8-4766-adcc-ca43d3960104",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEHTGpRWlWV1Qkm9gTA8QUcbnyb1l1fwoLrvyoGlo0kL1/2CfyqKGbCp42Q4uFOFr+w==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "m01-8265d3-05b8-4766-adcc-ca43d3960191",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEF+gowONTYaMqSUS6Tg9XduUGVtkssHlDTTjmpaqefOtcCRQm8xmUktEUQZfqGSYJQ==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "t03-8265d3-05b8-4766-adcc-ca43d3960193",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEBiN86J7WlTjmB0gy+/ITLJ/fN3YoL1l75eqkEpDU/g+jMF3CYhXU2W7aaTA3KnL5g==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "t04-8265d3-05b8-4766-adcc-ca43d3960194",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEAD1Ryi1aei9KTbhXkN10zkBig1hNS77wj5WQ2ymlghTCvkBX79m/A1FcWRdKvs/HA==");
        }
    }
}
