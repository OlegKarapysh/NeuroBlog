using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeuroBlog.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddReplyDepth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ReplyDepth",
                table: "Comments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            // Backfill depth for existing rows: top-level = 0, each reply = parent + 1.
            migrationBuilder.Sql(""""
                WITH RECURSIVE depth_cte AS (
                    SELECT "Id", 0::bigint AS depth
                    FROM "Comments"
                    WHERE "ParentCommentId" IS NULL
                    UNION ALL
                    SELECT c."Id", d.depth + 1
                    FROM "Comments" c
                    JOIN depth_cte d ON c."ParentCommentId" = d."Id"
                )
                UPDATE "Comments" AS t
                SET "ReplyDepth" = d.depth
                FROM depth_cte d
                WHERE t."Id" = d."Id";
                """");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReplyDepth",
                table: "Comments");
        }
    }
}
