namespace ErpPortal.Core.Contracts;

public interface IErpHttpClient
{
    Task<T> GetAsync<T>(string url, CancellationToken ct = default) where T : class;
    Task<T> PostAsync<T>(string url, object data, CancellationToken ct = default) where T : class;
    Task<T> PutAsync<T>(string url, object data, CancellationToken ct = default) where T : class;
    Task DeleteAsync(string url, CancellationToken ct = default);
    void SetAuthToken(string? token);
}
