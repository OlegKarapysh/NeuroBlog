namespace NeuroBlog.Server.Models;

public class Comment
{
    public Guid Id { get; private set; }
    public Guid ArticleId { get; private set; }
    public Article Article { get; private set; } = null!;
    public Guid? ParentCommentId { get; private set; }
    public Comment? ParentComment { get; private set; }
    public List<Comment> Replies { get; private set; } = [];
    public long ReplyDepth { get; private set; }
    public string Author { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public bool IsDeleted { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    
    private Comment() { } // Parameterless constructor for EF Core materialization only.

    public static Comment Create(Guid articleId, string author, string content, Guid? parentCommentId, long replyDepth) => new()
    {
        Id = Guid.NewGuid(),
        ArticleId = articleId,
        ParentCommentId = parentCommentId,
        ReplyDepth = replyDepth,
        Author = author,
        Content = content.Trim(),
        CreatedAt = DateTimeOffset.UtcNow,
    };

    public void Edit(string content)
    {
        if (IsDeleted)
            throw new InvalidOperationException("A deleted comment cannot be edited.");

        Content = content.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SoftDelete()
    {
        if (IsDeleted)
            return;

        IsDeleted = true;
        Content = string.Empty;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool IsAuthoredBy(string username) => string.Equals(Author, username, StringComparison.Ordinal);
}
