using System.Net.Http.Json;
using System.Text.Json;
using ErpPortal.Core.Contracts;

namespace ErpPortal.Infrastructure.Http;

public sealed class ErpHttpClient : IErpHttpClient
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public ErpHttpClient(HttpClient http) => _http = http;

    public async Task<T> GetAsync<T>(string url, CancellationToken ct = default) where T : class
    {
        T? result = await _http.GetFromJsonAsync<T>(url, _jsonOptions, ct);
        return result ?? throw new InvalidOperationException($"Null response from GET {url}");
    }

    public async Task<T> PostAsync<T>(string url, object data, CancellationToken ct = default) where T : class
    {
        HttpResponseMessage response = await _http.PostAsJsonAsync(url, data, _jsonOptions, ct);
        response.EnsureSuccessStatusCode();
        T? result = await response.Content.ReadFromJsonAsync<T>(_jsonOptions, ct);
        return result ?? throw new InvalidOperationException($"Null response from POST {url}");
    }

    public async Task<T> PutAsync<T>(string url, object data, CancellationToken ct = default) where T : class
    {
        HttpResponseMessage response = await _http.PutAsJsonAsync(url, data, _jsonOptions, ct);
        response.EnsureSuccessStatusCode();
        T? result = await response.Content.ReadFromJsonAsync<T>(_jsonOptions, ct);
        return result ?? throw new InvalidOperationException($"Null response from PUT {url}");
    }

    public async Task DeleteAsync(string url, CancellationToken ct = default)
    {
        HttpResponseMessage response = await _http.DeleteAsync(url, ct);
        response.EnsureSuccessStatusCode();
    }

    // In Blazor SSR the token is injected by AuthTokenHandler — this is a no-op here
    // but satisfies the interface for testability.
    public void SetAuthToken(string? token) { }
}
