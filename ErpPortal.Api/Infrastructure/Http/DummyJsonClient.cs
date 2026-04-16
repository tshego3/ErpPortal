// Infrastructure/Http/DummyJsonClient.cs
using System.Net.Http.Json;
using System.Text.Json;
using ErpPortal.Api.Core.Contracts;
using ErpPortal.Api.Core.Domain;

namespace ErpPortal.Api.Infrastructure.Http;

/// <summary>
/// Typed HttpClient for DummyJSON's authenticated endpoints.
/// The Bearer token is injected by <see cref="DummyJsonAuthHandler"/> —
/// this class never touches tokens directly.
/// </summary>
public sealed class DummyJsonClient : IDummyJsonClient
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public DummyJsonClient(HttpClient http) => _http = http;

    public async Task<ProductsResponse> GetProductsAsync(
        int skip = 0, int limit = 30, CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<ProductsResponse>(
            $"/auth/products?limit={limit}&skip={skip}", _jsonOptions, ct)
            ?? throw new InvalidOperationException("Null response from /auth/products");
    }

    public async Task<Product> GetProductByIdAsync(
        int id, CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<Product>(
            $"/auth/products/{id}", _jsonOptions, ct)
            ?? throw new InvalidOperationException($"Null response from /auth/products/{id}");
    }

    public async Task<TodosResponse> GetTodosAsync(
        int skip = 0, int limit = 30, CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<TodosResponse>(
            $"/auth/todos?limit={limit}&skip={skip}", _jsonOptions, ct)
            ?? throw new InvalidOperationException("Null response from /auth/todos");
    }

    public async Task<Todo> GetTodoByIdAsync(
        int id, CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<Todo>(
            $"/auth/todos/{id}", _jsonOptions, ct)
            ?? throw new InvalidOperationException($"Null response from /auth/todos/{id}");
    }
}
