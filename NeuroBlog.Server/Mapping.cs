namespace NeuroBlog.Server;

public static class Mapping
{
    public static ArticleDto ToDto(this Article a, int commentCount) => new()
    {
        Id = a.Id,
        Title = a.Title,
        Author = a.Author,
        Html = a.Html,
        CommentCount = commentCount,
        CreatedAt = a.CreatedAt,
        UpdatedAt = a.UpdatedAt,
    };

    public static CommentDto ToDto(this Comment c) => new()
    {
        Id = c.Id,
        ArticleId = c.ArticleId,
        ParentCommentId = c.ParentCommentId,
        Author = c.Author,
        Content = c.IsDeleted ? "" : c.Content,
        IsDeleted = c.IsDeleted,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt,
        ReplyDepth = c.ReplyDepth,
    };
}
