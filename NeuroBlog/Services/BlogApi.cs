namespace NeuroBlog.Services;

/// <summary>Typed wrapper over the backend REST API.</summary>
public class BlogApi
{
    private readonly HttpClient _http;

    public BlogApi(HttpClient http) => _http = http;

    // ---- Articles -------------------------------------------------------

    public async Task<List<ArticleSummaryDto>> GetArticlesAsync() =>
        await _http.GetFromJsonAsync<List<ArticleSummaryDto>>("api/articles") ?? new();

    public async Task<ArticleDto?> GetArticleAsync(Guid id)
    {
        var response = await _http.GetAsync($"api/articles/{id}");
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        return await ReadAsync<ArticleDto>(response);
    }

    public async Task<ArticleDto> CreateArticleAsync(CreateArticleRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/articles", request);
        return await ReadAsync<ArticleDto>(response);
    }

    public async Task<ArticleDto> UpdateArticleAsync(Guid id, UpdateArticleRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/articles/{id}", request);
        return await ReadAsync<ArticleDto>(response);
    }

    public async Task DeleteArticleAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"api/articles/{id}");
        await EnsureSuccessAsync(response);
    }

    // ---- Comments -------------------------------------------------------

    /// <summary>One page of top-level comments (replies excluded).</summary>
    public async Task<PagedResult<CommentDto>> GetCommentsAsync(Guid articleId, int page) =>
        await _http.GetFromJsonAsync<PagedResult<CommentDto>>($"api/articles/{articleId}/comments?page={page}") ?? new();

    /// <summary>One page of the direct replies of a comment.</summary>
    public async Task<PagedResult<CommentDto>> GetRepliesAsync(long commentId, int page) =>
        await _http.GetFromJsonAsync<PagedResult<CommentDto>>($"api/comments/{commentId}/replies?page={page}") ?? new();

    public async Task<CommentDto> AddCommentAsync(Guid articleId, CreateCommentRequest request)
    {
        var response = await _http.PostAsJsonAsync($"api/articles/{articleId}/comments", request);
        return await ReadAsync<CommentDto>(response);
    }

    public async Task<CommentDto> UpdateCommentAsync(long id, UpdateCommentRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/comments/{id}", request);
        return await ReadAsync<CommentDto>(response);
    }

    public async Task<CommentDto> DeleteCommentAsync(long id)
    {
        var response = await _http.DeleteAsync($"api/comments/{id}");
        return await ReadAsync<CommentDto>(response);
    }

    // ---- Helpers --------------------------------------------------------

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        await EnsureSuccessAsync(response);
        var value = await response.Content.ReadFromJsonAsync<T>();
        return value ?? throw new ApiException((int)response.StatusCode, "The server returned an empty response.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var message = await TryReadProblemAsync(response) ?? $"Request failed ({(int)response.StatusCode}).";
        throw new ApiException((int)response.StatusCode, message);
    }

    private static async Task<string?> TryReadProblemAsync(HttpResponseMessage response)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
            if (problem is null)
                return null;

            // Validation errors (ASP.NET ValidationProblemDetails) carry an "errors" map.
            if (problem.Errors is { Count: > 0 })
            {
                var first = problem.Errors.Values.SelectMany(v => v).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(first))
                    return first;
            }

            return problem.Detail ?? problem.Title;
        }
        catch
        {
            return null;
        }
    }

    private class ProblemResponse
    {
        public string? Title { get; set; }
        public string? Detail { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }
    }
}
