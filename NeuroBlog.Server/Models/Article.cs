namespace NeuroBlog.Server.Models;

/// <summary>
/// An article. All state changes go through methods and the setters are private,
/// so the entity owns its own invariants instead of being an anemic data bag.
/// </summary>
public class Article
{
    // Parameterless constructor for EF Core materialization only.
    private Article() { }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = "";

    /// <summary>Sanitized HTML body (already passed through the sanitizer by the caller).</summary>
    public string Html { get; private set; } = "";
    public string Author { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public List<Comment> Comments { get; private set; } = new();

    /// <summary>Creates a new article. <paramref name="html"/> must already be sanitized.</summary>
    public static Article Create(string? title, string html, string author) => new()
    {
        Id = Guid.NewGuid(),
        Title = NormalizeTitle(title),
        Html = html,
        Author = author,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    /// <summary>Updates the editable fields and stamps <see cref="UpdatedAt"/>.</summary>
    public void Update(string? title, string html)
    {
        Title = NormalizeTitle(title);
        Html = html;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool IsAuthoredBy(string username) =>
        string.Equals(Author, username, StringComparison.Ordinal);

    private static string NormalizeTitle(string? title)
    {
        title = title?.Trim();
        return string.IsNullOrEmpty(title) ? "Untitled" : title;
    }
}
