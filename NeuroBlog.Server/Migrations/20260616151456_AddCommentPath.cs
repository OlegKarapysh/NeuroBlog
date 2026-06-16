using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeuroBlog.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddCommentPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Path",
                table: "Comments",
                type: "text",
                nullable: false,
                defaultValue: "",
                collation: "C");

            // Backfill Path for existing rows: walk from each root down, appending
            // a fixed-width segment per level. The segment must byte-for-byte match
            // Comment.BuildPathSegment: to_char(...'YYYYMMDDHH24MISSUS') (20 UTC
            // microsecond digits) followed by the Id with hyphens stripped (32 hex
            // chars), so rows written after this migration sort consistently with
            // backfilled ones.
            migrationBuilder.Sql(""""
                WITH RECURSIVE path_cte AS (
                    SELECT "Id",
                           to_char("CreatedAt" AT TIME ZONE 'UTC', 'YYYYMMDDHH24MISSUS')
                               || replace("Id"::text, '-', '') AS path
                    FROM "Comments"
                    WHERE "ParentCommentId" IS NULL
                    UNION ALL
                    SELECT c."Id",
                           p.path
                               || to_char(c."CreatedAt" AT TIME ZONE 'UTC', 'YYYYMMDDHH24MISSUS')
                               || replace(c."Id"::text, '-', '')
                    FROM "Comments" c
                    JOIN path_cte p ON c."ParentCommentId" = p."Id"
                )
                UPDATE "Comments" AS t
                SET "Path" = p.path
                FROM path_cte p
                WHERE t."Id" = p."Id";
                """");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ArticleId_Path",
                table: "Comments",
                columns: new[] { "ArticleId", "Path" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Comments_ArticleId_Path",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "Path",
                table: "Comments");
        }
    }
}
