namespace NeuroBlog.Shared;

/// <summary>Lightweight article projection used for the article list page.</summary>
public record ArticleSummaryDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = "";
    public string Author { get; init; } = "";

    /// <summary>Plain-text excerpt derived from the (sanitized) HTML body.</summary>
    public string Excerpt { get; init; } = "";

    public int CommentCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}
