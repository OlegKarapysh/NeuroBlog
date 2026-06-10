namespace NeuroBlog.Server.Models;

public class Article
{
    public Guid Id { get; set; }

    public string Title { get; set; } = "";

    /// <summary>Sanitized HTML body (already passed through the sanitizer).</summary>
    public string Html { get; set; } = "";

    public string Author { get; set; } = "";

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public List<Comment> Comments { get; set; } = new();
}
