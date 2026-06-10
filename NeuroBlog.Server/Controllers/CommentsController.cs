namespace NeuroBlog.Server.Controllers;

public sealed class CommentsController(AppDbContext db) : ApiControllerBase
{
    /// <summary>All comments for an article (flat); the client builds the tree.</summary>
    [HttpGet("api/articles/{articleId:guid}/comments")]
    public async Task<ActionResult<List<CommentDto>>> GetForArticle(Guid articleId)
    {
        var articleExists = await db.Articles.AnyAsync(a => a.Id == articleId);
        if (!articleExists)
            return NotFound();

        var comments = await db.Comments
            .Where(c => c.ArticleId == articleId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        return Ok(comments.Select(c => c.ToDto()).ToList());
    }

    [HttpPost("api/articles/{articleId:guid}/comments")]
    public async Task<ActionResult<CommentDto>> Create(Guid articleId, [FromBody] CreateCommentRequest request)
    {
        if (CurrentUser is not { } user)
            return MissingUser();

        var articleExists = await db.Articles.AnyAsync(a => a.Id == articleId);
        if (!articleExists)
            return NotFound();

        if (request.ParentCommentId is { } parentId)
        {
            // The parent must exist and belong to the same article.
            var parentOk = await db.Comments.AnyAsync(c => c.Id == parentId && c.ArticleId == articleId);
            if (!parentOk)
                return Problem(detail: "Parent comment not found on this article.", statusCode: StatusCodes.Status400BadRequest);
        }

        var comment = Comment.Create(articleId, user, request.Content, request.ParentCommentId);

        db.Comments.Add(comment);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetForArticle), new { articleId }, comment.ToDto());
    }

    [HttpPut("api/comments/{id:guid}")]
    public async Task<ActionResult<CommentDto>> Update(Guid id, [FromBody] UpdateCommentRequest request)
    {
        if (CurrentUser is not { } user)
            return MissingUser();

        var comment = await db.Comments.FirstOrDefaultAsync(c => c.Id == id);
        if (comment is null)
            return NotFound();
        if (!comment.IsAuthoredBy(user))
            return NotOwner();
        if (comment.IsDeleted)
            return Problem(detail: "A deleted comment cannot be edited.", statusCode: StatusCodes.Status400BadRequest);

        comment.Edit(request.Content);
        await db.SaveChangesAsync();

        return Ok(comment.ToDto());
    }

    /// <summary>Soft-deletes a comment so its replies remain visible.</summary>
    [HttpDelete("api/comments/{id:guid}")]
    public async Task<ActionResult<CommentDto>> Delete(Guid id)
    {
        if (CurrentUser is not { } user)
            return MissingUser();

        var comment = await db.Comments.FirstOrDefaultAsync(c => c.Id == id);
        if (comment is null)
            return NotFound();
        if (!comment.IsAuthoredBy(user))
            return NotOwner();

        comment.Delete();
        await db.SaveChangesAsync();

        return Ok(comment.ToDto());
    }
}
