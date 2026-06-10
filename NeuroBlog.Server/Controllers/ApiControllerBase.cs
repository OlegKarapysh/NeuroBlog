namespace NeuroBlog.Server.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    public const string UsernameHeader = "X-Username";

    /// <summary>
    /// The acting user, taken from the <c>X-Username</c> header. There is no
    /// authentication in this app, so this is simply the username the client
    /// picked. Returns null when the header is missing, blank or too long.
    /// </summary>
    protected string? CurrentUser
    {
        get
        {
            var raw = Request.Headers[UsernameHeader].ToString();
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            // The client URL-encodes the value so non-ASCII names stay valid.
            var name = Uri.UnescapeDataString(raw).Trim();
            if (name.Length == 0 || name.Length > ContentLimits.MaxUsernameLength)
                return null;

            return name;
        }
    }

    /// <summary>Standard 400 for write requests that arrive without a username.</summary>
    protected ObjectResult MissingUser() =>
        Problem(detail: $"A non-empty '{UsernameHeader}' header is required.", statusCode: StatusCodes.Status400BadRequest);

    /// <summary>
    /// 403 for acting on someone else's content. We can't use the built-in
    /// <c>Forbid()</c> because there is no authentication scheme registered.
    /// </summary>
    protected ObjectResult NotOwner() =>
        Problem(detail: "You can only modify your own content.", statusCode: StatusCodes.Status403Forbidden);
}
