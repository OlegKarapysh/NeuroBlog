namespace NeuroBlog.Shared;

public static class ContentLimits
{
    public const int MinArticleContentLength = 1;
    public const int MaxArticleTitleLength = 200;

    public const int MinCommentLength = 1;
    public const int MaxCommentLength = 1000;

    public const int MaxUsernameLength = 50;

    public const int DefaultCommentPageSize = 10;

    // Each comment's materialized Path stores one 8-byte Id per ancestor (including
    // itself). A PostgreSQL B-tree key must fit in ~2704 bytes (one third of an
    // 8 KB page), and the (ArticleId, Path) index also spends 16 bytes on the
    // article's uuid plus per-entry overhead. Capping the depth keeps the deepest
    // path well under that limit, so an insert can never fail on index key size.
    //   budget ≈ (2704 − 16) / 8 ≈ 335 levels; 300 leaves a comfortable margin.
    public const int CommentPathBytesPerLevel = sizeof(long); // 8
    public const int MaxReplyDepth = 300;
}
