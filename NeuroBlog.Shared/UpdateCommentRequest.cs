namespace NeuroBlog.Shared;

public class UpdateCommentRequest
{
    [Required]
    [StringLength(ContentLimits.MaxCommentLength, MinimumLength = ContentLimits.MinCommentLength)]
    public string Content { get; set; } = "";
}
