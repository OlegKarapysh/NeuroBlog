namespace NeuroBlog.Components;

/// <summary>The values produced by <c>ArticleEditor</c> when the user submits.</summary>
public record ArticleDraft(string? Title, string Html);
