using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Mvc.Services;

public sealed class ApiClient
{
    private readonly IHttpClientFactory _factory;
    private readonly IHttpContextAccessor _context;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ApiClient(IHttpClientFactory factory, IHttpContextAccessor context)
    {
        _factory = factory;
        _context = context;
    }

    public async Task<(T? Value, string? Cookie)> SignInAsync<T>(string path, object body, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        HttpResponseMessage sent;
        try { sent = await _factory.CreateClient("Api").SendAsync(request, ct); }
        catch (HttpRequestException) { throw new ApiException("RaceDay services are temporarily unavailable.", HttpStatusCode.ServiceUnavailable); }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested) { throw new ApiException("The request took too long. Please try again.", HttpStatusCode.RequestTimeout); }
        using var response = sent;
        await EnsureSuccess(response, ct);
        var cookie = response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault()?.Split(';', 2)[0]
            : null;
        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct), cookie);
    }

    public Task<T?> GetAsync<T>(string path, CancellationToken ct = default) => SendAsync<T>(HttpMethod.Get, path, null, ct);
    public Task<T?> PostAsync<T>(string path, object body, CancellationToken ct = default) => SendAsync<T>(HttpMethod.Post, path, body, ct);
    public Task<T?> PutAsync<T>(string path, object body, CancellationToken ct = default) => SendAsync<T>(HttpMethod.Put, path, body, ct);

    public async Task<(byte[] Content, string ContentType)?> GetImageAsync(
        string path,
        CancellationToken ct = default)
    {
        using var response = await SendRequest(HttpMethod.Get, path, null, ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        await EnsureSuccess(response, ct);

        var content = await response.Content.ReadAsByteArrayAsync(ct);

        var contentType = response.Content.Headers.ContentType?.MediaType
            ?? "application/octet-stream";

        return (content, contentType);
    }

    public async Task DeleteAsync(string path, CancellationToken ct = default)
    {
        using var response = await SendRequest(HttpMethod.Delete, path, null, ct);
        await EnsureSuccess(response, ct);
    }

    public async Task<T?> UploadAsync<T>(string path, IFormFile file, CancellationToken ct = default)
    {
        await using var stream = file.OpenReadStream();
        using var form = new MultipartFormDataContent();
        var content = new StreamContent(stream);
        content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        form.Add(content, "file", Path.GetFileName(file.FileName));
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = form };
        AddApiCookie(request);
        HttpResponseMessage sent;
        try { sent = await _factory.CreateClient("Api").SendAsync(request, ct); }
        catch (HttpRequestException) { throw new ApiException("RaceDay services are temporarily unavailable.", HttpStatusCode.ServiceUnavailable); }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested) { throw new ApiException("The request took too long. Please try again.", HttpStatusCode.RequestTimeout); }
        using var response = sent;
        await EnsureSuccess(response, ct);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
    }

    public async Task PostNoContentAsync(string path, object body, CancellationToken ct = default)
    {
        using var response = await SendRequest(HttpMethod.Post, path, body, ct);
        await EnsureSuccess(response, ct);
    }

    public async Task<T?> PostWithResponseAsync<T>(string path, object body, CancellationToken ct = default)
    {
        using var response = await SendRequest(HttpMethod.Post, path, body, ct);
        await EnsureSuccess(response, ct);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
    }

    private async Task<T?> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        using var response = await SendRequest(method, path, body, ct);
        await EnsureSuccess(response, ct);
        if (response.StatusCode == HttpStatusCode.NoContent) return default;
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
    }

    private async Task<HttpResponseMessage> SendRequest(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        var request = new HttpRequestMessage(method, path);
        if (body is not null) request.Content = JsonContent.Create(body);
        AddApiCookie(request);
        try
        {
            return await _factory.CreateClient("Api").SendAsync(request, ct);
        }
        catch (HttpRequestException)
        {
            request.Dispose();
            throw new ApiException("RaceDay services are temporarily unavailable.", HttpStatusCode.ServiceUnavailable);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            request.Dispose();
            throw new ApiException("The request took too long. Please try again.", HttpStatusCode.RequestTimeout);
        }
    }

    private void AddApiCookie(HttpRequestMessage request)
    {
        var cookie = _context.HttpContext?.Session.GetString("ApiCookie");
        if (!string.IsNullOrWhiteSpace(cookie)) request.Headers.TryAddWithoutValidation("Cookie", cookie);
    }

    private async Task EnsureSuccess(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;
        // The API session has expired: clear the MVC session too so the next
        // request is redirected to the login page instead of staying "signed in".
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            _context.HttpContext?.Session.Clear();
        var friendly = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Your session has expired. Please sign in again.",
            HttpStatusCode.Forbidden => "You do not have permission to perform that action.",
            HttpStatusCode.NotFound => "That item could not be found.",
            HttpStatusCode.Conflict => "This action conflicts with the current event or enrolment state.",
            HttpStatusCode.BadRequest => "Please check the information and try again.",
            HttpStatusCode.ServiceUnavailable => "Image storage is not configured or is temporarily unavailable.",
            _ => "The request could not be completed. Please try again."
        };
        var detail = await response.Content.ReadAsStringAsync(ct);
        if (response.StatusCode == HttpStatusCode.BadRequest && !string.IsNullOrWhiteSpace(detail))
        {
            try
            {
                var problem = JsonDocument.Parse(detail).RootElement;
                if (problem.TryGetProperty("errors", out _)) friendly = "Please correct the highlighted fields.";
            }
            catch (JsonException) { }
        }
        throw new ApiException(friendly, response.StatusCode);
    }
}

public sealed class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public ApiException(string message, HttpStatusCode statusCode) : base(message) => StatusCode = statusCode;
}
