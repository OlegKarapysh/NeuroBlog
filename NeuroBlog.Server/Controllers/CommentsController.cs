namespace NeuroBlog.Server.Controllers;

public sealed class CommentsController(AppDbContext db) : ApiControllerBase
{
    [HttpGet("api/articles/{articleId:guid}/comments")]
    public async Task<ActionResult<PagedResult<CommentDto>>> GetCommentsPage(
        Guid articleId, int page = 1, int pageSize = ContentLimits.DefaultCommentPageSize)
    {
        if (!await db.Articles.AnyAsync(a => a.Id == articleId))
            return NotFound();
        
        var pageResult = await GetPage(
            query: db.Comments.Where(c => c.ArticleId == articleId && c.ParentCommentId == null), page, pageSize);
        
        return Ok(pageResult);
    }

    /// <summary>
    /// The first <c>maxCount</c> (100) comments of an article ordered by depth
    /// (breadth-first): the whole top level first, then every direct reply level
    /// by level, keeping replies grouped under their parent. Loads each level only
    /// as far as needed, so a parent with millions of replies never over-reads.
    /// </summary>
    [HttpGet("api/articles/{articleId:guid}/comments/first-page")]
    public async Task<ActionResult<PagedResult<CommentDto>>> GetFirstPageBfs(Guid articleId)
    {
        if (!await db.Articles.AnyAsync(a => a.Id == articleId))
            return NotFound();

        const int maxCount = 100;
        var collected = new List<CommentDto>(maxCount);

        var currentLevel = await FetchOldestFirstAsync(
            db.Comments.Where(c => c.ArticleId == articleId && c.ParentCommentId == null), maxCount);

        while (currentLevel.Count > 0 && collected.Count < maxCount)
        {
            foreach (var comment in currentLevel)
            {
                collected.Add(comment);
                if (collected.Count == maxCount)
                    break;
            }

            if (collected.Count == maxCount)
                break;

            var remainingCount = maxCount - collected.Count;
            var nextLevel = new List<CommentDto>(remainingCount);

            foreach (var parent in currentLevel)
            {
                if (nextLevel.Count >= remainingCount)
                    break;

                var replies = await FetchOldestFirstAsync(
                    db.Comments.Where(c => c.ParentCommentId == parent.Id), remainingCount - nextLevel.Count);

                nextLevel.AddRange(replies);
            }

            currentLevel = nextLevel;
        }

        return Ok(FirstPage(collected, maxCount));
    }

    /// <summary>
    /// The first 100 comments of an article in depth-first pre-order: each comment
    /// is immediately followed by its descendants (threaded reading order). Built
    /// with a raw recursive CTE that carries each node's ancestor CreatedAt path
    /// and orders by it (a parent's path is a prefix of its children's).
    /// NOTE: a recursive CTE with ORDER BY/LIMIT expands the whole article tree
    /// before limiting, so unlike the BFS endpoint this is not bounded to ~100 rows.
    /// At true scale a materialized path / ltree column would make it a single
    /// indexed "ORDER BY path LIMIT 100" with no recursion.
    /// </summary>
    [HttpGet("api/articles/{articleId:guid}/comments/first-page-dfs")]
    public async Task<ActionResult<PagedResult<CommentDto>>> GetFirstPageDfs(Guid articleId)
    {
        if (!await db.Articles.AnyAsync(a => a.Id == articleId))
            return NotFound();

        const int maxCount = 100;

        const string sql =
            """
            WITH RECURSIVE thread AS (
                SELECT c."Id", c."ArticleId", c."ParentCommentId", c."Author", c."Content",
                       c."IsDeleted", c."CreatedAt", c."UpdatedAt", c."ReplyDepth",
                       ARRAY[c."CreatedAt"] AS sort_path
                FROM "Comments" c
                WHERE c."ArticleId" = {0} AND c."ParentCommentId" IS NULL

                UNION ALL

                SELECT c."Id", c."ArticleId", c."ParentCommentId", c."Author", c."Content",
                       c."IsDeleted", c."CreatedAt", c."UpdatedAt", c."ReplyDepth",
                       t.sort_path || c."CreatedAt"
                FROM "Comments" c
                INNER JOIN thread t ON c."ParentCommentId" = t."Id"
            )
            SELECT "Id", "ArticleId", "ParentCommentId", "Author", "Content",
                   "IsDeleted", "CreatedAt", "UpdatedAt", "ReplyDepth"
            FROM thread
            ORDER BY sort_path
            LIMIT {1}
            """;

        var comments = await db.Comments
            .FromSqlRaw(sql, articleId, maxCount)
            .AsNoTracking()
            .ToListAsync();

        return Ok(FirstPage(comments.Select(c => c.ToDto()).ToList(), maxCount));
    }

    [HttpGet("api/comments/{commentId:guid}/replies")]
    public async Task<ActionResult<PagedResult<CommentDto>>> GetRepliesPage(
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

        long replyDepth = 0;
        if (request.ParentCommentId is { } parentId)
        {
            // Validate the parent and read its depth so the child can store parent depth + 1.
            var parent = await db.Comments
                .Where(c => c.Id == parentId && c.ArticleId == articleId)
                .Select(c => new { c.ReplyDepth })
                .FirstOrDefaultAsync();

            if (parent is null)
                return Problem(detail: "Parent comment not found on this article.", statusCode: StatusCodes.Status400BadRequest);

            replyDepth = parent.ReplyDepth + 1;
        }

        var comment = Comment.Create(articleId, user, request.Content, request.ParentCommentId, replyDepth);

        db.Comments.Add(comment);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCommentsPage), new { articleId }, comment.ToDto());
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
        var rows = await Project(query
                .OrderByDescending(c => c.CreatedAt)
                .ThenByDescending(c => c.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize + 1))
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

    // Oldest-first, projected, capped fetch of a comment query — the unit of work
    // for each BFS level (the top level and every parent's direct replies).
    private static Task<List<CommentDto>> FetchOldestFirstAsync(IQueryable<Comment> query, int take) =>
        Project(query.OrderBy(c => c.CreatedAt).ThenBy(c => c.Id).Take(take)).ToListAsync();

    // Response shape shared by the BFS and DFS first-page endpoints.
    private static PagedResult<CommentDto> FirstPage(List<CommentDto> items, int maxCount) => new()
    {
        Items = items,
        Page = 1,
        PageSize = maxCount,
        HasMore = items.Count == maxCount,
    };

    private static IQueryable<CommentDto> Project(IQueryable<Comment> source) =>
        source.Select(c => new CommentDto
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
            ReplyDepth = c.ReplyDepth,
        });
}
