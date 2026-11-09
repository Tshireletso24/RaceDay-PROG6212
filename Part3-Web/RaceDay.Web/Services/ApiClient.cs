using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Web.Models;

namespace RaceDay.Web.Services;

/// <summary>The outcome of an API call: success with data, or a failure with a friendly message.</summary>
public class ApiResult<T>
{
    public bool Success { get; init; }
    public HttpStatusCode StatusCode { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }

    public bool Unauthorized => StatusCode == HttpStatusCode.Unauthorized;
    public bool Forbidden => StatusCode == HttpStatusCode.Forbidden;
    public bool NotFound => StatusCode == HttpStatusCode.NotFound;
}

/// <summary>
/// Talks to the RaceDay API. It forwards the logged-in user's API session cookie on every call,
/// so the API keeps enforcing roles. All failures come back as friendly messages, never exceptions.
/// </summary>
public class ApiClient
{
    private const string ApiSessionCookieName = ".RaceDay.Session";
    private const string UnavailableMessage =
        "We couldn't reach the RaceDay service right now. Please try again in a moment.";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _context;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(HttpClient http, IHttpContextAccessor context, ILogger<ApiClient> logger)
    {
        _http = http;
        _context = context;
        _logger = logger;
    }

    public Task<ApiResult<T>> GetAsync<T>(string url) =>
        SendAsync<T>(HttpMethod.Get, url);

    public Task<ApiResult<T>> PostAsync<T>(string url, object? body = null) =>
        SendAsync<T>(HttpMethod.Post, url, body == null ? null : ToJson(body));

    public Task<ApiResult<T>> PutAsync<T>(string url, object body) =>
        SendAsync<T>(HttpMethod.Put, url, ToJson(body));

    public Task<ApiResult<object>> DeleteAsync(string url) =>
        SendAsync<object>(HttpMethod.Delete, url);

    /// <summary>Sends an uploaded image to the API as multipart/form-data (the API stores it in Azure).</summary>
    public Task<ApiResult<T>> PostFileAsync<T>(string url, IFormFile file)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(file.OpenReadStream());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", file.FileName);
        return SendAsync<T>(HttpMethod.Post, url, content);
    }

    /// <summary>Logs in against the API and returns the user plus the API session cookie to keep.</summary>
    public async Task<(ApiResult<UserDto> Result, string? SessionCookie)> LoginAsync(string email, string password)
    {
        try
        {
            // No old cookie is sent, so the API always starts a fresh session and sets a new cookie
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/login")
            {
                Content = ToJson(new { email, password })
            };
            using var response = await _http.SendAsync(request);

            string? cookie = null;
            if (response.Headers.TryGetValues("Set-Cookie", out var values))
            {
                cookie = values
                    .FirstOrDefault(v => v.StartsWith(ApiSessionCookieName + "=", StringComparison.Ordinal))
                    ?.Split(';')[0];
            }

            return (await ReadAsync<UserDto>(response), cookie);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Could not reach the API during login");
            return (Fail<UserDto>(HttpStatusCode.ServiceUnavailable, UnavailableMessage), null);
        }
    }

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, HttpContent? content = null)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url) { Content = content };

            var cookie = _context.HttpContext?.Session.GetString(SessionKeys.ApiCookie);
            if (!string.IsNullOrEmpty(cookie))
                request.Headers.Add("Cookie", cookie);

            using var response = await _http.SendAsync(request);
            return await ReadAsync<T>(response);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Could not reach the API: {Method} {Url}", method, url);
            return Fail<T>(HttpStatusCode.ServiceUnavailable, UnavailableMessage);
        }
    }

    private static async Task<ApiResult<T>> ReadAsync<T>(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            T? data = default;
            if (response.StatusCode != HttpStatusCode.NoContent && response.Content.Headers.ContentLength != 0)
            {
                try { data = await response.Content.ReadFromJsonAsync<T>(JsonOptions); }
                catch (JsonException) { /* an empty or non-JSON body is fine */ }
            }
            return new ApiResult<T> { Success = true, StatusCode = response.StatusCode, Data = data };
        }

        return new ApiResult<T>
        {
            Success = false,
            StatusCode = response.StatusCode,
            Error = await ExtractErrorAsync(response)
        };
    }

    /// <summary>Turns an API error response into a short message that is safe to show a user.</summary>
    private static async Task<string> ExtractErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(body))
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                        return message.GetString()!;

                    if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                    {
                        var messages = errors.EnumerateObject()
                            .Where(p => p.Value.ValueKind == JsonValueKind.Array)
                            .SelectMany(p => p.Value.EnumerateArray())
                            .Where(e => e.ValueKind == JsonValueKind.String)
                            .Select(e => e.GetString()!)
                            .ToList();
                        var joined = string.Join(" ", messages);
                        if (joined.Length > 0) return joined;
                    }
                }
            }
        }
        catch (JsonException) { /* fall through to the default message */ }

        return response.StatusCode switch
        {
            HttpStatusCode.BadRequest => "Some of the details you entered were not valid.",
            HttpStatusCode.Unauthorized => "Your session has expired. Please log in again.",
            HttpStatusCode.Forbidden => "You don't have permission to do that.",
            HttpStatusCode.NotFound => "We couldn't find what you were looking for.",
            HttpStatusCode.Conflict => "That conflicts with something that already exists.",
            HttpStatusCode.ServiceUnavailable => UnavailableMessage,
            _ => "Something went wrong. Please try again."
        };
    }

    private static ApiResult<T> Fail<T>(HttpStatusCode code, string message) =>
        new() { Success = false, StatusCode = code, Error = message };

    private static JsonContent ToJson(object body) =>
        JsonContent.Create(body, body.GetType(), null, JsonOptions);
}
