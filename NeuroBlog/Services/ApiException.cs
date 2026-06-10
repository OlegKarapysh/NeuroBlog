namespace NeuroBlog.Services;

/// <summary>Thrown by <see cref="BlogApi"/> when a request fails; carries a user-facing message.</summary>
public class ApiException : Exception
{
    public int StatusCode { get; }

    public ApiException(int statusCode, string message) : base(message) => StatusCode = statusCode;
}
