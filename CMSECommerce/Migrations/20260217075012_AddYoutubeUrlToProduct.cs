using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMSECommerce.Migrations
{
    /// <inheritdoc />
    public partial class AddYoutubeUrlToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "YoutubeUrl",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

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

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                column: "YoutubeUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                column: "YoutubeUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3,
                column: "YoutubeUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4,
                column: "YoutubeUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5,
                column: "YoutubeUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 6,
                column: "YoutubeUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 7,
                column: "YoutubeUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 8,
                column: "YoutubeUrl",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "YoutubeUrl",
                table: "Products");

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
                keyValue: "ab05-8265d3-05b8-4766-adcc-ca43d3960101",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEPxIXt4kddK2gXpFvEhEmq6K/puWnvzew0pYJqlISDGX+kroTw5Hye4sogMh3INd/A==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "ak09-8265d3-05b8-4766-adcc-ca43d3960103",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEIxE7niJWrgmrwE9D1iqDv3z8Dc4xDPtJz+BcALSMPqHitfdF2U5c2zZ0qS5dr+NaA==");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "al08-8265d3-05b8-4766-adcc-ca43d3960102",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEHrFyl25AgN+uQdViRnKGMqLxxo7IYcGkjMnYQUP07zwQqmloZbMHWxpYyoQ/mW/eg==");

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
                keyValue: "kh10-8265d3-05b8-4766-adcc-ca43d3960104",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEL+FS7wy1wqTWVqwUtf3o1HW8Rmr4pgZ9n+pelf44+qJ3U2DRDc/sQsVto1aMtLBfg==");

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
        }
    }
}
