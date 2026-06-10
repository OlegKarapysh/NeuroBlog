namespace NeuroBlog.Server.Models;

public class Comment
{
    public Guid Id { get; set; }

    public Guid ArticleId { get; set; }
    public Article Article { get; set; } = null!;

    /// <summary>Parent comment for replies; null for a top-level comment.</summary>
    public Guid? ParentCommentId { get; set; }
    public Comment? ParentComment { get; set; }
    public List<Comment> Replies { get; set; } = new();

    public string Author { get; set; } = "";

    /// <summary>Plain text. Cleared to empty string when soft-deleted.</summary>
    public string Content { get; set; } = "";

    /// <summary>When true the comment is shown as "This comment was deleted".</summary>
    public bool IsDeleted { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
