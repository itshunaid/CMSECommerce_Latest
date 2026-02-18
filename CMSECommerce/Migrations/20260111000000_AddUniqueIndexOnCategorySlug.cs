using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMSECommerce.Migrations
{
    public partial class AddUniqueIndexOnCategorySlug : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Safely deduplicate any existing duplicate slugs by appending a suffix "-n"
            // using a window function to assign numbers to duplicates, keeping the first occurrence unchanged.
            migrationBuilder.Sql(@"
                ;WITH cte AS (
                    SELECT Id, Slug,
                           ROW_NUMBER() OVER (PARTITION BY Slug ORDER BY Id) AS rn
                    FROM Categories
                    WHERE Slug IS NOT NULL
                )
                UPDATE c
                SET Slug = CONCAT(cte.Slug, '-', CONVERT(varchar(10), cte.rn - 1))
                FROM Categories c
                INNER JOIN cte ON c.Id = cte.Id
                WHERE cte.rn > 1;
            ");

            // 2) Create unique index on Slug
            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug",
                table: "Categories",
                column: "Slug",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Categories_Slug",
                table: "Categories");
        }
    }
}
