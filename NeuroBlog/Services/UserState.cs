using Microsoft.JSInterop;

namespace NeuroBlog.Services;

/// <summary>
/// Holds the current username. There is no authentication: the user simply
/// picks a name, which we persist in localStorage so it survives reloads.
/// </summary>
public class UserState
{
    private const string StorageKey = "neuroblog.username";
    private readonly IJSRuntime _js;

    public UserState(IJSRuntime js) => _js = js;

    public string? Username { get; private set; }
    public bool IsInitialized { get; private set; }
    public bool HasUser => !string.IsNullOrWhiteSpace(Username);

    /// <summary>Raised whenever the username is set or cleared.</summary>
    public event Action? Changed;

    public async Task InitializeAsync()
    {
        if (IsInitialized)
            return;

        Username = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        IsInitialized = true;
        Changed?.Invoke();
    }

    public async Task SetUsernameAsync(string username)
    {
        Username = username.Trim();
        await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, Username);
        Changed?.Invoke();
    }

    public async Task ClearAsync()
    {
        Username = null;
        await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
        Changed?.Invoke();
    }

    /// <summary>True when the given content author matches the current user.</summary>
    public bool Owns(string author) =>
        HasUser && string.Equals(author, Username, StringComparison.Ordinal);
}
