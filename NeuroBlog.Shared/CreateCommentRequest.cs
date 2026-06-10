namespace NeuroBlog.Shared;

public class CreateCommentRequest
{
    [Required]
    [StringLength(ContentLimits.MaxCommentLength, MinimumLength = ContentLimits.MinCommentLength)]
    public string Content { get; set; } = "";

    /// <summary>Set when replying to an existing comment; null for a top-level comment.</summary>
    public Guid? ParentCommentId { get; set; }
}
