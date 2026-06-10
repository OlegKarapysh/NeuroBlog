using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NeuroBlog.Server.Data;
using NeuroBlog.Server.Models;
using NeuroBlog.Server.Services;
using NeuroBlog.Shared;

namespace NeuroBlog.Server.Controllers;

[Route("api/articles")]
public class ArticlesController : ApiControllerBase
{
    private readonly AppDbContext _db;
    private readonly IArticleHtmlSanitizer _sanitizer;

    public ArticlesController(AppDbContext db, IArticleHtmlSanitizer sanitizer)
    {
        _db = db;
        _sanitizer = sanitizer;
    }

    [HttpGet]
    public async Task<ActionResult<List<ArticleSummaryDto>>> GetAll()
    {
        var rows = await _db.Articles
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                a.Id,
                a.Title,
                a.Author,
                a.Html,
                a.CreatedAt,
                a.UpdatedAt,
                CommentCount = a.Comments.Count,
            })
            .ToListAsync();

        var result = rows.Select(a => new ArticleSummaryDto
        {
            Id = a.Id,
            Title = a.Title,
            Author = a.Author,
            Excerpt = _sanitizer.ToExcerpt(a.Html),
            CommentCount = a.CommentCount,
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt,
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ArticleDto>> GetById(Guid id)
    {
        var article = await _db.Articles
            .Where(a => a.Id == id)
            .Select(a => new { Article = a, CommentCount = a.Comments.Count })
            .FirstOrDefaultAsync();

        if (article is null)
            return NotFound();

        return Ok(article.Article.ToDto(article.CommentCount));
    }

    [HttpPost]
    public async Task<ActionResult<ArticleDto>> Create([FromBody] CreateArticleRequest request)
    {
        if (CurrentUser is not { } user)
            return MissingUser();

        var html = _sanitizer.Sanitize(request.Html);
        if (string.IsNullOrWhiteSpace(html))
            return Problem(detail: "The article body is empty after sanitization.", statusCode: StatusCodes.Status400BadRequest);

        var article = new Article
        {
            Id = Guid.NewGuid(),
            Title = NormalizeTitle(request.Title),
            Html = html,
            Author = user,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _db.Articles.Add(article);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = article.Id }, article.ToDto(0));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ArticleDto>> Update(Guid id, [FromBody] UpdateArticleRequest request)
    {
        if (CurrentUser is not { } user)
            return MissingUser();

        var article = await _db.Articles.FirstOrDefaultAsync(a => a.Id == id);
        if (article is null)
            return NotFound();
        if (!SameUser(article.Author, user))
            return NotOwner();

        var html = _sanitizer.Sanitize(request.Html);
        if (string.IsNullOrWhiteSpace(html))
            return Problem(detail: "The article body is empty after sanitization.", statusCode: StatusCodes.Status400BadRequest);

        article.Title = NormalizeTitle(request.Title);
        article.Html = html;
        article.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();

        var commentCount = await _db.Comments.CountAsync(c => c.ArticleId == id);
        return Ok(article.ToDto(commentCount));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (CurrentUser is not { } user)
            return MissingUser();

        var article = await _db.Articles.FirstOrDefaultAsync(a => a.Id == id);
        if (article is null)
            return NotFound();
        if (!SameUser(article.Author, user))
            return NotOwner();

        _db.Articles.Remove(article);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static string NormalizeTitle(string? title)
    {
        title = title?.Trim();
        return string.IsNullOrEmpty(title) ? "Untitled" : title;
    }
}
