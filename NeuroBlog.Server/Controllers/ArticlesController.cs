namespace NeuroBlog.Server.Controllers;

[Route("api/articles")]
public sealed class ArticlesController(AppDbContext db, HtmlSanitizer sanitizer) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ArticleSummaryDto>>> GetAll()
    {
        var rows = await db.Articles
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
            Excerpt = sanitizer.Sanitize(a.Html),
            CommentCount = a.CommentCount,
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt,
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ArticleDto>> GetById(Guid id)
    {
        var article = await db.Articles
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

        var html = sanitizer.Sanitize(request.Html);
        if (string.IsNullOrWhiteSpace(html))
            return Problem(detail: "The article body is empty after sanitization.", statusCode: StatusCodes.Status400BadRequest);

        var article = Article.Create(request.Title, html, user);

        db.Articles.Add(article);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = article.Id }, article.ToDto(0));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ArticleDto>> Update(Guid id, [FromBody] UpdateArticleRequest request)
    {
        if (CurrentUser is not { } user)
            return MissingUser();

        var article = await db.Articles.FirstOrDefaultAsync(a => a.Id == id);
        if (article is null)
            return NotFound();
        if (!article.IsAuthoredBy(user))
            return NotOwner();

        var html = sanitizer.Sanitize(request.Html);
        if (string.IsNullOrWhiteSpace(html))
            return Problem(detail: "The article body is empty after sanitization.", statusCode: StatusCodes.Status400BadRequest);

        article.Update(request.Title, html);
        await db.SaveChangesAsync();

        var commentCount = await db.Comments.CountAsync(c => c.ArticleId == id);
        return Ok(article.ToDto(commentCount));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (CurrentUser is not { } user)
            return MissingUser();

        var article = await db.Articles.FirstOrDefaultAsync(a => a.Id == id);
        if (article is null)
            return NotFound();
        if (!article.IsAuthoredBy(user))
            return NotOwner();

        db.Articles.Remove(article);
        await db.SaveChangesAsync();

        return NoContent();
    }
}
