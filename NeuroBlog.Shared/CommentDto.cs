namespace NeuroBlog.Shared;

/// <summary>
/// A single comment. Comments form an unlimited-depth tree via
/// <see cref="ParentCommentId"/>; the client assembles the hierarchy.
/// </summary>
public record CommentDto
{
    public long Id { get; init; }
    public Guid ArticleId { get; init; }
    public long? ParentCommentId { get; init; }

    public string Author { get; init; } = "";

    /// <summary>Plain-text content. Empty when <see cref="IsDeleted"/> is true.</summary>
    public string Content { get; init; } = "";

    public bool IsDeleted { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }

    /// <summary>Number of direct (one level) replies, used to show the "Show replies" button.</summary>
    public int ReplyCount { get; init; }

    /// <summary>Nesting level: 0 for a top-level comment, parent depth + 1 for a reply.</summary>
    public long ReplyDepth { get; init; }
}
