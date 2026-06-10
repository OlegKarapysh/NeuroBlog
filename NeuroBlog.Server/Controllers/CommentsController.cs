namespace NeuroBlog.Server.Controllers;

public sealed class CommentsController(AppDbContext db) : ApiControllerBase
{
    [HttpGet("api/articles/{articleId:guid}/comments")]
    public async Task<ActionResult<PagedResult<CommentDto>>> GetTopLevel(
        Guid articleId, int page = 1, int pageSize = ContentLimits.DefaultCommentPageSize)
    {
        if (!await db.Articles.AnyAsync(a => a.Id == articleId))
            return NotFound();
        
        var pageResult = await GetPage(
            query: db.Comments.Where(c => c.ArticleId == articleId && c.ParentCommentId == null), page, pageSize);
        
        return Ok(pageResult);
    }

    [HttpGet("api/comments/{commentId:guid}/replies")]
    public async Task<ActionResult<PagedResult<CommentDto>>> GetReplies(
        Guid commentId, int page = 1, int pageSize = ContentLimits.DefaultCommentPageSize)
    {
        if (!await db.Comments.AnyAsync(c => c.Id == commentId))
            return NotFound();

        var pageResult = await GetPage(
            query: db.Comments.Where(c => c.ParentCommentId == commentId), page, pageSize);
        
        return Ok(pageResult);
    }

    [HttpPost("api/articles/{articleId:guid}/comments")]
    public async Task<ActionResult<CommentDto>> Create(Guid articleId, [FromBody] CreateCommentRequest request)
    {
        if (CurrentUser is not { } user)
            return MissingUser();

        if (!await db.Articles.AnyAsync(a => a.Id == articleId))
            return NotFound();

        if (request.ParentCommentId is { } parentId)
        {
            var parentBelongsToSameArticle =
                await db.Comments.AnyAsync(c => c.Id == parentId && c.ArticleId == articleId);
            
            if (!parentBelongsToSameArticle)
                return Problem(detail: "Parent comment not found on this article.", statusCode: StatusCodes.Status400BadRequest);
        }

        var comment = Comment.Create(articleId, user, request.Content, request.ParentCommentId);

        db.Comments.Add(comment);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTopLevel), new { articleId }, comment.ToDto());
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

        var hasChildComments = await db.Comments.AnyAsync(c => c.ParentCommentId == id);
        if (hasChildComments)
        {
            comment.SoftDelete();
        }
        else
        {
            db.Remove(comment);
        }
        
        await db.SaveChangesAsync();

        return Ok(comment.ToDto());
    }

    private static async Task<PagedResult<CommentDto>> GetPage(IQueryable<Comment> query, int page, int pageSize)
    {
        var rows = await query
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize + 1)
            .Select(c => new CommentDto
            {
                Id = c.Id,
                ArticleId = c.ArticleId,
                ParentCommentId = c.ParentCommentId,
                Author = c.Author,
                Content = c.IsDeleted ? "" : c.Content,
                IsDeleted = c.IsDeleted,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                ReplyCount = c.Replies.Count,
            })
            .ToListAsync();

        var hasMore = rows.Count > pageSize;
        if (hasMore)
            rows.RemoveAt(rows.Count - 1);

        return new PagedResult<CommentDto>
        {
            Items = rows,
            Page = page,
            PageSize = pageSize,
            HasMore = hasMore
        };
    }
}
