using System.Net;
using System.Text.RegularExpressions;
using Ganss.Xss;

namespace NeuroBlog.Server.Services;

public interface IArticleHtmlSanitizer
{
    /// <summary>Strips scripts, event handlers and dangerous markup from pasted HTML.</summary>
    string Sanitize(string html);

    /// <summary>Builds a short plain-text excerpt from (already sanitized) HTML.</summary>
    string ToExcerpt(string html, int maxLength = 200);
}

public sealed partial class ArticleHtmlSanitizer : IArticleHtmlSanitizer
{
    // HtmlSanitizer is thread-safe as long as its configuration is not mutated
    // after construction, so a single shared instance is fine.
    private readonly HtmlSanitizer _sanitizer = new();

    public string Sanitize(string html) => _sanitizer.Sanitize(html ?? "");

    public string ToExcerpt(string html, int maxLength = 200)
    {
        if (string.IsNullOrWhiteSpace(html))
            return "";

        var text = TagRegex().Replace(html, " ");
        text = WebUtility.HtmlDecode(text);
        text = WhitespaceRegex().Replace(text, " ").Trim();

        return text.Length <= maxLength ? text : text[..maxLength].TrimEnd() + "…";
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
