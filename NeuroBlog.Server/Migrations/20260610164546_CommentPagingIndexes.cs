using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeuroBlog.Server.Migrations
{
    /// <inheritdoc />
    public partial class CommentPagingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Comments_ArticleId",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_Comments_ParentCommentId",
                table: "Comments");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ArticleId_ParentCommentId_CreatedAt_Id",
                table: "Comments",
                columns: new[] { "ArticleId", "ParentCommentId", "CreatedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ParentCommentId_CreatedAt_Id",
                table: "Comments",
                columns: new[] { "ParentCommentId", "CreatedAt", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Comments_ArticleId_ParentCommentId_CreatedAt_Id",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_Comments_ParentCommentId_CreatedAt_Id",
                table: "Comments");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ArticleId",
                table: "Comments",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ParentCommentId",
                table: "Comments",
                column: "ParentCommentId");
        }
    }
}
