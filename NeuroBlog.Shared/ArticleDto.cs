namespace NeuroBlog.Shared;

/// <summary>Full article detail, including the sanitized HTML body.</summary>
public record ArticleDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = "";
    public string Author { get; init; } = "";

    /// <summary>Server-sanitized HTML, safe to render with MarkupString.</summary>
    public string Html { get; init; } = "";

    public int CommentCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}
