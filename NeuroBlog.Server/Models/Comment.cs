namespace NeuroBlog.Server.Models;

/// <summary>
/// A comment in an unlimited-depth thread. Mutating operations are methods so the
/// invariants (e.g. a deleted comment cannot be edited) live with the data.
/// </summary>
public class Comment
{
    // Parameterless constructor for EF Core materialization only.
    private Comment() { }

    public Guid Id { get; private set; }

    public Guid ArticleId { get; private set; }
    public Article Article { get; private set; } = null!;

    /// <summary>Parent comment for replies; null for a top-level comment.</summary>
    public Guid? ParentCommentId { get; private set; }
    public Comment? ParentComment { get; private set; }
    public List<Comment> Replies { get; private set; } = new();

    public string Author { get; private set; } = "";

    /// <summary>Plain text. Cleared to empty string when soft-deleted.</summary>
    public string Content { get; private set; } = "";

    /// <summary>When true the comment is shown as "This comment was deleted".</summary>
    public bool IsDeleted { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public static Comment Create(Guid articleId, string author, string content, Guid? parentCommentId) => new()
    {
        Id = Guid.NewGuid(),
        ArticleId = articleId,
        ParentCommentId = parentCommentId,
        Author = author,
        Content = content.Trim(),
        CreatedAt = DateTimeOffset.UtcNow,
    };

    /// <summary>Edits the text. Not allowed once the comment has been deleted.</summary>
    public void Edit(string content)
    {
        if (IsDeleted)
            throw new InvalidOperationException("A deleted comment cannot be edited.");

        Content = content.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Soft-deletes: keeps the row (and its replies) but clears the text. Idempotent.</summary>
    public void Delete()
    {
        if (IsDeleted)
            return;

        IsDeleted = true;
        Content = "";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool IsAuthoredBy(string username) =>
        string.Equals(Author, username, StringComparison.Ordinal);
}
