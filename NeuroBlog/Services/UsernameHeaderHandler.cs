namespace NeuroBlog.Services;

/// <summary>
/// Attaches the current username to every API request as the <c>X-Username</c>
/// header. The value is URL-encoded so that non-ASCII usernames remain a valid
/// HTTP header value; the server decodes it.
/// </summary>
public class UsernameHeaderHandler : DelegatingHandler
{
    public const string HeaderName = "X-Username";
    private readonly UserState _user;

    public UsernameHeaderHandler(UserState user) => _user = user;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_user.HasUser)
        {
            request.Headers.Remove(HeaderName);
            request.Headers.Add(HeaderName, Uri.EscapeDataString(_user.Username!));
        }

        return base.SendAsync(request, cancellationToken);
    }
}
