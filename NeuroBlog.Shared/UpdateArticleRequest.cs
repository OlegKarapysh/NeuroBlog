namespace NeuroBlog.Shared;

public class UpdateArticleRequest
{
    [StringLength(ContentLimits.MaxArticleTitleLength)]
    public string? Title { get; set; }

    [Required]
    [MinLength(ContentLimits.MinArticleContentLength)]
    public string Html { get; set; } = "";
}
