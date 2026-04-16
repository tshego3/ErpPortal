namespace ErpPortal.Core.Contracts;

public interface IRepository<T> where T : class
{
    Task<(IReadOnlyList<T> Data, int Total)> GetAllAsync(int skip = 0, int limit = 50, CancellationToken ct = default);
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<T> CreateAsync(T entity, CancellationToken ct = default);
    Task<T> UpdateAsync(int id, T entity, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
