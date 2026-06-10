namespace NeuroBlog.Shared;

/// <summary>
/// Validation limits shared between the API and the Blazor client so that both
/// sides enforce exactly the same rules.
/// </summary>
public static class ContentLimits
{
    public const int MinArticleContentLength = 1;
    public const int MaxArticleTitleLength = 200;

    public const int MinCommentLength = 1;
    public const int MaxCommentLength = 1000;

    public const int MaxUsernameLength = 50;
}
