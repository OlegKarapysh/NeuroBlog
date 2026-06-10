using System.ComponentModel.DataAnnotations;

namespace NeuroBlog.Shared;

public class CreateArticleRequest
{
    /// <summary>Optional title; falls back to "Untitled" when blank.</summary>
    [StringLength(ContentLimits.MaxArticleTitleLength)]
    public string? Title { get; set; }

    /// <summary>Raw HTML pasted by the user. Sanitized server-side before storage.</summary>
    [Required]
    [MinLength(ContentLimits.MinArticleContentLength)]
    public string Html { get; set; } = "";
}
