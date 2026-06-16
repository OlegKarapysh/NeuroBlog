namespace NeuroBlog.Server.Models;

public class Comment
{
    public long Id { get; private set; }
    public Guid ArticleId { get; private set; }
    public Article Article { get; private set; } = null!;
    public long? ParentCommentId { get; private set; }
    public Comment? ParentComment { get; private set; }
    public List<Comment> Replies { get; private set; } = [];
    public long ReplyDepth { get; private set; }

    // Materialized path: the parent's path plus this comment's own 8-byte
    // big-endian Id. Ordering by Path (raw byte order — bytea has no collation)
    // yields depth-first pre-order, so the first 100 comments are a single indexed
    // scan with no recursion. The Id is a monotonic per-article counter, so it both
    // orders siblings oldest-first and uniquely identifies them; appending it keeps
    // a parent's path a prefix of its descendants'.
    public byte[] Path { get; private set; } = [];
    public string Author { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public bool IsDeleted { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    
    private Comment() { } // Parameterless constructor for EF Core materialization only.

    public static Comment Create(
        long id, Guid articleId, string author, string content, long? parentCommentId, long replyDepth, byte[]? parentPath = null) => new()
    {
        Id = id,
        ArticleId = articleId,
        ParentCommentId = parentCommentId,
        ReplyDepth = replyDepth,
        Author = author,
        Content = content.Trim(),
        CreatedAt = DateTimeOffset.UtcNow,
        Path = BuildPath(parentPath, id),
    };

    // The parent's path (its ancestors + itself) followed by this comment's own
    // Id as 8 big-endian bytes. Big-endian so raw byte comparison of the bytea
    // matches numeric Id order; Ids are positive so the sign bit never flips it.
    private static byte[] BuildPath(byte[]? parentPath, long id)
    {
        var prefixLength = parentPath?.Length ?? 0;
        var path = new byte[prefixLength + sizeof(long)];
        if (prefixLength > 0)
            Buffer.BlockCopy(parentPath!, 0, path, 0, prefixLength);
        BinaryPrimitives.WriteInt64BigEndian(path.AsSpan(prefixLength), id);
        return path;
    }

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
