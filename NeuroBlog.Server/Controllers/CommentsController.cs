using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NeuroBlog.Server.Data;
using NeuroBlog.Server.Models;
using NeuroBlog.Shared;

namespace NeuroBlog.Server.Controllers;

public class CommentsController : ApiControllerBase
{
    private readonly AppDbContext _db;

    public CommentsController(AppDbContext db) => _db = db;

    /// <summary>All comments for an article (flat); the client builds the tree.</summary>
    [HttpGet("api/articles/{articleId:guid}/comments")]
    public async Task<ActionResult<List<CommentDto>>> GetForArticle(Guid articleId)
    {
        var articleExists = await _db.Articles.AnyAsync(a => a.Id == articleId);
        if (!articleExists)
            return NotFound();

        var comments = await _db.Comments
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

        var articleExists = await _db.Articles.AnyAsync(a => a.Id == articleId);
        if (!articleExists)
            return NotFound();

        if (request.ParentCommentId is { } parentId)
        {
            // The parent must exist and belong to the same article.
            var parentOk = await _db.Comments.AnyAsync(c => c.Id == parentId && c.ArticleId == articleId);
            if (!parentOk)
                return Problem(detail: "Parent comment not found on this article.", statusCode: StatusCodes.Status400BadRequest);
        }

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            ArticleId = articleId,
            ParentCommentId = request.ParentCommentId,
            Author = user,
            Content = request.Content.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetForArticle), new { articleId }, comment.ToDto());
    }

    [HttpPut("api/comments/{id:guid}")]
    public async Task<ActionResult<CommentDto>> Update(Guid id, [FromBody] UpdateCommentRequest request)
    {
        if (CurrentUser is not { } user)
            return MissingUser();

        var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == id);
        if (comment is null)
            return NotFound();
        if (comment.IsDeleted)
            return Problem(detail: "A deleted comment cannot be edited.", statusCode: StatusCodes.Status400BadRequest);
        if (!SameUser(comment.Author, user))
            return NotOwner();

        comment.Content = request.Content.Trim();
        comment.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(comment.ToDto());
    }

    /// <summary>Soft-deletes a comment so its replies remain visible.</summary>
    [HttpDelete("api/comments/{id:guid}")]
    public async Task<ActionResult<CommentDto>> Delete(Guid id)
    {
        if (CurrentUser is not { } user)
            return MissingUser();

        var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == id);
        if (comment is null)
            return NotFound();
        if (!SameUser(comment.Author, user))
            return NotOwner();

        if (!comment.IsDeleted)
        {
            comment.IsDeleted = true;
            comment.Content = "";
            comment.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();
        }

        return Ok(comment.ToDto());
    }
}
