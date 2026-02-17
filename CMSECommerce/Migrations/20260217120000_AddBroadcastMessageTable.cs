using Microsoft.EntityFrameworkCore.Migrations;

namespace CMSECommerce.Migrations
{
    public partial class AddBroadcastMessageTable : Migration
    {
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

            migrationBuilder.CreateIndex(
                name: "IX_BroadcastMessages_SentByUserId",
                table: "BroadcastMessages",
                column: "SentByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BroadcastMessages_DateSent",
                table: "BroadcastMessages",
                column: "DateSent");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BroadcastMessages");
        }
    }
}
