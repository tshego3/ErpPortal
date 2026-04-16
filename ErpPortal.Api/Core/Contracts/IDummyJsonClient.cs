using ErpPortal.Api.Core.Domain;

namespace ErpPortal.Api.Core.Contracts;

/// <summary>
/// Typed HttpClient wrapper for DummyJSON's protected /auth/* endpoints.
/// The Bearer token is injected transparently by <see cref="DummyJsonAuthHandler"/>.
/// </summary>
public interface IDummyJsonClient
{
    Task<ProductsResponse> GetProductsAsync(int skip = 0, int limit = 30, CancellationToken ct = default);
    Task<Product> GetProductByIdAsync(int id, CancellationToken ct = default);
    Task<TodosResponse> GetTodosAsync(int skip = 0, int limit = 30, CancellationToken ct = default);
    Task<Todo> GetTodoByIdAsync(int id, CancellationToken ct = default);
}
